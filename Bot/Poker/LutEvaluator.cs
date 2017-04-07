using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Poker
{
    public class LutEvaluator : IHandEvaluator
    {
        private static readonly Logger s_log = LogManager.GetCurrentClassLogger();

        private IDictionary<ulong, double>[] m_lookup2 = new Dictionary<ulong, double>[1];
        private IDictionary<ulong, double>[] m_lookup5 = new Dictionary<ulong, double>[10];
        private IDictionary<ulong, double>[] m_lookup6 = new Dictionary<ulong, double>[20];
        private IDictionary<ulong, double>[] m_lookup7 = new Dictionary<ulong, double>[56];

        private IHandEvaluator m_fallback;

        public LutEvaluator(IHandEvaluator fallback, string dataPath)
        {
            m_fallback = fallback;

            s_log.Debug("Loading 2.lut...");
            for (int i = 0; i < m_lookup2.Length; i++)
            {
                m_lookup2[i] = new Dictionary<ulong, double>(1326 / m_lookup2.Length);
            }
            LoadLookupTable(m_lookup2, dataPath + "/2.lut");

            s_log.Debug("Loading 5.lut...");
            for (int i = 0; i < m_lookup5.Length; i++)
            {
                m_lookup5[i] = new Dictionary<ulong, double>(2598960 / m_lookup5.Length);
            }
            LoadLookupTable(m_lookup5, dataPath + "/5.lut");

            s_log.Debug("Loading 6.lut...");
            for (int i = 0; i < m_lookup6.Length; i++)
            {
                m_lookup6[i] = new Dictionary<ulong, double>(20358520 / m_lookup6.Length);
            }
            LoadLookupTable(m_lookup6, dataPath + "/6.lut");

            s_log.Debug("Loading 7.lut...");
            for (int i = 0; i < m_lookup7.Length; i++)
            {
                m_lookup7[i] = new Dictionary<ulong, double>(133784560 / m_lookup7.Length);
            }
            LoadLookupTable(m_lookup7, dataPath + "/7.lut");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint MakeBucketKey(ulong x)
        {
            x ^= x >> 12;
            x ^= x << 25;
            x ^= x >> 27;
            return (uint)(x * 0x2545F4914F6CDD1D);
        }

        private static void LoadLookupTable(IDictionary<ulong, double>[] lut, string filepath)
        {
            DateTime start = DateTime.Now;
            const int CHUNK_SIZE = 512 * 1024;
            byte[] chunk = new byte[CHUNK_SIZE];
            using (FileStream stream = File.OpenRead(filepath))
            using (BinaryReader r = new BinaryReader(stream))
            {
                while (stream.Position < stream.Length)
                {
                    int bytesRead = r.Read(chunk, 0, CHUNK_SIZE);
                    Parallel.For(0, bytesRead / 16, (i) =>
                    {
                        ulong key = BitConverter.ToUInt64(chunk, i * 16);
                        double value = BitConverter.ToDouble(chunk, (i * 16) + 8);
                        var kvp = new KeyValuePair<ulong, double>(key, value);
                        var k = MakeBucketKey(key) % lut.Length;
                        var bucket = lut[k];
                        lock (bucket)
                        {
                            bucket.Add(kvp);
                        }
                    });
                }
            }
            s_log.Info(DateTime.Now - start);
        }

        public HandEvaluation Evaluate(IEnumerable<Card> cards)
        {
            if (m_fallback == null)
            {
                throw new InvalidOperationException("LutEvaluator cannot produce a full hand evaluation and no fallback was specified");
            }

            return m_fallback.Evaluate(cards);
        }

        public double EvaluateScore(IEnumerable<Card> cards)
        {
            int count = cards.Count();
            ulong bmp = Card.MakeHandBitmap(cards);

            switch (count)
            {
                case 2:
                    return m_lookup2[MakeBucketKey(bmp) % m_lookup2.Length][bmp];

                case 5:
                    return m_lookup5[MakeBucketKey(bmp) % m_lookup5.Length][bmp];

                case 6:
                    return m_lookup6[MakeBucketKey(bmp) % m_lookup6.Length][bmp];

                case 7:
                    return m_lookup7[MakeBucketKey(bmp) % m_lookup7.Length][bmp];

                default:
                    if (m_fallback == null)
                    {
                        throw new InvalidOperationException($"LutEvaluator cannot produce an evaluation for hand of size {count} and no fallback was specified");
                    }
                    return m_fallback.EvaluateScore(cards);
            }
        }
    }
}