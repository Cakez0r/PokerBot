using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Poker
{
    public static class GlobalRandom
    {
        private static ThreadLocal<Random> s_random = new ThreadLocal<Random>(() => new Random((int)DateTime.UtcNow.Ticks + Thread.CurrentThread.ManagedThreadId));

        public static int Next()
        {
            return s_random.Value.Next();
        }

        public static int Next(int min, int max)
        {
            return s_random.Value.Next(min, max);
        }

        public static int Next(int max)
        {
            return s_random.Value.Next(max);
        }

        public static double NextDouble()
        {
            return s_random.Value.NextDouble();
        }

        public static T WeightedRandom<T>(IEnumerable<Tuple<T, double>> weightedItems)
        {
            double sum = 1.0;// weightedItems.Sum(t => t.Item2);

            double rnd = s_random.Value.NextDouble();
            rnd *= sum;

            foreach (var item in weightedItems)
            {
                if (rnd < item.Item2)
                {
                    return item.Item1;
                }

                rnd -= item.Item2;
            }

            return weightedItems.Last().Item1;
        }
    }
}