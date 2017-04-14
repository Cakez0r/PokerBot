using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
            ISimulator simulator = new Simulator(evaluator, staticData);
            IHandPredictor predictor = new NeuralNetHandPredictor(staticData);

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

            const double amp = 1;
            while (true)
            {
                int amount = 200;
                var alice = new AiPlayer(simulator, predictor, staticData) { Name = "Alice", WeightAmplify = amp };
                List<IPlayer> players = new List<IPlayer>()
                {
                    //new ConsoleInteractivePlayer(amount) { Name = "Lewis" },
                    //new ConsoleInteractivePlayer(amount) { Name = "Bob" },
                    //new ConsoleInteractivePlayer(amount) { Name = "Dave" },
                    //new ConsoleInteractivePlayer(amount) { Name = "Edward" },
                    //new ConsoleInteractivePlayer(amount) { Name = "Fred" },

                    alice,
                    new AiPlayer(simulator,predictor, staticData) { Name = "Bob", WeightAmplify = amp },
                    new AiPlayer(simulator,predictor, staticData) { Name = "Charlie", WeightAmplify = amp },
                    new AiPlayer(simulator,predictor, staticData) { Name = "Dave", WeightAmplify = amp },
                    new AiPlayer(simulator,predictor, staticData) { Name = "Edward", WeightAmplify = amp },
                    new AiPlayer(simulator,predictor, staticData) { Name = "Fred", WeightAmplify = amp },

                    //new AlwaysCallPlayer(amount) { Name = "Bob" },
                    //new AlwaysCallPlayer(amount) { Name = "Charlie" },
                    //new AlwaysCallPlayer(amount) { Name = "Dave" },
                    //new AlwaysCallPlayer(amount) { Name = "Edward" },
                    //new AlwaysCallPlayer(amount) { Name = "Fred" },
                };

                foreach (var player in players)
                {
                    player.Balance = amount;
                }
                alice.ShowPredictions = true;
                //alice.RaiseThreshold = 0.4;

                PrintStatus(players);

                int d = GlobalRandom.Next(int.MaxValue);
                int hand = 0;
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
                            PrintStatus(players);
                        }
                    }

                    d++;
                    Console.Title = hand.ToString();
                    if (hand % 1 == 0)
                    {
                        PrintStatus(players);
                    }

                    if (players.Count == 1)
                    {
                        s_log.Info("{0} has won the tournament!", players.First());
                        tournaments++;
                        if (players[0].ToString() == "Alice")
                        {
                            wins++;
                        }
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
    }
}