using System;
using System.Collections.Generic;

namespace Poker
{
    public interface IHandPredictor
    {
        IReadOnlyList<Tuple<HandClass, double>> Predict(Game game, IPlayer player);
    }
}