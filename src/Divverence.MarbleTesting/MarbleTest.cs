using System;
using System.Collections.Generic;
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
        private static Func<string,IEnumerable<Moment>> _parseSequenceFunc = MarbleParser.ParseSequence;

        public MarbleTest(Func<Task> waitForIdle, Func<TimeSpan, Task> fastForward)
        {
            _waitForIdle = waitForIdle;
            _fastForward = fastForward;
        }

        public void SetMarbleParser(Func<string, IEnumerable<Moment>> parserFunc) => _parseSequenceFunc = parserFunc;

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
            var actions = ParseSequence(sequence).SelectMany(moment => CreateInputMarbles(moment, whatToDo));
            Inputs.Add(new InputMarbles(sequence, actions));
        }

        public void AddExpectations(ExpectedMarbles marbles) => Expectations.Add(marbles);

        public void Expect(string sequence, Func<string, Action> assertionFactory, Action nothingElseAssertion,
            Func<Moment, ExpectedMarble> unorderedGroupExpectationCreator) =>
            Expect(sequence, assertionFactory, nothingElseAssertion, m => Enumerable.Repeat(unorderedGroupExpectationCreator(m), 1));

        public void Expect(string sequence, Func<string, Action> assertionFactory, Action nothingElseAssertion, Func<Moment, IEnumerable<ExpectedMarble>> unorderedGroupExpectationsCreator = null)
        {
            var expectations = ParseSequence(sequence)
                .SelectMany(moment => CreateExpectations(moment, assertionFactory, nothingElseAssertion, unorderedGroupExpectationsCreator));
            AddExpectations(new ExpectedMarbles(sequence, expectations));
        }

        protected static IEnumerable<ExpectedMarble> CreateExpectations(Moment moment, Func<string, Action> assertionFactory, Action nothingElseAssertion, Func<Moment,IEnumerable<ExpectedMarble>> unorderedGroupExpectationsCreator = null)
        {
            if (!moment.IsOrderedGroup)
            {
                if (unorderedGroupExpectationsCreator == null)
                    throw new NotImplementedException();
                return unorderedGroupExpectationsCreator(moment);
            }
            return moment.Marbles
                .Select(
                    marble =>
                        new ExpectedMarble(moment.Time, marble, assertionFactory(marble)))
                .Concat(Enumerable.Repeat(new ExpectedMarble(moment.Time, null, nothingElseAssertion), 1));
        }

        protected static IEnumerable<Moment> ParseSequence(string sequence)
        {
            return _parseSequenceFunc(sequence);
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
    }
}