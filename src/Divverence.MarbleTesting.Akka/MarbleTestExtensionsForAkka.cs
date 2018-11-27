using Akka.Actor;
using Akka.TestKit;
using System;
using System.Threading.Tasks;

namespace Divverence.MarbleTesting.Akka
{
    public static class MarbleTestExtensionsForAkka
    {
        public static void WhenTelling(
            this MarbleTest marbleTest,
            string sequence,
            IActorRef toWhom,
            Func<string, object> whatToSend)
            => marbleTest.WhenTelling(sequence, toWhom, marble => Task.FromResult(whatToSend(marble)));

        public static void WhenTelling<T>(
            this MarbleTest marbleTest,
            string sequence,
            IActorRef toWhom,
            Func<string, Task<T>> whatToSend)
            => marbleTest.WhenDoing(sequence, async marble => toWhom.Tell(await whatToSend(marble)));

        public static void ExpectMsgs<T>(
            this MarbleTest marbleTest,
            string sequence,
            TestProbe probe,
            Func<string, Action<T>> assertionFactory,
            Action nothingElseAssertion = null)
        {
            marbleTest.Expect(sequence, marble => () => probe.ExpectMsg(assertionFactory(marble), TimeSpan.Zero),
                nothingElseAssertion ?? (() => probe.ExpectNoMsg(0)),
                moment => AkkaUnorderedExpectations.CreateExpectedMarbleForUnorderedGroup(moment, act => probe.ExpectMsg(act, TimeSpan.Zero), assertionFactory));
        }

        public static void ExpectMsgs(
            this MarbleTest marbleTest,
            string sequence,
            TestProbe probe,
            Func<string, Action<object>> assertion,
            Action nothingElseAssertion = null)
            => marbleTest.ExpectMsgs<object>(sequence, probe, assertion, nothingElseAssertion);

        [Obsolete("Use the overload with the Func<string, Action<T>> parameter where the action contains a custom assertion, such that more meaningful failure messages can be generated.")]
        public static void ExpectMsgs<T>(this MarbleTest marbleTest, string sequence, TestProbe probe,
            Func<string, T, bool> predicate) =>
            marbleTest.ExpectMsgs(sequence, probe, (Func<string, Action<T>>)(m => t => predicate(m, t)));

        [Obsolete("Use the overload with the Func<string, Action<object>> parameter where the action contains a custom assertion, such that more meaningful failure messages can be generated.")]
        public static void ExpectMsgs(this MarbleTest marbleTest, string sequence, TestProbe probe,
            Func<string, object, bool> predicate)
            => marbleTest.ExpectMsgs<object>(sequence, probe, predicate);

        [Obsolete("Use the overload with the Func<string, Action<T>> parameter where the action contains a custom assertion, such that more meaningful failure messages can be generated.")]
        public static void ExpectMsgs(this MarbleTest marbleTest, string sequence, TestProbe probe)
            => marbleTest.ExpectMsgs<string>(sequence, probe, (marble, msg) => marble == msg);
    }
}