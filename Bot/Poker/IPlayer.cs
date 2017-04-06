using System.Collections.Generic;

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