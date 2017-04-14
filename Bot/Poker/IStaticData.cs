using System;
using System.Collections.Generic;

namespace Poker
{
    public interface IStaticData
    {
        IReadOnlyDictionary<HandState, IReadOnlyList<double>> AveragePredictionVectors { get; }
        IReadOnlyList<Tuple<HandClass, double>> EvenWeights { get; }
        IReadOnlyList<HandClass> AllPossibleHands { get; }
        IReadOnlyDictionary<HandClass, IReadOnlyList<Card[]>> HandClassExpansions { get; }
        IReadOnlyList<double> BetRamp { get; }
    }
}