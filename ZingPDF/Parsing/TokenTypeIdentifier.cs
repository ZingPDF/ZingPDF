using ZingPDF.Diagnostics;
using ZingPDF.Logging;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.FileStructure;
using ZingPDF.Syntax.FileStructure.CrossReferences;
using ZingPDF.Syntax.FileStructure.Trailer;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;
using ZingPDF.Syntax.Objects.Strings;

namespace ZingPDF.Parsing
{
    internal class TokenTypeIdentifier : ITokenTypeIdentifier
    {
        private const int BufferSize = 128;

        public async Task<Type?> TryIdentifyAsync(Stream stream)
        {
            using var trace = PerformanceTrace.Measure("TokenTypeIdentifier.TryIdentifyAsync");
            long originalPosition = stream.Position;

            try
            {
                byte[] buffer = new byte[BufferSize];
                int read = await stream.ReadAsync(buffer.AsMemory(0, BufferSize));
                int index = 0;

                while (index < read && IsWhite(buffer[index]))
                {
                    index++;
                }

                if (index >= read)
                {
                    return null;
                }

                var identifiedType = IdentifyToken(buffer, index, read);
                Logger.Log(LogLevel.Trace, $"Identified as: {identifiedType?.Name ?? "null"}");
                return identifiedType;
            }
            finally
            {
                stream.Position = originalPosition;
            }
        }

        private static Type IdentifyToken(byte[] buffer, int index, int read)
        {
            byte current = buffer[index];

            return current switch
            {
                (byte)'%' => StartsWith(buffer, index, read, "%PDF-") ? typeof(Header) : typeof(Comment),
                (byte)'/' => typeof(Name),
                (byte)'[' => typeof(ArrayObject),
                (byte)'<' => index + 1 < read && buffer[index + 1] == (byte)'<' ? typeof(Dictionary) : typeof(PdfString),
                (byte)'(' => LooksLikeParsableDateLiteral(buffer, index, read) ? typeof(Date) : typeof(PdfString),
                (byte)'t' when StartsWith(buffer, index, read, "true") => typeof(BooleanObject),
                (byte)'f' when StartsWith(buffer, index, read, "false") => typeof(BooleanObject),
                (byte)'t' when StartsWith(buffer, index, read, Constants.Trailer) => typeof(Trailer),
                (byte)'x' when StartsWith(buffer, index, read, Constants.Xref) => typeof(Keyword),
                (byte)'s' when StartsWith(buffer, index, read, Constants.StartXref) => typeof(Keyword),
                (byte)'s' when StartsWith(buffer, index, read, Constants.StreamStart) => typeof(StreamObject<>),
                (byte)'e' when StartsWith(buffer, index, read, Constants.StreamEnd) => typeof(Keyword),
                (byte)'e' when StartsWith(buffer, index, read, Constants.ObjEnd) => typeof(Keyword),
                (byte)'n' when StartsWith(buffer, index, read, Constants.Null) => typeof(Keyword),
                _ when IsNumberStart(current) => IdentifyNumericToken(buffer, index, read),
                _ => typeof(Keyword),
            };
        }

        private static Type IdentifyNumericToken(byte[] buffer, int index, int read)
        {
            int cursor = index;
            bool hasDigit = false;
            bool hasDecimal = false;

            if (cursor < read && (buffer[cursor] == (byte)'+' || buffer[cursor] == (byte)'-'))
            {
                cursor++;
            }

            while (cursor < read)
            {
                byte current = buffer[cursor];
                if (current is >= (byte)'0' and <= (byte)'9')
                {
                    hasDigit = true;
                    cursor++;
                    continue;
                }

                if (current == (byte)'.' && !hasDecimal)
                {
                    hasDecimal = true;
                    cursor++;
                    continue;
                }

                break;
            }

            if (!hasDigit)
            {
                return typeof(Keyword);
            }

            if (hasDecimal)
            {
                return typeof(Number);
            }

            cursor = SkipWhitespace(buffer, cursor, read);
            int secondNumberStart = cursor;

            while (cursor < read && buffer[cursor] is >= (byte)'0' and <= (byte)'9')
            {
                cursor++;
            }

            if (cursor == secondNumberStart)
            {
                return typeof(Number);
            }

            cursor = SkipWhitespace(buffer, cursor, read);
            if (cursor >= read)
            {
                return typeof(Number);
            }

            return buffer[cursor] switch
            {
                (byte)'R' => typeof(IndirectObjectReference),
                (byte)'o' when StartsWith(buffer, cursor, read, "obj") => typeof(IndirectObject),
                (byte)'f' or (byte)'n' => typeof(CrossReferenceEntry),
                _ => typeof(Number),
            };
        }

        private static int SkipWhitespace(byte[] buffer, int index, int read)
        {
            while (index < read && IsWhite(buffer[index]))
            {
                index++;
            }

            return index;
        }

        private static bool StartsWith(byte[] buffer, int index, int read, string value)
        {
            if (read - index < value.Length)
            {
                return false;
            }

            for (int i = 0; i < value.Length; i++)
            {
                if (buffer[index + i] != (byte)value[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static bool LooksLikeParsableDateLiteral(byte[] buffer, int index, int read)
        {
            if (read - index < 5
                || buffer[index] != (byte)'('
                || buffer[index + 1] != (byte)'D'
                || buffer[index + 2] != (byte)':')
            {
                return false;
            }

            int cursor = index + 3;
            int digitCount = 0;

            while (cursor < read && digitCount < 14 && buffer[cursor] is >= (byte)'0' and <= (byte)'9')
            {
                cursor++;
                digitCount++;
            }

            if (digitCount < 4 || cursor >= read)
            {
                return false;
            }

            if (buffer[cursor] == (byte)')')
            {
                return true;
            }

            if (buffer[cursor] is not ((byte)'+' or (byte)'-'))
            {
                return false;
            }

            cursor++;

            if (!HasDigits(buffer, cursor, read, 2))
            {
                return false;
            }

            cursor += 2;

            if (cursor < read && buffer[cursor] == (byte)'\'')
            {
                cursor++;
            }

            if (!HasDigits(buffer, cursor, read, 2))
            {
                return false;
            }

            cursor += 2;

            if (cursor < read && buffer[cursor] == (byte)'\'')
            {
                cursor++;
            }

            return cursor < read && buffer[cursor] == (byte)')';
        }

        private static bool HasDigits(byte[] buffer, int index, int read, int count)
        {
            if (index + count > read)
            {
                return false;
            }

            for (int i = 0; i < count; i++)
            {
                if (buffer[index + i] is < (byte)'0' or > (byte)'9')
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsNumberStart(byte value)
            => value is (byte)'+' or (byte)'-' or (byte)'.' or >= (byte)'0' and <= (byte)'9';

        private static bool IsWhite(byte value)
            => value is 0x00 or 0x09 or 0x0A or 0x0C or 0x0D or 0x20;
    }
}
