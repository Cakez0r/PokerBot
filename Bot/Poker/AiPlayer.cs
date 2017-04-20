using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        public int SimulationCount { get; set; } = 500;

        private IHandPredictor m_predictor;

        private IStaticData m_staticData;

        private ISimulator m_simulator;

        private Dictionary<IPlayer, PlayerData> m_data = new Dictionary<IPlayer, PlayerData>();

        private HashSet<HandClass> m_preflopRaisers = new HashSet<HandClass>()
        {
            HandClass.FromCards(new Card(Suit.Clubs, Face.Ace), new Card(Suit.Diamonds, Face.Ace)),
            HandClass.FromCards(new Card(Suit.Clubs, Face.King), new Card(Suit.Diamonds, Face.King)),
            HandClass.FromCards(new Card(Suit.Clubs, Face.Queen), new Card(Suit.Diamonds, Face.Queen)),
            HandClass.FromCards(new Card(Suit.Clubs, Face.Jack), new Card(Suit.Diamonds, Face.Jack)),
            HandClass.FromCards(new Card(Suit.Clubs, Face.Ten), new Card(Suit.Diamonds, Face.Ten)),
            HandClass.FromCards(new Card(Suit.Clubs, Face.Nine), new Card(Suit.Diamonds, Face.Nine)),
            HandClass.FromCards(new Card(Suit.Clubs, Face.Eight), new Card(Suit.Diamonds, Face.Eight)),

            HandClass.FromCards(new Card(Suit.Clubs, Face.Ace), new Card(Suit.Clubs, Face.King)),
            HandClass.FromCards(new Card(Suit.Clubs, Face.Ace), new Card(Suit.Clubs, Face.Queen)),
            HandClass.FromCards(new Card(Suit.Clubs, Face.Ace), new Card(Suit.Clubs, Face.Jack)),
            HandClass.FromCards(new Card(Suit.Clubs, Face.Ace), new Card(Suit.Clubs, Face.Ten)),
            HandClass.FromCards(new Card(Suit.Clubs, Face.King), new Card(Suit.Clubs, Face.Queen)),
            HandClass.FromCards(new Card(Suit.Clubs, Face.King), new Card(Suit.Clubs, Face.Jack)),
            HandClass.FromCards(new Card(Suit.Clubs, Face.Queen), new Card(Suit.Clubs, Face.Jack)),

            HandClass.FromCards(new Card(Suit.Clubs, Face.Ace), new Card(Suit.Diamonds, Face.King)),
            HandClass.FromCards(new Card(Suit.Clubs, Face.Ace), new Card(Suit.Diamonds, Face.Queen)),
        };

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
                if (m_data.ContainsKey(player))
                {
                    opponentHandWeightings.Add(m_data[player].Weights);
                }
            }

            int numOpponentsLeft = game.NumberOfPlayersInHand - opponentHandWeightings.Count - 1;
            int maxCallers = Math.Min(numOpponentsLeft + opponentHandWeightings.Count, 4);
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

            string log = string.Format("{0} has {1} {2} (Predicted win rate: {3}%) (Actual win rate: {4}%)", Name, Hole[0], Hole[1], Utility.DoubleToPct(predictedWinRate), Utility.DoubleToPct(actualWinRate));
            if (Math.Abs(predictedWinRate - actualWinRate) < 0.2)
            {
                s_log.Info(log);
            }
            else
            {
                s_log.Warn(log);
            }

            if (predictedWinRate < potOdds)
            {
                return new GameAction(amountToCall == 0 ? GameActionType.Check : GameActionType.Fold);
            }

            double maxValue = GetMaxValue(game.BigBlind, predictedWinRate);
            int raiseAmount = 0;
            if (predictedWinRate > (1.0 / game.NumberOfPlayersInHand) && contribution <= game.BigBlind)
            {
                if (GlobalRandom.Next() > .5)
                {
                    if (game.State == HandState.Preflop)
                    {
                        if ((m_preflopRaisers.Contains(ct) || GlobalRandom.Next() > .5) && amountToCall < game.BigBlind * 2)
                        {
                            raiseAmount = (2 * game.BigBlind) + (game.BigBlind * game.GetBettersBefore(this));
                        }
                    }
                    else
                    {
                        raiseAmount = (int)(game.PotSize * 0.75);
                    }
                }
            }

            if (raiseAmount > 0 && raiseAmount < minRaise)
            {
                raiseAmount = minRaise;
            }

            //if (game.PotSize + amountToCall + raiseAmount > maxValue)
            //{
            //    raiseAmount = 0;
            //}

            //if (raiseAmount < minRaise)
            //{
            //    raiseAmount = 0;
            //}

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
            data.Weights = m_predictor.Predict(game, player);
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
                StringBuilder range = new StringBuilder();
                var poss = data.Weights.Where(w => w.Item2 > 1e-3);
                range.Append("Range ");
                range.Append(poss.Count());
                range.Append(" [");

                HandClass c = HandClass.FromCards(player.Hole[0], player.Hole[1]);
                for (int i = 0; i < data.Weights.Count; i++)
                {
                    var w = data.Weights[i];
                    if (w.Item2 < 1e-3)
                    {
                        break;
                    }

                    if (w.Item1 == c)
                    {
                        range.Append("*");
                    }
                    range.Append(w.Item1.ToString());
                    range.Append(" ");
                    range.Append(Utility.DoubleToPct(w.Item2));
                    range.Append("%");

                    if (w.Item1 == c)
                    {
                        range.Append("*");
                    }

                    range.Append(", ");
                }
                range.Remove(range.Length - 2, 2);
                range.Append("]");
                s_log.Debug(range.ToString());
            }
        }
    }
}