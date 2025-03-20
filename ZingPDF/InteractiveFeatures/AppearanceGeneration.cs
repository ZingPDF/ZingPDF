//using Nito.AsyncEx;
//using SkiaSharp;
//using System.Text;
//using ZingPDF.Elements.Drawing;
//using ZingPDF.Elements.Drawing.Text;
//using ZingPDF.Extensions;
//using ZingPDF.Graphics;
//using ZingPDF.Graphics.FormXObjects;
//using ZingPDF.IncrementalUpdates;
//using ZingPDF.InteractiveFeatures.Annotations.AppearanceStreams;
//using ZingPDF.InteractiveFeatures.Forms;
//using ZingPDF.Parsing.Parsers;
//using ZingPDF.Syntax;
//using ZingPDF.Syntax.CommonDataStructures;
//using ZingPDF.Syntax.ContentStreamsAndResources;
//using ZingPDF.Syntax.Objects;
//using ZingPDF.Syntax.Objects.Dictionaries;
//using ZingPDF.Syntax.Objects.IndirectObjects;
//using ZingPDF.Syntax.Objects.Streams;
//using ZingPDF.Syntax.Objects.Strings;
//using static ZingPDF.Syntax.ContentStreamsAndResources.ContentStream.Operators;

//namespace ZingPDF.InteractiveFeatures;

//internal class AppearanceGeneration
//{
//    private readonly PdfObjectManager _pdfObjectManager;

//    public AppearanceGeneration(PdfObjectManager pdfObjectManager)
//    {
//        ArgumentNullException.ThrowIfNull(pdfObjectManager, nameof(pdfObjectManager));

//        _pdfObjectManager = pdfObjectManager;
//    }

//    /// <summary>
//    /// Create an appearance stream for the provided text value and set it on the provided field.
//    /// </summary>
//    /// <remarks>
//    /// <para>This is a very complex section of the spec, take care if refactoring.</para>
//    /// 
//    /// <para>
//    /// For semantic or metadata purposes, the /V entry of the field dictionary contains the value. For display
//    /// purposes however, readers build an appearance stream for the visual representation of the field. Prior to 
//    /// version 2, it was not mandatory to create appearance streams for fields when writing a PDF. It was only required to 
//    /// provide a default appearance ( /DA ) string. This string could be used to generate a stream by the reader 
//    /// when rendering the page and when editing a field. To enable this behaviour, the writer sets the `NeedAppearances` 
//    /// entry of the form to `true`.
//    /// </para>
//    /// 
//    /// <para>
//    /// Version 2 makes it mandatory to provide an appearance stream for a field, and deprecates the NeedAppearances flag. 
//    /// Therefore, to support version 2 PDFs, when writing a field value, we must also build an appearance stream, and we must 
//    /// do so in a manner similar to how readers (particularly Acrobat Reader) do so. The reader controls how the field looks 
//    /// during editing, so it's important to provide an appearance stream which looks as close as identical to the generated 
//    /// field appearance. This way, there is no visual change when the user clicks into the field.
//    /// </para>
//    /// 
//    /// <para>
//    /// It's important to remember that the PDF spec is not the Acrobat spec, but was developed alongside it. It contains
//    /// allusions to key functionality of Acrobat, without revealing its workings. One prominant example is how it describes 
//    /// variable text field updates. "Variable text" is text which is not known until viewing time, such as editable fields. 
//    /// The spec describes how a PDF processor should construct an appearance stream for such fields. This is the way that 
//    /// Acrobat generates appearance streams, and should be followed to the letter when we are generating one.
//    /// </para>
//    /// 
//    /// <para>
//    /// It shows an example of an appearance stream as follows.
//    /// </para>
//    /// 
//    /// <para>
//    /// <code>
//    /// Example     The appearance stream includes the following section of marked-content, which represents the portion of the stream that draws the text:
//    /// /Tx BMC                                                                                  %Begin marked-content with tag Tx
//    ///      q                                                                                   %Save graphics state
//    ///              … Any required graphics state changes, such as clipping …
//    ///          BT                                                                              %Begin text object
//    ///              … Default appearance string ( DA ) …
//    ///              … Text-positioning and text-showing operators to show the variable text …
//    ///          ET                                                                              %End text object
//    ///      Q                                                                                   %Restore graphics state
//    /// EMC     
//    /// </code>
//    /// </para>
//    /// 
//    /// <para>
//    /// Note the marked content area wrapped by `/Tx BMC` and `EMC`. This is used by Acrobat Reader to find the text operations 
//    /// to replace when editing the field. Interestingly, if the marked content markers are missing, Acrobat just adds its own 
//    /// text operations onto the end of the stream, causing text duplication. This is alluded to in the spec... "If the existing 
//    /// appearance stream contains no marked-content with tag Tx, the new contents shall be appended to the end of the original stream." 
//    /// So while the spec doesn't go out of its way to state that marked content regions are a requirement, they are. From a different 
//    /// part of the spec "Marked-content operators (PDF 1.2) may identify a portion of a PDF content stream as a marked-content element 
//    /// of interest to a particular PDF processor". So we can conclude that `/Tx` marks variable text content for Acrobat Reader.
//    /// </para>
//    /// 
//    /// <para>There are 2 main states in which we will find a field: with and without an existing /AP. </para>
//    /// <para>
//    /// <list type="number">
//    /// <item>
//    /// If /AP does not exist, we create a new one, calculated from /V, /DA and /Q, ensuring to wrap it in a marked content region so 
//    /// that Acrobat Reader can replace it during editing.
//    /// </item>
//    /// <item>
//    /// If /AP does exist, the spec suggests replacing the marked content region with new instructions for the new text value, and if 
//    /// there is no marked content, to simply append the new text instructions to the existing stream. While this would serve to 
//    /// preserve visual elements, in practice this will cause issues as a lot of PDFs neglect to add the marked content region. A more 
//    /// reliable approach is to parse the existing stream and replace the text instructions, while adding a marked content region around them.
//    /// </item>
//    /// </list>
//    /// </para>
//    /// </remarks>
//    public async Task SetAppearanceStreamForTextAsync(
//        InteractiveFormDictionary formDictionary,
//        FieldDictionary fieldDictionary,
//        LiteralString value,
//        ResourceDictionary? defaultResources,
//        Name defaultFontResource
//        )
//    {

//        // Replace EOL characters with T* operators
//        // TODO: should this only occur for multiline fields?
//        // TODO: test this
//        value = value.Value.Replace(new string(Constants.EndOfLineCharacters), $") {TextPositioning.TStar} (");

//        var defaultApManager = new VariableTextAppearanceStreamManager(formDictionary, fieldDictionary, _pdfObjectManager);

//        await defaultApManager.WriteTextAsync(value);

//        //Number? fontSize = await defaultApManager.GetFontSizeAsync();
//        //Name? fontResourceName = await defaultApManager.GetFontResourceNameAsync();

//        ////ContentStream defaultAppearance = await GetDefaultAppearanceAsync(fieldDictionary, formDefaultAppearanceString);

//        ////await EnsureDAContainsValidFontOperationAsync(fieldDictionary, defaultResources, defaultAppearance, defaultFontResource);

//        ////(Number, Coordinate) fontFit = await GetFontSizeAndPositionAsync(fieldDictionary, defaultAppearance, defaultFontResource);

//        //// If /AP or /AP > N is missing, construct a new appearance stream
//        //// This is common for new fields that have not yet been edited.
//        //if (normalAppearance == null)
//        //{
//        //    await SetAppearanceStreamAsync(
//        //        fieldDictionary,
//        //        new ContentStream().WriteTextContentRegion(stream => defaultApManager.WriteNewAppearanceStreamAsync(stream, value)),
//        //        defaultResources
//        //        );

//        //    return;
//        //}

//        //// Parse existing appearance stream. If there's a /Tx marked content region, replace contents.
//        //// If there's no /Tx region, find the text if any, and replace with a new value, wrapped with
//        //// a /Tx region so Acrobat Reader can process it.

//        //StreamObject<IStreamDictionary> normalApStream = await GetStreamObjectFromNormalAppearanceEntry(normalAppearance);

//        //var apData = await normalApStream.GetDecompressedDataAsync(_pdfObjectManager);

//        //var normalAppearanceStream = await new ContentStreamParser().ParseAsync(apData);

//        //// Locate the marked content region if present.
//        //var textContentMarkerIndex = normalAppearanceStream.Operations.FindIndex(x =>
//        //    x.Operator == MarkedContent.BMC
//        //    && x.Operands != null
//        //    && x.GetOperand<Name>(0) == Constants.Acrobat.MarkedContent.Tx);

//        //// If there is no marked region, the spec says: "If the existing appearance stream contains no marked-content
//        //// with tag Tx, the new contents shall be appended to the end of the original stream." This is a bit heavy handed 
//        //// as it will visibly duplicate text if a field has a text showing operation but does not have the marked content 
//        //// region. If we do not find the region, replace existing text operations with a new marked content region.
//        //if (textContentMarkerIndex == -1)
//        //{
//        //    // Get all indices of operations that are text showing (e.g. Tj, TJ, ', ")
//        //    var textOperationIndices = Enumerable.Range(0, normalAppearanceStream.Operations.Count)
//        //        .Where(i => TextShowing.All.Contains(normalAppearanceStream.Operations[i].Operator))
//        //        .ToList();

//        //    if (textOperationIndices.Count != 0)
//        //    {
//        //        // There are existing text operations.
//        //        // The best we can do to preserve existing formatting while replacing the text value is to 
//        //        // find the first and last text showing operations and replace with a new marked content region.
//        //        // We take all operations before and after existing text and place it outside the region.
//        //        // This attempts to preserve existing formatting during all future field edits.

//        //        normalAppearanceStream = new ContentStream()
//        //            .AddOperations(normalAppearanceStream.Operations.Take(textOperationIndices.First()))
//        //            .WriteTextContentRegion(stream => stream.WriteTextFromDefaultAppearance(
//        //                value,
//        //                fontFit.Item2,
//        //                defaultAppearance.Operations
//        //                ))
//        //            .AddOperations(normalAppearanceStream.Operations.Skip(textOperationIndices.Last() + 1));
//        //    }
//        //    else
//        //    {
//        //        // There are no text showing operations, simply append a new marked content region at the end.
//        //        normalAppearanceStream.WriteTextContentRegion(stream => stream.WriteTextFromDefaultAppearance(
//        //            value,
//        //            fontFit.Item2,
//        //            defaultAppearance.Operations
//        //            ));
//        //    }
//        //}
//        //else
//        //{
//        //    // There is a marked content region, replace contents with new operations
//        //    normalAppearanceStream.ClearAndOperateBetween(
//        //        x => x.Operator == MarkedContent.BMC && x.Operands != null && x.GetOperand<Name>(0) == Constants.Acrobat.MarkedContent.Tx,
//        //        x => x.Operator == MarkedContent.EMC,
//        //        stream => stream.WriteTextFromDefaultAppearance(
//        //            value,
//        //            fontFit.Item2,
//        //            defaultAppearance.Operations
//        //            )
//        //        );
//        //}

//        //// From the spec: "To update an existing appearance stream to reflect a new field value, the interactive PDF processor shall
//        //// first copy any needed resources from the document’s DR dictionary (see "Table 224 — Entries in the interactive
//        //// form dictionary") into the stream’s Resources dictionary. (If the DR and Resources dictionaries contain
//        //// resources with the same name, the one already in the Resources dictionary shall be left intact, not replaced
//        //// with the corresponding value from the DR dictionary.)"
//        //var newResourceDictionary = normalApStream.Dictionary.MergeInto(defaultResources);

//        //await SetAppearanceStreamAsync(fieldDictionary, normalAppearanceStream, ResourceDictionary.FromDictionary(newResourceDictionary));
//    }

//    private async Task<StreamObject<IStreamDictionary>> GetStreamObjectFromNormalAppearanceEntry(Either<StreamObject<IStreamDictionary>, Dictionary> normalAppearance)
//    {
//        StreamObject<IStreamDictionary> normalApStream;

//        if (normalAppearance.Value is StreamObject<IStreamDictionary> st)
//        {
//            normalApStream = st;
//        }
//        else if (normalAppearance.Value is Dictionary normalApDict)
//        {
//            // For a text field, the normal appearance value is unlikely to be an appearance subdictionary.
//            // If it is, it's poorly written, and may have an on and off state, similar to a checkbox
//            var onStateApRef = normalApDict.FirstOrDefault(k => k.Key != Constants.ButtonStates.Off).Value as IndirectObjectReference
//                ?? throw new InvalidPdfException("Malformed text field encountered");

//            normalApStream = await _pdfObjectManager.GetAsync<StreamObject<IStreamDictionary>>(onStateApRef)
//                ?? throw new InvalidPdfException("Malformed text field encountered");
//        }
//        else
//        {
//            throw new InvalidOperationException();
//        }

//        return normalApStream;
//    }

//    private async Task<ContentStream> GetDefaultAppearanceAsync(FieldDictionary fieldDictionary, LiteralString? formDefaultAppearanceString)
//    {
//        // /DA is required but inheritable from the parent form
//        if (fieldDictionary.DA == null && formDefaultAppearanceString == null)
//        {
//            throw new InvalidPdfException("Form field or form does not have a default appearance ( /DA ) entry");
//        }

//        var daIsOnForm = fieldDictionary.DA == null;

//        var defaultAppearanceString = (daIsOnForm
//                ? await fieldDictionary.DA!.GetAsync(_pdfObjectManager)
//                : formDefaultAppearanceString
//                )
//            ?? throw new InvalidPdfException("Form field or form does not have a default appearance ( /DA ) entry");

//        var daStream = new MemoryStream(Encoding.ASCII.GetBytes(defaultAppearanceString));

//        return await new ContentStreamParser().ParseAsync(daStream);
//    }

//    //private async Task EnsureNonZeroFontSizeInDefaultAppearanceAsync(
//    //    FieldDictionary fieldDictionary,
//    //    ResourceDictionary? defaultResources,
//    //    ContentStream defaultAppearance,
//    //    Name fallbackFontResource
//    //    )
//    //{
//    //    var fontOperation = defaultAppearance.Operations.FirstOrDefault(x => x.Operator == TextState.Tf);

//    //    // If there is no font operation, or it's zero fix it
//    //    if (fontOperation == null)
//    //    {
//    //        var boundingBox = await fieldDictionary.Rect.GetAsync(_pdfObjectManager);
//    //        var (fontSize, baselineOffset) = await CalculateFontSizeAndBaselineAsync(defaultResources, fallbackFontResource, boundingBox);

//    //        // Tf is required in the default appearance string, so this shouldn't happen. Repair.
//    //        defaultAppearance.SetTextState(fallbackFontResource, fontSize);
//    //    }
//    //    else if (fontOperation.Operands != null && fontOperation.GetOperand<Number>(1) == 0)
//    //    {
//    //        var boundingBox = await fieldDictionary.Rect.GetAsync(_pdfObjectManager);
//    //        var (fontSize, baselineOffset) = await CalculateFontSizeAndBaselineAsync(defaultResources, fontOperation.GetOperand<Name>(0), boundingBox);

//    //        // /DA can autosize text by using zero for font size. This is not supported in an appearance stream.
//    //        // If zero, use the provided fallback value.
//    //        fontOperation.Operands[1] = (Number)fontSize;

//    //        await fieldDictionary.SetDefaultAppearanceAsync(defaultAppearance);
//    //    }
//    //}

//    //private async Task EnsureDAContainsValidFontOperationAsync(
//    //    FieldDictionary fieldDictionary,
//    //    ResourceDictionary? defaultResources,
//    //    ContentStream defaultAppearance,
//    //    Name fallbackFontResource
//    //    )
//    //{
//    //    var fontOperation = defaultAppearance.Operations.FirstOrDefault(x => x.Operator == TextState.Tf);

//    //    // If there is no font operation, or it's zero fix it
//    //    if (fontOperation == null)
//    //    {
//    //        var boundingBox = await fieldDictionary.Rect.GetAsync(_pdfObjectManager);
//    //        var (fontSize, baselineOffset) = await CalculateFontSizeAndBaselineAsync(defaultResources, fallbackFontResource, boundingBox);

//    //        // Tf is required in the default appearance string, so this shouldn't happen. Repair.
//    //        defaultAppearance.SetTextState(fallbackFontResource, fontSize);
//    //    }
//    //    else if (fontOperation.Operands != null && fontOperation.GetOperand<Number>(1) == 0)
//    //    {
//    //        var boundingBox = await fieldDictionary.Rect.GetAsync(_pdfObjectManager);
//    //        var (fontSize, baselineOffset) = await CalculateFontSizeAndBaselineAsync(defaultResources, fontOperation.GetOperand<Name>(0), boundingBox);

//    //        // /DA can autosize text by using zero for font size. This is not supported in an appearance stream.
//    //        // If zero, use the provided fallback value.
//    //        fontOperation.Operands[1] = (Number)fontSize;

//    //        await fieldDictionary.SetDefaultAppearanceAsync(defaultAppearance);
//    //    }
//    //}

//    //private async Task<(float fontSize, float baselineOffset)> CalculateFontSizeAndBaselineAsync(
//    //    ResourceDictionary? defaultResources,
//    //    Name fontResourceName,
//    //    Rectangle boundingBox
//    //    )
//    //{
//    //    var textCalculations = new TextCalculations();

//    //    // Case 1: font is one of the standard 14 fonts included in the spec
//    //    if (StandardPdfFonts.Metrics.TryGetValue(fontResourceName, out var fontMetrics))
//    //    {
//    //        return textCalculations.FitTextInBox(fontMetrics, boundingBox.Height);
//    //    }

//    //    // Font is not one of the standard ones, it should be embedded.
//    //    if (defaultResources?.Font != null)
//    //    {
//    //        var fontDict = await defaultResources.Font.GetAsync(_pdfObjectManager);
//    //        if (fontDict == null)
//    //        {
//    //            // Missing /Font entry in resource dictionary
//    //            // TODO: choose a font
//    //        }
//    //    }

//    //    var fontResource = fontDict.

//    //    // Case 2: font is embedded


//    //    var fontFit = new TextCalculations().FitTextInBox();

//    //    // TODO
//    //    return 10;
//    //}

//    private async Task SetAppearanceStreamAsync(FieldDictionary fieldDictionary, ContentStream appearanceStream, ResourceDictionary resourceDictionary)
//    {
//        var fieldRect = await fieldDictionary.Rect.GetAsync(_pdfObjectManager);

//        var ms = new MemoryStream();

//        await appearanceStream.WriteAsync(ms);

//        // TODO: when reliably complete, add flatedecode filter
//        var contentStreamDictionary = new Type1FormDictionary(
//            bBox: Rectangle.FromSize(fieldRect.Size),
//            resources: resourceDictionary,
//            length: ms.Length,
//            filter: null,
//            decodeParms: null,
//            f: null,
//            fFilter: null,
//            fDecodeParms: null,
//            dL: ms.Length
//            );

//        var apFormXObject = new StreamObject<Type1FormDictionary>(ms, contentStreamDictionary);

//        var apIndirectObject = _pdfObjectManager.Add(apFormXObject);

//        fieldDictionary.SetAppearanceDictionary(AppearanceDictionary.Create(apIndirectObject.Id.Reference));
//    }
//}
