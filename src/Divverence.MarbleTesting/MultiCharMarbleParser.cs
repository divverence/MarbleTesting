using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Divverence.MarbleTesting
{
    public static class MultiCharMarbleParser
    {
        public static IEnumerable<Moment> ParseSequence(string sequence)
        {
            if (sequence == null)
                throw new ArgumentNullException(nameof(sequence));
            int? timeOffset;
            var moments = ParseSequence(sequence, out timeOffset);
            if (!timeOffset.HasValue || timeOffset.Value == 0)
                return moments;
            return moments.Select(m => new Moment(m.Time + timeOffset.Value, m.Marbles));
        }

        private static IEnumerable<Moment> ParseSequence(string sequence, out int? timeOffset)
        {
            var retVal = new List<Moment>();
            var time = 0;
            int groupTime = 0;
            timeOffset = null;
            List<string> groupMarbles = null;
            int marbleTime = 0;
            StringBuilder marbleBuilder = new StringBuilder();
            foreach (var character in sequence.Trim())
            {
                if (groupMarbles == null)
                    switch (character)
                    {
                        case ' ':
                        case '-':
                            FlushMarble(marbleBuilder, retVal, marbleTime, time);
                            retVal.Add(new Moment(time));
                            break;
                        case '^':
                            FlushMarble(marbleBuilder, retVal, marbleTime, time);
                            if (timeOffset.HasValue)
                                throw new ArgumentException("Only one ^ character allowed", nameof(sequence));
                            timeOffset = -time;
                            retVal.Add(new Moment(time, character.ToString()));
                            break;
                        case '(':
                            FlushMarble(marbleBuilder, retVal, marbleTime, time);
                            groupTime = time;
                            groupMarbles = new List<string>();
                            break;
                        case ')':
                            throw new ArgumentException("Closing parentheses without opening parentheses",
                                nameof(sequence));
                        case ',':
                            throw new ArgumentException("Comma should not occur outside of a group",
                                nameof(sequence));
                        default:
                            GrowMarble(marbleBuilder, ref marbleTime, time, character);
                            break;
                    }
                else
                    switch (character)
                    {
                        case ',':
                        case ' ':
                            FlushMarbleToGroup(marbleBuilder, groupMarbles);
                            break;
                        case '-':
                            throw new ArgumentException("Cannot use - within a group", nameof(sequence));
                        case '^':
                            if (timeOffset.HasValue)
                                throw new ArgumentException("Only one ^ character allowed", nameof(sequence));
                            FlushMarbleToGroup(marbleBuilder, groupMarbles);
                            timeOffset = -groupTime;
                            groupMarbles.Add(character.ToString());
                            break;
                        case '(':
                            throw new ArgumentException("Cannot have nested parentheses", nameof(sequence));
                        case ')':
                            FlushMarbleToGroup(marbleBuilder, groupMarbles);
                            retVal.Add(new Moment(groupTime, groupMarbles.ToArray()));
                            groupMarbles = null;
                            AddEmptyMoments(retVal, time + 1);
                            break;
                        default:
                            GrowMarble(marbleBuilder, ref marbleTime, groupTime, character);
                            break;
                    }

                time++;
            }
            FlushMarble(marbleBuilder, retVal, marbleTime, time);
            if (groupMarbles != null)
                throw new ArgumentException("Opening parentheses without closing parentheses", nameof(sequence));
            return retVal;
        }

        private static void GrowMarble(StringBuilder marbleBuilder, ref int marbleTime, int time, char character)
        {
            if (marbleBuilder.Length == 0)
                marbleTime = time;
            marbleBuilder.Append(character);
        }

        private static void FlushMarbleToGroup(StringBuilder marbleBuilder, List<string> groupMarbles)
        {
            if (marbleBuilder.Length <= 0)
                return;
            var marble = marbleBuilder.ToString();
            marbleBuilder.Clear();
            groupMarbles.Add(marble);
        }

        private static void FlushMarble(StringBuilder marbleBuilder, List<Moment> retVal, int marbleTime, int time)
        {
            if (marbleBuilder.Length <= 0)
                return;
            var marble = marbleBuilder.ToString();
            marbleBuilder.Clear();

            retVal.Add(new Moment(marbleTime, marble));

            AddEmptyMoments(retVal, time);
        }

        private static void AddEmptyMoments(List<Moment> retVal, int time)
        {
            for (int emptyTime = retVal.Last().Time + 1; emptyTime < time; emptyTime++)
                retVal.Add(new Moment(emptyTime));
        }
    }
}