using System.Text;
using ZingPDF.Elements.Drawing;
using ZingPDF.Extensions;
using ZingPDF.Graphics;
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
using static ZingPDF.Syntax.ContentStreamsAndResources.ContentStream.Operators;

namespace ZingPDF.InteractiveFeatures;

internal class AppearanceGeneration
{
    private readonly PdfObjectManager _pdfObjectManager;

    public AppearanceGeneration(PdfObjectManager pdfObjectManager)
    {
        ArgumentNullException.ThrowIfNull(pdfObjectManager, nameof(pdfObjectManager));

        _pdfObjectManager = pdfObjectManager;
    }

    /// <summary>
    /// Create an appearance stream for the provided text value and set it on the provided field.
    /// </summary>
    /// <remarks>
    /// <para>This is a very complex section of the spec, take care if refactoring.</para>
    /// 
    /// <para>
    /// For semantic or metadata purposes, the /V entry of the field dictionary contains the value. For display
    /// purposes however, readers build an appearance stream for the visual representation of the field. Prior to 
    /// version 2, it was not mandatory to create appearance streams for fields when writing a PDF. It was only required to 
    /// provide a default appearance ( /DA ) string. This string could be used to generate a stream by the reader 
    /// when rendering the page and when editing a field. To enable this behaviour, the writer sets the `NeedAppearances` 
    /// entry of the form to `true`.
    /// </para>
    /// 
    /// <para>
    /// Version 2 makes it mandatory to provide an appearance stream for a field, and deprecates the NeedAppearances flag. 
    /// Therefore, to support version 2 PDFs, when writing a field value, we must also build an appearance stream, and we must 
    /// do so in a manner similar to how readers (particularly Acrobat Reader) do so. The reader controls how the field looks 
    /// during editing, so it's important to provide an appearance stream which looks as close as identical to the generated 
    /// field appearance. This way, there is no visual change when the user clicks into the field.
    /// </para>
    /// 
    /// <para>
    /// It's important to remember that the PDF spec is not the Acrobat spec, but was developed alongside it. It contains
    /// allusions to key functionality of Acrobat, without revealing its workings. One prominant example is how it describes 
    /// variable text field updates. "Variable text" is text which is not known until viewing time, such as editable fields. 
    /// The spec describes how a PDF processor should construct an appearance stream for such fields. This is the way that 
    /// Acrobat generates appearance streams, and should be followed to the letter when we are generating one.
    /// </para>
    /// 
    /// <para>
    /// It shows an example of an appearance stream as follows.
    /// </para>
    /// 
    /// <para>
    /// <code>
    /// Example     The appearance stream includes the following section of marked-content, which represents the portion of the stream that draws the text:
    /// /Tx BMC                                                                                  %Begin marked-content with tag Tx
    ///      q                                                                                   %Save graphics state
    ///              … Any required graphics state changes, such as clipping …
    ///          BT                                                                              %Begin text object
    ///              … Default appearance string ( DA ) …
    ///              … Text-positioning and text-showing operators to show the variable text …
    ///          ET                                                                              %End text object
    ///      Q                                                                                   %Restore graphics state
    /// EMC     
    /// </code>
    /// </para>
    /// 
    /// <para>
    /// Note the marked content area wrapped by `/Tx BMC` and `EMC`. This is used by Acrobat Reader to find the text operations 
    /// to replace when editing the field. Interestingly, if the marked content markers are missing, Acrobat just adds its own 
    /// text operations onto the end of the stream, causing text duplication. This is alluded to in the spec... "If the existing 
    /// appearance stream contains no marked-content with tag Tx, the new contents shall be appended to the end of the original stream." 
    /// So while the spec doesn't go out of its way to state that marked content regions are a requirement, they are. From a different 
    /// part of the spec "Marked-content operators (PDF 1.2) may identify a portion of a PDF content stream as a marked-content element 
    /// of interest to a particular PDF processor". So we can conclude that `/Tx` marks variable text content for Acrobat Reader.
    /// </para>
    /// 
    /// <para>There are 2 main states in which we will find a field: with and without an existing /AP. </para>
    /// <para>
    /// <list type="number">
    /// <item>
    /// If /AP does not exist, we create a new one, calculated from /V, /DA and /Q, ensuring to wrap it in a marked content region so 
    /// that Acrobat Reader can replace it during editing.
    /// </item>
    /// <item>
    /// If /AP does exist, the spec suggests replacing the marked content region with new instructions for the new text value, and if 
    /// there is no marked content, to simply append the new text instructions to the existing stream. While this would serve to 
    /// preserve visual elements, in practice this will cause issues as a lot of PDFs neglect to add the marked content region. A more 
    /// reliable approach is to parse the existing stream and replace the text instructions, while adding a marked content region around them.
    /// </item>
    /// </list>
    /// </para>
    /// </remarks>
    public async Task SetAppearanceStreamForTextAsync(
        FieldDictionary fieldDictionary,
        LiteralString value,
        Dictionary defaultResources,
        Name defaultFontResource,
        Number defaultFontSize,
        RGBColour defaultFontColour
        )
    {

        // Replace EOL characters with T* operators
        // TODO: should this only occur for multiline fields?
        // TODO: test this
        value = value.Value.Replace(new string(Constants.EndOfLineCharacters), $") {TextPositioning.TStar} (");

        AppearanceDictionary? existingAppearanceDictionary = null;
        IndirectObject? normalAppearanceIndirectObject = null;
        Either<StreamObject<IStreamDictionary>, Dictionary>? normalAppearance = null;

        if (fieldDictionary.AP != null)
        {
            existingAppearanceDictionary = await fieldDictionary.AP.GetAsync(_pdfObjectManager);

            if (existingAppearanceDictionary.N != null)
            {
                normalAppearanceIndirectObject = await existingAppearanceDictionary.N.GetIndirectObjectAsync(_pdfObjectManager);
                normalAppearance = await existingAppearanceDictionary.N.GetAsync(_pdfObjectManager);
            }
        }

        // If /AP or /AP > N is missing, construct a new appearance stream
        if (normalAppearance == null)
        {
            await ConstructNewAppearanceStreamAsync(fieldDictionary, value, defaultResources);

            return;
        }

        // Parse existing appearance stream. If there's a /Tx marked content region, replace contents.
        // If there's no /Tx region, find the text if any, and replace with a new value, wrapped with
        // a /Tx region so Acrobat Reader can process it.

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

        var apData = await normalApStream.GetDecompressedDataAsync(_pdfObjectManager);

        var normalAppearanceStream = await new ContentStreamParser().ParseAsync(apData);

        // Locate the marked content region if present.
        var textContentMarkerIndex = normalAppearanceStream.Operations.FindIndex(x =>
            x.Operator == MarkedContent.BMC
            && x.Operands != null
            && (Name)x.Operands[0] == Constants.Acrobat.MarkedContent.Tx);

        // If there is no marked region, the spec says: "If the existing appearance stream contains no marked-content
        // with tag Tx, the new contents shall be appended to the end of the original stream." This is a bit heavy handed 
        // as it will visibly duplicate text if a field has a text showing operation but does not have the marked content 
        // region. If we do not find the region, replace existing text operations with a new marked content region.
        if (textContentMarkerIndex == -1)
        {
            // Get all indices of operations that are text showing (e.g. Tj, TJ, ', ")
            var textOperationIndices = Enumerable.Range(0, normalAppearanceStream.Operations.Count)
                .Where(i => TextShowing.All.Contains(normalAppearanceStream.Operations[i].Operator))
                .ToList();

            if (textOperationIndices.Count != 0)
            {
                // There are existing text operations.
                // The best we can do to preserve existing formatting while replacing the text value is to 
                // find the first and last text showing operations and replace with a new marked content region.
                // We take all operations before and after existing text and place it outside the region.
                // This attempts to preserve existing formatting during all future field edits.
                var newContentStream = new ContentStream();

                newContentStream
                    .AddOperations(normalAppearanceStream.Operations.Take(textOperationIndices.First()))
                    .BeginMarkedContentRegion(Constants.Acrobat.MarkedContent.Tx)
                    .BeginTextObject()
                    .ShowText(value)
                    .EndTextObject()
                    .EndMarkedContentRegion()
                    .AddOperations(normalAppearanceStream.Operations.Skip(textOperationIndices.Last() + 1));

                // Replace the original instruction list with the new one.
                normalAppearanceStream = newContentStream;
            }
            else
            {
                // There are no text showing operations, simply append a new marked content region at the end.
                normalAppearanceStream
                    .BeginMarkedContentRegion(Constants.Acrobat.MarkedContent.Tx)
                    .BeginTextObject()
                    .ShowText(value)
                    .EndTextObject()
                    .EndMarkedContentRegion();
            }
        }
        else
        {
            // There is a marked content region, replace contents with new operations
            // The only positioning values which should be applied within this region are those to align the text according to /Q.
            var textOrigin = await CalculateTextOriginAsync(fieldDictionary, value);
            var fontSize = await CalculateFontSizeForFieldAsync(fieldDictionary, value);

            normalAppearanceStream.ClearAndOperateBetween(
                x => x.Operator == MarkedContent.BMC && x.Operands != null && (Name)x.Operands[0] == Constants.Acrobat.MarkedContent.Tx,
                x => x.Operator == MarkedContent.EMC,
                stream => stream
                    .BeginTextObject()
                    .SetTextState("Helv", fontSize)
                    .SetTextPosition(textOrigin)
                    .ShowText(value)
                    .EndTextObject()
                );
        }

        // From the spec: "To update an existing appearance stream to reflect a new field value, the interactive PDF processor shall
        // first copy any needed resources from the document’s DR dictionary (see "Table 224 — Entries in the interactive
        // form dictionary") into the stream’s Resources dictionary. (If the DR and Resources dictionaries contain
        // resources with the same name, the one already in the Resources dictionary shall be left intact, not replaced
        // with the corresponding value from the DR dictionary.)"
        var newResourceDictionary = normalApStream.Dictionary.MergeInto(defaultResources);

        await SetAppearanceStreamAsync(fieldDictionary, normalAppearanceStream, ResourceDictionary.FromDictionary(newResourceDictionary));
    }

    /// <summary>
    /// Creates a new appearance stream and sets it as the /AP entry on the field.
    /// </summary>
    /// <remarks>
    /// To construct an appearance stream from the default appearance string, build a FormXObject with a Type1FormDictionary.
    /// <list type="bullet">
    /// <item>The default appearance string ( /DA ) should use a font from the forms default resources entry ( /DR ), so we use this as the new 
    /// appearance stream's resources dictionary ( /Resources ).</item>
    /// <item>Its bounding box entry ( /BBox ) should be set to 0, 0, width, height, where width and height are taken from the dimensions of the field's /Rect.</item>
    /// <item>The content stream should consist of the existing /DA instructions, wrapped in a marked content section (/Tx).</item>
    /// <item>
    /// If /DA has a Tm (text matrix) operator, this must be modified, with its horizontal and vertical components adjusted according to the 
    /// text value and the quadding value from the field (/QA).
    /// </item>
    /// <item>If /DA does not have a Tm operator, one should be added before text-positioning and text-showing operators.</item>
    /// </list>
    /// </remarks>
    private async Task ConstructNewAppearanceStreamAsync(
        FieldDictionary fieldDictionary,
        LiteralString value,
        Dictionary defaultResources
        )
    {
        // Parse existing /DA
        var daString = await fieldDictionary.DA.GetAsync(_pdfObjectManager); // TODO: this field is required, but nullable because it can be inherited. Look at inheritance.
        var daStream = new MemoryStream(Encoding.ASCII.GetBytes(daString));
        var defaultAppearance = await new ContentStreamParser().ParseAsync(daStream);

        var appearanceStream = new ContentStream()
            .BeginMarkedContentRegion(Constants.Acrobat.MarkedContent.Tx)
            .SaveGraphicsState()
            .BeginTextObject()
            .AddOperations(defaultAppearance.Operations);

        var textOrigin = await CalculateTextOriginAsync(fieldDictionary, value);

        var tmOperation = appearanceStream.Operations.FirstOrDefault(x => x.Operator == TextPositioning.Tm);
        if (tmOperation != null && tmOperation.Operands != null)
        {
            tmOperation.Operands[2] = textOrigin.X;
            tmOperation.Operands[3] = textOrigin.Y;
        }
        else
        {
            // Set new text matrix
            appearanceStream.SetTextMatrix(1, 0, 0, 1, textOrigin.X, textOrigin.Y);
        }

        // /DA can autosize text by using zero for font size. This is not supported in an appearance stream.
        // If zero, calculate a size and modify /DA to match.
        await RepairDAFontSize(fieldDictionary, defaultAppearance, appearanceStream, value);

        appearanceStream
            .ShowText(value)
            .EndTextObject()
            .RestoreGraphicsState()
            .EndMarkedContentRegion();

        await SetAppearanceStreamAsync(fieldDictionary, appearanceStream, ResourceDictionary.FromDictionary(defaultResources));
    }

    private static async Task RepairDAFontSize(FieldDictionary fieldDictionary, ContentStream defaultAppearance, ContentStream appearanceStream, LiteralString value)
    {
        var fontOperation = appearanceStream.Operations.FirstOrDefault(x => x.Operator == TextState.Tf);
        if (fontOperation != null && fontOperation.Operands != null && (Number)fontOperation.Operands[1] == 0)
        {
            Number fontSize = await CalculateFontSizeForFieldAsync(fieldDictionary, value);
            fontOperation.Operands[1] = fontSize;

            var defaultApFontInstruction = defaultAppearance.Operations.First(x => x.Operator == TextState.Tf);
            defaultApFontInstruction.Operands[1] = fontSize;

            await fieldDictionary.SetDefaultAppearanceAsync(defaultAppearance);
        }
    }

    /// <summary>
    /// Given a text value, calculate the offset for its origin based on the field's quadding value ( /Q ).
    /// </summary>
    private async Task<Coordinate> CalculateTextOriginAsync(FieldDictionary fieldDictionary, LiteralString? value)
    {
        // TODO
        return new Coordinate(2, 5);

        //        Number textOriginX = 2;
        //        Number textOriginY = 0;

        //        int alignment = fieldDictionary.Q != null
        //            ? await fieldDictionary.Q?.GetAsync(_pdfObjectManager) ?? 0 // TODO: this can be inherited, look at inheritance.
        //            : 0;

        //        switch (alignment)
        //        {
        //            case 0: // left-justified
        //                textOriginY = 5;
        //                break;
        //            case 1: // centred
        //                textOriginY = 5; // TODO: calculate center
        //                break;
        //            case 2: // right-justified
        //                textOriginY = 5; // TODO: calculate offset by text width
        //                break;
        //        }

        //        return new Coordinate(textOriginX, textOriginY);
    }

    private static async Task<Number> CalculateFontSizeForFieldAsync(FieldDictionary fieldDictionary, LiteralString? value)
    {
        // TODO
        return 10;
    }

    private async Task SetAppearanceStreamAsync(FieldDictionary fieldDictionary, ContentStream appearanceStream, ResourceDictionary resourceDictionary)
    {
        var fieldRect = await fieldDictionary.Rect.GetAsync(_pdfObjectManager);

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

        fieldDictionary.SetAppearanceDictionary(AppearanceDictionary.Create(apIndirectObject.Id.Reference));
    }
}

//    public RGBColour? GetTextColourFromContentStream(ContentStream appearance)
//    {
//        var rgbColourInstruction = appearance.Instructions.FirstOrDefault(x => x.Operator == Colour.rg);
//        var cmykColourInstruction = appearance.Instructions.FirstOrDefault(x => x.Operator == Colour.k);
//        var grayColourInstruction = appearance.Instructions.FirstOrDefault(x => x.Operator == Colour.g);

//        if (rgbColourInstruction != null && rgbColourInstruction.Operands != null && rgbColourInstruction.Operands.Count == 3)
//        {
//            return new RGBColour(
//                (Number)rgbColourInstruction.Operands[0],
//                (Number)rgbColourInstruction.Operands[1],
//                (Number)rgbColourInstruction.Operands[2]
//                );
//        }
//        else if (cmykColourInstruction != null && cmykColourInstruction.Operands != null && cmykColourInstruction.Operands.Count == 4)
//        {
//            // TODO
//        }
//        else if (grayColourInstruction != null && grayColourInstruction.Operands != null && grayColourInstruction.Operands.Count == 1)
//        {
//            // TODO
//        }

//        return null;
//    }

//    /// <summary>
//    /// Get the font name and size from the provided content stream.
//    /// </summary>
//    /// <remarks>
//    /// Attempts to extract the text font and size from the Tf operation if present.
//    /// </remarks>
//    public (Name, Number)? GetFontNameAndSizeFromContentStream(ContentStream appearance)
//    {
//        var textFontAndSizeOperation = appearance.Instructions.FirstOrDefault(x => x.Operator == TextState.Tf);
//        if (textFontAndSizeOperation == null)
//        {
//            return null;
//        }

//        return (
//            (Name)textFontAndSizeOperation.Operands![0],
//            (Number)textFontAndSizeOperation.Operands[1]
//            );
//    }

//    /// <summary>
//    /// Get the clipping rectangle from the provided content stream.
//    /// </summary>
//    /// <remarks>
//    /// Attempts to find a clipping path from the content stream. The stream should have a clipping path operator (W or W*).
//    /// This method will return a rectangle from either the nearest "re" operation, or derive a rectangle from other path construction operations.
//    /// </remarks>
//    public Rectangle? GetBoundingBoxFromContentStream(ContentStream appearance)
//    {
//        // Find the clipping path operator (W or W*)
//        var clippingPathOperationIndex = appearance.Instructions.FindIndex(x => ClippingPaths.All.Contains(x.Operator));
//        if (clippingPathOperationIndex == -1)
//        {
//            return null;
//        }

//        // Search backward for the nearest "re" (rectangle) operator.
//        // This is the most common way to define the clipping path.
//        var rectOpIndex = appearance.Instructions.FindLastIndex(clippingPathOperationIndex, x => x.Operator == PathConstruction.re);
//        var rectOp = appearance.Instructions[rectOpIndex];
//        if (rectOp.Operands?.Count == 4)
//        {
//            return new Rectangle(
//                new((Number)rectOp.Operands.ElementAt(0), (Number)rectOp.Operands.ElementAt(1)),
//                new((Number)rectOp.Operands.ElementAt(2), (Number)rectOp.Operands.ElementAt(3))
//            );
//        }

//        // If no "re" found, compute bounds from other path commands
//        double? minX = null, minY = null, maxX = null, maxY = null;

//        for (int i = clippingPathOperationIndex - 1; i >= 0; i--)
//        {
//            var instr = appearance.Instructions[i];
//            if (instr.Operator == PathConstruction.m || instr.Operator == PathConstruction.l || instr.Operator == PathConstruction.c)
//            {
//                foreach (var operand in instr.Operands ?? Enumerable.Empty<IPdfObject>())
//                {
//                    if (operand is Number num)
//                    {
//                        if (minX is null || num < minX) minX = num;
//                        if (maxX is null || num > maxX) maxX = num;
//                        if (minY is null || num < minY) minY = num;
//                        if (maxY is null || num > maxY) maxY = num;
//                    }
//                }
//            }
//        }

//        if (minX.HasValue && minY.HasValue && maxX.HasValue && maxY.HasValue)
//        {
//            return new Rectangle(new(minX.Value, minY.Value), new(maxX.Value, maxY.Value));
//        }

//        return null;
//    }

//    public Coordinate? GetOriginFromContentStream(ContentStream appearance)
//    {
//        foreach (var instruction in appearance.Instructions)
//        {
//            if (instruction.Operator == TextPositioning.Tm && instruction.Operands?.Count == 6)
//            {
//                // Tm: a b c d e f -> e, f are the origin coordinates
//                return new Coordinate(
//                    (Number)instruction.Operands.ElementAt(4),
//                    (Number)instruction.Operands.ElementAt(5)
//                );
//            }

//            if ((instruction.Operator == TextPositioning.Td || instruction.Operator == TextPositioning.TD) && instruction.Operands?.Count == 2)
//            {
//                // Td / TD: x y -> Relative movement
//                return new Coordinate(
//                    (Number)instruction.Operands.ElementAt(0),
//                    (Number)instruction.Operands.ElementAt(1)
//                );
//            }
//        }

//        return null;
//    }

