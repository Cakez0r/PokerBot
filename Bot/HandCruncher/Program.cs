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
            MakeTrainingData(@"D:\Poker data\PS");
        }

        private static void MakeTrainingData(string folder)
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
            List<double[]> vectors = new List<double[]>();
            List<string> labels = new List<string>();
            HandState state = HandState.Preflop;
            Parallel.ForEach(GetFilesRecursive(folder), (fileName) =>
            //foreach (string fileName in GetFilesRecursive(@"D:\Poker data\PS"))
            {
                HandHistoryParser parser = new HandHistoryParser(new PokerstarsHistoryParserRules());
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

                        foreach (var kvp in game.KnownHoleCards)
                        {
                            double[] vector = game.MakeVector(kvp.Key, state);

                            HandClass cls = HandClass.FromCards(kvp.Value[0], kvp.Value[1]);
                            int bucket = ordering[cls];
                            string label = MakeLabel(bucket);
                            lock (vectors)
                            {
                                vectors.Add(vector);
                                labels.Add(label);
                            }
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