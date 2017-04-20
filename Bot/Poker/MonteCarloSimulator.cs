using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Poker
{
    public class MonteCarloSimulator : ISimulator
    {
        private IHandEvaluator m_evaluator;
        private IStaticData m_staticData;

        public bool UseRandomOffset { get; set; } = true;

        public MonteCarloSimulator(IHandEvaluator evaluator, IStaticData staticData)
        {
            m_evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
            m_staticData = staticData ?? throw new ArgumentNullException(nameof(staticData));
        }

        private Card[] SelectRandomHand(IReadOnlyList<Tuple<HandClass, double>> weights, ulong usedCards)
        {
            Card[] result = null;
            while (result == null)
            {
                var cls = GlobalRandom.WeightedRandom(weights);
                result = ExpandRandom(cls, usedCards);
            }

            return result;
        }

        private Card[] ExpandRandom(HandClass c, ulong usedCards)
        {
            IReadOnlyList<Card[]> expansions = m_staticData.HandClassExpansions[c];
            int ofs = GlobalRandom.Next(expansions.Count);
            for (int i = 0; i < expansions.Count; i++)
            {
                var idx = (i + ofs) % expansions.Count;
                var hand = expansions[idx];
                var bm = Card.MakeHandBitmap(hand);
                if ((bm & usedCards) == 0)
                {
                    return hand;
                }
            }

            return null;
        }

        public double Simulate(Card a, Card b, IEnumerable<Card> boardCards, IReadOnlyList<IReadOnlyList<Tuple<HandClass, double>>> opponentWeights, int sampleCount)
        {
            int wins = 0;

            Dictionary<HandClass, int> losses = new Dictionary<HandClass, int>();
            Parallel.For(0, sampleCount, (n) =>
            {
                Card[] board = new Card[5];

                int dealsNeeded = 5;

                foreach (var c in boardCards)
                {
                    board[5 - dealsNeeded] = c;
                    dealsNeeded--;
                }

                ulong usedCards = 0;
                Card[] hand = new Card[] { a, b };
                usedCards |= Card.ToBitmap(a);
                usedCards |= Card.ToBitmap(b);

                foreach (var c in boardCards)
                {
                    usedCards |= Card.ToBitmap(c);
                }

                List<Card[]> oppponents = new List<Card[]>(opponentWeights.Count);
                int randomOffset = UseRandomOffset ? GlobalRandom.Next() : 0;
                for (int i = 0; i < opponentWeights.Count; i++)
                {
                    var oh = SelectRandomHand(opponentWeights[(i + randomOffset) % opponentWeights.Count], usedCards);
                    usedCards |= Card.ToBitmap(oh[0]);
                    usedCards |= Card.ToBitmap(oh[1]);
                    oppponents.Add(oh);
                }

                List<Card> riggedCards = hand.Concat(boardCards).Concat(oppponents.SelectMany(c => c)).ToList();
                IDeck deck = new RiggedDeck(riggedCards);
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
                Card[] oppHand = null;
                for (int i = 0; i < oppponents.Count; i++)
                {
                    var opponentScore = m_evaluator.EvaluateScore(board.Concat(oppponents[i]));
                    if (opponentScore > myScore)
                    {
                        oppHand = oppponents[i];
                        lost = true;
                        break;
                    }
                }

                if (!lost)
                {
                    Interlocked.Increment(ref wins);
                }
                else
                {
                    HandClass oc = HandClass.FromCards(oppHand[0], oppHand[1]);
                    lock (losses)
                    {
                        if (!losses.ContainsKey(oc))
                        {
                            losses[oc] = 0;
                        }
                        losses[oc]++;
                    }
                }
            });

            foreach (var l in losses.OrderByDescending(kv => kv.Value).Take(10))
            {
                //Console.WriteLine("Lose to {0}", l.Key);
            }

            return (double)wins / sampleCount;
        }

        public double SimulateActual(Card a, Card b, IReadOnlyList<Card[]> opponents, IReadOnlyList<Card> board, int sampleCount)
        {
            int wins = 0;
            Card[] hand = new Card[] { a, b };

            Parallel.For(0, sampleCount, (n) =>
            {
                Card[] newBoard = new Card[5];
                for (int i = 0; i < board.Count; i++)
                {
                    newBoard[i] = board[i];
                }

                List<Card> riggedCards = hand.Concat(board).Concat(opponents.SelectMany(c => c)).ToList();
                IDeck deck = new RiggedDeck(riggedCards);
                for (int i = 0; i < riggedCards.Count; i++)
                {
                    deck.Deal();
                }

                for (int i = 0; i < 5 - board.Count; i++)
                {
                    newBoard[4 - i] = deck.Deal();
                }

                var myScore = m_evaluator.EvaluateScore(newBoard.Concat(hand));

                bool lost = false;
                for (int i = 0; i < opponents.Count; i++)
                {
                    var opponentScore = m_evaluator.EvaluateScore(newBoard.Concat(opponents[i]));
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