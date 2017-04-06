﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Poker
{
    public enum GameRuleExceptionType
    {
        PlayerBankrupt,
        IllegalCheck,
        BetLessThanAmountToCall,
        RaiseLessThanMinRaise,
        InvalidAction
    }
}
