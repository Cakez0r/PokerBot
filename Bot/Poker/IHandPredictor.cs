using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Poker
{
    public interface IHandPredictor
    {
        IReadOnlyList<Tuple<HandClass, double>> Estimate(IReadOnlyList<double> predictionVector);
    }
}
