using Nito.AsyncEx;
using ZingPDF.Elements.Drawing;
using ZingPDF.Elements.Drawing.Text;
using ZingPDF.Extensions;
using ZingPDF.Fonts;
using ZingPDF.Graphics.FormXObjects;
using ZingPDF.InteractiveFeatures.Annotations.AppearanceStreams;
using ZingPDF.InteractiveFeatures.Forms;
using ZingPDF.Parsing;
using ZingPDF.Parsing.Parsers;
using ZingPDF.Syntax;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;
using ZingPDF.Syntax.Objects.Strings;
using static ZingPDF.Syntax.ContentStreamsAndResources.ContentStream.Operators;

namespace ZingPDF.InteractiveFeatures;

/// <summary>
/// Encapsulates logic for managing the appearance of a variable text field.
/// </summary>
internal class VariableTextAppearanceStreamManager
{
    private readonly InteractiveFormDictionary _formDict;
    private readonly FieldDictionary _fieldDict;
    private readonly IPdf _pdf;
    private readonly IParser<ContentStream> _contentStreamParser;
    private readonly IEnumerable<IFontMetricsProvider> _fontProviders;

    private readonly AsyncLazy<ResourceDictionary?> _formDefaultResources;

    private readonly AsyncLazy<PdfString?> _formDA;
    private readonly AsyncLazy<PdfString?> _fieldDA;
    private readonly AsyncLazy<ContentStream> _defaultAppearanceStream;

    private readonly AsyncLazy<StreamObject<IStreamDictionary>?> _fieldAppearanceStreamObject;
    private readonly AsyncLazy<ContentStream?> _fieldAppearanceStream;

    private readonly ObjectContext _ObjectContext = ObjectContext.WithOrigin(ObjectOrigin.ParsedContentStream);

    public VariableTextAppearanceStreamManager(
        InteractiveFormDictionary formDict,
        FieldDictionary fieldDict,
        IPdf pdf,
        IParser<ContentStream> contentStreamParser,
        IEnumerable<IFontMetricsProvider> fontProviders
        )
    {
        ArgumentNullException.ThrowIfNull(formDict, nameof(formDict));
        ArgumentNullException.ThrowIfNull(fieldDict, nameof(fieldDict));
        ArgumentNullException.ThrowIfNull(pdf, nameof(pdf));
        ArgumentNullException.ThrowIfNull(contentStreamParser, nameof(contentStreamParser));
        ArgumentNullException.ThrowIfNull(fontProviders, nameof(fontProviders));

        _formDict = formDict;
        _fieldDict = fieldDict;
        _pdf = pdf;
        _contentStreamParser = contentStreamParser;
        _fontProviders = fontProviders;

        _formDA = new AsyncLazy<PdfString?>(async () =>
        {
            if (_formDict.DA != null)
            {
                return await _formDict.DA.GetAsync();
            }

            return null;
        });

        _fieldDA = new AsyncLazy<PdfString?>(async () =>
        {
            if (_fieldDict.DA != null)
            {
                return await _fieldDict.DA.GetAsync();
            }

            return null;
        });

        _defaultAppearanceStream = new AsyncLazy<ContentStream>(async () =>
        {
            PdfString? formDa = await _formDA;
            PdfString? fieldDa = await _fieldDA;

            PdfString defaultAppearance = fieldDa ?? formDa
                ?? throw new InvalidPdfException("The field does not define a default appearance string.");

            var daStream = new MemoryStream(defaultAppearance.Bytes);

            return await _contentStreamParser.ParseAsync(daStream, _ObjectContext);
        });

        _fieldAppearanceStreamObject = new AsyncLazy<StreamObject<IStreamDictionary>?>(async () =>
        {
            var existingAppearanceDictionary = await _fieldDict.AP.GetAsync();
            if (existingAppearanceDictionary == null)
            {
                return null;
            }

            var normalAppearance = await existingAppearanceDictionary.N.GetAsync();
            if (normalAppearance == null) // TODO: fix optional Either properties. This can't be null.
            {
                return null;
            }

            return await GetStreamObjectFromNormalAppearanceEntry(normalAppearance);
        });

        _fieldAppearanceStream = new AsyncLazy<ContentStream?>(async () =>
        {
            var normalApStreamObject = await _fieldAppearanceStreamObject;

            if (normalApStreamObject == null)
            {
                return null;
            }

            var apData = await normalApStreamObject.GetDecompressedDataAsync();

            return await _contentStreamParser.ParseAsync(apData, _ObjectContext);
        });

        _formDefaultResources = new AsyncLazy<ResourceDictionary?>(async () =>
        {
            var defaultResources = await _formDict.DR.GetAsync();
            if (defaultResources == null)
            {
                return null;
            }

            return ResourceDictionary.FromDictionary(defaultResources);
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
        await _fieldDict.SetDefaultAppearanceAsync(
            new ContentStream().SetTextState("Helv", 0)
            );
    }

    public async Task WriteTextAsync(PdfString value)
    {
        ContentStream? fieldAp = await _fieldAppearanceStream;
        ResourceDictionary? formDefaultResources = await _formDefaultResources;

        // If there is no existing appearance stream for the field, generate and set a new one
        if (fieldAp == null)
        {
            var newAppearanceStream = await new ContentStream()
                .WriteTextContentRegionAsync(async stream => await WriteNewAppearanceStreamAsync(stream, value));

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
        StreamObject<IStreamDictionary>? fieldApStreamObject = await _fieldAppearanceStreamObject;
        if (fieldApStreamObject == null)
        {
            throw new InvalidPdfException("Expected an appearance stream object for the field.");
        }

        var newResourceDictionary = fieldApStreamObject.Dictionary.MergeInto(formDefaultResources ?? new Dictionary(_pdf, ObjectContext.UserCreated));

        await SetAppearanceStreamAsync(fieldAp, ResourceDictionary.FromDictionary(newResourceDictionary, _pdf, ObjectContext.UserCreated));
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

    public async Task WriteNewAppearanceStreamAsync(ContentStream stream, PdfString newText)
    {
        var defaultAppearanceStream = await _defaultAppearanceStream;


        var fieldDimensions = await _fieldDict.Rect.GetAsync();

        var fontOperation = defaultAppearanceStream.Operations.FirstOrDefault(x => x.Operator == TextState.Tf)
            ?? throw new InvalidPdfException("The default appearance stream does not define a font operation.");
        var fontResourceName = fontOperation.GetOperand<Name>(0);
        var fontSize = fontOperation.GetOperand<Number>(1);

        var formDefaultResources = await _formDefaultResources
            ?? throw new InvalidPdfException("The form does not define default resources for appearance generation.");
        var fontMapDict = await formDefaultResources.Font.GetAsync();
        if (fontMapDict == null)
        {
            throw new InvalidPdfException("The form default resources do not define a font map.");
        }

        var fontDict = await fontMapDict.GetRequiredProperty<Dictionary>(fontResourceName).GetAsync();
        var fontName = await fontDict.GetRequiredProperty<Name>(Constants.DictionaryKeys.Font.BaseFont).GetAsync();

        Coordinate textOrigin;

        TextFit fontFit = new TextCalculations(_fontProviders).CalculateTextFit(fontName, fieldDimensions, newText.Decode());

        // This is left aligned. TODO: account for other quadding values, maybe return a TextOrigin object
        textOrigin = fontFit.TextOrigin;

        if (fontSize == 0)
        {
            // Set DA to match calculated font size
            if (fontOperation.Operands == null || fontOperation.Operands.Count < 2)
            {
                throw new InvalidPdfException("The default appearance stream does not contain writable font operands.");
            }

            fontOperation.Operands[1] = (Number)fontFit.FontSize;
            await _fieldDict.SetDefaultAppearanceAsync(defaultAppearanceStream);

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

            normalApStream = await _pdf.Objects.GetAsync<StreamObject<IStreamDictionary>>(onStateApRef)
                ?? throw new InvalidPdfException("Malformed text field encountered");
        }
        else
        {
            throw new InvalidOperationException();
        }

        return normalApStream;
    }

    private async Task SetAppearanceStreamAsync(ContentStream appearanceStream, ResourceDictionary? resourceDictionary)
    {
        var fieldRect = await _fieldDict.Rect.GetAsync();

        var ms = new MemoryStream();

        await appearanceStream.WriteAsync(ms);

        resourceDictionary ??= new ResourceDictionary(_pdf, ObjectContext.UserCreated);

        var contentStreamDictionary = new Type1FormDictionary(
            pdf: _pdf,
            context: ObjectContext.UserCreated,
            bBox: fieldRect.Size,
            resources: resourceDictionary
            );

        // TODO: when reliably complete, add flatedecode filter (will need to implement GetFilters method in ContentStreamFactory)
        var apFormXObject = await new ContentStreamFactory([appearanceStream]).CreateAsync(contentStreamDictionary, ObjectContext.UserCreated);

        var apIndirectObject = await _pdf.Objects.AddAsync(apFormXObject);

        _fieldDict.SetAppearanceDictionary(
            AppearanceDictionary.Create(
                _pdf,
                ObjectContext.UserCreated,
                apIndirectObject.Reference
                )
            );
    }
}
