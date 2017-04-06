using System.Collections.Generic;

namespace Poker
{
    public class HandEvaluation
    {
        public double Score { get; private set; }
        public IList<Card> Kickers { get; private set; }
        public IList<Card> Active { get; private set; }
        public HandType Type { get; private set; }

        public HandEvaluation(IList<Card> active, IList<Card> kickers, double score, HandType type)
        {
            Active = active;
            Kickers = kickers;
            Score = score;
            Type = type;
        }
    }
}