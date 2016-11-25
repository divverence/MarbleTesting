using System;
using System.Collections.Generic;
using System.Linq;

namespace Divverence.MarbleTesting
{
    public static class MarbleParser
    {
        public static IEnumerable<Moment> ParseMarbles(string line)
        {
            if (line == null)
                throw new ArgumentNullException(nameof(line));
            int? timeOffset;
            var moments = ParseMarbles(line, out timeOffset);
            if (!timeOffset.HasValue || timeOffset.Value == 0)
                return moments;
            return moments.Select(m => new Moment(m.Time + timeOffset.Value, m.Marbles));
        }

        public static List<Moment> ParseMarbles(string line, out int? timeOffset)
        {
            var retVal = new List<Moment>();
            var time = 0;
            int groupTime = 0;
            timeOffset = null;
            List<string> groupMarbles = null;
            foreach (var character in line)
            {
                if (groupMarbles == null)
                switch (character)
                {
                    case ' ':
                        continue;
                    case '-':
                        retVal.Add(new Moment(time));
                        break;
                    case '^':
                        if (timeOffset.HasValue)
                            throw new ArgumentException("Only one ^ character allowed", nameof(line));
                        timeOffset = -time;
                        retVal.Add(new Moment(time, character.ToString()));
                        break;
                    case '(':
                        groupTime = time;
                        groupMarbles = new List<string>();
                        break;
                    case ')':
                        throw new ArgumentException("Closing parentheses without opening parentheses", nameof(line));
                    default:
                        retVal.Add(new Moment(time, character.ToString()));
                        break;
                }
                else switch (character)
                {
                    case ' ':
                        continue;
                    case '-':
                            throw new ArgumentException("Cannot use - within a group", nameof(line));
                    case '^':
                        if (timeOffset.HasValue)
                            throw new ArgumentException("Only one ^ character allowed", nameof(line));
                        timeOffset = -groupTime;
                        groupMarbles.Add(character.ToString());
                        break;
                    case '(':
                        throw new ArgumentException("Cannot have nested parentheses", nameof(line));
                    case ')':
                        retVal.Add(new Moment(groupTime, groupMarbles.ToArray()));
                        groupMarbles = null;
                        break;
                    default:
                        groupMarbles.Add(character.ToString());
                        break;
                }

                time++;
            }
            return retVal;
        }
    }
}