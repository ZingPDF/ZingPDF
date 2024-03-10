using MorseCode.ITask;
using ZingPdf.Core.Objects;
using ZingPdf.Core.Objects.ObjectGroups;

namespace ZingPdf.Core.Parsing.ObjectGroupParsers
{
    internal class PdfObjectGroupParser : IPdfObjectParser<PdfObjectGroup>
    {
        public async ITask<PdfObjectGroup> ParseAsync(Stream stream)
        {
            var items = new List<IPdfObject>();

            while (stream.Position < stream.Length)
            {
                var type = await TokenTypeIdentifier.TryIdentifyAsync(stream);

                if (type != null)
                {
                    try
                    {
                        items.Add(await Parser.For(type).ParseAsync(stream));
                    }
                    catch
                    {
                        // If any exception is thrown, gracefully exit.
                        // The subobject could be invalid or not understood by this library.
                        // There are also scenarios where we don't have complete data, but want to parse what we can anyway,
                        // such as reading a fixed size chunk from the beginning of the file to find the linearization dictionary.
                        break;
                    }
                }
                else
                {
                    stream.Position += 1;
                }
            }

            return items.ToArray();
        }
    }
}