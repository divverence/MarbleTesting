using Akka.TestKit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Divverence.MarbleTesting.Akka
{
    public static class AkkaUnorderedExpectations
    {
        public static ExpectedMarble CreateExpectedMarbleForUnorderedGroup<T>(
            Moment moment,
            TestKitBase probe,
            Func<string, Action<T>> assertionFactory)
        {
            return new ExpectedMarble(
                moment.Time,
                FormatMarblesInUnorderedGroup(moment.Marbles),
                CreateExpectationForUnorderedGroup(
                    moment.Marbles,
                    probe,
                    assertionFactory));
        }

        private static Action CreateExpectationForUnorderedGroup<T>(
            string[] marblesInGroup,
            TestKitBase probe,
            Func<string, Action<T>> expectationGenerator)
        {
            return () =>
            {
                var results = new List<List<ExpectationResult>>();
                var marblesReceived = new List<string>();
                for (int i = 0; i < marblesInGroup.Length - 1; i++)
                {
                    var expectationResults = new List<ExpectationResult>();
                    results.Add(expectationResults);
                    TryExpectMsg(marblesInGroup, probe, expectationGenerator, marblesReceived, expectationResults, results);
                }
                try
                {
                    probe.ExpectMsg<T>(n =>
                        {
                            var expectationResults = new List<ExpectationResult>();
                            results.Add(expectationResults);
                            TryExpectationsForEachMarble(
                                marblesInGroup,
                                marblesReceived,
                                expectationGenerator,
                                expectationResults,
                                n);
                            var marblesNotYetReceived = marblesInGroup.Except(marblesReceived).ToList();
                            if (marblesNotYetReceived.Any())
                            {
                                var failedMarbles = DistinctResults(results)
                                    .Where(e => !marblesReceived.Contains(e.Marble)).ToList();
                                throw new AggregateException(
                                    "Did not receive all expected marbles in unordered group, expected: " +
                                    $"'{FormatMarblesInUnorderedGroup(marblesInGroup)}' but got " +
                                    $"'{FormatMarblesInUnorderedGroup(marblesReceived)}' " +
                                    $"{BuildAssertionsMessage(failedMarbles)}.", NonNullFailures(failedMarbles));
                            }
                        },
                        TimeSpan.Zero);
                }
                catch (Exception e) when (e.Message.Contains("Timeout"))
                {
                    EnhanceMessageForTimeoutWhileWaitingForMessage(e, marblesInGroup, marblesReceived, DistinctResults(results));
                }
            };
        }

        private static void TryExpectMsg<T>(
            string[] marblesInGroup,
            TestKitBase probe,
            Func<string, Action<T>> expectationGenerator,
            List<string> marblesReceived,
            List<ExpectationResult> expectationResults,
            List<List<ExpectationResult>> results)
        {
            try
            {
                probe.ExpectMsg<T>(n =>
                    {
                        TryExpectationsForEachMarble(
                            marblesInGroup,
                            marblesReceived,
                            expectationGenerator,
                            expectationResults,
                            n);
                    },
                    TimeSpan.Zero);
            }
            catch (Exception e) when (e.Message.Contains("Timeout"))
            {
                EnhanceMessageForTimeoutWhileWaitingForMessage(e, marblesInGroup, marblesReceived, DistinctResults(results));
            }
        }

        private static IEnumerable<ExpectationResult> DistinctResults(IEnumerable<List<ExpectationResult>> results)
        {
            return results
                .SelectMany(r => r)
                .Distinct(new ExpectationResultCompareOnMarble());
        }

        private static IEnumerable<Exception> NonNullFailures(IEnumerable<ExpectationResult> failedMarbles) => failedMarbles.Where(m => m.Failure != null).Select(r => r.Failure);

        private static string BuildAssertionsMessage(IEnumerable<ExpectationResult> failedMarbles)
        {
            return string.Join(Environment.NewLine,
                failedMarbles.Select(m =>
                {
                    if (m.Failure == null)
                    {
                        return $"did not receive marble '{m.Marble}'";
                    }
                    return $"receiving marble '{m.Marble}' : lead to failure: {m.Failure}";
                }));
        }

        public static string FormatMarblesInUnorderedGroup(IEnumerable<string> marbles) =>
            $"< {string.Join(" ", marbles)} >";

        private static void EnhanceMessageForTimeoutWhileWaitingForMessage(
            Exception e,
            IEnumerable<string> marblesInGroup,
            IEnumerable<string> marblesReceived,
            IEnumerable<ExpectationResult> resultsSoFar)
        {
            throw new Exception($"Did not receive all marbles in group '{FormatMarblesInUnorderedGroup(marblesInGroup)}' only received '{FormatMarblesInUnorderedGroup(marblesReceived)}'{Environment.NewLine}{string.Join(Environment.NewLine, NonNullFailures(resultsSoFar))}", e);
        }

        private static void TryExpectationsForEachMarble<T>(
            IEnumerable<string> marblesInGroup,
            ICollection<string> marblesReceived,
            Func<string, Action<T>> expectationGenerator,
            ICollection<ExpectationResult> results,
            T received)
        {
            foreach (var marble in marblesInGroup)
            {
                var expResult = new ExpectationResult(marble);
                results.Add(expResult);
                try
                {
                    expectationGenerator(marble)(received);
                    marblesReceived.Add(marble);
                }
                catch (Exception e)
                {
                    expResult.Failure = e;
                }
            }
        }

        private class ExpectationResult
        {
            public ExpectationResult(string marble)
            {
                Marble = marble;
            }

            public string Marble { get; }
            public Exception Failure { get; set; }
        }

        private class ExpectationResultCompareOnMarble : IEqualityComparer<ExpectationResult>
        {
            public bool Equals(ExpectationResult x, ExpectationResult y)
            {
                return string.Equals(x?.Marble, y?.Marble);
            }

            public int GetHashCode(ExpectationResult obj)
            {
                return obj.Marble.GetHashCode();
            }
        }
    }
}