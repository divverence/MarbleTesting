using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.TestKit.Xunit2;
using Divverence.MarbleTesting.Akka.Async;
using FluentAssertions;
using Xunit;

namespace Divverence.MarbleTesting.Akka.Tests
{
    public class AkkaMarbleTestTests : TestKit
    {
        public sealed class MapperActor : ReceiveActor
        {
            public static Props Props(IDictionary<string, string[]> mapping, IActorRef testProbe) => global::Akka.Actor.Props.Create(
                () => new MapperActor(mapping, testProbe));

            public MapperActor(IDictionary<string, string[]> mapping, IActorRef probe)
            {
                Receive<string>(s =>
                {
                    foreach (var mapped in mapping[s])
                    {
                        probe.Tell(mapped);
                    }
                });
            }
        }

        public AkkaMarbleTestTests() : base(ConfigExtensionsForAsyncTesting.AwaitableTaskDispatcherConfig)
        {
        }

        public static IEnumerable<object[]> UnorderedGroupTestInput()
        {
            yield return new object[] { ("a", new[] { "a" }), ("b", new[] { "c", "b", "e", "f", "d", "g", "i", "h" }), ("j", new[] { "j" }) };
            yield return new object[] { ("a", new[] { "a" }), ("b", new[] { "b", "c", "i", "d", "g", "f", "h", "e" }), ("j", new[] { "j" }) };
        }

        [Theory]
        [MemberData(nameof(UnorderedGroupTestInput))]
        public async Task Should_handle_unordered_group_as_unordered(params (string Key, string[] ToSend)[] inputMappings)
        {
            var marbleTest = SetupMarbleTest(
                "ab---------j",
                "a<bcdefghi>j",
                inputMappings);
            await marbleTest.Run();
        }

        public static IEnumerable<object[]> UnorderedGroupTestInputFailureCases()
        {
            yield return new object[] { ("a", new[] { "a" }), ("b", new[] { "c", "c" }), ("d", new[] { "d" }) };
            yield return new object[] { ("a", new[] { "a" }), ("b", new[] { "b", "b" }), ("d", new[] { "d" }) };
            yield return new object[] { ("a", new[] { "a" }), ("b", new[] { "b" }), ("d", new[] { "d" }) };
            yield return new object[] { ("a", new[] { "a" }), ("b", new[] { "c" }), ("d", new[] { "d" }) };
            yield return new object[] { ("a", new[] { "a" }), ("b", new[] { "q", "s" }), ("d", new[] { "d" }) };
            yield return new object[] { ("a", new[] { "a" }), ("b", new[] { "q" }), ("d", new[] { "d" }) };
        }

        [Theory]
        [MemberData(nameof(UnorderedGroupTestInputFailureCases))]
        public async Task Should_report_error(params (string Key, string[] ToSend)[] inputMappings)
        {
            var marbleTest = SetupMarbleTest(
                "ab---d",
                "a<bc>d",
                inputMappings);
            var actual = await Assert.ThrowsAsync<Exception>(() => marbleTest.Run());
            Assert.NotNull(actual.InnerException);
        }

        private AkkaMarbleTest SetupMarbleTest(string inputSequence, string expectedOutputSequence, params (string Key, string[] ToSend)[] inputMappings)
        {
            var inputMapping = inputMappings.ToDictionary(valueTuple => valueTuple.Key, valueTuple => valueTuple.ToSend);
            var testProbe = CreateTestProbe(Sys, "OutputProbe");
            var mapperActor = Sys.ActorOf(MapperActor.Props(inputMapping, testProbe), "Mapper");
            var marbleTest = new AkkaMarbleTest(Sys);
            marbleTest.WhenTelling(
                inputSequence, mapperActor, s => s);
            marbleTest.ExpectMsgs<string>(
                expectedOutputSequence, testProbe, s => o => o.Should().Be(s));
            return marbleTest;
        }
    }
}
