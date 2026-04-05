
namespace ZingPDF.Parsing;

public interface ITokenTypeIdentifier
{
    Task<Type?> TryIdentifyAsync(Stream stream);
}