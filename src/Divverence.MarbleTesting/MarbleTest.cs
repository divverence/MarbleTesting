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

        protected List<ExpectedMarbles> ExpectedMarbleStrings = new List<ExpectedMarbles>();

        protected List<InputMarbles> Inputs = new List<InputMarbles>();

        public MarbleTest(Func<Task> waitForIdle, Func<TimeSpan, Task> fastForward)
        {
            _waitForIdle = waitForIdle;
            _fastForward = fastForward;
        }

        private Task SystemIdle => _waitForIdle();

        public async Task Run(TimeSpan? interval = null)
        {
            var maxTime = ExpectedMarbleStrings.Max(etl => etl.LastTime);
            for (var time = 0; time <= maxTime; time++)
            {
                var localTime = time;
                await Task.WhenAll(Inputs.Select(atl => atl.Run(localTime)));
                await SystemIdle;
                ExpectedMarbleStrings.ForEach(etl => etl.Verify(time));
                if (interval.HasValue)
                    await FastForward(interval.Value);
            }
        }

        public void WhenDoing(string timeline, Func<string, Task> whatToDo)
        {
            var actions = ParseMarbles(timeline).SelectMany((tokens, t) => CreateInputMarbles(whatToDo, tokens, t));
            Inputs.Add(new InputMarbles(timeline, actions));
        }

        public void WhenDoing(string timeline, Action<string> whatToDo)
        {
            var actions =
                ParseMarbles(timeline)
                    .SelectMany((tokens, t) => CreateInputMarbles(async token => whatToDo(token), tokens, t));
            Inputs.Add(new InputMarbles(timeline, actions));
        }

        private Task FastForward(TimeSpan howMuch) => _fastForward(howMuch);

        public static IEnumerable<string[]> ParseMarbles(string line)
        {
            return line.Select(kar => kar == '-' ? new string[0] : new[] {kar.ToString()});
        }

        protected IEnumerable<InputMarble> CreateInputMarbles(Func<string, Task> whatToDo, string[] tokens, int time)
        {
            return tokens.Select(token => new InputMarble(time, token, () => whatToDo(token)));
        }

        protected class Expectation
        {
            public Expectation(int time, string token, Action assertion)
            {
                Time = time;
                Token = token;
                Assertion = assertion;
            }

            public int Time { get; }
            public string Token { get; }
            public Action Assertion { get; }
        }

        protected class ExpectedMarbles
        {
            public ExpectedMarbles(string marbleString, IEnumerable<Expectation> expectations)
            {
                MarbleString = marbleString;
                Expectations = expectations.ToImmutableList();
                LastTime = Expectations.Max(e => e.Time);
            }

            public string MarbleString { get; }
            public ImmutableList<Expectation> Expectations { get; }
            public int LastTime { get; set; }

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
                        if (exp.Token == null)
                            throw new Exception(
                                $"Unexpected message(s) received at time {time} on timeline {MarbleString}", e);
                        throw new Exception(
                            $"Message '{exp.Token}' not received at time {time} on timeline {MarbleString}", e);
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