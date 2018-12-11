using Akka.Actor;
using Akka.TestKit;
using Microsoft.FSharp.Core;
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
            Func<string, object> whatToSend) =>
                marbleTest.WhenTelling(sequence, toWhom, marble => Task.FromResult(whatToSend(marble)));

        public static void WhenTelling<T>(
            this MarbleTest marbleTest,
            string sequence,
            IActorRef toWhom,
            Func<string, Task<T>> whatToSend) =>
                marbleTest.WhenDoing(sequence, async marble => toWhom.Tell(await whatToSend(marble)));

        public static void ExpectMsgs<T>(
            this MarbleTest marbleTest,
            string sequence,
            TestProbe probe,
            Func<string, Action<T>> assertionFactory) =>
                marbleTest.Expect(
                    sequence,
                    probe.EventProducer<T>(),
                    (m, ev) => assertionFactory(m)(ev));

        public static void ExpectMsgs(
            this MarbleTest marbleTest,
            string sequence,
            TestProbe probe,
            Func<string, Action<object>> assertion) =>
                marbleTest.ExpectMsgs<object>(
                    sequence,
                    probe,
                    assertion);

        public static Func<FSharpOption<TEvent>> EventProducer<TEvent>(this TestProbe testProbe)
        {
            return () => testProbe.HasMessages
                ? FSharpOption<TEvent>.Some(testProbe.ExpectMsg<TEvent>(_ => true, TimeSpan.Zero))
                : FSharpOption<TEvent>.None;
        }
    }
}