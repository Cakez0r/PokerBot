using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Poker
{
    public class RiggedDeck : IDeck
    {
        private static Logger s_log = LogManager.GetCurrentClassLogger();

        private static readonly IReadOnlyList<Card> s_allCards;

        private Card[] m_cards = new Card[52];
        private int m_ptr;

        static RiggedDeck()
        {
            Card[] allCards = new Card[52];
            for (int s = 0; s < 4; s++)
            {
                for (int f = 2; f < 15; f++)
                {
                    Card c = new Card((Suit)s, (Face)f);
                    allCards[Card.ToIndex(c)] = c;
                }
            }
            s_allCards = allCards;
        }

        private RiggedDeck()
        {
        }

        public RiggedDeck(IReadOnlyList<Card> dealFirst)
        {
            ulong bm = Card.MakeHandBitmap(dealFirst);
            int[] indices = new int[dealFirst.Count];

            for (int i = 0; i < 52; i++)
            {
                int rand = GlobalRandom.Next(i + 1);
                if (rand != i)
                {
                    m_cards[i] = m_cards[rand];
                }

                m_cards[rand] = s_allCards[i];
            }

            int idx = 0;
            int trueCount = 0;
            while (bm > 0)
            {
                var c = Card.ToBitmap(m_cards[idx]);
                if ((bm & c) > 0)
                {
                    for (int i = 0; i < dealFirst.Count; i++)
                    {
                        if (Card.ToIndex(m_cards[idx]) == Card.ToIndex(dealFirst[i]))
                        {
                            m_cards[idx] = m_cards[i];
                            m_cards[i] = dealFirst[i];
                            break;
                        }
                    }

                    bm = bm ^ c;
                }
                idx++;
                trueCount++;
                idx = idx % m_cards.Length;
            }
        }

        public IDeck Clone()
        {
            return new RiggedDeck()
            {
                m_cards = (Card[])m_cards.Clone(),
                m_ptr = m_ptr
            };
        }

        public Card Deal()
        {
            int idx = Interlocked.Increment(ref m_ptr);
            return m_cards[idx - 1];
        }
    }
}