using ZingPDF.DocumentInterchange.Metadata;
using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.Dictionaries.PropertyWrappers;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;
using ZingPDF.Syntax.Objects.Strings;

namespace ZingPDF.Graphics.Images
{
    /// <summary>
    /// ISO 32000-2:2020 8.9.5 - Image dictionaries
    /// </summary>
    public class ImageDictionary : XObjectDictionary
    {
        public ImageDictionary(Dictionary dict)
            : base(dict) { }

        private ImageDictionary(Dictionary<string, IPdfObject> dict, IPdf pdf, ObjectContext context)
            : base(dict, pdf, context) { }

        public ImageDictionary(
            IPdf pdf,
            ObjectContext context,
            int width,
            int height,
            string? ColorSpace,
            int? bitsPerComponent,
            ShorthandArrayObject? filters,
            ShorthandArrayObject? decodeParms
            )
            : this(
                new Dictionary<string, IPdfObject>
                {
                    [Constants.DictionaryKeys.Subtype] = (Name)Subtypes.Image
                },
                pdf,
                context
              )
        {

            Set<Number>(Constants.DictionaryKeys.Image.Width, width);
            Set<Number>(Constants.DictionaryKeys.Image.Height, height);

            if (!string.IsNullOrWhiteSpace(ColorSpace))
            {
                Set<Name>(Constants.DictionaryKeys.Image.ColorSpace, ColorSpace);
            }

            if (bitsPerComponent.HasValue)
            {
                Set<Number>(Constants.DictionaryKeys.Image.BitsPerComponent, bitsPerComponent.Value);
            }

            if (filters?.Any() ?? false)
            {
                Set(Constants.DictionaryKeys.Stream.Filter, filters);

                if (decodeParms?.Any() ?? false)
                {
                    Set(Constants.DictionaryKeys.Stream.DecodeParms, decodeParms);
                } 
            }
        }

        /// <summary>
        /// (Required) The width of the image, in samples.
        /// </summary>
        public RequiredProperty<Number> Width => GetRequiredProperty<Number>(Constants.DictionaryKeys.Image.Width);

        /// <summary>
        /// (Required) The height of the image, in samples.
        /// </summary>
        public RequiredProperty<Number> Height => GetRequiredProperty<Number>(Constants.DictionaryKeys.Image.Height);

        /// <summary>
        /// <para>(Required for images, except those that use the JPXDecode filter; not permitted for image masks) 
        /// The colour space in which image samples shall be specified; it can be any type of colour space except Pattern.</para>
        /// <para>If the image uses the JPXDecode filter, this entry may be present:</para>
        /// <para>• If ColorSpace is present, any colour space specifications in the JPEG 2000 data shall be ignored.</para>
        /// <para>• If ColorSpace is absent, the colour space specifications in the JPEG 2000 data shall be used. 
        /// The Decode array shall also be ignored unless ImageMask is true.</para>
        /// </summary>
        public OptionalProperty<IPdfObject> ColorSpace => GetOptionalProperty<IPdfObject>(Constants.DictionaryKeys.Image.ColorSpace);

        /// <summary>
        /// <para>(Required except for image masks and images that use the JPXDecode filter) The number of bits 
        /// used to represent each colour component. Only a single value shall be specified; the number of bits 
        /// shall be the same for all colour components. The value shall be 1, 2, 4, 8, or (from PDF 1.5) 16. 
        /// If ImageMask is true, this entry is optional, but if specified, its value shall be 1.</para>
        /// <para>If the image stream uses a filter, the value of BitsPerComponent shall be consistent with the 
        /// size of the data samples that the filter delivers. In particular, a CCITTFaxDecode or JBIG2Decode 
        /// filter shall always deliver 1-bit samples, a RunLengthDecode or DCTDecode filter shall always 
        /// deliver 8-bit samples, and an LZWDecode or FlateDecode filter shall deliver samples of a specified 
        /// size if a predictor function is used.</para>
        /// <para>If the image stream uses the JPXDecode filter, this entry is optional and shall be ignored if
        /// present. The bit depth is determined by the PDF processor in the process of decoding the JPEG 2000 image.</para>
        /// </summary>
        public OptionalProperty<Number> BitsPerComponent => GetOptionalProperty<Number>(Constants.DictionaryKeys.Image.BitsPerComponent);

        /// <summary>
        /// (Optional; PDF 1.1) The name of a colour rendering intent that shall be used in rendering any image 
        /// that is not an image mask (see 8.6.5.8, "Rendering intents"). This value is ignored if ImageMask is true. 
        /// Default value: the current rendering intent in the graphics state.
        /// </summary>
        public OptionalProperty<Name> Intent => GetOptionalProperty<Name>(Constants.DictionaryKeys.Image.Intent);

        /// <summary>
        /// (Optional) A flag indicating whether the image shall be treated as an image mask (see 8.9.6, "Masked images"). 
        /// If this flag is true, the value of BitsPerComponent, if present, shall be 1 and Mask and ColorSpace shall 
        /// not be specified; unmasked areas shall be painted using the current nonstroking colour. Default value: false.
        /// </summary>
        public OptionalProperty<BooleanObject> ImageMask => GetOptionalProperty<BooleanObject>(Constants.DictionaryKeys.Image.ImageMask);

        /// <summary>
        /// (Optional; shall not be present for image masks; PDF 1.3) An image XObject defining an image mask to be 
        /// applied to this image (see 8.9.6.3, "Explicit masking"), or an array specifying a range of colours to 
        /// be applied to it as a colour key mask (see 8.9.6.4, "Colour key masking"). If ImageMask is true, this 
        /// entry shall not be present.
        /// </summary>
        public OptionalProperty<IPdfObject> Mask => GetOptionalProperty<IPdfObject>(Constants.DictionaryKeys.Image.Mask);

        /// <summary>
        /// (Optional) An array of numbers describing how to map image samples into the range of values appropriate 
        /// for the image’s colour space (see 8.9.5.2, "Decode arrays"). If ImageMask is true, the array shall be 
        /// either [0 1] or [1 0]; otherwise, its length shall be twice the number of colour components required by 
        /// ColorSpace. If the image uses the JPXDecode filter and if ColorSpace is absent, the Decode array shall 
        /// be ignored unless ImageMask is true.
        /// Default value: see "Table 88 — Default decode arrays".
        /// </summary>
        public OptionalProperty<ArrayObject> Decode => GetOptionalProperty<ArrayObject>(Constants.DictionaryKeys.Image.Decode);

        /// <summary>
        /// (Optional) A flag indicating whether image interpolation should be performed by a PDF processor 
        /// (see 8.9.5.3, "Image interpolation"). Default value: false.
        /// </summary>
        public OptionalProperty<BooleanObject> Interpolate => GetOptionalProperty<BooleanObject>(Constants.DictionaryKeys.Image.Interpolate);

        /// <summary>
        /// (Optional; PDF 1.3) An array of alternate image dictionaries for this image (see 8.9.5.4, "Alternate images"). 
        /// This entry shall not be present in an image XObject that is itself an alternate image.
        /// 
        /// <para>Additional limitations also apply to this key when used in soft-mask image dictionaries - see clause 
        /// 11.6.5.2 Soft-mask images.</para>
        /// </summary>
        public OptionalProperty<ArrayObject> Alternates => GetOptionalProperty<ArrayObject>(Constants.DictionaryKeys.Image.Alternates);

        /// <summary>
        /// <para>(Optional; PDF 1.4) A subsidiary image XObject defining a soft-mask image (see 11.6.5.2, "Soft-mask images") 
        /// that shall be used as a source of mask shape or mask opacity values in the transparent imaging model. The alpha 
        /// source parameter in the graphics state determines whether the mask values shall be interpreted as shape or opacity.</para>
        /// <para>If present, this entry shall override the current soft mask in the graphics state, as well as the image’s 
        /// Mask entry, if any. However, the other transparency-related graphics state parameters — blend mode and alpha 
        /// constant — shall remain in effect. If SMask is absent and SMaskInData has value 0, the image shall have no 
        /// associated soft mask (although the current soft mask in the graphics state may still apply).</para>
        /// <para>NOTE 1 Interactions between SMask, SMaskInData and the current soft mask in the graphics state are set out 
        /// in clause 11.6.4.3, "Mask shape and opacity".</para>
        /// <para>Additional limitations also apply to this key when used in soft-mask image dictionaries - see clause 
        /// 11.6.5.2 Soft-mask images.</para>
        /// </summary>
        public OptionalProperty<IndirectObjectReference> SMask => GetOptionalProperty<IndirectObjectReference>(Constants.DictionaryKeys.Image.SMask);

        /// <summary>
        /// <para>(Optional for images that use the JPXDecode filter, meaningless otherwise; PDF 1.5) A code specifying 
        /// how soft-mask information (see 11.6.5.2, "Soft-mask images") encoded with image samples shall be used:</para>
        /// <para>0 If present, encoded soft-mask image information shall be ignored.</para>
        /// <para>1 The image’s data stream includes encoded soft-mask values. A PDF processor shall create a soft-mask 
        /// image from the information to be used as a source of mask shape or mask opacity in the transparency imaging 
        /// model.</para>
        /// <para>2 The image’s data stream includes colour channels that have been premultiplied with an opacity channel; 
        /// the image data also includes the opacity channel. A PDF processor shall create a soft-mask image from the 
        /// opacity channel information to be used as a source of mask shape or mask opacity in the transparency model.</para>
        /// <para>If this entry has a non-zero value, SMask shall not be specified. See also 7.4.9, "JPXDecode filter".</para>
        /// <para>NOTE 2 Interactions between SMask, SMaskInData and the current soft mask in the graphics state are set out in clause 11.6.4.3, "Mask shape and opacity".</para>
        /// <para>Default value: 0.</para>
        /// </summary>
        public OptionalProperty<Number> SMaskInData => GetOptionalProperty<Number>(Constants.DictionaryKeys.Image.SMaskInData);

        /// <summary>
        /// (Required in PDF 1.0; optional otherwise; deprecated in PDF 2.0) The name by which this image XObject is 
        /// referenced in the XObject subdictionary of the current resource dictionary (see 7.8.3, "Resource dictionaries").
        /// </summary>
        public OptionalProperty<Name> Name => GetOptionalProperty<Name>(Constants.DictionaryKeys.Image.Name);

        /// <summary>
        /// (Required if the image is a structural content item; PDF 1.3) The integer key of the image’s entry in the 
        /// structural parent tree (see 14.7.5.4, "Finding structure elements from content items").
        /// 
        /// <para>Additional limitations also apply to this key when used in soft-mask image dictionaries - see clause 
        /// 11.6.5.2 Soft-mask images.</para>
        /// </summary>
        public OptionalProperty<Number> StructParent => GetOptionalProperty<Number>(Constants.DictionaryKeys.Image.StructParent);

        /// <summary>
        /// (Optional; PDF 1.3; indirect reference preferred) The digital identifier of the image’s parent Web Capture 
        /// content set (see 14.10.6, "Object attributes related to web capture").
        /// 
        /// <para>Additional limitations also apply to this key when used in soft-mask image dictionaries - see clause 
        /// 11.6.5.2 Soft-mask images.</para>
        /// </summary>
        public OptionalProperty<HexadecimalString> ID => GetOptionalProperty<HexadecimalString>(Constants.DictionaryKeys.Image.ID);

        /// <summary>
        /// (Optional; PDF 1.2; deprecated in PDF 2.0) An OPI version dictionary for the image; see 14.11.7, 
        /// "Open prepress interface (OPI)". If ImageMask is true, this entry shall be ignored.
        /// 
        /// <para>Additional limitations also apply to this key when used in soft-mask image dictionaries - see clause 
        /// 11.6.5.2 Soft-mask images.</para>
        /// </summary>
        public OptionalProperty<Dictionary> OPI => GetOptionalProperty<Dictionary>(Constants.DictionaryKeys.Image.OPI);

        /// <summary>
        /// (Optional; PDF 1.4) A metadata stream containing metadata for the image (see 14.3.2, "Metadata streams").
        /// </summary>
        public OptionalProperty<StreamObject<MetadataStreamDictionary>> Metadata
            => GetOptionalProperty<StreamObject<MetadataStreamDictionary>>(Constants.DictionaryKeys.Image.Metadata);

        /// <summary>
        /// (Optional; PDF 1.5) An optional content group or optional content membership dictionary 
        /// (see 8.11, "Optional content"), specifying the optional content properties for this image XObject. 
        /// Before the image is processed by a PDF processor, its visibility shall be determined based on 
        /// this entry. If it is determined to be invisible, the entire image shall be skipped, as if there 
        /// were no Do operator to invoke it.
        /// </summary>
        public OptionalProperty<Dictionary> OC => GetOptionalProperty<Dictionary>(Constants.DictionaryKeys.Image.OC);

        /// <summary>
        /// (Optional; PDF 2.0) An array of one or more file specification dictionaries 
        /// (7.11.3, "File specification dictionaries") which denote the associated files for this image XObject. 
        /// See 14.13, "Associated files" and 14.13.7, "Associated files linked to XObjects" for more details.
        /// </summary>
        public OptionalProperty<ArrayObject> AF => GetOptionalProperty<ArrayObject>(Constants.DictionaryKeys.Image.AF);

        /// <summary>
        /// (Optional; PDF 2.0) A measure dictionary (see "Table 266 — Entries in a measure dictionary") 
        /// that specifies the scale and units which shall apply to the image.
        /// </summary>
        public OptionalProperty<Dictionary> Measure => GetOptionalProperty<Dictionary>(Constants.DictionaryKeys.Image.Measure);

        /// <summary>
        /// (Optional; PDF 2.0) A point data dictionary (see "Table 272 — Entries in a point data dictionary") 
        /// that specifies the extended geospatial data that shall apply to the image.
        /// </summary>
        public OptionalProperty<Dictionary> PtData => GetOptionalProperty<Dictionary>(Constants.DictionaryKeys.Image.PtData);

        new public static ImageDictionary FromDictionary(Dictionary<string, IPdfObject> dict, IPdf pdf, ObjectContext context)
        {
            if (
                !dict.TryGetValue(Constants.DictionaryKeys.Type, out IPdfObject? type)
                || (Name)type != Constants.DictionaryTypes.XObject
                || !dict.TryGetValue(Constants.DictionaryKeys.Subtype, out IPdfObject? subtype)
                || (Name)subtype != Subtypes.Image
                )
            {
                throw new ArgumentException("Supplied argument is not a type 1 form dictionary.", nameof(dict));
            }

            return new(dict, pdf, context);
        }
    }
}
