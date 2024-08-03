using ZingPDF.Syntax.Filters;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF.DocumentInterchange.Metadata
{
    internal class MetadataStream : StreamObject<MetadataStreamDictionary>
    {
        public MetadataStream(IEnumerable<IFilter>? filters) : base(filters, false)
        {
        }

        protected override Task<Stream> GetSourceDataAsync(MetadataStreamDictionary dictionary)
        {
            throw new NotImplementedException();
        }

        protected override MetadataStreamDictionary GetSpecialisedDictionary()
        {
            throw new NotImplementedException();
        }
    }
}
