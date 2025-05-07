using MorseCode.ITask;
using ZingPDF.Logging;
using ZingPDF.Syntax;

namespace ZingPDF.Parsing.Parsers
{
    internal class PdfObjectGroupParser : IObjectParser<PdfObjectGroup>
    {
        private readonly IPdfContext _pdfContext;

        public PdfObjectGroupParser(IPdfContext pdfContext)
        {
            ArgumentNullException.ThrowIfNull(pdfContext, nameof(pdfContext));

            _pdfContext = pdfContext;
        }

        public async ITask<PdfObjectGroup> ParseAsync(Stream stream, ParseContext context)
        {
            Logger.Log(LogLevel.Trace, $"Parsing PdfObjectGroup from {stream.GetType().Name} at offset: {stream.Position}.");

            var items = new List<IPdfObject>();

            while (stream.Position < stream.Length)
            {
                var type = await TokenTypeIdentifier.TryIdentifyAsync(stream);

                if (type != null)
                {
                    try
                    {
                        items.Add(await _pdfContext.Parser.For(type).ParseAsync(stream, context));
                    }
                    catch
                    {
                        // If any exception is thrown, gracefully exit.
                        // The sub-object could be invalid or not understood by this library.
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

            return new PdfObjectGroup(items, context.Origin);
        }
    }
}