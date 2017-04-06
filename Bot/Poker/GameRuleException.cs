using System;

namespace Poker
{
    public class GameRuleException : Exception
    {
        public GameRuleExceptionType Type { get; private set; }

        public GameRuleException(GameRuleExceptionType type)
        {
            Type = type;
        }
    }
}