using System;
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
        private const string CURRENCY_MATCH = @"\$([0-9\.]+)";

        public RegexOptions sFlags = RegexOptions.None;
        public Regex GameStartExpr => new Regex("^PokerStars Game #", sFlags);

        public Regex GameEndExpr => new Regex(@"^\*\*\* SUMMARY \*\*\*", sFlags);

        public Regex DealerSeatExpr => new Regex("#([0-9]) is the button$", sFlags);

        public Regex SeatInfoExpr => new Regex(@"^Seat ([0-9]): ([^ ]+) \(" + CURRENCY_MATCH + @" in chips\)", sFlags);

        public Regex GameIdExpr => new Regex(@"^PokerStars Game #([^:]+)", sFlags);

        public Regex TableIdExpr => new Regex(@"Table '([^']+)'", sFlags);

        public Regex SmallBlindPostExpr => new Regex(@"^([^:]+): posts small blind " + CURRENCY_MATCH, sFlags);

        public Regex BigBlindPostExpr => new Regex(@"^([^:]+): posts big blind " + CURRENCY_MATCH, sFlags);

        public Regex PreflopStartExpr => new Regex(@"^\*\*\* HOLE CARDS \*\*\*", sFlags);

        public Regex FlopStartExpr => new Regex(@"^\*\*\* FLOP \*\*\* \[([^\]]+)\]", sFlags);

        public Regex TurnStartExpr => new Regex(@"^\*\*\* TURN \*\*\* \[[^\]]+\] \[([^\]]+)\]", sFlags);

        public Regex RiverStartExpr => new Regex(@"^\*\*\* RIVER \*\*\* \[[^\]]+\] \[([^\]]+)\]", sFlags);

        public Regex FoldExpr => new Regex(@"^([^:]+): folds", sFlags);

        public Regex RaiseExpr => new Regex(@"^([^:]+): raises " + CURRENCY_MATCH + " to " + CURRENCY_MATCH, sFlags);

        public Regex CallExpr => new Regex(@"^([^:]+): calls " + CURRENCY_MATCH, sFlags);

        public Regex BetExpr => new Regex(@"^([^:]+): bets " + CURRENCY_MATCH, sFlags);

        public Regex CheckExpr => new Regex(@"^([^:]+): checks", sFlags);

        public Regex ShowsCardsExpr => new Regex(@"^([^:]+): shows \[([^\]]+)\]", sFlags);

        private Dictionary<string, int> m_bets = new Dictionary<string, int>();

        private BetInfo ExtractBetInfo(MatchInfo match)
        {
            string name = match.Match.Groups[1].Value;
            double v = double.Parse(match.Match.Groups[2].Value);
            v *= 100;
            int a = (int)v;
            m_bets[name] = a;
            return new BetInfo(name, a);
        }

        public BetInfo GetBet(MatchInfo match)
        {
            return ExtractBetInfo(match);
        }

        public BetInfo GetBigBlindPost(MatchInfo match)
        {
            return ExtractBetInfo(match);
        }

        public BetInfo GetCall(MatchInfo match)
        {
            return ExtractBetInfo(match);
        }

        public int GetDealerSeat(MatchInfo match)
        {
            m_bets.Clear();
            return int.Parse(match.Match.Groups[1].Value);
        }

        public Card[] GetFlopCards(MatchInfo match)
        {
            m_bets.Clear();
            string[] cardStrings = match.Match.Groups[1].Value.Split(' ');

            return cardStrings.Select(CardParser.MakeCard).ToArray();
        }

        public string GetGameId(MatchInfo match)
        {
            return match.Match.Groups[1].Value;
        }

        public string GetPlayerWhoChecked(MatchInfo match)
        {
            return match.Match.Groups[1].Value;
        }

        public string GetPlayerWhoFolded(MatchInfo match)
        {
            return match.Match.Groups[1].Value;
        }

        public BetInfo GetRaise(MatchInfo match)
        {
            string name = match.Match.Groups[1].Value;
            double v = double.Parse(match.Match.Groups[3].Value);
            int a = (int)(v * 100);
            int bet = 0;
            m_bets.TryGetValue(name, out bet);
            return new BetInfo(name, a - bet);
        }

        public Card GetRiverCard(MatchInfo match)
        {
            m_bets.Clear();
            return CardParser.MakeCard(match.Match.Groups[1].Value);
        }

        public SeatInfo GetSeatInfo(MatchInfo match)
        {
            int number = int.Parse(match.Match.Groups[1].Value);
            string name = match.Match.Groups[2].Value;
            double v = double.Parse(match.Match.Groups[3].Value);
            int a = (int)(v * 100);

            return new SeatInfo(number, name, a);
        }

        public Tuple<string, Card[]> GetShownCards(MatchInfo match)
        {
            string name = match.Match.Groups[1].Value;
            Card[] cards = match.Match.Groups[2].Value.Split(' ').Select(CardParser.MakeCard).ToArray();

            return Tuple.Create(name, cards);
        }

        public BetInfo GetSmallBlindPost(MatchInfo match)
        {
            return ExtractBetInfo(match);
        }

        public string GetTableId(MatchInfo match)
        {
            m_bets.Clear();
            return match.Match.Groups[1].Value;
        }

        public Card GetTurnCard(MatchInfo match)
        {
            m_bets.Clear();
            return CardParser.MakeCard(match.Match.Groups[1].Value);
        }
    }
}