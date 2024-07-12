using ZingPDF.ObjectModel.Filters;
using ZingPDF.ObjectModel.Objects.Streams;

namespace ZingPDF.ObjectModel.ContentStreamsAndResources
{
    /// <summary>
    /// ISO 32000-2:2020 7.8.2 - Content streams
    /// </summary>
    internal class ContentStream : StreamObject<ResourceDictionary>
    {
        public ContentStream(IEnumerable<IFilter>? filters) : base(filters)
        {
        }

        protected override Task<Stream> GetSourceDataAsync(ResourceDictionary dictionary)
        {
            throw new NotImplementedException();
        }

        protected override Task<ResourceDictionary> GetSpecialisedDictionaryAsync()
        {
            throw new NotImplementedException();
        }
    }
}
