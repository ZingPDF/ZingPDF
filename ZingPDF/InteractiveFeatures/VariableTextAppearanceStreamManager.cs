using Nito.AsyncEx;
using System.Text;
using ZingPDF.Elements.Drawing;
using ZingPDF.Elements.Drawing.Text;
using ZingPDF.Extensions;
using ZingPDF.Fonts;
using ZingPDF.Graphics.FormXObjects;
using ZingPDF.IncrementalUpdates;
using ZingPDF.InteractiveFeatures.Annotations.AppearanceStreams;
using ZingPDF.InteractiveFeatures.Forms;
using ZingPDF.Parsing.Parsers;
using ZingPDF.Syntax;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;
using ZingPDF.Syntax.Objects.Strings;
using static ZingPDF.Constants.DictionaryKeys;
using static ZingPDF.Syntax.ContentStreamsAndResources.ContentStream.Operators;

namespace ZingPDF.InteractiveFeatures;

/// <summary>
/// Encapsulates logic for managing the appearance of a variable text field.
/// </summary>
internal class VariableTextAppearanceStreamManager
{
    private readonly InteractiveFormDictionary _formDict;
    private readonly FieldDictionary _fieldDict;
    private readonly PdfObjectManager _pdfObjectManager;
    private readonly IEnumerable<IFontProvider> _fontProviders;

    private readonly AsyncLazy<ResourceDictionary?> _formDefaultResources;

    private readonly AsyncLazy<LiteralString?> _formDA;
    private readonly AsyncLazy<LiteralString?> _fieldDA;
    private readonly AsyncLazy<ContentStream> _defaultAppearanceStream;

    private readonly AsyncLazy<StreamObject<IStreamDictionary>?> _fieldAppearanceStreamObject;
    private readonly AsyncLazy<ContentStream?> _fieldAppearanceStream;

    public VariableTextAppearanceStreamManager(
        InteractiveFormDictionary formDict,
        FieldDictionary fieldDict,
        PdfObjectManager pdfObjectManager,
        IEnumerable<IFontProvider> fontProviders
        )
    {
        ArgumentNullException.ThrowIfNull(formDict, nameof(formDict));
        ArgumentNullException.ThrowIfNull(fieldDict, nameof(fieldDict));
        ArgumentNullException.ThrowIfNull(pdfObjectManager, nameof(pdfObjectManager));
        ArgumentNullException.ThrowIfNull(fontProviders, nameof(fontProviders));

        _formDict = formDict;
        _fieldDict = fieldDict;
        _pdfObjectManager = pdfObjectManager;
        _fontProviders = fontProviders;

        _formDA = new AsyncLazy<LiteralString?>(async () =>
        {
            if (_formDict.DA != null)
            {
                return await _formDict.DA.GetAsync(_pdfObjectManager);
            }

            return null;
        });

        _fieldDA = new AsyncLazy<LiteralString?>(async () =>
        {
            if (_fieldDict.DA != null)
            {
                return await _fieldDict.DA.GetAsync(_pdfObjectManager);
            }

            return null;
        });

        _defaultAppearanceStream = new AsyncLazy<ContentStream>(async () =>
        {
            LiteralString? formDa = await _formDA;
            LiteralString? fieldDa = await _fieldDA;

            var daStream = new MemoryStream(Encoding.ASCII.GetBytes((fieldDa ?? formDa)!));

            return await new ContentStreamParser().ParseAsync(daStream);
        });

        _fieldAppearanceStreamObject = new AsyncLazy<StreamObject<IStreamDictionary>?>(async () =>
        {
            if (_fieldDict.AP == null)
            {
                return null;
            }

            AppearanceDictionary existingAppearanceDictionary = await _fieldDict.AP.GetAsync(_pdfObjectManager);
            if (existingAppearanceDictionary.N == null)
            {
                return null;
            }

            IndirectObject normalAppearanceIndirectObject = await existingAppearanceDictionary.N.GetIndirectObjectAsync(_pdfObjectManager);
            Either<StreamObject<IStreamDictionary>, Dictionary> normalAppearance = await existingAppearanceDictionary.N.GetAsync(_pdfObjectManager);

            return await GetStreamObjectFromNormalAppearanceEntry(normalAppearance);
        });

        _fieldAppearanceStream = new AsyncLazy<ContentStream?>(async () =>
        {
            var normalApStreamObject = await _fieldAppearanceStreamObject;

            if (normalApStreamObject == null)
            {
                return null;
            }

            var apData = await normalApStreamObject.GetDecompressedDataAsync(_pdfObjectManager);

            return await new ContentStreamParser().ParseAsync(apData);
        });

        _formDefaultResources = new AsyncLazy<ResourceDictionary?>(async () =>
        {
            if (_formDict.DR == null)
            {
                return null;
            }

            return ResourceDictionary.FromDictionary(await _formDict.DR.GetAsync(_pdfObjectManager));
        });
    }

    // temp methods for testing
    internal async Task<ContentStream?> GetAPAsync()
    {
        return await _fieldAppearanceStream;
    }

    internal async Task WipeFieldAsync()
    {
        _fieldDict.SetAppearanceDictionary(null);
        _fieldDict.SetValue(null);

        // set zero so we can test how acrobat sizes text when we save from reader
        await _fieldDict.SetDefaultAppearanceAsync(new ContentStream().SetTextState("Helv", 0));
    }

    public async Task WriteTextAsync(LiteralString value)
    {
        StreamObject<IStreamDictionary>? fieldApStreamObject = await _fieldAppearanceStreamObject;
        ContentStream? fieldAp = await _fieldAppearanceStream;
        ResourceDictionary? formDefaultResources = await _formDefaultResources;

        // If there is no existing appearance stream for the field, generate and set a new one
        if (fieldAp == null)
        {
            var newAppearanceStream = new ContentStream()
                .WriteTextContentRegion(async stream => await WriteNewAppearanceStreamAsync(stream, value));

            await SetAppearanceStreamAsync(newAppearanceStream, formDefaultResources);
            return;
        }

        // If there is an existing appearance stream for the field, check for a marked content region
        // If there is a marked content region, generate a new one and replace the existing one
        // If there is no marked content region, add a new one to the end of the stream

        // There is a marked content region, replace contents with new operations
        await fieldAp.ClearAndOperateBetweenAsync(
            x => x.Operator == MarkedContent.BMC && x.Operands != null && x.GetOperand<Name>(0) == Constants.Acrobat.MarkedContent.Tx,
            x => x.Operator == MarkedContent.EMC,
            async stream => await WriteNewAppearanceStreamAsync(stream, value)
        );

        // From the spec: "To update an existing appearance stream to reflect a new field value, the interactive PDF processor shall
        // first copy any needed resources from the document’s DR dictionary (see "Table 224 — Entries in the interactive
        // form dictionary") into the stream’s Resources dictionary. (If the DR and Resources dictionaries contain
        // resources with the same name, the one already in the Resources dictionary shall be left intact, not replaced
        // with the corresponding value from the DR dictionary.)"
        var newResourceDictionary = fieldApStreamObject.Dictionary.MergeInto(formDefaultResources ?? []);

        await SetAppearanceStreamAsync(fieldAp, ResourceDictionary.FromDictionary(newResourceDictionary));
    }

    /// <summary>
    /// Get the font size from the default appearance string. In practice, this can be null or a number, and can be zero for auto-sizing.
    /// </summary>
    public async Task<Number?> GetFontSizeAsync()
    {
        var fontOperation = (await _defaultAppearanceStream).Operations.FirstOrDefault(x => x.Operator == TextState.Tf);

        if (fontOperation == null)
        {
            return null;
        }

        return fontOperation.GetOperand<Number>(1);
    }

    /// <summary>
    /// Get the font name from the default appearance string. In practice, this can be null or a name.
    /// </summary>
    public async Task<Name?> GetFontResourceNameAsync()
    {
        var fontOperation = (await _defaultAppearanceStream).Operations.FirstOrDefault(x => x.Operator == TextState.Tf);

        if (fontOperation == null)
        {
            return null;
        }

        return fontOperation.GetOperand<Name>(0);
    }

    public async Task<Coordinate?> GetDefaultTextOriginAsync()
    {
        var contentStream = await _defaultAppearanceStream;

        var tmOperation = contentStream.Operations.FirstOrDefault(x => x.Operator == TextPositioning.Tm);
        var tdOperations = contentStream.Operations.Where(x => x.Operator == TextPositioning.Td).ToList();

        if (tmOperation != null)
        {
            var x = tmOperation.GetOperand<Number>(4);
            var y = tmOperation.GetOperand<Number>(5);

            // Adjust the coordinates by any Td operations
            foreach (var tdOperation in tdOperations)
            {
                x += tdOperation.GetOperand<Number>(0);
                y += tdOperation.GetOperand<Number>(1);
            }

            return new Coordinate(x, y);
        }
        else if (tdOperations.Count != 0)
        {
            // If no Tm operation is found, but Td operations exist, use the Td translations
            var x = tdOperations.Sum(td => td.GetOperand<Number>(0));
            var y = tdOperations.Sum(td => td.GetOperand<Number>(1));

            return new Coordinate(x, y);
        }

        return null;
    }

    public async Task WriteNewAppearanceStreamAsync(ContentStream stream, LiteralString newText)
    {
        var defaultAppearanceStream = await _defaultAppearanceStream;


        var fieldDimensions = await _fieldDict.Rect.GetAsync(_pdfObjectManager);

        var fontOperation = defaultAppearanceStream.Operations.FirstOrDefault(x => x.Operator == TextState.Tf);
        var fontResourceName = fontOperation.GetOperand<Name>(0);
        var fontSize = fontOperation.GetOperand<Number>(1);

        var formDefaultResources = await _formDefaultResources;
        var fontMapDict = await formDefaultResources.Font.GetAsync(_pdfObjectManager);
        var fontDict = await fontMapDict.Get<Dictionary>(fontResourceName).GetAsync(_pdfObjectManager);
        var fontName = await fontDict.Get<Name>("BaseFont").GetAsync(_pdfObjectManager);

        Coordinate textOrigin;

        if (fontSize == 0)
        {
            TextFit fontFit = new TextCalculations(_fontProviders).CalculateTextFit(fontName, fieldDimensions, newText);

            // This is left aligned. TODO: account for other quadding values, maybe return a TextOrigin object
            textOrigin = fontFit.TextOrigin;

            // Set DA to match calculated font size
            fontOperation.Operands[1] = new Number(fontFit.FontSize);
            await _fieldDict.SetDefaultAppearanceAsync(defaultAppearanceStream);

        }
        else
        {
            textOrigin = await GetDefaultTextOriginAsync() ?? new Coordinate(0, 0); // TODO: fallback should be calculated the same as Acrobat Reader
        }

        stream
            .SaveGraphicsState()
            .BeginTextObject()
            .AddOperations(defaultAppearanceStream.Operations);

        // Find the existing Tm operation
        var tmOperation = defaultAppearanceStream.Operations.FirstOrDefault(x => x.Operator == TextPositioning.Tm);
        if (tmOperation != null)
        {
            // Preserve the existing transformation components
            var a = tmOperation.GetOperand<Number>(0);
            var b = tmOperation.GetOperand<Number>(1);
            var c = tmOperation.GetOperand<Number>(2);
            var d = tmOperation.GetOperand<Number>(3);

            // Update the translation components with the new origin
            stream.SetTextMatrix(a, b, c, d, textOrigin.X, textOrigin.Y);
        }
        else
        {
            // If no existing Tm operation, add a new one with identity matrix and new origin
            stream.SetTextMatrix(1, 0, 0, 1, textOrigin.X, textOrigin.Y);
        }

        stream
            .ShowText(newText)
            .EndTextObject()
            .RestoreGraphicsState();
    }


    public async Task EnsureValid()
    {
        
        // if /DA is on field, ensure it has a font operation
        // if /DA is on form, ensure it has a font operation

        // if /DA is on form, but font size is zero, calculate font size and copy full /DA to field
        // if /DA is on field, but font size is zero, calculate font size and modify operation
    }

    private async Task<StreamObject<IStreamDictionary>> GetStreamObjectFromNormalAppearanceEntry(Either<StreamObject<IStreamDictionary>, Dictionary> normalAppearance)
    {
        StreamObject<IStreamDictionary> normalApStream;

        if (normalAppearance.Value is StreamObject<IStreamDictionary> st)
        {
            normalApStream = st;
        }
        else if (normalAppearance.Value is Dictionary normalApDict)
        {
            // For a text field, the normal appearance value is unlikely to be an appearance subdictionary.
            // If it is, it's poorly written, and may have an on and off state, similar to a checkbox
            var onStateApRef = normalApDict.FirstOrDefault(k => k.Key != Constants.ButtonStates.Off).Value as IndirectObjectReference
                ?? throw new InvalidPdfException("Malformed text field encountered");

            normalApStream = await _pdfObjectManager.GetAsync<StreamObject<IStreamDictionary>>(onStateApRef)
                ?? throw new InvalidPdfException("Malformed text field encountered");
        }
        else
        {
            throw new InvalidOperationException();
        }

        return normalApStream;
    }

    private async Task SetAppearanceStreamAsync(ContentStream appearanceStream, ResourceDictionary resourceDictionary)
    {
        var fieldRect = await _fieldDict.Rect.GetAsync(_pdfObjectManager);

        var ms = new MemoryStream();

        await appearanceStream.WriteAsync(ms);

        // TODO: when reliably complete, add flatedecode filter
        var contentStreamDictionary = new Type1FormDictionary(
            bBox: Rectangle.FromSize(fieldRect.Size),
            resources: resourceDictionary,
            length: ms.Length,
            filter: null,
            decodeParms: null,
            f: null,
            fFilter: null,
            fDecodeParms: null,
            dL: ms.Length
            );

        var apFormXObject = new StreamObject<Type1FormDictionary>(ms, contentStreamDictionary);

        var apIndirectObject = _pdfObjectManager.Add(apFormXObject);

        _fieldDict.SetAppearanceDictionary(AppearanceDictionary.Create(apIndirectObject.Id.Reference));
    }
}
