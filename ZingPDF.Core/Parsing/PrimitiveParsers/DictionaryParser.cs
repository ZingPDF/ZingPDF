using MorseCode.ITask;
using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects;
using ZingPdf.Core.Objects.ObjectGroups;
using ZingPdf.Core.Objects.ObjectGroups.CrossReferenceTable;
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
            // A dictionary is a key-value collection, where the key is always a 'Name' object
            // and the valuie can be any type of PDF object

            // << /Size 50 /Root 49 0 R /Info 47 0 R /ID [ <66dbd809c84b6f6bd19bb2f8865b77cc> <66dbd809c84b6f6bd19bb2f8865b77cc> ] >>

            await stream.AdvanceBeyondNextAsync(Constants.DictionaryStart);

            var dictStart = stream.Position;

            // Find end of dictionary
            var content = string.Empty;
            int countStart = 1;
            int countEnd = 0;

            while (stream.Position < stream.Length)
            {
                int i = content.Length;
                content += await stream.GetAsync();

                for (; i < content.Length - 1; i++)
                {
                    // TODO: consider if objects can contain escaped dictionary delimiters which may break this logic, write tests

                    var c = content[i..(i + Constants.DictionaryEnd.Length)];

                    if (c == Constants.DictionaryStart)
                    {
                        countStart++;
                        i++; // increment so that nested dictionaries don't cause false positives <<<<
                    }

                    if (c == Constants.DictionaryEnd)
                    {
                        countEnd++;
                        i++; // increment so that nested dictionaries don't cause false positives >>>>
                    }

                    if (countStart > 0 && countEnd == countStart)
                    {
                        break;
                    }
                }

                if (countStart > 0 && countEnd == countStart)
                {
                    stream.Position = dictStart + i - 1;

                    break;
                }
            }

            using var dictStream = await stream.RangeAsync(dictStart, stream.Position);

            stream.Position += Constants.DictionaryEnd.Length;

            var objectGroup = await Parser.For<PdfObjectGroup>().ParseAsync(dictStream);

            if (objectGroup.Objects.Count % 2 != 0)
            {
                throw new InvalidOperationException("Odd count of objects parsed from dictionary.");
            }

            Dictionary<Name, PdfObject> dictionary = new();

            for (int j = 0; j < objectGroup.Objects.Count; j += 2)
            {
                dictionary.Add((Name)objectGroup.Objects[j], objectGroup.Objects[j + 1]);
            }

            if (dictionary.ContainsKey(Constants.DictionaryKeys.Type))
            {
                switch ((Name)dictionary[Constants.DictionaryKeys.Type])
                {
                    case Page.DictionaryKeys.Page:
                        return Page.FromDictionary(dictionary);

                    case PageTreeNode.DictionaryKeys.Pages:
                        return PageTreeNode.FromDictionary(dictionary);

                    case CrossReferenceStreamDictionary.DictionaryKeys.XRef:
                        return CrossReferenceStreamDictionary.FromDictionary(dictionary);

                    case ObjectStreamDictionary.DictionaryKeys.ObjStm:
                        return ObjectStreamDictionary.FromDictionary(dictionary);
                }
            }

            if (dictionary.ContainsKey(LinearizationDictionary.DictionaryKeys.Linearized))
            {
                return LinearizationDictionary.FromDictionary(dictionary);
            }

            return dictionary;
        }
    }
}