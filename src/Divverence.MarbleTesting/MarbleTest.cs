using Microsoft.FSharp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Divverence.MarbleTesting
{
    public class MarbleTest
    {
        private static Func<string, IEnumerable<Moment>> _parseSequenceFunc;
        private readonly Func<TimeSpan, Task> _fastForward;
        private readonly Func<Task> _waitForIdle;
        private readonly List<ExpectedMarbles> _expectations = new List<ExpectedMarbles>();
        private readonly List<InputMarbles> _inputs = new List<InputMarbles>();

        public MarbleTest(
            Func<Task> waitForIdle,
            Func<TimeSpan, Task> fastForward,
            Func<string, IEnumerable<Moment>> parserFunc = null)
        {
            _waitForIdle = waitForIdle;
            _fastForward = fastForward;
            _parseSequenceFunc = parserFunc ?? MarbleParser.ParseSequence;
        }

        public async Task Run(TimeSpan? interval = null)
        {
            var maxTime = _expectations.Max(etl => etl.LastTime);
            var minTime = _expectations.Min(etl => etl.FirstTime);
            for (var time = minTime; time <= maxTime; time++)
            {
                var localTime = time;
                await Task.WhenAll(_inputs.Select(atl => atl.Run(localTime)));
                await SystemIdle;
                _expectations.ForEach(etl => etl.Verify(time));
                _expectations.ForEach(etl => etl.VerifyNothingElse(time));
                if (interval.HasValue)
                    await FastForward(interval.Value);
            }
        }

        public void WhenDoing(string sequence, Func<string, Task> whatToDo)
        {
            var actions = ParseSequence(sequence).SelectMany(moment => CreateInputMarbles(moment, whatToDo));
            _inputs.Add(new InputMarbles(sequence, actions));
        }

#pragma warning disable 1998 // Seems the best way to convert Action<string> into a Func<string,Task> ...


        public void WhenDoing(string sequence, Action<string> whatToDo)
            => WhenDoing(sequence, async marble => whatToDo(marble));

#pragma warning restore 1998

        public void Assert(
            string sequence,
            Action<string> assertion)
        {
            AddExpectations(
                sequence,
                mom => CreateAssertionExpectations(mom, assertion));
        }

        public void Expect<TEvent>(
            string sequence,
            Func<FSharpOption<TEvent>> eventProducer,
            Action<string, TEvent> assertion)
        {
            AddExpectations(
                sequence,
                mom => CreateExpectations(mom, eventProducer, assertion));
        }

        public void LooselyExpect<TEvent>(
            string sequence,
            Func<FSharpOption<TEvent>> eventProducer,
            Action<string, TEvent> assertion)
        {
            AddExpectations(
                sequence,
                mom => CreateLooseExpectations(mom, eventProducer, assertion));
        }

        private static IEnumerable<InputMarble> CreateInputMarbles(Moment moment, Func<string, Task> whatToDo) =>
            moment.Marbles.Select(marble => new InputMarble(moment.Time, marble, () => whatToDo(marble)));

        private static IEnumerable<Moment> ParseSequence(string sequence) =>
            _parseSequenceFunc(sequence);

        private Task FastForward(TimeSpan howMuch) =>
            _fastForward(howMuch);

        private Task SystemIdle =>
            _waitForIdle();

        private void AddExpectations(
            string sequence,
            Func<Moment, IEnumerable<ExpectedMarble>> marbleFactory)
        {
            var expectations = ParseSequence(sequence)
                .SelectMany(marbleFactory);
            _expectations.Add(new ExpectedMarbles(sequence, expectations));
        }

        private static IEnumerable<ExpectedMarble> CreateAssertionExpectations(
            Moment moment, 
            Action<string> assertion) =>
                moment.Marbles.Select(marble => new ExpectedMarble(moment.Time, marble, () => assertion(marble)));

        private static IEnumerable<ExpectedMarble> CreateExpectations<TEvent>(
            Moment moment,
            Func<FSharpOption<TEvent>> eventProducer,
            Action<string, TEvent> assertion)
        {
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
                    momentMarbles.Add(CreateOrderedGroupExpectedMarble(moment, eventProducer, assertion));
                    break;
                case Moment.MomentType.UnorderedGroup:
                    momentMarbles.Add(CreateUnorderedGroupExpectedMarble(moment, eventProducer, assertion));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            momentMarbles.Add(
                new ExpectedMarble(
                    moment.Time,
                    null,
                    FlushProducerAndAssertIfRequiredActionCreator(eventProducer, moment)));
            return momentMarbles;
        }

        private static IEnumerable<ExpectedMarble> CreateLooseExpectations<TEvent>(
            Moment moment,
            Func<FSharpOption<TEvent>> eventProducer,
            Action<string, TEvent> assertion)
        {
            var momentMarbles = new List<ExpectedMarble>();
            switch (moment.Type)
            {
                case Moment.MomentType.Empty:
                    break;
                case Moment.MomentType.Single:
                    var marble = moment.Marbles[0];
                    momentMarbles.Add(CreateSingleLooselyExpectedMarble(moment, eventProducer, assertion, marble));
                    break;
                case Moment.MomentType.OrderedGroup:
                    momentMarbles.Add(CreateOrderedGroupLooselyExpectedMarble(moment, eventProducer, assertion));
                    break;
                case Moment.MomentType.UnorderedGroup:
                    momentMarbles.Add(CreateSingleLooselyExpectedMarble(moment, eventProducer, assertion, moment.ToString()));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            momentMarbles.Add(
                new ExpectedMarble(
                    moment.Time,
                    null,
                    () => ExhaustProducer(eventProducer)));
            return momentMarbles;
        }

        private static ExpectedMarble CreateUnorderedGroupExpectedMarble<TEvent>(
            Moment moment,
            Func<FSharpOption<TEvent>> eventProducer,
            Action<string, TEvent> assertion) =>
                new ExpectedMarble(
                    moment.Time,
                    moment.ToString(),
                    () =>
                        {
                            var produced = moment.Marbles.Select(_ => eventProducer()).ToList();
                            var received = produced.Where(FSharpOption<TEvent>.get_IsSome).Select(t => t.Value).ToList();
                            if (received.Count != moment.Marbles.Length)
                            {
                                throw new Exception(
                                    $"At time {moment.Time}, expecting an unordered group {moment} with {moment.Marbles.Length} elements but got {received.Count} events: [{string.Join(" ", received)}]");
                            }

                            var table = FillAssertionResultTable(moment, assertion, received);
                            var success = table.AllRowsAtLeastOneSuccess() && table.AllColumnsAtLeastOneSuccess();
                            if (!success)
                            {
                                throw new Exception(table.ToString());
                            }
                        });

        private static MarbleEventAssertionResultTable FillAssertionResultTable<TEvent>(
            Moment moment,
            Action<string, TEvent> assertion,
            IList<TEvent> received)
        {
            var table = new MarbleEventAssertionResultTable();
            foreach (var m in moment.Marbles)
            {
                var row = new List<MarbleEventAssertionResultTable.Result>();
                foreach (var @event in received)
                {
                    var (succeeded, exception, _, _) = CheckEvent(assertion, m, @event);
                    row.Add(new MarbleEventAssertionResultTable.Result(m, @event,
                        succeeded ? string.Empty : exception.Message));
                }
                table.Results.Add(row);
            }
            return table;
        }

        private static ExpectedMarble CreateOrderedGroupExpectedMarble<TEvent>(
            Moment moment,
            Func<FSharpOption<TEvent>> eventProducer,
            Action<string, TEvent> assertion) =>
                new ExpectedMarble(
                    moment.Time,
                    moment.ToString(),
                    () =>
                        {
                            var produced = moment.Marbles.Select(_ => eventProducer()).ToList();
                            var received = produced.Where(FSharpOption<TEvent>.get_IsSome).Select(t => t.Value).ToList();
                            if (received.Count < moment.Marbles.Length)
                            {
                                throw new MissingEventException(
                                    $"At time {moment.Time}, expecting an ordered group {moment} with {moment.Marbles.Length} elements but got {received.Count} events: [{string.Join(" ", received)}]");
                            }


                            var exceptions = received.Zip(moment.Marbles, (@event, m) => CheckEvent(assertion, m, @event))
                                .Where(t => !t.Succeeded)
                                .Select(t => (t.Event, t.Expection, t.Marble)).ToList();
                            if (exceptions.Any())
                            {
                                var failures = string.Join($"{Environment.NewLine}- ",
                                    exceptions.Select(e => $"Marble '{e.Marble}', event '{e.Event}', failure:{Environment.NewLine}    {e.Expection.Message}"));
                                throw new Exception(
                                    $"At time {moment.Time}, expecting an ordered group {moment} with {moment.Marbles.Length} elements but {exceptions.Count} " +
                                    "events failed their assertion: " +
                                    $"{Environment.NewLine}- {failures}");
                            }
                        });

        private static ExpectedMarble CreateOrderedGroupLooselyExpectedMarble<TEvent>(
            Moment moment,
            Func<FSharpOption<TEvent>> eventProducer,
            Action<string, TEvent> assertion) =>
                new ExpectedMarble(
                    moment.Time,
                    moment.ToString(),
                    () =>
                    {
                        var produced = ExhaustProducer(eventProducer);
                        if (produced.Count < moment.Marbles.Length)
                        {
                            throw new MissingEventException(
                                $"At time {moment.Time}, expecting an ordered group {moment} with {moment.Marbles.Length} elements but got {produced.Count} events: [{string.Join(" ", produced)}]");
                        }

                        var table = FillAssertionResultTable(moment, assertion, produced);
                        if (!(table.AllRowsAtLeastOneSuccess() && table.MonotonicSuccess()))
                        {
                            throw new Exception(table.ToString());
                        }
                    });

        private static ExpectedMarble CreateSingleExpectedMarble<TEvent>(
            Moment moment,
            Func<FSharpOption<TEvent>> eventProducer,
            Action<string, TEvent> assertion,
            string marble) =>
                new ExpectedMarble(
                    moment.Time,
                    marble,
                    () =>
                        {
                            var option = eventProducer();
                            if (FSharpOption<TEvent>.get_IsSome(option))
                            {
                                assertion(marble, option.Value);
                            }
                            else
                            {
                                throw new MissingEventException($"At time {moment.Time}, an event for '{marble}' was expected, but there was no event");
                            }
                        });

        private static ExpectedMarble CreateSingleLooselyExpectedMarble<TEvent>(
            Moment moment,
            Func<FSharpOption<TEvent>> eventProducer,
            Action<string, TEvent> assertion,
            string marble) =>
                new ExpectedMarble(
                    moment.Time,
                    marble,
                    () =>
                        {
                            var received = ExhaustProducer(eventProducer);
                            if (received.Any())
                            {
                                var table = FillAssertionResultTable(moment, assertion, received);
                                var success = table.AllRowsAtLeastOneSuccess();
                                if (!success)
                                {
                                    throw new Exception(table.ToString());
                                }
                            }
                            else
                            {
                                throw new MissingEventException($"At time {moment.Time}, an event for '{marble}' was expected, but there was no event");
                            }
                        });

        private static Action FlushProducerAndAssertIfRequiredActionCreator<TEvent>(
            Func<FSharpOption<TEvent>> eventProducer,
            Moment moment) =>
            () =>
                {
                    var received = ExhaustProducer(eventProducer);
                    if (received.Any())
                    {
                        var receivedList = string.Join(", ", received);
                        string message;
                        switch (moment.Type)
                        {
                            case Moment.MomentType.Empty:
                                message = $"At time {moment.Time}, expected no events but received [{receivedList}]";
                                break;
                            case Moment.MomentType.Single:
                                message = $"At time {moment.Time}, expected a single event for marble '{moment}' but received [{receivedList}]";
                                break;
                            case Moment.MomentType.OrderedGroup:
                                message = $"At time {moment.Time}, expected ordered group '{moment}' but received superfluous events [{receivedList}]";
                                break;
                            case Moment.MomentType.UnorderedGroup:
                                message = $"At time {moment.Time}, expected unordered group '{moment}' but received superfluous events [{receivedList}]";
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                        throw new UnexpectedEventsException(message);
                    }
                };

        private static IList<TEvent> ExhaustProducer<TEvent>(Func<FSharpOption<TEvent>> eventProducer)
        {
            var received = new List<TEvent>();
            FSharpOption<TEvent> produced;
            while (FSharpOption<TEvent>.get_IsSome(produced = eventProducer()))
            {
                received.Add(produced.Value);
            }
            return received;
        }

        private static (bool Succeeded, Exception Expection, string Marble, TEvent Event) CheckEvent<TEvent>(
            Action<string, TEvent> assertion,
            string marble,
            TEvent @event)
        {
            try
            {
                assertion(marble, @event);
                return (true, null, marble, @event);
            }
            catch (Exception e)
            {
                return (false, e, marble, @event);
            }
        }
    }
}