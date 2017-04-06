using NLog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Poker
{
    public class BasicAiPlayer : IPlayer
    {
        private static Logger s_log = LogManager.GetCurrentClassLogger();

        public IReadOnlyList<Card> Hole { get; set; } = new Card[2];

        public int Balance { get; set; }

        public string Name { get; set; }

        private IHandEvaluator m_evaluator;

        public BasicAiPlayer(IHandEvaluator evaluator)
        {
            m_evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
        }

        public GameAction Act(Game game, int contribution, int amountToCall, int minRaise)
        {
            var ct = HandClass.FromCards(Hole[0], Hole[1]);
            float wr = 0; // FIXME
            s_log.Info("{0} has {1} {2} (Win rate: {3})", Name, Hole[0], Hole[1], wr);

            if (wr > 0.65)
            {
                var eval = m_evaluator.Evaluate(Hole.Concat(game.Board));
                int handWorth = 0;
                if (game.State == HandState.Preflop)
                {
                    handWorth = (int)((Balance * 0.05) * wr);
                }
                else
                {
                    handWorth = (int)((eval.Score / 9) * Balance * 0.5);
                }

                int amt = handWorth - contribution;

                if (amt < amountToCall)
                {
                    return new GameAction(amountToCall == 0 ? GameActionType.Check : GameActionType.Fold);
                }

                if (amt > amountToCall)
                {
                    if (amt - amountToCall < minRaise)
                    {
                        amt = amountToCall;
                    }
                }

                return new GameAction(GameActionType.Bet, Math.Min(amt, Balance));
            }

            return new GameAction(amountToCall == 0 ? GameActionType.Check : GameActionType.Fold);
        }

        public override string ToString()
        {
            return Name;
        }

        public void OnPlayerActed(Game game, IPlayer player, GameAction action, int amountToCall)
        {
        }
    }
}