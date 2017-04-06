using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Poker
{
    public class ConsoleInteractivePlayer : IPlayer
    {
        private static ILogger s_log = LogManager.GetCurrentClassLogger();

        public string Name { get; set; }

        public IReadOnlyList<Card> Hole { get; set; } = new Card[2];

        public int Balance { get; set; }

        public ConsoleInteractivePlayer(int balance)
        {
            Balance = balance;
        }

        public GameAction Act(Game game, int contribution, int amountToCall, int minRaise)
        {
            s_log.Info("You have {0} {1}.", Hole[0], Hole[1]);

            if (amountToCall == 0)
            {
                s_log.Info("   [1] Check");
            }
            else
            {
                s_log.Info("   [1] Call {0}", amountToCall);
            }
            s_log.Info("   [2] Raise (Min: {0})", minRaise);
            s_log.Info("   [3] Fold");

            int choice = 0;
            while (!int.TryParse(Console.ReadLine(), out choice))
            {
                Console.WriteLine("Invalid option");
            }

            switch (choice)
            {
                case 1:
                    return new GameAction(amountToCall == 0 ? GameActionType.Check : GameActionType.Bet, Math.Min(amountToCall, Balance));

                case 2:
                    Console.Write("Amount: ");
                    int amt = 0;
                    while (!int.TryParse(Console.ReadLine(), out amt))
                    {
                        Console.WriteLine("Invalid option");
                    }
                    return new GameAction(GameActionType.Bet, Math.Min(amt + amountToCall, Balance));

                case 3:
                    return new GameAction(GameActionType.Fold);
            }

            return new GameAction(GameActionType.Fold);
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
