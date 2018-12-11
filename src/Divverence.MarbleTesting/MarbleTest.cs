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
                var toDumpFailures = AppendFailureTable(result);
                result.AppendLine();
                AppendEventLegend(result);
                result.AppendLine();
                AppendFailures(toDumpFailures, result);
                return result.ToString();
            }

            private List<(string FailureId, string FailureMessage)> AppendFailureTable(StringBuilder result)
            {
                var marbleColumnLength = Results.Max(row => row[0].Marble.Length);
                var toDumpFailures = new List<(string FailureId, string FailureMessage)>();
                var errorIndex = 'a';
                result.AppendLine("Summary:");
                result.Append(new string(' ', marbleColumnLength + 1));
                result.AppendLine(string.Join("   ", Results.Select((row, index) => $"e{index}")));
                for (var rowIndex = 0; rowIndex < Results.Count; rowIndex++)
                {
                    var row = Results[rowIndex];
                    result.AppendFormat($"{{0,{marbleColumnLength}}} ", row[0].Marble);
                    var wasMarbleSatisfied = row.Any(c => c.Succeeded);
                    for (var i = 0; i < row.Count; i++)
                    {
                        errorIndex = AppendCell(i, wasMarbleSatisfied, row, errorIndex, toDumpFailures, result);
                    }
                }

                return toDumpFailures;
            }

            private static void AppendFailures(List<(string FailureId, string FailureMessage)> toDumpFailures, StringBuilder result)
            {
                result.AppendLine("Failure messages:");
                foreach (var (failureId, failureMessage) in toDumpFailures)
                {
                    result.AppendLine($"{failureId}: {failureMessage}");
                }
            }

            private void AppendEventLegend(StringBuilder result)
            {
                result.AppendLine("Events:");
                var firstRow = Results[0];
                for (var i = 0; i < firstRow.Count; i++)
                {
                    result.AppendLine($"e{i} : {firstRow[i].Event}");
                }
            }

            private char AppendCell(
                int columnIndex,
                bool wasMarbleSatisfied, List<Result> row, char errorIndex, List<(string FailureId, string FailureMessage)> toDumpFailures,
                StringBuilder result)
            {
                var eventWasExpected = EventExpected(columnIndex);
                var problem = !(eventWasExpected && wasMarbleSatisfied);
                var cell = row[columnIndex].Succeeded ? "✔ " : !problem ? "❌ " : $"❌{errorIndex++}";
                if (problem)
                {
                    toDumpFailures.Add((cell, row[columnIndex].AssertionExceptionMessage));
                }

                result.Append(cell);
                result.Append("  ");
                if (columnIndex == row.Count - 1)
                {
                    result.AppendLine();
                }

                return errorIndex;
            }

            public bool AllRowsAtLeastOneSuccess() => Results.All(row => row.Count(c => c.Succeeded) >= 1);

            public bool AllColumnsAtLeastOneSuccess() => Enumerable.Range(0, Results[0].Count).All(i => Results.Any(r => r[i].Succeeded));

            private bool EventExpected(int eventIndex) => Results.Any(r => r[eventIndex].Succeeded);
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
            var nothingElseMarble = new ExpectedMarble(moment.Time, null, ExpectNothing(eventProducer, moment));
            var momentMarbles = new List<ExpectedMarble>();
            switch (moment.Type)
            {
                case Moment.MomentType.Empty:
                    break;
                case Moment.MomentType.Single:
                    var marble = moment.Marbles[0];
                    momentMarbles.Add(CreateSingleExpectedMarble(moment, eventProducer, assertion, marble));
                    break;
                case Moment.MomentType.OrderedGroup:
                    momentMarbles.Add(new ExpectedMarble(moment.Time, moment.ToString(), () =>
                    {
                        var produced = moment.Marbles.Select(_ => eventProducer()).ToList();
                        var received = produced.Where(FSharpOption<TEvent>.get_IsSome).Select(t => t.Value).ToList();
                        if (received.Count < moment.Marbles.Length)
                        {
                            throw new MissingEventException(
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

                        var success = table.AllRowsAtLeastOneSuccess() && table.AllColumnsAtLeastOneSuccess();
                        if (!success)
                        {
                            throw new Exception(table.ToString());
                        }
                    }));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }


            momentMarbles.Add(nothingElseMarble);
            return momentMarbles;
        }

        private static ExpectedMarble CreateSingleExpectedMarble<TEvent>(Moment moment, Func<FSharpOption<TEvent>> eventProducer, Action<string, TEvent> assertion, string marble)
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
                    throw new MissingEventException($"Expecting an event for '{marble}' at moment '{moment.Time}', but there was no event");
                }
            });
        }

        private static Action ExpectNothing<TEvent>(Func<FSharpOption<TEvent>> eventProducer, Moment moment)
        {
            return () =>
            {
                var received = new List<TEvent>();
                FSharpOption<TEvent> produced;
                while (FSharpOption<TEvent>.get_IsSome(produced = eventProducer()))
                {
                    received.Add(produced.Value);
                }
                if (received.Any())
                {
                    var receivedList = string.Join(", ", received);
                    string message;
                    switch (moment.Type)
                    {
                        case Moment.MomentType.Empty:
                            message = $"Expecting no events for marble '{moment}' at moment '{moment.Time}' but received [{receivedList}]";
                            break;
                        case Moment.MomentType.Single:
                            message = $"Expecting single event for marble '{moment}' at moment '{moment.Time}' but received [{receivedList}]";
                            break;
                        case Moment.MomentType.OrderedGroup:
                            message = $"Expecting an ordered set of events for marble '{moment}' at moment '{moment.Time}' but received superfluous events [{receivedList}]";
                            break;
                        case Moment.MomentType.UnorderedGroup:
                            message = $"Expecting an unordered set of events for marble '{moment}' at moment '{moment.Time}' but received superfluous events [{receivedList}]";
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    throw new UnexpectedEventsException(message);
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