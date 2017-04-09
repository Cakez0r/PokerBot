using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HandCruncher
{
    public class MatchInfo
    {
        public IReadOnlyList<string> Lines { get; private set; }
        public Match Match { get; private set; }
        public int MatchLineIndex { get; private set; }

        public MatchInfo(IReadOnlyList<string> lines, Match match, int matchLineIndex)
        {
            Lines = lines ?? throw new ArgumentNullException(nameof(lines));
            Match = match ?? throw new ArgumentNullException(nameof(match));
            if (matchLineIndex < 0 || matchLineIndex > lines.Count)
            {
                throw new ArgumentException("Index out of range", nameof(matchLineIndex));
            }
            MatchLineIndex = matchLineIndex;
        }
    }
}