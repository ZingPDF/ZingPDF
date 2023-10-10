using ZingPdf.Core.Objects;

namespace ZingPdf.UnitTests
{
    internal static class FluentAssertionsHelpers
    {
        public static readonly Func<FluentAssertions.Equivalency.EquivalencyAssertionOptions<PdfObject>, FluentAssertions.Equivalency.EquivalencyAssertionOptions<PdfObject>> ExcludeOptionalProperties
            = options => options
                .Excluding(i => i.ByteOffset)
                .Excluding(i => i.Written)
                .Excluding(i => i.Length);
    }
}
