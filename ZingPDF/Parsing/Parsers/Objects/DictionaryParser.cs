using MorseCode.ITask;
using System.Text;
using ZingPDF.Elements.Drawing;
using ZingPDF.Extensions;
using ZingPDF.InteractiveFeatures.Annotations;
using ZingPDF.InteractiveFeatures.Annotations.AppearanceStreams;
using ZingPDF.InteractiveFeatures.Forms;
using ZingPDF.Linearization;
using ZingPDF.Logging;
using ZingPDF.Syntax;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.DocumentStructure;
using ZingPDF.Syntax.DocumentStructure.PageTree;
using ZingPDF.Syntax.FileStructure.CrossReferences.CrossReferenceStreams;
using ZingPDF.Syntax.FileStructure.ObjectStreams;
using ZingPDF.Syntax.Objects;

namespace ZingPDF.Parsing.Parsers.Objects
{
    /// <summary>
    /// Creates a <see cref="Dictionary"/> from the provided tokens.
    /// </summary>
    /// <remarks>
    /// This parser will find the first &lt;&lt; delimiter from the provided tokens.
    /// </remarks>
    internal class DictionaryParser : IPdfObjectParser<Dictionary>
    {
        private static readonly List<char> _halfDelimiters = [Constants.LessThan, Constants.GreaterThan];

        // Rectangles are parsed as ArrayObjects. We'll identify them by their keys.
        private readonly List<Name> _rectKeys =
        [
            Constants.DictionaryKeys.PageTree.MediaBox,
            Constants.DictionaryKeys.PageTree.CropBox,
            Constants.DictionaryKeys.PageTree.Page.BleedBox,
            Constants.DictionaryKeys.PageTree.Page.TrimBox,
            Constants.DictionaryKeys.PageTree.Page.ArtBox,
            Constants.DictionaryKeys.Annotation.Rect,
            Constants.DictionaryKeys.Form.Type1.BBox,
        ];

        public async ITask<Dictionary> ParseAsync(Stream stream)
        {
            //Logger.Log(LogLevel.Trace, $"Parsing Dictionary from {stream.GetType().Name} at offset: {stream.Position}.");

            // A dictionary is a key-value collection, where the key is always a 'Name' object
            // and the value can be any type of PDF object

            // << /Size 50 /Root 49 0 R /Info 47 0 R /ID [ <66dbd809c84b6f6bd19bb2f8865b77cc> <66dbd809c84b6f6bd19bb2f8865b77cc> ] >>

            var initialStreamPosition = stream.Position; // Reference starting point for output
            
            var buffer = new byte[1024];

            var countStart = 0;                          // Tracks "<<" occurrences
            var countEnd = 0;                            // Tracks ">>" occurrences

            long dictStart = 0;                          // Byte offset for dictionary start
            long dictEnd = 0;                            // Byte offset for dictionary end

            var lastEncounteredDelimiterEndsAt = 0;

            do
            {
                // Read from the stream
                var read = await stream.ReadAsync(buffer.AsMemory());

                // Convert the buffer to a string and prepend the carryover
                string currentContent = Encoding.ASCII.GetString(buffer, 0, read);

                //Logger.Log(LogLevel.Trace, currentContent[..Math.Min(currentContent.Length, 100)]);

                for (var i = 0; i < currentContent.Length - 1; i++)
                {
                    var processedContent = currentContent[..i];
                    var byteOffsetForDecodedPosition = Encoding.ASCII.GetByteCount(processedContent);

                    // Check for dictionary delimiters
                    var c = currentContent[i..(i+2)]; // Extract two characters starting at i

                    if (c == Constants.DictionaryStart)
                    {
                        countStart++;
                        lastEncounteredDelimiterEndsAt = i + 2;

                        if (countStart == 1)
                        {
                            dictStart = stream.Position - read + byteOffsetForDecodedPosition + 2;
                        }

                        i++; // Increment past current delimiter
                    }
                    else if (c == Constants.DictionaryEnd)
                    {
                        countEnd++;
                        lastEncounteredDelimiterEndsAt = i + 2;

                        if (countEnd == countStart)
                        {
                            dictEnd = stream.Position - read + byteOffsetForDecodedPosition;

                            goto ReadyToParse;
                        }

                        i++; // Increment past current delimiter
                    }
                }

                // If a delimiter straddles the buffer boundary, we must ensure it is counted.
                // Identifying this is tricky. We can't just check the last 2 characters to see if the 2nd is a '<' or '>',
                // as nested dictionaries cause sequences like this >>>>>>.
                if (IsHalfADelimiter(currentContent.Last()) && lastEncounteredDelimiterEndsAt != stream.Position)
                {
                    stream.Position--;
                }
            }
            while (countStart != countEnd && stream.Position < stream.Length);

            if (countStart != countEnd)
            {
                throw new ParserException($"Unable to find end of dictionary. Start Count: {countStart}, End Count: {countEnd}, Stream Position: {stream.Position}.");
            }

            ReadyToParse:
            Dictionary? output = null;
            
            if (dictEnd - dictStart > 1)
            {
                var dictStream = new SubStream(stream, dictStart, dictEnd);

                var objectGroup = await Parser.For<PdfObjectGroup>().ParseAsync(dictStream);

                if (objectGroup.Objects.Count % 2 != 0)
                {
                    throw new InvalidOperationException("Odd count of objects parsed from dictionary.");
                }

                Dictionary<Name, IPdfObject> dict = [];

                for (int j = 0; j < objectGroup.Objects.Count; j += 2)
                {
                    var key = (Name)objectGroup.Objects[j];
                    var val = objectGroup.Objects[j + 1];

                    if (_rectKeys.Contains(key))
                    {
                        var ary = (ArrayObject)val;

                        val = new Rectangle(
                            new Coordinate(ary[0].ToRealNumber(), ary[1].ToRealNumber()),
                            new Coordinate(ary[2].ToRealNumber(), ary[3].ToRealNumber())
                            );
                    }

                    dict.Add(key, val);
                }

                if (dict.ContainsKey(Constants.DictionaryKeys.Type))
                {
                    switch ((Name)dict[Constants.DictionaryKeys.Type])
                    {
                        case Constants.DictionaryTypes.Catalog:
                            output = DocumentCatalogDictionary.FromDictionary(dict);
                            goto DictionaryParsed;

                        case Constants.DictionaryTypes.Page:
                            output = PageDictionary.FromDictionary(dict);
                            goto DictionaryParsed;

                        case Constants.DictionaryTypes.Pages:
                            output = PageTreeNodeDictionary.FromDictionary(dict);
                            goto DictionaryParsed;

                        case Constants.DictionaryTypes.XRef:
                            output = CrossReferenceStreamDictionary.FromDictionary(dict);
                            goto DictionaryParsed;

                        case Constants.DictionaryTypes.ObjStm:
                            output = ObjectStreamDictionary.FromDictionary(dict);
                            goto DictionaryParsed;
                        case Constants.DictionaryTypes.Annot:
                            output = (string)(Name)dict[Constants.DictionaryKeys.Subtype] switch
                            {
                                AnnotationDictionary.Subtypes.Widget => WidgetAnnotationDictionary.FromDictionary(dict),
                                _ => AnnotationDictionary.FromDictionary(dict),
                            };
                            goto DictionaryParsed;
                    }
                }

                if (dict.ContainsKey(Constants.DictionaryKeys.InteractiveForm.Fields))
                {
                    output = InteractiveFormDictionary.FromDictionary(dict);
                    goto DictionaryParsed;
                }

                if (dict.ContainsKey(Constants.DictionaryKeys.Field.FT))
                {
                    output = FieldDictionary.FromDictionary(dict);
                    goto DictionaryParsed;
                }

                if (dict.ContainsKey(Constants.DictionaryKeys.LinearizationParameter.Linearized))
                {
                    output = LinearizationParameterDictionary.FromDictionary(dict);
                    goto DictionaryParsed;
                }

                if (dict.ContainsKey(Constants.DictionaryKeys.Appearance.N))
                {
                    output = AppearanceDictionary.FromDictionary(dict);
                    goto DictionaryParsed;
                }

                output ??= dict;
            }

            output ??= Dictionary.Empty;

        DictionaryParsed:
            stream.Position = dictEnd + 2;

            output!.ByteOffset = initialStreamPosition;

            Logger.Log(LogLevel.Trace, $"Parsed Dictionary from {stream.GetType().Name} between offsets: {initialStreamPosition} - {stream.Position}");

            return output;
        }

        private static bool IsHalfADelimiter(char c) => _halfDelimiters.Contains(c);
    }
}