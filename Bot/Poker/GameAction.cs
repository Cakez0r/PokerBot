namespace Poker
{
    public class GameAction
    {
        public GameActionType Type { get; private set; }
        public int Amount { get; private set; }

        public GameAction(GameActionType type) : this(type, 0)
        {
        }

        public GameAction(GameActionType type, int amount)
        {
            if (amount < 0)
            {
                throw new GameRuleException(GameRuleExceptionType.InvalidAction);
            }

            if (amount == 0 && type == GameActionType.Bet)
            {
                throw new GameRuleException(GameRuleExceptionType.InvalidAction);
            }

            Type = type;
            Amount = amount;
        }
    }
}