using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Poker
{
    public class Pot
    {
        public IDictionary<IPlayer, int> Contributions { get; private set; } = new Dictionary<IPlayer, int>();
        public ISet<IPlayer> EligibleWinners { get; private set; } = new HashSet<IPlayer>();

        public int Total
        {
            get { return Contributions.Values.Sum(); }
        }

    }
}
