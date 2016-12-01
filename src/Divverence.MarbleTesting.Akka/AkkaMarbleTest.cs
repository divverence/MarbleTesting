using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.TestKit;
using Divverence.MarbleTesting.Akka.Async;

namespace Divverence.MarbleTesting.Akka
{
    public class AkkaMarbleTest : MarbleTest
    {
        public AkkaMarbleTest(ActorSystem sys)
            : this(MultiDispatcherAwaiter.CreateFromActorSystem(sys).Idle, sys.FastForward)
        {
        }

        public AkkaMarbleTest(ActorSystem sys, Func<string, IEnumerable<Moment>> marbleParserFunc) : this(sys)
        {
            SetMarbleParser(marbleParserFunc);
        }

        public AkkaMarbleTest(Func<Task> waitForIdle, Func<TimeSpan, Task> fastForward) : base(waitForIdle, fastForward)
        {
        }

        public AkkaMarbleTest(Func<Task> waitForIdle, Func<TimeSpan, Task> fastForward,
            Func<string, IEnumerable<Moment>> marbleParserFunc) : this(waitForIdle, fastForward)
        {
            SetMarbleParser(marbleParserFunc);
        }

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

        public void ExpectMsgs(string sequence, TestProbe probe, Func<string, object, bool> predicate)
            => ExpectMsgs<object>(sequence, probe, predicate);

        public void ExpectMsgs(string sequence, TestProbe probe)
            => ExpectMsgs<string>(sequence, probe, (marble, msg) => marble == msg);

        private IEnumerable<ExpectedMarble> CreateExpectations<T>(Moment moment, TestProbe probe,
            Func<string, T, bool> predicate)
        {
            return moment.Marbles
                .Select(
                    marble =>
                        new ExpectedMarble(moment.Time, marble,
                            () => probe.ExpectMsg<T>(t => predicate(marble, t), TimeSpan.Zero)))
                .Concat(Enumerable.Repeat(new ExpectedMarble(moment.Time, null, () => probe.ExpectNoMsg(0)), 1));
        }
    }
}