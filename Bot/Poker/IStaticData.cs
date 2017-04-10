using System;
using System.Collections.Generic;

namespace Poker
{
    public interface IStaticData
    {
        IReadOnlyList<double> AveragePreflopPredictionVector { get; }
        IReadOnlyList<double> AverageFlopPredictionVector { get; }
        IReadOnlyList<Tuple<HandClass, double>> EvenWeights { get; }
        IReadOnlyList<HandClass> AllPossibleHands { get; }
        IReadOnlyDictionary<HandClass, IReadOnlyList<Card[]>> HandClassExpansions { get; }
    }
}