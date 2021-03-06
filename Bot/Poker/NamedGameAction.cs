﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Poker
{
    public class NamedGameAction : GameAction
    {
        public string Name { get; set; }

        public bool IsRaise { get; set; }

        public NamedGameAction(string name, GameActionType type, int amount, bool isRaise) : base(type, amount)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            IsRaise = isRaise;
        }

        public NamedGameAction(string name, GameActionType type) : this(name, type, 0, false)
        {
        }
    }
}