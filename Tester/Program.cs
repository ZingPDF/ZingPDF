using System.Text;
using System.Text.RegularExpressions;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Parsing;
using ZingPDF.Syntax.DocumentStructure.PageTree;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax;
using ZingPDF;
using ZingPDF.FromHTML;
using ZingPDF.Elements.Drawing;
using ZingPDF.Graphics;
using ZingPDF.Elements;
using ZingPDF.Elements.Forms.FieldTypes.Text;
using ZingPDF.Elements.Forms.FieldTypes.Button;
using ZingPDF.Elements.Forms.FieldTypes.Choice;
using ZingPDF.Extensions;
using System;
using ZingPDF.Syntax.Objects.Strings;
using ZingPDF.Syntax.FileStructure;
using ZingPDF.Syntax.FileStructure.CrossReferences;
using ZingPDF.Syntax.FileStructure.Trailer;
using ZingPDF.Text;
using ZingPDF.Fonts;
using ZingPDF.GoogleFonts;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using ZingPDF.InteractiveFeatures.Annotations;
using ZingPDF.InteractiveFeatures.Forms;
using ZingPDF.Graphics.Images;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.Streams;
using DrawingPath = ZingPDF.Elements.Drawing.Path;
using IOPath = System.IO.Path;

//using var outputFileStream = new FileStream("output.pdf", FileMode.Create);
//var pdf = new Pdf();

//pdf.AppendPage();

//await pdf.WriteAsync(outputFileStream);

//await CreateNewPdfAndValidate("output.pdf");

//await ParseResaveValidate("Spec/ISO_32000-2-2020.pdf", "output.pdf");
//await ParseResaveValidate("testfiles/pdf/generated-image-heavy.pdf", "output.pdf");
//await ParseResaveValidate("testfiles/pdf/GS9_Color_Management.pdf", "output.pdf");
//await ParseResaveValidate("output.pdf", "output2.pdf");
//await ParseResaveValidate("testfiles/pdf/form.pdf", "output.pdf");
//await ParseResaveValidate("testfiles/pdf/test.pdf", "output.pdf");

//await AppendPdf("testfiles/pdf/minimal3.pdf", "testfiles/pdf/minimal.pdf", "output.pdf");
//await AppendPdf("testfiles/pdf/minimal.pdf", "testfiles/pdf/form.pdf", "output.pdf");
//await AppendPdf("testfiles/pdf/test.pdf", "testfiles/pdf/form.pdf", "output.pdf");
//await AppendPdf("testfiles/pdf/combobox-form.pdf", "testfiles/pdf/test.pdf", "output.pdf");
//await AppendPdf("testfiles/pdf/minimal.pdf", "testfiles/pdf/minimal2.pdf", "output.pdf");
//await Parse("testfiles/pdf/minimal.pdf");
//await Parse("testfiles/pdf/minimal3.pdf");
//await Parse("testfiles/pdf/test.pdf");
//await Parse("testfiles/pdf/form.pdf");
//await Parse("output.pdf");
//await Parse("testfiles/pdf/generated-text-heavy.pdf");
//await Parse("testfiles/pdf/encrypted.pdf");

//await ConvertFromHTML(new Uri("https://www.google.com"), "output.pdf");
//await ConvertFromHTMLContent("testfiles/html/form-test.html", "form-test.pdf");

//await AddPage("testfiles/pdf/test.pdf", "output.pdf");

//await AddTextToPage();

//await AddImageToPage();

//await RunRecentFeatureManualTestsAsync();
//await RemoveHistory();

//await RotatePage();

//await RotateWholeDocument();
//await Watermark("testfiles/pdf/test.pdf", "output.pdf", "CONFIDENTIAL");

var command = Environment.GetCommandLineArgs().Skip(1).FirstOrDefault();

if (string.Equals(command, "sanitize-complex-form", StringComparison.OrdinalIgnoreCase))
{
    await SanitizeComplexFormFixtureAsync();
    return;
}

await CompleteForm("testfiles/pdf/complex-form.pdf", "output.pdf");
//await CompleteForm("testfiles/pdf/combobox-form.pdf", "output.pdf");

//await WipeFields();

//await TempFieldApTest();

//await Test();

//await Decompress("testfiles/pdf/form.pdf", "decompressed-form.pdf");
//await Decompress("testfiles/pdf/generated-mixed-workload.pdf", "decompressed-generated-mixed-workload.pdf");
//await Decompress("testfiles/pdf/combobox-form.pdf", "decompressed-combobox-form.pdf");

//await ExtractText("testfiles/pdf/generated-text-heavy.pdf");
//await ExtractText("testfiles/pdf/combobox-form.pdf");

//var test = new CrossReferenceEntry(0, 0, true, true);

static async Task RunRecentFeatureManualTestsAsync()
{
    var outputDirectory = IOPath.Combine(AppContext.BaseDirectory, "manual-output");
    Directory.CreateDirectory(outputDirectory);
    var runStamp = CreateManualTestRunStamp();

    Console.WriteLine($"Writing manual test artifacts to {outputDirectory}");

    await ManualTest_AddTextWithFontOptionsAsync(outputDirectory, runStamp);
    await ManualTest_TextLayoutOptionsAsync(outputDirectory, runStamp);
    await ManualTest_AddImagesAsync(outputDirectory, runStamp);
    await ManualTest_AddPathsAsync(outputDirectory, runStamp);
    await ManualTest_RegisterStandardFontAsync(outputDirectory, runStamp);

    var localTrueTypeFontPath = TryGetLocalTrueTypeFontPath();
    if (localTrueTypeFontPath is not null)
    {
        await ManualTest_RegisterTrueTypeFontsAsync(outputDirectory, runStamp, localTrueTypeFontPath);
    }
    else
    {
        Console.WriteLine("Skipping embedded TrueType font registration tests because no local .ttf font was found.");
    }

    var googleFontsApiKey = TryGetGoogleFontsApiKey();
    if (!string.IsNullOrWhiteSpace(googleFontsApiKey))
    {
        await ManualTest_RegisterGoogleFontAsync(outputDirectory, runStamp, googleFontsApiKey);
    }
    else
    {
        Console.WriteLine("Skipping Google Fonts registration test because GOOGLE_FONTS_API_KEY is not set.");
    }
}

static string CreateManualTestRunStamp()
    => DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss");

static async Task SanitizeComplexFormFixtureAsync()
{
    var fixturePath = IOPath.GetFullPath("testfiles/pdf/complex-form.pdf");
    var tempOutputPath = IOPath.Combine(IOPath.GetDirectoryName(fixturePath)!, "complex-form.sanitized.tmp.pdf");

    Console.WriteLine($"Sanitizing {fixturePath}");

    using var inputFileStream = new FileStream(fixturePath, FileMode.Open, FileAccess.Read, FileShare.Read);
    var pdf = Pdf.Load(inputFileStream);

    await SanitizeComplexFormFieldsAsync(pdf);
    await SanitizePdfObjectsAsync(pdf);
    await WriteReachablePdfAsync(pdf, tempOutputPath);

    pdf.Dispose();

    File.Copy(tempOutputPath, fixturePath, overwrite: true);
    File.Delete(tempOutputPath);

    Console.WriteLine($"Replaced fixture with sanitized copy: {fixturePath}");
}

static async Task SanitizeComplexFormFieldsAsync(Pdf pdf)
{
    var form = await pdf.GetFormAsync();
    if (form is null)
    {
        return;
    }

    foreach (var field in await form.GetFieldsAsync())
    {
        switch (field)
        {
            case TextFormField textField:
                await textField.SetValueAsync(string.Empty);
                break;

            case CheckboxFormField checkboxField:
                foreach (var option in await checkboxField.GetOptionsAsync())
                {
                    if (option.Selected)
                    {
                        await option.DeselectAsync();
                    }
                }
                break;

            case RadioButtonFormField radioField:
                if (!radioField.Properties.NoToggleToOff)
                {
                    foreach (var option in await radioField.GetOptionsAsync())
                    {
                        if (option.Selected)
                        {
                            await option.DeselectAsync();
                        }
                    }
                }
                break;

            case ComboBoxFormField comboBoxField:
                foreach (var option in await comboBoxField.GetOptionsAsync())
                {
                    if (option.Selected)
                    {
                        await option.DeselectAsync();
                    }
                }
                break;

            case ListBoxFormField listBoxField:
                foreach (var option in await listBoxField.GetOptionsAsync())
                {
                    if (option.Selected)
                    {
                        await option.DeselectAsync();
                    }
                }
                break;
        }
    }

    var documentCatalog = await pdf.Objects.GetDocumentCatalogAsync();
    var formDictionary = await documentCatalog.AcroForm.GetAsync();
    formDictionary?.SetNeedAppearances(BooleanObject.FromBool(true, ObjectContext.UserCreated));
}

static async Task SanitizePdfObjectsAsync(Pdf pdf)
{
    var trailer = await pdf.Objects.GetLatestTrailerDictionaryAsync();
    if (trailer.Info is not null)
    {
        var infoObject = await pdf.Objects.GetAsync(trailer.Info);
        if (infoObject.Object is Dictionary infoDictionary)
        {
            infoDictionary.Set(Constants.DictionaryKeys.DocumentInformation.Title, PdfString.FromTextAuto("Sanitized form fixture", ObjectContext.UserCreated));
            infoDictionary.Set(Constants.DictionaryKeys.DocumentInformation.Author, PdfString.FromTextAuto("Sanitized", ObjectContext.UserCreated));
            infoDictionary.Set(Constants.DictionaryKeys.DocumentInformation.Subject, PdfString.FromTextAuto("Sanitized test artifact", ObjectContext.UserCreated));
            infoDictionary.Set(Constants.DictionaryKeys.DocumentInformation.Keywords, PdfString.FromTextAuto("sanitized,form,fixture", ObjectContext.UserCreated));
            infoDictionary.Set(Constants.DictionaryKeys.DocumentInformation.Creator, PdfString.FromTextAuto("ZingPDF Tester", ObjectContext.UserCreated));
            infoDictionary.Unset(Constants.DictionaryKeys.DocumentInformation.CreationDate);
            infoDictionary.Unset(Constants.DictionaryKeys.DocumentInformation.ModDate);
            pdf.Objects.Update(infoObject);
        }
    }

    var fieldCounter = 1;
    var optionCounter = 1;
    var annotationCounter = 1;

    await foreach (var indirectObject in pdf.Objects)
    {
        switch (indirectObject.Object)
        {
            case PageDictionary pageDictionary:
                pageDictionary.Unset(Constants.DictionaryKeys.PageTree.Page.Contents);
                pageDictionary.Unset(Constants.DictionaryKeys.PageTree.Page.Thumb);
                pageDictionary.Unset(Constants.DictionaryKeys.PageTree.Page.Metadata);
                pageDictionary.Unset(Constants.DictionaryKeys.PageTree.Page.PieceInfo);
                StripPageResources(pageDictionary);
                await RemoveImageReferencesAsync(pageDictionary, pdf);
                pdf.Objects.Update(indirectObject);
                break;

            case FieldDictionary fieldDictionary:
                SanitizeFieldDictionary(fieldDictionary, ref fieldCounter, ref optionCounter, ref annotationCounter);
                await RemoveImageReferencesAsync(fieldDictionary, pdf);
                pdf.Objects.Update(indirectObject);
                break;

            case WidgetAnnotationDictionary widgetAnnotation:
                SanitizeWidgetAnnotation(widgetAnnotation, ref annotationCounter);
                await RemoveImageReferencesAsync(widgetAnnotation, pdf);
                pdf.Objects.Update(indirectObject);
                break;

            case AnnotationDictionary annotationDictionary:
                annotationDictionary.Unset(Constants.DictionaryKeys.Annotation.Contents);
                annotationDictionary.Unset(Constants.DictionaryKeys.Annotation.Lang);
                await RemoveImageReferencesAsync(annotationDictionary, pdf);
                pdf.Objects.Update(indirectObject);
                break;

            case Dictionary dictionary:
                await RemoveImageReferencesAsync(dictionary, pdf);
                pdf.Objects.Update(indirectObject);
                break;
        }
    }
}

static void SanitizeFieldDictionary(FieldDictionary fieldDictionary, ref int fieldCounter, ref int optionCounter, ref int annotationCounter)
{
    fieldDictionary.SetValue(null);
    fieldDictionary.Unset(Constants.DictionaryKeys.Field.DV);
    fieldDictionary.Unset(Constants.DictionaryKeys.Field.VariableText.RV);
    fieldDictionary.Unset(Constants.DictionaryKeys.Field.VariableText.DS);
    fieldDictionary.Unset(Constants.DictionaryKeys.Annotation.Contents);
    fieldDictionary.Unset(Constants.DictionaryKeys.Annotation.Lang);

    if (fieldDictionary.T is not null)
    {
        fieldDictionary.Set(Constants.DictionaryKeys.Field.T, PdfString.FromTextAuto($"Field{fieldCounter:000}", ObjectContext.UserCreated));
        fieldCounter++;
    }

    if (fieldDictionary.TU is not null)
    {
        fieldDictionary.Set(Constants.DictionaryKeys.Field.TU, PdfString.FromTextAuto("Sanitized field", ObjectContext.UserCreated));
    }

    if (fieldDictionary.TM is not null)
    {
        fieldDictionary.Set(Constants.DictionaryKeys.Field.TM, PdfString.FromTextAuto($"Map{fieldCounter:000}", ObjectContext.UserCreated));
    }

    if (fieldDictionary.Opt is not null)
    {
        var existingOptions = fieldDictionary.GetAs<ArrayObject>(Constants.DictionaryKeys.Field.Opt);
        if (existingOptions is not null)
        {
            fieldDictionary.Set(Constants.DictionaryKeys.Field.Opt, SanitizeChoiceOptions(existingOptions, ref optionCounter));
        }
    }

    SanitizeWidgetAnnotation(fieldDictionary, ref annotationCounter);
}

static ArrayObject SanitizeChoiceOptions(ArrayObject options, ref int optionCounter)
{
    var sanitizedOptions = new ArrayObject([], ObjectContext.UserCreated);

    foreach (var option in options)
    {
        if (option is ArrayObject pair && pair.Get<PdfString>(0) is { } exportValue && pair.Get<PdfString>(1) is { } displayValue)
        {
            sanitizedOptions.Add(new ArrayObject(
            [
                PdfString.FromTextAuto($"Option{optionCounter:000}", ObjectContext.UserCreated, syntax: exportValue.Syntax),
                PdfString.FromTextAuto($"Choice {optionCounter:000}", ObjectContext.UserCreated, syntax: displayValue.Syntax)
            ], ObjectContext.UserCreated));
            optionCounter++;
            continue;
        }

        if (option is PdfString textValue)
        {
            sanitizedOptions.Add(PdfString.FromTextAuto($"Option{optionCounter:000}", ObjectContext.UserCreated, syntax: textValue.Syntax));
            optionCounter++;
            continue;
        }

        sanitizedOptions.Add((IPdfObject)option.Clone());
    }

    return sanitizedOptions;
}

static void SanitizeWidgetAnnotation(WidgetAnnotationDictionary widgetAnnotation, ref int annotationCounter)
{
    widgetAnnotation.Unset(Constants.DictionaryKeys.Annotation.Contents);
    widgetAnnotation.Unset(Constants.DictionaryKeys.Annotation.Lang);

    if (widgetAnnotation.NM is not null)
    {
        widgetAnnotation.Set(Constants.DictionaryKeys.Annotation.NM, PdfString.FromTextAuto($"Annot{annotationCounter:000}", ObjectContext.UserCreated));
        annotationCounter++;
    }

    if (widgetAnnotation.GetAs<Dictionary>(Constants.DictionaryKeys.WidgetAnnotation.MK) is { } appearanceCharacteristics)
    {
        if (appearanceCharacteristics.ContainsKey("CA"))
        {
            appearanceCharacteristics.Set("CA", PdfString.FromTextAuto($"Button {annotationCounter:000}", ObjectContext.UserCreated));
        }

        if (appearanceCharacteristics.ContainsKey("RC"))
        {
            appearanceCharacteristics.Unset("RC");
        }

        if (appearanceCharacteristics.ContainsKey("AC"))
        {
            appearanceCharacteristics.Unset("AC");
        }
    }

    if (widgetAnnotation.AS is not null)
    {
        widgetAnnotation.SetAppearanceState(Constants.ButtonStates.Off);
    }
}

static void StripPageResources(PageDictionary pageDictionary)
{
    var resources = pageDictionary.GetAs<Dictionary>(Constants.DictionaryKeys.PageTree.Resources);
    if (resources is null)
    {
        return;
    }

    resources.Unset(Constants.DictionaryKeys.Resource.XObject);

    if (resources.GetAs<ArrayObject>(Constants.DictionaryKeys.Resource.ProcSet) is { } procSet)
    {
        var retainedProcSetNames = procSet
            .OfType<Name>()
            .Where(name => !name.Value.StartsWith("/Image", StringComparison.Ordinal))
            .Select(name => (IPdfObject)name.Clone())
            .ToList();

        procSet.Clear();
        procSet.AddRange(retainedProcSetNames);
    }
}

static void StripXObjectResources(Dictionary dictionary)
{
    if (dictionary.ContainsKey(Constants.DictionaryKeys.Resource.XObject))
    {
        dictionary.Unset(Constants.DictionaryKeys.Resource.XObject);
    }

    if (dictionary.GetAs<ArrayObject>(Constants.DictionaryKeys.Resource.ProcSet) is { } procSet)
    {
        var retainedProcSetNames = procSet
            .OfType<Name>()
            .Where(name => !name.Value.StartsWith("/Image", StringComparison.Ordinal))
            .Select(name => (IPdfObject)name.Clone())
            .ToList();

        procSet.Clear();
        procSet.AddRange(retainedProcSetNames);
    }
}

static async Task RemoveImageReferencesAsync(IPdfObject pdfObject, Pdf pdf)
{
    switch (pdfObject)
    {
        case Dictionary dictionary:
            StripXObjectResources(dictionary);

            foreach (var key in dictionary.Keys.ToList())
            {
                var value = dictionary.InnerDictionary[key];

                if (value is IndirectObjectReference reference)
                {
                    var referencedObject = await pdf.Objects.GetAsync(reference);
                    if (await ContainsImageContentAsync(referencedObject.Object, pdf, []))
                    {
                        dictionary.Unset(key);
                        continue;
                    }
                }

                await RemoveImageReferencesAsync(value, pdf);
            }
            break;

        case ArrayObject array:
            for (var index = array.Count() - 1; index >= 0; index--)
            {
                var value = array[index];

                if (value is IndirectObjectReference reference)
                {
                    var referencedObject = await pdf.Objects.GetAsync(reference);
                    if (await ContainsImageContentAsync(referencedObject.Object, pdf, []))
                    {
                        var rewritten = array
                            .Where((_, itemIndex) => itemIndex != index)
                            .ToList();
                        array.Clear();
                        array.AddRange(rewritten);
                        continue;
                    }
                }

                await RemoveImageReferencesAsync(value, pdf);
            }
            break;
    }
}

static async Task<bool> ContainsImageContentAsync(IPdfObject pdfObject, Pdf pdf, HashSet<IndirectObjectId> visited)
{
    if (IsImageStream(pdfObject))
    {
        return true;
    }

    switch (pdfObject)
    {
        case Dictionary dictionary:
            foreach (var value in dictionary.InnerDictionary.Values)
            {
                if (value is IndirectObjectReference reference)
                {
                    if (!visited.Add(reference.Id))
                    {
                        continue;
                    }

                    var referencedObject = await pdf.Objects.GetAsync(reference);
                    if (await ContainsImageContentAsync(referencedObject.Object, pdf, visited))
                    {
                        return true;
                    }

                    continue;
                }

                if (await ContainsImageContentAsync(value, pdf, visited))
                {
                    return true;
                }
            }
            break;

        case ArrayObject array:
            foreach (var value in array)
            {
                if (value is IndirectObjectReference reference)
                {
                    if (!visited.Add(reference.Id))
                    {
                        continue;
                    }

                    var referencedObject = await pdf.Objects.GetAsync(reference);
                    if (await ContainsImageContentAsync(referencedObject.Object, pdf, visited))
                    {
                        return true;
                    }

                    continue;
                }

                if (await ContainsImageContentAsync(value, pdf, visited))
                {
                    return true;
                }
            }
            break;
    }

    return false;
}

static async Task WriteReachablePdfAsync(Pdf pdf, string outputPath)
{
    var trailer = await pdf.Objects.GetLatestTrailerDictionaryAsync();
    var rootReference = trailer.Root ?? throw new InvalidOperationException("Missing trailer root.");

    var reachableIds = new HashSet<IndirectObjectId>();
    var pendingReferences = new Queue<IndirectObjectReference>();
    var parentMap = new Dictionary<IndirectObjectId, IndirectObjectId?>();
    pendingReferences.Enqueue(rootReference);
    parentMap[rootReference.Id] = null;

    if (trailer.Info is not null)
    {
        pendingReferences.Enqueue(trailer.Info);
        parentMap[trailer.Info.Id] = null;
    }

    while (pendingReferences.Count > 0)
    {
        var reference = pendingReferences.Dequeue();
        if (!reachableIds.Add(reference.Id))
        {
            continue;
        }

        var indirectObject = await pdf.Objects.GetAsync(reference);
        foreach (var childReference in EnumerateReferences(indirectObject.Object))
        {
            parentMap.TryAdd(childReference.Id, reference.Id);
            pendingReferences.Enqueue(childReference);
        }
    }

    var reachableObjects = new List<IndirectObject>(reachableIds.Count);
    foreach (var id in reachableIds.OrderBy(x => x.Index))
    {
        reachableObjects.Add(await pdf.Objects.GetAsync(new IndirectObjectReference(id, ObjectContext.UserCreated)));
    }

    foreach (var imageObject in reachableObjects.Where(x => IsImageStream(x.Object)))
    {
        Console.WriteLine($"Reachable image object: {imageObject.Id.Index} {imageObject.Id.GenerationNumber} obj");
        Console.WriteLine($"Reference chain: {FormatReferenceChain(imageObject.Id, parentMap)}");
    }

    await using var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
    await new Header(await ReadPdfVersionAsync(pdf), ObjectContext.UserCreated).WriteAsync(outputStream);

    foreach (var indirectObject in reachableObjects)
    {
        await indirectObject.WriteAsync(outputStream);
    }

    var xrefTable = new CrossReferenceTable(BuildCrossReferenceSections(reachableObjects), ObjectContext.UserCreated);
    await xrefTable.WriteAsync(outputStream);

    var trailerDictionary = CreateTrailerDictionary(
        size: reachableObjects.Max(x => x.Id.Index) + 1,
        root: rootReference,
        info: trailer.Info is not null && reachableIds.Contains(trailer.Info.Id) ? trailer.Info : null,
        pdf: pdf);
    await new Trailer(trailerDictionary, xrefTable.ByteOffset!.Value, ObjectContext.UserCreated).WriteAsync(outputStream);
}

static string FormatReferenceChain(IndirectObjectId targetId, IReadOnlyDictionary<IndirectObjectId, IndirectObjectId?> parentMap)
{
    var chain = new List<string>();
    IndirectObjectId? current = targetId;

    while (current is not null)
    {
        chain.Add($"{current.Index} {current.GenerationNumber} obj");
        current = parentMap.TryGetValue(current, out var parent) ? parent : null;
    }

    chain.Reverse();
    return string.Join(" -> ", chain);
}

static bool IsImageStream(IPdfObject pdfObject)
    => pdfObject is ZingPDF.Syntax.Objects.Streams.IStreamObject streamObject
       && streamObject.Dictionary.GetAs<Name>(Constants.DictionaryKeys.Subtype)?.Value == ZingPDF.Graphics.XObjectDictionary.Subtypes.Image;

static IEnumerable<IndirectObjectReference> EnumerateReferences(IPdfObject pdfObject)
{
    switch (pdfObject)
    {
        case IndirectObjectReference reference:
            yield return reference;
            yield break;

        case ArrayObject array:
            foreach (var item in array)
            {
                foreach (var childReference in EnumerateReferences(item))
                {
                    yield return childReference;
                }
            }
            yield break;

        case ZingPDF.Syntax.Objects.Streams.IStreamObject streamObject:
            foreach (var childReference in EnumerateReferences(streamObject.Dictionary))
            {
                yield return childReference;
            }
            yield break;

        case Dictionary dictionary:
            foreach (var value in dictionary.InnerDictionary.Values)
            {
                foreach (var childReference in EnumerateReferences(value))
                {
                    yield return childReference;
                }
            }
            yield break;
    }
}

static IEnumerable<CrossReferenceSection> BuildCrossReferenceSections(IReadOnlyCollection<IndirectObject> objects)
{
    var objectsByIndex = objects.ToDictionary(x => x.Id.Index);
    var maxIndex = objectsByIndex.Count == 0 ? 0 : objectsByIndex.Keys.Max();
    var section = new CrossReferenceSection(0, ObjectContext.UserCreated);
    section.Add(CrossReferenceEntry.RootFreeEntry);

    for (var index = 1; index <= maxIndex; index++)
    {
        if (objectsByIndex.TryGetValue(index, out var indirectObject))
        {
            section.Add(new CrossReferenceEntry(indirectObject.ByteOffset!.Value, indirectObject.Id.GenerationNumber, inUse: true, compressed: false, ObjectContext.UserCreated));
        }
        else
        {
            section.Add(new CrossReferenceEntry(0, 0, inUse: false, compressed: false, ObjectContext.UserCreated));
        }
    }

    return [section];
}

static TrailerDictionary CreateTrailerDictionary(int size, IndirectObjectReference root, IndirectObjectReference? info, Pdf pdf)
{
    var createNew = typeof(TrailerDictionary).GetMethod(
        "CreateNew",
        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("Unable to locate TrailerDictionary.CreateNew.");

    var originalId = PdfString.FromBytes(Guid.NewGuid().ToByteArray(), PdfStringSyntax.Hex, ObjectContext.UserCreated);
    var updateId = PdfString.FromBytes(Guid.NewGuid().ToByteArray(), PdfStringSyntax.Hex, ObjectContext.UserCreated);
    var fileIdentifier = new ArrayObject([originalId, updateId], ObjectContext.UserCreated);

    return (TrailerDictionary)createNew.Invoke(
        null,
        [
            size,
            null!,
            root,
            null!,
            info,
            fileIdentifier,
            pdf,
            ObjectContext.UserCreated
        ])!;
}

static async Task<double> ReadPdfVersionAsync(Pdf pdf)
{
    var originalPosition = pdf.Data.Position;

    try
    {
        pdf.Data.Position = 0;
        byte[] headerBytes = new byte[8];
        var read = await pdf.Data.ReadAsync(headerBytes, 0, headerBytes.Length);
        if (read < headerBytes.Length)
        {
            return 2.0;
        }

        var version = Encoding.ASCII.GetString(headerBytes, 5, 3);
        return double.Parse(version, System.Globalization.CultureInfo.InvariantCulture);
    }
    finally
    {
        pdf.Data.Position = originalPosition;
    }
}

static async Task RemoveHistory()
{
    using var inputFileStream = new FileStream("testfiles/pdf/generated-incremental-history.pdf", FileMode.Open);
    using var outputFileStream = new FileStream("output.pdf", FileMode.Create);
    var pdf = Pdf.Load(inputFileStream);
    await pdf.RemoveHistoryAsync();
    await pdf.SaveAsync(outputFileStream);
}

static async Task Watermark(string input, string output, string text)
{
    using var inputFileStream = new FileStream(input, FileMode.Open);
    using var outputFileStream = new FileStream(output, FileMode.Create);

    var pdf = Pdf.Load(inputFileStream);

    await pdf.AddWatermarkAsync(text);

    await pdf.SaveAsync(outputFileStream);
}

static async Task ExtractText(string input)
{
    using var inputFileStream = new FileStream(input, FileMode.Open);
    var pdf = Pdf.Load(inputFileStream);

    var extract = await pdf.ExtractTextAsync();

    extract.ToList().ForEach(x =>
    {
        Console.WriteLine($"Page: {x.PageNumber}, Font: {x.FontName}, Text: {x.Text}, X: {x.X}, Y: {x.Y}");
        Console.WriteLine();
    });
}

static async Task Decompress(string input, string output)
{
    using var inputFileStream = new FileStream(input, FileMode.Open);
    using var outputFileStream = new FileStream(output, FileMode.Create);

    var pdf = Pdf.Load(inputFileStream);

    await pdf.DecompressAsync();

    await pdf.SaveAsync(outputFileStream);
}

static async Task WipeFields()
{
    using var inputFileStream = new FileStream("testfiles/pdf/combobox-form.pdf", FileMode.Open);
    using var outputFileStream = new FileStream("blank-form.pdf", FileMode.Create);

    var pdf = Pdf.Load(inputFileStream);

    var form = await pdf.GetFormAsync();

    var fields = await form!.GetFieldsAsync();

    var textFields = fields.OfType<TextFormField>();

    foreach (var field in textFields)
    {
        await field.ClearAsync();
    }

    await pdf.SaveAsync(outputFileStream);
}

static async Task TempFieldApTest()
{
    using var inputFileStream = new FileStream("test.pdf", FileMode.Open);

    var pdf = Pdf.Load(inputFileStream);

    var form = await pdf.GetFormAsync();

    var fields = await form!.GetFieldsAsync();

    var textFields = fields.Where(x => x is TextFormField textField);

    foreach (var field in textFields)
    {
        var apStream = await ((TextFormField)field).GetAPAsync();

        if (apStream == null)
        {
            continue;
        }

        using var ms = new MemoryStream();

        await apStream.WriteAsync(ms);

        ms.Position = 0;

        Console.WriteLine($"Field Size: {await field.GetFieldDimensionsAsync()}");
        Console.WriteLine($"Field AP: {await ms.GetAsync()}");
    }
}

static async Task Test()
{
    //using var inputFileStream = new FileStream("testfiles/pdf/generated-text-heavy.pdf", FileMode.Open);
    //using var inputFileStream = new FileStream("testfiles/pdf/complex-form.pdf", FileMode.Open);
    //using var inputFileStream = new FileStream("testfiles/pdf/combobox-form.pdf", FileMode.Open);
    using var inputFileStream = new FileStream("testfiles/pdf/encrypted.pdf", FileMode.Open);

    var pdf = Pdf.Load(inputFileStream);

    //var encrypted = pdf.Encrypted;

    var count = await pdf.GetPageCountAsync();
}

static async Task Parse(string input)
{
    using var inputFileStream = new FileStream(input, FileMode.Open);

    var pdf = Pdf.Load(inputFileStream);

    var pageCount = await pdf.GetPageCountAsync();
}

static async Task AppendPdf(string input1, string input2, string output)
{
    using var inputFileStream1 = new FileStream(input1, FileMode.Open);
    using var inputFileStream2 = new FileStream(input2, FileMode.Open);
    using var outputFileStream = new FileStream("output.pdf", FileMode.Create);

    var pdf = Pdf.Load(inputFileStream1);

    await pdf.AppendPdfAsync(inputFileStream2);

    await pdf.SaveAsync(outputFileStream);
}

static async Task CompleteForm(string input, string output)
{
    using var inputFileStream = new FileStream(input, FileMode.Open);
    using var outputFileStream = new FileStream(output, FileMode.Create);

    var pdf = Pdf.Load(inputFileStream);

    var form = await pdf.GetFormAsync()!;
    
    var fields = await form.GetFieldsAsync();

    foreach (var field in fields)
    {
        if (field is TextFormField textField)
        {
            await textField.SetValueAsync("test");
        }
        else if (field is CheckboxFormField cbField)
        {
            var options = await cbField.GetOptionsAsync();

            await options[0].SelectAsync();
        }
        else if (field is RadioButtonFormField rbField)
        {
            var options = await rbField.GetOptionsAsync();

            await options[0].SelectAsync();
        }
        else if (field is ListBoxFormField listBoxFormField)
        {
            var options = await listBoxFormField.GetOptionsAsync();

            await options[3].SelectAsync();
        }
        else if (field is ComboBoxFormField comboBoxFormField)
        {
            var options = await comboBoxFormField.GetOptionsAsync();

            //options[1].Select();
            await comboBoxFormField.SelectCustomValueAsync("TEST");
        }
    }

    await pdf.SaveAsync(outputFileStream);
}

static async Task AddTextToPage()
{
    using var inputFileStream = new FileStream("test.pdf", FileMode.Open);
    using var outputFileStream = new FileStream("output.pdf", FileMode.Create);

    var pdf = Pdf.Load(inputFileStream);

    var page = await pdf.InsertPageAsync(1, options => options.MediaBox = Rectangle.FromDimensions(200, 200));

    await page.AddTextAsync(new ZingPDF.Text.TextObject(
        "test",
        Rectangle.FromDimensions(200, 200),
        new ZingPDF.Text.FontOptions
        {
            ResourceName = "Helv",
            Size = 24,
            Colour = RGBColour.PrimaryRed
        }));

    await pdf.SaveAsync(outputFileStream);
}

static async Task RotateWholeDocument()
{
    using var inputFileStream = new FileStream("testfiles/pdf/generated-mixed-workload.pdf", FileMode.Open);
    using var outputFileStream = new FileStream("output.pdf", FileMode.Create);

    var pdf = Pdf.Load(inputFileStream);

    await pdf.SetRotationAsync(Rotation.Degrees90);

    await pdf.SaveAsync(outputFileStream);
}

static async Task RotatePage()
{
    using var inputFileStream = new FileStream("testfiles/pdf/test.pdf", FileMode.Open);
    using var outputFileStream = new FileStream("output.pdf", FileMode.Create);

    var pdf = Pdf.Load(inputFileStream);

    var page = await pdf.GetPageAsync(1);

    page.RotateAsync(Rotation.Degrees90);

    await pdf.SaveAsync(outputFileStream);
}

static async Task AddImageToPage()
{
    using var inputFileStream = new FileStream("testfiles/pdf/minimal.pdf", FileMode.Open);
    using var outputFileStream = new FileStream("output.pdf", FileMode.Create);

    var pdf = Pdf.Load(inputFileStream);

    var page = await pdf.GetPageAsync(1);

    //var page = await pdf.InsertPageAsync(1, options => options.MediaBox = Rectangle.FromSize(200, 200));

    await page.AddImageAsync(Image.FromFile("testfiles/image/cat.jpg", Rectangle.FromDimensions(200, 200)));

    await pdf.SaveAsync(outputFileStream);
}

static async Task ManualTest_AddTextWithFontOptionsAsync(string outputDirectory, string runStamp)
{
    using var inputFileStream = OpenTestAsset("test.pdf");
    using var outputFileStream = CreateOutputPdf(outputDirectory, "manual-text-font-options.pdf", runStamp);

    var pdf = Pdf.Load(inputFileStream);
    var page = await pdf.InsertPageAsync(1, options => options.MediaBox = Rectangle.FromDimensions(400, 220));

    await page.AddTextAsync(
        "FontOptions overload using an existing page font resource. jumping glyphs prove descenders stay visible.",
        Rectangle.FromCoordinates(new Coordinate(24, 120), new Coordinate(376, 184)),
        new FontOptions
        {
            ResourceName = "Helv",
            Size = 20,
            Colour = RGBColour.PrimaryBlue
        });

    await pdf.SaveAsync(outputFileStream);
    Console.WriteLine($"Created {outputFileStream.Name}");
}

static async Task ManualTest_TextLayoutOptionsAsync(string outputDirectory, string runStamp)
{
    using var pdf = Pdf.Create();
    using var outputFileStream = CreateOutputPdf(outputDirectory, "manual-text-layout-options.pdf", runStamp);
    var page = await pdf.GetPageAsync(1);
    var font = await pdf.RegisterStandardFontAsync(StandardPdfFonts.Helvetica);

    await page.AddTextAsync(
        "Visible overflow keeps descenders intact and lets the line run naturally beyond the box if needed.",
        Rectangle.FromCoordinates(new Coordinate(24, 180), new Coordinate(300, 232)),
        font,
        18,
        RGBColour.Black);

    await page.AddPathAsync(new DrawingPath(
        new StrokeOptions(new RGBColour(0.75, 0.75, 0.75), 1),
        null,
        PathType.Linear,
        [
            new Coordinate(24, 180),
            new Coordinate(300, 180),
            new Coordinate(300, 232),
            new Coordinate(24, 232),
            new Coordinate(24, 180)
        ]));

    await page.AddTextAsync(
        "Clip overflow keeps everything inside the box, including the end of the sentence.",
        Rectangle.FromCoordinates(new Coordinate(24, 110), new Coordinate(300, 162)),
        font,
        18,
        RGBColour.PrimaryBlue,
        new TextLayoutOptions
        {
            Overflow = TextOverflowMode.Clip
        });

    await page.AddPathAsync(new DrawingPath(
        new StrokeOptions(new RGBColour(0.75, 0.75, 0.75), 1),
        null,
        PathType.Linear,
        [
            new Coordinate(24, 110),
            new Coordinate(300, 110),
            new Coordinate(300, 162),
            new Coordinate(24, 162),
            new Coordinate(24, 110)
        ]));

    await page.AddTextAsync(
        "Shrink to fit keeps the full sentence visible within the available width.",
        Rectangle.FromCoordinates(new Coordinate(24, 40), new Coordinate(300, 92)),
        font,
        18,
        RGBColour.PrimaryRed,
        new TextLayoutOptions
        {
            Overflow = TextOverflowMode.ShrinkToFit
        });

    await page.AddPathAsync(new DrawingPath(
        new StrokeOptions(new RGBColour(0.75, 0.75, 0.75), 1),
        null,
        PathType.Linear,
        [
            new Coordinate(24, 40),
            new Coordinate(300, 40),
            new Coordinate(300, 92),
            new Coordinate(24, 92),
            new Coordinate(24, 40)
        ]));

    await pdf.SaveAsync(outputFileStream);
    Console.WriteLine($"Created {outputFileStream.Name}");
}

static async Task ManualTest_AddImagesAsync(string outputDirectory, string runStamp)
{
    using var pdf = Pdf.Create();
    using var outputFileStream = CreateOutputPdf(outputDirectory, "manual-images-jpg-and-png.pdf", runStamp);
    var page = await pdf.GetPageAsync(1);

    await page.AddImageAsync(
        ResolveTestAssetPath(IOPath.Combine("testfiles", "image", "cat.jpg")),
        Rectangle.FromCoordinates(new Coordinate(24, 24), new Coordinate(204, 204)));

    await using var pngStream = await CreateSamplePngAsync();
    await page.AddImageAsync(
        pngStream,
        Rectangle.FromCoordinates(new Coordinate(220, 40), new Coordinate(380, 140)),
        preserveAspectRatio: false);

    await pdf.SaveAsync(outputFileStream);
    Console.WriteLine($"Created {outputFileStream.Name}");
}

static async Task ManualTest_AddPathsAsync(string outputDirectory, string runStamp)
{
    using var pdf = Pdf.Create();
    using var outputFileStream = CreateOutputPdf(outputDirectory, "manual-drawing-paths.pdf", runStamp);
    var page = await pdf.GetPageAsync(1);

    await page.AddPathAsync(new DrawingPath(
        new StrokeOptions(RGBColour.PrimaryRed, 3),
        null,
        PathType.Linear,
        [
            new Coordinate(30, 30),
            new Coordinate(120, 130),
            new Coordinate(200, 50),
            new Coordinate(280, 140)
        ]));

    await page.AddPathAsync(new DrawingPath(
        new StrokeOptions(RGBColour.PrimaryBlue, 2),
        new FillOptions(new RGBColour(0.78, 0.9, 1)),
        PathType.Bezier,
        [
            new Coordinate(60, 210),
            new Coordinate(110, 310),
            new Coordinate(220, 310),
            new Coordinate(280, 210)
        ]));

    await pdf.SaveAsync(outputFileStream);
    Console.WriteLine($"Created {outputFileStream.Name}");
}

static async Task ManualTest_RegisterStandardFontAsync(string outputDirectory, string runStamp)
{
    using var pdf = Pdf.Create();
    using var outputFileStream = CreateOutputPdf(outputDirectory, "manual-registered-standard-font.pdf", runStamp);
    var page = await pdf.GetPageAsync(1);
    var standardFont = await pdf.RegisterStandardFontAsync(StandardPdfFonts.HelveticaBold);

    await page.AddTextAsync(
        "Registered standard font: Helvetica Bold",
        Rectangle.FromCoordinates(new Coordinate(40, 120), new Coordinate(520, 180)),
        standardFont,
        28,
        RGBColour.Black);

    await page.AddTextAsync(
        "This sample should be clearly visible near the middle of the page.",
        Rectangle.FromCoordinates(new Coordinate(40, 84), new Coordinate(540, 128)),
        standardFont,
        16,
        RGBColour.PrimaryBlue);

    await pdf.SaveAsync(outputFileStream);
    Console.WriteLine($"Created {outputFileStream.Name}");
}

static async Task ManualTest_RegisterTrueTypeFontsAsync(string outputDirectory, string runStamp, string fontPath)
{
    using var pdf = Pdf.Create();
    using var outputFileStream = CreateOutputPdf(outputDirectory, "manual-registered-truetype-fonts.pdf", runStamp);

    var firstPage = await pdf.GetPageAsync(1);
    var pathFont = await pdf.RegisterTrueTypeFontAsync(fontPath);

    await firstPage.AddTextAsync(
        $"Registered from file path: {IOPath.GetFileName(fontPath)}",
        Rectangle.FromCoordinates(new Coordinate(24, 124), new Coordinate(560, 184)),
        pathFont,
        24,
        RGBColour.PrimaryRed);

    await firstPage.AddTextAsync(
        "Embedded TrueType font sample rendered from a local file.",
        Rectangle.FromCoordinates(new Coordinate(24, 88), new Coordinate(560, 128)),
        pathFont,
        15,
        RGBColour.Black);

    var secondPage = await pdf.AppendPageAsync();
    await using var fontStream = File.OpenRead(fontPath);
    var streamFont = await pdf.RegisterTrueTypeFontAsync(fontStream);

    await secondPage.AddTextAsync(
        "Registered from a supplied font stream.",
        Rectangle.FromCoordinates(new Coordinate(24, 124), new Coordinate(520, 184)),
        streamFont,
        24,
        RGBColour.PrimaryBlue);

    await secondPage.AddTextAsync(
        "Second page verifies the stream-based registration path.",
        Rectangle.FromCoordinates(new Coordinate(24, 88), new Coordinate(520, 128)),
        streamFont,
        15,
        RGBColour.Black);

    await pdf.SaveAsync(outputFileStream);
    Console.WriteLine($"Created {outputFileStream.Name}");
}

static async Task ManualTest_RegisterGoogleFontAsync(string outputDirectory, string runStamp, string apiKey)
{
    using var pdf = Pdf.Create();
    using var outputFileStream = CreateOutputPdf(outputDirectory, "manual-registered-google-font.pdf", runStamp);
    var page = await pdf.GetPageAsync(1);
    var client = new GoogleFontsClient(apiKey);
    var font = await pdf.RegisterGoogleFontAsync(
        client,
        new GoogleFontRequest
        {
            Family = "Inter",
            Variant = "regular",
            PreferVariableFont = true
        });

    await page.AddTextAsync(
        "Registered from Google Fonts.",
        Rectangle.FromCoordinates(new Coordinate(24, 124), new Coordinate(420, 184)),
        font,
        24,
        RGBColour.Black);

    await page.AddTextAsync(
        "If this page renders text, Google Fonts registration is working.",
        Rectangle.FromCoordinates(new Coordinate(24, 88), new Coordinate(560, 128)),
        font,
        15,
        RGBColour.PrimaryBlue);

    await pdf.SaveAsync(outputFileStream);
    Console.WriteLine($"Created {outputFileStream.Name}");
}

static FileStream CreateOutputPdf(string outputDirectory, string fileName, string runStamp)
{
    var baseName = IOPath.GetFileNameWithoutExtension(fileName);
    var extension = IOPath.GetExtension(fileName);
    var stampedBaseName = $"{baseName}-{runStamp}";

    for (var attempt = 0; attempt < 20; attempt++)
    {
        var candidateName = attempt == 0
            ? $"{stampedBaseName}{extension}"
            : $"{stampedBaseName}-{attempt + 1}{extension}";
        var candidatePath = IOPath.Combine(outputDirectory, candidateName);

        try
        {
            return new FileStream(candidatePath, FileMode.Create, FileAccess.Write, FileShare.None);
        }
        catch (IOException) when (File.Exists(candidatePath))
        {
        }
    }

    throw new IOException($"Unable to create an output file for '{fileName}' in '{outputDirectory}'.");
}

static FileStream OpenTestAsset(string relativePath)
    => new(ResolveTestAssetPath(relativePath), FileMode.Open, FileAccess.Read, FileShare.Read);

static string ResolveTestAssetPath(string relativePath)
    => IOPath.Combine(AppContext.BaseDirectory, relativePath);

static async Task<MemoryStream> CreateSamplePngAsync()
{
    using var image = new SixLabors.ImageSharp.Image<Rgba32>(120, 80, new Rgba32(36, 132, 228, 220));
    var output = new MemoryStream();
    await image.SaveAsync(output, new PngEncoder());
    output.Position = 0;
    return output;
}

static string? TryGetGoogleFontsApiKey()
    => Environment.GetEnvironmentVariable("GOOGLE_FONTS_API_KEY")
        ?? Environment.GetEnvironmentVariable("ZINGPDF_GOOGLE_FONTS_API_KEY");

static string? TryGetLocalTrueTypeFontPath()
{
    var candidatePaths = new[]
    {
        ResolveTestAssetPath(IOPath.Combine("testfiles", "font", "NotoSans-Regular.ttf")),
        IOPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf"),
        IOPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "segoeui.ttf"),
        IOPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "calibri.ttf"),
        "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
        "/usr/share/fonts/truetype/liberation2/LiberationSans-Regular.ttf",
        "/Library/Fonts/Arial.ttf",
        "/System/Library/Fonts/Supplemental/Arial.ttf"
    };

    return candidatePaths.FirstOrDefault(File.Exists);
}

static async Task ConvertFromHTML(Uri uri, string output)
{
    using var outputFileStream = new FileStream(output, FileMode.Create);

    using var pdfStream = await Converter.ToPdfAsync(uri);

    await pdfStream.CopyToAsync(outputFileStream);
}

static async Task ConvertFromHTMLContent(string htmlFilePath, string output)
{
    using var inputFileStream = new FileStream(htmlFilePath, FileMode.Open);
    using var outputFileStream = new FileStream(output, FileMode.Create);

    var htmlContent = await inputFileStream.ReadToEndAsync();

    using var pdfStream = await Converter.ToPdfAsync(Encoding.UTF8.GetString(htmlContent));

    await pdfStream.CopyToAsync(outputFileStream);
}

static async Task AddPage(string input, string output)
{
    using var inputFileStream = new FileStream(input, FileMode.Open);
    using var outputFileStream = new FileStream(output, FileMode.Create);

    var pdf = Pdf.Load(inputFileStream);

    var count1 = await pdf.GetPageCountAsync();

    var page = await pdf.AppendPageAsync();

    var count2 = await pdf.GetPageCountAsync();

    await pdf.SaveAsync(outputFileStream);
}

static async Task ParseResaveValidate(string input, string output)
{
    using var inputFileStream = new FileStream(input, FileMode.Open);
    var pdf = Pdf.Load(inputFileStream);

    //var test = await pdf.IndirectObjects.GetAsync(new IndirectObjectReference(new IndirectObjectId(17, 0)));

    var page1 = await pdf.GetPageAsync(1);

    //var annots = (await pdf.IndirectObjects.GetAsync<ArrayObject>(page1.Get<PageDictionary>().Annots)).Cast<IndirectObjectReference>();

    //var annotObjs = new List<IPdfObject>();

    //foreach(var ior in annots)
    //{
    //    var obj = await pdf.IndirectObjects.GetAsync(ior);
    //    annotObjs.Add(obj);
    //}

    //var count1 = await pdf.GetPageCountAsync();

    //await pdf.InsertPageAsync(2, new Page.PageCreationOptions { MediaBox = new Rectangle(new(0, 0), new(200, 200)) });
    //await pdf.DeletePageAsync(1);

    //var count2 = await pdf.GetPageCountAsync();

    //await pdf.AppendPageAsync();

    //var count2 = await pdf.GetPageCountAsync();

    //var test = await pdf.GetPageAsync(1);
    //var test2 = await pdf.GetPageAsync(2);

    using var outputFileStream = new FileStream(output, FileMode.Create);

    await pdf.SaveAsync(outputFileStream);

    Console.WriteLine($"Parsed {input} to {output} with ZingPdf");
}

