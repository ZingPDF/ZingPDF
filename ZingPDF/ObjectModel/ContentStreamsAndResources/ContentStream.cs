using ZingPDF.ObjectModel.Filters;
using ZingPDF.ObjectModel.Objects.Streams;

namespace ZingPDF.ObjectModel.ContentStreamsAndResources
{
    /// <summary>
    /// ISO 32000-2:2020 7.8.2 - Content streams
    /// </summary>
    internal class ContentStream<TDictionary> : StreamObject<TDictionary> where TDictionary : class, IStreamDictionary
    {
        public ContentStream(IEnumerable<IFilter>? filters) : base(filters)
        {
        }

        protected override Task<Stream> GetSourceDataAsync(TDictionary dictionary)
        {
            throw new NotImplementedException();
        }

        protected override Task<TDictionary> GetSpecialisedDictionaryAsync()
        {
            throw new NotImplementedException();
        }
    }
}
