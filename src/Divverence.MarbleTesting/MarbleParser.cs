﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Divverence.MarbleTesting
{
    public static class MarbleParser
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
            foreach (var character in sequence)
            {
                if (groupMarbles == null)
                switch (character)
                {
                    case ' ':
                    case '-':
                        retVal.Add(new Moment(time));
                        break;
                    case '^':
                        if (timeOffset.HasValue)
                            throw new ArgumentException("Only one ^ character allowed", nameof(sequence));
                        timeOffset = -time;
                        retVal.Add(new Moment(time, character.ToString()));
                        break;
                    case '(':
                        groupTime = time;
                        groupMarbles = new List<string>();
                        break;
                    case ')':
                        throw new ArgumentException("Closing parentheses without opening parentheses", nameof(sequence));
                    default:
                        retVal.Add(new Moment(time, character.ToString()));
                        break;
                }
                else switch (character)
                {
                    case ' ':
                            throw new ArgumentException("Cannot use <space> within a group", nameof(sequence));
                        case '-':
                            throw new ArgumentException("Cannot use - within a group", nameof(sequence));
                    case '^':
                        if (timeOffset.HasValue)
                            throw new ArgumentException("Only one ^ character allowed", nameof(sequence));
                        timeOffset = -groupTime;
                        groupMarbles.Add(character.ToString());
                        break;
                    case '(':
                        throw new ArgumentException("Cannot have nested parentheses", nameof(sequence));
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