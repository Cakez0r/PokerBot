using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Poker
{
    public interface IStaticData
    {
        IReadOnlyList<double> AveragePredictionVector { get; }
        IReadOnlyList<Tuple<HandClass, double>> EvenWeights { get; }
        IReadOnlyList<HandClass> AllPossibleHands { get; }
    }
}
