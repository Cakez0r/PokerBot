using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Poker
{
    public class GameStateLog
    {
        public string GameId { get; set; }
        public string TableId { get; set; }
        public double SmallBlind { get; set; }
        public double BigBlind { get; set; }
        public IDictionary<string, double> StartBalances { get; private set; } = new Dictionary<string, double>();
        public IDictionary<string, int> Seats { get; private set; } = new Dictionary<string, int>();
        public int DealerSeat { get; set; }
        public IDictionary<string, Card[]> KnownHoleCards { get; private set; } = new Dictionary<string, Card[]>();
        public IList<Card> BoardCards { get; private set; } = new List<Card>();
        public IList<NamedGameAction> PreflopActions { get; private set; } = new List<NamedGameAction>();
        public IList<NamedGameAction> FlopActions { get; private set; } = new List<NamedGameAction>();
        public IList<NamedGameAction> TurnActions { get; private set; } = new List<NamedGameAction>();
        public IList<NamedGameAction> RiverActions { get; private set; } = new List<NamedGameAction>();
    }
}