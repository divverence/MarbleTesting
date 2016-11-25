using System.Collections.Generic;
using System.Linq;

namespace Divverence.MarbleTesting
{
    public static class MarbleParser
    {
        public static IEnumerable<Moment> ParseMarbles(string line) => line.Select(
            (time, character) => character == '-' ? new Moment(time) : new Moment(time, character.ToString()));
    }
}