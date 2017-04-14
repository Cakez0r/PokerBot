using System;
using System.Collections.Generic;

namespace Poker
{
    public interface ISimulator
    {
        double Simulate(Card a, Card b, IEnumerable<Card> boardCards, IReadOnlyList<IReadOnlyList<Tuple<HandClass, double>>> opponentWeights, int sampleCount);

        double SimulateActual(Card a, Card b, IReadOnlyList<Card[]> opponents, IReadOnlyList<Card> board, int sampleCount);
    }
}