using ZingPDF.Parsing;

namespace ZingPDF.Objects.Primitives.Streams
{
    /// <summary>
    /// Defines a <see cref="ParsedStreamObject{IStreamDictionary}"/> from a range within a <see cref="Stream"/>
    /// </summary>
    internal class SubStreamObject : ParsedStreamObject<IStreamDictionary>
    {
        public SubStreamObject(Stream stream, long from, long to, IStreamDictionary dictionary)
            : base(dictionary, new SubStream(stream, from, to, setToStart: false))
        {
        }
    }
}
