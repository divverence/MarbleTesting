using FluentAssertions;
using Microsoft.FSharp.Core;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Divverence.MarbleTesting.Tests
{
    public class MarbleTestTests
    {
        private readonly MarbleTest _marbleTest;

        public MarbleTestTests()
        {
            _marbleTest = new MarbleTest(() => Task.FromResult(true), _ => Task.FromResult(true), MultiCharMarbleParser.ParseSequence);
        }

        private Func<Task> RunMableTest
        {
            get { return () => _marbleTest.Run(); }
        }

        [Fact]
        public void ExpectShouldDetectNoEvent() =>
            AssertDiagnosedFailure(
                expectMessageToContain: "marble 'a' not all expected events were received",
                sequence: "-a-",
                null, null, null);

        [Fact]
        public void ExpectShouldDetectUnexpectedEvents() =>
            AssertDiagnosedFailure(
                expectMessageToContain: "unexpected events were received",
                sequence: "-",
                "a", "b");

        [Fact]
        public void ExpectShouldFailWhenWrongEventIsReceived() =>
            AssertDiagnosedFailure(
                expectMessageToContain: "marble 'a' its assertion was not satisfied",
                sequence: "-a-",
                null, "b", null);

        [Fact]
        public void ExpectShouldHandleUnorderedGroups() =>
            AssertDiagnosedFailure(
                expectMessageToContain: "its assertion was not satisfied",
                sequence: "-<Fred Barney Wilma Rubbles>-",
                null, "Fred", "Wilma", "BamBam", "Barney", null);

        [Fact]
        public void ExpectShouldHandleUnorderedGroupsWithNotEnoughEvents() =>
            AssertDiagnosedFailure(
                expectMessageToContain: "its assertion was not satisfied",
                sequence: "-<a b>-",
                null, "a", null);

        [Fact]
        public void ExpectShouldHandleOrderedGroupsWithNonMatchingSecond() =>
            AssertDiagnosedFailure(
                expectMessageToContain: "its assertion was not satisfied",
                sequence: "-(a b)-",
                null, "a", "c", null);

        [Fact]
        public void ExpectShouldHandleOrderedGroups() =>
            AssertDiagnosedFailure(
                expectMessageToContain: "its assertion was not satisfied",
                sequence: "-(a b)-",
                null, "b", "a", null);

        [Fact]
        public void ExpectShouldHandleOrderedGroupsWhenNotEnoughElementsAreGiven() =>
            AssertDiagnosedFailure(
                expectMessageToContain: "not all expected events were received",
                sequence: "-(a b)-",
                null, "a", null, null);

        [Fact]
        public void ExpectShouldHandleUnorderedGroupsAndNothingElseAssertion() =>
            AssertDiagnosedFailure(
                expectMessageToContain: " unexpected events were received",
                sequence: "-<a b>-",
                null, "a", "b", "c", null);

        [Fact]
        public void ExpectShouldHandleOrderedGroupsAndNothingElseAssertion() =>
            AssertDiagnosedFailure(
                expectMessageToContain: " unexpected events were received",
                sequence: "-(a b)-",
                null, "a", "b", "c", null);

        private void AssertDiagnosedFailure(
            string expectMessageToContain,
            string sequence,
            params string[] toProduce)
        {
            var eventProducer = CreateProducer(toProduce);
            _marbleTest.Expect(
                sequence,
                eventProducer,
                MarbleShouldEqualActual);
            RunMableTest.Should().Throw<Exception>()
                .And
                .Message.Should().Contain(expectMessageToContain);
        }

        private static Func<FSharpOption<string>> CreateProducer(params string[] toSend)
        {
            var index = 0;
            return () =>
            {
                if (toSend.Length <= index)
                {
                    return FSharpOption<string>.None;
                }
                var s = toSend[index++];
                if (s == null)
                {
                    return FSharpOption<string>.None;
                }

                return FSharpOption<string>.Some(s);
            };
        }

        private static void MarbleShouldEqualActual(string marble, string actual)
        {
            actual.Should().Be(marble);
        }
    }
}