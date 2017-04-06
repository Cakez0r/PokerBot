using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Poker
{
    public interface IHandEvaluator
    {
        HandEvaluation Evaluate(IEnumerable<Card> cards);
        double EvaluateScore(IEnumerable<Card> cards);
    }
}
