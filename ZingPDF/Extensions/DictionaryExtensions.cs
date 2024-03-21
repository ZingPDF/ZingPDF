namespace ZingPDF.Extensions
{
    internal static class DictionaryExtensions
    {
        /// <summary>
        /// Merges the target dictionary into the source dictionary.<para></para>
        /// Existing values in the source dictionary will be overwritten.
        /// </summary>
        public static void MergeInto<TKey, TValue>(this IDictionary<TKey, TValue> source, IDictionary<TKey, TValue> target) where TKey : notnull
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(target);

            source.ToList().ForEach(x => target[x.Key] = x.Value);
        }
    }
}
