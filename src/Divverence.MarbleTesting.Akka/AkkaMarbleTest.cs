﻿using System;
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

        public void WhenTelling(string sequence, IActorRef toWhom, Func<string, object> whatToSend)
            => WhenTelling(sequence, toWhom, marble => Task.FromResult(whatToSend(marble)));

        public void WhenTelling<T>(string sequence, IActorRef toWhom, Func<string, Task<T>> whatToSend)
            => WhenDoing(sequence, async marble => toWhom.Tell(await whatToSend(marble)));

        public void ExpectMsgs<T>(string sequence, TestProbe probe, Func<string, T, bool> predicate)
        {
            var expectations = ParseSequence(sequence)
                .SelectMany(moment => CreateExpectations(moment, probe, predicate));
            Expectations.Add(new ExpectedMarbles(sequence, expectations));
        }

        public void ExpectMsgs<T>(string sequence, TestProbe probe, Func<string, Action<T>> predicate)
        {
            var expectations = ParseSequence(sequence)
                .SelectMany(moment => CreateExpectations(moment, probe, predicate));
            Expectations.Add(new ExpectedMarbles(sequence, expectations));
        }

        public void ExpectMsgs(string sequence, TestProbe probe, Func<string, object, bool> predicate)
            => ExpectMsgs<object>(sequence, probe, predicate);

        public void ExpectMsgs(string sequence, TestProbe probe, Func<string, Action<object>> predicate)
            => ExpectMsgs<object>(sequence, probe, predicate);

        public void ExpectMsgs(string sequence, TestProbe probe)
            => ExpectMsgs<string>(sequence, probe, (marble, msg) => marble == msg);

        private static IEnumerable<ExpectedMarble> CreateExpectations<T>(Moment moment, TestKitBase probe,
            Func<string, T, bool> predicate)
        {
            return CreateExpectationsImpl(
                moment, 
                probe, 
                m => () => probe.ExpectMsg<T>(t => predicate(m, t), TimeSpan.Zero));
        }

        private static IEnumerable<ExpectedMarble> CreateExpectations<T>(Moment moment, TestKitBase probe,
            Func<string, Action<T>> assertionFactory)
        {
            return CreateExpectationsImpl(
                moment, 
                probe, 
                m => () => probe.ExpectMsg(assertionFactory(m), TimeSpan.Zero));
        }

        private static IEnumerable<ExpectedMarble> CreateExpectationsImpl(
            Moment moment, 
            TestKitBase probe, 
            Func<string, Action> expectionGenerator)
        {
            return moment.Marbles
                .Select(
                    marble =>
                        new ExpectedMarble(moment.Time, marble, expectionGenerator(marble)))
                .Concat(Enumerable.Repeat(new ExpectedMarble(moment.Time, null, () => probe.ExpectNoMsg(0)), 1));
        }
    }
}