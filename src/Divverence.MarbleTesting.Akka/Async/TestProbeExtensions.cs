using Akka.Actor;
using Akka.TestKit;
using System;
using System.Threading.Tasks;

namespace Divverence.MarbleTesting.Akka.Async
{
    /// <summary>
    /// Incomplete set of helpers that help write reliable, fast, asynchronous actor tests.
    /// </summary>
    public static class TestProbeExtensions
    {
        public static async Task ExpectNoMsgAsync(this TestProbe testProbe)
        {
            await testProbe.Sys.Idle();
            testProbe.ExpectNoMsg(TimeSpan.Zero);
        }

        public static async Task<T> ExpectMsgAsync<T>(this TestProbe testProbe, string hint = null)
        {
            await testProbe.Sys.Idle();
            return testProbe.ExpectMsg<T>(TimeSpan.Zero, hint);
        }

        public static async Task<T> ExpectMsgAsync<T>(this TestProbe testProbe, Predicate<T> predicate, string hint = null)
        {
            await testProbe.Sys.Idle();
            return testProbe.ExpectMsg(predicate, TimeSpan.Zero, hint);
        }

        public static async Task<T> ExpectMsgAsync<T>(this TestProbe testProbe, Action<T> assertion, string hint = null)
        {
            await testProbe.Sys.Idle();
            return testProbe.ExpectMsg(assertion, TimeSpan.Zero, hint);
        }

        public static async Task ExpectTerminatedAsync<T>(this TestProbe testProbe, IActorRef actorRef, string hint = null)
        {
            await testProbe.Sys.Idle();
            testProbe.ExpectTerminated(actorRef, TimeSpan.Zero, hint);
        }
    }
}