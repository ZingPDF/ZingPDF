using ZingPDF.Fonts;
using ZingPDF.Fonts.FontProviders;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;
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

        public static async Task<IEnumerable<IFontMetricsProvider>> GetFontMetricsProvidersAsync(this ResourceDictionary resources, IPdfContext pdfContext)
        {
            var fontResourceMap = await resources.Font.GetAsync();
            if (fontResourceMap == null)
            {
                return [];
            }

            var simpleFontMetricsProvider = new SimpleFontMetricsProvider();

            List<IFontMetricsProvider> fontProviders = [simpleFontMetricsProvider];

            Dictionary<string, Stream> fontStreams = [];
            foreach (var kvp in fontResourceMap)
            {
                var fontDict = await pdfContext.Objects.GetAsync<FontDictionary>((IndirectObjectReference)kvp.Value);
                var fontDescriptor = await fontDict.FontDescriptor.GetAsync();

                if (fontDescriptor != null)
                {
                    ArrayObject? widthsArray = await fontDict.Widths.GetAsync();
                    Number? firstCharCode = await fontDict.FirstChar.GetAsync();

                    var widths = widthsArray
                        .Cast<Number>()
                        .Select((width, index) => new { width, index })
                        .ToDictionary(x => (char)(firstCharCode + x.index), x => (int)x.width);

                    simpleFontMetricsProvider.FontMetrics.TryAdd(
                        await fontDescriptor.FontName.GetAsync(),
                        await fontDescriptor.ToFontMetricsAsync(widths)
                        );
                }

                //fontStreams.Add(kvp.Key, await font.GetDecompressedDataAsync(_pdfEditor));
            }

            return fontProviders;
        }

        public static async Task<FontMetrics> ToFontMetricsAsync(
            this FontDescriptorDictionary fontDescriptor,
            Dictionary<char, int> widths
            )
        {
            var fontProperties = new FontProperties(await fontDescriptor.Flags.GetAsync());

            return new FontMetrics
            {
                Ascent = await fontDescriptor.Ascent.GetAsync(),
                Descent = await fontDescriptor.Descent.GetAsync(),
                StandardHorizontalWidth = fontDescriptor.StemH != null ? await fontDescriptor.StemH.GetAsync() : null,
                StandardVerticalWidth = fontDescriptor.StemV != null ? await fontDescriptor.StemV.GetAsync() : null,
                CapHeight = await fontDescriptor.CapHeight.GetAsync(),
                ItalicAngle = await fontDescriptor.ItalicAngle.GetAsync(),
                IsFixedPitch = fontProperties.IsFixedPitch,
                Widths = widths
            };
        }
    }
}
