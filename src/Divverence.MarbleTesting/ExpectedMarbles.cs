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

        public void Verify(int time)
        {
            var expectations = Expectations.Where(e => e.Time == time).Where(e => e.Marble != null).ToList();
            if (!expectations.Any())
                return;

            foreach (var exp in expectations)
                try
                {
                    exp.Assertion();
                }
                catch (MissingEventException me)
                {
                    throw new Exception(
                        $"At time {time}, marble '{exp.Marble}' did not receive all expected events {ErrorMessageHelper.SequenceWithPointerToOffendingMoment(Sequence, time)}{Environment.NewLine}{me.Message}.", me);
                }
                catch (UnexpectedEventsException me)
                {
                    throw new Exception(
                        $"At time {time}, marble '{exp.Marble}' received unexpected events {ErrorMessageHelper.SequenceWithPointerToOffendingMoment(Sequence, time)}{Environment.NewLine}{me.Message}.", me);
                }

                catch (Exception e)
                {
                    throw new Exception(
                        $"Expected marble '{exp.Marble}' at time {time} was not satisfied {ErrorMessageHelper.SequenceWithPointerToOffendingMoment(Sequence, time)}{Environment.NewLine}{e.Message}.", e);
                }
        }

        public void VerifyNothingElse(int time)
        {
            var expectations = Expectations.Where(e => e.Time == time).Where(e => e.Marble == null).ToList();
            if (!expectations.Any())
                return;

            foreach (var exp in expectations)
                try
                {
                    exp.Assertion();
                }
                catch (Exception e)
                {
                    if (exp.Marble == null)
                        throw new Exception(
                            $"Unexpected event received at time {time} in sequence {ErrorMessageHelper.SequenceWithPointerToOffendingMoment(Sequence, time)}{Environment.NewLine}{e.Message}.", e);
                }
        }
    }
}