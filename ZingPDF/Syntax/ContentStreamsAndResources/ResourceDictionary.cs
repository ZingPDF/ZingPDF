using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.Dictionaries.PropertyWrappers;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Syntax.ContentStreamsAndResources
{
    /// <summary>
    /// ISO 32000-2:2020 7.8.3 - Resource dictionaries
    /// </summary>
    public class ResourceDictionary : Dictionary
    {
        public ResourceDictionary(Dictionary resourceDictionary)
            : base(resourceDictionary) { }

        public ResourceDictionary(
            IPdf pdf,
            ObjectOrigin objectOrigin,
            Dictionary<string, IPdfObject>? extGState = null,
            Dictionary<string, IPdfObject>? colorSpace = null,
            Dictionary<string, IPdfObject>? pattern = null,
            Dictionary<string, IPdfObject>? shading = null,
            Dictionary<string, IPdfObject>? xObject = null,
            Dictionary<string, IPdfObject>? font = null,
            Dictionary<string, IPdfObject>? procSet = null,
            Dictionary<string, IPdfObject>? properties = null
            )
            : base(pdf, objectOrigin)
        {
            if (extGState is not null) Set(Constants.DictionaryKeys.Resource.ExtGState, new Dictionary(extGState, pdf, objectOrigin));
            if (colorSpace is not null) Set(Constants.DictionaryKeys.Resource.ColorSpace, new Dictionary(colorSpace, pdf, objectOrigin));
            if (pattern is not null) Set(Constants.DictionaryKeys.Resource.Pattern, new Dictionary(pattern, pdf, objectOrigin));
            if (shading is not null) Set(Constants.DictionaryKeys.Resource.Shading, new Dictionary(shading, pdf, objectOrigin));
            if (xObject is not null) Set(Constants.DictionaryKeys.Resource.XObject, new Dictionary(xObject, pdf, objectOrigin));
            if (font is not null) Set(Constants.DictionaryKeys.Resource.Font, new Dictionary(font, pdf, objectOrigin));
            if (procSet is not null) Set(Constants.DictionaryKeys.Resource.ProcSet, new Dictionary(procSet, pdf, objectOrigin));
            if (properties is not null) Set(Constants.DictionaryKeys.Resource.Properties, new Dictionary(properties, pdf, objectOrigin));
        }

        public ResourceDictionary(Dictionary<string, IPdfObject> dict, IPdf pdf, ObjectOrigin objectOrigin)
            : base(dict, pdf, objectOrigin) { }

        /// <summary>
        /// <para>(Optional)</para>
        /// <para>A dictionary that maps resource names to graphics state parameter 
        /// dictionaries (see 8.4.5, "Graphics state parameter dictionaries").</para>
        /// </summary>
        public OptionalProperty<Dictionary> ExtGState => GetOptionalProperty<Dictionary>(Constants.DictionaryKeys.Resource.ExtGState);

        /// <summary>
        /// <para>(Optional)</para>
        /// <para>A dictionary that maps each resource name to either the name of a device-dependent 
        /// colour space or an array describing a colour space (see 8.6, "Colour spaces").</para>
        /// </summary>
        public OptionalProperty<Dictionary> ColorSpace => GetOptionalProperty<Dictionary>(Constants.DictionaryKeys.Resource.ColorSpace);

        /// <summary>
        /// <para>(Optional)</para>
        /// <para>A dictionary that maps resource names to pattern objects (see 8.7, "Patterns").</para>
        /// </summary>
        public OptionalProperty<Dictionary> Pattern => GetOptionalProperty<Dictionary>(Constants.DictionaryKeys.Resource.Pattern);

        /// <summary>
        /// <para>(Optional; PDF 1.3)</para>
        /// <para>A dictionary that maps resource names to shading dictionaries (see 8.7.4.3, "Shading dictionaries").</para>
        /// </summary>
        public OptionalProperty<Dictionary> Shading => GetOptionalProperty<Dictionary>(Constants.DictionaryKeys.Resource.Shading);

        /// <summary>
        /// <para>(Optional)</para>
        /// <para>A dictionary that maps resource names to external objects (see 8.8, "External objects").</para>
        /// </summary>
        public OptionalProperty<Dictionary> XObject => GetOptionalProperty<Dictionary>(Constants.DictionaryKeys.Resource.XObject);

        /// <summary>
        /// <para>(Optional)</para>
        /// <para>A dictionary that maps resource names to font dictionaries (see 9, "Text").</para>
        /// </summary>
        public OptionalProperty<Dictionary> Font => GetOptionalProperty<Dictionary>(Constants.DictionaryKeys.Resource.Font);

        /// <summary>
        /// <para>(Optional; deprecated in PDF 2.0)</para>
        /// <para>An array of predefined procedure set names (see 14.2, "Procedure sets").</para>
        /// </summary>
        public OptionalProperty<ArrayObject> ProcSet => GetOptionalProperty<ArrayObject>(Constants.DictionaryKeys.Resource.ProcSet);

        /// <summary>
        /// <para>(Optional; PDF 1.2)</para>
        /// <para>A dictionary that maps resource names to property list dictionaries for 
        /// marked-content (see 14.6.2, "Property lists").</para>
        /// </summary>
        public OptionalProperty<Dictionary> Properties => GetOptionalProperty<Dictionary>(Constants.DictionaryKeys.Resource.Properties);

        public async Task AddXObjectAsync(Name name, IndirectObjectReference xObjectReference, IPdf pdf)
        {
            ArgumentNullException.ThrowIfNull(name, nameof(name));
            ArgumentNullException.ThrowIfNull(xObjectReference, nameof(xObjectReference));
            ArgumentNullException.ThrowIfNull(pdf, nameof(pdf));

            Set(
                Constants.DictionaryKeys.Resource.XObject,
                await AddRefToSubDictionaryAsync(XObject, name, xObjectReference, pdf)
                );
        }

        public async Task AddFontAsync(Name name, IndirectObjectReference fontReference, IPdf pdf)
        {
            ArgumentNullException.ThrowIfNull(name, nameof(name));
            ArgumentNullException.ThrowIfNull(fontReference, nameof(fontReference));
            ArgumentNullException.ThrowIfNull(pdf, nameof(pdf));

            Set(
                Constants.DictionaryKeys.Resource.Font,
                await AddRefToSubDictionaryAsync(Font, name, fontReference, pdf)
                );
        }

        public static ResourceDictionary FromDictionary(
            Dictionary<string, IPdfObject> resourceDictionary,
            IPdf pdf,
            ObjectOrigin objectOrigin
            )
        {
            return resourceDictionary is null
                ? throw new ArgumentNullException(nameof(resourceDictionary))
                : new(resourceDictionary, pdf, objectOrigin);
        }

        public static ResourceDictionary FromDictionary(Dictionary resourceDictionary)
        {
            return resourceDictionary is null
                ? throw new ArgumentNullException(nameof(resourceDictionary))
                : new(resourceDictionary);
        }

        private static async Task<Dictionary> AddRefToSubDictionaryAsync(
            OptionalProperty<Dictionary> dictionaryProperty,
            Name name,
            IndirectObjectReference reference,
            IPdf pdf
            )
        {
            var dict = await dictionaryProperty.GetAsync()
                ?? new Dictionary(pdf, ObjectOrigin.UserCreated);

            dict.Set(name, reference);

            return dict;
        }
    }
}
