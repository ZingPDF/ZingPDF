using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Parsing
{
    internal class DictionaryParser : IPdfObjectParser<Dictionary>
    {
        private static string _defaultExceptionMessage = "Invalid dictionary";

        public Dictionary Parse(IEnumerable<string> tokens)
        {
            throw new NotImplementedException();
        }
    }
}
