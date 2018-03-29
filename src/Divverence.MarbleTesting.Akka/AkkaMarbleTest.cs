using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Akka.TestKit;
using Divverence.MarbleTesting.Akka.Async;

namespace Divverence.MarbleTesting.Akka
{
    public class AkkaMarbleTest : MarbleTest
    {
        /// <summary>
        /// The most default constructor. Use if you want to use default, industry-standard Marble Sequence syntax, and are using AkkaMarbleTest.AsyncTestingConfig as config.
        /// </summary>
        /// <param name="sys">Your TestKit.Sys ActorSystem</param>
        public AkkaMarbleTest(ActorSystem sys)
            : this(MultiDispatcherAwaiter.CreateFromActorSystem(sys).Idle, sys.FastForward)
        {
        }

        /// <summary>
        /// Use if you are using AkkaMarbleTest.AsyncTestingConfig as config, but want to provide a custom Marble Sequence Parser (eg. MultiCharMarbleParser.ParseSequence)
        /// </summary>
        /// <param name="sys">Your TestKit.Sys ActorSystem</param>
        /// <param name="marbleParserFunc">Your Marble Sequence parser function</param>
        public AkkaMarbleTest(ActorSystem sys, Func<string, IEnumerable<Moment>> marbleParserFunc) : this(sys)
        {
            SetMarbleParser(marbleParserFunc);
        }

        /// <summary>
        /// Use this constructor if you don't want to use the Awaitable Dispatcher for fast async testing. Provide your own methods for 'waiting for idle' and fastforwarding.
        /// </summary>
        /// <param name="waitForIdle">Function that provides a Task that Run(..?) will await after issueing all actions, before verifying expectations</param>
        /// <param name="fastForward">Function that provides a Task that Run(step) will await after veryfying expectations</param>
        public AkkaMarbleTest(Func<Task> waitForIdle, Func<TimeSpan, Task> fastForward) : base(waitForIdle, fastForward)
        {
        }

        /// <summary>
        /// Use this constructor if you don't want to use the Awaitable Dispatcher for fast async testing. Provide your own methods for 'waiting for idle' and fastforwarding.
        /// You also want to provide a custom Marble Sequence Parser (eg. MultiCharMarbleParser.ParseSequence)
        /// </summary>
        /// <param name="waitForIdle">Function that provides a Task that Run(..?) will await after issueing all actions, before verifying expectations</param>
        /// <param name="fastForward">Function that provides a Task that Run(step) will await after veryfying expectations</param>
        /// <param name="marbleParserFunc">Your Marble Sequence parser function</param>
        public AkkaMarbleTest(Func<Task> waitForIdle, Func<TimeSpan, Task> fastForward,
            Func<string, IEnumerable<Moment>> marbleParserFunc) : this(waitForIdle, fastForward)
        {
            SetMarbleParser(marbleParserFunc);
        }

        /// <summary>
        /// Returns the piece of Akka.Net configuration that makes sure the Awaitable Dispatcher is used for both test probes and all actors.
        /// It also makes sure the Akka Testkit TestScheduler is active
        /// To be combined with your own Config.
        /// </summary>
        public static Config DispatcherAndSchedulerConfig => ConfigExtensionsForAsyncTesting.AwaitableTaskDispatcherConfig.WithTestScheduler();

        /// <summary>
        /// Returns the Akka.Testkit default config, patched with the settings that make sure the Awaitable Dispatcher is used for both test probes and all actors, and TestScheduler from Akka Testkit is the active scheduler.
        /// </summary>
        public static Config AsyncTestingConfig => ConfigExtensionsForAsyncTesting.AsyncTestingConfig;
    }

    public static class MarbleTestExtensionsForAkka
    {
        public static void WhenTelling(this MarbleTest marbleTest, string sequence, IActorRef toWhom,
            Func<string, object> whatToSend)
            => marbleTest.WhenTelling(sequence, toWhom, marble => Task.FromResult(whatToSend(marble)));

        public static void WhenTelling<T>(this MarbleTest marbleTest, string sequence, IActorRef toWhom,
            Func<string, Task<T>> whatToSend)
            => marbleTest.WhenDoing(sequence, async marble => toWhom.Tell(await whatToSend(marble)));

        public static void ExpectMsgs<T>(this MarbleTest marbleTest, string sequence, TestProbe probe,
            Func<string, Action<T>> assertionFactory)
        {
            marbleTest.Expect(sequence, marble => () => probe.ExpectMsg(assertionFactory(marble), TimeSpan.Zero),
                () => probe.ExpectNoMsg(0),
                moment => AkkaUnorderedExpectations.CreateExpectedMarbleForUnorderedGroup(moment, probe, assertionFactory));
        }

        public static void ExpectMsgs<T>(this MarbleTest marbleTest, string sequence, TestProbe probe,
            Func<string, T, bool> predicate) =>
            marbleTest.ExpectMsgs(sequence, probe, (Func<string, Action<T>>) (m => t => predicate(m, t)));

        public static void ExpectMsgs(this MarbleTest marbleTest, string sequence, TestProbe probe,
            Func<string, object, bool> predicate)
            => marbleTest.ExpectMsgs<object>(sequence, probe, predicate);

        public static void ExpectMsgs(this MarbleTest marbleTest, string sequence, TestProbe probe,
            Func<string, Action<object>> predicate)
            => marbleTest.ExpectMsgs<object>(sequence, probe, predicate);

        public static void ExpectMsgs(this MarbleTest marbleTest, string sequence, TestProbe probe)
            => marbleTest.ExpectMsgs<string>(sequence, probe, (marble, msg) => marble == msg);
    }

    public static class AkkaUnorderedExpectations
    { 
        public static ExpectedMarble CreateExpectedMarbleForUnorderedGroup<T>(Moment moment, TestKitBase probe, Func<string, Action<T>> assertionFactory)
        {
            return new ExpectedMarble(moment.Time, FormatMarblesInUnorderedGroup(moment.Marbles),
                CreateExpectationForUnorderedGroup(moment.Marbles, probe, assertionFactory));
        }

        private static Action CreateExpectationForUnorderedGroup<T>(
            string[] marblesInGroup,
            TestKitBase probe,
            Func<string, Action<T>> expectionGenerator)
        {
            return () =>
            {
                var results = new List<List<ExpectationResult>>();
                var marblesReceived = new List<string>();
                for (int i = 0; i < marblesInGroup.Length - 1; i++)
                {
                    var expectationResults = new List<ExpectationResult>();
                    results.Add(expectationResults);
                    TryExpectMsg(marblesInGroup, probe, expectionGenerator, marblesReceived, expectationResults, results);
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
                            expectionGenerator,
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

        private static void TryExpectMsg<T>(string[] marblesInGroup, TestKitBase probe, Func<string, Action<T>> expectionGenerator,
            List<string> marblesReceived, List<ExpectationResult> expectationResults, List<List<ExpectationResult>> results)
        {
            try
            {
                probe.ExpectMsg<T>(n =>
                {
                    TryExpectationsForEachMarble(
                        marblesInGroup,
                        marblesReceived,
                        expectionGenerator,
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
            throw new Exception($"Did not recieve all marbles in group '{FormatMarblesInUnorderedGroup(marblesInGroup)}' only received '{FormatMarblesInUnorderedGroup(marblesReceived)}'{Environment.NewLine}{string.Join(Environment.NewLine, NonNullFailures(resultsSoFar))}", e);
        }

        private static void TryExpectationsForEachMarble<T>(
            IEnumerable<string> marblesInGroup,
            ICollection<string> marblesReceived,
            Func<string, Action<T>> expectionGenerator,
            ICollection<ExpectationResult> results,
            T received)
        {
            foreach (var marble in marblesInGroup)
            {
                var expResult = new ExpectationResult(marble);
                results.Add(expResult);
                try
                {
                    expectionGenerator(marble)(received);
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