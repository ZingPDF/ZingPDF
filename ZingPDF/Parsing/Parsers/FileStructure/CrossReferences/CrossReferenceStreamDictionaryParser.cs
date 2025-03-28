using MorseCode.ITask;
using ZingPDF.IncrementalUpdates;
using ZingPDF.Logging;
using ZingPDF.Parsing.Parsers.Objects.Dictionaries;
using ZingPDF.Syntax;
using ZingPDF.Syntax.FileStructure.CrossReferences.CrossReferenceStreams;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;

namespace ZingPDF.Parsing.Parsers.FileStructure.CrossReferences;

/// <summary>
/// A simple parser for xref stream dictionaries.
/// </summary>
/// <remarks>
/// This parser is limited, and is designed for parsing xref stream dictionaries.
/// <list type="bullet">
///     <item>
///     While the parsed <see cref="Dictionary"/> will contain <see cref="AsyncProperty{T}"/> or values, 
///     these values cannot be accessed directly if they are indirect objects.
///     </item>
///     <item>
///     Subdictionaries are not supported and will throw an exception during parsing.
///     </item>
/// </list>
/// </remarks>
internal class CrossReferenceStreamDictionaryParser : DictionaryParser, IObjectParser<Dictionary>
{
    public async ITask<Dictionary> ParseAsync(Stream stream)
    {
        var initialStreamPosition = stream.Position; // Reference starting point for output

        SubStream? dictStream = await ExtractDictionarySegmentAsync(stream);

        if (dictStream == null)
        {
            return new Dictionary((PdfObjectManager)null);
        }

        var objectGroup = await new PdfObjectGroupParser().ParseAsync(dictStream);

        if (objectGroup.Objects.Count % 2 != 0)
        {
            throw new InvalidOperationException("Odd count of objects parsed from dictionary.");
        }

        Dictionary<Name, IPdfObject> dict = [];

        for (int j = 0; j < objectGroup.Objects.Count; j += 2)
        {
            var key = (Name)objectGroup.Objects[j];
            var val = objectGroup.Objects[j + 1];

            dict.Add(key, val);
        }

        stream.Position = dictStream.To + 2;

        var output = CrossReferenceStreamDictionary.FromDictionary(dict);

        output!.ByteOffset = initialStreamPosition;

        Logger.Log(LogLevel.Trace, $"Parsed Dictionary from {stream.GetType().Name} between offsets: {initialStreamPosition} - {stream.Position}");

        return output;
    }
}
