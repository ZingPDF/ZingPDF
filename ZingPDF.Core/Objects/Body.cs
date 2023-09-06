namespace ZingPdf.Core.Objects
{
    /// <summary>
    /// PDF 32000-1:2008 7.5.3
    /// </summary>
    internal class Body : PdfObject
    {
        private readonly PdfObject[] _objects;

        public Body(PdfObject[] objects)
        {
            _objects = objects ?? throw new ArgumentNullException(nameof(objects));
        }

        public override async Task WriteOutputAsync(Stream stream)
        {
            foreach (var obj in _objects)
            {
                await obj.WriteAsync(stream);
            }
        }
    }
}
