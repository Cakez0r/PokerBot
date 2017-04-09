using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandCruncher
{
    public class SeatInfo
    {
        public int SeatNumber { get; private set; }
        public string Name { get; private set; }
        public int Balance { get; private set; }

        public SeatInfo(int seatNumber, string name, int balance)
        {
            SeatNumber = seatNumber;
            Name = name ?? string.Empty;
            Balance = balance;
        }
    }
}