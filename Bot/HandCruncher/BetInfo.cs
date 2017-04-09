using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandCruncher
{
    public class BetInfo
    {
        public string Name { get; private set; }
        public int Amount { get; private set; }

        public BetInfo(string name, int amount)
        {
            Name = name ?? string.Empty;
            Amount = amount;
        }
    }
}