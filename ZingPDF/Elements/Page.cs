using ZingPDF.Elements.Drawing;
using ZingPDF.Fonts;
using ZingPDF.Fonts.FontProviders;
using ZingPDF.Graphics.Images;
using ZingPDF.Extensions;
using ZingPDF.Syntax;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.DocumentStructure.PageTree;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;
using ZingPDF.Text;

namespace ZingPDF.Elements
{
    /// <summary>
    /// Represents a single page in a PDF document.
    /// </summary>
    public class Page
    {
        private readonly IPdf _pdf;

        internal Page(IndirectObject pageObject, IPdf pdf)
        {
            ArgumentNullException.ThrowIfNull(pageObject, nameof(pageObject));
            ArgumentNullException.ThrowIfNull(pdf);

            IndirectObject = pageObject;
            _pdf = pdf;
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
        /// Adds a text object to the page contents.
        /// </summary>
        public async Task AddTextAsync(TextObject text)
        {
            ArgumentNullException.ThrowIfNull(text);

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
            await AddTextAsync(new TextObject(text, layout.TextOrigin, layout.FontOptions, layout.ClipBounds));
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

            var textWidth = MeasureTextWidth(text, metrics, fontSize);
            var ascent = ScaleMetric(metrics?.Ascent ?? 800, fontSize);
            var descent = ScaleMetric(metrics?.Descent ?? -200, fontSize);
            var textHeight = ascent + descent;

            var contentLeft = (double)contentBounds.LowerLeft.X;
            var contentRight = (double)contentBounds.UpperRight.X;
            var contentBottom = (double)contentBounds.LowerLeft.Y;
            var contentTop = (double)contentBounds.UpperRight.Y;
            var availableWidth = Math.Max(0, contentRight - contentLeft);
            var availableHeight = Math.Max(0, contentTop - contentBottom);

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
                new Coordinate(originX, originY),
                clipBounds);
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

        private sealed record ResolvedTextLayout(FontOptions FontOptions, Coordinate TextOrigin, Rectangle? ClipBounds);
    }
}
