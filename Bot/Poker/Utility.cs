using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Poker
{
    public static class Utility
    {
        public static string DoubleToPct(double d)
        {
            return string.Format("{0:0.##}", d * 100);
        }
    }
}