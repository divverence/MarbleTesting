using Divverence.MarbleTesting.Akka.Async;
using Xunit;

namespace Divverence.MarbleTesting.Akka.Tests.Async
{
    public class AsyncTestKitHelperTests
    {
        [Fact]
        public void Should_accept_empty_string()
        {
            var actual = AsyncTestKitHelper.GetDefaultConfig();
            Assert.NotNull(actual);
        }
    }
}
