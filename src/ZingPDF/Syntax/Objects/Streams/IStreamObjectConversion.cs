namespace ZingPDF.Syntax.Objects.Streams;

internal interface IStreamObjectConversion
{
    object? ConvertTo(Type target);
}
