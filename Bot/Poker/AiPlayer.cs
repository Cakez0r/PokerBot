using NLog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Poker
{
    public class AiPlayer : IPlayer
    {
        private class PlayerData
        {
            public int NumChecks { get; set; }
            public int NumCalls { get; set; }
            public int NumRaises { get; set; }
            public int Contributions { get; set; }
            public IReadOnlyList<Tuple<HandClass, double>> Weights { get; set; }
        }

        private static Logger s_log = LogManager.GetCurrentClassLogger();

        public IReadOnlyList<Card> Hole { get; set; } = new Card[2];

        public int Balance { get; set; }

        public string Name { get; set; }

        public bool ShowPredictions { get; set; }

        private IHandPredictor m_predictor;

        private IStaticData m_staticData;

        private ISimulator m_simulator;

        private Dictionary<IPlayer, PlayerData> m_data = new Dictionary<IPlayer, PlayerData>();

        private Game m_currentHand = null;

        public AiPlayer(ISimulator simulator, IHandPredictor predictor, IStaticData data)
        {
            m_predictor = predictor ?? throw new ArgumentNullException(nameof(predictor));
            m_staticData = data ?? throw new ArgumentNullException(nameof(data));
            m_simulator = simulator ?? throw new ArgumentNullException(nameof(simulator));
        }

        public GameAction Act(Game game, int contribution, int amountToCall, int minRaise)
        {
            if (game != m_currentHand)
            {
                m_data.Clear();
                m_currentHand = game;
            }

            var ct = HandClass.FromCards(Hole[0], Hole[1]);
            var opponentHandWeightings = new List<IReadOnlyList<Tuple<HandClass, double>>>(m_data.Select(kv => kv.Value.Weights));
            for (int i = 0; i < game.NumberOfPlayersInHand - opponentHandWeightings.Count; i++)
            {
                opponentHandWeightings.Add(m_staticData.EvenWeights);
            }

            double potOdds = amountToCall / ((double)game.PotSize + amountToCall);

            double winRate = m_simulator.Simulate(Hole[0], Hole[1], game.Board, opponentHandWeightings, 100000);
            s_log.Info("{0} has {1} {2} (Win rate: {3})", Name, Hole[0], Hole[1], winRate);

            if (winRate < potOdds)
            {
                return new GameAction(amountToCall == 0 ? GameActionType.Check : GameActionType.Fold);
            }

            double maxValue = Balance * winRate;
            if (Balance - maxValue < game.BigBlind * 10)
            {
                maxValue = Balance;
            }

            int handWorth = 0;
            if (winRate > 0.5 && game.PotSize < maxValue)
            {
                if (game.State == HandState.Preflop)
                {
                    handWorth = (3 * game.BigBlind) + (game.BigBlind * game.GetBettersBefore(this));
                }
                else
                {
                    handWorth = game.PotSize + (int)(game.PotSize * 0.75);
                }
            }

            int amount = handWorth - contribution;

            amount = Math.Max(amountToCall, amount);
            if (amount > amountToCall)
            {
                if (amount - amountToCall < minRaise)
                {
                    amount = amountToCall;
                }
            }

            amount = Math.Min(amount, Balance);

            return new GameAction(amount == 0 ? GameActionType.Check : GameActionType.Bet, amount);
        }

        public override string ToString()
        {
            return Name;
        }

        public void OnPlayerActed(Game game, IPlayer player, GameAction action, int amountToCall)
        {
            if (m_currentHand != game)
            {
                m_data.Clear();
                m_currentHand = game;
            }

            if (game.State == HandState.Preflop)
            {
                if (player == this)
                {
                    return;
                }

                if (game.HasFolded(player))
                {
                    if (m_data.ContainsKey(player))
                    {
                        m_data.Remove(player);
                    }

                    return;
                }

                if (!m_data.ContainsKey(player))
                {
                    m_data[player] = new PlayerData();
                }

                var data = m_data[player];

                data.Contributions += action.Amount;
                if (action.Type == GameActionType.Check)
                {
                    data.NumChecks++;
                }
                else
                {
                    if (action.Amount > amountToCall)
                    {
                        data.NumRaises++;
                    }
                    else
                    {
                        data.NumCalls++;
                    }
                }

                double con = data.Contributions;
                if (game.GetIPlayerAfterButton(1) == player)
                {
                    con -= game.SmallBlind;
                }
                else if (game.GetIPlayerAfterButton(2) == player)
                {
                    con -= game.BigBlind;
                }

                //Normalise all for table size?
                double[] vector = new double[]
                {
                    (double)game.GetNumberOfPeopleToActAfter(player) / game.Players.Count,
                    con / game.BigBlind,
                    con / (player.Balance + con),
                    con / (game.PotSize - con),
                    con / (double)game.Players.Average(p => p.Balance),
                    game.GetBettersBefore(player),
                    game.GetNumberOfPeopleToActAfter(player),
                    (double)player.Balance / game.Players.Average(p => p.Balance),
                    data.NumCalls + data.NumChecks + data.NumRaises,
                    data.NumChecks,
                    data.NumCalls,
                    data.NumRaises,
                    (double)(game.PotSize - data.Contributions) / game.BigBlind,
                    game.GetIPlayerAfterButton(2) == player ? 1.0 : 0.0,
                    game.GetIPlayerAfterButton(1) == player ? 1.0 : 0.0
                };

                for (int i = 0; i < vector.Length; i++)
                {
                    vector[i] -= m_staticData.AveragePredictionVector[i];
                }

                data.Weights = m_predictor.Estimate(Array.AsReadOnly(vector));

                if (ShowPredictions)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        s_log.Debug("{0} might have {1} ({2})", player, data.Weights[i].Item1, data.Weights[i].Item2);
                    }
                    HandClass c = HandClass.FromCards(player.Hole[0], player.Hole[1]);
                    var actual = data.Weights.First(t => t.Item1.A == c.A && t.Item1.B == c.B && t.Item1.Suited == c.Suited);
                    s_log.Debug("{0} might have {1} ({2})", player, actual.Item1, actual.Item2);
                }
            }
        }
    }
}