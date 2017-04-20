using MathNet.Numerics.Random;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Poker
{
    public static class GlobalRandom
    {
        private static Logger s_log = LogManager.GetCurrentClassLogger();

        private static ThreadLocal<Xorshift> s_random = new ThreadLocal<Xorshift>(() => new Xorshift(false));

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

        /// <summary>
        /// WARN: Assumes all weights sum to 1.0
        /// </summary>
        public static T WeightedRandom<T>(IReadOnlyList<Tuple<T, double>> weightedItems)
        {
            double rnd = s_random.Value.NextDouble();

            for (int i = 0; i < weightedItems.Count; i++)
            {
                var item = weightedItems[i];
                if (rnd < item.Item2)
                {
                    return item.Item1;
                }

                rnd -= item.Item2;
            }

            s_log.Warn("Weighted random failed");

            return weightedItems.First().Item1;
        }
    }
}