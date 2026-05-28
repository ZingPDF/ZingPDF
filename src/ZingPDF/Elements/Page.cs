using PDFtoImage;
using SkiaSharp;
using System.Runtime.Versioning;
using ZingPDF.Elements.Drawing;
using ZingPDF.Fonts;
using ZingPDF.Fonts.FontProviders;
using ZingPDF.Graphics;
using ZingPDF.Graphics.Images;
using ZingPDF.Extensions;
using ZingPDF.Syntax;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.DocumentStructure.PageTree;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;
using ZingPDF.Syntax.Objects.Strings;
using ZingPDF.Text;
using ZingPDF.Text.SimpleFonts;
using ZingPDF.Rendering;
using System.Text.RegularExpressions;

namespace ZingPDF.Elements
{
    /// <summary>
    /// Represents a single page in a PDF document.
    /// </summary>
    public class Page
    {
        private const double MultilineLineHeightMultiplier = 1.2d;
        private static readonly Regex LineTokenRegex = new(@"\S+\s*", RegexOptions.Compiled);

        private readonly IPdf _pdf;
        private readonly int? _pageNumber;

        internal Page(IndirectObject pageObject, IPdf pdf, int? pageNumber = null)
        {
            ArgumentNullException.ThrowIfNull(pageObject, nameof(pageObject));
            ArgumentNullException.ThrowIfNull(pdf);

            IndirectObject = pageObject;
            _pdf = pdf;
            _pageNumber = pageNumber;
        }

        /// <summary>
        /// Gets the underlying indirect object for the page.
        /// </summary>
        public IndirectObject IndirectObject { get; }

        /// <summary>
        /// Gets the page dictionary for the page.
        /// </summary>
        public PageDictionary Dictionary => (PageDictionary)IndirectObject.Object;

        /// <summary>
        /// Gets the visible page geometry and coordinate mapping used for display.
        /// </summary>
        /// <remarks>
        /// Page coordinates use the PDF bottom-left origin. Display coordinates use a
        /// top-left origin after crop and clockwise page rotation are applied.
        /// </remarks>
        public async Task<PdfPageGeometry> GetGeometryAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var mediaBox = NormalizeBox(await Dictionary.MediaBox.GetAsync()
                ?? throw new InvalidPdfException("Unable to resolve the page MediaBox."));
            cancellationToken.ThrowIfCancellationRequested();

            var cropBox = NormalizeBox(await Dictionary.CropBox.GetAsync() ?? mediaBox);
            var visibleBox = Intersect(mediaBox, cropBox);
            cancellationToken.ThrowIfCancellationRequested();

            var rawRotation = await Dictionary.Rotate.GetAsync();
            var rotationDegrees = NormalizeRotation(rawRotation?.Value ?? 0);
            var pageNumber = await GetPageNumberAsync(cancellationToken);
            var swapsDimensions = rotationDegrees is 90 or 270;

            return new PdfPageGeometry
            {
                PageNumber = pageNumber,
                MediaBox = mediaBox,
                CropBox = cropBox,
                VisibleBox = visibleBox,
                RotationDegrees = rotationDegrees,
                DisplayWidth = swapsDimensions ? visibleBox.Height : visibleBox.Width,
                DisplayHeight = swapsDimensions ? visibleBox.Width : visibleBox.Height
            };
        }

        /// <summary>
        /// Renders this page to PNG bytes.
        /// </summary>
        /// <remarks>
        /// Rendering includes the current in-memory page edits by staging an incremental PDF snapshot before
        /// rasterization. The returned geometry uses PDF page units; image dimensions are in pixels.
        /// </remarks>
        [SupportedOSPlatform("android31.0")]
        [SupportedOSPlatform("ios13.6")]
        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("maccatalyst13.5")]
        [SupportedOSPlatform("macos")]
        [SupportedOSPlatform("windows")]
        public async Task<PdfPageRenderResult> RenderAsync(
            PdfPageRenderOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            options ??= new PdfPageRenderOptions();
            options.Validate();
            cancellationToken.ThrowIfCancellationRequested();

            var geometry = await GetGeometryAsync(cancellationToken);
            var sourceBox = options.UseVisibleBox ? geometry.VisibleBox : geometry.MediaBox;
            var swapsDimensions = options.ApplyPageRotation && geometry.RotationDegrees is 90 or 270;
            var renderWidth = swapsDimensions ? (double)sourceBox.Height : (double)sourceBox.Width;
            var renderHeight = swapsDimensions ? (double)sourceBox.Width : (double)sourceBox.Height;

            var pixelWidth = ScaleToPixelDimension(renderWidth, options.Scale, nameof(geometry.DisplayWidth));
            var pixelHeight = ScaleToPixelDimension(renderHeight, options.Scale, nameof(geometry.DisplayHeight));
            var pdfBytes = await CreateRenderSnapshotAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var renderOptions = new RenderOptions(
                Dpi: 72,
                Width: pixelWidth,
                Height: pixelHeight,
                WithAnnotations: true,
                WithFormFill: true,
                WithAspectRatio: false,
                Rotation: options.ApplyPageRotation ? PdfRotation.Rotate0 : InverseRotation(geometry.RotationDegrees),
                AntiAliasing: PdfAntiAliasing.All,
                BackgroundColor: ToSkColor(options.Background),
                Bounds: null,
                UseTiling: true,
                DpiRelativeToBounds: false,
                Grayscale: false);

            using var pngStream = new MemoryStream();
            try
            {
                Conversion.SavePng(
                    pngStream,
                    pdfBytes,
                    new Index(geometry.PageNumber - 1),
                    password: null,
                    options: renderOptions);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                throw new PdfRenderException($"Unable to render page {geometry.PageNumber} to PNG.", exception);
            }

            cancellationToken.ThrowIfCancellationRequested();

            return new PdfPageRenderResult
            {
                PageNumber = geometry.PageNumber,
                PixelWidth = pixelWidth,
                PixelHeight = pixelHeight,
                Scale = options.Scale,
                Geometry = geometry,
                PngBytes = pngStream.ToArray()
            };
        }

        /// <summary>
        /// Adds a text object to the page contents.
        /// </summary>
        public async Task AddTextAsync(TextObject text)
        {
            ArgumentNullException.ThrowIfNull(text);

            foreach (var font in text.ReferencedFonts)
            {
                await EnsureFontResourceAsync(font);
            }

            await AddContentStreamAsync(text);
        }

        /// <summary>
        /// Adds text to the page using the provided bounds and font settings.
        /// </summary>
        public async Task AddTextAsync(string text, Rectangle boundingBox, FontOptions fontOptions, TextLayoutOptions? layoutOptions = null)
        {
            ArgumentNullException.ThrowIfNull(text);
            ArgumentNullException.ThrowIfNull(boundingBox);
            ArgumentNullException.ThrowIfNull(fontOptions);

            var layout = await ResolveTextLayoutAsync(text, boundingBox, fontOptions, layoutOptions ?? new TextLayoutOptions());

            foreach (var segment in layout.Segments)
            {
                await AddTextAsync(new TextObject(segment.Text, segment.TextOrigin, layout.FontOptions, layout.ClipBounds));
            }
        }

        /// <summary>
        /// Adds text to the page using a registered font.
        /// </summary>
        public async Task AddTextAsync(string text, Rectangle boundingBox, PdfFont font, Number size, Graphics.RGBColour colour, TextLayoutOptions? layoutOptions = null)
        {
            ArgumentNullException.ThrowIfNull(text);
            ArgumentNullException.ThrowIfNull(boundingBox);
            ArgumentNullException.ThrowIfNull(font);
            ArgumentNullException.ThrowIfNull(colour);

            await EnsureFontResourceAsync(font);
            await AddTextAsync(text, boundingBox, font.CreateOptions(size, colour), layoutOptions);
        }

        /// <summary>
        /// Adds a simple text watermark to the page.
        /// </summary>
        public async Task AddWatermarkAsync(string text)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(text);

            var watermarkFont = new Type1FontDictionary(_pdf, ObjectContext.UserCreated);
            watermarkFont.Set(Constants.DictionaryKeys.Font.BaseFont, (Name)StandardPdfFonts.Helvetica);
            watermarkFont.Set(Constants.DictionaryKeys.Font.Encoding, (Name)Text.Encoding.PDFEncoding.WinAnsi);

            var fontObject = await _pdf.Objects.AddAsync(watermarkFont);
            await AddWatermarkAsync(text, fontObject.Reference, (Name)UniqueStringGenerator.Generate());
        }

        /// <summary>
        /// Adds an image to the page contents.
        /// </summary>
        public async Task AddImageAsync(Image image)
        {
            ArgumentNullException.ThrowIfNull(image);

            var preparedImage = await ImageXObjectBuilder.CreateAsync(image.ImageData);
            var imageDictionary = CreateImageDictionary(preparedImage);

            if (preparedImage.SoftMask is not null)
            {
                var softMaskDictionary = CreateImageDictionary(preparedImage.SoftMask);
                var softMaskObject = new StreamObject<ImageDictionary>(
                    preparedImage.SoftMask.Data,
                    softMaskDictionary,
                    ObjectContext.UserCreated);
                var softMaskIndirectObject = await _pdf.Objects.AddAsync(softMaskObject);

                imageDictionary.Set(Constants.DictionaryKeys.Image.SMask, softMaskIndirectObject.Reference);
            }

            var imageXObject = new StreamObject<ImageDictionary>(
                preparedImage.Data,
                imageDictionary,
                ObjectContext.UserCreated);

            var imageXObjectIndirectObject = await _pdf.Objects.AddAsync(imageXObject);

            var resourceName = UniqueStringGenerator.Generate();

            await Dictionary.AddXObjectResourceAsync(resourceName, imageXObjectIndirectObject.Reference, _pdf);

            var imageRect = image.MaxBounds;
            if (image.PreserveAspectRatio)
            {
                var (newWidth, newHeight) = ScaleToFit(preparedImage.Width, preparedImage.Height, image.MaxBounds.Width, image.MaxBounds.Height);

                imageRect = Rectangle.FromCoordinates(
                    image.MaxBounds.LowerLeft,
                    new Coordinate(
                        image.MaxBounds.LowerLeft.X + newWidth,
                        image.MaxBounds.LowerLeft.Y + newHeight
                    )
                );
            }

            await AddContentStreamAsync(new ImageXObjectContentStream(resourceName, imageRect, ObjectContext.UserCreated));
        }

        /// <summary>
        /// Adds an image from a stream to the page contents.
        /// </summary>
        public Task AddImageAsync(Stream imageData, Rectangle maxBounds, bool preserveAspectRatio = true)
        {
            ArgumentNullException.ThrowIfNull(imageData);
            ArgumentNullException.ThrowIfNull(maxBounds);

            return AddImageAsync(new Image(imageData, maxBounds, preserveAspectRatio));
        }

        /// <summary>
        /// Adds an image from a file to the page contents.
        /// </summary>
        public async Task AddImageAsync(string imagePath, Rectangle maxBounds, bool preserveAspectRatio = true)
        {
            using var image = Image.FromFile(imagePath, maxBounds, preserveAspectRatio);
            await AddImageAsync(image);
        }

        /// <summary>
        /// Adds a drawable path to the page contents.
        /// </summary>
        public Task AddPathAsync(ZingPDF.Elements.Drawing.Path path)
        {
            ArgumentNullException.ThrowIfNull(path);

            return AddContentStreamAsync(new PathContentStream(path, ObjectContext.UserCreated));
        }

        /// <summary>
        /// Applies an additional rotation to the page.
        /// </summary>
        public async Task RotateAsync(Rotation rotation)
        {
            // TODO: Ensure contents don't need some sort of transform to match

            ArgumentNullException.ThrowIfNull(rotation);

            Rotation existingRotation = await Dictionary.Rotate.GetAsync() ?? Rotation.None;

            // The page may already be rotated, or inherit a value for rotation.
            // In practice, it is likely desired to rotate by a further n degrees.
            Dictionary.SetRotation(existingRotation + rotation);

            _pdf.Objects.Update(IndirectObject);
        }

        private async Task<int> GetPageNumberAsync(CancellationToken cancellationToken)
        {
            if (_pageNumber is int pageNumber)
            {
                return pageNumber;
            }

            var pageObjects = await _pdf.GetAllPagesAsync();
            for (var index = 0; index < pageObjects.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (pageObjects[index].Id == IndirectObject.Id)
                {
                    return index + 1;
                }
            }

            throw new InvalidOperationException("Unable to determine the page number for geometry retrieval.");
        }

        private static Rectangle NormalizeBox(Rectangle box)
            => Rectangle.FromCoordinates(
                new Coordinate(
                    Math.Min(box.LowerLeft.X, box.UpperRight.X),
                    Math.Min(box.LowerLeft.Y, box.UpperRight.Y)),
                new Coordinate(
                    Math.Max(box.LowerLeft.X, box.UpperRight.X),
                    Math.Max(box.LowerLeft.Y, box.UpperRight.Y)),
                box.Context);

        private static Rectangle Intersect(Rectangle first, Rectangle second)
        {
            var left = Math.Max(first.LowerLeft.X, second.LowerLeft.X);
            var bottom = Math.Max(first.LowerLeft.Y, second.LowerLeft.Y);
            var right = Math.Min(first.UpperRight.X, second.UpperRight.X);
            var top = Math.Min(first.UpperRight.Y, second.UpperRight.Y);

            return Rectangle.FromCoordinates(
                new Coordinate(left, bottom),
                new Coordinate(Math.Max(left, right), Math.Max(bottom, top)),
                first.Context);
        }

        private static int NormalizeRotation(double rawRotation)
        {
            if (!double.IsFinite(rawRotation) || rawRotation % 90 != 0)
            {
                throw new InvalidPdfException("Page Rotate must be a finite multiple of 90 degrees.");
            }

            var rotation = (int)(rawRotation % 360);
            return rotation < 0 ? rotation + 360 : rotation;
        }

        private async Task<byte[]> CreateRenderSnapshotAsync(CancellationToken cancellationToken)
        {
            var originalPosition = _pdf.Data.CanSeek ? _pdf.Data.Position : 0;

            try
            {
                using var snapshot = new MemoryStream();
                if (_pdf.Data.CanSeek)
                {
                    _pdf.Data.Position = 0;
                }

                await _pdf.Data.CopyToAsync(snapshot, cancellationToken);

                if (await _pdf.Objects.GenerateUpdateDeltaAsync() is { } incrementalUpdate)
                {
                    await incrementalUpdate.WriteAsync(snapshot);
                }

                return snapshot.ToArray();
            }
            finally
            {
                if (_pdf.Data.CanSeek)
                {
                    _pdf.Data.Position = originalPosition;
                }
            }
        }

        private static int ScaleToPixelDimension(double pageUnits, double scale, string dimensionName)
        {
            if (!double.IsFinite(pageUnits) || pageUnits <= 0)
            {
                throw new PdfRenderException($"{dimensionName} must be greater than zero to render a page.");
            }

            var scaled = Math.Round(pageUnits * scale, MidpointRounding.AwayFromZero);
            if (scaled is < 1 or > int.MaxValue)
            {
                throw new PdfRenderException($"{dimensionName} and scale produce an unsupported pixel dimension.");
            }

            return (int)scaled;
        }

        private static PdfRotation InverseRotation(int rotationDegrees)
            => rotationDegrees switch
            {
                0 => PdfRotation.Rotate0,
                90 => PdfRotation.Rotate270,
                180 => PdfRotation.Rotate180,
                270 => PdfRotation.Rotate90,
                _ => throw new InvalidOperationException("Page rotation must be normalised before rendering.")
            };

        private static SKColor ToSkColor(RGBColour colour)
            => new(
                ToByte(colour.Red),
                ToByte(colour.Green),
                ToByte(colour.Blue),
                255);

        private static byte ToByte(Number value)
            => (byte)Math.Clamp(Math.Round((double)value * 255d, MidpointRounding.AwayFromZero), byte.MinValue, byte.MaxValue);

        // TODO: move to testable class (ICalulations maybe)
        private static (int newWidth, int newHeight) ScaleToFit(int originalWidth, int originalHeight, int maxWidth, int maxHeight)
        {
            // Calculate aspect ratios
            float aspectRatioOriginal = originalWidth / (float)originalHeight;
            float aspectRatioMax = maxWidth / maxHeight;

            // Determine scaling factor based on which dimension is more restrictive
            float newWidth, newHeight;
            if (aspectRatioOriginal > aspectRatioMax)
            {
                // Scale based on maxWidth
                newWidth = maxWidth;
                newHeight = maxWidth / aspectRatioOriginal;
            }
            else
            {
                // Scale based on maxHeight
                newWidth = maxHeight * aspectRatioOriginal;
                newHeight = maxHeight;
            }

            return ((int)newWidth, (int)newHeight);
        }

        private async Task AddContentStreamAsync(ContentStream contentStream)
        {
            var contentStreamObject = await new ContentStreamFactory([contentStream])
                .CreateAsync(new StreamDictionary(_pdf, ObjectContext.UserCreated), ObjectContext.UserCreated);

            var contentStreamIndirectObject = await _pdf.Objects.AddAsync(contentStreamObject);

            await Dictionary.AddContentAsync(contentStreamIndirectObject.Reference);

            _pdf.Objects.Update(IndirectObject);
        }

        internal async Task AddWatermarkAsync(string text, IndirectObjectReference fontReference, Name fontResourceName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(text);
            ArgumentNullException.ThrowIfNull(fontReference);
            ArgumentNullException.ThrowIfNull(fontResourceName);

            var mediaBox = await Dictionary.MediaBox.GetAsync();
            var resources = ResourceDictionary.FromDictionary(await Dictionary.Resources.GetAsync());
            await resources.AddFontAsync(fontResourceName, fontReference, _pdf);
            Dictionary.SetResources(resources);

            var pageWidth = mediaBox.UpperRight.X - mediaBox.LowerLeft.X;
            var pageHeight = mediaBox.UpperRight.Y - mediaBox.LowerLeft.Y;
            var fontSize = 42;
            var x = mediaBox.LowerLeft.X + (pageWidth * 0.2);
            var y = mediaBox.LowerLeft.Y + (pageHeight * 0.5);

            var watermarkContent = new ContentStream()
                .SaveGraphicsState()
                .SetColour(new RGBColour(0.8, 0.8, 0.8))
                .BeginTextObject()
                .SetTextState(fontResourceName, fontSize)
                .SetTextMatrix(
                    1,
                    0,
                    0,
                    1,
                    x,
                    y)
                .ShowText(PdfString.FromTextAuto(text, ObjectContext.UserCreated))
                .EndTextObject()
                .RestoreGraphicsState();

            await AddContentStreamAsync(watermarkContent);
        }

        private ImageDictionary CreateImageDictionary(PreparedImageXObject preparedImage)
        {
            var filter = preparedImage.FilterName;
            var dictionary = new ImageDictionary(
                _pdf,
                ObjectContext.UserCreated,
                preparedImage.Width,
                preparedImage.Height,
                preparedImage.ColorSpace,
                preparedImage.BitsPerComponent,
                filters: filter != null ? [(Name)filter] : null,
                decodeParms: null);

            dictionary.Set<Number>(Constants.DictionaryKeys.Stream.Length, preparedImage.Data.Length);

            return dictionary;
        }

        private async Task EnsureFontResourceAsync(PdfFont font)
        {
            var resources = ResourceDictionary.FromDictionary(await Dictionary.Resources.GetAsync());
            await resources.AddFontAsync(font.ResourceName, font.FontReference, _pdf);
            Dictionary.SetResources(resources);
        }

        private async Task<ResolvedTextLayout> ResolveTextLayoutAsync(
            string text,
            Rectangle boundingBox,
            FontOptions fontOptions,
            TextLayoutOptions layoutOptions)
        {
            var contentBounds = ApplyPadding(boundingBox, layoutOptions.Padding);
            var metrics = await ResolveFontMetricsAsync(fontOptions.ResourceName);
            var readingDirection = ResolveReadingDirection(text, layoutOptions.ReadingDirection);

            var fontSize = (double)fontOptions.Size;
            if (layoutOptions.Overflow == TextOverflowMode.ShrinkToFit)
            {
                fontSize = ShrinkToFit(text, metrics, fontSize, contentBounds, layoutOptions.MinFontSize);
            }

            var contentLeft = (double)contentBounds.LowerLeft.X;
            var contentRight = (double)contentBounds.UpperRight.X;
            var contentBottom = (double)contentBounds.LowerLeft.Y;
            var contentTop = (double)contentBounds.UpperRight.Y;
            var availableWidth = Math.Max(0, contentRight - contentLeft);
            var availableHeight = Math.Max(0, contentTop - contentBottom);

            var wrapText = layoutOptions.Wrap || text.Contains('\n') || text.Contains('\r');

            if (wrapText)
            {
                var wrappedLayout = ResolveWrappedTextLayout(
                    text,
                    layoutOptions,
                    fontOptions,
                    metrics,
                    contentLeft,
                    contentRight,
                    contentBottom,
                    contentTop,
                    availableWidth,
                    availableHeight,
                    fontSize);

                Rectangle? wrappedClipBounds = layoutOptions.Overflow == TextOverflowMode.Clip && availableWidth > 0 && availableHeight > 0
                    ? contentBounds
                    : null;

                return new ResolvedTextLayout(
                    fontOptions with { Size = (Number)wrappedLayout.FontSize },
                    wrappedLayout.Segments,
                    wrappedClipBounds);
            }

            var textWidth = MeasureTextWidth(text, metrics, fontSize);
            var ascent = ScaleMetric(metrics?.Ascent ?? 800, fontSize);
            var descent = ScaleMetric(metrics?.Descent ?? -200, fontSize);
            var textHeight = ascent + descent;

            var originX = CalculateHorizontalOrigin(
                layoutOptions.HorizontalAlignment,
                readingDirection,
                contentLeft,
                contentRight,
                textWidth);

            var originY = CalculateVerticalOrigin(
                layoutOptions.VerticalAlignment,
                contentBottom,
                contentTop,
                textHeight,
                ascent,
                descent);

            var resolvedFontOptions = fontOptions with { Size = (Number)fontSize };
            Rectangle? clipBounds = layoutOptions.Overflow == TextOverflowMode.Clip && availableWidth > 0 && availableHeight > 0
                ? contentBounds
                : null;

            return new ResolvedTextLayout(
                resolvedFontOptions,
                [new ResolvedTextSegment(text, new Coordinate(originX, originY))],
                clipBounds);
        }

        private static WrappedTextLayout ResolveWrappedTextLayout(
            string text,
            TextLayoutOptions layoutOptions,
            FontOptions fontOptions,
            FontMetrics? metrics,
            double contentLeft,
            double contentRight,
            double contentBottom,
            double contentTop,
            double availableWidth,
            double availableHeight,
            double requestedFontSize)
        {
            var fontSize = requestedFontSize;
            var minimum = Math.Max(0.1d, layoutOptions.MinFontSize);
            List<string> lines;
            double ascent;
            double descent;
            double lineAdvance;
            double contentHeight;

            while (true)
            {
                lines = WrapTextIntoLines(text, metrics, fontSize, availableWidth);
                ascent = ScaleMetric(metrics?.Ascent ?? 800, fontSize);
                descent = ScaleMetric(metrics?.Descent ?? -200, fontSize);
                lineAdvance = fontSize * MultilineLineHeightMultiplier;
                contentHeight = ascent + descent + ((Math.Max(lines.Count, 1) - 1) * lineAdvance);

                var widestLine = lines.Count == 0 ? 0d : lines.Max(line => MeasureTextWidth(line, metrics, fontSize));
                var fitsWidth = widestLine <= availableWidth;
                var fitsHeight = contentHeight <= availableHeight;

                if (layoutOptions.Overflow != TextOverflowMode.ShrinkToFit || (fitsWidth && fitsHeight) || fontSize <= minimum)
                {
                    break;
                }

                fontSize = Math.Max(minimum, fontSize - 0.25d);
            }

            var firstBaseline = CalculateWrappedFirstBaseline(
                layoutOptions.VerticalAlignment,
                contentBottom,
                contentTop,
                contentHeight,
                ascent,
                descent,
                lineAdvance,
                Math.Max(lines.Count, 1));

            var segments = new List<ResolvedTextSegment>(lines.Count);
            for (var index = 0; index < lines.Count; index++)
            {
                var line = lines[index];
                var lineWidth = MeasureTextWidth(line, metrics, fontSize);
                var lineOriginX = CalculateHorizontalOrigin(
                    layoutOptions.HorizontalAlignment,
                    ResolveReadingDirection(line, layoutOptions.ReadingDirection),
                    contentLeft,
                    contentRight,
                    lineWidth);

                segments.Add(new ResolvedTextSegment(
                    line,
                    new Coordinate(lineOriginX, firstBaseline - (index * lineAdvance))));
            }

            if (segments.Count == 0)
            {
                segments.Add(new ResolvedTextSegment(string.Empty, new Coordinate(contentLeft, firstBaseline)));
            }

            return new WrappedTextLayout(fontSize, segments);
        }

        private async Task<FontMetrics?> ResolveFontMetricsAsync(Name resourceName)
        {
            var resourcesDictionary = await Dictionary.Resources.GetAsync();
            if (resourcesDictionary is null)
            {
                return null;
            }

            var resources = ResourceDictionary.FromDictionary(resourcesDictionary);
            var fontResources = await resources.Font.GetAsync();
            if (fontResources is null || !fontResources.ContainsKey(resourceName))
            {
                return null;
            }

            var fontReference = fontResources.GetAs<IndirectObjectReference>(resourceName);
            if (fontReference is null)
            {
                return null;
            }

            var fontDictionary = await _pdf.Objects.GetAsync<FontDictionary>(fontReference);
            if (fontDictionary is null)
            {
                return null;
            }

            var baseFontName = await fontDictionary.BaseFont.GetAsync();
            if (baseFontName is not null)
            {
                var standardFontMetrics = new PDFStandardFontMetricsProvider();
                if (standardFontMetrics.IsSupported(baseFontName))
                {
                    return standardFontMetrics.GetFontMetrics(baseFontName);
                }
            }

            var fontDescriptor = await fontDictionary.FontDescriptor.GetAsync();
            ArrayObject? widthsArray = await fontDictionary.Widths.GetAsync();
            Number? firstCharCode = await fontDictionary.FirstChar.GetAsync();
            if (fontDescriptor is null || widthsArray is null || firstCharCode is null)
            {
                return null;
            }

            var widths = widthsArray
                .Cast<Number>()
                .Select((width, index) => new { width, index })
                .ToDictionary(x => (char)(firstCharCode + x.index), x => (int)x.width);

            return await fontDescriptor.ToFontMetricsAsync(widths);
        }

        private static Rectangle ApplyPadding(Rectangle boundingBox, TextPadding padding)
        {
            var left = (double)boundingBox.LowerLeft.X + padding.Left;
            var bottom = (double)boundingBox.LowerLeft.Y + padding.Bottom;
            var right = (double)boundingBox.UpperRight.X - padding.Right;
            var top = (double)boundingBox.UpperRight.Y - padding.Top;

            if (right < left)
            {
                right = left;
            }

            if (top < bottom)
            {
                top = bottom;
            }

            return Rectangle.FromCoordinates(new Coordinate(left, bottom), new Coordinate(right, top));
        }

        private static double ShrinkToFit(string text, FontMetrics? metrics, double requestedFontSize, Rectangle contentBounds, double minFontSize)
        {
            var fontSize = requestedFontSize;
            var availableWidth = Math.Max(0, (double)contentBounds.Width);
            var availableHeight = Math.Max(0, (double)contentBounds.Height);
            var minimum = Math.Max(0.1d, minFontSize);

            while (fontSize > minimum)
            {
                var textWidth = MeasureTextWidth(text, metrics, fontSize);
                var textHeight = ScaleMetric(metrics?.Ascent ?? 800, fontSize) + ScaleMetric(metrics?.Descent ?? -200, fontSize);

                if (textWidth <= availableWidth && textHeight <= availableHeight)
                {
                    break;
                }

                var widthScale = textWidth > 0 ? availableWidth / textWidth : 1;
                var heightScale = textHeight > 0 ? availableHeight / textHeight : 1;
                var scale = Math.Min(widthScale, heightScale);

                if (scale > 0 && scale < 1)
                {
                    fontSize = Math.Max(minimum, fontSize * scale);
                }
                else
                {
                    fontSize = Math.Max(minimum, fontSize - 0.5d);
                }
            }

            return Math.Max(minimum, fontSize);
        }

        private static double MeasureTextWidth(string text, FontMetrics? metrics, double fontSize)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            if (metrics is not null)
            {
                return metrics.CalculateStringWidth(text, fontSize);
            }

            return text.Sum(ch => char.IsWhiteSpace(ch) ? fontSize * 0.33d : fontSize * 0.55d);
        }

        private static List<string> WrapTextIntoLines(string text, FontMetrics? metrics, double fontSize, double availableWidth)
        {
            var normalizedText = text.Replace("\r\n", "\n").Replace('\r', '\n');
            var paragraphs = normalizedText.Split('\n');
            var lines = new List<string>();

            foreach (var paragraph in paragraphs)
            {
                if (paragraph.Length == 0)
                {
                    lines.Add(string.Empty);
                    continue;
                }

                var currentLine = string.Empty;
                foreach (Match tokenMatch in LineTokenRegex.Matches(paragraph))
                {
                    var token = tokenMatch.Value;
                    var candidateLine = currentLine + token;

                    if (currentLine.Length == 0 || MeasureTextWidth(candidateLine, metrics, fontSize) <= availableWidth)
                    {
                        currentLine = candidateLine;
                        continue;
                    }

                    lines.Add(currentLine.TrimEnd());
                    currentLine = token.TrimStart();

                    while (currentLine.Length > 0 && MeasureTextWidth(currentLine, metrics, fontSize) > availableWidth)
                    {
                        var splitIndex = FindLargestPrefixThatFits(currentLine, metrics, fontSize, availableWidth);
                        lines.Add(currentLine[..splitIndex]);
                        currentLine = currentLine[splitIndex..].TrimStart();
                    }
                }

                if (currentLine.Length > 0)
                {
                    lines.Add(currentLine.TrimEnd());
                }
            }

            return lines.Count == 0 ? [string.Empty] : lines;
        }

        private static int FindLargestPrefixThatFits(string text, FontMetrics? metrics, double fontSize, double availableWidth)
        {
            for (var length = text.Length; length > 1; length--)
            {
                if (MeasureTextWidth(text[..length], metrics, fontSize) <= availableWidth)
                {
                    return length;
                }
            }

            return 1;
        }

        private static double ScaleMetric(int metric, double fontSize)
            => Math.Abs(metric) / 1000d * fontSize;

        private static double CalculateHorizontalOrigin(
            TextHorizontalAlignment alignment,
            TextReadingDirection readingDirection,
            double left,
            double right,
            double textWidth)
        {
            return alignment switch
            {
                TextHorizontalAlignment.Center => left + ((right - left - textWidth) / 2d),
                TextHorizontalAlignment.End when readingDirection == TextReadingDirection.RightToLeft => left,
                TextHorizontalAlignment.End => right - textWidth,
                TextHorizontalAlignment.Start when readingDirection == TextReadingDirection.RightToLeft => right - textWidth,
                _ => left
            };
        }

        private static double CalculateVerticalOrigin(
            TextVerticalAlignment alignment,
            double bottom,
            double top,
            double textHeight,
            double ascent,
            double descent)
        {
            return alignment switch
            {
                TextVerticalAlignment.Top => top - ascent,
                TextVerticalAlignment.Bottom => bottom + descent,
                _ => bottom + ((top - bottom - textHeight) / 2d) + descent
            };
        }

        private static double CalculateWrappedFirstBaseline(
            TextVerticalAlignment alignment,
            double bottom,
            double top,
            double contentHeight,
            double ascent,
            double descent,
            double lineAdvance,
            int lineCount)
        {
            return alignment switch
            {
                TextVerticalAlignment.Top => top - ascent,
                TextVerticalAlignment.Bottom => bottom + descent + ((lineCount - 1) * lineAdvance),
                _ => bottom + ((top - bottom - contentHeight) / 2d) + descent + ((lineCount - 1) * lineAdvance)
            };
        }

        private static TextReadingDirection ResolveReadingDirection(string text, TextReadingDirection readingDirection)
        {
            if (readingDirection != TextReadingDirection.Auto)
            {
                return readingDirection;
            }

            foreach (var character in text)
            {
                if (char.IsWhiteSpace(character) || char.IsPunctuation(character))
                {
                    continue;
                }

                if (IsRightToLeftCharacter(character))
                {
                    return TextReadingDirection.RightToLeft;
                }

                return TextReadingDirection.LeftToRight;
            }

            return TextReadingDirection.LeftToRight;
        }

        private static bool IsRightToLeftCharacter(char character)
        {
            var codePoint = (int)character;
            return codePoint is
                >= 0x0590 and <= 0x05FF or
                >= 0x0600 and <= 0x06FF or
                >= 0x0700 and <= 0x08FF or
                >= 0xFB1D and <= 0xFDFF or
                >= 0xFE70 and <= 0xFEFF;
        }

        private sealed record ResolvedTextSegment(string Text, Coordinate TextOrigin);
        private sealed record WrappedTextLayout(double FontSize, IReadOnlyList<ResolvedTextSegment> Segments);
        private sealed record ResolvedTextLayout(FontOptions FontOptions, IReadOnlyList<ResolvedTextSegment> Segments, Rectangle? ClipBounds);
    }
}
