﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Poker;

namespace HandCruncher
{
    public class PokerstarsHistoryParserRules : IHistoryParserRules
    {
        private const string CURRENCY_MATCH = @"\$([0-9\.,]+)";

        private static RegexOptions sFlags = RegexOptions.Compiled;

        private static Regex s_dealerSeatExpr = new Regex("#([0-9]+) is the button$", sFlags);

        private static Regex s_seatInfoExpr = new Regex(@"^Seat ([0-9]+): ([^ ]+) \(" + CURRENCY_MATCH + @" in chips\)", sFlags);

        private static Regex s_gameIdExpr = new Regex(@"^PokerStars Game|Hand #([^:]+)", sFlags);

        private static Regex s_tableIdExpr = new Regex(@"Table '([^']+)'", sFlags);

        private static Regex s_smallBlindPostExpr = new Regex(@"^([^:]+): posts small blind " + CURRENCY_MATCH, sFlags);

        private static Regex s_bigBlindPostExpr = new Regex(@"^([^:]+): posts big blind " + CURRENCY_MATCH, sFlags);

        private static Regex s_flopStartExpr = new Regex(@"^\*\*\* FLOP \*\*\* \[([^\]]+)\]", sFlags);

        private static Regex s_turnStartExpr = new Regex(@"^\*\*\* TURN \*\*\* \[[^\]]+\] \[([^\]]+)\]", sFlags);

        private static Regex s_riverStartExpr = new Regex(@"^\*\*\* RIVER \*\*\* \[[^\]]+\] \[([^\]]+)\]", sFlags);

        private static Regex s_foldExpr = new Regex(@"^([^:]+): folds", sFlags);

        private static Regex s_raiseExpr = new Regex(@"^([^:]+): raises " + CURRENCY_MATCH + " to " + CURRENCY_MATCH, sFlags);

        private static Regex s_callExpr = new Regex(@"^([^:]+): calls " + CURRENCY_MATCH, sFlags);

        private static Regex s_betExpr = new Regex(@"^([^:]+): bets " + CURRENCY_MATCH, sFlags);

        private static Regex s_checkExpr = new Regex(@"^([^:]+): checks", sFlags);

        private static Regex s_showsCardsExpr = new Regex(@"^([^:]+): shows \[([^\]]+)\]", sFlags);

        private Dictionary<string, int> m_bets = new Dictionary<string, int>();

        private BetInfo ExtractBetInfo(ParserCheckInfo match)
        {
            var m = (Match)match.State;
            string name = m.Groups[1].Value;
            decimal v = decimal.Parse(m.Groups[2].Value.Replace(",", ""));
            v *= 100;
            int a = (int)v;
            if (!m_bets.ContainsKey(name))
            {
                m_bets[name] = 0;
            }
            m_bets[name] += a;
            return new BetInfo(name, a);
        }

        public BetInfo GetBet(ParserCheckInfo match)
        {
            return ExtractBetInfo(match);
        }

        public BetInfo GetBigBlindPost(ParserCheckInfo match)
        {
            return ExtractBetInfo(match);
        }

        public BetInfo GetCall(ParserCheckInfo match)
        {
            return ExtractBetInfo(match);
        }

        public int GetDealerSeat(ParserCheckInfo match)
        {
            m_bets.Clear();

            Match m = (Match)match.State;
            return int.Parse(m.Groups[1].Value);
        }

        public Card[] GetFlopCards(ParserCheckInfo match)
        {
            m_bets.Clear();

            Match m = (Match)match.State;
            string[] cardStrings = m.Groups[1].Value.Split(' ');

            return cardStrings.Select(CardParser.MakeCard).ToArray();
        }

        public string GetGameId(ParserCheckInfo match)
        {
            Match m = (Match)match.State;
            return m.Groups[1].Value;
        }

        public string GetPlayerWhoChecked(ParserCheckInfo match)
        {
            Match m = (Match)match.State;
            return m.Groups[1].Value;
        }

        public string GetPlayerWhoFolded(ParserCheckInfo match)
        {
            Match m = (Match)match.State;
            return m.Groups[1].Value;
        }

        public BetInfo GetRaise(ParserCheckInfo match)
        {
            Match m = (Match)match.State;
            string name = m.Groups[1].Value;
            decimal v = decimal.Parse(m.Groups[3].Value.Replace(",", ""));
            int a = (int)(v * 100);
            if (!m_bets.ContainsKey(name))
            {
                m_bets[name] = 0;
            }
            int bet = m_bets[name];

            var amt = a - bet;
            m_bets[name] += amt;

            return new BetInfo(name, amt);
        }

        public Card GetRiverCard(ParserCheckInfo match)
        {
            m_bets.Clear();
            Match m = (Match)match.State;
            return CardParser.MakeCard(m.Groups[1].Value);
        }

        public SeatInfo GetSeatInfo(ParserCheckInfo match)
        {
            Match m = (Match)match.State;
            int number = int.Parse(m.Groups[1].Value);
            string name = m.Groups[2].Value;
            decimal v = decimal.Parse(m.Groups[3].Value.Replace(",", ""));
            int a = (int)(v * 100);

            return new SeatInfo(number, name, a);
        }

        public Tuple<string, Card[]> GetShownCards(ParserCheckInfo match)
        {
            Match m = (Match)match.State;
            string name = m.Groups[1].Value;
            Card[] cards = m.Groups[2].Value.Split(' ').Select(CardParser.MakeCard).ToArray();

            return Tuple.Create(name, cards);
        }

        public BetInfo GetSmallBlindPost(ParserCheckInfo match)
        {
            return ExtractBetInfo(match);
        }

        public string GetTableId(ParserCheckInfo match)
        {
            Match m = (Match)match.State;
            m_bets.Clear();
            return m.Groups[1].Value;
        }

        public Card GetTurnCard(ParserCheckInfo match)
        {
            Match m = (Match)match.State;
            m_bets.Clear();
            return CardParser.MakeCard(m.Groups[1].Value);
        }

        public bool IsGameStart(string line)
        {
            return line.StartsWith("PokerStars Hand #") || line.StartsWith("PokerStars Game #");
        }

        public bool IsGameEnd(string line)
        {
            return line.StartsWith("*** SUM");
        }

        public ParserCheckInfo IsDealerSeat(string line)
        {
            var match = s_dealerSeatExpr.Match(line);
            return new ParserCheckInfo() { IsMatch = match.Success, State = match };
        }

        public ParserCheckInfo IsSeatInfo(string line)
        {
            var match = s_seatInfoExpr.Match(line);
            return new ParserCheckInfo() { IsMatch = match.Success, State = match };
        }

        public ParserCheckInfo IsGameId(string line)
        {
            var match = s_gameIdExpr.Match(line);
            return new ParserCheckInfo() { IsMatch = match.Success, State = match };
        }

        public ParserCheckInfo IsTableId(string line)
        {
            var match = s_tableIdExpr.Match(line);
            return new ParserCheckInfo() { IsMatch = match.Success, State = match };
        }

        public ParserCheckInfo IsSmallBlindPost(string line)
        {
            var match = s_smallBlindPostExpr.Match(line);
            return new ParserCheckInfo() { IsMatch = match.Success, State = match };
        }

        public ParserCheckInfo IsBigBlindPost(string line)
        {
            var match = s_bigBlindPostExpr.Match(line);
            return new ParserCheckInfo() { IsMatch = match.Success, State = match };
        }

        public bool IsPreflopStart(string line)
        {
            return line.StartsWith("*** HOLE CARDS");
        }

        public ParserCheckInfo IsFlopStart(string line)
        {
            var match = s_flopStartExpr.Match(line);
            return new ParserCheckInfo() { IsMatch = match.Success, State = match };
        }

        public ParserCheckInfo IsTurnStart(string line)
        {
            var match = s_turnStartExpr.Match(line);
            return new ParserCheckInfo() { IsMatch = match.Success, State = match };
        }

        public ParserCheckInfo IsRiverStart(string line)
        {
            var match = s_riverStartExpr.Match(line);
            return new ParserCheckInfo() { IsMatch = match.Success, State = match };
        }

        public ParserCheckInfo IsFold(string line)
        {
            var match = s_foldExpr.Match(line);
            return new ParserCheckInfo() { IsMatch = match.Success, State = match };
        }

        public ParserCheckInfo IsRaise(string line)
        {
            var match = s_raiseExpr.Match(line);
            return new ParserCheckInfo() { IsMatch = match.Success, State = match };
        }

        public ParserCheckInfo IsCall(string line)
        {
            var match = s_callExpr.Match(line);
            return new ParserCheckInfo() { IsMatch = match.Success, State = match };
        }

        public ParserCheckInfo IsBet(string line)
        {
            var match = s_betExpr.Match(line);
            return new ParserCheckInfo() { IsMatch = match.Success, State = match };
        }

        public ParserCheckInfo IsCheck(string line)
        {
            var match = s_checkExpr.Match(line);
            return new ParserCheckInfo() { IsMatch = match.Success, State = match };
        }

        public ParserCheckInfo IsShowCards(string line)
        {
            var match = s_showsCardsExpr.Match(line);
            return new ParserCheckInfo() { IsMatch = match.Success, State = match };
        }

        public bool IsParseGameIndicator(string line)
        {
            return s_showsCardsExpr.IsMatch(line);
        }

        public bool IsCorruptionIndicator(string line)
        {
            bool corruptSeat = line.StartsWith("Seat") && line.TrimEnd(' ').EndsWith("chips)") && !s_seatInfoExpr.IsMatch(line);
            bool corruptButton = line.StartsWith("Table") && !s_dealerSeatExpr.IsMatch(line);

            return corruptButton || corruptSeat;
        }
    }
}