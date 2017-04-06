using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Poker
{
    public interface ISimulator
    {
        double Simulate(Card a, Card b, IEnumerable<Card> boardCards, IReadOnlyList<IReadOnlyList<Tuple<HandClass, double>>> opponentWeights, int sampleCount);
    }
}
