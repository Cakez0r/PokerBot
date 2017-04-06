using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Poker
{
    public class Simulator : ISimulator
    {
        private IHandEvaluator m_evaluator;

        public Simulator(IHandEvaluator evaluator)
        {
            m_evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
        }

        private Card[] SelectRandomHand(IReadOnlyList<Tuple<HandClass, double>> weights, ISet<Card> usedCards)
        {
            Card[] result = null;
            while (result == null)
            {
                var cls = GlobalRandom.WeightedRandom(weights);
                result = ExpandRandom(cls, usedCards);
            }

            return result;
        }

        private Card[] ExpandRandom(HandClass c, ISet<Card> usedCards)
        {
            IList<Card[]> possibilities = c.Expand();

            for (int i = possibilities.Count - 1; i >= 0; i--)
            {
                if (usedCards.Contains(possibilities[i][0]) || usedCards.Contains(possibilities[i][1]))
                {
                    possibilities.RemoveAt(i);
                }
            }

            if (possibilities.Count == 0)
            {
                return null;
            }

            return possibilities[GlobalRandom.Next(possibilities.Count)];
        }

        public double Simulate(Card a, Card b, IEnumerable<Card> boardCards, IReadOnlyList<IReadOnlyList<Tuple<HandClass, double>>> opponentWeights, int sampleCount)
        {
            int wins = 0;

            Parallel.For(0, sampleCount, (n) =>
            {
                Card[] board = new Card[5];

                int dealsNeeded = 5;

                foreach (var c in boardCards)
                {
                    board[5 - dealsNeeded] = c;
                    dealsNeeded--;
                }

                HashSet<Card> usedCards = new HashSet<Card>();
                Card[] hand = new Card[] { a, b };
                usedCards.Add(a);
                usedCards.Add(b);

                foreach (var c in boardCards)
                {
                    usedCards.Add(c);
                }

                List<Card[]> oppponents = new List<Card[]>(opponentWeights.Count);
                int randomOffset = GlobalRandom.Next();
                for (int i = 0; i < opponentWeights.Count; i++)
                {
                    var oh = SelectRandomHand(opponentWeights[(i + randomOffset) % opponentWeights.Count], usedCards);
                    usedCards.Add(oh[0]);
                    usedCards.Add(oh[1]);
                    oppponents.Add(oh);
                }

                Deck deck = new Deck();
                deck.Rig(hand.Concat(boardCards).Concat(oppponents.SelectMany(c => c)));
                for (int i = 0; i < boardCards.Count() + 2 + (opponentWeights.Count * 2); i++)
                {
                    deck.Deal();
                }

                int ofs = 5 - dealsNeeded;
                for (int i = 0; i < dealsNeeded; i++)
                {
                    board[ofs + i] = deck.Deal();
                }

                var myScore = m_evaluator.EvaluateScore(board.Concat(hand));

                bool lost = false;
                for (int i = 0; i < oppponents.Count; i++)
                {
                    var opponentScore = m_evaluator.EvaluateScore(board.Concat(oppponents[i]));
                    if (opponentScore > myScore)
                    {
                        lost = true;
                        break;
                    }
                }

                if (!lost)
                {
                    Interlocked.Increment(ref wins);
                }
            });

            return (double)wins / sampleCount;
        }
    }
}