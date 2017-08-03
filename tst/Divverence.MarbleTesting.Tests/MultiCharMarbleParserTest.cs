using System;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Divverence.MarbleTesting.Tests
{
    public class MultiCharMarbleParserTest
    {
        [Fact]
        public void Should_accept_empty_string()
        {
            var actual = MultiCharMarbleParser.ParseSequence(string.Empty);
            actual.Should().BeEmpty();
        }

        [Fact]
        public void Should_throw_ArgumentException_when_passing_null()
        {

            Action parsingNull = () => MultiCharMarbleParser.ParseSequence(null);
            parsingNull.ShouldThrow<ArgumentNullException>();
        }

        [Theory]
        [InlineData("((")]
        [InlineData("())")]
        [InlineData(")")]
        [InlineData("(")]
        public void Should_throw_ArgumentException_when_passing_wrong_parentheses(string marbleLine)
        {
            Action parsingNull = () => MultiCharMarbleParser.ParseSequence(marbleLine);
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
            Action parsingNull = () => MultiCharMarbleParser.ParseSequence(marbleLine);
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
            Action parsingNull = () => MultiCharMarbleParser.ParseSequence(marbleLine);
            parsingNull.ShouldThrow<ArgumentException>();
        }

        [Theory]
        [InlineData(",")]
        [InlineData("a,b")]
        [InlineData(",a-,a")]
        [InlineData("(a,b),")]
        public void Should_throw_ArgumentException_when_passing_comma_outside_group(string marbleLine)
        {
            Action parsingNull = () => MultiCharMarbleParser.ParseSequence(marbleLine);
            parsingNull.ShouldThrow<ArgumentException>();
        }

        [Fact]
        public void Should_have_time_increasing_from_0()
        {
            var actual = MultiCharMarbleParser.ParseSequence("a-c");
            actual.Select(m => m.Time).Should().Equal(0, 1, 2);
        }

        [Fact]
        public void Start_at_beginning_behaves_like_normal_marble()
        {
            var actual = MultiCharMarbleParser.ParseSequence("^");
            actual.Should().HaveCount(1);
            actual.Single().Time.Should().Be(0);
            actual.Single().Marbles.Should().Equal("^");
        }

        [Fact]
        public void Should_take_moment_of_circumflex_as_0_time()
        {
            var actual = MultiCharMarbleParser.ParseSequence("a-c^d-f");
            actual.First().Time.Should().Be(-3);
            actual.Skip(3).First().Time.Should().Be(0);
            actual.Skip(3).First().Marbles.Should().Equal("^");
        }

        [Fact]
        public void Should_take_moment_of_group_start_as_0_time()
        {
            var actual = MultiCharMarbleParser.ParseSequence("a-(c1^d1)-f");
            actual.First().Time.Should().Be(-2);
            actual.Skip(2).First().Time.Should().Be(0);
        }

        [Fact]
        public void Supports_space_separated_group()
        {
            var sequence = "(cx dx)";
            var actual = MultiCharMarbleParser.ParseSequence(sequence);
            actual.First().Marbles.Should().Equal("cx","dx");
        }

        [Fact]
        public void Supports_comma_separated_group()
        {
            var sequence = "(cx,dx)";
            var actual = MultiCharMarbleParser.ParseSequence(sequence);
            actual.First().Marbles.Should().Equal("cx", "dx");
        }

        [Fact]
        public void Supports_comma_space_separated_group()
        {
            var sequence = "(cx, dx)";
            var actual = MultiCharMarbleParser.ParseSequence(sequence);
            actual.First().Marbles.Should().Equal("cx", "dx");
        }

        [Fact]
        public void Supports_space_and_comma_separated_group()
        {
            var sequence = "(cx,dx ex)";
            var actual = MultiCharMarbleParser.ParseSequence(sequence);
            actual.First().Marbles.Should().Equal("cx", "dx", "ex");
        }

        [Fact]
        public void Supports_circumflex_separated_group()
        {
            var sequence = "(cx^dx)";
            var actual = MultiCharMarbleParser.ParseSequence(sequence);
            actual.First().Marbles.Should().Equal("cx", "^", "dx");
        }

        [Fact]
        public void Supports_circumflex_and_space_separated_group()
        {
            var sequence = "(^ cx dx)";
            var actual = MultiCharMarbleParser.ParseSequence(sequence);
            actual.First().Marbles.Should().Equal("^", "cx", "dx");
        }

        [Fact]
        public void Supports_group_in_middle()
        {
            var sequence = "a-(cx dx)-f";
            var actual = MultiCharMarbleParser.ParseSequence(sequence);
            actual.Take(3).Select(m => string.Join("+", m.Marbles)).Should().Equal("a", "", "cx+dx");
            actual.Take(3).Select(m => m.Time).Should().Equal(0, 1, 2);
            actual.Last().Marbles.Should().Equal("f");
            actual.Last().Time.Should().Be(sequence.Length - 1);
        }

        [Fact]
        public void Supports_multiple_groups()
        {
            var actual = MultiCharMarbleParser.ParseSequence("a-(c,d)e-(g,h)");
            actual.Select(m => m.Time).Should().Equal(Enumerable.Range(0, 14));
            actual.Select(m => string.Join("+",m.Marbles)).Should().Equal("a", "", "c+d", "", "", "", "", "e", "", "g+h", "", "", "", "");
        }

        [Fact]
        public void Supports_group_at_start()
        {
            var actual = MultiCharMarbleParser.ParseSequence("(c,d)e");
            actual.Take(1).Select(m => string.Join("+", m.Marbles)).Should().Equal("c+d");
            actual.Last().Marbles.Should().Equal("e");
        }

        [Fact]
        public void Supports_group_at_end()
        {
            var actual = MultiCharMarbleParser.ParseSequence("a(c,d)");
            actual.Skip(1).First().Marbles.Should().Equal("c", "d");
            actual.Skip(1).First().Time.Should().Be(1);
        }

        [Fact]
        public void Should_return_one_marble_per_timeslot()
        {
            var actual = MultiCharMarbleParser.ParseSequence("a-b-c");
            actual.Select(m => m.Marbles.Length).Should().Equal(1, 0, 1, 0, 1);
        }

        [Fact]
        public void Should_return_correct_marble_per_timeslot()
        {
            var actual = MultiCharMarbleParser.ParseSequence("a-b-c");
            actual.Select(m => m.Marbles.FirstOrDefault()).Should().Equal("a", null, "b", null, "c");
        }

        [Fact]
        public void Glued_characters_form_single_marble()
        {
            var actual = MultiCharMarbleParser.ParseSequence("abc");
            actual.First().Marbles.Should().Equal("abc");
        }

        [Fact]
        public void Time_hidden_by_multichar_marble_is_equivalent_to_dashes()
        {
            var actual = MultiCharMarbleParser.ParseSequence("abc");
            actual.Select(m => string.Join("+", m.Marbles)).Should().Equal("abc", "", "");
            actual.Select(m => m.Time).Should().Equal(0, 1, 2);
        }

        [Fact]
        public void Time_hidden_by_group_is_equivalent_to_dashes()
        {
            var actual = MultiCharMarbleParser.ParseSequence("(a,b)");
            actual.Select(m => m.Time).Should().Equal(0, 1, 2, 3, 4);
            actual.Select(m => string.Join("+", m.Marbles)).Should().Equal("a+b", "", "", "", "");
        }

        [Fact]
        public void Should_return_empty_moment_for_dash()
        {
            var actual = MultiCharMarbleParser.ParseSequence("-");
            actual.Should().HaveCount(1);
            actual.First().Marbles.Should().BeEmpty();
        }

        [Fact]
        public void Should_return_empty_moments_for_dash()
        {
            var actual = MultiCharMarbleParser.ParseSequence("--");
            actual.Should().HaveCount(2);
            actual.First().Marbles.Should().BeEmpty();
            actual.Skip(1).First().Marbles.Should().BeEmpty();
        }

        [Fact]
        public void Should_use_single_moment_for_all_grouped_marbles()
        {
            var actual = MultiCharMarbleParser.ParseSequence("(a,b,c)");
            actual.First().Marbles.Should().Equal("a", "b", "c");
        }

        [Fact]
        public void Should_use_single_moment_for_all_grouped_marbles_when_separated_by_space()
        {
            var actual = MultiCharMarbleParser.ParseSequence("(ab c)");
            actual.First().Marbles.Should().Equal("ab", "c");
        }

        [Fact]
        public void Not_leading_or_trailing_spaces_are_equivalent_to_dashes()
        {
            var actual = MultiCharMarbleParser.ParseSequence("a b  c -  (de)   f-");
            var expected = MultiCharMarbleParser.ParseSequence("a-b--c----(de)---f-");
            actual.Select(m => m.Time).Should().Equal(expected.Select(m => m.Time));
            actual.Select(m => string.Join("+", m.Marbles)).Should().Equal(expected.Select(m => string.Join("+", m.Marbles)));
        }

        [Fact]
        public void Leading_spaces_are_ignored()
        {
            var actual = MultiCharMarbleParser.ParseSequence("   a--b");
            var expected = MultiCharMarbleParser.ParseSequence("a--b");
            actual.Select(m => m.Time).Should().Equal(expected.Select(m => m.Time));
            actual.Select(m => string.Join("+", m.Marbles)).Should().Equal(expected.Select(m => string.Join("+", m.Marbles)));
        }

        [Fact]
        public void Trailing_spaces_are_ignored()
        {
            var actual = MultiCharMarbleParser.ParseSequence("a--b   ");
            var expected = MultiCharMarbleParser.ParseSequence("a--b");
            actual.Select(m => m.Time).Should().Equal(expected.Select(m => m.Time));
            actual.Select(m => string.Join("+", m.Marbles)).Should().Equal(expected.Select(m => string.Join("+", m.Marbles)));
        }

        [Fact]
        public void Leading_and_trailing_spaces_are_ignored()
        {
            var actual = MultiCharMarbleParser.ParseSequence("   a--b   ");
            var expected = MultiCharMarbleParser.ParseSequence("a--b");
            actual.Select(m => m.Time).Should().Equal(expected.Select(m => m.Time));
            actual.Select(m => string.Join("+", m.Marbles)).Should().Equal(expected.Select(m => string.Join("+", m.Marbles)));
        }
    }
}