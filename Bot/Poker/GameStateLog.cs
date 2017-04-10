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

        public IList<string> GetPreflopActOrder()
        {
            // Doesn't hand case when bb is all in
            List<string> actOrder = new List<string>();
            var orderedSeats = Seats.OrderBy(kv => kv.Value).ToList();
            int n = 0;
            foreach (var seat in orderedSeats.Concat(orderedSeats).Concat(orderedSeats).SkipWhile(kv => kv.Value < DealerSeat).Skip(3))
            {
                actOrder.Add(seat.Key);
                n++;
                if (n == Seats.Count)
                {
                    break;
                }
            }

            return actOrder;
        }

        public IList<string> GetFlopActOrder()
        {
            List<string> actOrder = new List<string>();
            var orderedSeats = Seats.OrderBy(kv => kv.Value).ToList();
            int n = 0;
            foreach (var seat in orderedSeats.Concat(orderedSeats).Concat(orderedSeats).SkipWhile(kv => kv.Value < DealerSeat).Skip(1))
            {
                if (!PreflopActions.Any(a => a.Name == seat.Key && a.Type == GameActionType.Fold))
                {
                    actOrder.Add(seat.Key);
                }

                n++;
                if (n == Seats.Count)
                {
                    break;
                }
            }

            return actOrder;
        }

        public double[] MakeVector(string id, HandState state)
        {
            int preflopBetsBefore = 0;
            int preflopActsBefore = 0;
            int preflopNumActorsAfter = 0;

            var preflopActOrder = GetPreflopActOrder();
            preflopNumActorsAfter = preflopActOrder.SkipWhile(s => s != id).Count();
            foreach (var action in PreflopActions.TakeWhile(a => a.Name != id))
            {
                preflopActsBefore++;
                if (action.Type == GameActionType.Bet)
                {
                    preflopBetsBefore++;
                }
            }

            double preflopBet = (double)PreflopActions.Where(a => a.Name == id).Sum(a => a.Amount);
            double preflopPot = (double)PreflopActions.Sum(a => a.Amount) + SmallBlind + BigBlind;
            double avgStack = StartBalances.Values.Average();

            double[] preflopVector = new double[15]
            {
                (double)preflopNumActorsAfter / (preflopActOrder.Count - 1),
                preflopBet / BigBlind,
                preflopBet / StartBalances[id],
                preflopBet / (preflopPot - preflopBet),
                preflopBet / avgStack,
                preflopBetsBefore,
                preflopNumActorsAfter,
                StartBalances[id] / avgStack,
                PreflopActions.Count(a => a.Name == id),
                PreflopActions.Count(a => a.Name == id && a.Type == GameActionType.Check),
                PreflopActions.Count(a => a.Name == id && a.Type == GameActionType.Bet && !a.IsRaise),
                PreflopActions.Count(a => a.Name == id && a.Type == GameActionType.Bet && a.IsRaise),
                (preflopPot - preflopBet) / BigBlind,
                Seats.Concat(Seats).SkipWhile(kv => kv.Value != DealerSeat).Skip(2).First().Key == id ? 1 : 0,
                Seats.Concat(Seats).SkipWhile(kv => kv.Value != DealerSeat).Skip(1).First().Key == id ? 1 : 0
            };

            if (state == HandState.Preflop)
            {
                return preflopVector;
            }

            int flopBetsBefore = 0;
            int flopActsBefore = 0;
            int flopNumActorsAfter = 0;

            var flopActOrder = GetFlopActOrder();
            flopNumActorsAfter = flopActOrder.SkipWhile(s => s != id).Count();
            foreach (var action in FlopActions.TakeWhile(a => a.Name != id))
            {
                flopActsBefore++;
                if (action.Type == GameActionType.Bet)
                {
                    flopBetsBefore++;
                }
            }

            double flopBet = (double)FlopActions.Where(a => a.Name == id).Sum(a => a.Amount);
            double flopPot = (double)FlopActions.Sum(a => a.Amount);

            double[] flopVector = new double[26]
            {
                (double)flopNumActorsAfter / (flopActOrder.Count - 1),
                flopBet / BigBlind,
                flopBet / StartBalances[id],
                flopBet / (flopPot - flopBet),
                flopBet / avgStack,
                flopBetsBefore,
                flopNumActorsAfter,
                FlopActions.Count(a => a.Name == id),
                FlopActions.Count(a => a.Name == id && a.Type == GameActionType.Check),
                FlopActions.Count(a => a.Name == id && a.Type == GameActionType.Bet && !a.IsRaise),
                FlopActions.Count(a => a.Name == id && a.Type == GameActionType.Bet && a.IsRaise),
                (flopPot - flopBet) / BigBlind,

                BoardCards.Take(3).Count(c => c.Face == Face.Two),
                BoardCards.Take(3).Count(c => c.Face == Face.Three),
                BoardCards.Take(3).Count(c => c.Face == Face.Four),
                BoardCards.Take(3).Count(c => c.Face == Face.Five),
                BoardCards.Take(3).Count(c => c.Face == Face.Six),
                BoardCards.Take(3).Count(c => c.Face == Face.Seven),
                BoardCards.Take(3).Count(c => c.Face == Face.Eight),
                BoardCards.Take(3).Count(c => c.Face == Face.Nine),
                BoardCards.Take(3).Count(c => c.Face == Face.Ten),
                BoardCards.Take(3).Count(c => c.Face == Face.Jack),
                BoardCards.Take(3).Count(c => c.Face == Face.Queen),
                BoardCards.Take(3).Count(c => c.Face == Face.King),
                BoardCards.Take(3).Count(c => c.Face == Face.Ace),
                BoardCards.Take(3).GroupBy(c => c.Suit).Max(g => g.Count())
            };

            double[] combinedVector = new double[preflopVector.Length + flopVector.Length];
            Buffer.BlockCopy(preflopVector, 0, combinedVector, 0, preflopVector.Length * sizeof(double));
            Buffer.BlockCopy(flopVector, 0, combinedVector, preflopVector.Length * sizeof(double), flopVector.Length * sizeof(double));

            return combinedVector;
        }
    }
}