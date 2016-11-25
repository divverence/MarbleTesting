using System;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Divverence.MarbleTesting.Tests
{
    public class MarbleParserTest
    {
        [Fact]
        public void Should_accept_empty_string()
        {
            var actual = MarbleParser.ParseMarbles(string.Empty);
            actual.Should().BeEmpty();
        }

        [Fact]
        public void Should_throw_ArgumentException_when_passing_null()
        {
            
            Action parsingNull = () => MarbleParser.ParseMarbles(null);
            parsingNull.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Should_have_time_increasing_from_0()
        {
            var actual = MarbleParser.ParseMarbles("abc");
            actual.Select(m => m.Time).Should().BeEquivalentTo(new[] {0, 1, 2});
        }

        [Fact]
        public void Should_return_one_marble_per_timeslot()
        {
            var actual = MarbleParser.ParseMarbles("abc");
            actual.Select(m => m.Marbles.Length).Should().BeEquivalentTo(new[] { 1, 1, 1 });
        }

        [Fact]
        public void Should_return_correct_marble_per_timeslot()
        {
            var actual = MarbleParser.ParseMarbles("abc");
            actual.Select(m => m.Marbles.FirstOrDefault()).Should().BeEquivalentTo("a", "b", "c");
        }

        [Fact]
        public void Should_return_empty_moment_for_dash()
        {
            var actual = MarbleParser.ParseMarbles("-");
            actual.Should().HaveCount(1);
            actual.First().Marbles.Should().BeEmpty();
        }

        [Fact]
        public void Should_return_empty_moments_for_dash()
        {
            var actual = MarbleParser.ParseMarbles("--");
            actual.Should().HaveCount(2);
            actual.First().Marbles.Should().BeEmpty();
            actual.Skip(1).First().Marbles.Should().BeEmpty();
        }
    }
}
