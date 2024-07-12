using ZingPDF.ObjectModel.Filters;

namespace ZingPDF.ObjectModel.Functions.Type0
{
    /// <summary>
    /// ISO 32000-2:2020 - 7.10.2 Type 0 (sampled) functions
    /// </summary>
    internal class Type0Function : StreamFunction<Type0FunctionDictionary>
    {
        // TODO

        public Type0Function(IEnumerable<IFilter>? filters) : base(filters)
        {
        }

        protected override Task<Stream> GetSourceDataAsync(Type0FunctionDictionary dictionary)
        {
            throw new NotImplementedException();
        } 

        protected override Task<Type0FunctionDictionary> GetSpecialisedDictionaryAsync()
        {
            throw new NotImplementedException();
        }
    }
}
