using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Poker
{
    public class MockEvaluator : IHandEvaluator
    {
        public HandEvaluation Evaluate(IEnumerable<Card> cards)
        {
            return new HandEvaluation(new List<Card>(), new List<Card>(), 0, HandType.HighCard);
        }

        public double EvaluateScore(IEnumerable<Card> cards)
        {
            return 0;
        }
    }
}