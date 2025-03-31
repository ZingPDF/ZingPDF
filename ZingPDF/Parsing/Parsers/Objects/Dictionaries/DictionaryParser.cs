using System.Text;

namespace ZingPDF.Parsing.Parsers.Objects.Dictionaries;

internal abstract class DictionaryParser
{
    private static readonly List<char> _halfDelimiters = [Constants.LessThan, Constants.GreaterThan];

    private static bool IsHalfADelimiter(char c) => _halfDelimiters.Contains(c);

    /// <summary>
    /// Analyses the given stream forward from the current position and extracts the portion containing the dictionary.
    /// </summary>
    /// <param name="source">The PDF input stream</param>
    /// <returns>A <see cref="SubStream"/> instance for the portion of the stream containing the dictionary.</returns>
    /// <exception cref="ParserException"></exception>
    protected static async Task<SubStream> ExtractDictionarySegmentAsync(Stream source)
    {
        var buffer = new byte[1024];

        var countStart = 0;                          // Tracks "<<" occurrences
        var countEnd = 0;                            // Tracks ">>" occurrences

        long dictStart = 0;                          // Byte offset for dictionary start
        long dictEnd = 0;                            // Byte offset for dictionary end

        var lastEncounteredDelimiterEndsAt = 0;

        do
        {
            // Read from the stream
            var read = await source.ReadAsync(buffer.AsMemory());

            // Convert the buffer to a string and prepend the carryover
            string currentContent = Encoding.ASCII.GetString(buffer, 0, read);

            //Logger.Log(LogLevel.Trace, currentContent[..Math.Min(currentContent.Length, 100)]);

            for (var i = 0; i < currentContent.Length - 1; i++)
            {
                var processedContent = currentContent[..i];
                var byteOffsetForDecodedPosition = Encoding.ASCII.GetByteCount(processedContent);

                // Check for dictionary delimiters
                var c = currentContent[i..(i + 2)]; // Extract two characters starting at i

                if (c == Constants.DictionaryStart)
                {
                    countStart++;
                    lastEncounteredDelimiterEndsAt = i + 2;

                    if (countStart == 1)
                    {
                        dictStart = source.Position - read + byteOffsetForDecodedPosition + 2;
                    }

                    i++; // Increment past current delimiter
                }
                else if (c == Constants.DictionaryEnd)
                {
                    countEnd++;
                    lastEncounteredDelimiterEndsAt = i + 2;

                    if (countEnd == countStart)
                    {
                        dictEnd = source.Position - read + byteOffsetForDecodedPosition;

                        goto ReadyToParse;
                    }

                    i++; // Increment past current delimiter
                }
            }

            // If a delimiter straddles the buffer boundary, we must ensure it is counted.
            // Identifying this is tricky. We can't just check the last 2 characters to see if the 2nd is a '<' or '>',
            // as nested dictionaries cause sequences like this >>>>>>.
            if (IsHalfADelimiter(currentContent.Last()) && lastEncounteredDelimiterEndsAt != source.Position)
            {
                source.Position--;
            }
        }
        while (countStart != countEnd && source.Position < source.Length);

        if (countStart != countEnd)
        {
            throw new ParserException($"Unable to find end of dictionary. Start Count: {countStart}, End Count: {countEnd}, Stream Position: {source.Position}.");
        }

    ReadyToParse:
        //if (dictEnd - dictStart > 1)
        //{
        return new SubStream(source, dictStart, dictEnd);
        //}

        //return null;
    }
}
