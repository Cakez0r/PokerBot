using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Poker
{
    public interface IPlayer
    {
        IReadOnlyList<Card> Hole { get; set; }

        int Balance { get; set; }

        GameAction Act(Game game, int contribution, int amountToCall, int minRaise);

        void OnPlayerActed(Game game, IPlayer player, GameAction action, int amountToCall);
    }
}
