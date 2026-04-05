using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF;

internal sealed class PdfObjectGraphCopier
{
    private readonly Pdf _targetPdf;
    private readonly IPdf _sourcePdf;
    private readonly Dictionary<IndirectObjectReference, IndirectObjectReference> _oldToNewMap = [];

    public PdfObjectGraphCopier(Pdf targetPdf, IPdf sourcePdf)
    {
        ArgumentNullException.ThrowIfNull(targetPdf);
        ArgumentNullException.ThrowIfNull(sourcePdf);

        _targetPdf = targetPdf;
        _sourcePdf = sourcePdf;
    }

    public void RegisterMapping(IndirectObjectReference sourceReference, IndirectObjectReference targetReference)
    {
        ArgumentNullException.ThrowIfNull(sourceReference);
        ArgumentNullException.ThrowIfNull(targetReference);

        _oldToNewMap[sourceReference] = targetReference;
    }

    public async Task<IPdfObject> CopyAsync(IPdfObject obj)
    {
        if (obj is Dictionary dictionary)
        {
            return await CopyDictionaryReferencesAsync(dictionary);
        }

        if (obj is IStreamObject streamObject)
        {
            return await CopyStreamObjectReferencesAsync(streamObject);
        }

        if (obj is ArrayObject array)
        {
            return await CopyArrayReferencesAsync(array);
        }

        if (obj is IndirectObjectReference reference)
        {
            return await CopyReferenceAsync(reference);
        }

        return obj;
    }

    public async Task<IndirectObjectReference> CopyReferenceAsync(IndirectObjectReference reference)
    {
        if (_oldToNewMap.TryGetValue(reference, out var value))
        {
            return value;
        }

        var obj = await _sourcePdf.Objects.GetAsync(reference)
            ?? throw new InvalidPdfException("Unable to dereference PDF object from source PDF.");

        IPdfObject target;
        if (obj.Object is IStreamObject streamObject)
        {
            var ms = new MemoryStream();
            streamObject.Data.Position = 0;
            await streamObject.Data.CopyToAsync(ms);
            ms.Position = 0;

            var clonedDictionary = StreamDictionary.FromDictionary(
                streamObject.Dictionary.ToDictionary(
                    entry => entry.Key,
                    entry => (IPdfObject)entry.Value.Clone()),
                _targetPdf,
                streamObject.Dictionary.Context);

            target = new StreamObject<IStreamDictionary>(ms, clonedDictionary, streamObject.Context);
        }
        else
        {
            target = (IPdfObject)obj.Object.Clone();
        }

        var newObj = await _targetPdf.Objects.AddAsync(target);

        _oldToNewMap.Add(reference, newObj.Reference);

        newObj.Object = await CopyAsync(target);

        return newObj.Reference;
    }

    private async Task<Dictionary> CopyDictionaryReferencesAsync(Dictionary dictionary)
    {
        foreach (var entry in dictionary.ToList())
        {
            dictionary.Set(entry.Key, await CopyAsync(entry.Value));
        }

        return dictionary;
    }

    private async Task<IStreamObject> CopyStreamObjectReferencesAsync(IStreamObject streamObject)
    {
        foreach (var entry in streamObject.Dictionary.ToList())
        {
            streamObject.Dictionary.Set(entry.Key, await CopyAsync(entry.Value));
        }

        return streamObject;
    }

    private async Task<ArrayObject> CopyArrayReferencesAsync(ArrayObject array)
    {
        var newArray = new ArrayObject([], array.Context);

        foreach (var item in array)
        {
            newArray.Add(await CopyAsync(item));
        }

        return newArray;
    }
}
