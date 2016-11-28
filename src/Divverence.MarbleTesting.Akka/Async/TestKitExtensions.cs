using System;
using System.Threading.Tasks;
using Akka.TestKit;

namespace Divverence.MarbleTesting.Akka.Async
{
    /// <summary>
    /// Incomplete set of helpers that help write reliable, fast, asynchronous actor tests.
    /// </summary>
    public static class TestKitExtensions
    {
        public static async Task ExpectNoMsgAsync( this TestKitBase testKit )
        {
            await testKit.Sys.Idle();
            testKit.ExpectNoMsg(TimeSpan.Zero);
        }

        public static async Task<T> ExpectMsgAsync<T>(this TestKitBase testKit)
        {
            await testKit.Sys.Idle();
            return testKit.ExpectMsg<T>(TimeSpan.Zero);
        }

        public static async Task<T> ExpectMsgAsync<T>(this TestKitBase testKit, Predicate<T> predicate)
        {
            await testKit.Sys.Idle();
            return testKit.ExpectMsg(predicate, TimeSpan.Zero);
        }
    }
}