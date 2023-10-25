using FluentAssertions;
using System.Text;
using Xunit;

namespace ZingPdf.Core.Objects.Primitives
{
    public class LiteralStringTests
    {
        [Fact]
        public void ConstructorThrowsForASCIIEncoding()
        {
            var act = () => new LiteralString("test", Encoding.ASCII);

            act.Should().Throw<ArgumentException>().WithParameterName("encodeUsing");
        }

        [Fact]
        public void ConstructorThrowsForUTF16LEEncoding()
        {
            var act = () => new LiteralString("test", Encoding.GetEncoding(1200));

            act.Should().Throw<ArgumentException>().WithParameterName("encodeUsing");
        }
    }
}
