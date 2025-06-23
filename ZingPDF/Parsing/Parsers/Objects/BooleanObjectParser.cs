using MorseCode.ITask;
using ZingPDF.Extensions;
using ZingPDF.Logging;
using ZingPDF.Syntax.Objects;

namespace ZingPDF.Parsing.Parsers.Objects
{
    internal class BooleanObjectParser : IParser<BooleanObject>
    {
        private IPdfObjectCollection _pdfObjects;

        public BooleanObjectParser(IPdfObjectCollection pdfObjects)
        {
            _pdfObjects = pdfObjects;
        }

        public async ITask<BooleanObject> ParseAsync(Stream stream, ParseContext context)
        {
            stream.AdvancePastWhitepace();

            //Logger.Log(LogLevel.Trace, $"Parsing Boolean from {stream.GetType().Name} at offset: {stream.Position}.");

            var parsed = bool.Parse(await stream.ReadUpToExcludingAsync([..Constants.Delimiters, ..Constants.WhitespaceCharacters]));
            
            Logger.Log(LogLevel.Trace, $"Parsed Boolean: {{{parsed}}}. {stream.GetType().Name} now at {stream.Position}.");

            return BooleanObject.FromBool(parsed, context.Origin);
        }
    }
}
