using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Poker
{
    [Serializable]
    public struct HandClass
    {
        public Face A { get; set; }

        public Face B { get; set; }

        public bool Suited { get; set; }

        public static HandClass FromCards(Card a, Card b)
        {
            Card lowest = default(Card);
            Card highest = default(Card);

            if ((int)a.Face > (int)b.Face)
            {
                lowest = b;
                highest = a;
            }
            else
            {
                lowest = a;
                highest = b;
            }

            return new HandClass()
            {
                A = highest.Face,
                B = lowest.Face,
                Suited = highest.Suit == lowest.Suit
            };
        }

        public IList<Card[]> Expand()
        {
            List<Card[]> hands = new List<Card[]>(4);
            if (A == B)
            {
                hands.Add(new Card[2]
                {
                    new Card(Suit.Clubs, A),
                    new Card(Suit.Diamonds, B),
                });

                hands.Add(new Card[2]
                {
                    new Card(Suit.Clubs, A),
                    new Card(Suit.Hearts, B),
                });

                hands.Add(new Card[2]
                {
                    new Card(Suit.Clubs, A),
                    new Card(Suit.Spades, B),
                });

                hands.Add(new Card[2]
                {
                    new Card(Suit.Diamonds, A),
                    new Card(Suit.Hearts, B),
                });

                hands.Add(new Card[2]
                {
                    new Card(Suit.Diamonds, A),
                    new Card(Suit.Spades, B),
                });

                hands.Add(new Card[2]
                {
                    new Card(Suit.Hearts, A),
                    new Card(Suit.Spades, B),
                });
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    Suit s1 = (Suit)i;

                    if (Suited)
                    {
                        hands.Add(new Card[2]
                        {
                            new Card(s1, A),
                            new Card(s1, B),
                        });
                    }
                    else
                    {
                        for (int j = 0; j < 4; j++)
                        {
                            if (i == j)
                            {
                                continue;
                            }

                            Suit s2 = (Suit)j;
                            hands.Add(new Card[2]
                            {
                                new Card(s1, A),
                                new Card(s2, B),
                            });
                        }
                    }
                }
            }

            return hands;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            if ((int)A < 10)
            {
                sb.Append(((int)A).ToString());
            }
            else
            {
                sb.Append(A.ToString()[0]);
            }

            sb.Append(" ");

            if ((int)B < 10)
            {
                sb.Append(((int)B).ToString());
            }
            else
            {
                sb.Append(B.ToString()[0]);
            }


            sb.Append(" ");

            sb.Append(Suited ? "Suited" : "Off-suit");

            return sb.ToString();
        }
    }

}
