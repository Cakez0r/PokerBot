using NLog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Poker
{
    public class Game
    {
        private static ILogger s_log = LogManager.GetCurrentClassLogger();

        public HandState State { get; private set; }

        public IReadOnlyList<IPlayer> Players { get; private set; }

        public int Dealer { get; private set; }

        public IList<Card> Board { get; private set; } = new List<Card>(5);

        public GameStateLog Log { get; private set; } = new GameStateLog();

        public int NumberOfPlayersInHand
        {
            get => Players.Count - m_folded.Count;
        }

        public int BigBlind { get; private set; }
        public int SmallBlind { get; private set; }

        private Pot ActivePot
        {
            get => m_pots.First();
        }

        public int PotSize
        {
            get => m_pots.Sum(p => p.Total);
        }

        private Deck m_deck = new Deck();

        private ISet<IPlayer> m_folded = new HashSet<IPlayer>();

        private ISet<IPlayer> m_allIn = new HashSet<IPlayer>();

        private IList<Pot> m_pots = new List<Pot>();

        private IHandEvaluator m_evaluator = null;

        private bool m_initialised;

        public Game(IHandEvaluator evaluator)
        {
            m_evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
        }

        public void Initialise(int smallBlind, int bigBlind, int dealer, IReadOnlyList<IPlayer> players)
        {
            Players = players;
            SmallBlind = smallBlind;
            BigBlind = bigBlind;
            Dealer = dealer;
            m_pots.Add(new Pot());
            m_initialised = true;
            Log.BigBlind = BigBlind;
            Log.SmallBlind = SmallBlind;
            Log.DealerSeat = Dealer;
            Log.GameId = Guid.NewGuid().ToString();
            Log.TableId = Guid.NewGuid().ToString();
            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                Log.StartBalances[player.ToString()] = player.Balance;
                Log.Seats[player.ToString()] = i;
            }
        }

        public int GetNumberOfPeopleToActAfter(IPlayer player)
        {
            int n = 0;
            int ofs = State == HandState.Preflop ? 3 : 1;
            while (GetIPlayerAfterButton(ofs + n) != player)
            {
                n++;
            }

            return Players.Count - n - 1;
        }

        public bool HasFolded(IPlayer player)
        {
            return m_folded.Contains(player);
        }

        public bool IsAllIn(IPlayer player)
        {
            return m_allIn.Contains(player);
        }

        public int GetPlayerAfterButton(int offset)
        {
            int i = Dealer + offset;

            while (i < 0)
            {
                i += Players.Count;
            }

            return i % Players.Count;
        }

        public IPlayer GetIPlayerAfterButton(int offset)
        {
            return Players[GetPlayerAfterButton(offset)];
        }

        private void TransferToPot(IPlayer player, int amount)
        {
            if (player.Balance < amount)
            {
                throw new GameRuleException(GameRuleExceptionType.PlayerBankrupt);
            }

            player.Balance -= amount;
            if (!ActivePot.Contributions.ContainsKey(player))
            {
                ActivePot.Contributions.Add(player, 0);
            }

            ActivePot.Contributions[player] += amount;

            if (!ActivePot.EligibleWinners.Contains(player))
            {
                ActivePot.EligibleWinners.Add(player);
            }
        }

        private void ResolvePots()
        {
            foreach (var pot in m_pots)
            {
                var winners = pot.EligibleWinners.GroupBy(p => m_evaluator.EvaluateScore(Board.Concat(p.Hole))).OrderByDescending(g => g.Key).First();
                s_log.Info("----------");
                foreach (var winner in winners)
                {
                    var eval = m_evaluator.Evaluate(Board.Concat(winner.Hole));
                    s_log.Info("{0} wins {1} with {2} {3} ({4})", winner, pot.Total / winners.Count(), winner.Hole[0], winner.Hole[1], eval.Type);
                    winner.Balance += pot.Total / winners.Count();
                }
                s_log.Info("----------");
            }
        }

        private void DealHoleCards()
        {
            for (int i = 0; i < 2; i++)
            {
                for (int p = 0; p < Players.Count; p++)
                {
                    IPlayer player = Players[GetPlayerAfterButton(p + 1)];

                    Card[] cards = new Card[2] { m_deck.Deal(), m_deck.Deal() };
                    player.Hole = cards;
                }
            }
        }

        public int GetContributions(IPlayer p)
        {
            int n = 0;

            foreach (var pot in m_pots)
            {
                int x = 0;
                if (pot.Contributions.TryGetValue(p, out x))
                {
                    n += x;
                }
            }

            return n;
        }

        public int GetBettersBefore(IPlayer player)
        {
            int betters = 0;
            IPlayer p = null;
            int n = 0;
            while ((p = GetIPlayerAfterButton(3 + n)) != player)
            {
                if (p == GetIPlayerAfterButton(1))
                {
                    if (GetContributions(p) > SmallBlind)
                    {
                        betters++;
                    }
                }
                else if (p == GetIPlayerAfterButton(2))
                {
                    if (GetContributions(p) > BigBlind)
                    {
                        betters++;
                    }
                }
                else
                {
                    if (GetContributions(p) > 0)
                    {
                        betters++;
                    }
                }

                n++;
            }

            return betters;
        }

        private void DoOrbit()
        {
            int actionCounter = 1;
            int lastToAct = 0;

            int highestBet = 0;
            int minRaise = BigBlind;

            Dictionary<IPlayer, int> contributions = new Dictionary<IPlayer, int>();

            foreach (var p in Players)
            {
                contributions[p] = 0;
            }

            if (State == HandState.Preflop)
            {
                highestBet = BigBlind;
                actionCounter = Players.Count == 2 ? 1 : 3;
                lastToAct = Players.Count == 2 ? 0 : 2;

                var sbp = Players.Count == 2 ? GetIPlayerAfterButton(0) : GetIPlayerAfterButton(1);
                s_log.Info("{0} posts small blind", sbp);
                if (sbp.Balance <= SmallBlind)
                {
                    m_allIn.Add(sbp);
                    TransferToPot(sbp, sbp.Balance);
                    contributions[sbp] = sbp.Balance;
                }
                else
                {
                    TransferToPot(sbp, SmallBlind);
                    contributions[sbp] = SmallBlind;
                }

                var bbp = Players.Count == 2 ? GetIPlayerAfterButton(1) : GetIPlayerAfterButton(2);
                s_log.Info("{0} posts big blind", bbp);
                if (bbp.Balance <= BigBlind)
                {
                    m_allIn.Add(bbp);
                    TransferToPot(bbp, bbp.Balance);
                    contributions[bbp] = bbp.Balance;
                }
                else
                {
                    TransferToPot(bbp, BigBlind);
                    contributions[bbp] = BigBlind;
                }
            }

            lastToAct = GetPlayerAfterButton(lastToAct);

            while (true)
            {
                if (IsNoMoreAction())
                {
                    break;
                }

                int playerIndex = GetPlayerAfterButton(actionCounter);

                var playerToAct = Players[playerIndex];
                if (!(m_allIn.Contains(playerToAct) || m_folded.Contains(playerToAct)))
                {
                    int contribution = contributions[playerToAct];
                    bool isRaise = false;
                    int amountToCall = highestBet - contribution;

                    s_log.Info("{0} to act (balance {1})", playerToAct, playerToAct.Balance);
                    var act = playerToAct.Act(this, contribution, amountToCall, minRaise);

                    if (act.Type == GameActionType.Check)
                    {
                        s_log.Info("{0} checks", playerToAct);
                        if (amountToCall > 0)
                        {
                            throw new GameRuleException(GameRuleExceptionType.IllegalCheck);
                        }
                    }
                    else if (act.Type == GameActionType.Fold)
                    {
                        s_log.Info("{0} folds", playerToAct);
                        m_folded.Add(playerToAct);
                        foreach (var pot in m_pots)
                        {
                            pot.EligibleWinners.Remove(playerToAct);
                        }
                    }
                    else if (act.Type == GameActionType.Bet)
                    {
                        if (act.Amount < amountToCall && act.Amount < playerToAct.Balance)
                        {
                            throw new GameRuleException(GameRuleExceptionType.BetLessThanAmountToCall);
                        }

                        TransferToPot(playerToAct, act.Amount);
                        contributions[playerToAct] += act.Amount;

                        int raiseAmount = act.Amount - amountToCall;

                        if (raiseAmount > 0)
                        {
                            isRaise = true;
                            if (playerToAct.Balance == 0)
                            {
                                s_log.Info("{0} raises all for {1} more (total {2})", playerToAct, raiseAmount, contributions[playerToAct]);
                                m_allIn.Add(playerToAct);
                            }
                            else
                            {
                                if (raiseAmount < minRaise)
                                {
                                    throw new GameRuleException(GameRuleExceptionType.RaiseLessThanMinRaise);
                                }

                                s_log.Info("{0} raises by {1} more (total {2})", playerToAct, raiseAmount, contributions[playerToAct]);
                            }

                            lastToAct = GetPlayerAfterButton(actionCounter - 1);
                            highestBet = contributions[playerToAct];

                            if (raiseAmount > minRaise)
                            {
                                minRaise = raiseAmount;
                            }
                        }
                        else
                        {
                            if (playerToAct.Balance == 0)
                            {
                                s_log.Info("{0} calls all in for {1} more (total {2})", playerToAct, act.Amount, contributions[playerToAct]);
                                m_allIn.Add(playerToAct);
                            }
                            else
                            {
                                s_log.Info("{0} calls {1} more (total {2})", playerToAct, act.Amount, contributions[playerToAct]);
                            }
                        }
                    }

                    IList<NamedGameAction> actionList = null;
                    switch (State)
                    {
                        case HandState.Preflop:
                            actionList = Log.PreflopActions;
                            break;

                        case HandState.Flop:
                            actionList = Log.FlopActions;
                            break;

                        case HandState.Turn:
                            actionList = Log.TurnActions;
                            break;

                        case HandState.River:
                            actionList = Log.RiverActions;
                            break;
                    }

                    actionList.Add(new NamedGameAction(playerToAct.ToString(), act.Type, act.Amount) { IsRaise = isRaise });

                    foreach (var p in Players)
                    {
                        if (p == playerToAct)
                        {
                            continue;
                        }

                        p.OnPlayerActed(this, playerToAct, act, amountToCall);
                    }
                }

                if (playerIndex == lastToAct)
                {
                    break;
                }

                actionCounter++;
            }

            MakeSidepots();
        }

        private void MakeSidepots()
        {
            var groupedContributions = ActivePot.Contributions.GroupBy(kv => kv.Value, kv => kv.Key).OrderBy(g => g.Key).Select(g => (g.Key, g.ToList())).ToList();

            int highestContrib = groupedContributions.Last().Item1;
            for (int i = 0; i < groupedContributions.Count - 1; i++)
            {
                var contrib = groupedContributions[i];

                var playersInHand = contrib.Item2.Where(p => !m_folded.Contains(p));
                if (playersInHand.Any())
                {
                    if (contrib.Item1 < highestContrib && contrib.Item1 > 0)
                    {
                        Pot sidepot = new Pot();
                        foreach (var p in ActivePot.EligibleWinners)
                        {
                            sidepot.EligibleWinners.Add(p);
                        }
                        foreach (var c in ActivePot.Contributions)
                        {
                            sidepot.Contributions[c.Key] = Math.Min(ActivePot.Contributions[c.Key], contrib.Item1);
                        }
                        foreach (var c in sidepot.Contributions)
                        {
                            ActivePot.Contributions[c.Key] -= c.Value;
                        }

                        for (int j = 0; j < groupedContributions.Count; j++)
                        {
                            groupedContributions[j] = (Math.Max(groupedContributions[j].Item1 - contrib.Item1, 0), groupedContributions[j].Item2);
                        }

                        foreach (var p in contrib.Item2)
                        {
                            ActivePot.EligibleWinners.Remove(p);
                            ActivePot.Contributions.Remove(p);
                        }

                        m_pots.Add(sidepot);

                        while (ActivePot.EligibleWinners.Count() == 1)
                        {
                            ActivePot.EligibleWinners.First().Balance += ActivePot.Total;
                            m_pots.RemoveAt(0);
                        }
                    }
                }
            }
        }

        private bool IsNoMoreAction()
        {
            return Players.Count - Players.Count(p => m_folded.Contains(p) || m_allIn.Contains(p)) <= 1;
        }

        public void Step()
        {
            if (!m_initialised)
            {
                throw new InvalidOperationException("Call Initialise first");
            }

            switch (State)
            {
                case HandState.Preflop:
                    s_log.Info("Preflop");
                    DealHoleCards();
                    DoOrbit();
                    if (Players.Count - m_folded.Count == 1)
                    {
                        ResolvePots();
                        State = HandState.Finished;
                    }
                    else
                    {
                        State = HandState.Flop;
                    }
                    break;

                case HandState.Flop:
                    s_log.Info("----------");
                    s_log.Info("Flop");
                    Board.Add(m_deck.Deal());
                    Board.Add(m_deck.Deal());
                    Board.Add(m_deck.Deal());
                    for (int i = 0; i < 3; i++)
                    {
                        Log.BoardCards.Add(Board[i]);
                    }
                    s_log.Info("Board is {0} {1} {2}", Board[0], Board[1], Board[2]);
                    DoOrbit();
                    if (Players.Count - m_folded.Count == 1)
                    {
                        ResolvePots();
                        State = HandState.Finished;
                    }
                    else
                    {
                        State = HandState.Turn;
                    }
                    break;

                case HandState.Turn:
                    s_log.Info("----------");
                    s_log.Info("Turn");
                    Board.Add(m_deck.Deal());
                    Log.BoardCards.Add(Board[3]);
                    s_log.Info("Board is {0} {1} {2} {3}", Board[0], Board[1], Board[2], Board[3]);
                    DoOrbit();
                    if (Players.Count - m_folded.Count == 1)
                    {
                        ResolvePots();
                        State = HandState.Finished;
                    }
                    else
                    {
                        State = HandState.River;
                    }
                    break;

                case HandState.River:
                    s_log.Info("----------");
                    s_log.Info("River");
                    Board.Add(m_deck.Deal());
                    Log.BoardCards.Add(Board[4]);
                    s_log.Info("Board is {0} {1} {2} {3} {4}", Board[0], Board[1], Board[2], Board[3], Board[4]);
                    DoOrbit();
                    ResolvePots();
                    State = HandState.Finished;
                    break;

                case HandState.Finished:
                    s_log.Info("Finished");
                    break;
            }
        }
    }
}