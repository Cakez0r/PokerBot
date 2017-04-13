using Poker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HandCruncher
{
    public class HandHistoryParser
    {
        private IHistoryParserRules m_rules;

        public HandHistoryParser(IHistoryParserRules rules)
        {
            m_rules = rules ?? throw new ArgumentNullException(nameof(rules));
        }

        public IEnumerable<GameStateLog> GetGames(string[] lines)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                if (m_rules.IsGameStart(lines[i]))
                {
                    bool parse = false;
                    bool corrupt = false;
                    int j = i;

                    try
                    {
                        for (j = i; !m_rules.IsGameEnd(lines[j]); j++)
                        {
                            if (!parse && m_rules.IsParseGameIndicator(lines[j]))
                            {
                                parse = true;
                            }

                            if (!corrupt && m_rules.IsCorruptionIndicator(lines[j]))
                            {
                                //Console.WriteLine("Corrupt");
                                corrupt = true;
                                break;
                            }
                        }
                    }
                    catch (IndexOutOfRangeException)
                    {
                        Console.WriteLine("Failed to find game end");
                        corrupt = true;
                    }

                    if (parse && !corrupt)
                    {
                        ArraySegment<string> gameLines = new ArraySegment<string>(lines, i, j - i);
                        GameStateLog log = null;
                        try
                        {
                            log = GetLog(gameLines);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }

                        if (log != null)
                        {
                            yield return log;
                        }
                    }
                }
            }
        }

        private GameStateLog GetLog(IList<string> lines)
        {
            GameStateLog log = new GameStateLog();
            IList<NamedGameAction> currentActionList = null;

            foreach (string line in lines)
            {
                ParserCheckInfo pci = null;

                pci = m_rules.IsFold(line);
                if (pci.IsMatch)
                {
                    string folder = m_rules.GetPlayerWhoFolded(pci);
                    currentActionList.Add(new NamedGameAction(folder, GameActionType.Fold));
                    continue;
                }

                pci = m_rules.IsBet(line);
                if (pci.IsMatch)
                {
                    var bi = m_rules.GetBet(pci);
                    currentActionList.Add(new NamedGameAction(bi.Name, GameActionType.Bet, bi.Amount, true));
                    continue;
                }

                pci = m_rules.IsCall(line);
                if (pci.IsMatch)
                {
                    var bi = m_rules.GetCall(pci);
                    currentActionList.Add(new NamedGameAction(bi.Name, GameActionType.Bet, bi.Amount, false));
                    continue;
                }

                pci = m_rules.IsRaise(line);
                if (pci.IsMatch)
                {
                    var bi = m_rules.GetRaise(pci);
                    currentActionList.Add(new NamedGameAction(bi.Name, GameActionType.Bet, bi.Amount, true));
                    continue;
                }

                pci = m_rules.IsBigBlindPost(line);
                if (pci.IsMatch)
                {
                    var bi = m_rules.GetBigBlindPost(pci);
                    log.BigBlind = bi.Amount;
                    continue;
                }

                pci = m_rules.IsCheck(line);
                if (pci.IsMatch)
                {
                    string checker = m_rules.GetPlayerWhoChecked(pci);
                    currentActionList.Add(new NamedGameAction(checker, GameActionType.Check));
                    continue;
                }

                pci = m_rules.IsDealerSeat(line);
                if (pci.IsMatch)
                {
                    int button = m_rules.GetDealerSeat(pci);
                    log.DealerSeat = button;
                    continue;
                }

                pci = m_rules.IsFlopStart(line);
                if (pci.IsMatch)
                {
                    currentActionList = log.FlopActions;
                    var cards = m_rules.GetFlopCards(pci);
                    for (int i = 0; i < 3; i++)
                    {
                        log.BoardCards.Add(cards[i]);
                    }
                    continue;
                }

                pci = m_rules.IsGameId(line);
                if (pci.IsMatch)
                {
                    string id = m_rules.GetGameId(pci);
                    log.GameId = id;
                    continue;
                }

                bool preflopStart = m_rules.IsPreflopStart(line);
                if (preflopStart)
                {
                    currentActionList = log.PreflopActions;
                    continue;
                }

                pci = m_rules.IsRiverStart(line);
                if (pci.IsMatch)
                {
                    currentActionList = log.RiverActions;
                    var card = m_rules.GetRiverCard(pci);
                    log.BoardCards.Add(card);
                    continue;
                }

                pci = m_rules.IsSeatInfo(line);
                if (pci.IsMatch)
                {
                    var si = m_rules.GetSeatInfo(pci);
                    log.Seats[si.Name] = si.SeatNumber;
                    log.StartBalances[si.Name] = si.Balance;
                    continue;
                }

                pci = m_rules.IsShowCards(line);
                if (pci.IsMatch)
                {
                    var cards = m_rules.GetShownCards(pci);
                    log.KnownHoleCards[cards.Item1] = cards.Item2;
                    continue;
                }

                pci = m_rules.IsSmallBlindPost(line);
                if (pci.IsMatch)
                {
                    var bi = m_rules.GetSmallBlindPost(pci);
                    log.SmallBlind = bi.Amount;
                    continue;
                }

                pci = m_rules.IsTableId(line);
                if (pci.IsMatch)
                {
                    string tableId = m_rules.GetTableId(pci);
                    log.TableId = tableId;
                    continue;
                }

                pci = m_rules.IsTurnStart(line);
                if (pci.IsMatch)
                {
                    currentActionList = log.TurnActions;
                    var card = m_rules.GetTurnCard(pci);
                    log.BoardCards.Add(card);
                    continue;
                }
            }

            if (log.SmallBlind == 0)
            {
                throw new HistoryParserException("Invalid small blind");
            }

            if (log.BigBlind == 0)
            {
                throw new HistoryParserException("Invalid big blind");
            }

            if (!log.Seats.Any(kv => kv.Value == log.DealerSeat))
            {
                throw new HistoryParserException("Missing dealer name");
            }

            if (log.Seats.Count() != log.StartBalances.Count())
            {
                throw new HistoryParserException("Mismatched seats and start balances");
            }

            if (log.BoardCards.Count > 5)
            {
                throw new HistoryParserException("Too many board cards");
            }

            var actors = log.PreflopActions.Concat(log.FlopActions).Concat(log.TurnActions).Concat(log.RiverActions).Select(a => a.Name).Distinct();
            if (!actors.All(log.Seats.ContainsKey) || !actors.All(log.StartBalances.ContainsKey))
            {
                throw new HistoryParserException("Action for unseated player");
            }

            Dictionary<string, int> spends = new Dictionary<string, int>();
            foreach (var kv in log.Seats)
            {
                spends[kv.Key] = 0;
            }

            foreach (var act in log.PreflopActions.Concat(log.FlopActions).Concat(log.TurnActions).Concat(log.RiverActions))
            {
                spends[act.Name] += act.Amount;
            }

            if (spends.Any(s => s.Value > log.StartBalances[s.Key]))
            {
                throw new HistoryParserException("Spent more than start balance");
            }

            var cardList = log.BoardCards.Concat(log.KnownHoleCards.SelectMany(c => c.Value)).ToList();
            if (cardList.Count != cardList.Distinct().Count())
            {
                throw new HistoryParserException("Duplicate cards");
            }

            return log;
        }
    }
}