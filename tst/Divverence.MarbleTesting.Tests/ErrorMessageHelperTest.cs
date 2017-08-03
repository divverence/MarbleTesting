using Xunit;

namespace Divverence.MarbleTesting.Tests
{
    public class ErrorMessageHelperTest
    {
        [Theory]
        [InlineData("-x-", 0, 
            @"
  -x-
  ↑")]
        [InlineData("-x-", 1, 
            @"
  -x-
   ↑")]
        [InlineData("-x-", 2, 
            @"
  -x-
    ↑")]
        public void ShouldProvidePointerToOffendingMarbleAtCorrectPoint(string sequence, int offender, string expected)
        {
            var actual = ErrorMessageHelper.SequenceWithPointerToOffendingMoment(sequence, offender);
            Assert.Equal(expected, actual);
        }
    }
}