using System.Globalization;
using System.Text;
using ZingPDF.Syntax.Filters;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;

namespace ZingPDF.Extensions;

internal static class StreamExtensions
{
    private static readonly Encoding _defaultEncoding = Encoding.ASCII;

    public static Task WriteTextAsync(this Stream stream, string text)
        => stream.WriteTextAsync(text, _defaultEncoding);

    public static async Task WriteTextAsync(this Stream stream, string text, Encoding encoding)
        => await stream.WriteAsync(encoding.GetBytes(text));

    public static async Task WriteCharsAsync(this Stream stream, params char[] characters)
        => await stream.WriteAsync(_defaultEncoding.GetBytes(characters));

    public static async Task WriteIntAsync(this Stream stream, int value)
        => await stream.WriteTextAsync(value.ToString("G", CultureInfo.InvariantCulture));

    public static async Task WriteLongAsync(this Stream stream, long value)
        => await stream.WriteTextAsync(value.ToString("G", CultureInfo.InvariantCulture));

    public static async Task WriteDoubleAsync(this Stream stream, double value)
        => await stream.WriteTextAsync(value.ToString("N3", CultureInfo.InvariantCulture));

    public static async Task WriteLeftPaddedAsync(this Stream stream, ushort value, int padToBytes)
        => await stream.WriteTextAsync(value.ToString("G", CultureInfo.InvariantCulture).PadLeft(padToBytes, '0'));

    public static async Task WriteLeftPaddedAsync(this Stream stream, long value, int padToBytes)
        => await stream.WriteTextAsync(value.ToString("G", CultureInfo.InvariantCulture).PadLeft(padToBytes, '0'));

    /// <summary>
    /// Write a single whitespace character to the stream.
    /// </summary>
    public static async Task WriteWhitespaceAsync(this Stream stream)
        => await stream.WriteCharsAsync(Constants.Whitespace);

    /// <summary>
    /// Write a unix-style new line character to the stream (\n).
    /// </summary>
    public static async Task WriteNewLineAsync(this Stream stream)
        => await stream.WriteCharsAsync(Constants.LineFeed);

    /// <summary>
    /// Write a full new line character sequence to the stream (\r\n).
    /// </summary>
    public static async Task WriteFullNewLineAsync(this Stream stream)
        => await stream.WriteCharsAsync(Constants.EndOfLineCharacters);

    /// <summary>
    /// Finds the specified value in the stream and advances its position to it.
    /// </summary>
    public static async Task AdvanceToNextAsync(this Stream stream, char value)
        => await stream.AdvanceAsync(value.ToString(), skipValue: false);

    /// <summary>
    /// Finds the specified value in the stream and advances its position to it.
    /// </summary>
    public static async Task AdvanceToNextAsync(this Stream stream, string value)
        => await stream.AdvanceAsync(value.ToString(), skipValue: false);

    /// <summary>
    /// Finds the specified value in the stream and advances its position to it.
    /// </summary>
    public static async Task AdvanceBeyondNextAsync(this Stream stream, char value)
        => await stream.AdvanceAsync(value.ToString(), skipValue: true);

    /// <summary>
    /// Finds the specified value in the stream and advances its position to it.
    /// </summary>
    public static async Task AdvanceBeyondNextAsync(this Stream stream, string value)
        => await stream.AdvanceAsync(value, skipValue: true);

    /// <summary>
    /// Finds the specified value in the stream and advances its position to it.
    /// </summary>
    private static async Task AdvanceAsync(this Stream stream, string value, bool skipValue)
    {
        var bufferSize = Math.Max(value.Length, 128);
        var valueBytes = _defaultEncoding.GetBytes(value);

        var matchIndex = 0;

        var buffer = new byte[bufferSize];

        var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, bufferSize));
        if (bytesRead == 0)
        {
            return;
        }

        for (int i = 0; i < bytesRead; i++)
        {
            if (buffer[i] == valueBytes[matchIndex])
            {
                matchIndex++;
                if (matchIndex == valueBytes.Length)
                {
                    if (skipValue)
                    {
                        stream.Position -= bytesRead - i - 1;
                    }
                    else
                    {
                        stream.Position -= bytesRead - i;
                    }
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Reads the stream into a string until it finds any of the specified characters.
    /// The found character is not included in the output.
    /// </summary>
    public static Task<string> ReadUpToExcludingAsync(this Stream stream, params char[] c)
        => stream.ReadUpToAsync(includeValueInOutput: false, c);

    /// <summary>
    /// Reads the stream into a string until it finds any of the specified characters.
    /// The found character is included in the output.
    /// </summary>
    public static Task<string> ReadUpToIncludingAsync(this Stream stream, params char[] c)
        => stream.ReadUpToAsync(includeValueInOutput: true, c);

    private static async Task<string> ReadUpToAsync(this Stream stream, bool includeValueInOutput, params char[] c)
    {
        var bufferSize = 128;
        var buffer = new byte[bufferSize];
        var builder = new StringBuilder();
        Decoder decoder = _defaultEncoding.GetDecoder();

        var charBuffer = new char[bufferSize];
        var remainingBytes = new List<byte>();

        while (true)
        {
            var read = await stream.ReadAsync(buffer.AsMemory());
            if (read == 0)
            {
                break; // End of stream
            }

            // Handle leftover bytes from the previous read
            if (remainingBytes.Count > 0)
            {
                buffer = [.. remainingBytes, .. buffer.Take(read)];
                read += remainingBytes.Count;
                remainingBytes.Clear();
            }

            // Decode bytes into characters
            int charsDecoded = decoder.GetChars(buffer, 0, read, charBuffer, 0, false);
            string chunk = new(charBuffer, 0, charsDecoded);
            builder.Append(chunk);

            // Look for the target characters
            int index = builder.ToString().IndexOfAny(c);
            if (index != -1)
            {
                if (includeValueInOutput)
                {
                    index++;
                }

                // Handle leftover bytes and reset the stream position if seekable
                if (stream.CanSeek)
                {
                    var extraBytes = Encoding.UTF8.GetByteCount(builder.ToString(index, builder.Length - index));
                    stream.Position -= extraBytes;
                }

                return builder.ToString(0, index);
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Read stream until the predicate is satisfied.
    /// </summary>
    /// <remarks>
    /// Data is buffered from the stream at the size specified in <paramref name="bufferSize"/>.<para></para>
    /// One character at a time is supplied to your predicate.<para></para>
    /// When this method returns, the stream will be set to the position of the current character.
    /// </remarks>
    public static async Task<string> ReadUntilAsync(this Stream stream, Func<char, bool> condition, int bufferSize = 256)
    {
        var buffer = new byte[bufferSize];
        var content = string.Empty;

        do
        {
            var read = await stream.ReadAsync(buffer.AsMemory(0, bufferSize));
            var str = _defaultEncoding.GetString(buffer, 0, read);

            content += str;

            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                if (condition(c))
                {
                    stream.Position -= read - i;
                    return content[0..i];
                }
            }
        }
        while (stream.Position < stream.Length);

        return content;
    }

    /// <summary>
    /// Returns the next <paramref name="numBytes"/> from the stream.
    /// </summary>
    /// <remarks>
    /// This method returns the next specified number of bytes from the stream, decoded as UTF8.<para></para>
    /// If there are fewer than the requested number of bytes left in the stream, the rest of the stream will be returned. <para></para>
    /// The stream will advance by the number of bytes returned.
    /// </remarks>
    public static async Task<string> GetAsync(this Stream stream, int numBytes = 1024)
    {
        var buffer = new byte[numBytes];

        var read = await stream.ReadAsync(buffer.AsMemory(0, numBytes));

        return Encoding.UTF8.GetString(buffer, 0, read);
    }

    /// <summary>
    /// Advance the stream to the next non-whitespace character.
    /// </summary>
    public static void AdvancePastWhitepace(this Stream stream)
    {
        if (stream.Position == stream.Length)
        {
            return;
        }

        string? str;
        do
        {
            var i = stream.ReadByte();
            str = _defaultEncoding.GetString(new[] { (byte)i });

            if (!string.IsNullOrWhiteSpace(str))
            {
                stream.Position--;
                break;
            }
        }
        while (stream.Position < stream.Length);

        return;
    }

    /// <summary>
    /// Read the remaining contents of the stream.
    /// </summary>
    public static async Task<byte[]> ReadToEndAsync(this Stream stream)
    {
        using var ms = new MemoryStream();

        await stream.CopyToAsync(ms);

        return ms.ToArray();
    }

    public static async Task<Stream> UncompressAsync(
        this Stream stream,
        IEnumerable<Name>? filters,
        IEnumerable<Dictionary>? decodeParms
        )
    {
        ArgumentNullException.ThrowIfNull(stream, nameof(stream));

        filters ??= [];
        decodeParms ??= [];

        // TODO: stream contents may be encrypted, decrypt.

        stream.Position = 0;

        // If there are no filters, return the source data as-is.
        if (!filters.Any())
        {
            return stream;
        }

        var workingData = await stream.ReadToEndAsync();

        var filterInstances = FilterFactory.CreateFilterInstances(filters, decodeParms);

        foreach (var filter in filterInstances)
        {
            workingData = filter.Decode(workingData);
        }

        return new MemoryStream(workingData);
    }
}
