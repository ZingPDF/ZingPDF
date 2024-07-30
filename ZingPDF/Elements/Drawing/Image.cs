namespace ZingPDF.Elements.Drawing
{
    public class Image
    {
        public Image(Stream data, Point position)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            Position = position ?? throw new ArgumentNullException(nameof(position));
        }

        /// <summary>
        /// The image to be rendered.
        /// </summary>
        public Stream Data { get; }

        /// <summary>
        /// The position at which to render the image.
        /// </summary>
        public Point Position { get; }
    }
}
