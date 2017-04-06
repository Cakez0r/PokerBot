using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Poker
{
    public class Card : IEquatable<Card>, IEqualityComparer<Card>
    {
        private static readonly IReadOnlyList<Card> s_cards = null;
        private static readonly IReadOnlyDictionary<ulong, Card> s_cardBitmaps = null;

        public Suit Suit { get; private set; }
        public Face Face { get; private set; }

        static Card()
        {
            int i = 0;
            Dictionary<ulong, Card> bitmaps = new Dictionary<ulong, Card>();
            Card[] cards = new Card[52];
            for (int f = 2; f < 15; f++)
            {
                for (int s = 0; s < 4; s++)
                {
                    var c = new Card((Suit)s, (Face)f);
                    var idx = c.GetHashCode();
                    cards[idx] = c;
                    bitmaps[1ul << i] = cards[i];
                    i++;
                }
            }

            s_cards = cards;
            s_cardBitmaps = bitmaps;
        }

        public Card()
        {
        }

        public Card(Suit suit, Face face)
        {
            Suit = suit;
            Face = face;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            if ((int)Face < 10)
            {
                sb.Append(((int)Face).ToString());
            }
            else
            {
                sb.Append(Face.ToString()[0]);
            }

            switch (Suit)
            {
                case Suit.Clubs:
                    sb.Append("♣");
                    break;

                case Suit.Diamonds:
                    sb.Append("♦");
                    break;

                case Suit.Hearts:
                    sb.Append("♥");
                    break;

                case Suit.Spades:
                    sb.Append("♠");
                    break;
            }

            return sb.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong MakeHandBitmap(IEnumerable<Card> cards)
        {
            ulong bm = 0;

            foreach (var c in cards)
            {
                bm = bm | ToBitmap(c);
            }

            return bm;
        }

        public bool Equals(Card other)
        {
            return Equals(this, other);
        }

        public bool Equals(Card x, Card y)
        {
            return ToIndex(x) == ToIndex(y);
        }

        public override bool Equals(object obj)
        {
            var item = obj as Card;

            if (item == null)
            {
                return false;
            }

            return Equals(item);
        }

        public static Card FromIndex(int i)
        {
            return s_cards[i];
        }

        public static Card FromBitmap(ulong bm)
        {
            return s_cardBitmaps[bm];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ToBitmap(Card c)
        {
            return 1ul << ToIndex(c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToIndex(Card c)
        {
            return (int)c.Suit + (((int)c.Face - 2) * 4);
        }

        public int GetHashCode(Card obj)
        {
            return ToIndex(obj);
        }

        public override int GetHashCode()
        {
            return ToIndex(this);
        }
    }
}