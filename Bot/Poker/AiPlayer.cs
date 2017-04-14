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

        public double WeightAmplify { get; set; } = 1.0;

        public int SimulationCount { get; set; } = 200_000;

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
            var opponentHandWeightings = new List<IReadOnlyList<Tuple<HandClass, double>>>();
            foreach (var player in game.Players)
            {
                IReadOnlyList<Tuple<HandClass, double>> weights = null;
                if (m_data.ContainsKey(player))
                {
                    weights = m_data[player].Weights;
                }
            }

            int numOpponentsLeft = game.NumberOfPlayersInHand - opponentHandWeightings.Count - 1;
            int maxCallers = Math.Min(numOpponentsLeft + opponentHandWeightings.Count, 3);
            maxCallers -= opponentHandWeightings.Count;
            for (int i = 0; i < maxCallers; i++)
            {
                opponentHandWeightings.Add(m_staticData.EvenWeights);
            }

            double impliedPotSize = game.PotSize;
            if (impliedPotSize < 3 * game.BigBlind)
            {
                impliedPotSize = game.BigBlind * 3;
            }
            double potOdds = amountToCall / (impliedPotSize + amountToCall);

            double predictedWinRate = m_simulator.Simulate(Hole[0], Hole[1], game.Board, opponentHandWeightings, SimulationCount);
            List<Card[]> opponents = new List<Card[]>();
            foreach (var player in game.Players)
            {
                if (!game.HasFolded(player) && player != this)
                {
                    opponents.Add(new Card[] { player.Hole[0], player.Hole[1] });
                }
            }
            double actualWinRate = m_simulator.SimulateActual(Hole[0], Hole[1], opponents, game.Board.Select(c => c).ToList(), SimulationCount);
            s_log.Info("{0} has {1} {2} (Predicted win rate: {3}) (Actual win rate: {4})", Name, Hole[0], Hole[1], predictedWinRate, actualWinRate);

            //if (game.State != HandState.Preflop)
            //{
            //    string txt = string.Format("{0} has {1} {2} (Predicted win rate: {3}) (Actual win rate: {4})", Name, Hole[0], Hole[1], predictedWinRate, actualWinRate);
            //    System.IO.File.AppendAllText("guesses.txt", txt + Environment.NewLine);
            //}

            if (predictedWinRate < potOdds)
            {
                return new GameAction(amountToCall == 0 ? GameActionType.Check : GameActionType.Fold);
            }

            double maxValue = GetMaxValue(game.BigBlind, predictedWinRate);
            int raiseAmount = 0;
            if (game.PotSize < maxValue && contribution <= game.BigBlind)
            {
                if (game.State == HandState.Preflop)
                {
                    raiseAmount = (2 * game.BigBlind) + (game.BigBlind * game.GetBettersBefore(this));
                }
                else
                {
                    raiseAmount = (int)(game.PotSize * 0.75);
                }

                if (raiseAmount < minRaise)
                {
                    raiseAmount = minRaise;
                }
            }

            if (game.PotSize + amountToCall + raiseAmount > maxValue)
            {
                raiseAmount = 0;
            }

            if (raiseAmount < minRaise)
            {
                raiseAmount = 0;
            }

            int amount = amountToCall + raiseAmount;

            amount = Math.Min(amount, Balance);
            if (Balance - amount < game.BigBlind * 10)
            {
                amount = Balance;
            }

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

            double[] vector = game.Log.MakeVector(player.ToString(), game.State);
            IReadOnlyList<double> average = m_staticData.AveragePredictionVectors[game.State];
            data.Vector = vector;
            for (int i = 0; i < vector.Length; i++)
            {
                vector[i] -= average[i];
            }
            data.Weights = m_predictor.Estimate(game.State, vector);
            //double amp = data.Weights.First().Item2 / (100.0 / 169);
            //amp = Math.Max(amp, 1);
            //List<Tuple<HandClass, double>> amplifiedWeights = new List<Tuple<HandClass, double>>(169);
            //foreach (var w in data.Weights)
            //{
            //    amplifiedWeights.Add(Tuple.Create(w.Item1, w.Item2 * amp));
            //}
            //data.Weights = amplifiedWeights;

            PrintPredictions(player, data);
        }

        private int GetMaxValue(int bigBlind, double winRate)
        {
            winRate *= 100;
            int ceil = (int)Math.Ceiling(winRate);
            int floor = (int)Math.Floor(winRate);
            var ramp = m_staticData.BetRamp;

            double min = ramp[floor];
            double max = ramp[ceil];
            double delta = max - min;
            double interp = winRate - floor;

            return (int)(bigBlind * (min + (delta * interp)));
        }

        private void PrintPredictions(IPlayer player, PlayerData data)
        {
            if (ShowPredictions)
            {
                for (int i = 0; i < 5; i++)
                {
                    s_log.Debug("{0} might have {1} ({2})", player, data.Weights[i].Item1, data.Weights[i].Item2);
                }
                HandClass c = HandClass.FromCards(player.Hole[0], player.Hole[1]);
                var actual = data.Weights.First(t => t.Item1.A == c.A && t.Item1.B == c.B && t.Item1.Suited == c.Suited);
                s_log.Debug("---");
                double randomChance = 1.0 / 169;
                double diff = actual.Item2 / randomChance;
                s_log.Debug("{0} has {1} ({2}) ({3} times more likely than random)", player, actual.Item1, actual.Item2, diff);
                s_log.Debug("---");
                for (int i = 0; i < 5; i++)
                {
                    s_log.Debug("{0} might not have {1} ({2})", player, data.Weights[data.Weights.Count - i - 1].Item1, data.Weights[data.Weights.Count - i - 1].Item2);
                }
            }
        }
    }
}