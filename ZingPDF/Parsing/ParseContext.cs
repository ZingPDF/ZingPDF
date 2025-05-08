namespace ZingPDF.Parsing;

public class ParseContext
{
    public required ObjectOrigin Origin { get; set; }

    public static ParseContext WithOrigin(ObjectOrigin origin)
    {
        return new ParseContext
        {
            Origin = origin
        };
    }
}
