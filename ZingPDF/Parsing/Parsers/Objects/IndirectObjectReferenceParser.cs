using MorseCode.ITask;
using ZingPDF.Extensions;
using ZingPDF.Logging;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Parsing.Parsers.Objects
{
    internal class IndirectObjectReferenceParser : IObjectParser<IndirectObjectReference>
    {
        private IPdfContext _pdfContext;

        public IndirectObjectReferenceParser(IPdfContext pdfContext)
        {
            _pdfContext = pdfContext;
        }

        public async ITask<IndirectObjectReference> ParseAsync(Stream stream, ParseContext context)
        {
            //Logger.Log(LogLevel.Trace, $"Parsing IndirectObjectReference from {stream.GetType().Name} at offset: {stream.Position}.");

            var content = await stream.ReadUpToIncludingAsync(Constants.Characters.IndirectReference);

            content = content.TrimStart();

            var parts = content.Split(Constants.Characters.Whitespace);

            var id = int.Parse(parts[0]);
            var generation = ushort.Parse(parts[1]);

            var ior = new IndirectObjectReference(new(id, generation), context.Origin);

            Logger.Log(LogLevel.Trace, $"Parsed IndirectObjectReference: {{{ior}}}. {stream.GetType().Name} now at: {stream.Position}.");

            return ior;
        }
    }
}
