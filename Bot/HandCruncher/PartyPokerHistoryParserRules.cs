using Poker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HandCruncher
{
    public class PartyPokerHistoryParserRules : IHistoryParserRules
    {
        private const string CURRENCY_MATCH = @"\$([0-9\.,]+)";

        private static RegexOptions sFlags = RegexOptions.Compiled;

        private static Regex s_dealerSeatExpr = new Regex("^Seat ([0-9]+) is the button$", sFlags);

        private static Regex s_seatInfoExpr = new Regex(@"^Seat ([0-9]+): ([^ ]+) \( " + CURRENCY_MATCH + @" USD \)", sFlags);

        private static Regex s_gameIdExpr = new Regex(@"^#Game No : ([0-9]+) $", sFlags);

        private static Regex s_tableIdExpr = new Regex(@"^Table Table  ([0-9]+)", sFlags);

        private static Regex s_smallBlindPostExpr = new Regex(@"^([^ ]+) posts small blind \[" + CURRENCY_MATCH, sFlags);

        private static Regex s_bigBlindPostExpr = new Regex(@"^([^ ]+) posts big blind \[" + CURRENCY_MATCH, sFlags);

        private static Regex s_flopStartExpr = new Regex(@"^\*\* Dealing Flop \*\* \[([^\]]+)\]", sFlags);

        private static Regex s_turnStartExpr = new Regex(@"^\*\* Dealing Turn \*\* \[([^\]]+)\]", sFlags);

        private static Regex s_riverStartExpr = new Regex(@"^\*\* Dealing River \*\* \[([^\]]+)\]", sFlags);

        private static Regex s_foldExpr = new Regex(@"^([^ ]+) folds$", sFlags);

        private static Regex s_raiseExpr = new Regex(@"^([^ ]+) raises \[" + CURRENCY_MATCH, sFlags);

        private static Regex s_callExpr = new Regex(@"^([^ ]+) calls \[" + CURRENCY_MATCH, sFlags);

        private static Regex s_betExpr = new Regex(@"^([^ ]+) bets \[" + CURRENCY_MATCH, sFlags);

        private static Regex s_checkExpr = new Regex(@"^([^ ]+) checks$", sFlags);

        private static Regex s_showsCardsExpr = new Regex(@"^([^ ]+) (shows|doesn't show) \[([^\]]+)\]", sFlags);

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
            string[] cardStrings = m.Groups[1].Value.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);

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
            return GetBet(match);
        }

        public Card GetRiverCard(ParserCheckInfo match)
        {
            m_bets.Clear();
            Match m = (Match)match.State;
            return CardParser.MakeCard(m.Groups[1].Value.Trim());
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
            Card[] cards = m.Groups[3].Value.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(CardParser.MakeCard).ToArray();

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
            return CardParser.MakeCard(m.Groups[1].Value.Trim());
        }

        public bool IsGameStart(string line)
        {
            return s_gameIdExpr.IsMatch(line);
        }

        public bool IsGameEnd(string line)
        {
            return line == string.Empty;// line.StartsWith("Game #") && line.EndsWith(" starts.");
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
            return line.StartsWith("** Dealing down cards **");
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
            bool corruptSeat = line.StartsWith("Seat ") && !line.EndsWith("the button") && (!line.EndsWith("USD )") || !s_seatInfoExpr.IsMatch(line));
            return line == "G" || line.StartsWith("Connection Lost") || line.Contains("could not respond in time.") || corruptSeat;
        }
    }
}