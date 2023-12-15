namespace ZingPdf.Core.Extensions
{
    internal static class DictionaryExtensions
    {
        public static void MergeInto<TKey, TValue>(this IDictionary<TKey, TValue> source, IDictionary<TKey, TValue> target) where TKey : notnull
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (target is null) throw new ArgumentNullException(nameof(target));

            source.ToList().ForEach(x => target[x.Key] = x.Value);
        }
    }
}
