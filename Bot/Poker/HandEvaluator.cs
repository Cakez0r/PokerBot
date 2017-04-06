using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Poker
{
    public class HandEvaluator : IHandEvaluator
    {
        private static Logger s_log = LogManager.GetCurrentClassLogger();

        public double EvaluateScore(IEnumerable<Card> cards)
        {
            return Evaluate(cards).Score;
        }

        public HandEvaluation Evaluate(IEnumerable<Card> cards)
        {
            List<Card> actives = new List<Card>(5);
            List<Card> kickers = new List<Card>(5);

            var groupedBySuit = cards.GroupBy(c => c.Suit).ToList();
            bool isFlush = groupedBySuit.Any(g => g.Count() == 5);

            if (isFlush)
            {
                actives.AddRange(groupedBySuit.First(g => g.Count() == 5));
            }

            List<Card> straightActives = new List<Card>(5);
            bool isStraight = false;
            for (int i = 14; i > 4; i--)
            {
                actives.Clear();
                bool consecutive = true;
                for (int j = 0; j < 5; j++)
                {
                    bool found = cards.Any(c => (int)c.Face == i - j);
                    
                    if (i == 5 && j == 4)
                    {
                        found = cards.Any(c => c.Face == Face.Ace);
                    }

                    if (found)
                    {
                        actives.Add(cards.FirstOrDefault(c => (int)c.Face == i - j) ?? cards.First(c => c.Face == Face.Ace));
                    }
                    else
                    {
                        consecutive = false;
                        break;
                    }
                }

                if (consecutive)
                {
                    isStraight = true;
                    straightActives.AddRange(actives);
                    break;
                }
            }

            //straight flush
            if (isStraight && straightActives.All(c => c.Suit == straightActives.First().Suit))
            {
                return new HandEvaluation(actives, kickers, (double)8 + MakeDouble(actives.OrderByDescending(c => c.Face).Take(1)), HandType.StraightFlush);
            }

            actives.Clear();
            
            //Quads
            var groupedByFace = cards.GroupBy(c => c.Face).OrderByDescending(f => (int)f.Key).ToList();
            if (groupedByFace.Any(g => g.Count() == 4))
            {
                actives.AddRange(groupedByFace.First(g => g.Count() == 4));
                kickers.AddRange(cards.Where(c => !actives.Contains(c)).OrderByDescending(c => c.Face).Take(1));
                return new HandEvaluation(actives, kickers, (double)7 + MakeDouble(actives.Take(1)) + (MakeDouble(kickers) / 100), HandType.FourOfAKind);
            }

            //Full House
            var trips = groupedByFace.FirstOrDefault(g => g.Count() == 3);
            if (trips != null)
            {
                var dubs = groupedByFace.FirstOrDefault(g => g.Count() >= 2 && g.Key != trips.Key);
                if (dubs != null)
                {
                    actives.AddRange(trips);
                    actives.AddRange(dubs);

                    return new HandEvaluation(actives, kickers, (double)6 + MakeDouble(trips.Take(1)) + (MakeDouble(dubs.Take(1)) / 100), HandType.FullHouse);
                }
            }

            //Flush
            if (isFlush)
            {
                actives.AddRange(groupedBySuit.First(g => g.Count() == 5));

                return new HandEvaluation(actives, kickers, (double)5 + MakeDouble(actives.OrderByDescending(c => c.Face).Take(1)), HandType.Flush);
            }

            //Straight
            if (isStraight)
            {
                return new HandEvaluation(straightActives, kickers, (double)4 + MakeDouble(straightActives.OrderByDescending(c => c.Face).Take(1)), HandType.Straight);
            }

            //Trips
            if (trips != null)
            {
                actives.AddRange(trips);
                kickers.AddRange(cards.Where(c => !actives.Contains(c)).OrderByDescending(c => c.Face).Take(2));

                return new HandEvaluation(actives, kickers, (double)3 + MakeDouble(kickers), HandType.ThreeOfAKind);
            }

            //Two pair
            if (groupedByFace.Count(g => g.Count() == 2) >= 2)
            {
                actives.AddRange(groupedByFace.Where(g => g.Count() == 2).OrderByDescending(g => (int)g.Key).Take(2).SelectMany(g => g));
                kickers.AddRange(cards.Where(c => !actives.Contains(c)).OrderByDescending(c => c.Face).Take(1));

                return new HandEvaluation(actives, kickers, (double)2 + MakeDouble(actives.Take(1)) + (MakeDouble(actives.Skip(2).Take(1)) / 100) + (MakeDouble(kickers) / 10000), HandType.TwoPairs);
            }

            //Pair
            if (groupedByFace.Any(g => g.Count() == 2))
            {
                actives.AddRange(groupedByFace.Where(g => g.Count() == 2).OrderByDescending(g => (int)g.Key).First());
                kickers.AddRange(cards.Where(c => !actives.Contains(c)).OrderByDescending(c => c.Face).Take(3));

                return new HandEvaluation(actives, kickers, (double)1 + MakeDouble(actives.Take(1)) + (MakeDouble(kickers) / 100), HandType.Pair);
            }

            //High card
            actives.Add(cards.OrderByDescending(c => c.Face).First());
            kickers.AddRange(cards.OrderByDescending(c => c.Face).Skip(1).Take(4));

            return new HandEvaluation(actives, kickers, (double)0 + MakeDouble(actives) + (MakeDouble(kickers) / 100), HandType.HighCard);
        }

        private static double MakeDouble(IEnumerable<Card> cards)
        {
            double d = 0;
            int divisor = 100;
            const double worth = (double)100.0 / 13;
            foreach (int n in cards.Select(c => (int)c.Face).OrderByDescending(c => c))
            {
                double c = (((double)(n - 2)) * worth) / divisor;
                d += c;
                divisor *= 100;
            }

            return d;
        }
    }
}
