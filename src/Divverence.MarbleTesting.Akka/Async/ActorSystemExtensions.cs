using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.TestKit;

namespace Divverence.MarbleTesting.Akka.Async
{
    public static class ActorSystemExtensions
    {
        /// <summary>
        /// Await the returned Task in order to wait for the ActorSystem to become Idle - no more messages being processed or queued in any mailboxes.
        /// Note that there can still be Stashed messages and Scheduled messages.
        /// Note that even ReceiveAsync handlers must complete before the system is regarded as Idle.
        /// </summary>
        public static Task Idle(this ActorSystem actorSystem)
            => MultiDispatcherAwaiter.CreateFromActorSystem(actorSystem).Idle();

        /// <summary>
        /// Await the returned Task in order to wait for the ActorSystem to become Active. Needed only when an asynchronous action should active the system.
        /// Not needed when performing a Tell or Ask, or when FastForwarding the test scheduler
        /// </summary>
        public static Task Active(this ActorSystem actorSystem)
            => MultiDispatcherAwaiter.CreateFromActorSystem(actorSystem).Active();

        /// <summary>
        /// Get the actorSystem's Scheduler as TestScheduler
        /// If another scheduler is configured, throws an exception
        /// </summary>
        /// <param name="actorSystem"></param>
        /// <returns>The TestScheduler. Never null.</returns>
        public static TestScheduler TestScheduler(this ActorSystem actorSystem)
        {
            var testScheduler = actorSystem.Scheduler as TestScheduler;
            if (testScheduler == null)
                throw new InvalidOperationException("akka.scheduler.implementation must be configured to \"Akka.TestKit.TestScheduler, Akka.TestKit\"");
            return testScheduler;
        }

        /// <summary>
        /// Fast Forwards the TestScheduler in the ActorSystem for the indicated amount of time and returns a task that completes when the system has
        /// once again become idle.
        /// </summary>
        /// <param name="actorSystem"></param>
        /// <param name="howMuch">How far to fastforward time</param>
        /// <returns></returns>
        public static Task FastForward(this ActorSystem actorSystem, TimeSpan howMuch)
        {
            actorSystem.TestScheduler().Advance(howMuch);
            return actorSystem.Idle();
        }
    }
}