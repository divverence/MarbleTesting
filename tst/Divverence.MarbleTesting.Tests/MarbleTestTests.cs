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

        [Fact]
        public async Task ExpectShouldDetectNoEvent()
        {
            var eventProducer = CreateProducer(null, null, null);
            _marbleTest.Expect(
                "-a-",
                eventProducer,
                MarbleShouldEqualActual);
            await _marbleTest.Run();
        }

        [Fact]
        public async Task ExpectShouldDetectUnexpectedEvents()
        {
            var eventProducer = CreateProducer("a", "b");
            _marbleTest.Expect(
                "-",
                eventProducer,
                MarbleShouldEqualActual);
            await _marbleTest.Run();
        }

        [Fact]
        public async Task ExpectShouldDistinguishNoEventFromWrongEvent()
        {
            var eventProducer = CreateProducer(null, "b", null);
            _marbleTest.Expect(
                "-a-",
                eventProducer,
                MarbleShouldEqualActual);
            await _marbleTest.Run();
        }

        [Fact]
        public async Task ExpectShouldHandleUnorderedGroups()
        {
            var eventProducer = CreateProducer(null, "Fred", "Wilma", "BamBam", "Barney", null);
            _marbleTest.Expect(
                "-<Fred Barney Wilma Rubbles>-",
                eventProducer,
                MarbleShouldEqualActual);
            await _marbleTest.Run();
        }

        [Fact]
        public async Task ExpectShouldHandleUnorderedGroupsWithNotEnoughEvents()
        {
            var eventProducer = CreateProducer(null, "a", null);
            _marbleTest.Expect(
                "-<a b>-",
                eventProducer,
                MarbleShouldEqualActual);
            await _marbleTest.Run();
        }

        [Fact]
        public async Task ExpectShouldHandleOrderedGroupsWithNonMatchingSecond()
        {
            var eventProducer = CreateProducer(null, "a", "c", null);
            _marbleTest.Expect(
                "-(a b)-",
                eventProducer,
                MarbleShouldEqualActual);
            await _marbleTest.Run();
        }

        [Fact]
        public async Task ExpectShouldHandleOrderedGroups()
        {
            var eventProducer = CreateProducer(null, "b", "a", null);
            _marbleTest.Expect(
                "-(a b)-",
                eventProducer,
                MarbleShouldEqualActual);
            await _marbleTest.Run();
        }

        [Fact]
        public async Task ExpectShouldHandleOrderedGroupsWhenNotEnoughElementsAreGiven()
        {
            var eventProducer = CreateProducer(null, "a", null, null);
            _marbleTest.Expect(
                "-(a b)-",
                eventProducer,
                MarbleShouldEqualActual);
            await _marbleTest.Run();
        }

        [Fact]
        public async Task ExpectShouldHandleUnorderedGroupsAndNothingElseAssertion()
        {
            var eventProducer = CreateProducer(null, "a", "b", "c", null);
            _marbleTest.Expect(
                "-<a b>-",
                eventProducer,
                MarbleShouldEqualActual);
            await _marbleTest.Run();
        }

        [Fact]
        public async Task ExpectShouldHandleOrderedGroupsAndNothingElseAssertion()
        {
            var eventProducer = CreateProducer(null, "a", "b", "c", null);
            _marbleTest.Expect(
                "-(a b)-",
                eventProducer,
                MarbleShouldEqualActual);
            await _marbleTest.Run();
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

        private static void Fail()
        {
            true.Should().BeFalse("no event expected");
        }

        private static void Ignore()
        {
        }
    }
}