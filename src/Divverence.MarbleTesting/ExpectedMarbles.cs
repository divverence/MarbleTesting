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
            InvokeAssertions(time, expectations);
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
                    throw new Exception(
                        $"At time {time}, unexpected events were received {ErrorMessageHelper.SequenceWithPointerToOffendingMoment(Sequence, time)}{Environment.NewLine}{e.Message}.", e);
                }
        }

        private void InvokeAssertions(int time, List<ExpectedMarble> expectations)
        {
            if (!expectations.Any())
                return;

            foreach (var exp in expectations)
                try
                {
                    exp.Assertion();
                }
                catch (MissingEventException mee)
                {
                    throw new Exception(
                        $"At time {time}, for marble '{exp.Marble}' not all expected events were received {ErrorMessageHelper.SequenceWithPointerToOffendingMoment(Sequence, time)}{Environment.NewLine}{mee.Message}.",
                        mee);
                }
                catch (UnexpectedEventsException uee)
                {
                    throw new Exception(
                        $"At time {time}, for marble '{exp.Marble}' unexpected events were received {ErrorMessageHelper.SequenceWithPointerToOffendingMoment(Sequence, time)}{Environment.NewLine}{uee.Message}.",
                        uee);
                }

                catch (Exception e)
                {
                    throw new Exception(
                        $"At time {time}, for marble '{exp.Marble}' its assertion was not satisfied {ErrorMessageHelper.SequenceWithPointerToOffendingMoment(Sequence, time)}{Environment.NewLine}{e.Message}.",
                        e);
                }
        }
    }
}