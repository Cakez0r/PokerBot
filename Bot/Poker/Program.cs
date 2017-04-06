using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Poker
{
    internal class Program
    {
        private static ILogger s_log = LogManager.GetCurrentClassLogger();

        private static int wins = 0;
        private static int tournaments = 0;

        private static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.Unicode;
            Console.BackgroundColor = ConsoleColor.DarkGreen;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Clear();

            IHandEvaluator evaluator = new HandEvaluator();
            ISimulator simulator = new Simulator(evaluator);
            IStaticData staticData = new StaticData("../datafiles");
            IHandPredictor predictor = new NeuralNetHandPredictor(staticData);

            while (true)
            {
                int amount = 200;
                List<IPlayer> players = new List<IPlayer>()
                {
                    //new ConsoleInteractivePlayer(amount) { Name = "Lewis" },

                    new AiPlayer(simulator,predictor, staticData) { Name = "Alice" },
                    new AiPlayer(simulator,predictor, staticData) { Name = "Bob" },
                    new AiPlayer(simulator,predictor, staticData) { Name = "Charlie" },
                    new AiPlayer(simulator,predictor, staticData) { Name = "Dave" },
                    new AiPlayer(simulator,predictor, staticData) { Name = "Edward" },
                    new AiPlayer(simulator,predictor, staticData) { Name = "Fred" },

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
                ((AiPlayer)players.First(p => p is AiPlayer)).ShowPredictions = true;

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

                    Console.ReadKey();
                    //Console.Clear();
                }
            }
        }

        private static void PrintStatus(IEnumerable<IPlayer> players)
        {
            //Console.Clear();
            //Console.WriteLine("Wins: {0}  Games: {1}  Win rate: {2}", wins, tournaments, (double)wins / tournaments);
            //foreach (var p in players.OrderByDescending(p => p.Balance))
            //{
            //    Console.WriteLine("{0}: {1}", p, p.Balance);
            //}
        }
    }
}