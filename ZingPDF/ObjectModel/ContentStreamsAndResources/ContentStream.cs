using ZingPDF.Extensions;
using ZingPDF.ObjectModel.Filters;
using ZingPDF.ObjectModel.Objects.Streams;

namespace ZingPDF.ObjectModel.ContentStreamsAndResources;

/// <summary>
/// ISO 32000-2:2020 7.8.2 - Content streams
/// </summary>
internal class ContentStream<TDictionary> : StreamObject<TDictionary> where TDictionary : class, IStreamDictionary
{
    private readonly IEnumerable<IContentStreamInstruction> _instructions;

    public ContentStream(IEnumerable<IContentStreamInstruction> instructions, IEnumerable<IFilter>? filters) : base(filters)
    {
        _instructions = instructions ?? throw new ArgumentNullException(nameof(instructions));
    }

    protected override async Task<Stream> GetSourceDataAsync(TDictionary dictionary)
    {
        var ms = new MemoryStream();

        foreach (var instruction in _instructions)
        {
            await ms.WriteTextAsync(instruction.Operator);
            await ms.WriteWhitespaceAsync();

            await instruction.Operand.WriteAsync(ms);
        }

        ms.Position = 0;

        return ms;
    }

    protected override Task<TDictionary> GetSpecialisedDictionaryAsync() => Task.FromResult((TDictionary)StreamDictionary.Empty());
}
