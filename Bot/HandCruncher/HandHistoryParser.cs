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
            GameStateLog log = null;
            IList<NamedGameAction> currentActionList = null;

            for (int i = 0; i < lines.Length; i++)
            {
                Match match = null;

                match = m_rules.GameEndExpr.Match(lines[i]);
                if (match.Success)
                {
                    if (log != null)
                    {
                        yield return log;
                    }
                    log = null;
                    continue;
                }

                try
                {
                    Func<Regex, Action<MatchInfo>, bool> ifMatch = (expr, a) =>
                    {
                        match = expr.Match(lines[i]);
                        if (match.Success)
                        {
                            a(new MatchInfo(lines, match, i));
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    };

                    if (ifMatch(m_rules.GameStartExpr, (m) => log = new GameStateLog()))
                    {
                        bool cardsShown = false;
                        int j = i;
                        for (j = i; !m_rules.GameEndExpr.IsMatch(lines[j]); j++)
                        {
                            if (m_rules.ShowsCardsExpr.IsMatch(lines[j]))
                            {
                                cardsShown = true;
                                break;
                            }
                        }

                        if (!cardsShown)
                        {
                            i = j + 1;
                            log = null;
                            continue;
                        }
                    }

                    if (log == null)
                    {
                        continue;
                    }

                    if (ifMatch(m_rules.FoldExpr, (m) => currentActionList.Add(new NamedGameAction(m_rules.GetPlayerWhoFolded(m), GameActionType.Fold))))
                    {
                        continue;
                    }
                    if (ifMatch(m_rules.CheckExpr, (m) => currentActionList.Add(new NamedGameAction(m_rules.GetPlayerWhoChecked(m), GameActionType.Check))))
                    {
                        continue;
                    }
                    if (ifMatch(m_rules.RaiseExpr, (m) =>
                    {
                        BetInfo bi = m_rules.GetRaise(m);
                        currentActionList.Add(new NamedGameAction(bi.Name, GameActionType.Bet, bi.Amount) { IsRaise = true });
                    }))
                    {
                        continue;
                    }

                    if (ifMatch(m_rules.BetExpr, (m) =>
                    {
                        BetInfo bi = m_rules.GetBet(m);
                        currentActionList.Add(new NamedGameAction(bi.Name, GameActionType.Bet, bi.Amount) { IsRaise = true });
                    }))
                    {
                        continue;
                    }

                    if (ifMatch(m_rules.CallExpr, (m) =>
                    {
                        BetInfo bi = m_rules.GetCall(m);
                        currentActionList.Add(new NamedGameAction(bi.Name, GameActionType.Bet, bi.Amount));
                    }))
                    {
                        continue;
                    }

                    bool seatInfoMatch = ifMatch(m_rules.SeatInfoExpr, (m) =>
                    {
                        SeatInfo si = m_rules.GetSeatInfo(m);
                        log.Seats[si.Name] = si.SeatNumber;
                        log.StartBalances[si.Name] = si.Balance;
                    });
                    if (lines[i].StartsWith("Seat") && lines[i].TrimEnd(' ').EndsWith("chips)") && !seatInfoMatch)
                    {
                        Console.WriteLine("Corrupt player id");
                        log = null;
                        continue;
                    }

                    if (seatInfoMatch)
                    {
                        continue;
                    }

                    if (lines[i].StartsWith("Table") && !ifMatch(m_rules.DealerSeatExpr, (m) => log.DealerSeat = m_rules.GetDealerSeat(m)))
                    {
                        Console.WriteLine("Corrupt table id");
                        log = null;
                        continue;
                    }

                    if (ifMatch(m_rules.GameIdExpr, (m) => log.GameId = m_rules.GetGameId(m)))
                    {
                        continue;
                    }

                    if (ifMatch(m_rules.SmallBlindPostExpr, (m) =>
                    {
                        BetInfo bi = m_rules.GetSmallBlindPost(m);
                        log.SmallBlind = bi.Amount;
                    }))
                    {
                        continue;
                    }

                    if (ifMatch(m_rules.BigBlindPostExpr, (m) =>
                    {
                        BetInfo bi = m_rules.GetBigBlindPost(m);
                        log.BigBlind = bi.Amount;
                    }))
                    {
                        continue;
                    }

                    if (ifMatch(m_rules.TableIdExpr, (m) => log.TableId = m_rules.GetTableId(m)))
                    {
                        continue;
                    }

                    if (ifMatch(m_rules.PreflopStartExpr, (m) => currentActionList = log.PreflopActions))
                    {
                        continue;
                    }

                    if (ifMatch(m_rules.FlopStartExpr, (m) =>
                    {
                        currentActionList = log.FlopActions;
                        Card[] cards = m_rules.GetFlopCards(m);
                        foreach (var card in cards)
                        {
                            log.BoardCards.Add(card);
                        }
                    }))
                    {
                        continue;
                    }

                    if (ifMatch(m_rules.TurnStartExpr, (m) =>
                    {
                        currentActionList = log.TurnActions;
                        Card card = m_rules.GetTurnCard(m);
                        log.BoardCards.Add(card);
                    }))
                    {
                        continue;
                    }

                    if (ifMatch(m_rules.RiverStartExpr, (m) =>
                    {
                        currentActionList = log.RiverActions;
                        Card card = m_rules.GetRiverCard(m);
                        log.BoardCards.Add(card);
                    }))
                    {
                        continue;
                    }

                    ifMatch(m_rules.ShowsCardsExpr, (m) =>
                    {
                        var cards = m_rules.GetShownCards(m);
                        log.KnownHoleCards[cards.Item1] = cards.Item2;
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Parse failure " + ex.ToString());
                    log = null;
                    continue;
                }
            }
        }
    }
}