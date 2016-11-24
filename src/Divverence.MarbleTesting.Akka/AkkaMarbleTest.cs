using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.TestKit;

namespace Divverence.MarbleTesting.Akka
{
    public class AkkaMarbleTest : MarbleTest
    {
        public AkkaMarbleTest(Func<Task> waitForIdle, Func<TimeSpan, Task> fastForward)
            : base(waitForIdle, fastForward)
        {
        }

        public void WhenTelling(string timeline, IActorRef toWhom, Func<string, object> whatToSend)
            => WhenTelling(timeline, toWhom, token => Task.FromResult(whatToSend(token)));

        public void WhenTelling<T>(string timeline, IActorRef toWhom, Func<string, Task<T>> whatToSend)
        {
            var actions =
                ParseMarbles(timeline).SelectMany((tokens, t) => CreateTestAction(whatToSend, toWhom, tokens, t));
            Inputs.Add(new InputMarbles(timeline, actions));
        }

        public void ExpectMsgs<T>(string timeline, TestProbe probe, Func<string, T, bool> predicate)
        {
            var expectations =
                ParseMarbles(timeline).SelectMany((tokens, t) => GetExpectations(predicate, probe, tokens, t));
            ExpectedMarbleStrings.Add(new ExpectedMarbles(timeline, expectations));
        }

        public void ExpectMsgs(string timeline, TestProbe probe)
        {
            ExpectMsgs<string>(timeline, probe, (token, msg) => token == msg);
        }

        private IEnumerable<Expectation> GetExpectations<T>(Func<string, T, bool> predicate, TestProbe probe,
            string[] tokens, int time)
        {
            return tokens
                .Select(
                    token =>
                        new Expectation(time, token,
                            () => probe.ExpectMsg<T>(t => predicate(token, t), TimeSpan.Zero)))
                .Concat(Enumerable.Repeat(new Expectation(time, null, () => probe.ExpectNoMsg(0)), 1));
        }

        private IEnumerable<InputMarble> CreateTestAction<T>(Func<string, Task<T>> whatToSend, IActorRef toWhom,
            string[] tokens, int time)
        {
            return tokens.Select(token => new InputMarble(time, token, async () => toWhom.Tell(await whatToSend(token))));
        }
    }
}