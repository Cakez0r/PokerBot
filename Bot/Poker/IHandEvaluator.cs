using System.Collections.Generic;

namespace Poker
{
    public interface IHandEvaluator
    {
        HandEvaluation Evaluate(IEnumerable<Card> cards);

        double EvaluateScore(IEnumerable<Card> cards);
    }
}