using ZingPdf.Core.Extensions;

namespace ZingPdf.Core.Objects.Primitives
{
    internal class StreamObjectFactory
    {
        private readonly IndirectObjectCollection _indirectObjectCollection;

        public StreamObjectFactory(IndirectObjectCollection indirectObjectCollection)
        {
            _indirectObjectCollection = indirectObjectCollection;
        }

        public IndirectObject Create()
        {
            var streamDictionary = new Dictionary<Name, PdfObject>()
            {
                { "Length", new Integer(0) }, // TODO: this is the byte length of the stream content
            };

            return _indirectObjectCollection.Add(new Dictionary(streamDictionary), new StreamObject());
        }

        /// <summary>
        /// PDF 32000-1:2008 7.3.8
        /// </summary>
        private class StreamObject : PdfObject
        {
            public override async Task WriteOutputAsync(Stream stream)
            {
                await stream.WriteNewLineAsync();
                await stream.WriteTextAsync(Constants.StreamStart);
                await stream.WriteNewLineAsync();

                // TODO: content

                await stream.WriteNewLineAsync();
                await stream.WriteTextAsync(Constants.StreamEnd);
            }
        }
    }
}
