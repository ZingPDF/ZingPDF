namespace ZingPDF.Syntax.Objects;

/// <summary>
/// Type of array which will not render square brackets when there is a single child object.
/// </summary>
public class ShorthandArrayObject(IEnumerable<IPdfObject> values, ObjectOrigin objectOrigin)
    : ArrayObject(values, objectOrigin)
{
    public ShorthandArrayObject(ObjectOrigin objectOrigin) : this([], objectOrigin)
    {
    }

    public ShorthandArrayObject() : this(ObjectOrigin.UserCreated)
    {
    }

    protected override async Task WriteOutputAsync(Stream stream)
    {
        if (this.Count() == 1)
        {
            await this.ElementAt(0).WriteAsync(stream);
        }
        else
        {
            await base.WriteOutputAsync(stream);
        }
    }
}
