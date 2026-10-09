using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SkiaSharp;
using ZingPDF;
using ZingPDF.Graphics;
using ZingPDF.OCR;
using ZingPDF.Rendering;
using ZingPDF.Elements;

[assembly: System.Runtime.Versioning.SupportedOSPlatform("linux")]

if (args.Contains("--simulate-stall", StringComparer.Ordinal))
{
    Console.WriteLine("Supervisor stall check started.");
    await Task.Delay(Timeout.InfiniteTimeSpan);
    return;
}

if (args.Contains("--engine-self-check", StringComparer.Ordinal))
{
    TesseractCliEngine.RunSelfCheck();
    Console.WriteLine("OCR orientation and TSV parsing self-check passed.");
    return;
}

var outputDirectory = Path.GetFullPath(args.FirstOrDefault(x => !x.StartsWith("--", StringComparison.Ordinal)) ?? ".tools/document-workflow");
Directory.CreateDirectory(outputDirectory);
var corpusDirectory = Path.Combine(outputDirectory, "corpus");
var previewDirectory = Path.Combine(outputDirectory, "previews");
Directory.CreateDirectory(corpusDirectory);
Directory.CreateDirectory(previewDirectory);

var applicationDeadline = TimeSpan.FromSeconds(55);
const int workerSupervisorDeadlineSeconds = 60;
using var workerDeadline = new CancellationTokenSource(applicationDeadline);
var runStartedUtc = DateTimeOffset.UtcNow;
var fontPath = Path.Combine(AppContext.BaseDirectory, "NotoSans-Regular.ttf");
if (!File.Exists(fontPath)) throw new FileNotFoundException("The bundled Noto Sans font is required to generate the scan corpus.", fontPath);
var cases = await CreateCorpusAsync(corpusDirectory, fontPath, workerDeadline.Token);
if (args.Contains("--generate-only", StringComparer.Ordinal))
{
    foreach (var item in cases)
    {
        await using var source = File.OpenRead(item.PdfPath);
        using var pdf = Pdf.Load(source);
        var pageCount = await pdf.GetPageCountAsync();
        for (var pageNumber = 1; pageNumber <= pageCount; pageNumber++)
        {
            var preview = await pdf.RenderPageAsync(pageNumber, new PdfPageRenderOptions
            {
                Scale = 150d / 72d,
                ApplyPageRotation = true,
                UseVisibleBox = true
            }, workerDeadline.Token);
            await File.WriteAllBytesAsync(Path.Combine(previewDirectory, $"{item.Name}-page-{pageNumber:D2}.png"),
                preview.PngBytes.ToArray(), workerDeadline.Token);
        }
    }
    Console.WriteLine($"Generated {cases.Count} synthetic PDF inputs.");
    return;
}
if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TESSDATA_PREFIX")))
    throw new InvalidOperationException("TESSDATA_PREFIX must point to installed language data.");

var engine = new TesseractCliEngine(Environment.GetEnvironmentVariable("TESSDATA_PREFIX")!, "eng");
var results = new List<CaseResult>();

foreach (var item in cases)
{
    workerDeadline.Token.ThrowIfCancellationRequested();
    var pdfHash = await HashFileAsync(item.PdfPath, workerDeadline.Token);
    await using var input = File.OpenRead(item.PdfPath);
    using var pdf = Pdf.Load(input);
    var pageCount = await pdf.GetPageCountAsync();
    if (pageCount != item.Pages.Count) throw new InvalidDataException($"Unexpected page count for {item.Name}.");

    var pages = new List<PageResult>();
    for (var pageNumber = 1; pageNumber <= pageCount; pageNumber++)
    {
        workerDeadline.Token.ThrowIfCancellationRequested();
        var expected = item.Pages[pageNumber - 1];
        var timer = Stopwatch.StartNew();
        var ocr = await pdf.ExtractTextWithOcrAsync(pageNumber, engine, new PdfOcrOptions
        {
            Mode = expected.MustUseOcr ? PdfOcrMode.RenderedPage : PdfOcrMode.ImageCandidate,
            Dpi = 300,
            MaxPixelCount = 50_000_000,
            PreferEmbeddedText = true
        }, workerDeadline.Token);
        var actualText = ocr.Text;
        var source = ocr.Source.ToString();
        var inputCoverage = ocr.InputCoverage.ToString();
        var usedOcr = ocr.UsedOcr;
        var orientation = usedOcr ? engine.LastOrientation : null;
        var ocrConfidence = usedOcr ? engine.LastConfidence : null;
        var recognizedWordCount = usedOcr ? engine.LastRecognizedWordCount : null;
        var orientationTrials = usedOcr ? engine.LastTrials.ToArray() : [];

        var preview = await pdf.RenderPageAsync(pageNumber, new PdfPageRenderOptions
        {
            Scale = 150d / 72d,
            ApplyPageRotation = true,
            UseVisibleBox = true
        }, workerDeadline.Token);
        var previewName = $"{item.Name}-page-{pageNumber:D2}.png";
        var previewPath = Path.Combine(previewDirectory, previewName);
        await File.WriteAllBytesAsync(previewPath, preview.PngBytes.ToArray(), workerDeadline.Token);
        timer.Stop();

        var normalized = NormalizeForCer(actualText);
        var expectedNormalized = NormalizeForCer(expected.Text);
        var cer = CharacterErrorRate(expectedNormalized, normalized);
        var expectedTokens = expected.Tokens;
        var tokenSearchText = NormalizeForTokenMatch(actualText);
        var missingTokens = expectedTokens.Where(token => !tokenSearchText.Contains(NormalizeForTokenMatch(token), StringComparison.Ordinal)).ToArray();
        pages.Add(new PageResult(pageNumber, expected.Description, source, inputCoverage, usedOcr,
            preview.PixelWidth, preview.PixelHeight, previewName,
            await HashFileAsync(previewPath, workerDeadline.Token), HashText(actualText), timer.ElapsedMilliseconds,
            expected.Text, actualText, cer, expectedTokens, missingTokens, orientation, ocrConfidence, recognizedWordCount, orientationTrials));
        var partialCase = new CaseResult(item.Name, Path.GetFileName(item.PdfPath), pdfHash, pageCount, pages.ToArray());
        await File.WriteAllTextAsync(Path.Combine(outputDirectory, "progress.json"),
            JsonSerializer.Serialize(new { CurrentCase = item.Name, CompletedCases = results, Current = partialCase }, new JsonSerializerOptions { WriteIndented = true }),
            workerDeadline.Token);

        if (missingTokens.Length != 0)
            throw new InvalidDataException($"{item.Name} page {pageNumber} missed {missingTokens.Length} expected OCR token(s).");
        if (cer > expected.MaximumCer)
            throw new InvalidDataException($"{item.Name} page {pageNumber} character error rate {cer:P2} exceeds {expected.MaximumCer:P2}.");
        if (expected.MustUseOcr && !usedOcr)
            throw new InvalidDataException($"{item.Name} page {pageNumber} did not use OCR for its image-only content.");
        if (expected.MustUseEmbeddedText && usedOcr)
            throw new InvalidDataException($"{item.Name} page {pageNumber} unexpectedly used OCR instead of embedded text.");
    }

    results.Add(new CaseResult(item.Name, Path.GetFileName(item.PdfPath), pdfHash, pageCount, pages));
}

var run = new RunManifest(
    "ZingPDF full-page OCR Linux acceptance corpus",
    runStartedUtc,
    Environment.Version.ToString(),
    "tesseract-cli",
    "eng",
    workerSupervisorDeadlineSeconds,
    applicationDeadline.TotalSeconds,
    ["JBIG2-encoded image streams", "CCITT Group 4-encoded image streams", "handwriting", "other OCR languages", "large-scale corpus accuracy"],
    await Task.WhenAll(cases.Select(async x => new InputRecord(Path.GetFileName(x.PdfPath), await HashFileAsync(x.PdfPath, workerDeadline.Token)))),
    results);
await File.WriteAllTextAsync(Path.Combine(outputDirectory, "results.json"),
    JsonSerializer.Serialize(run, new JsonSerializerOptions { WriteIndented = true }), workerDeadline.Token);
var workerEnvironment = await ReadWorkerEnvironmentAsync(workerDeadline.Token);
await File.WriteAllTextAsync(Path.Combine(outputDirectory, "worker-environment.json"),
    JsonSerializer.Serialize(workerEnvironment, new JsonSerializerOptions { WriteIndented = true }), workerDeadline.Token);
File.Delete(Path.Combine(outputDirectory, "progress.json"));
Console.WriteLine($"Verified {results.Sum(x => x.Pages.Count)} PDF pages across {results.Count} synthetic documents.");

static async Task<IReadOnlyList<CorpusCase>> CreateCorpusAsync(string directory, string fontPath, CancellationToken cancellationToken)
{
    var pageSize = (Width: 595d, Height: 842d);
    var combinedPng = Path.Combine(directory, "combined-scan.png");
    SaveScanImage(combinedPng, fontPath, 1190, 970,
        ["SCAN BODY: AMOUNT 184.75 AUD", "CUSTOMER: ALICE EXAMPLE", "REFERENCE: Q7M-29K"], 54);
    var combinedPdf = Path.Combine(directory, "digital-header-scan-body.pdf");
    await new PdfAuthoringBuilder()
        .Page(page => page.Size(pageSize.Width, pageSize.Height)
            .Text(text => text.Value("DIGITAL HEADER: INVOICE ZX-1042").At(42, 785).FontSize(20))
            .Image(image => image.FromFile(combinedPng).At(40, 340).Size(515, 420).PreserveAspectRatio(false)))
        .SaveToFileAsync(combinedPdf);

    var tiledPdf = Path.Combine(directory, "tiled-scan-page.pdf");
    var tileNames = new[] { "TILE ONE: NORTH 5831", "TILE TWO: MIDDLE 2746", "TILE THREE: SOUTH 9105" };
    var builder = new PdfAuthoringBuilder();
    builder.Page(page =>
    {
        page.Size(pageSize.Width, pageSize.Height);
        for (var i = 0; i < tileNames.Length; i++)
        {
            var tilePath = Path.Combine(directory, $"tile-{i + 1}.png");
            SaveScanImage(tilePath, fontPath, 1190, 360, [tileNames[i]], 60);
            page.Image(image => image.FromFile(tilePath).At(40, 555 - i * 245).Size(515, 155).PreserveAspectRatio(false));
        }
    });
    await builder.SaveToFileAsync(tiledPdf);

    var rotatedPng = Path.Combine(directory, "rotated-scan.png");
    SaveScanImage(rotatedPng, fontPath, 1190, 1450,
        ["ROTATED PAGE: EAST 7712", "DATE: 2026-10-09", "STATUS: APPROVED"], 62);
    var rotatedUnmodified = Path.Combine(directory, "rotated-source.pdf");
    await new PdfAuthoringBuilder().Page(page => page.Size(pageSize.Width, pageSize.Height)
        .Image(image => image.FromFile(rotatedPng).At(42, 105).Size(511, 632).PreserveAspectRatio(false)))
        .SaveToFileAsync(rotatedUnmodified);
    var rotatedPdf = Path.Combine(directory, "rotated-scan-page.pdf");
    await using (var source = File.OpenRead(rotatedUnmodified))
    using (var rotated = Pdf.Load(source))
    {
        await rotated.SetRotationAsync(Rotation.Degrees90);
        await using var rotatedOutput = File.Create(rotatedPdf);
        await rotated.SaveAsync(rotatedOutput);
    }
    File.Delete(rotatedUnmodified);

    var cleanPng = Path.Combine(directory, "clean-printed-english.png");
    SaveScanImage(cleanPng, fontPath, 1190, 1600,
        ["CLEAN PRINTED ENGLISH", "The quick brown fox jumps over the lazy dog.", "Invoice total: 245.60 AUD.", "Please retain this page for your records."], 46);
    var cleanPdf = Path.Combine(directory, "clean-printed-english.pdf");
    await new PdfAuthoringBuilder().Page(page => page.Size(pageSize.Width, pageSize.Height)
        .Image(image => image.FromFile(cleanPng).At(42, 45).Size(511, 752).PreserveAspectRatio(false)))
        .SaveToFileAsync(cleanPdf);

    var vectorPdf = Path.Combine(directory, "vector-text.pdf");
    await new PdfAuthoringBuilder().Page(page => page.Size(pageSize.Width, pageSize.Height)
        .Text(text => text.Value("VECTOR TEXT: CONTRACT 6308").At(48, 760).FontSize(22))
        .Text(text => text.Value("This line is selectable PDF text.").At(48, 720).FontSize(16))
        .Text(text => text.Value("TOTAL 319.40 AUD").At(48, 680).FontSize(18)))
        .SaveToFileAsync(vectorPdf);

    cancellationToken.ThrowIfCancellationRequested();
    return
    [
        new("digital-header-scan-body", combinedPdf,
        [
            new("Digital header and raster scan body", "DIGITAL HEADER: INVOICE ZX-1042 SCAN BODY: AMOUNT 184.75 AUD CUSTOMER: ALICE EXAMPLE REFERENCE: Q7M-29K",
                ["ZX-1042", "184.75", "ALICE", "Q7M-29K"], 0.02, true, false)
        ]),
        new("tiled-scan-page", tiledPdf,
        [
            new("Three raster image tiles on one page", "TILE ONE: NORTH 5831 TILE TWO: MIDDLE 2746 TILE THREE: SOUTH 9105",
                ["NORTH", "5831", "MIDDLE", "2746", "SOUTH", "9105"], 0.02, true, false)
        ]),
        new("rotated-scan-page", rotatedPdf,
        [
            new("90 degree PDF page rotation", "ROTATED PAGE: EAST 7712 DATE: 2026-10-09 STATUS: APPROVED",
                ["ROTATED", "EAST", "7712", "APPROVED"], 0.02, true, false)
        ]),
        new("clean-printed-english", cleanPdf,
        [
            new("Clean printed English raster scan", "CLEAN PRINTED ENGLISH THE QUICK BROWN FOX JUMPS OVER THE LAZY DOG. INVOICE TOTAL: 245.60 AUD. PLEASE RETAIN THIS PAGE FOR YOUR RECORDS.",
                ["QUICK", "BROWN", "245.60", "RECORDS"], 0.02, true, false)
        ]),
        new("vector-text", vectorPdf,
        [
            new("Selectable vector text", "VECTOR TEXT: CONTRACT 6308 THIS LINE IS SELECTABLE PDF TEXT. TOTAL 319.40 AUD",
                ["VECTOR", "6308", "SELECTABLE", "319.40"], 0.02, false, true)
        ])
    ];
}

static SKBitmap MakeScanImage(string fontPath, int width, int height, IReadOnlyList<string> lines, float textSize)
{
    var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Opaque);
    using var canvas = new SKCanvas(bitmap);
    canvas.Clear(new SKColor(253, 252, 248));
    using var typeface = SKTypeface.FromFile(fontPath);
    using var font = new SKFont(typeface, textSize) { Subpixel = true };
    using var paint = new SKPaint { Color = new SKColor(24, 27, 30), IsAntialias = true };
    var y = Math.Max(100, height / 2 - (lines.Count * (textSize + 36)) / 2);
    foreach (var line in lines)
    {
        canvas.DrawText(line, 70, y, SKTextAlign.Left, font, paint);
        y += (int)(textSize + 36);
    }
    canvas.Flush();
    return bitmap;
}

static void SaveScanImage(string path, string fontPath, int width, int height, IReadOnlyList<string> lines, float textSize)
{
    using var bitmap = MakeScanImage(fontPath, width, height, lines, textSize);
    using var image = SKImage.FromBitmap(bitmap);
    using var data = image.Encode(SKEncodedImageFormat.Png, 100);
    using var output = File.Create(path);
    data.SaveTo(output);
}

static string NormalizeForCer(string value)
{
    var normalized = value.Normalize(NormalizationForm.FormKC).ToUpperInvariant();
    return System.Text.RegularExpressions.Regex.Replace(normalized, @"\s+", " ").Trim();
}

static string NormalizeForTokenMatch(string value) => string.Concat(value.ToUpperInvariant().Where(char.IsLetterOrDigit));

static string HashText(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

static double CharacterErrorRate(string expected, string actual)
{
    if (expected.Length == 0) return actual.Length == 0 ? 0d : 1d;
    var previous = Enumerable.Range(0, actual.Length + 1).ToArray();
    for (var i = 1; i <= expected.Length; i++)
    {
        var current = new int[actual.Length + 1];
        current[0] = i;
        for (var j = 1; j <= actual.Length; j++)
            current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + (expected[i - 1] == actual[j - 1] ? 0 : 1));
        previous = current;
    }
    return (double)previous[actual.Length] / expected.Length;
}

static async Task<string> HashFileAsync(string path, CancellationToken cancellationToken)
{
    await using var stream = File.OpenRead(path);
    var hash = await SHA256.HashDataAsync(stream, cancellationToken);
    return Convert.ToHexString(hash).ToLowerInvariant();
}

static async Task<WorkerEnvironment> ReadWorkerEnvironmentAsync(CancellationToken cancellationToken)
{
    static string ReadLimit(string name) => File.Exists($"/sys/fs/cgroup/{name}") ? File.ReadAllText($"/sys/fs/cgroup/{name}").Trim() : "unavailable";
    var tessdataPath = Environment.GetEnvironmentVariable("TESSDATA_PREFIX") ?? throw new InvalidOperationException("TESSDATA_PREFIX is required.");
    var languageHashes = await Task.WhenAll(new[] { "eng", "osd" }.Select(async language =>
        new NamedHash(language, await HashFileAsync(Path.Combine(tessdataPath, $"{language}.traineddata"), cancellationToken))));
    var pdfiumPath = Path.Combine(AppContext.BaseDirectory, "runtimes", "linux-x64", "native", "libpdfium.so");
    var pdfiumHash = await HashFileAsync(pdfiumPath, cancellationToken);
    using var dependencies = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "VerifyDocumentWorkflow.deps.json"), cancellationToken));
    var pdfiumPackage = dependencies.RootElement.GetProperty("libraries").EnumerateObject()
        .Select(package => package.Name)
        .FirstOrDefault(name => name.StartsWith("bblanchon.PDFium.Linux/", StringComparison.Ordinal))
        ?? throw new InvalidDataException("The published PDFium package version is missing.");

    return new WorkerEnvironment(Environment.UserName, Environment.ProcessId, Environment.ProcessorCount,
        ReadLimit("cpu.max"), ReadLimit("memory.max"), ReadLimit("pids.max"), OperatingSystem.IsLinux(),
        Directory.Exists(Path.GetTempPath()) ? Directory.GetFiles(Path.GetTempPath(), "zingpdf-ocr-*").Length : -1,
        pdfiumPackage, pdfiumHash, languageHashes);
}

internal sealed class TesseractCliEngine(string tessdataPath, string language) : IOcrEngine
{
    private static readonly int[] TrialRotations = [0, 90, 180, 270];
    private const int MinimumRecognizedWords = 3;
    private const int MinimumRecognizedLetters = 12;

    public int? LastOrientation { get; private set; }
    public double? LastConfidence { get; private set; }
    public int? LastRecognizedWordCount { get; private set; }
    public IReadOnlyList<OrientationTrialResult> LastTrials { get; private set; } = [];

    public async Task<string> RecognizeAsync(OcrInputImage image, CancellationToken cancellationToken = default)
    {
        LastOrientation = null;
        LastConfidence = null;
        LastRecognizedWordCount = null;
        LastTrials = [];
        using var sourceBitmap = SKBitmap.Decode(image.Data)
            ?? throw new InvalidDataException("Tesseract input was not a decodable image.");
        var candidates = new List<OcrCandidate>(TrialRotations.Length);
        foreach (var rotation in TrialRotations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var trialBitmap = RotateBitmap(sourceBitmap, rotation);
            using var trialImage = SKImage.FromBitmap(trialBitmap);
            using var png = trialImage.Encode(SKEncodedImageFormat.Png, 100)
                ?? throw new InvalidDataException("Could not encode a rotated OCR trial image.");
            var candidate = await RecognizeTsvAsync(png.ToArray(), cancellationToken);
            candidates.Add(candidate with { Rotation = rotation });
        }

        LastTrials = candidates.Select(candidate => new OrientationTrialResult(candidate.Rotation,
            candidate.LengthWeightedConfidence, candidate.WordCount, candidate.Text.Length, candidate.LetterCount)).ToArray();
        var best = SelectCandidate(candidates);
        if (best is null)
            throw new InvalidDataException("Tesseract orientation trials found too few recognized words.");

        LastOrientation = best.Rotation;
        LastConfidence = best.LengthWeightedConfidence;
        LastRecognizedWordCount = best.WordCount;
        return best.Text;
    }

    private async Task<OcrCandidate> RecognizeTsvAsync(byte[] pngBytes, CancellationToken cancellationToken)
    {
        var inputPath = Path.Combine(Path.GetTempPath(), $"zingpdf-ocr-{Guid.NewGuid():N}.png");
        try
        {
            await File.WriteAllBytesAsync(inputPath, pngBytes, cancellationToken);
            var start = new ProcessStartInfo("/usr/bin/tesseract")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            start.ArgumentList.Add(inputPath);
            start.ArgumentList.Add("stdout");
            start.ArgumentList.Add("--tessdata-dir");
            start.ArgumentList.Add(tessdataPath);
            start.ArgumentList.Add("-l");
            start.ArgumentList.Add(language);
            start.ArgumentList.Add("--psm");
            start.ArgumentList.Add("3");
            start.ArgumentList.Add("tsv");

            using var process = new Process { StartInfo = start, EnableRaisingEvents = true };
            if (!process.Start()) throw new InvalidOperationException("Could not start the installed Tesseract executable.");
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(20));
            var stdoutTask = process.StandardOutput.ReadToEndAsync(timeout.Token);
            var stderrTask = process.StandardError.ReadToEndAsync(timeout.Token);
            try
            {
                await process.WaitForExitAsync(timeout.Token);
                var tsv = await stdoutTask;
                _ = await stderrTask;
                if (process.ExitCode != 0) throw new InvalidOperationException($"Tesseract exited with code {process.ExitCode}.");
                return ParseTsv(tsv);
            }
            catch (OperationCanceledException)
            {
                try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
                catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException) { }
                try { await process.WaitForExitAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(2)); } catch (TimeoutException) { }
                await ObserveCompletionAsync(stdoutTask);
                await ObserveCompletionAsync(stderrTask);
                throw;
            }
        }
        finally
        {
            try { File.Delete(inputPath); } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        }
    }

    private static SKBitmap RotateBitmap(SKBitmap source, int clockwiseDegrees)
    {
        var swapDimensions = clockwiseDegrees is 90 or 270;
        var width = swapDimensions ? source.Height : source.Width;
        var height = swapDimensions ? source.Width : source.Height;
        var rotated = new SKBitmap(width, height, source.ColorType, source.AlphaType);
        using var canvas = new SKCanvas(rotated);
        canvas.Clear(SKColors.White);
        canvas.Translate(width / 2f, height / 2f);
        canvas.RotateDegrees(clockwiseDegrees);
        canvas.DrawBitmap(source, -source.Width / 2f, -source.Height / 2f);
        canvas.Flush();
        return rotated;
    }

    private static OcrCandidate ParseTsv(string tsv)
    {
        var words = new List<(int Page, int Block, int Paragraph, int Line, int Order, string Text, double Confidence)>();
        foreach (var line in tsv.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.StartsWith("level\t", StringComparison.Ordinal)) continue;
            var fields = line.TrimEnd('\r').Split('\t', 12, StringSplitOptions.None);
            if (fields.Length < 12 || fields[0] != "5" || string.IsNullOrWhiteSpace(fields[11])) continue;
            if (!double.TryParse(fields[10], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var wordConfidence)
                || !double.IsFinite(wordConfidence) || wordConfidence is < 0d or > 100d)
                continue;
            if (!int.TryParse(fields[1], out var page) || !int.TryParse(fields[2], out var block)
                || !int.TryParse(fields[3], out var paragraph) || !int.TryParse(fields[4], out var lineNumber)
                || !int.TryParse(fields[5], out var wordOrder))
                continue;
            words.Add((page, block, paragraph, lineNumber, wordOrder, fields[11].Trim(), wordConfidence));
        }

        var ordered = words.OrderBy(x => x.Page).ThenBy(x => x.Block).ThenBy(x => x.Paragraph)
            .ThenBy(x => x.Line).ThenBy(x => x.Order).ToArray();
        var textLines = ordered.GroupBy(x => (x.Page, x.Block, x.Paragraph, x.Line))
            .Select(group => string.Join(' ', group.Select(word => word.Text)));
        var totalLength = ordered.Sum(word => word.Text.Length);
        var confidence = totalLength == 0 ? 0d : ordered.Sum(word => word.Confidence * word.Text.Length) / totalLength;
        if (!double.IsFinite(confidence)) confidence = 0d;
        var text = string.Join(Environment.NewLine, textLines);
        return new OcrCandidate(0, text, confidence, ordered.Length, text.Count(char.IsLetter));
    }

    private static OcrCandidate? SelectCandidate(IEnumerable<OcrCandidate> candidates) => candidates
        .Where(candidate => candidate.WordCount >= MinimumRecognizedWords
            && candidate.LetterCount >= MinimumRecognizedLetters
            && double.IsFinite(candidate.LengthWeightedConfidence)
            && candidate.LengthWeightedConfidence is >= 0d and <= 100d)
        .OrderByDescending(candidate => candidate.LengthWeightedConfidence)
        .ThenBy(candidate => candidate.Rotation)
        .FirstOrDefault();

    internal static void RunSelfCheck()
    {
        const string sample = "level\tpage_num\tblock_num\tpar_num\tline_num\tword_num\tleft\ttop\twidth\theight\tconf\ttext\n"
            + "5\t1\t1\t1\t2\t2\t20\t20\t10\t10\t90\tWORLD\n"
            + "5\t1\t1\t1\t1\t2\t20\t10\t10\t10\t80\tTWO\n"
            + "5\t1\t1\t1\t1\t1\t5\t10\t10\t10\t100\tONE\n"
            + "5\t1\t1\t1\t2\t1\t5\t20\t10\t10\t60\tHELLO\n";
        var parsed = ParseTsv(sample);
        if (parsed.Text != $"ONE TWO{Environment.NewLine}HELLO WORLD" || parsed.WordCount != 4
            || Math.Abs(parsed.LengthWeightedConfidence - 80.625d) > 0.001)
            throw new InvalidOperationException("TSV parser did not preserve line order or length-weighted confidence.");

        var candidates = new[]
        {
            new OcrCandidate(0, "selected tie break", 92, 4, 17),
            new OcrCandidate(90, "same confidence", 92, 4, 17),
            new OcrCandidate(180, "too few", 99, 2, 20),
            new OcrCandidate(270, "too few letters", 98, 8, 11),
            new OcrCandidate(270, "lower confidence", 88, 8, 18)
        };
        var selected = SelectCandidate(candidates);
        if (selected?.Rotation != 0)
            throw new InvalidOperationException("Orientation selector did not apply minimum-content and deterministic tie-break rules.");

        using var source = new SKBitmap(13, 7);
        source.Erase(SKColors.White);
        using (var sourceCanvas = new SKCanvas(source))
        {
            using var red = new SKPaint { Color = SKColors.Red };
            using var blue = new SKPaint { Color = SKColors.Blue };
            sourceCanvas.DrawRect(0, 0, 3, 2, red);
            sourceCanvas.DrawRect(10, 5, 3, 2, blue);
        }
        using var rotated = RotateBitmap(source, 90);
        if (rotated.Width != 7 || rotated.Height != 13
            || rotated.GetPixel(5, 0) != SKColors.Red || rotated.GetPixel(0, 10) != SKColors.Blue)
            throw new InvalidOperationException("Quarter-turn image rotation did not preserve clockwise orientation and dimensions.");
    }

    private static async Task ObserveCompletionAsync(Task<string> outputTask)
    {
        try { _ = await outputTask; } catch (Exception) { }
    }
}

internal sealed record OcrCandidate(int Rotation, string Text, double LengthWeightedConfidence, int WordCount, int LetterCount);
internal sealed record OrientationTrialResult(int RotationDegrees, double LengthWeightedWordConfidence, int RecognizedWordCount,
    int TextCharacterCount, int RecognizedLetterCount);

internal sealed record ExpectedPage(string Description, string Text, string[] Tokens, double MaximumCer, bool MustUseOcr, bool MustUseEmbeddedText);
internal sealed record CorpusCase(string Name, string PdfPath, IReadOnlyList<ExpectedPage> Pages);
internal sealed record InputRecord(string File, string Sha256);
internal sealed record PageResult(int Page, string Description, string Source, string InputCoverage, bool UsedOcr, int PreviewWidth, int PreviewHeight,
    string PreviewFile, string PreviewSha256, string OcrTextSha256, long ElapsedMilliseconds, string ExpectedText, string OcrText, double CharacterErrorRate,
    string[] ExpectedTokens, string[] MissingTokens, int? SelectedOcrRotationDegrees, double? LengthWeightedWordConfidence,
    int? RecognizedWordCount, IReadOnlyList<OrientationTrialResult> OrientationTrials);
internal sealed record CaseResult(string Name, string PdfFile, string PdfSha256, int PageCount, IReadOnlyList<PageResult> Pages);
internal sealed record NamedHash(string Name, string Sha256);
internal sealed record RunManifest(string Name, DateTimeOffset StartedUtc, string DotnetRuntime, string OcrEngine, string Language,
    int WorkerDeadlineSeconds, double ApplicationCancellationSeconds, string[] NotCovered, IReadOnlyList<InputRecord> Inputs, IReadOnlyList<CaseResult> Results);
internal sealed record WorkerEnvironment(string User, int ProcessId, int ProcessorCount, string CpuMax, string MemoryMax, string PidsMax,
    bool IsLinux, int OcrTempFilesAfterRun, string PdfiumPackage, string PdfiumSha256, IReadOnlyList<NamedHash> TessdataHashes);
