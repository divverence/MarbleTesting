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
        public static async Task ExpectNoMsg( this TestKitBase testKit )
        {
            await testKit.Sys.Idle();
            testKit.ExpectNoMsg(TimeSpan.Zero);
        }

        public static async Task ExpectMsg<T>(this TestKitBase testKit)
        {
            await testKit.Sys.Idle();
            testKit.ExpectMsg<T>();
        }

        public static async Task ExpectMsg<T>(this TestKitBase testKit, Predicate<T> predicate)
        {
            await testKit.Sys.Idle();
            testKit.ExpectMsg(predicate);
        }
    }
}