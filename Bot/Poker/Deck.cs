using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Poker
{
    public class Deck
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
                m_cards[i] = s_allCards[i];
            }

            Shuffle();
        }

        public Card Deal()
        {
            int idx = Interlocked.Increment(ref m_ptr);
            return m_cards[idx-1];
        }

        private void Shuffle()
        {
            for (int i = 0; i < 51; i++)
            {
                int j = 0;
                j = GlobalRandom.Next(i, 52);
                Card t = m_cards[i];
                m_cards[i] = m_cards[j];
                m_cards[j] = t;
            }
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
