namespace ZingPDF.Graphics.Images;

internal sealed class PreparedImageXObject(
    Stream data,
    int width,
    int height,
    string colorSpace,
    int bitsPerComponent,
    string? filterName,
    PreparedImageXObject? softMask = null)
{
    public Stream Data { get; } = data ?? throw new ArgumentNullException(nameof(data));
    public int Width { get; } = width;
    public int Height { get; } = height;
    public string ColorSpace { get; } = colorSpace ?? throw new ArgumentNullException(nameof(colorSpace));
    public int BitsPerComponent { get; } = bitsPerComponent;
    public string? FilterName { get; } = filterName;
    public PreparedImageXObject? SoftMask { get; } = softMask;
}
