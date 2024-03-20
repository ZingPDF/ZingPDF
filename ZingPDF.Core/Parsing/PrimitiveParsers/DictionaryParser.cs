using MorseCode.ITask;
using System.Text;
using ZingPdf.Core.Extensions;
using ZingPdf.Core.Logging;
using ZingPdf.Core.Objects;
using ZingPdf.Core.Objects.ObjectGroups;
using ZingPdf.Core.Objects.ObjectGroups.CrossReferences.CrossReferenceStreams;
using ZingPdf.Core.Objects.Pages;
using ZingPdf.Core.Objects.Primitives;
using ZingPdf.Core.Objects.Primitives.Streams;

namespace ZingPdf.Core.Parsing.PrimitiveParsers
{
    /// <summary>
    /// Creates a <see cref="Dictionary"/> from the provided tokens.
    /// </summary>
    /// <remarks>
    /// This parser will find the first &lt;&lt; delimiter from the provided tokens.
    /// </remarks>
    internal class DictionaryParser : IPdfObjectParser<Dictionary>
    {
        public async ITask<Dictionary> ParseAsync(Stream stream)
        {
            Logger.Log(LogLevel.Trace, $"Parsing Dictionary from {stream.GetType().Name} at offset: {stream.Position}.");

            // A dictionary is a key-value collection, where the key is always a 'Name' object
            // and the value can be any type of PDF object

            // << /Size 50 /Root 49 0 R /Info 47 0 R /ID [ <66dbd809c84b6f6bd19bb2f8865b77cc> <66dbd809c84b6f6bd19bb2f8865b77cc> ] >>

            var initialStreamPosition = stream.Position;
            var dictStart = 0L;
            var dictEnd = 0L;

            // Find end of dictionary
            var content = string.Empty;
            int countStart = 0;
            int countEnd = 0;

            var bufferSize = 1024;
            var buffer = new byte[bufferSize];

            do
            {
                int i = content.Length;
                var read = await stream.ReadAsync(buffer.AsMemory());

                content += Encoding.ASCII.GetString(buffer, 0, read);

                Logger.Log(LogLevel.Trace, content[..Math.Min(100, read)]);

                for (; i < content.Length - 1; i++)
                {
                    // TODO: consider if objects can contain escaped dictionary delimiters which may break this logic, write tests

                    var c = content[i..(i + 2)];

                    if (c == Constants.DictionaryStart)
                    {
                        countStart++;
                        
                        if (countStart == 1)
                        {
                            dictStart = initialStreamPosition + i + 2;
                        }

                        i++; // increment so that nested dictionaries don't cause false positives <<<<
                    }

                    if (c == Constants.DictionaryEnd)
                    {
                        countEnd++;

                        if (countEnd == countStart)
                        {
                            // TODO: this is used to build a substream, and move past the array
                            //      but i is a character count, not a byte count. Use the proper byte length of the content.

                            dictEnd = initialStreamPosition + i;

                            break;
                        }

                        i++; // increment so that nested dictionaries don't cause false positives >>>>
                    }

                    if (countStart > 0 && countEnd == countStart)
                    {
                        break;
                    }
                }
            }
            while (countStart != countEnd && stream.Position < stream.Length);
            
            if (countStart != countEnd)
            {
                throw new ParserException($"Unable to find end of dictionary. PDF may be corrupt.");
            }

            Dictionary output = [];

            if (dictEnd - dictStart > 1)
            {
                var dictStream = new SubStream(stream, dictStart, dictEnd);

                var objectGroup = await Parser.For<PdfObjectGroup>().ParseAsync(dictStream);

                if (objectGroup.Objects.Count % 2 != 0)
                {
                    throw new InvalidOperationException("Odd count of objects parsed from dictionary.");
                }

                for (int j = 0; j < objectGroup.Objects.Count; j += 2)
                {
                    output.Add((Name)objectGroup.Objects[j], objectGroup.Objects[j + 1]);
                }

                if (output.ContainsKey(Constants.DictionaryKeys.Type))
                {
                    switch ((Name)output[Constants.DictionaryKeys.Type])
                    {
                        case Page.DictionaryKeys.Page:
                            output = Page.FromDictionary(output);
                            break;

                        case PageTreeNode.DictionaryKeys.Pages:
                            output = PageTreeNode.FromDictionary(output);
                            break;

                        case CrossReferenceStreamDictionary.DictionaryKeys.XRef:
                            output = CrossReferenceStreamDictionary.FromDictionary(output);
                            break;

                        case ObjectStreamDictionary.DictionaryKeys.ObjStm:
                            output = ObjectStreamDictionary.FromDictionary(output);
                            break;
                    }
                }

                if (output.ContainsKey(LinearizationDictionary.DictionaryKeys.Linearized))
                {
                    output = LinearizationDictionary.FromDictionary(output);
                }
            } 

            stream.Position = dictEnd + 2;

            Logger.Log(LogLevel.Trace, $"Parsed Dictionary between offsets: {initialStreamPosition} - {stream.Position}");

            return output;
        }
    }
}