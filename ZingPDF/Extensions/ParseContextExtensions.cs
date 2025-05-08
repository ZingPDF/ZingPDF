using ZingPDF.Parsing;

namespace ZingPDF.Extensions;

public static class ParseContextExtensions
{
    public static ParseContext WithOrigin(this ParseContext context, ObjectOrigin origin)
    {
        context.Origin = origin;

        return context;
    }
}
