using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
