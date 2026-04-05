using MorseCode.ITask;
using ZingPDF.Extensions;
using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Parsing.Parsers.Objects;

internal class IndirectObjectReferenceParser : IParser<IndirectObjectReference>
{
    public async ITask<IndirectObjectReference> ParseAsync(Stream stream, ObjectContext context)
    {
        var content = await stream.ReadUpToIncludingAsync(Constants.Characters.IndirectReference);

        content = content.TrimStart();

        var parts = content.Split(Constants.Characters.Whitespace);

        var id = int.Parse(parts[0]);
        var generation = ushort.Parse(parts[1]);

        var ior = new IndirectObjectReference(new(id, generation), context);

        return ior;
    }
}
