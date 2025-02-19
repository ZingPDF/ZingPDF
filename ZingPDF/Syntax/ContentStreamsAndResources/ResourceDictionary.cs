using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Syntax.ContentStreamsAndResources
{
    /// <summary>
    /// ISO 32000-2:2020 7.8.3 - Resource dictionaries
    /// </summary>
    public class ResourceDictionary : Dictionary
    {
        public ResourceDictionary(
            Dictionary? extGState = null,
            Dictionary? colorSpace = null,
            Dictionary? pattern = null,
            Dictionary? shading = null,
            Dictionary? xObject = null,
            Dictionary? font = null,
            Dictionary? procSet = null,
            Dictionary? properties = null
            )
            : base((Name?)null)
        {
            if (extGState is not null) Set(Constants.DictionaryKeys.Resource.ExtGState, extGState);
            if (colorSpace is not null) Set(Constants.DictionaryKeys.Resource.ColorSpace, colorSpace);
            if (pattern is not null) Set(Constants.DictionaryKeys.Resource.Pattern, pattern);
            if (shading is not null) Set(Constants.DictionaryKeys.Resource.Shading, shading);
            if (xObject is not null) Set(Constants.DictionaryKeys.Resource.XObject, xObject);
            if (font is not null) Set(Constants.DictionaryKeys.Resource.Font, font);
            if (procSet is not null) Set(Constants.DictionaryKeys.Resource.ProcSet, procSet);
            if (properties is not null) Set(Constants.DictionaryKeys.Resource.Properties, properties);
        }

        private ResourceDictionary(Dictionary dict) : base(dict) { }

        /// <summary>
        /// <para>(Optional)</para>
        /// <para>A dictionary that maps resource names to graphics state parameter 
        /// dictionaries (see 8.4.5, "Graphics state parameter dictionaries").</para>
        /// </summary>
        public AsyncProperty<Dictionary>? ExtGState => Get<Dictionary>(Constants.DictionaryKeys.Resource.ExtGState);

        /// <summary>
        /// <para>(Optional)</para>
        /// <para>A dictionary that maps each resource name to either the name of a device-dependent 
        /// colour space or an array describing a colour space (see 8.6, "Colour spaces").</para>
        /// </summary>
        public AsyncProperty<Dictionary>? ColorSpace => Get<Dictionary>(Constants.DictionaryKeys.Resource.ColorSpace);

        /// <summary>
        /// <para>(Optional)</para>
        /// <para>A dictionary that maps resource names to pattern objects (see 8.7, "Patterns").</para>
        /// </summary>
        public AsyncProperty<Dictionary>? Pattern => Get<Dictionary>(Constants.DictionaryKeys.Resource.Pattern);

        /// <summary>
        /// <para>(Optional; PDF 1.3)</para>
        /// <para>A dictionary that maps resource names to shading dictionaries (see 8.7.4.3, "Shading dictionaries").</para>
        /// </summary>
        public AsyncProperty<Dictionary>? Shading => Get<Dictionary>(Constants.DictionaryKeys.Resource.Shading);

        /// <summary>
        /// <para>(Optional)</para>
        /// <para>A dictionary that maps resource names to external objects (see 8.8, "External objects").</para>
        /// </summary>
        public AsyncProperty<Dictionary>? XObject => Get<Dictionary>(Constants.DictionaryKeys.Resource.XObject);

        /// <summary>
        /// <para>(Optional)</para>
        /// <para>A dictionary that maps resource names to font dictionaries (see 9, "Text").</para>
        /// </summary>
        public AsyncProperty<Dictionary>? Font => Get<Dictionary>(Constants.DictionaryKeys.Resource.Font);

        /// <summary>
        /// <para>(Optional; deprecated in PDF 2.0)</para>
        /// <para>An array of predefined procedure set names (see 14.2, "Procedure sets").</para>
        /// </summary>
        public AsyncProperty<ArrayObject>? ProcSet => Get<ArrayObject>(Constants.DictionaryKeys.Resource.ProcSet);

        /// <summary>
        /// <para>(Optional; PDF 1.2)</para>
        /// <para>A dictionary that maps resource names to property list dictionaries for 
        /// marked-content (see 14.6.2, "Property lists").</para>
        /// </summary>
        public AsyncProperty<Dictionary>? Properties => Get<Dictionary>(Constants.DictionaryKeys.Resource.Properties);

        public async Task AddXObjectAsync(Name name, IndirectObjectReference xObjectReference, IIndirectObjectDictionary indirectObjectDictionary)
        {
            ArgumentNullException.ThrowIfNull(name, nameof(name));
            ArgumentNullException.ThrowIfNull(xObjectReference, nameof(xObjectReference));
            ArgumentNullException.ThrowIfNull(indirectObjectDictionary, nameof(indirectObjectDictionary));

            Set(
                Constants.DictionaryKeys.Resource.XObject,
                await AddRefToSubDictionaryAsync(XObject, name, xObjectReference, indirectObjectDictionary)
                );
        }

        public async Task AddFontAsync(Name name, IndirectObjectReference fontReference, IIndirectObjectDictionary indirectObjectDictionary)
        {
            ArgumentNullException.ThrowIfNull(name, nameof(name));
            ArgumentNullException.ThrowIfNull(fontReference, nameof(fontReference));
            ArgumentNullException.ThrowIfNull(indirectObjectDictionary, nameof(indirectObjectDictionary));

            Set(
                Constants.DictionaryKeys.Resource.Font,
                await AddRefToSubDictionaryAsync(Font, name, fontReference, indirectObjectDictionary)
                );
        }

        public static ResourceDictionary FromDictionary(Dictionary resourceDictionary)
        {
            return resourceDictionary is null
                ? throw new ArgumentNullException(nameof(resourceDictionary))
                : new(resourceDictionary);
        }

        private static async Task<Dictionary> AddRefToSubDictionaryAsync(
            AsyncProperty<Dictionary>? dictionaryProperty,
            Name name,
            IndirectObjectReference reference,
            IIndirectObjectDictionary indirectObjectDictionary
            )
        {
            Dictionary dict = Empty;

            if (dictionaryProperty != null)
            {
                dict = await dictionaryProperty.GetAsync(indirectObjectDictionary);
            }

            dict[name] = reference;

            return dict;
        }
    }
}
