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

        [Theory]
        [InlineData("((")]
        [InlineData("())")]
        [InlineData(")")]
        public void Should_throw_ArgumentException_when_passing_wrong_parentheses( string marbleLine )
        {
            Action parsingNull = () => MarbleParser.ParseMarbles(marbleLine);
            parsingNull.ShouldThrow<ArgumentException>();
        }

        [Theory]
        [InlineData("^^")]
        [InlineData("(^)^")]
        [InlineData("(^)(^)")]
        [InlineData("^(^)")]
        [InlineData("(^^)")]
        [InlineData("^abc^")]
        public void Should_throw_ArgumentException_when_passing_two_starters(string marbleLine)
        {
            Action parsingNull = () => MarbleParser.ParseMarbles(marbleLine);
            parsingNull.ShouldThrow<ArgumentException>();
        }

        [Theory]
        [InlineData("(-)")]
        [InlineData("(-^)")]
        [InlineData("(^-)")]
        [InlineData("(ab-)")]
        [InlineData("ab(-b)")]
        public void Should_throw_ArgumentException_when_passing_dash_in_group(string marbleLine)
        {
            Action parsingNull = () => MarbleParser.ParseMarbles(marbleLine);
            parsingNull.ShouldThrow<ArgumentException>();
        }

        [Fact]
        public void Should_have_time_increasing_from_0()
        {
            var actual = MarbleParser.ParseMarbles("abc");
            actual.Select(m => m.Time).Should().BeEquivalentTo(new[] {0, 1, 2});
        }

        [Fact]
        public void Start_at_beginning_behaves_like_normal_marble()
        {
            var actual = MarbleParser.ParseMarbles("^");
            actual.Should().HaveCount(1);
            actual.Single().Time.Should().Be(0);
            actual.Single().Marbles.Should().BeEquivalentTo("^");
        }

        [Fact]
        public void Should_have_time_increasing_from_minus3()
        {
            var actual = MarbleParser.ParseMarbles("abc^def");
            actual.Select(m => m.Time).Should().BeEquivalentTo(new[] { -3, -2, -1, 0, 1, 2, 3 });
        }

        [Fact]
        public void Should_have_time_increasing_from_minus2()
        {
            var actual = MarbleParser.ParseMarbles("ab(c^d)ef");
            actual.Select(m => string.Concat(m.Marbles)).Should().BeEquivalentTo("a", "b", "c^d", "e", "f");
            actual.Select(m => m.Time).Should().BeEquivalentTo(new[] { -2, -1, 0, 5, 6 });
        }

        [Fact]
        public void Supports_group_in_middle()
        {
            var actual = MarbleParser.ParseMarbles("ab(cd)ef");
            actual.Select(m => string.Concat(m.Marbles)).Should().BeEquivalentTo("a", "b", "cd", "e", "f");
            actual.Select(m => m.Time).Should().BeEquivalentTo(new[] {0, 1, 2, 6, 7});
        }

        [Fact]
        public void Supports_multiple_groups()
        {
            var actual = MarbleParser.ParseMarbles("ab(cd)ef(gh)");
            actual.Select(m => string.Concat(m.Marbles)).Should().BeEquivalentTo("a", "b", "cd", "e", "f", "gh");
            actual.Select(m => m.Time).Should().BeEquivalentTo(new[] { 0, 1, 2, 6, 7, 8 });
        }

        [Fact]
        public void Supports_group_at_start()
        {
            var actual = MarbleParser.ParseMarbles("(cd)ef");
            actual.Select(m => string.Concat(m.Marbles)).Should().BeEquivalentTo("cd", "e", "f");
            actual.Select(m => m.Time).Should().BeEquivalentTo(new[] { 0, 4, 5 });
        }

        [Fact]
        public void Supports_group_at_end()
        {
            var actual = MarbleParser.ParseMarbles("ab(cd)");
            actual.Select(m => string.Concat(m.Marbles)).Should().BeEquivalentTo("a", "b", "cd");
            actual.Select(m => m.Time).Should().BeEquivalentTo(new[] { 0, 1, 2 });
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

        [Fact]
        public void Should_use_single_moment_for_all_grouped_marbles()
        {
            var actual = MarbleParser.ParseMarbles("(abc)");
            actual.Select(m => m.Time).Should().BeEquivalentTo(new[] { 0 });
            actual.Single().Marbles.Should().BeEquivalentTo("a", "b", "c");
        }

        [Fact]
        public void Should_ignore_spaces()
        {
            var actual = MarbleParser.ParseMarbles("   a b  c -  (de)   f  ");
            var expected = MarbleParser.ParseMarbles("abc-(de)f");
            actual.Select(m => m.Time).Should().BeEquivalentTo(expected.Select(m => m.Time));
            actual.Select(m => string.Concat(m.Marbles)).Should().BeEquivalentTo(expected.Select(m => string.Concat(m.Marbles)));
        }
    }
}
