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
            var actual = MarbleParser.ParseSequence(string.Empty);
            actual.Should().BeEmpty();
        }

        [Fact]
        public void Should_throw_ArgumentException_when_passing_null()
        {

            Action parsingNull = () => MarbleParser.ParseSequence(null);
            parsingNull.Should().Throw<ArgumentNullException>();
        }

        [Theory]
        [InlineData("((")]
        [InlineData("())")]
        [InlineData(")")]
        [InlineData("(")]
        public void Should_throw_ArgumentException_when_passing_wrong_parentheses(string marbleLine)
        {
            Action parsingNull = () => MarbleParser.ParseSequence(marbleLine);
            parsingNull.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData("<<")]
        [InlineData("<>>")]
        [InlineData(">")]
        [InlineData("<")]
        public void Should_throw_ArgumentException_when_passing_wrong_braces(string marbleLine)
        {
            Action parsingNull = () => MarbleParser.ParseSequence(marbleLine);
            parsingNull.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData("<a>")]
        [InlineData("(a)")]
        [InlineData("x<a>")]
        [InlineData("(a)x")]
        [InlineData("<>")]
        [InlineData("()")]
        [InlineData("x<>")]
        [InlineData("()x")]
        public void Should_throw_ArgumentException_when_having_single_element_or_empty_groups(string marbleLine)
        {
            Action parsingNull = () => MarbleParser.ParseSequence(marbleLine);
            parsingNull.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData("<<>>")]
        [InlineData("<()>")]
        [InlineData("(<>)")]
        public void Should_throw_ArgumentException_when_nesting_groups_braces(string marbleLine)
        {
            Action parsingNull = () => MarbleParser.ParseSequence(marbleLine);
            parsingNull.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData("^^")]
        [InlineData("(^)^")]
        [InlineData("(^)(^)")]
        [InlineData("^(^)")]
        [InlineData("(^^)")]
        [InlineData("<^>^")]
        [InlineData("<^><^>")]
        [InlineData("^<^>")]
        [InlineData("<^^>")]
        [InlineData("^abc^")]
        public void Should_throw_ArgumentException_when_passing_two_starters(string marbleLine)
        {
            Action parsingNull = () => MarbleParser.ParseSequence(marbleLine);
            parsingNull.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData("(-)")]
        [InlineData("(-^)")]
        [InlineData("(^-)")]
        [InlineData("(ab-)")]
        [InlineData("ab(-b)")]
        [InlineData("<->")]
        [InlineData("<-^>")]
        [InlineData("<^->")]
        [InlineData("<ab->")]
        [InlineData("ab<-b>")]
        public void Should_throw_ArgumentException_when_passing_dash_in_group(string marbleLine)
        {
            Action parsingNull = () => MarbleParser.ParseSequence(marbleLine);
            parsingNull.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Should_have_time_increasing_from_0()
        {
            var actual = MarbleParser.ParseSequence("abc");
            actual.Select(m => m.Time).Should().Equal(0, 1, 2);
        }

        [Fact]
        public void Start_at_beginning_behaves_like_normal_marble()
        {
            var actual = MarbleParser.ParseSequence("^");
            actual.Should().HaveCount(1);
            actual.Single().Time.Should().Be(0);
            actual.Single().Marbles.Should().Equal("^");
        }

        [Fact]
        public void Should_have_time_increasing_from_minus3()
        {
            var actual = MarbleParser.ParseSequence("abc^def");
            actual.Select(m => m.Time).Should().Equal(-3, -2, -1, 0, 1, 2, 3);
        }

        [Fact]
        public void Should_have_time_increasing_from_minus2()
        {
            var actual = MarbleParser.ParseSequence("ab(c^d)ef");
            actual.Select(m => string.Concat(m.Marbles)).Should().Equal("a", "b", "c^d", "e", "f");
            actual.Select(m => m.Time).Should().Equal(-2, -1, 0, 5, 6);
        }

        [Theory]
        [InlineData("a-(c^d)-f", true)]
        [InlineData("a-<c^d>-f", false)]
        public void Should_take_moment_of_group_start_as_0_time(string inputSequence, bool isOrderedGroup)
        {
            var actual = MarbleParser.ParseSequence(inputSequence);
            actual.First().Time.Should().Be(-2);
            var moment = actual.Skip(2).First();
            moment.IsOrderedGroup.Should().Be(isOrderedGroup);
            moment.Time.Should().Be(0);
        }

        [Fact]
        public void Supports_group_in_middle()
        {
            var actual = MarbleParser.ParseSequence("ab(cd)ef");
            actual.Select(m => string.Concat(m.Marbles)).Should().Equal("a", "b", "cd", "e", "f");
            actual.Select(m => m.Time).Should().Equal(0, 1, 2, 6, 7);
        }

        [Fact]
        public void Supports_unordered_group_in_middle()
        {
            var actual = MarbleParser.ParseSequence("ab<cd>ef").ToList();
            actual.Select(m => string.Concat(m.Marbles)).Should().Equal("a", "b", "cd", "e", "f");
            actual.Select(m => m.Time).Should().Equal(0, 1, 2, 6, 7);
            actual[2].IsOrderedGroup.Should().BeFalse();
        }

        [Fact]
        public void Supports_multiple_groups()
        {
            var actual = MarbleParser.ParseSequence("ab(cd)ef(gh)");
            actual.Select(m => string.Concat(m.Marbles)).Should().Equal("a", "b", "cd", "e", "f", "gh");
            actual.Select(m => m.Time).Should().Equal(0, 1, 2, 6, 7, 8);
        }

        [Fact]
        public void Supports_multiple_unordered_groups()
        {
            var actual = MarbleParser.ParseSequence("ab<cd>ef<gh>").ToList();
            actual.Select(m => string.Concat(m.Marbles)).Should().Equal("a", "b", "cd", "e", "f", "gh");
            actual.Select(m => m.Time).Should().Equal(0, 1, 2, 6, 7, 8);
            actual[2].IsOrderedGroup.Should().BeFalse();
            actual[5].IsOrderedGroup.Should().BeFalse();
        }

        [Theory]
        [InlineData("ab<cd>ef(gh)", 5, 2)]
        [InlineData("ab(cd)ef<gh>", 2, 5)]
        public void Supports_multiple_unordered_and_ordered_groups(string inputSequence, int orderedGroupIndex, int unorderedGroupIndex)
        {
            var actual = MarbleParser.ParseSequence(inputSequence).ToList();
            actual.Select(m => string.Concat(m.Marbles)).Should().Equal("a", "b", "cd", "e", "f", "gh");
            actual.Select(m => m.Time).Should().Equal(0, 1, 2, 6, 7, 8);
            actual[orderedGroupIndex].IsOrderedGroup.Should().BeTrue();
            actual[unorderedGroupIndex].IsOrderedGroup.Should().BeFalse();
        }

        [Fact]
        public void Supports_group_at_start()
        {
            var actual = MarbleParser.ParseSequence("(cd)ef");
            actual.Select(m => string.Concat(m.Marbles)).Should().Equal("cd", "e", "f");
            actual.Select(m => m.Time).Should().Equal(0, 4, 5);
        }

        [Fact]
        public void Supports_unordered_group_at_start()
        {
            var actual = MarbleParser.ParseSequence("<cd>ef").ToList();
            actual.Select(m => string.Concat(m.Marbles)).Should().Equal("cd", "e", "f");
            actual.Select(m => m.Time).Should().Equal(0, 4, 5);
            actual[0].IsOrderedGroup.Should().BeFalse();
        }

        [Fact]
        public void Supports_group_at_end()
        {
            var actual = MarbleParser.ParseSequence("ab(cd)");
            actual.Select(m => string.Concat(m.Marbles)).Should().Equal("a", "b", "cd");
            actual.Select(m => m.Time).Should().Equal(0, 1, 2);
        }

        [Fact]
        public void Supports_unordered_group_at_end()
        {
            var actual = MarbleParser.ParseSequence("ab<cd>").ToList();
            actual.Select(m => string.Concat(m.Marbles)).Should().Equal("a", "b", "cd");
            actual.Select(m => m.Time).Should().Equal(0, 1, 2);
            actual[2].IsOrderedGroup.Should().BeFalse();
        }

        [Fact]
        public void Should_return_one_marble_per_timeslot()
        {
            var actual = MarbleParser.ParseSequence("abc");
            actual.Select(m => m.Marbles.Length).Should().Equal(1, 1, 1);
        }

        [Fact]
        public void Should_return_correct_marble_per_timeslot()
        {
            var actual = MarbleParser.ParseSequence("abc");
            actual.Select(m => m.Marbles.FirstOrDefault()).Should().Equal("a", "b", "c");
        }

        [Fact]
        public void Should_return_empty_moment_for_dash()
        {
            var actual = MarbleParser.ParseSequence("-");
            actual.Should().HaveCount(1);
            actual.First().Marbles.Should().BeEmpty();
        }

        [Fact]
        public void Should_return_empty_moments_for_dash()
        {
            var actual = MarbleParser.ParseSequence("--");
            actual.Should().HaveCount(2);
            actual.First().Marbles.Should().BeEmpty();
            actual.Skip(1).First().Marbles.Should().BeEmpty();
        }

        [Fact]
        public void Should_use_single_moment_for_all_grouped_marbles()
        {
            var actual = MarbleParser.ParseSequence("(abc)");
            actual.Select(m => m.Time).Should().Equal(0);
            actual.Single().Marbles.Should().Equal("a", "b", "c");
        }

        [Fact]
        public void Spaces_are_dashes()
        {
            var actual = MarbleParser.ParseSequence("   a b  c -  (de)   f  ");
            var expected = MarbleParser.ParseSequence("---a-b--c----(de)---f--");
            actual.Select(m => m.Time).Should().Equal(expected.Select(m => m.Time));
            actual.Select(m => string.Concat(m.Marbles)).Should().Equal(expected.Select(m => string.Concat(m.Marbles)));
        }
    }
}
