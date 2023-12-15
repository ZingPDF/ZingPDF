using ZingPdf.Core.Parsing;

namespace ZingPdf.Core.Objects.Primitives.Streams
{
    /// <summary>
    /// Defines a <see cref="ParsedStreamObject{IStreamDictionary}"/> from a range within a <see cref="Stream"/>
    /// </summary>
    internal class SubStreamObject : ParsedStreamObject<IStreamDictionary>
    {
        public SubStreamObject(Stream stream, long from, long to, IStreamDictionary dictionary)
            : base(dictionary, new SubStream(stream, from, to))
        {
        }
    }
}
