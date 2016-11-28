using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.TestKit;

namespace Divverence.MarbleTesting.Akka.Async
{
    /// <summary>
    /// Incomplete set of helpers that help write reliable, fast, asynchronous actor tests.
    /// </summary>
    public static class TestKitExtensions
    {
        public static async Task ExpectNoMsgAsync(this TestKitBase testKit)
        {
            await testKit.Sys.Idle();
            testKit.ExpectNoMsg(TimeSpan.Zero);
        }

        public static async Task<T> ExpectMsgAsync<T>(this TestKitBase testKit, string hint = null)
        {
            await testKit.Sys.Idle();
            return testKit.ExpectMsg<T>(TimeSpan.Zero, hint);
        }

        public static async Task<T> ExpectMsgAsync<T>(this TestKitBase testKit, Predicate<T> predicate, string hint = null)
        {
            await testKit.Sys.Idle();
            return testKit.ExpectMsg(predicate, TimeSpan.Zero, hint);
        }

        public static async Task<T> ExpectMsgAsync<T>(this TestKitBase testKit, Action<T> assertion, string hint = null)
        {
            await testKit.Sys.Idle();
            return testKit.ExpectMsg(assertion, TimeSpan.Zero, hint);
        }

        public static async Task ExpectTerminatedAsync<T>(this TestKitBase testKit, IActorRef actorRef, string hint = null)
        {
            await testKit.Sys.Idle();
            testKit.ExpectTerminated(actorRef, TimeSpan.Zero, hint);
        }
    }
}