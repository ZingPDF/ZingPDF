using ZingPDF.ObjectModel.Filters;
using ZingPDF.ObjectModel.Objects.Streams;

namespace ZingPDF.DocumentInterchange.Metadata
{
    internal class MetadataStream : StreamObject<MetadataStreamDictionary>
    {
        public MetadataStream(IEnumerable<IFilter>? filters) : base(filters)
        {
        }

        protected override Task<Stream> GetSourceDataAsync(MetadataStreamDictionary dictionary)
        {
            throw new NotImplementedException();
        }

        protected override Task<MetadataStreamDictionary> GetSpecialisedDictionaryAsync()
        {
            throw new NotImplementedException();
        }
    }
}
