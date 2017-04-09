using Poker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandCruncher
{
    public class CardParser
    {
        public static Card MakeCard(string str)
        {
            return new Card(CharToSuit(str[1]), CharToFace(str[0]));
        }

        public static Suit CharToSuit(char c)
        {
            c = c.ToString().ToLower()[0];

            switch (c)
            {
                case 'c': return Suit.Clubs;
                case 's': return Suit.Spades;
                case 'h': return Suit.Hearts;
                case 'd': return Suit.Diamonds;
            }

            return Suit.Clubs;
        }

        public static Face CharToFace(char c)
        {
            c = c.ToString().ToLower()[0];

            switch (c)
            {
                case 'a': return Face.Ace;
                case '2': return Face.Two;
                case '3': return Face.Three;
                case '4': return Face.Four;
                case '5': return Face.Five;
                case '6': return Face.Six;
                case '7': return Face.Seven;
                case '8': return Face.Eight;
                case '9': return Face.Nine;
                case 't': return Face.Ten;
                case 'j': return Face.Jack;
                case 'q': return Face.Queen;
                case 'k': return Face.King;
            }

            return Face.Ace;
        }
    }
}