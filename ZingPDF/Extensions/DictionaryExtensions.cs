using ZingPDF.Fonts;
using ZingPDF.Text;

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

        public static async Task<FontMetrics> ToFontMetricsAsync(
            this FontDescriptorDictionary fontDescriptor,
            Dictionary<char, int> widths,
            IIndirectObjectDictionary indirectObjectDictionary
            )
        {
            var fontProperties = new FontProperties(await fontDescriptor.Flags.GetAsync(indirectObjectDictionary));

            return new FontMetrics
            {
                Ascent = await fontDescriptor.Ascent.GetAsync(indirectObjectDictionary),
                Descent = await fontDescriptor.Descent.GetAsync(indirectObjectDictionary),
                StandardHorizontalWidth = fontDescriptor.StemH != null ? await fontDescriptor.StemH.GetAsync(indirectObjectDictionary) : null,
                StandardVerticalWidth = fontDescriptor.StemV != null ? await fontDescriptor.StemV.GetAsync(indirectObjectDictionary) : null,
                CapHeight = await fontDescriptor.CapHeight.GetAsync(indirectObjectDictionary),
                XHeight = await fontDescriptor.XHeight.GetAsync(indirectObjectDictionary),
                ItalicAngle = await fontDescriptor.ItalicAngle.GetAsync(indirectObjectDictionary),
                IsFixedPitch = fontProperties.IsFixedPitch,
                Widths = widths
            };
        }
    }
}
