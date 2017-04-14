using NLog;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Poker
{
    public class Deck : IDeck
    {
        private static Logger s_log = LogManager.GetCurrentClassLogger();

        private static readonly IReadOnlyList<Card> s_allCards;

        private Card[] m_cards = new Card[52];
        private int m_ptr;

        static Deck()
        {
            Card[] allCards = new Card[52];
            int i = 0;
            for (int s = 0; s < 4; s++)
            {
                for (int f = 2; f < 15; f++)
                {
                    Card c = new Card((Suit)s, (Face)f);
                    allCards[i++] = c;
                }
            }
            s_allCards = allCards;
        }

        public Deck()
        {
            for (int i = 0; i < 52; i++)
            {
                int rand = GlobalRandom.Next(i + 1);
                if (rand != i)
                {
                    m_cards[i] = m_cards[rand];
                }
                m_cards[rand] = s_allCards[i];
            }
        }

        public IDeck Clone()
        {
            return new Deck()
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

        public void Rig(IEnumerable<Card> dealNext)
        {
            int i = 0;
            foreach (Card c in dealNext)
            {
                int n = Array.IndexOf(m_cards, c);

                if (n < 0)
                {
                    throw new Exception("Failed to find rig card");
                }

                m_cards[n] = m_cards[i];
                m_cards[i] = c;
                i++;
            }
        }
    }
}