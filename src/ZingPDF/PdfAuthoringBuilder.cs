using ZingPDF.Elements;
using ZingPDF.Elements.Drawing;
using ZingPDF.Fonts;
using ZingPDF.Graphics;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Text;
using ImageElement = ZingPDF.Elements.Image;
using DrawingPath = ZingPDF.Elements.Drawing.Path;
using DrawingRectangle = ZingPDF.Syntax.CommonDataStructures.Rectangle;

namespace ZingPDF;

/// <summary>
/// Fluent authoring surface for creating PDFs.
/// </summary>
public sealed class PdfAuthoringBuilder
{
    private static readonly Size DefaultPageSize = new(595, 842);

    private readonly List<PagePlan> _pages = [];

    /// <summary>
    /// Adds a page to the document and configures its authored content.
    /// </summary>
    public PdfAuthoringBuilder Page(Action<PdfPageAuthoringBuilder> configurePage)
    {
        ArgumentNullException.ThrowIfNull(configurePage);

        var builder = new PdfPageAuthoringBuilder();
        configurePage(builder);
        _pages.Add(builder.Build());

        return this;
    }

    /// <summary>
    /// Adds a page to the document and configures its authored content.
    /// </summary>
    public PdfAuthoringBuilder AddPage(Action<PdfPageAuthoringBuilder> configurePage)
        => Page(configurePage);

    /// <summary>
    /// Materializes the fluent authoring plan and saves the created PDF to the supplied stream.
    /// </summary>
    public async Task SaveAsync(Stream output)
    {
        ArgumentNullException.ThrowIfNull(output);

        if (_pages.Count == 0)
        {
            throw new InvalidOperationException("At least one page must be configured before saving.");
        }

        using var pdf = Pdf.Create(options => options.MediaBox = DrawingRectangle.FromSize(_pages[0].PageSize ?? DefaultPageSize));
        var context = new AuthoringContext(pdf);

        var firstPage = await pdf.GetPageAsync(1);
        await _pages[0].ApplyAsync(firstPage, context);

        foreach (var pagePlan in _pages.Skip(1))
        {
            var page = await pdf.AppendPageAsync(options => options.MediaBox = DrawingRectangle.FromSize(pagePlan.PageSize ?? DefaultPageSize));
            await pagePlan.ApplyAsync(page, context);
        }

        await pdf.SaveAsync(output);
    }

    /// <summary>
    /// Materializes the fluent authoring plan and saves the created PDF to a file path.
    /// </summary>
    public async Task SaveToFileAsync(string outputPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        var fullPath = System.IO.Path.GetFullPath(outputPath);
        var directory = System.IO.Path.GetDirectoryName(fullPath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var output = File.Create(fullPath);
        await SaveAsync(output);
    }

    internal sealed class AuthoringContext(Pdf pdf)
    {
        private readonly Dictionary<object, PdfFont> _fonts = [];

        public async Task<PdfFont> ResolveFontAsync(FontPlan fontPlan)
        {
            if (_fonts.TryGetValue(fontPlan.CacheKey, out var font))
            {
                return font;
            }

            font = await fontPlan.Factory(pdf);
            _fonts[fontPlan.CacheKey] = font;

            return font;
        }
    }

    internal sealed record FontPlan(object CacheKey, Func<Pdf, Task<PdfFont>> Factory);

    internal sealed record PagePlan(Size? PageSize, IReadOnlyList<IPageOperation> Operations)
    {
        public async Task ApplyAsync(Page page, AuthoringContext context)
        {
            foreach (var operation in Operations)
            {
                await operation.ApplyAsync(page, context);
            }
        }
    }

    internal interface IPageOperation
    {
        Task ApplyAsync(Page page, AuthoringContext context);
    }

    internal sealed record TextOperation(
        string Value,
        double? X,
        double? Y,
        DrawingRectangle? Bounds,
        FontPlan Font,
        double FontSize,
        RGBColour Colour,
        TextLayoutOptions? LayoutOptions) : IPageOperation
    {
        public async Task ApplyAsync(Page page, AuthoringContext context)
        {
            var font = await context.ResolveFontAsync(Font);

            if (Bounds is not null)
            {
                await page.AddTextAsync(
                    Value,
                    Bounds,
                    font,
                    FontSize,
                    Colour,
                    LayoutOptions);
                return;
            }

            var text = new TextObject(
                Value,
                new Coordinate(X!.Value, Y!.Value),
                font,
                FontSize,
                Colour);

            await page.AddTextAsync(text);
        }
    }

    internal sealed record RectangleOperation(
        double X,
        double Y,
        double Width,
        double Height,
        RGBColour? StrokeColour,
        int StrokeWidth,
        RGBColour? FillColour) : IPageOperation
    {
        public Task ApplyAsync(Page page, AuthoringContext context)
        {
            var stroke = StrokeColour is null ? null : new StrokeOptions(StrokeColour, StrokeWidth);
            var fill = FillColour is null ? null : new FillOptions(FillColour);

            var path = new DrawingPath(
                stroke,
                fill,
                PathType.Linear,
                [
                    new Coordinate(X, Y),
                    new Coordinate(X + Width, Y),
                    new Coordinate(X + Width, Y + Height),
                    new Coordinate(X, Y + Height),
                    new Coordinate(X, Y)
                ]);

            return page.AddPathAsync(path);
        }
    }

    internal sealed record LineOperation(
        double FromX,
        double FromY,
        double ToX,
        double ToY,
        RGBColour StrokeColour,
        int StrokeWidth) : IPageOperation
    {
        public Task ApplyAsync(Page page, AuthoringContext context)
        {
            var path = new DrawingPath(
                new StrokeOptions(StrokeColour, StrokeWidth),
                null,
                PathType.Linear,
                [
                    new Coordinate(FromX, FromY),
                    new Coordinate(ToX, ToY)
                ]);

            return page.AddPathAsync(path);
        }
    }

    internal sealed record PathOperation(
        IReadOnlyList<Coordinate> Points,
        PathType PathType,
        RGBColour? StrokeColour,
        int StrokeWidth,
        RGBColour? FillColour) : IPageOperation
    {
        public Task ApplyAsync(Page page, AuthoringContext context)
        {
            var stroke = StrokeColour is null ? null : new StrokeOptions(StrokeColour, StrokeWidth);
            var fill = FillColour is null ? null : new FillOptions(FillColour);
            var path = new DrawingPath(stroke, fill, PathType, Points);

            return page.AddPathAsync(path);
        }
    }

    internal sealed record ImageOperation(
        Func<ImageElement> ImageFactory) : IPageOperation
    {
        public async Task ApplyAsync(Page page, AuthoringContext context)
        {
            using var image = ImageFactory();
            await page.AddImageAsync(image);
        }
    }

    internal sealed record WatermarkOperation(string Text) : IPageOperation
    {
        public Task ApplyAsync(Page page, AuthoringContext context) => page.AddWatermarkAsync(Text);
    }

    /// <summary>
    /// Configures the content for a single authored page.
    /// </summary>
    public sealed class PdfPageAuthoringBuilder
    {
        private readonly List<IPageOperation> _operations = [];
        private Size? _pageSize;

        /// <summary>
        /// Sets the page size in PDF points.
        /// </summary>
        public PdfPageAuthoringBuilder Size(double width, double height)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

            _pageSize = new Size(width, height);
            return this;
        }

        /// <summary>
        /// Sets the page size using a <see cref="ZingPDF.Elements.Drawing.Size"/> value.
        /// </summary>
        public PdfPageAuthoringBuilder Size(Size pageSize)
        {
            ArgumentNullException.ThrowIfNull(pageSize);
            return Size(pageSize.Width, pageSize.Height);
        }

        /// <summary>
        /// Adds a text operation to the page.
        /// </summary>
        public PdfPageAuthoringBuilder Text(Action<PdfTextAuthoringBuilder> configureText)
        {
            ArgumentNullException.ThrowIfNull(configureText);

            var builder = new PdfTextAuthoringBuilder();
            configureText(builder);
            _operations.Add(builder.Build());

            return this;
        }

        /// <summary>
        /// Adds a rectangle operation to the page.
        /// </summary>
        public PdfPageAuthoringBuilder Rectangle(Action<PdfRectangleAuthoringBuilder> configureRectangle)
        {
            ArgumentNullException.ThrowIfNull(configureRectangle);

            var builder = new PdfRectangleAuthoringBuilder();
            configureRectangle(builder);
            _operations.Add(builder.Build());

            return this;
        }

        /// <summary>
        /// Adds a straight line operation to the page.
        /// </summary>
        public PdfPageAuthoringBuilder Line(Action<PdfLineAuthoringBuilder> configureLine)
        {
            ArgumentNullException.ThrowIfNull(configureLine);

            var builder = new PdfLineAuthoringBuilder();
            configureLine(builder);
            _operations.Add(builder.Build());

            return this;
        }

        /// <summary>
        /// Adds a path operation to the page.
        /// </summary>
        public PdfPageAuthoringBuilder Path(Action<PdfPathAuthoringBuilder> configurePath)
        {
            ArgumentNullException.ThrowIfNull(configurePath);

            var builder = new PdfPathAuthoringBuilder();
            configurePath(builder);
            _operations.Add(builder.Build());

            return this;
        }

        /// <summary>
        /// Adds an image operation to the page.
        /// </summary>
        public PdfPageAuthoringBuilder Image(Action<PdfImageAuthoringBuilder> configureImage)
        {
            ArgumentNullException.ThrowIfNull(configureImage);

            var builder = new PdfImageAuthoringBuilder();
            configureImage(builder);
            _operations.Add(builder.Build());

            return this;
        }

        /// <summary>
        /// Adds a text watermark operation to the page.
        /// </summary>
        public PdfPageAuthoringBuilder Watermark(string text)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(text);
            _operations.Add(new WatermarkOperation(text));
            return this;
        }

        internal PagePlan Build() => new(_pageSize, [.. _operations]);
    }

    /// <summary>
    /// Configures a text operation for an authored page.
    /// </summary>
    public sealed class PdfTextAuthoringBuilder
    {
        private string? _value;
        private double? _x;
        private double? _y;
        private DrawingRectangle? _bounds;
        private FontPlan _fontPlan = new(
            StandardPdfFonts.Helvetica,
            pdf => pdf.RegisterStandardFontAsync(StandardPdfFonts.Helvetica));
        private double _fontSize = 12;
        private RGBColour _colour = RGBColour.Black;
        private TextLayoutOptions _layoutOptions = new();

        /// <summary>
        /// Sets the text content to render.
        /// </summary>
        public PdfTextAuthoringBuilder Value(string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            _value = value;
            return this;
        }

        /// <summary>
        /// Sets the text origin in PDF points for single-line positioned text.
        /// </summary>
        public PdfTextAuthoringBuilder At(double x, double y)
        {
            _x = x;
            _y = y;
            _bounds = null;
            return this;
        }

        /// <summary>
        /// Places the text inside a layout box in PDF points.
        /// </summary>
        public PdfTextAuthoringBuilder InBox(double x, double y, double width, double height)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

            _bounds = DrawingRectangle.FromCoordinates(
                new Coordinate(x, y),
                new Coordinate(x + width, y + height));
            _x = null;
            _y = null;

            return this;
        }

        /// <summary>
        /// Places the text inside a layout box.
        /// </summary>
        public PdfTextAuthoringBuilder InBox(DrawingRectangle bounds)
        {
            ArgumentNullException.ThrowIfNull(bounds);

            _bounds = bounds;
            _x = null;
            _y = null;

            return this;
        }

        /// <summary>
        /// Uses a standard PDF font by name.
        /// </summary>
        public PdfTextAuthoringBuilder Font(string fontName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fontName);
            _fontPlan = new FontPlan(fontName, pdf => pdf.RegisterStandardFontAsync(fontName));
            return this;
        }

        /// <summary>Uses Helvetica.</summary>
        public PdfTextAuthoringBuilder Helvetica() => Font(StandardPdfFonts.Helvetica);
        /// <summary>Uses Helvetica Bold.</summary>
        public PdfTextAuthoringBuilder HelveticaBold() => Font(StandardPdfFonts.HelveticaBold);
        /// <summary>Uses Helvetica Oblique.</summary>
        public PdfTextAuthoringBuilder HelveticaOblique() => Font(StandardPdfFonts.HelveticaOblique);
        /// <summary>Uses Helvetica Bold Oblique.</summary>
        public PdfTextAuthoringBuilder HelveticaBoldOblique() => Font(StandardPdfFonts.HelveticaBoldOblique);
        /// <summary>Uses Times Roman.</summary>
        public PdfTextAuthoringBuilder TimesRoman() => Font(StandardPdfFonts.TimesRoman);
        /// <summary>Uses Times Bold.</summary>
        public PdfTextAuthoringBuilder TimesBold() => Font(StandardPdfFonts.TimesBold);
        /// <summary>Uses Times Italic.</summary>
        public PdfTextAuthoringBuilder TimesItalic() => Font(StandardPdfFonts.TimesItalic);
        /// <summary>Uses Times Bold Italic.</summary>
        public PdfTextAuthoringBuilder TimesBoldItalic() => Font(StandardPdfFonts.TimesBoldItalic);
        /// <summary>Uses Courier.</summary>
        public PdfTextAuthoringBuilder Courier() => Font(StandardPdfFonts.Courier);
        /// <summary>Uses Courier Bold.</summary>
        public PdfTextAuthoringBuilder CourierBold() => Font(StandardPdfFonts.CourierBold);
        /// <summary>Uses Courier Oblique.</summary>
        public PdfTextAuthoringBuilder CourierOblique() => Font(StandardPdfFonts.CourierOblique);
        /// <summary>Uses Courier Bold Oblique.</summary>
        public PdfTextAuthoringBuilder CourierBoldOblique() => Font(StandardPdfFonts.CourierBoldOblique);

        /// <summary>
        /// Resolves the font from a custom registration function.
        /// </summary>
        public PdfTextAuthoringBuilder Font(Func<IPdf, Task<PdfFont>> fontFactory, object? cacheKey = null)
        {
            ArgumentNullException.ThrowIfNull(fontFactory);

            _fontPlan = new FontPlan(
                cacheKey ?? fontFactory,
                pdf => fontFactory(pdf));
            return this;
        }

        /// <summary>
        /// Registers and uses an embedded TrueType font from a file path.
        /// </summary>
        public PdfTextAuthoringBuilder WithTrueTypeFont(string fontPath, string? resourceName = null, string? fontName = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fontPath);

            var fullPath = System.IO.Path.GetFullPath(fontPath);
            var cacheKey = string.Join("|", "truetype-file", fullPath, resourceName ?? string.Empty, fontName ?? string.Empty);

            return Font(
                pdf => pdf.RegisterTrueTypeFontAsync(fullPath, resourceName, fontName),
                cacheKey);
        }

        /// <summary>
        /// Registers and uses an embedded TrueType font from a stream factory.
        /// </summary>
        public PdfTextAuthoringBuilder WithTrueTypeFont(Func<Stream> streamFactory, object? cacheKey = null, string? resourceName = null, string? fontName = null)
        {
            ArgumentNullException.ThrowIfNull(streamFactory);

            return Font(
                async pdf =>
                {
                    await using var stream = streamFactory();
                    return await pdf.RegisterTrueTypeFontAsync(stream, resourceName, fontName);
                },
                cacheKey ?? streamFactory);
        }

        /// <summary>
        /// Sets the requested font size in points.
        /// </summary>
        public PdfTextAuthoringBuilder FontSize(double fontSize)
        {
            if (fontSize <= 0) throw new ArgumentOutOfRangeException(nameof(fontSize));
            _fontSize = fontSize;
            return this;
        }

        /// <summary>
        /// Sets the text colour.
        /// </summary>
        public PdfTextAuthoringBuilder Color(RGBColour colour)
        {
            _colour = colour ?? throw new ArgumentNullException(nameof(colour));
            return this;
        }

        /// <summary>
        /// Sets the text colour.
        /// </summary>
        public PdfTextAuthoringBuilder Colour(RGBColour colour) => Color(colour);

        /// <summary>
        /// Applies uniform padding inside the text layout box.
        /// </summary>
        public PdfTextAuthoringBuilder Padding(double uniformPadding)
            => Padding(TextPadding.Uniform(uniformPadding));

        /// <summary>
        /// Applies per-edge padding inside the text layout box.
        /// </summary>
        public PdfTextAuthoringBuilder Padding(double left, double top, double right, double bottom)
            => Padding(new TextPadding(left, top, right, bottom));

        /// <summary>
        /// Applies padding inside the text layout box.
        /// </summary>
        public PdfTextAuthoringBuilder Padding(TextPadding padding)
        {
            _layoutOptions = _layoutOptions with { Padding = padding };
            return this;
        }

        /// <summary>Aligns boxed text to the start edge.</summary>
        public PdfTextAuthoringBuilder AlignStart()
        {
            _layoutOptions = _layoutOptions with { HorizontalAlignment = TextHorizontalAlignment.Start };
            return this;
        }

        /// <summary>Centers boxed text horizontally.</summary>
        public PdfTextAuthoringBuilder AlignCenter()
        {
            _layoutOptions = _layoutOptions with { HorizontalAlignment = TextHorizontalAlignment.Center };
            return this;
        }

        /// <summary>Aligns boxed text to the end edge.</summary>
        public PdfTextAuthoringBuilder AlignEnd()
        {
            _layoutOptions = _layoutOptions with { HorizontalAlignment = TextHorizontalAlignment.End };
            return this;
        }

        /// <summary>Aligns boxed text to the top of its layout box.</summary>
        public PdfTextAuthoringBuilder AlignTop()
        {
            _layoutOptions = _layoutOptions with { VerticalAlignment = TextVerticalAlignment.Top };
            return this;
        }

        /// <summary>Centers boxed text vertically.</summary>
        public PdfTextAuthoringBuilder AlignMiddle()
        {
            _layoutOptions = _layoutOptions with { VerticalAlignment = TextVerticalAlignment.Middle };
            return this;
        }

        /// <summary>Aligns boxed text to the bottom of its layout box.</summary>
        public PdfTextAuthoringBuilder AlignBottom()
        {
            _layoutOptions = _layoutOptions with { VerticalAlignment = TextVerticalAlignment.Bottom };
            return this;
        }

        /// <summary>
        /// Clips boxed text to the layout bounds.
        /// </summary>
        public PdfTextAuthoringBuilder ClipOverflow()
        {
            _layoutOptions = _layoutOptions with { Overflow = TextOverflowMode.Clip };
            return this;
        }

        /// <summary>
        /// Wraps boxed text onto multiple lines.
        /// </summary>
        public PdfTextAuthoringBuilder Wrap()
        {
            _layoutOptions = _layoutOptions with { Wrap = true };
            return this;
        }

        /// <summary>
        /// Allows text to overflow the layout box without clipping.
        /// </summary>
        public PdfTextAuthoringBuilder AllowOverflow()
        {
            _layoutOptions = _layoutOptions with { Overflow = TextOverflowMode.Visible };
            return this;
        }

        /// <summary>
        /// Shrinks boxed text until it fits within the layout box.
        /// </summary>
        public PdfTextAuthoringBuilder ShrinkToFit(double minFontSize = 4)
        {
            if (minFontSize <= 0) throw new ArgumentOutOfRangeException(nameof(minFontSize));

            _layoutOptions = _layoutOptions with
            {
                Overflow = TextOverflowMode.ShrinkToFit,
                MinFontSize = minFontSize
            };
            return this;
        }

        /// <summary>Uses left-to-right layout when start/end alignment is applied.</summary>
        public PdfTextAuthoringBuilder LeftToRight()
        {
            _layoutOptions = _layoutOptions with { ReadingDirection = TextReadingDirection.LeftToRight };
            return this;
        }

        /// <summary>Uses right-to-left layout when start/end alignment is applied.</summary>
        public PdfTextAuthoringBuilder RightToLeft()
        {
            _layoutOptions = _layoutOptions with { ReadingDirection = TextReadingDirection.RightToLeft };
            return this;
        }

        /// <summary>Infers reading direction from the text content.</summary>
        public PdfTextAuthoringBuilder AutoDirection()
        {
            _layoutOptions = _layoutOptions with { ReadingDirection = TextReadingDirection.Auto };
            return this;
        }

        internal TextOperation Build()
        {
            if (_value is null)
            {
                throw new InvalidOperationException("Text must have a value.");
            }

            if (_bounds is null && (_x is null || _y is null))
            {
                throw new InvalidOperationException("Text must have either a position or a layout box.");
            }

            return new TextOperation(
                _value,
                _x,
                _y,
                _bounds,
                _fontPlan,
                _fontSize,
                _colour,
                _bounds is null ? null : _layoutOptions);
        }
    }

    /// <summary>
    /// Configures a rectangle operation for an authored page.
    /// </summary>
    public sealed class PdfRectangleAuthoringBuilder
    {
        private double? _x;
        private double? _y;
        private double? _width;
        private double? _height;
        private RGBColour? _strokeColour = RGBColour.Black;
        private int _strokeWidth = 1;
        private RGBColour? _fillColour;

        /// <summary>Sets the rectangle origin in PDF points.</summary>
        public PdfRectangleAuthoringBuilder At(double x, double y)
        {
            _x = x;
            _y = y;
            return this;
        }

        /// <summary>Sets the rectangle width in PDF points.</summary>
        public PdfRectangleAuthoringBuilder Width(double width)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            _width = width;
            return this;
        }

        /// <summary>Sets the rectangle height in PDF points.</summary>
        public PdfRectangleAuthoringBuilder Height(double height)
        {
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            _height = height;
            return this;
        }

        /// <summary>Sets the rectangle size in PDF points.</summary>
        public PdfRectangleAuthoringBuilder Size(double width, double height)
        {
            Width(width);
            Height(height);
            return this;
        }

        /// <summary>Sets the rectangle size.</summary>
        public PdfRectangleAuthoringBuilder Size(Size size)
        {
            ArgumentNullException.ThrowIfNull(size);
            return Size(size.Width, size.Height);
        }

        /// <summary>Applies a stroke colour and width.</summary>
        public PdfRectangleAuthoringBuilder Stroke(RGBColour colour, int width = 1)
        {
            ArgumentNullException.ThrowIfNull(colour);
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));

            _strokeColour = colour;
            _strokeWidth = width;
            return this;
        }

        /// <summary>Removes the rectangle stroke.</summary>
        public PdfRectangleAuthoringBuilder WithoutStroke()
        {
            _strokeColour = null;
            return this;
        }

        /// <summary>Applies a fill colour.</summary>
        public PdfRectangleAuthoringBuilder Fill(RGBColour colour)
        {
            _fillColour = colour ?? throw new ArgumentNullException(nameof(colour));
            return this;
        }

        internal RectangleOperation Build()
        {
            if (_x is null || _y is null)
            {
                throw new InvalidOperationException("Rectangle must have a position.");
            }

            if (_width is null || _height is null)
            {
                throw new InvalidOperationException("Rectangle must have a width and height.");
            }

            return new RectangleOperation(
                _x.Value,
                _y.Value,
                _width.Value,
                _height.Value,
                _strokeColour,
                _strokeWidth,
                _fillColour);
        }
    }

    /// <summary>
    /// Configures a line operation for an authored page.
    /// </summary>
    public sealed class PdfLineAuthoringBuilder
    {
        private double? _fromX;
        private double? _fromY;
        private double? _toX;
        private double? _toY;
        private RGBColour _strokeColour = RGBColour.Black;
        private int _strokeWidth = 1;

        /// <summary>Sets the start point in PDF points.</summary>
        public PdfLineAuthoringBuilder From(double x, double y)
        {
            _fromX = x;
            _fromY = y;
            return this;
        }

        /// <summary>Sets the end point in PDF points.</summary>
        public PdfLineAuthoringBuilder To(double x, double y)
        {
            _toX = x;
            _toY = y;
            return this;
        }

        /// <summary>Applies a stroke colour and width.</summary>
        public PdfLineAuthoringBuilder Stroke(RGBColour colour, int width = 1)
        {
            ArgumentNullException.ThrowIfNull(colour);
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));

            _strokeColour = colour;
            _strokeWidth = width;
            return this;
        }

        internal LineOperation Build()
        {
            if (_fromX is null || _fromY is null)
            {
                throw new InvalidOperationException("Line must have a start point.");
            }

            if (_toX is null || _toY is null)
            {
                throw new InvalidOperationException("Line must have an end point.");
            }

            return new LineOperation(_fromX.Value, _fromY.Value, _toX.Value, _toY.Value, _strokeColour, _strokeWidth);
        }
    }

    /// <summary>
    /// Configures a path operation for an authored page.
    /// </summary>
    public sealed class PdfPathAuthoringBuilder
    {
        private readonly List<Coordinate> _points = [];
        private PathType _pathType = PathType.Linear;
        private RGBColour? _strokeColour = RGBColour.Black;
        private int _strokeWidth = 1;
        private RGBColour? _fillColour;

        /// <summary>Uses straight line segments between points.</summary>
        public PdfPathAuthoringBuilder Linear()
        {
            _pathType = PathType.Linear;
            return this;
        }

        /// <summary>Uses bezier curve segments between points.</summary>
        public PdfPathAuthoringBuilder Bezier()
        {
            _pathType = PathType.Bezier;
            return this;
        }

        /// <summary>Adds a point to the path.</summary>
        public PdfPathAuthoringBuilder Point(double x, double y)
        {
            _points.Add(new Coordinate(x, y));
            return this;
        }

        /// <summary>Applies a stroke colour and width.</summary>
        public PdfPathAuthoringBuilder Stroke(RGBColour colour, int width = 1)
        {
            ArgumentNullException.ThrowIfNull(colour);
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));

            _strokeColour = colour;
            _strokeWidth = width;
            return this;
        }

        /// <summary>Removes the path stroke.</summary>
        public PdfPathAuthoringBuilder WithoutStroke()
        {
            _strokeColour = null;
            return this;
        }

        /// <summary>Applies a fill colour.</summary>
        public PdfPathAuthoringBuilder Fill(RGBColour colour)
        {
            _fillColour = colour ?? throw new ArgumentNullException(nameof(colour));
            return this;
        }

        internal PathOperation Build()
        {
            if (_points.Count == 0)
            {
                throw new InvalidOperationException("Path must contain at least one point.");
            }

            return new PathOperation([.. _points], _pathType, _strokeColour, _strokeWidth, _fillColour);
        }
    }

    /// <summary>
    /// Configures an image operation for an authored page.
    /// </summary>
    public sealed class PdfImageAuthoringBuilder
    {
        private string? _imagePath;
        private Func<Stream>? _streamFactory;
        private double? _x;
        private double? _y;
        private double? _width;
        private double? _height;
        private bool _preserveAspectRatio = true;

        /// <summary>Uses an image loaded from disk.</summary>
        public PdfImageAuthoringBuilder FromFile(string imagePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(imagePath);
            _imagePath = imagePath;
            _streamFactory = null;
            return this;
        }

        /// <summary>Uses an image supplied by a stream factory.</summary>
        public PdfImageAuthoringBuilder FromStream(Func<Stream> streamFactory)
        {
            ArgumentNullException.ThrowIfNull(streamFactory);
            _streamFactory = streamFactory;
            _imagePath = null;
            return this;
        }

        /// <summary>Sets the image origin in PDF points.</summary>
        public PdfImageAuthoringBuilder At(double x, double y)
        {
            _x = x;
            _y = y;
            return this;
        }

        /// <summary>Sets the maximum image width in PDF points.</summary>
        public PdfImageAuthoringBuilder Width(double width)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            _width = width;
            return this;
        }

        /// <summary>Sets the maximum image height in PDF points.</summary>
        public PdfImageAuthoringBuilder Height(double height)
        {
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            _height = height;
            return this;
        }

        /// <summary>Sets the maximum image size in PDF points.</summary>
        public PdfImageAuthoringBuilder Size(double width, double height)
        {
            Width(width);
            Height(height);
            return this;
        }

        /// <summary>Sets the maximum image size.</summary>
        public PdfImageAuthoringBuilder Size(Size size)
        {
            ArgumentNullException.ThrowIfNull(size);
            return Size(size.Width, size.Height);
        }

        /// <summary>Controls whether the image should preserve its aspect ratio inside the supplied bounds.</summary>
        public PdfImageAuthoringBuilder PreserveAspectRatio(bool preserveAspectRatio = true)
        {
            _preserveAspectRatio = preserveAspectRatio;
            return this;
        }

        internal ImageOperation Build()
        {
            if (_x is null || _y is null)
            {
                throw new InvalidOperationException("Image must have a position.");
            }

            if (_width is null || _height is null)
            {
                throw new InvalidOperationException("Image must have a width and height.");
            }

            var bounds = DrawingRectangle.FromCoordinates(
                new Coordinate(_x.Value, _y.Value),
                new Coordinate(_x.Value + _width.Value, _y.Value + _height.Value));

            if (_imagePath is not null)
            {
                var fullPath = System.IO.Path.GetFullPath(_imagePath);
                return new ImageOperation(() => ImageElement.FromFile(fullPath, bounds, _preserveAspectRatio));
            }

            if (_streamFactory is not null)
            {
                return new ImageOperation(() => new ImageElement(_streamFactory(), bounds, _preserveAspectRatio));
            }

            throw new InvalidOperationException("Image must have a source.");
        }
    }
}
