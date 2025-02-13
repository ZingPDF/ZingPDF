using ZingPDF.IncrementalUpdates;
using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;

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

    public static async Task<T> ResolveAsync<T>(this IPdfObject obj, IIndirectObjectDictionary indirectObjectDictionary)
        where T : class, IPdfObject
    {
        if (obj is T typed)
        {
            return typed;
        }
        else if (obj is IndirectObjectReference ior)
        {
            return await indirectObjectDictionary.GetAsync<T>(ior)
                ?? throw new InvalidPdfException($"Unable to resolve indirect object reference: {ior}");
        }

        throw new InvalidOperationException("Internal error");
    }
}
