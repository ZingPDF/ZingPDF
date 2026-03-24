namespace ZingPDF.Parsing.Parsers.Objects.Dictionaries;

internal abstract class DictionaryParser
{
    /// <summary>
    /// Analyses the given stream forward from the current position and extracts the portion containing the dictionary.
    /// </summary>
    /// <param name="source">The PDF input stream</param>
    /// <returns>A <see cref="SubStream"/> instance for the portion of the stream containing the dictionary.</returns>
    /// <exception cref="ParserException"></exception>
    protected static async Task<SubStream> ExtractDictionarySegmentAsync(Stream source)
    {
        await Task.Yield();

        var countStart = 0;
        var countEnd = 0;

        long dictStart = -1;
        long dictEnd = -1;

        var inComment = false;
        var inHexString = false;
        var literalStringDepth = 0;
        var escapeNext = false;

        while (source.Position < source.Length)
        {
            var currentPosition = source.Position;
            var current = source.ReadByte();

            if (current < 0)
            {
                break;
            }

            if (inComment)
            {
                if (current is '\r' or '\n')
                {
                    inComment = false;
                }

                continue;
            }

            if (literalStringDepth > 0)
            {
                if (escapeNext)
                {
                    escapeNext = false;
                    continue;
                }

                if (current == '\\')
                {
                    escapeNext = true;
                    continue;
                }

                if (current == '(')
                {
                    literalStringDepth++;
                    continue;
                }

                if (current == ')')
                {
                    literalStringDepth--;
                }

                continue;
            }

            if (inHexString)
            {
                if (current == '>')
                {
                    inHexString = false;
                }

                continue;
            }

            if (current == '%')
            {
                inComment = true;
                continue;
            }

            if (current == '(')
            {
                literalStringDepth = 1;
                continue;
            }

            if (current == '<')
            {
                var next = source.ReadByte();
                if (next < 0)
                {
                    break;
                }

                if (next == '<')
                {
                    countStart++;

                    if (countStart == 1)
                    {
                        dictStart = currentPosition + 2;
                    }

                    continue;
                }

                inHexString = true;
                source.Position--;
                continue;
            }

            if (current == '>')
            {
                var next = source.ReadByte();
                if (next < 0)
                {
                    break;
                }

                if (next == '>')
                {
                    countEnd++;

                    if (countEnd == countStart)
                    {
                        dictEnd = currentPosition;
                        break;
                    }

                    continue;
                }

                source.Position--;
            }
        }

        if (countStart == 0 || countStart != countEnd || dictStart < 0 || dictEnd < 0)
        {
            throw new ParserException($"Unable to find end of dictionary. Start Count: {countStart}, End Count: {countEnd}, Stream Position: {source.Position}.");
        }

        return new SubStream(source, dictStart, dictEnd);
    }
}
