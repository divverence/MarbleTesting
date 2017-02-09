using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Divverence.MarbleTesting
{
    public class ExpectedMarbles
    {
        public ExpectedMarbles(string sequence, IEnumerable<ExpectedMarble> expectations)
        {
            Sequence = sequence;
            Expectations = expectations.ToImmutableList();
            FirstTime = Expectations.Min(e => e.Time);
            LastTime = Expectations.Max(e => e.Time);
        }

        public string Sequence { get; }
        public ImmutableList<ExpectedMarble> Expectations { get; }
        public int LastTime { get; set; }
        public int FirstTime { get; set; }

        public bool Verify(int time)
        {
            var expectations = Expectations.Where(e => e.Time == time).ToList();
            if (!expectations.Any())
                return false;

            foreach (var exp in expectations)
                try
                {
                    exp.Assertion();
                }
                catch (Exception e)
                {
                    if (exp.Marble == null)
                        throw new Exception(
                            $"Unexpected event received at time {time} on sequence {ErrorMessageHelper.SequenceWithPointerToOffendingMoment(Sequence, time)}", e);
                    throw new Exception(
                        $"Marble '{exp.Marble}' not received at time {time} on sequence {ErrorMessageHelper.SequenceWithPointerToOffendingMoment(Sequence, time)}", e);
                }
            return true;
        }
    }
}