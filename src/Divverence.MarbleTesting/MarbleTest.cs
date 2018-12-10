using Microsoft.FSharp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Divverence.MarbleTesting
{
    public class MarbleTest
    {
        private class MarbleEventAssertionResultTable
        {
            public class Result
            {
                public Result(
                    string marble,
                    object @event,
                    string assertionExceptionMessage)
                {
                    Marble = marble;
                    Event = @event;
                    Succeeded = string.IsNullOrWhiteSpace(assertionExceptionMessage);
                    AssertionExceptionMessage = assertionExceptionMessage;
                }

                public string Marble { get; }
                public object Event { get; }
                public bool Succeeded { get; }
                public string AssertionExceptionMessage { get; }
            }

            public List<List<Result>> Results { get; } = new List<List<Result>>();

            public override string ToString()
            {
                var result = new StringBuilder();
                result.AppendLine("Summary:");
                result.AppendLine("     " + string.Join("    ", Results.Select((row, index) => $"e_{index}")));
                for (var rowIndex = 0; rowIndex < Results.Count; rowIndex++)
                {
                    result.Append("m_");
                    result.Append(rowIndex);
                    result.Append("  ");
                    var row = Results[rowIndex];
                    for (var i = 0; i < row.Count; i++)
                    {
                        var cell = row[i].Succeeded ? "✔     " : $"x_{rowIndex}_{i}  ";
                        result.Append(cell);
                        if (i == (row.Count - 1))
                        {
                            result.AppendLine();
                        }
                    }
                }
                result.AppendLine();
                result.AppendLine("Marbles:");
                for (var rowIndex = 0; rowIndex < Results.Count; rowIndex++)
                {
                    var row = Results[rowIndex];
                    result.Append("m_");
                    result.Append(rowIndex);
                    result.Append(" : ");
                    result.AppendLine(row[0].Marble);
                }
                result.AppendLine();
                result.AppendLine("Events:");
                var firstRow = Results[0];
                for (var i = 0; i < firstRow.Count; i++)
                {
                    result.Append("e_");
                    result.Append(i);
                    result.Append(" : ");
                    result.AppendLine(firstRow[i].Event.ToString());
                }

                result.AppendLine();
                result.AppendLine("Failure messages:");
                for (var rowIndex = 0; rowIndex < Results.Count; rowIndex++)
                {
                    var row = Results[rowIndex];
                    for (var i = 0; i < row.Count; i++)
                    {
                        if (!row[i].Succeeded)
                        {
                            result.AppendLine($"x_{rowIndex}_{i} : {row[i].AssertionExceptionMessage}");
                        }
                    }

                }
                return result.ToString();
            }

            public bool AllRowsAtLeastOneSuccess()
            {
                return Results.All(row => row.Count(c => c.Succeeded) >= 1);
            }

            public bool AllCollumnsAtLeastOneSuccess()
            {
                var collums = new List<List<bool>>();
                for (var rowIndex = 0; rowIndex < Results.Count; rowIndex++)
                {
                    var row = Results[rowIndex];
                    for (var i = 0; i < row.Count; i++)
                    {
                        if (collums.Count < i)
                        {
                            collums.Add(new List<bool>(Results.Count));
                        }

                        collums[i][rowIndex] = row[i].Succeeded;
                    }
                }

                return collums.All(c => c.Count(r => r) >= 1);
            }
        }

        private readonly Func<TimeSpan, Task> _fastForward;
        private readonly Func<Task> _waitForIdle;
        protected List<ExpectedMarbles> Expectations = new List<ExpectedMarbles>();
        protected List<InputMarbles> Inputs = new List<InputMarbles>();
        private static Func<string, IEnumerable<Moment>> _parseSequenceFunc;

        public MarbleTest(
            Func<Task> waitForIdle,
            Func<TimeSpan, Task> fastForward,
            Func<string, IEnumerable<Moment>> parserFunc = null)
        {
            _waitForIdle = waitForIdle;
            _fastForward = fastForward;
            _parseSequenceFunc = parserFunc ?? MarbleParser.ParseSequence;
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
                Expectations.ForEach(etl => etl.VerifyNothingElse(time));
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

        public void Expect<TEvent>(
            string sequence,
            Func<FSharpOption<TEvent>> eventProducer,
            Action<string, TEvent> assertion)
        {
            var expectations = ParseSequence(sequence)
                .SelectMany(moment => CreateExpectations(moment, eventProducer, assertion));
            AddExpectations(new ExpectedMarbles(sequence, expectations));

        }

        private IEnumerable<ExpectedMarble> CreateExpectations<TEvent>(
            Moment moment,
            Func<FSharpOption<TEvent>> eventProducer,
            Action<string, TEvent> assertion)
        {
            var nothingElseMarble = new ExpectedMarble(moment.Time, null, ExpectNothing(eventProducer));
            var momentMarbles = new List<ExpectedMarble>();
            switch (moment.Type)
            {
                case Moment.MomentType.Empty:
                    break;
                case Moment.MomentType.Single:
                    var marble = moment.Marbles[0];
                    momentMarbles.Add(CreateSinglExpectedMarble(moment, eventProducer, assertion, marble));
                    break;
                case Moment.MomentType.OrderedGroup:
                    momentMarbles.Add(new ExpectedMarble(moment.Time, moment.ToString(), () =>
                    {
                        var produced = moment.Marbles.Select(_ => eventProducer()).ToList();
                        var received = produced.Where(FSharpOption<TEvent>.get_IsSome).Select(t => t.Value).ToList();
                        if (received.Count != moment.Marbles.Length)
                        {
                            throw new Exception(
                                $"Expecting an ordered group at moment '{moment}' with {moment.Marbles.Length} elements but got {received.Count} events: [{string.Join(" ", received)}]");
                        }


                        var exceptions = received.Zip(moment.Marbles, (@event, m) => CheckEvent(assertion, m, @event))
                            .Where(t => !t.Succeeded)
                            .Select(t => (t.Event, t.Expection, t.Marble)).ToList();
                        if (exceptions.Any())
                        {
                            var failures = string.Join($"{Environment.NewLine}- ",
                                exceptions.Select(e => $"Marble '{e.Marble}', event '{e.Event}', failure:{Environment.NewLine}    {e.Expection.Message}"));
                            throw new Exception(
                                $"Expecting an ordered group '{moment}' with {moment.Marbles.Length} elements but {exceptions.Count} " +
                                "events failed their assertion: " +
                                $"{Environment.NewLine}- {failures}");
                        }
                    }));
                    break;
                case Moment.MomentType.UnorderedGroup:
                    momentMarbles.Add(new ExpectedMarble(moment.Time, moment.ToString(), () =>
                    {
                        var produced = moment.Marbles.Select(_ => eventProducer()).ToList();
                        var received = produced.Where(FSharpOption<TEvent>.get_IsSome).Select(t => t.Value).ToList();
                        if (received.Count != moment.Marbles.Length)
                        {
                            throw new Exception(
                                $"Expecting an unordered group at moment '{moment}' with {moment.Marbles.Length} elements but got {received.Count} events: [{string.Join(" ", received)}]");
                        }

                        var table = new MarbleEventAssertionResultTable();
                        foreach (var m in moment.Marbles)
                        {
                            var row = new List<MarbleEventAssertionResultTable.Result>();
                            foreach (var @event in received)
                            {
                                var (succeeded, exception, _, _) = CheckEvent(assertion, m, @event);
                                row.Add(new MarbleEventAssertionResultTable.Result(m, @event, succeeded ? string.Empty : exception.Message));
                            }
                            table.Results.Add(row);
                        }

                        var success = table.AllRowsAtLeastOneSuccess() && table.AllCollumnsAtLeastOneSuccess();
                        if (!success)
                        {
                            throw new Exception(table.ToString());
                        }
                        var events = new Queue<TEvent>(received);
                        var toCheck = new List<string>(moment.Marbles);
                        var succeededEvents = new List<TEvent>();
                        var exceptions = new List<(TEvent Event, IList<Exception>, string Marble)>();
                        while (events.Count > 0)
                        {
                            var top = events.Dequeue();
                            var (succeeded, exceptionForEvent, m) = CheckEvent(assertion, top, toCheck);
                            if (succeeded)
                            {
                                succeededEvents.Add(top);
                            }
                            else
                            {
                                exceptions.Add((top, exceptionForEvent, m));
                            }
                        }

                        if (exceptions.Any())
                        {
                            var successes = succeededEvents.Count > 0
                                ? $"Found marbles [{string.Join(", ", succeededEvents)}]"
                                : string.Empty;
                            var failures = string.Join($"{Environment.NewLine}- ",
                                exceptions.Select(e =>
                                {
                                    var messages = string.Join($"{Environment.NewLine}    ",
                                        e.Item2.Select(ex => ex.Message));
                                    return $"Marble '{e.Marble}', event '{e.Event}', failures:{Environment.NewLine}    {messages}";
                                }));
                            throw new Exception(
                                $"Expecting an unordered group '{moment}' with {moment.Marbles.Length} elements. {Environment.NewLine}{successes}{Environment.NewLine}{exceptions.Count} " +
                                "marbles are missing: " +
                                $"{Environment.NewLine}- {failures}{Environment.NewLine}{successes}");
                        }
                    }));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }


            momentMarbles.Add(nothingElseMarble);
            return momentMarbles;
        }

        private static ExpectedMarble CreateSinglExpectedMarble<TEvent>(Moment moment, Func<FSharpOption<TEvent>> eventProducer, Action<string, TEvent> assertion, string marble)
        {
            return new ExpectedMarble(moment.Time, marble, () =>
            {
                var option = eventProducer();
                if (FSharpOption<TEvent>.get_IsSome(option))
                {
                    assertion(marble, option.Value);
                }
                else
                {
                    throw new Exception($"Expecting '{marble}' at moment '{moment.Time}', but there was no event.");
                }

            });
        }

        private static Action ExpectNothing<TEvent>(Func<FSharpOption<TEvent>> eventProducer)
        {
            return () =>
            {
                var option = eventProducer();
                if (FSharpOption<TEvent>.get_IsSome(option))
                {
                    throw new Exception($"Expecting no event but received '{option.Value}'");
                }
            };
        }

        private static (bool Succeeded, Exception Expection, string Marble, TEvent Event) CheckEvent<TEvent>(
            Action<string, TEvent> assertion,
            string marble,
            TEvent top)
        {
            try
            {
                assertion(marble, top);
                return (true, null, marble, top);
            }
            catch (Exception e)
            {
                return (false, e, marble, top);
            }
        }
        private static (bool Succeeded, IList<Exception>, string Marble) CheckEvent<TEvent>(
            Action<string, TEvent> assertion,
            TEvent top,
            ICollection<string> toCheck)
        {
            var exceptions = new List<Exception>();
            var localCopy = toCheck.ToArray();
            foreach (var marble in localCopy)
            {
                try
                {
                    assertion(marble, top);
                    toCheck.Remove(marble);
                    return (true, null, marble);
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }
            }

            return (false, exceptions, "?");
        }

        public void Expect(
            string sequence,
            Func<string, Action> assertionFactory,
            Action nothingElseAssertion,
            Func<Moment, ExpectedMarble> unorderedGroupExpectationCreator) =>
                Expect(
                    sequence,
                    assertionFactory,
                    nothingElseAssertion,
                    m => Enumerable.Repeat(unorderedGroupExpectationCreator(m), 1));

        public void Expect(
            string sequence,
            Func<string, Action> assertionFactory,
            Action nothingElseAssertion,
            Func<Moment, IEnumerable<ExpectedMarble>> unorderedGroupExpectationsCreator = null)
        {
            var expectations = ParseSequence(sequence)
                .SelectMany(moment => CreateExpectations(moment, assertionFactory, nothingElseAssertion, unorderedGroupExpectationsCreator));
            AddExpectations(new ExpectedMarbles(sequence, expectations));
        }

        protected static IEnumerable<ExpectedMarble> CreateExpectations(
            Moment moment,
            Func<string, Action> assertionFactory,
            Action nothingElseAssertion,
            Func<Moment, IEnumerable<ExpectedMarble>> unorderedGroupExpectationsCreator = null)
        {
            if (moment.Type == Moment.MomentType.UnorderedGroup)
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