using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandCruncher
{
    public class HistoryParserException : Exception
    {
        public HistoryParserException(string message) : base(message)
        {
        }
    }
}