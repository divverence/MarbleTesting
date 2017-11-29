using System;
using System.Collections.Generic;
using System.Linq;

namespace Divverence.MarbleTesting
{
    public static class MarbleParser
    {
        private enum ParsingState
        {
            NotInGroup,
            InOrderedGroup,
            InUnorderedGroup
        }

        public static IEnumerable<Moment> ParseSequence(string sequence)
        {
            if (sequence == null)
                throw new ArgumentNullException(nameof(sequence));
            int? timeOffset;
            var moments = ParseSequence(sequence, out timeOffset);
            if (!timeOffset.HasValue || timeOffset.Value == 0)
                return moments;
            return moments.Select(m => new Moment(m.Time + timeOffset.Value, m.Marbles, m.IsOrderedGroup));
        }

        private static IEnumerable<Moment> ParseSequence(string sequence, out int? timeOffset)
        {
            var retVal = new List<Moment>();
            var time = 0;
            int groupTime = 0;
            timeOffset = null;
            List<string> groupMarbles = null;
            List<string> unOrderedGroupMarbles = null;
            var parsingState = ParsingState.NotInGroup;
            foreach (var character in sequence)
            {
                switch (parsingState)
                {
                    case ParsingState.NotInGroup:
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
                                parsingState = ParsingState.InOrderedGroup;
                                groupMarbles = new List<string>();
                                break;
                            case '<':
                                groupTime = time;
                                parsingState = ParsingState.InUnorderedGroup;
                                unOrderedGroupMarbles = new List<string>();
                                break;
                            case ')':
                                throw new ArgumentException("Closing parentheses without opening parentheses",
                                    nameof(sequence));
                            case '>':
                                throw new ArgumentException("Closing brace without opening brace",
                                    nameof(sequence));
                            default:
                                retVal.Add(new Moment(time, character.ToString()));
                                break;
                        }
                        break;
                    case ParsingState.InUnorderedGroup:
                        switch (character)
                        {
                            case ' ':
                                throw new ArgumentException("Cannot use <space> within a group", nameof(sequence));
                            case '-':
                                throw new ArgumentException("Cannot use - within a group", nameof(sequence));
                            case '^':
                                if (timeOffset.HasValue)
                                    throw new ArgumentException("Only one ^ character allowed", nameof(sequence));
                                timeOffset = -groupTime;
                                unOrderedGroupMarbles.Add(character.ToString());
                                break;
                            case '>':
                                if (unOrderedGroupMarbles.Count <= 1)
                                {
                                    throw new ArgumentException("Only groups with multiple marbles are allowed", nameof(sequence));
                                }
                                retVal.Add(new Moment(groupTime, unOrderedGroupMarbles.ToArray(), false));
                                unOrderedGroupMarbles = null;
                                parsingState = ParsingState.NotInGroup;
                                break;
                            case '<':
                                throw new ArgumentException("Cannot have braces nested in an unordered group", nameof(sequence));
                            case '(':
                                throw new ArgumentException("Cannot have parentheses nested in an unordered group", nameof(sequence));
                            case ')':
                                throw new ArgumentException("Cannot have parentheses nested in an unordered group", nameof(sequence));
                            default:
                                unOrderedGroupMarbles.Add(character.ToString());
                                break;
                        }
                        break;
                    case ParsingState.InOrderedGroup:
                        switch (character)
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
                                if (groupMarbles.Count <= 1)
                                {
                                    throw new ArgumentException("Only groups with multiple marbles are allowed", nameof(sequence));
                                }
                                retVal.Add(new Moment(groupTime, groupMarbles.ToArray()));
                                groupMarbles = null;
                                parsingState = ParsingState.NotInGroup;
                                break;
                            case '<':
                                throw new ArgumentException("Cannot have braces nested in an ordered group", nameof(sequence));
                            case '>':
                                throw new ArgumentException("Cannot have braces nested in an ordered group", nameof(sequence));
                            default:
                                groupMarbles.Add(character.ToString());
                                break;
                        }
                        break;
                }
                time++;
            }
            if (parsingState == ParsingState.InOrderedGroup)
                throw new ArgumentException("Opening parentheses without closing parentheses", nameof(sequence));
            if (parsingState == ParsingState.InUnorderedGroup)
                throw new ArgumentException("Opening brace without closing brace", nameof(sequence));
            return retVal;
        }
    }
}