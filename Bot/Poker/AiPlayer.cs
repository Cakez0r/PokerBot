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
            public double[] Vector { get; set; }
            public IReadOnlyList<Tuple<HandClass, double>> Weights { get; set; }
        }

        private static Logger s_log = LogManager.GetCurrentClassLogger();

        public IReadOnlyList<Card> Hole { get; set; } = new Card[2];

        public int Balance { get; set; }

        public string Name { get; set; }

        public bool ShowPredictions { get; set; }

        public double RaiseThreshold { get; set; } = 0.5;

        public int SimulationCount { get; set; } = 100_000;

        private IHandPredictor m_predictor;

        private IStaticData m_staticData;

        private ISimulator m_simulator;

        private Dictionary<IPlayer, PlayerData> m_preflopData = new Dictionary<IPlayer, PlayerData>();
        private Dictionary<IPlayer, PlayerData> m_flopData = new Dictionary<IPlayer, PlayerData>();

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
                m_preflopData.Clear();
                m_currentHand = game;
            }

            var ct = HandClass.FromCards(Hole[0], Hole[1]);
            var opponentHandWeightings = new List<IReadOnlyList<Tuple<HandClass, double>>>();
            foreach (var player in game.Players)
            {
                IReadOnlyList<Tuple<HandClass, double>> weights = null;
                if (m_flopData.ContainsKey(player))
                {
                    weights = m_flopData[player].Weights;
                }
                else if (m_preflopData.ContainsKey(player))
                {
                    weights = m_preflopData[player].Weights;
                }
            }
            for (int i = 0; i < game.NumberOfPlayersInHand - opponentHandWeightings.Count; i++)
            {
                opponentHandWeightings.Add(m_staticData.EvenWeights);
            }

            double potOdds = amountToCall / ((double)game.PotSize + amountToCall);

            double winRate = m_simulator.Simulate(Hole[0], Hole[1], game.Board, opponentHandWeightings, SimulationCount);
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
            if (winRate > RaiseThreshold && game.PotSize < maxValue)
            {
                if (game.State == HandState.Preflop)
                {
                    handWorth = (3 * game.BigBlind) + (game.BigBlind * game.GetBettersBefore(this));
                }
                else
                {
                    handWorth = game.PotSize + (int)(game.PotSize * 0.75);
                    if (handWorth - contribution < minRaise)
                    {
                        handWorth = minRaise;
                    }
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
                m_preflopData.Clear();
                m_flopData.Clear();
                m_currentHand = game;
            }

            if (player == this)
            {
                return;
            }

            if (game.State == HandState.Preflop)
            {
                if (game.HasFolded(player))
                {
                    if (m_preflopData.ContainsKey(player))
                    {
                        m_preflopData.Remove(player);
                    }

                    return;
                }

                if (!m_preflopData.ContainsKey(player))
                {
                    m_preflopData[player] = new PlayerData();
                }

                var data = m_preflopData[player];

                double[] vector = game.Log.MakeVector(player.ToString(), HandState.Preflop);

                data.Vector = vector;
                for (int i = 0; i < vector.Length; i++)
                {
                    vector[i] -= m_staticData.AveragePreflopPredictionVector[i];
                }
                data.Weights = m_predictor.Estimate(HandState.Preflop, Array.AsReadOnly(vector));

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

            if (game.State == HandState.Flop)
            {
                if (game.HasFolded(player))
                {
                    if (m_preflopData.ContainsKey(player))
                    {
                        m_preflopData.Remove(player);
                    }

                    if (m_flopData.ContainsKey(player))
                    {
                        m_flopData.Remove(player);
                    }

                    return;
                }

                if (!m_flopData.ContainsKey(player))
                {
                    m_flopData[player] = new PlayerData();
                }

                var data = m_flopData[player];

                double[] vector = game.Log.MakeVector(player.ToString(), HandState.Flop);

                data.Vector = vector;
                for (int i = 0; i < vector.Length; i++)
                {
                    vector[i] -= m_staticData.AverageFlopPredictionVector[i];
                }
                data.Weights = m_predictor.Estimate(HandState.Flop, Array.AsReadOnly(vector));

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