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
        bool IsGameStart(string line);

        bool IsGameEnd(string line);

        ParserCheckInfo IsDealerSeat(string line);

        ParserCheckInfo IsSeatInfo(string line);

        ParserCheckInfo IsGameId(string line);

        ParserCheckInfo IsTableId(string line);

        ParserCheckInfo IsSmallBlindPost(string line);

        ParserCheckInfo IsBigBlindPost(string line);

        bool IsPreflopStart(string line);

        ParserCheckInfo IsFlopStart(string line);

        ParserCheckInfo IsTurnStart(string line);

        ParserCheckInfo IsRiverStart(string line);

        ParserCheckInfo IsFold(string line);

        ParserCheckInfo IsRaise(string line);

        ParserCheckInfo IsCall(string line);

        ParserCheckInfo IsBet(string line);

        ParserCheckInfo IsCheck(string line);

        ParserCheckInfo IsShowCards(string line);

        bool IsParseGameIndicator(string line);

        bool IsCorruptionIndicator(string line);

        int GetDealerSeat(ParserCheckInfo match);

        SeatInfo GetSeatInfo(ParserCheckInfo match);

        string GetGameId(ParserCheckInfo match);

        string GetTableId(ParserCheckInfo match);

        BetInfo GetSmallBlindPost(ParserCheckInfo match);

        BetInfo GetBigBlindPost(ParserCheckInfo match);

        Card[] GetFlopCards(ParserCheckInfo match);

        Card GetTurnCard(ParserCheckInfo match);

        Card GetRiverCard(ParserCheckInfo match);

        string GetPlayerWhoFolded(ParserCheckInfo match);

        string GetPlayerWhoChecked(ParserCheckInfo match);

        BetInfo GetRaise(ParserCheckInfo match);

        BetInfo GetCall(ParserCheckInfo match);

        BetInfo GetBet(ParserCheckInfo match);

        Tuple<string, Card[]> GetShownCards(ParserCheckInfo match);
    }
}