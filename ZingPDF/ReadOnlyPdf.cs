using ZingPDF.Linearization;
using ZingPDF.Parsing;
using ZingPDF.Syntax.DocumentStructure;
using ZingPDF.Syntax.FileStructure.Trailer;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF;

/// <summary>
/// Represents a PDF document which is not editable.
/// </summary>
/// <remarks>
/// This class is disposable. The underlying <see cref="Stream"/> will remain open until the instance is disposed.<para></para>
/// </remarks>
public class ReadOnlyPdf : BasePdf
{
    /// <summary>
    /// Internal constructor for creating a <see cref="ReadOnlyPdf"/> from its constituent parts.
    /// </summary>
    public ReadOnlyPdf(
        Stream pdfInputStream,
        DocumentCatalogDictionary documentCatalog,
        Trailer? trailer,
        IndirectObject? xrefStream,
        ReadOnlyIndirectObjectDictionary indirectObjectDictionary,
        LinearizationParameterDictionary? linearizationDictionary
        )
        : base(pdfInputStream, documentCatalog, trailer, xrefStream, indirectObjectDictionary, linearizationDictionary)
    {
    }
}
