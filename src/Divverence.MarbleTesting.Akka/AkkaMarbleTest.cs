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
            : base(MultiDispatcherAwaiter.CreateFromActorSystem(sys).Idle, sys.FastForward)
        {
        }

        public void WhenTelling(string timeline, IActorRef toWhom, Func<string, object> whatToSend)
            => WhenTelling(timeline, toWhom, token => Task.FromResult(whatToSend(token)));

        public void WhenTelling<T>(string timeline, IActorRef toWhom, Func<string, Task<T>> whatToSend)
            => WhenDoing(timeline, async token => toWhom.Tell(await whatToSend(token)));

        public void ExpectMsgs<T>(string timeline, TestProbe probe, Func<string, T, bool> predicate)
        {
            var expectations =
                MarbleParser.ParseMarbles(timeline).SelectMany(moment => CreateExpectations(moment, probe, predicate));
            Expectations.Add(new ExpectedMarbles(timeline, expectations));
        }

        public void ExpectMsgs(string timeline, TestProbe probe)
            => ExpectMsgs<string>(timeline, probe, (token, msg) => token == msg);

        private IEnumerable<ExpectedMarble> CreateExpectations<T>(Moment moment, TestProbe probe,
            Func<string, T, bool> predicate)
        {
            return moment.Marbles.Where(m => m != "^")
                .Select(
                    marble =>
                        new ExpectedMarble(moment.Time, marble,
                            () => probe.ExpectMsg<T>(t => predicate(marble, t), TimeSpan.Zero)))
                .Concat(Enumerable.Repeat(new ExpectedMarble(moment.Time, null, () => probe.ExpectNoMsg(0)), 1));
        }
    }
}