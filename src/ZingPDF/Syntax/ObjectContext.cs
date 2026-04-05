using ZingPDF.Parsing;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Syntax;

public sealed record ObjectContext
{
    /// <summary>
    /// The origin of the object.
    /// </summary>
    public required ObjectOrigin Origin { get; init; }

    /// <summary>
    /// The object reference of the nearest parent indirect object.
    /// </summary>
    public IndirectObjectReference? NearestParent { get; init; }

    public static ObjectContext FromImplicitOperator => new()
    {
        Origin = ObjectOrigin.ImplicitOperatorConversion,
        NearestParent = null
    };

    public static ObjectContext UserCreated => new()
    {
        Origin = ObjectOrigin.UserCreated,
        NearestParent = null
    };

    public static ObjectContext None => new()
    {
        Origin = ObjectOrigin.None,
        NearestParent = null
    };

    public static ObjectContext WithOrigin(ObjectOrigin origin)
    {
        return new ObjectContext
        {
            Origin = origin
        };
    }
}
