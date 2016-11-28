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

        public void WhenDoing(string sequence, Func<string, Task> whatToDo)
        {
            var actions = MarbleParser.ParseSequence(sequence).SelectMany(moment => CreateInputMarbles(moment, whatToDo));
            Inputs.Add(new InputMarbles(sequence, actions));
        }
#pragma warning disable 1998 // Seems the best way to convert Action<string> into a Func<string,Task> ...
        public void WhenDoing(string sequence, Action<string> whatToDo)
            => WhenDoing(sequence, async marble => whatToDo(marble));
#pragma warning restore 1998

        private Task FastForward(TimeSpan howMuch) => _fastForward(howMuch);

        protected IEnumerable<InputMarble> CreateInputMarbles(Moment moment, Func<string, Task> whatToDo)
        {
            return moment.Marbles.Select(marble => new InputMarble(moment.Time, marble, () => whatToDo(marble)));
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
            public ExpectedMarbles(string sequence, IEnumerable<ExpectedMarble> expectations)
            {
                Sequence = sequence;
                Expectations = expectations.ToImmutableList();
                FirstTime = Expectations.Min(e => e.Time);
                LastTime = Expectations.Max(e => e.Time);
            }

            public string Sequence { get; }
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
                                $"Unexpected event received at time {time} on sequence {Sequence}", e);
                        throw new Exception(
                            $"Marble '{exp.Marble}' not received at time {time} on sequence {Sequence}", e);
                    }
                return true;
            }
        }

        protected class InputMarble
        {
            public InputMarble(int time, string marble, Func<Task> action)
            {
                Time = time;
                Marble = marble;
                Action = action;
            }

            public int Time { get; }
            public string Marble { get; }
            public Func<Task> Action { get; }
        }

        protected class InputMarbles
        {
            public InputMarbles(string sequence, IEnumerable<InputMarble> actions)
            {
                Sequence = sequence;
                Actions = actions.ToImmutableList();
            }

            public string Sequence { get; }
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
                        $"Error when firing marble '{m.Marble}' at time {time} on sequence {Sequence}", e);
                }
            }
        }
    }
}