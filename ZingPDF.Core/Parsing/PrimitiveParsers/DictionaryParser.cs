using MorseCode.ITask;
using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects;
using ZingPdf.Core.Objects.ObjectGroups;
using ZingPdf.Core.Objects.Primitives;

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
        private static readonly string _defaultExceptionMessage = "Invalid dictionary";

        public async ITask<Dictionary> ParseAsync(Stream stream)
        {
            // A dictionary is a key-value collection, where the key is always a 'Name' object
            // and the valuie can be any type of PDF object

            // << /Size 50 /Root 49 0 R /Info 47 0 R /ID [ <66dbd809c84b6f6bd19bb2f8865b77cc> <66dbd809c84b6f6bd19bb2f8865b77cc> ] >>

            await stream.AdvanceToNextAsync(Constants.DictionaryStart);

            var dictStart = stream.Position;

            // Find end of dictionary
            var content = string.Empty;
            int countStart = 0;
            int countEnd = 0;
            long dictEnd = 0;

            while (stream.Position < stream.Length)
            {
                int i = content.Length;
                content += await stream.GetAsync();

                for (; i < content.Length - 1; i++)
                {
                    // TODO: consider if objects can contain escaped dictionary delimiters which may break this logic, write tests

                    var c = content[i..(i + 2)];

                    if (c == Constants.DictionaryStart) { countStart++; }
                    if (c == Constants.DictionaryEnd) { countEnd++; }

                    if (countStart > 0 && countEnd == countStart)
                    {
                        break;
                    }
                }

                if (countStart > 0 && countEnd == countStart)
                {
                    dictEnd = i;
                    stream.Position = dictStart + i;

                    await stream.AdvanceBeyondNextAsync(Constants.DictionaryEnd);
                    break;
                }
            }

            Dictionary<Name, PdfObject> dictionary = new();

            using var dictStream = await stream.RangeAsync(dictStart + 2, dictEnd + dictStart);

            var objectGroup = await Parser.For<PdfObjectGroup>().ParseAsync(dictStream);

            for (int j = 0; j < objectGroup.Objects.Count; j += 2)
            {
                dictionary.Add((Name)objectGroup.Objects[j], objectGroup.Objects[j + 1]);
            }

            return dictionary;
        }
    }
}