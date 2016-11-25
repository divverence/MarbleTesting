using System.Collections.Generic;
using System.Linq;

namespace Divverence.MarbleTesting
{
    public static class MarbleParser
    {
        public static IEnumerable<Moment> ParseMarbles(string line) => line.Select(
            (character, time) => character == '-' ? new Moment(time) : new Moment(time, character.ToString()));
    }
}