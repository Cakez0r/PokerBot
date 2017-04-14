using Combinatorics.Collections;
using HandCruncher;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Poker
{
    internal class Program
    {
        private const int NUM_BUCKETS = 169;

        private static void Main(string[] args)
        {
            double[] ramp = BankrollSimulator(1500);
            File.WriteAllText("ramp.json", JsonConvert.SerializeObject(ramp));
            //CreateTrainingFiles(HandState.Preflop);
            //CreateTrainingFiles(HandState.Flop);
            //CreateTrainingFiles(HandState.Turn);
            //CreateTrainingFiles(HandState.River);
        }

        private static double[] BankrollSimulator(double bankroll)
        {
            const int NUM_GAMES = 25_000_000;
            double[] betRamp = new double[101];
            Parallel.For(1, 101, (i) =>
            {
                int threshold = 100 - i;
                for (int ramp = 0; ramp < 500; ramp++)
                {
                    double balance = bankroll;
                    double betSize = bankroll * ((double)(500 - ramp) / 1000);
                    for (int j = 0; j < NUM_GAMES; j++)
                    {
                        int res = GlobalRandom.Next(100);
                        if (res >= threshold)
                        {
                            double odds = (double)threshold / i;
                            odds = Math.Min(odds, 10);
                            balance += betSize;// + (betSize * odds);
                        }
                        else
                        {
                            balance -= betSize;
                        }

                        if (balance <= 0)
                        {
                            break;
                        }

                        if (balance > bankroll * 1.2)
                        {
                            balance = bankroll * 1.2;
                        }
                    }

                    if (balance > bankroll)
                    {
                        Console.WriteLine("At {0}% risk, max bet is {1}", i, betSize);
                        betRamp[i] = betSize / 2.0;
                        break;
                    }
                }
            });

            return betRamp;
        }

        private static void CreateTrainingFiles(HandState state)
        {
            ConcurrentDictionary<HandClass, ConcurrentBag<Tuple<double[], string>>> data = new ConcurrentDictionary<HandClass, ConcurrentBag<Tuple<double[], string>>>();
            MakeTrainingData<FullTiltPokerHistoryParserRules>(@"D:\Poker data\FTP", state, data);
            MakeTrainingData<PokerstarsHistoryParserRules>(@"D:\Poker data\PS", state, data);
            MakeTrainingData<PartyPokerHistoryParserRules>(@"D:\Poker data\PTY", state, data);

            int minSamples = data.Min(kv => kv.Value.Count) * 5;// (data.Min(kv => kv.Value.Count) + data.Max(kv => kv.Value.Count)) / 2;
            List<double[]> vectors = new List<double[]>(minSamples * data.Count);
            List<string> labels = new List<string>(minSamples * data.Count);

            foreach (var kv in data)
            {
                var bag = kv.Value.ToList();
                int[] idxs = Shuffle(bag.Count);
                for (int i = 0; i < minSamples; i++)
                {
                    int idx = idxs[i % idxs.Length];
                    vectors.Add(bag[idx].Item1);
                    labels.Add(bag[idx].Item2);
                }
            }

            int[] shuffle = Shuffle(vectors.Count);
            List<double[]> shuffledVectors = new List<double[]>(vectors.Count);
            List<string> shuffledLabels = new List<string>(labels.Count);
            for (int i = 0; i < shuffle.Length; i++)
            {
                shuffledVectors.Add(vectors[shuffle[i]]);
                shuffledLabels.Add(labels[shuffle[i]]);
            }

            vectors = shuffledVectors;
            labels = shuffledLabels;

            double[] avg = new double[vectors.First().Length];
            foreach (double[] v in vectors)
            {
                for (int i = 0; i < v.Length; i++)
                {
                    avg[i] += v[i];
                }
            }

            for (int i = 0; i < avg.Length; i++)
            {
                avg[i] /= vectors.Count;
            }

            foreach (double[] v in vectors)
            {
                for (int i = 0; i < v.Length; i++)
                {
                    v[i] -= avg[i];
                }
            }

            string stateString = state.ToString().ToLower();
            File.WriteAllText(stateString + "_average.json", JsonConvert.SerializeObject(avg));
            File.WriteAllLines(stateString + "_labels", labels);
            File.WriteAllLines(stateString + "_data", vectors.Select(v => string.Join(" ", v)));
            File.WriteAllLines(stateString + "_rep", data.OrderByDescending(kv => kv.Value.Count).Select(kv => kv.Key.ToString() + ": " + kv.Value.Count));
        }

        private static int[] Shuffle(int count)
        {
            int[] idxs = new int[count];
            for (int i = 0; i < count; i++)
            {
                int j = GlobalRandom.Next(i + 1);
                if (j != i)
                {
                    idxs[i] = idxs[j];
                }

                idxs[j] = i;
            }

            return idxs;
        }

        private static void MakeTrainingData<T>(string folder, HandState state, ConcurrentDictionary<HandClass, ConcurrentBag<Tuple<double[], string>>> data) where T : IHistoryParserRules, new()
        {
            Dictionary<HandClass, int> ordering = new Dictionary<HandClass, int>();
            List<HandClass> loadedHands = JsonConvert.DeserializeObject<List<HandClass>>(File.ReadAllText("../../../../datafiles/ordering.json"));
            int n = 0;
            foreach (var o in loadedHands)
            {
                ordering[o] = n;
                n++;
            }

            int count = 0;
            Parallel.ForEach(GetFilesRecursive(folder), (fileName) =>
            //foreach (string fileName in GetFilesRecursive(folder))
            {
                var parserRules = new T();
                HandHistoryParser parser = new HandHistoryParser(parserRules);
                foreach (var game in parser.GetGames(File.ReadAllLines(fileName)))
                {
                    try
                    {
                        if (game.Seats.Count < 3)
                        {
                            continue;
                        }

                        //if (game.PreflopActions.Count < game.Seats.Count && (game.PreflopActions.All(a => a.Type == GameActionType.Fold) || game.StartBalances.Values.Any(b => b == game.BigBlind)))
                        //{
                        //    continue;
                        //}

                        if (state == HandState.Flop)
                        {
                            if (game.BoardCards.Count < 3)
                            {
                                continue;
                            }

                            if (game.FlopActions.Count == 0)
                            {
                                continue;
                            }

                            if (game.FlopActions.Select(kv => kv.Name).Distinct().Count() == 1)
                            {
                                continue;
                            }
                        }

                        if (state == HandState.Turn)
                        {
                            if (game.BoardCards.Count < 4)
                            {
                                continue;
                            }

                            if (game.TurnActions.Count == 0)
                            {
                                continue;
                            }

                            if (game.TurnActions.Select(kv => kv.Name).Distinct().Count() == 1)
                            {
                                continue;
                            }
                        }

                        if (state == HandState.River)
                        {
                            if (game.BoardCards.Count < 5)
                            {
                                continue;
                            }

                            if (game.RiverActions.Count == 0)
                            {
                                continue;
                            }

                            if (game.RiverActions.Select(kv => kv.Name).Distinct().Count() == 1)
                            {
                                continue;
                            }
                        }

                        foreach (var kvp in game.KnownHoleCards)
                        {
                            double[] vector = game.MakeVector(kvp.Key, state);
                            if (vector.Any(d => double.IsNaN(d) || double.IsInfinity(d)))
                            {
                                throw new Exception("Bad vector value");
                            }

                            HandClass cls = HandClass.FromCards(kvp.Value[0], kvp.Value[1]);
                            int bucket = ordering[cls];
                            string label = MakeLabel(bucket);
                            data.AddOrUpdate(cls, new ConcurrentBag<Tuple<double[], string>>(), (c, b) =>
                            {
                                b.Add(Tuple.Create(vector, label));
                                return b;
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Failed to make vector " + ex);
                    }
                }
                Interlocked.Increment(ref count);
                Console.Title = count.ToString();
                Console.WriteLine(fileName);
            });
        }

        private static void GetHandRepresentations(string folder)
        {
            ConcurrentDictionary<HandClass, int> dict = new ConcurrentDictionary<HandClass, int>();
            Parallel.ForEach(GetFilesRecursive(folder), (fileName) =>
            {
                string[] lines = File.ReadAllLines(fileName);
                foreach (string s in lines)
                {
                    //if (s.Contains("showed") && !s.Contains("said"))
                    if (s.Contains("shows"))
                    {
                        try
                        {
                            var splits = s.Split('[', ']');
                            var a = CardParser.MakeCard(splits[1].Substring(1, 2));
                            var b = CardParser.MakeCard(splits[1].Substring(5, 2));
                            var c = HandClass.FromCards(a, b);
                            dict.AddOrUpdate(c, 1, (h, i) => i + 1);
                        }
                        catch { }
                    }
                }
                //dict.AddOrUpdate()
            });

            File.WriteAllLines("rep2", dict.OrderByDescending(kv => kv.Value).Select(kv => kv.Key.ToString() + " - " + kv.Value));
        }

        private static void MakeLookups()
        {
            MakeLookup(2);
            MakeLookup(5);
            MakeLookup(6);
            MakeLookup(7);
        }

        private static void MakeLookup(int numCards)
        {
            DateTime start = DateTime.Now;
            var iter = new Combinations<int>(Enumerable.Range(0, 52).ToList(), numCards, GenerateOption.WithoutRepetition);
            int capacity = 0;
            switch (numCards)
            {
                case 2: capacity = 1326; break;
                case 5: capacity = 2598960; break;
                case 6: capacity = 20358520; break;
                case 7: capacity = 133784560; break;
            }
            var scores = new ConcurrentDictionary<ulong, double>(16, capacity);
            HandEvaluator evaluator = new HandEvaluator();
            Parallel.ForEach(iter, (combo) =>
            {
                Card[] cards = combo.Select(c => Card.FromIndex(c)).ToArray();
                double score = evaluator.Evaluate(cards).Score;
                scores[Card.MakeHandBitmap(cards)] = score;
            });

            using (BinaryWriter w = new BinaryWriter(File.OpenWrite(numCards.ToString() + ".lut")))
            {
                foreach (var kv in scores)
                {
                    w.Write(kv.Key);
                    w.Write(kv.Value);
                }
            }
            Console.WriteLine(DateTime.Now - start);
        }

        private static string MakeLabel(int i)
        {
            int[] labels = new int[NUM_BUCKETS];
            labels[i] = 1;

            StringBuilder sb = new StringBuilder();

            for (int n = 0; n < labels.Length; n++)
            {
                sb.Append(labels[n]);
                sb.Append(' ');
            }

            return sb.ToString().Trim();
        }

        private static IEnumerable<string> GetFilesRecursive(string folder)
        {
            foreach (var fileName in Directory.GetFiles(folder))
            {
                yield return fileName;
            }

            foreach (var dir in Directory.GetDirectories(folder))
            {
                foreach (var fileName in GetFilesRecursive(dir))
                {
                    yield return fileName;
                }
            }
        }
    }
}