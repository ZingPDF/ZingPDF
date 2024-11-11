using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects;

namespace ZingPDF.Extensions;

internal static class PdfObjectExtensions
{
    /// <summary>
    /// Returns a <see cref="RealNumber"/> instance from an <see cref="IPdfObject"/>.
    /// The underlying type must be either a <see cref="RealNumber"/> or an <see cref="Integer"/>
    /// </summary>
    public static RealNumber ToRealNumber(this IPdfObject obj)
    {
        return obj switch
        {
            RealNumber realNumber => realNumber,
            Integer integer => integer,
            _ => throw new InvalidOperationException(),
        };
    }
}
