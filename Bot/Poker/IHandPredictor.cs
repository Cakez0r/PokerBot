using System;
using System.Collections.Generic;

namespace Poker
{
    public interface IHandPredictor
    {
        IReadOnlyList<Tuple<HandClass, double>> Estimate(IReadOnlyList<double> predictionVector);
    }
}