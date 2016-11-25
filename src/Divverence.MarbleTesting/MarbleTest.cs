using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Divverence.MarbleTesting
{
    public class MarbleTest
    {
        private readonly Func<TimeSpan, Task> _fastForward;
        private readonly Func<Task> _waitForIdle;
        protected List<ExpectedMarbles> Expectations = new List<ExpectedMarbles>();
        protected List<InputMarbles> Inputs = new List<InputMarbles>();

        public MarbleTest(Func<Task> waitForIdle, Func<TimeSpan, Task> fastForward)
        {
            _waitForIdle = waitForIdle;
            _fastForward = fastForward;
        }

        private Task SystemIdle => _waitForIdle();

        public async Task Run(TimeSpan? interval = null)
        {
            var maxTime = Expectations.Max(etl => etl.LastTime);
            var minTime = Expectations.Max(etl => etl.FirstTime);
            for (var time = minTime; time <= maxTime; time++)
            {
                var localTime = time;
                await Task.WhenAll(Inputs.Select(atl => atl.Run(localTime)));
                await SystemIdle;
                Expectations.ForEach(etl => etl.Verify(time));
                if (interval.HasValue)
                    await FastForward(interval.Value);
            }
        }

        public void WhenDoing(string timeline, Func<string, Task> whatToDo)
        {
            var actions = MarbleParser.ParseMarbles(timeline).SelectMany(moment => CreateInputMarbles(moment, whatToDo));
            Inputs.Add(new InputMarbles(timeline, actions));
        }
#pragma warning disable 1998 // Seems the best way to convert Action<string> into a Func<string,Task> ...
        public void WhenDoing(string timeline, Action<string> whatToDo)
            => WhenDoing(timeline, async token => whatToDo(token));
#pragma warning restore 1998

        private Task FastForward(TimeSpan howMuch) => _fastForward(howMuch);

        protected IEnumerable<InputMarble> CreateInputMarbles(Moment moment, Func<string, Task> whatToDo)
        {
            return moment.Marbles.Select(token => new InputMarble(moment.Time, token, () => whatToDo(token)));
        }

        protected class ExpectedMarble
        {
            public ExpectedMarble(int time, string marble, Action assertion)
            {
                Time = time;
                Marble = marble;
                Assertion = assertion;
            }

            public int Time { get; }
            public string Marble { get; }
            public Action Assertion { get; }
        }

        protected class ExpectedMarbles
        {
            public ExpectedMarbles(string marbleString, IEnumerable<ExpectedMarble> expectations)
            {
                MarbleString = marbleString;
                Expectations = expectations.ToImmutableList();
                FirstTime = Expectations.Min(e => e.Time);
                LastTime = Expectations.Max(e => e.Time);
            }

            public string MarbleString { get; }
            public ImmutableList<ExpectedMarble> Expectations { get; }
            public int LastTime { get; set; }
            public int FirstTime { get; set; }

            public bool Verify(int time)
            {
                var expectations = Expectations.Where(e => e.Time == time).ToList();
                if (!expectations.Any())
                    return false;

                foreach (var exp in expectations)
                    try
                    {
                        exp.Assertion();
                    }
                    catch (Exception e)
                    {
                        if (exp.Marble == null)
                            throw new Exception(
                                $"Unexpected event received at time {time} on timeline {MarbleString}", e);
                        throw new Exception(
                            $"Marble '{exp.Marble}' not received at time {time} on timeline {MarbleString}", e);
                    }
                return true;
            }
        }

        protected class InputMarble
        {
            public InputMarble(int time, string token, Func<Task> action)
            {
                Time = time;
                Token = token;
                Action = action;
            }

            public int Time { get; }
            public string Token { get; }
            public Func<Task> Action { get; }
        }

        protected class InputMarbles
        {
            public InputMarbles(string marbleString, IEnumerable<InputMarble> actions)
            {
                MarbleString = marbleString;
                Actions = actions.ToImmutableList();
            }

            public string MarbleString { get; }
            public ImmutableList<InputMarble> Actions { get; }

            public Task Run(int time)
            {
                return Task.WhenAll(Actions.Where(e => e.Time == time).Select(exp => Run(exp, time)));
            }

            private async Task Run(InputMarble m, int time)
            {
                try
                {
                    await m.Action();
                }
                catch (Exception e)
                {
                    throw new Exception(
                        $"Error when firing marble '{m.Token}' at time {time} on marble string {MarbleString}", e);
                }
            }
        }
    }
}