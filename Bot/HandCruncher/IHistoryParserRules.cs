using Poker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HandCruncher
{
    public interface IHistoryParserRules
    {
        Regex GameStartExpr { get; }
        Regex GameEndExpr { get; }

        Regex DealerSeatExpr { get; }
        Regex SeatInfoExpr { get; }

        Regex GameIdExpr { get; }
        Regex TableIdExpr { get; }

        Regex SmallBlindPostExpr { get; }
        Regex BigBlindPostExpr { get; }

        Regex PreflopStartExpr { get; }
        Regex FlopStartExpr { get; }
        Regex TurnStartExpr { get; }
        Regex RiverStartExpr { get; }

        Regex FoldExpr { get; }
        Regex RaiseExpr { get; }
        Regex CallExpr { get; }
        Regex BetExpr { get; }
        Regex CheckExpr { get; }

        Regex ShowsCardsExpr { get; }

        int GetDealerSeat(MatchInfo match);

        SeatInfo GetSeatInfo(MatchInfo match);

        string GetGameId(MatchInfo match);

        string GetTableId(MatchInfo match);

        BetInfo GetSmallBlindPost(MatchInfo match);

        BetInfo GetBigBlindPost(MatchInfo match);

        Card[] GetFlopCards(MatchInfo match);

        Card GetTurnCard(MatchInfo match);

        Card GetRiverCard(MatchInfo match);

        string GetPlayerWhoFolded(MatchInfo match);

        string GetPlayerWhoChecked(MatchInfo match);

        BetInfo GetRaise(MatchInfo match);

        BetInfo GetCall(MatchInfo match);

        BetInfo GetBet(MatchInfo match);

        Tuple<string, Card[]> GetShownCards(MatchInfo match);
    }
}