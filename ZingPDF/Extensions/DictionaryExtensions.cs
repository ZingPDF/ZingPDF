namespace ZingPDF.Extensions
{
    internal static class DictionaryExtensions
    {
        /// <summary>
        /// Creates a new dictionary from the target dictionary, overriden with values from the source dictionary.
        /// </summary>
        public static Dictionary<TKey, TValue> MergeInto<TKey, TValue>(
            this IEnumerable<KeyValuePair<TKey, TValue>> source,
            IEnumerable<KeyValuePair<TKey, TValue>> target
            ) where TKey : notnull
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(target);

            var targetDict = target.ToDictionary();

            source.ToList().ForEach(x => targetDict[x.Key] = x.Value);

            return targetDict;
        }
    }
}
