using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Poker
{
    public class AlwaysCallPlayer : IPlayer
    {
        private static Logger s_log = LogManager.GetCurrentClassLogger();

        public IReadOnlyList<Card> Hole { get; set; } = new Card[2];

        public int Balance { get; set; }

        public string Name { get; set; }

        public AlwaysCallPlayer(int balance)
        {
            Balance = balance;
        }

        public GameAction Act(Game game, int contribution, int amountToCall, int minRaise)
        {
            s_log.Info("{0} has {1} {2}", Name, Hole[0], Hole[1]);
            return new GameAction(amountToCall == 0 ? GameActionType.Check : GameActionType.Bet, Math.Min(amountToCall, Balance));
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
