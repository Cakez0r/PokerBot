using Combinatorics.Collections;
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
            MakeLookup(2);
            MakeLookup(5);
            MakeLookup(6);
            MakeLookup(7);
            // ParseFiles();
            // MakeLabelsAndData();
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

        private static void MakeLabelsAndData()
        {
            Dictionary<HandClass, float> hands = new Dictionary<HandClass, float>();
            HashSet<HandClass> testedTypes = new HashSet<HandClass>();
            /*for (int s1 = 0; s1 < 4; s1++)
            {
                Parallel.For(2, 15, (f1) =>
                {
                    for (int s2 = 0; s2 < 4; s2++)
                    {
                        for (int f2 = 2; f2 < 15; f2++)
                        {
                            if (s1 == s2 && f1 == f2)
                            {
                                continue;
                            }

                            Card[] testHand = new Card[] { new Card((Suit)s1, (Face)f1), new Card((Suit)s2, (Face)f2) };

                            int sampleCount = 100_000;

                            var handClass = HandClass.FromCards(testHand[0], testHand[1]);

                            lock (testedTypes)
                            {
                                if (testedTypes.Contains(handClass))
                                {
                                    continue;
                                }

                                testedTypes.Add(handClass);
                            }

                            float winRate = (float)HandEvaluator.WeightedSimulate(testHand[0], testHand[1], new Card[0], new List<List<Tuple<HandClass, double>>>() { HandEvaluator.EvenWeights }, sampleCount);
                            lock (hands)
                            {
                                hands[handClass] = winRate;
                            }
                        }
                    }
                });
            }

            File.WriteAllText("ordering.json", JsonConvert.SerializeObject(hands.OrderByDescending(kv => kv.Value).Select(kv => kv.Key)));
            */

            Dictionary<HandClass, int> ordering = new Dictionary<HandClass, int>();
            //int n = 0;
            //foreach (var o in hands.OrderByDescending(kv => kv.Value))
            //{
            //    Console.WriteLine("{0}: {1}", o.Key, o.Value);
            //    ordering[o.Key] = n;
            //    n++;
            //}
            List<HandClass> loadedHands = JsonConvert.DeserializeObject<List<HandClass>>(File.ReadAllText("ordering.json"));
            int n = 0;
            foreach (var o in loadedHands)
            {
                ordering[o] = n;
                n++;
            }
            string[] lines = File.ReadAllLines("data.txt");
            List<string> labels = new List<string>();
            List<double[]> data = new List<double[]>();
            double[] avg = new double[15];
            foreach (string line in lines)
            {
                Card a = new Card(CharToSuit(line[1]), CharToFace(line[0]));
                Card b = new Card(CharToSuit(line[4]), CharToFace(line[3]));
                HandClass c = HandClass.FromCards(a, b);
                int bucket = ordering[c];

                labels.Add(MakeLabel(bucket));
                double[] lineDoubles = line.Substring(6).Split(' ').Select(d => double.Parse(d)).ToArray();
                for (int i = 0; i < lineDoubles.Length; i++)
                {
                    avg[i] += lineDoubles[i];
                }
                data.Add(lineDoubles);
            }

            for (int i = 0; i < avg.Length; i++)
            {
                avg[i] /= data.Count;
            }

            foreach (var d in data)
            {
                for (int i = 0; i < avg.Length; i++)
                {
                    d[i] -= avg[i];
                }
            }

            File.WriteAllText("average.json", JsonConvert.SerializeObject(avg));

            File.WriteAllLines("labels", labels);
            File.WriteAllLines("data", data.Select(d => string.Join(" ", d)));
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

        private static Suit CharToSuit(char c)
        {
            c = c.ToString().ToLower()[0];

            switch (c)
            {
                case 'c': return Suit.Clubs;
                case 's': return Suit.Spades;
                case 'h': return Suit.Hearts;
                case 'd': return Suit.Diamonds;
            }

            return Suit.Clubs;
        }

        private static Face CharToFace(char c)
        {
            c = c.ToString().ToLower()[0];

            switch (c)
            {
                case 'a': return Face.Ace;
                case '2': return Face.Two;
                case '3': return Face.Three;
                case '4': return Face.Four;
                case '5': return Face.Five;
                case '6': return Face.Six;
                case '7': return Face.Seven;
                case '8': return Face.Eight;
                case '9': return Face.Nine;
                case 't': return Face.Ten;
                case 'j': return Face.Jack;
                case 'q': return Face.Queen;
                case 'k': return Face.King;
            }

            return Face.Ace;
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

        private static void ParseFiles()
        {
            List<string> dat = new List<string>();
            //Parallel.ForEach(GetFilesRecursive(@"D:\Poker data\PS\PS-2009-07-01_2009-07-23_25NLH_OBFU"), (fileName) =>
            Parallel.ForEach(GetFilesRecursive(@"D:\Poker data\PS"), (fileName) =>
            //foreach (var fileName in GetFilesRecursive(@"D:\Poker data\PS\PS-2009-07-01_2009-07-23_25NLH_OBFU"))
            {
                Console.WriteLine(fileName);
                ParseFile(fileName, dat);
            });

            File.WriteAllLines("data.txt", dat);
        }

        private static void ParseFile(string fileName, List<string> dat)
        {
            string[] lines = File.ReadAllLines(fileName);
            for (int i = 0; i < lines.Length; i++)
            {
                try
                {
                    if (lines[i].Contains("shows ["))
                    {
                        var cardsMatch = Regex.Match(lines[i], "\\[(.)(.) (.)(.)\\]");

                        string id = Regex.Match(lines[i], "[^:]+").Value;
                        int backptr = i - 1;
                        decimal betSize = 0;
                        while (!lines[backptr].Contains("Game #"))
                        {
                            backptr--;
                        }

                        backptr++;
                        var buttonMatch = Regex.Match(lines[backptr], "#([0-9]) is the button");
                        int button = int.Parse(buttonMatch.Groups[1].Value);

                        backptr++;
                        Match seatMatch = null;
                        Dictionary<string, int> seats = new Dictionary<string, int>();
                        Dictionary<string, decimal> chips = new Dictionary<string, decimal>();
                        while ((seatMatch = Regex.Match(lines[backptr], "Seat ([0-9]): ([^ ]+) \\(\\$(([0-9]|\\.)+) in chips")).Success)
                        {
                            seats[seatMatch.Groups[2].Value] = int.Parse(seatMatch.Groups[1].Value);
                            chips[seatMatch.Groups[2].Value] = decimal.Parse(seatMatch.Groups[3].Value);
                            backptr++;
                        }

                        bool isSmallBlind = false;
                        decimal pot = 0;
                        //sb
                        Match sbMatch = null;
                        while (!(sbMatch = Regex.Match(lines[backptr], "posts small blind \\$(([0-9]|\\.)+)")).Success)
                        {
                            backptr++;
                        }
                        if (lines[backptr].Contains(id))
                        {
                            isSmallBlind = true;
                        }
                        decimal sb = decimal.Parse(sbMatch.Groups[1].Value);
                        pot += sb;
                        backptr++;

                        bool isBigBlind = false;
                        //bb
                        Match bbMatch = null;
                        while (!(bbMatch = Regex.Match(lines[backptr], "posts big blind \\$(([0-9]|\\.)+)")).Success)
                        {
                            backptr++;
                        }
                        if (lines[backptr].Contains(id))
                        {
                            isBigBlind = true;
                        }
                        decimal bb = decimal.Parse(bbMatch.Groups[1].Value);
                        pot += bb;
                        backptr++;

                        while (!lines[backptr].Contains("HOLE CARDS"))
                        {
                            backptr++;
                        }

                        backptr++;

                        List<decimal> potPercentages = new List<decimal>();

                        int betsBefore = 0;
                        int numActs = 0;
                        int numChecks = 0;
                        int numCalls = 0;
                        int numRaises = 0;
                        while (!lines[backptr].Contains("*** ") || lines[backptr].Contains("said"))
                        {
                            if (lines[backptr].Contains(id) && lines[backptr].Contains("checks"))
                            {
                                numActs++;
                                numChecks++;
                            }

                            var match = Regex.Match(lines[backptr], "([^:]+): (bets|calls) \\$(([0-9]|\\.)+)");
                            if (match.Success)
                            {
                                var d = decimal.Parse(match.Groups[3].Value);
                                if (lines[backptr].Contains(id))
                                {
                                    betSize += d;
                                    potPercentages.Add(d / pot);
                                    numActs++;

                                    if (lines[backptr].Contains("bets"))
                                    {
                                        numRaises++;
                                    }
                                    else
                                    {
                                        numCalls++;
                                    }
                                }
                                else
                                {
                                    if (numActs == 0)
                                    {
                                        betsBefore++;
                                    }
                                }
                                pot += d;
                            }
                            //2nd group is value

                            match = Regex.Match(lines[backptr], "([^:]+): raises \\$(([0-9]|\\.)+) to \\$(([0-9]|\\.)+)");
                            if (match.Success)
                            {
                                var d = decimal.Parse(match.Groups[4].Value);
                                if (lines[backptr].Contains(id))
                                {
                                    betSize = d;
                                    potPercentages.Add(d / pot);
                                    numActs++;
                                    numRaises++;
                                }
                                else
                                {
                                    if (numActs == 0)
                                    {
                                        betsBefore++;
                                    }
                                }
                                pot += d;
                            }

                            backptr++;
                        }

                        if (pot == sb + bb)
                        {
                            continue;
                        }

                        if (seats.Count != 2)
                        {
                            continue;
                        }

                        var seatList = seats.ToList();
                        int seatListDealer = seatList.FindIndex(kv => kv.Value == button);

                        int actPosition = 0;
                        int firstToActIndex = (seatListDealer + 3) % seatList.Count;
                        for (int p = 0; p < seats.Count; p++)
                        {
                            int x = (firstToActIndex + p) % seatList.Count;
                            if (seatList[x].Key == id)
                            {
                                actPosition = p;
                            }
                        }

                        decimal actPct = (decimal)actPosition / (seats.Count - 1);
                        if (actPct > 1 || actPct < 0)
                        {
                            throw new Exception("Bad actPct");
                        }

                        decimal numBigBlinds = betSize / bb;
                        decimal stackPct = betSize / chips[id];
                        if (stackPct > 1)
                        {
                            throw new Exception("Bad stackPct");
                        }

                        decimal potPct = betSize / pot;
                        if (potPct > 1)
                        {
                            throw new Exception("Bad potPct");
                        }

                        decimal avgStackPct = betSize / chips.Values.Average();
                        int numBettersBefore = betsBefore;

                        int numPeopleToAct = seats.Count - 1 - actPosition;
                        if (numPeopleToAct < 0)
                        {
                            throw new Exception("Bad numPeopleToAct");
                        }

                        int numTimesActed = numActs;
                        if (numActs <= 0)
                        {
                            throw new Exception("Bad numActs");
                        }
                        if (numTimesActed != numRaises + numChecks + numCalls)
                        {
                            throw new Exception("Bad act counts");
                        }

                        if (numRaises + numChecks + numCalls == 0)
                        {
                            throw new Exception("Bad act counts 2");
                        }

                        decimal stackSizeVsAverage = chips[id] / chips.Values.Average();
                        decimal potSizeWithoutContributions = (pot - betSize) / bb;
                        if (potSizeWithoutContributions < 0)
                        {
                            throw new Exception("Bad potSizeWithoutContributions");
                        }

                        var str = string.Format("{0}{1} {2}{3} {4} {5} {6} {7} {8} {9} {10} {11} {12} {13} {14} {15} {16} {17} {18}",
                            cardsMatch.Groups[1].Value, cardsMatch.Groups[2].Value,
                            cardsMatch.Groups[3].Value, cardsMatch.Groups[4].Value,
                            actPct,
                            numBigBlinds,
                            stackPct,
                            potPct,
                            avgStackPct,
                            numBettersBefore,
                            numPeopleToAct,
                            stackSizeVsAverage,
                            numTimesActed,
                            numChecks,
                            numCalls,
                            numRaises,
                            potSizeWithoutContributions,
                            isBigBlind ? 1.0 : 0.0,
                            isSmallBlind ? 1.0 : 0.0);

                        //Console.WriteLine(str);
                        lock (dat)
                        {
                            dat.Add(str);
                        }
                    }
                }
                catch (KeyNotFoundException)
                {
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }
    }
}