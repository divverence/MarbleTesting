using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Divverence.MarbleTesting
{
    internal class MarbleEventAssertionResultTable
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

        private readonly IList<List<Result>> _results = new List<List<Result>>();

        public void AddResultsRow(IEnumerable<Result> resultRow)
        {
            _results.Add(resultRow.ToList());
        }

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

        public bool AllRowsAtLeastOneSuccess() => _results.All(row => row.Count(c => c.Succeeded) >= 1);

        public bool AllColumnsAtLeastOneSuccess() => Enumerable.Range(0, _results[0].Count).All(i => _results.Any(r => r[i].Succeeded));

        public bool MonotonicSuccess()
        {
            var maxSuccessIndex = -1;
            foreach (var t in _results)
            {
                var successIndex = t.FindIndex(r => r.Succeeded);
                if (successIndex <= maxSuccessIndex)
                {
                    return false;
                }
                maxSuccessIndex = successIndex;
            }
            return true;
        }

        private IEnumerable<(string FailureId, string FailureMessage)> AppendFailureTable(StringBuilder result)
        {
            var marbleColumnLength = _results.Max(row => row[0].Marble.Length);
            var toDumpFailures = new List<(string FailureId, string FailureMessage)>();
            var errorIndex = 'a';
            result.AppendLine("Summary:");
            result.Append(new string(' ', marbleColumnLength + 1));
            result.AppendLine(string.Join("   ", _results[0].Select((row, index) => $"e{index}")));
            foreach (var row in _results)
            {
                result.AppendFormat($"{{0,{marbleColumnLength}}} ", row[0].Marble);
                var wasMarbleSatisfied = row.Any(c => c.Succeeded);
                for (var i = 0; i < row.Count; i++)
                {
                    errorIndex = AppendCell(i, wasMarbleSatisfied, row, errorIndex, toDumpFailures, result);
                }

                result.AppendLine();
            }

            return toDumpFailures;
        }

        private static void AppendFailures(IEnumerable<(string FailureId, string FailureMessage)> toDumpFailures, StringBuilder result)
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
            var firstRow = _results[0];
            for (var i = 0; i < firstRow.Count; i++)
            {
                result.AppendLine($"e{i} : {firstRow[i].Event}");
            }
        }

        private char AppendCell(
            int columnIndex,
            bool wasMarbleSatisfied,
            IReadOnlyList<Result> row,
            char errorIndex,
            ICollection<(string FailureId, string FailureMessage)> toDumpFailures,
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
            return errorIndex;
        }

        private bool EventExpected(int eventIndex) => _results.Any(r => r[eventIndex].Succeeded);
    }
}