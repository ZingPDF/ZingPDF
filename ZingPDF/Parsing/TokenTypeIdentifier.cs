using System.Buffers;
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

        public Task<Type?> TryIdentifyAsync(Stream stream)
        {
            using var trace = PerformanceTrace.Measure("TokenTypeIdentifier.TryIdentifyAsync");
            var originalPosition = stream.Position;
            var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
            var bufferReturned = false;

            try
            {
                if (CanUseSynchronousRead(stream))
                {
                    var read = stream.Read(buffer, 0, BufferSize);
                    var identifiedType = IdentifyFromBuffer(buffer.AsSpan(0, read));
                    RestoreStreamAndReturnBuffer(stream, originalPosition, buffer);
                    bufferReturned = true;
                    return Task.FromResult(identifiedType);
                }

                var readTask = stream.ReadAsync(buffer.AsMemory(0, BufferSize));
                if (readTask.IsCompletedSuccessfully)
                {
                    var identifiedType = IdentifyFromBuffer(buffer.AsSpan(0, readTask.Result));
                    RestoreStreamAndReturnBuffer(stream, originalPosition, buffer);
                    bufferReturned = true;
                    return Task.FromResult(identifiedType);
                }

                return AwaitAndIdentifyAsync(stream, originalPosition, buffer, readTask);
            }
            catch
            {
                if (!bufferReturned)
                {
                    RestoreStreamAndReturnBuffer(stream, originalPosition, buffer);
                }

                throw;
            }
        }

        private static bool CanUseSynchronousRead(Stream stream)
            => stream is MemoryStream or SubStream;

        private static Type? IdentifyFromBuffer(ReadOnlySpan<byte> content)
        {
            var index = SkipWhitespace(content);

            Type? identifiedType = null;
            if (index < content.Length)
            {
                identifiedType = IdentifyToken(content[index..]);
            }

            if (Logger.LogLevel <= LogLevel.Trace)
            {
                Logger.Log(LogLevel.Trace, $"Identified as: {identifiedType?.Name ?? "null"}");
            }

            return identifiedType;
        }

        private static void RestoreStreamAndReturnBuffer(Stream stream, long originalPosition, byte[] buffer)
        {
            if (stream.Position != originalPosition)
            {
                stream.Position = originalPosition;
            }

            ArrayPool<byte>.Shared.Return(buffer);
        }

        private static async Task<Type?> AwaitAndIdentifyAsync(
            Stream stream,
            long originalPosition,
            byte[] buffer,
            ValueTask<int> readTask)
        {
            try
            {
                var read = await readTask.ConfigureAwait(false);
                return IdentifyFromBuffer(buffer.AsSpan(0, read));
            }
            finally
            {
                RestoreStreamAndReturnBuffer(stream, originalPosition, buffer);
            }
        }

        private static Type IdentifyToken(ReadOnlySpan<byte> token)
        {
            var current = token[0];

            return current switch
            {
                (byte)'%' => StartsWith(token, "%PDF-"u8) ? typeof(Header) : typeof(Comment),
                (byte)'/' => typeof(Name),
                (byte)'[' => typeof(ArrayObject),
                (byte)'<' => token.Length > 1 && token[1] == (byte)'<' ? typeof(Dictionary) : typeof(PdfString),
                (byte)'(' => LooksLikeParsableDateLiteral(token) ? typeof(Date) : typeof(PdfString),
                (byte)'t' when StartsWith(token, "true"u8) => typeof(BooleanObject),
                (byte)'f' when StartsWith(token, "false"u8) => typeof(BooleanObject),
                (byte)'t' when StartsWith(token, "trailer"u8) => typeof(Trailer),
                (byte)'x' when StartsWith(token, "xref"u8) => typeof(Keyword),
                (byte)'s' when StartsWith(token, "startxref"u8) => typeof(Keyword),
                (byte)'s' when StartsWith(token, "stream"u8) => typeof(StreamObject<>),
                (byte)'e' when StartsWith(token, "endstream"u8) => typeof(Keyword),
                (byte)'e' when StartsWith(token, "endobj"u8) => typeof(Keyword),
                (byte)'n' when StartsWith(token, "null"u8) => typeof(Keyword),
                _ when IsNumberStart(current) => IdentifyNumericToken(token),
                _ => typeof(Keyword),
            };
        }

        private static Type IdentifyNumericToken(ReadOnlySpan<byte> token)
        {
            var cursor = 0;
            bool hasDigit = false;
            bool hasDecimal = false;

            if (cursor < token.Length && (token[cursor] == (byte)'+' || token[cursor] == (byte)'-'))
            {
                cursor++;
            }

            while (cursor < token.Length)
            {
                var current = token[cursor];
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

            cursor = SkipWhitespace(token, cursor);
            var secondNumberStart = cursor;

            while (cursor < token.Length && token[cursor] is >= (byte)'0' and <= (byte)'9')
            {
                cursor++;
            }

            if (cursor == secondNumberStart)
            {
                return typeof(Number);
            }

            cursor = SkipWhitespace(token, cursor);
            if (cursor >= token.Length)
            {
                return typeof(Number);
            }

            return token[cursor] switch
            {
                (byte)'R' => typeof(IndirectObjectReference),
                (byte)'o' when StartsWith(token[cursor..], "obj"u8) => typeof(IndirectObject),
                (byte)'f' or (byte)'n' => typeof(CrossReferenceEntry),
                _ => typeof(Number),
            };
        }

        private static int SkipWhitespace(ReadOnlySpan<byte> buffer, int index = 0)
        {
            while (index < buffer.Length && IsWhite(buffer[index]))
            {
                index++;
            }

            return index;
        }

        private static bool StartsWith(ReadOnlySpan<byte> buffer, ReadOnlySpan<byte> value)
        {
            return buffer.Length >= value.Length && buffer[..value.Length].SequenceEqual(value);
        }

        private static bool LooksLikeParsableDateLiteral(ReadOnlySpan<byte> buffer)
        {
            if (buffer.Length < 5
                || buffer[0] != (byte)'('
                || buffer[1] != (byte)'D'
                || buffer[2] != (byte)':')
            {
                return false;
            }

            var cursor = 3;
            var digitCount = 0;

            while (cursor < buffer.Length && digitCount < 14 && buffer[cursor] is >= (byte)'0' and <= (byte)'9')
            {
                cursor++;
                digitCount++;
            }

            if (digitCount < 4 || cursor >= buffer.Length)
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

            if (!HasDigits(buffer, cursor, 2))
            {
                return false;
            }

            cursor += 2;

            if (cursor < buffer.Length && buffer[cursor] == (byte)'\'')
            {
                cursor++;
            }

            if (!HasDigits(buffer, cursor, 2))
            {
                return false;
            }

            cursor += 2;

            if (cursor < buffer.Length && buffer[cursor] == (byte)'\'')
            {
                cursor++;
            }

            return cursor < buffer.Length && buffer[cursor] == (byte)')';
        }

        private static bool HasDigits(ReadOnlySpan<byte> buffer, int index, int count)
        {
            if (index + count > buffer.Length)
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
