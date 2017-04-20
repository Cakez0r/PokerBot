using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Poker
{
    internal class Program
    {
        private static ILogger s_log = LogManager.GetCurrentClassLogger();

        private static int wins = 0;
        private static int tournaments = 0;

        private static bool interactive = true;

        private static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.Unicode;
            Console.BackgroundColor = ConsoleColor.DarkGreen;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Clear();

            string dataPath = "../datafiles";
            IHandEvaluator evaluator = new HandEvaluator();
            evaluator = new LutEvaluator(evaluator, dataPath);
            //evaluator = new MockEvaluator();
            IStaticData staticData = new StaticData(dataPath);
            ISimulator simulator = new MonteCarloSimulator(evaluator, staticData) { UseRandomOffset = false };
            IHandPredictor cheatingPredictor = new CheatingHandPredictor(staticData);
            IHandPredictor neuralNetPredictor = new NeuralNetHandPredictor(staticData);

            //Console.WriteLine(simulator.Simulate(new Card(Suit.Clubs, Face.Ace), new Card(Suit.Diamonds, Face.Ace), new Card[0], new List<IReadOnlyList<Tuple<HandClass, double>>>() { staticData.EvenWeights }, 10));
            //Console.WriteLine(simulator.Simulate(new Card(Suit.Clubs, Face.Ace), new Card(Suit.Diamonds, Face.Ace), new Card[0], new List<IReadOnlyList<Tuple<HandClass, double>>>() { staticData.EvenWeights }, 100));
            //Console.WriteLine(simulator.Simulate(new Card(Suit.Clubs, Face.Ace), new Card(Suit.Diamonds, Face.Ace), new Card[0], new List<IReadOnlyList<Tuple<HandClass, double>>>() { staticData.EvenWeights }, 1000));
            //Console.WriteLine(simulator.Simulate(new Card(Suit.Clubs, Face.Ace), new Card(Suit.Diamonds, Face.Ace), new Card[0], new List<IReadOnlyList<Tuple<HandClass, double>>>() { staticData.EvenWeights }, 10000));
            //Console.WriteLine(simulator.Simulate(new Card(Suit.Clubs, Face.Ace), new Card(Suit.Diamonds, Face.Ace), new Card[0], new List<IReadOnlyList<Tuple<HandClass, double>>>() { staticData.EvenWeights }, 100000));
            //Console.WriteLine(simulator.Simulate(new Card(Suit.Clubs, Face.Ace), new Card(Suit.Diamonds, Face.Ace), new Card[0], new List<IReadOnlyList<Tuple<HandClass, double>>>() { staticData.EvenWeights }, 1000000));
            //Dictionary<HandClass, double> winRates = new Dictionary<HandClass, double>();
            //var board = new Card[0];
            //foreach (var h in staticData.AllPossibleHands)
            //{
            //    var hand = h.Expand();
            //    winRates[h] = simulator.Simulate(hand[0][0], hand[0][1], board, new List<IReadOnlyList<Tuple<HandClass, double>>>() { staticData.EvenWeights, staticData.EvenWeights, staticData.EvenWeights, staticData.EvenWeights, staticData.EvenWeights }, 100_000);
            //    Console.WriteLine(h);
            //};

            //foreach (var wr in winRates.OrderByDescending(kv => kv.Value))
            //{
            //    Console.WriteLine("{0}: {1}", wr.Key, wr.Value);
            //}

            if (!interactive)
            {
                LogManager.Configuration.LoggingRules.Clear();
                LogManager.Configuration.Reload();
            }

            Dictionary<HandClass, int> ordering = new Dictionary<HandClass, int>();
            int n = 0;
            foreach (var o in staticData.AllPossibleHands)
            {
                ordering[o] = n;
                n++;
            }

            const double amp = 1;
            List<BlendedPredictor> predictors = new List<BlendedPredictor>(9);
            for (int i = 0; i < 9; i++)
            {
                predictors.Add(new BlendedPredictor(neuralNetPredictor, cheatingPredictor));
            }
            int hand = 0;
            while (true)
            {
                int amount = 200;
                var alice = new AiPlayer(simulator, predictors[0], staticData) { Name = "Alice", WeightAmplify = amp };
                List<IPlayer> players = new List<IPlayer>()
                {
                    //new ConsoleInteractivePlayer(amount) { Name = "Lewis" },
                    //new ConsoleInteractivePlayer(amount) { Name = "Bob" },
                    //new ConsoleInteractivePlayer(amount) { Name = "Charlie" },
                    //new ConsoleInteractivePlayer(amount) { Name = "Dave" },
                    //new ConsoleInteractivePlayer(amount) { Name = "Edward" },
                    //new ConsoleInteractivePlayer(amount) { Name = "Fred" },
                    //new ConsoleInteractivePlayer(amount) { Name = "Gina" },
                    //new ConsoleInteractivePlayer(amount) { Name = "Harry" },
                    //new ConsoleInteractivePlayer(amount) { Name = "Ian" },

                    alice,
                    new AiPlayer(simulator, predictors[1], staticData) { Name = "Bob" },
                    new AiPlayer(simulator, predictors[2], staticData) { Name = "Charlie" },
                    new AiPlayer(simulator, predictors[3], staticData) { Name = "Dave" },
                    new AiPlayer(simulator, predictors[4], staticData) { Name = "Edward" },
                    new AiPlayer(simulator, predictors[5], staticData) { Name = "Fred" },
                    new AiPlayer(simulator, predictors[6], staticData) { Name = "Gina" },
                    new AiPlayer(simulator, predictors[7], staticData) { Name = "Harry" },
                    new AiPlayer(simulator, predictors[8], staticData) { Name = "Ian" },

                    //new AlwaysCallPlayer(amount) { Name = "Bob" },
                    //new AlwaysCallPlayer(amount) { Name = "Charlie" },
                    //new AlwaysCallPlayer(amount) { Name = "Dave" },
                    //new AlwaysCallPlayer(amount) { Name = "Edward" },
                    //new AlwaysCallPlayer(amount) { Name = "Fred" },
                };

                for (int i = 0; i < players.Count; i++)
                {
                    players[i].Balance = amount;
                    predictors[i].BlendFactor = interactive ? 0 : GlobalRandom.NextDouble() * .5;
                }
                alice.ShowPredictions = true;

                //PrintStatus(players);

                int d = GlobalRandom.Next(int.MaxValue);
                bool go = true;
                while (go)
                {
                    d = d % players.Count;

                    Game g = new Game(evaluator);
                    g.Initialise(1, 2, d, players.AsReadOnly());

                    while (g.State != HandState.Finished)
                    {
                        g.Step();
                    }

                    hand++;

                    for (int i = players.Count - 1; i >= 0; i--)
                    {
                        if (players[i].Balance == 0)
                        {
                            if (players[i].ToString() == "Alice")
                            {
                                go = false;
                                tournaments++;
                            }
                            s_log.Info("{0} is bankrupt", players[i]);
                            players.RemoveAt(i);
                            //PrintStatus(players);
                        }
                    }

                    if (!interactive)
                    {
                        foreach (var h in g.Log.KnownHoleCards)
                        {
                            HandClass hc = HandClass.FromCards(h.Value[0], h.Value[1]);
                            string label = MakeLabel(ordering[hc]) + Environment.NewLine;

                            if (g.Log.PreflopActions.Any(a => a.Type != GameActionType.Fold && a.Name == h.Key))
                            {
                                var vec = string.Join(" ", g.Log.MakeVector(h.Key, HandState.Preflop)) + Environment.NewLine;
                                File.AppendAllText("preflop_labels", label);
                                File.AppendAllText("preflop_data", vec);
                            }

                            if (g.Log.FlopActions.Any(a => a.Type != GameActionType.Fold && a.Name == h.Key))
                            {
                                var vec = string.Join(" ", g.Log.MakeVector(h.Key, HandState.Flop)) + Environment.NewLine;
                                File.AppendAllText("flop_labels", label);
                                File.AppendAllText("flop_data", vec);
                            }

                            if (g.Log.TurnActions.Any(a => a.Type != GameActionType.Fold && a.Name == h.Key))
                            {
                                var vec = string.Join(" ", g.Log.MakeVector(h.Key, HandState.Turn)) + Environment.NewLine;
                                File.AppendAllText("turn_labels", label);
                                File.AppendAllText("turn_data", vec);
                            }

                            if (g.Log.RiverActions.Any(a => a.Type != GameActionType.Fold && a.Name == h.Key))
                            {
                                var vec = string.Join(" ", g.Log.MakeVector(h.Key, HandState.River)) + Environment.NewLine;
                                File.AppendAllText("river_labels", label);
                                File.AppendAllText("river_data", vec);
                            }
                        }
                    }

                    d++;
                    //Console.Title = hand.ToString();
                    if (hand % 1 == 0)
                    {
                        //Console.Clear();
                        //Console.WriteLine(hand);
                        //PrintStatus(players);
                    }

                    if (players.Count < 6)
                    {
                        s_log.Info("Table is short handed");
                        tournaments++;
                        break;
                    }

                    if (interactive)
                    {
                        Console.WriteLine("Press any key for next hand...");
                        Console.ReadKey();
                        Console.Clear();
                    }
                }
            }
        }

        private static void PrintStatus(IEnumerable<IPlayer> players)
        {
            if (!interactive)
            {
                Console.Clear();
                Console.WriteLine("Wins: {0}  Games: {1}  Win rate: {2}", wins, tournaments, (double)wins / tournaments);
                foreach (var p in players.OrderByDescending(p => p.Balance))
                {
                    Console.WriteLine("{0}: {1}", p, p.Balance);
                }
            }
        }

        private static string MakeLabel(int i)
        {
            int[] labels = new int[169];
            labels[i] = 1;

            StringBuilder sb = new StringBuilder();

            for (int n = 0; n < labels.Length; n++)
            {
                sb.Append(labels[n]);
                sb.Append(' ');
            }

            return sb.ToString().Trim();
        }
    }
}