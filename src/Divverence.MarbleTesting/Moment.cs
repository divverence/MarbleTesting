using System;
using System.Collections.Generic;

namespace Divverence.MarbleTesting
{
    public class Moment
    {
        public enum MomentType
        {
            Empty,
            Single,
            OrderedGroup,
            UnorderedGroup
        }

        public static Moment Empty(int time) => new Moment(time, new string[0], MomentType.Empty);

        public static Moment Single(int time, string marble) => new Moment(time, new[] { marble }, MomentType.Single);

        public static Moment OrderedGroup(int time, string[] marbles) => new Moment(time, marbles, MomentType.OrderedGroup);

        public static Moment UnorderedGroup(int time, string[] marbles) => new Moment(time, marbles, MomentType.UnorderedGroup);

        private Moment(int time, string[] marbles, MomentType momentType)
        {
            if (marbles == null)
            {
                throw new ArgumentNullException(nameof(marbles));
            }

            if (momentType == MomentType.Empty && marbles.Length != 0)
            {
                throw MomentTypeMarbleCountMismatchException(marbles, momentType);
            }
            if (momentType == MomentType.Single && marbles.Length != 1)
            {
                throw MomentTypeMarbleCountMismatchException(marbles, momentType);
            }
            if ((momentType == MomentType.OrderedGroup || momentType == MomentType.UnorderedGroup) && marbles.Length <= 1)
            {
                throw MomentTypeMarbleCountMismatchException(marbles, momentType);
            }
            Time = time;
            Marbles = marbles;
            Type = momentType;
        }

        public int Time { get; }

        public string[] Marbles { get; }

        public MomentType Type { get; }

        public override string ToString()
        {
            if (Type == MomentType.Empty)
            {
                return "-";
            }
            if (Type == MomentType.Single)
            {
                return Marbles[0];
            }

            var spaceSeparated = string.Join(" ", Marbles);
            if (Type == MomentType.OrderedGroup)
            {
                return $"({spaceSeparated})";
            }
            return $"<{spaceSeparated}>";
        }

        public Moment TimeShiftedClone(int newTime) => new Moment(Time + newTime, Marbles, Type);

        private static ArgumentException MomentTypeMarbleCountMismatchException(IReadOnlyCollection<string> marbles, MomentType momentType)
        {
            return new ArgumentException($"MomentType '{momentType}' and number of marbles '{marbles.Count}' do not match");
        }
    }
}