using System;
using System.Threading.Tasks;
using Akka.Configuration;
using Akka.TestKit;

namespace Divverence.MarbleTesting.Akka.Async
{
    /// <summary>
    /// Helper class that allows writing tests for actors without having to rely on arbitrary timeout values
    /// Instead, you can write fully asynchronous, reliable tests.
    /// Uses a special Dispatcher implementation in order to reliably and asynchronously tell when the local
    /// actor system has become idle.
    /// </summary>
    public class AsyncTestKitHelper
    {
        private readonly TestKitBase _akkaTestKit;
        private readonly Func<Task> _systemIdle;
        private readonly Func<Task> _systemActive;

        public AsyncTestKitHelper(TestKitBase akkaTestKit) : this(akkaTestKit, MultiDispatcherAwaiter.CreateFromActorSystem(akkaTestKit.Sys))
        {
        }

        private AsyncTestKitHelper(
            TestKitBase akkaTestKit,
            IWaitForIdleOrActive idleOrActive) :
                this(
                    akkaTestKit,
                    idleOrActive.Idle,
                    idleOrActive.Active)
        { }

        public AsyncTestKitHelper(
            TestKitBase akkaTestKit,
            Func<Task> systemIdle,
            Func<Task> systemActive)
        {
            _akkaTestKit = akkaTestKit;
            _systemIdle = systemIdle;
            _systemActive = systemActive;
            var actorSystem = akkaTestKit.Sys;
            TestScheduler = actorSystem.Scheduler as TestScheduler;
        }

        #region Dispatcher

        /// <summary>
        /// Await the returned Task in order to wait for the ActorSystem to become Idle - no more messages being processed or queued in any mailboxes.
        /// Note that there can still be Stashed messages and Scheduled messages.
        /// Note that even ReceiveAsync handlers must complete before the system is regarded as Idle.
        /// </summary>
        public Task SystemIdle => _systemIdle();

        /// <summary>
        /// Await the returned Task in order to wait for the ActorSystem to become Active. Needed only when an asynchronous action should active the system.
        /// Not needed when performing a Tell or Ask, or when FastForwarding the test scheduler
        /// </summary>
        public Task SystemActive => _systemActive();

        /// <summary>
        /// Block the thread until the ActorSystem is idle
        /// Warning: If any of the tasks running or waited for in the actor system need the running thread, this may block
        /// </summary>
        public void WaitSystemIdle()
        {
            SystemIdle.Wait();
        }

        #endregion

        #region Scheduler

        public TestScheduler TestScheduler { get; }
        public static TimeSpan OneTick { get; } = TimeSpan.FromTicks(1);

        protected async Task FastForward(TimeSpan by)
        {
            if (TestScheduler == null)
                throw new InvalidOperationException(
                    "akka.scheduler.implementation must be configured to \"Akka.TestKit.TestScheduler, Akka.TestKit\"");

            await SystemIdle;
            TestScheduler.Advance(by);
            await SystemIdle;
        }

        protected async Task FastForwardTo(DateTimeOffset to)
        {
            if (TestScheduler == null)
                throw new InvalidOperationException(
                    "akka.scheduler.implementation must be configured to \"Akka.TestKit.TestScheduler, Akka.TestKit\"");

            await SystemIdle;
            TestScheduler.AdvanceTo(to);
            await SystemIdle;
        }

        protected Task Tick() => FastForward(OneTick);

        #endregion

        #region Expectations

        public async Task ExpectNoMsg()
        {
            await SystemIdle;
            _akkaTestKit.ExpectNoMsg(TimeSpan.Zero);
        }

        public async Task<T> ExpectMsg<T>()
        {
            await SystemIdle;
            return _akkaTestKit.ExpectMsg<T>(TimeSpan.Zero);
        }

        public async Task<T> ExpectMsg<T>(Predicate<T> pred)
        {
            await SystemIdle;
            return _akkaTestKit.ExpectMsg(pred, TimeSpan.Zero);
        }

        public async Task<T> ExpectMsg<T>(Action<T> assertion)
        {
            await SystemIdle;
            return _akkaTestKit.ExpectMsg(assertion, TimeSpan.Zero);
        }

        #endregion

        #region Config

        public static Config GetDefaultConfig() => ConfigExtensionsForAsyncTesting.AsyncTestingConfig;
        public static Config GetDefaultConfigWith(string hokon) => ConfigurationFactory.ParseString(hokon).WithFallback(ConfigExtensionsForAsyncTesting.AsyncTestingConfig);

        #endregion
    }
}