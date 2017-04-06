using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Poker
{
    public class LutEvaluator : IHandEvaluator
    {
        private static readonly Logger s_log = LogManager.GetCurrentClassLogger();

        private Dictionary<ulong, double> m_lookup2 = new Dictionary<ulong, double>(1326);
        private Dictionary<ulong, double> m_lookup5 = new Dictionary<ulong, double>(2598960);
        private Dictionary<ulong, double> m_lookup6 = new Dictionary<ulong, double>(20358520);
        private Dictionary<ulong, double> m_lookup7 = new Dictionary<ulong, double>(133784560);

        private IHandEvaluator m_fallback;

        public LutEvaluator(IHandEvaluator fallback, string dataPath)
        {
            m_fallback = fallback;

            s_log.Debug("Loading 2.lut...");
            LoadLookupTable(m_lookup2, dataPath + "/2.lut");

            s_log.Debug("Loading 5.lut...");
            LoadLookupTable(m_lookup5, dataPath + "/5.lut");

            s_log.Debug("Loading 6.lut...");
            LoadLookupTable(m_lookup6, dataPath + "/6.lut");

            s_log.Debug("Loading 7.lut...");
            LoadLookupTable(m_lookup7, dataPath + "/7.lut");
        }

        private static void LoadLookupTable(Dictionary<ulong, double> lut, string filepath)
        {
            const int CHUNK_SIZE = 512 * 1024;
            byte[] chunk = new byte[CHUNK_SIZE];
            using (FileStream stream = File.OpenRead(filepath))
            using (BinaryReader r = new BinaryReader(stream))
            using (MemoryStream ms = new MemoryStream(chunk))
            using (BinaryReader r2 = new BinaryReader(ms))
            {
                while (stream.Position < stream.Length)
                {
                    ms.Position = 0;
                    int bytesRead = r.Read(chunk, 0, CHUNK_SIZE);
                    while (ms.Position < bytesRead)
                    {
                        ulong key = r2.ReadUInt64();
                        double value = r2.ReadDouble();
                        lut[key] = value;
                    }
                }
            }
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
                    return m_lookup2[bmp];

                case 5:
                    return m_lookup5[bmp];

                case 6:
                    return m_lookup6[bmp];

                case 7:
                    return m_lookup7[bmp];

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