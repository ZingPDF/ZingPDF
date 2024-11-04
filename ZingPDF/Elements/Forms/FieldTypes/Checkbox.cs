using ZingPDF.Syntax.Objects;

namespace ZingPDF.Elements.Forms.FieldTypes;

public class Checkbox
{
    private readonly Action<Name> _onChange;

    internal Checkbox(Name value, bool @checked, Action<Name> onChange)
    {
        Value = value;
        Checked = @checked;

        _onChange = onChange;
    }
    
    public bool Checked { get; }
    public Name Value { get; }

    public void Check()
    {
        _onChange(Value);
    }

    public void Uncheck()
    {
        _onChange(Value);
    }
}