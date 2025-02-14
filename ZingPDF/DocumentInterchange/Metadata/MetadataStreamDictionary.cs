using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF.DocumentInterchange.Metadata
{
    internal class MetadataStreamDictionary : StreamDictionary
    {
        private const string _subtype = "XML";

        public MetadataStreamDictionary() : base(Constants.DictionaryTypes.Metadata)
        {
            Set<Name>(Constants.DictionaryKeys.Subtype, _subtype);
        }

        private MetadataStreamDictionary(Dictionary streamDictionary) : base(streamDictionary)
        {
        }
    }
}
