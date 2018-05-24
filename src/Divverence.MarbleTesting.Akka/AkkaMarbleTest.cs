using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
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
            : this(MultiDispatcherAwaiter.CreateFromActorSystem(sys).Idle, sys.FastForward, MultiCharMarbleParser.ParseSequence)
        {
        }

        /// <summary>
        /// Use if you are using AkkaMarbleTest.AsyncTestingConfig as config, but want to provide a custom Marble Sequence Parser (eg. MultiCharMarbleParser.ParseSequence)
        /// </summary>
        /// <param name="sys">Your TestKit.Sys ActorSystem</param>
        /// <param name="marbleParserFunc">Your Marble Sequence parser function</param>
        public AkkaMarbleTest(ActorSystem sys, Func<string, IEnumerable<Moment>> marbleParserFunc) : this(MultiDispatcherAwaiter.CreateFromActorSystem(sys).Idle, sys.FastForward, marbleParserFunc)
        {
        }

        /// <summary>
        /// Use this constructor if you don't want to use the Awaitable Dispatcher for fast async testing. Provide your own methods for 'waiting for idle' and fastforwarding.
        /// </summary>
        /// <param name="waitForIdle">Function that provides a Task that Run(..?) will await after issueing all actions, before verifying expectations</param>
        /// <param name="fastForward">Function that provides a Task that Run(step) will await after veryfying expectations</param>
        public AkkaMarbleTest(Func<Task> waitForIdle, Func<TimeSpan, Task> fastForward) : this(waitForIdle, fastForward, MultiCharMarbleParser.ParseSequence)
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
            Func<string, IEnumerable<Moment>> marbleParserFunc) : base(waitForIdle, fastForward, marbleParserFunc)
        {
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
}