using ZingPDF.DocumentInterchange.Metadata;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.Objects;

namespace ZingPDF.Graphics.FormXObjects
{
    internal class Type1FormDictionary : FormDictionary
    {
        private const int _formType = 1;

        public Type1FormDictionary(Rectangle bBox) : base(Subtypes.Form)
        {
            ArgumentNullException.ThrowIfNull(bBox);

            Set<Integer>(Constants.DictionaryKeys.Form.FormType, _formType);
            Set(Constants.DictionaryKeys.Form.Type1.BBox, bBox);
        }

        private Type1FormDictionary(Dictionary dictionary) : base(dictionary)
        {
        }

        /// <summary>
        /// (Required) An array of four numbers in the form coordinate system (see above), 
        /// giving the coordinates of the left, bottom, right, and top edges, respectively, 
        /// of the form XObject’s bounding box. These boundaries shall be used to clip the 
        /// form XObject and to determine its size for caching.
        /// </summary>
        public Rectangle BBox => Get<Rectangle>(Constants.DictionaryKeys.Form.Type1.BBox)!;

        /// <summary>
        /// (Optional) An array of six numbers specifying the form matrix, which maps form 
        /// space into user space (see 8.3.4, "Transformation matrices"). 
        /// Default value: the identity matrix [1 0 0 1 0 0].
        /// </summary>
        public ArrayObject? Matrix => Get<ArrayObject>(Constants.DictionaryKeys.Form.Type1.Matrix);

        /// <summary>
        /// <para>(Optional but strongly recommended; PDF 1.2) A dictionary specifying any resources 
        /// (such as fonts and images) required by the form XObject (see 7.8, "Content streams and resources").</para>
        /// <para>In a PDF whose version is 1.1 and earlier, all named resources used in the form 
        /// XObject shall be included in the resource dictionary of each page object on which the 
        /// form XObject appears, regardless of whether they also appear in the resource dictionary 
        /// of the form XObject. These resources should also be specified in the form XObject’s resource 
        /// dictionary as well, to determine which resources are used inside the form XObject. If a 
        /// resource is included in both dictionaries, it shall have the same name in both locations.</para>
        /// <para>In PDF 1.2 and later versions, form XObjects may be independent of the content streams 
        /// in which they appear, and this is strongly recommended although not required. In an independent 
        /// form XObject, the resource dictionary of the form XObject is required and shall contain all named 
        /// resources used by the form XObject. These resources shall not be promoted to the outer content 
        /// stream’s resource dictionary, although that stream’s resource dictionary refers to the form XObject.</para>
        /// </summary>
        public ResourceDictionary? Resources => Get<ResourceDictionary>(Constants.DictionaryKeys.Form.Type1.Resources);

        // TODO: group attributes dictionary type
        /// <summary>
        /// <para>(Optional; PDF 1.4) A group attributes dictionary indicating that the contents 
        /// of the form XObject shall be treated as a group and specifying the attributes of that 
        /// group (see 8.10.3, "Group XObjects").</para>
        /// <para>If a Ref entry (see below) is present, the group attributes shall also apply to 
        /// the external page imported by that entry, which allows such an imported page to be 
        /// treated as a group without further modification.</para>
        /// </summary>
        public Dictionary? Group => Get<Dictionary>(Constants.DictionaryKeys.Form.Type1.Group);

        // TODO: reference dictionary type
        /// <summary>
        /// (Optional; PDF 1.4) A reference dictionary identifying a page to be imported from 
        /// another PDF file, and for which the form XObject serves as a proxy (see 8.10.4, "Reference XObjects").
        /// </summary>
        public Dictionary? Ref => Get<Dictionary>(Constants.DictionaryKeys.Form.Type1.Ref);

        /// <summary>
        /// (Optional; PDF 1.4) A metadata stream containing metadata for the form XObject (see 14.3.2, "Metadata streams").
        /// </summary>
        public MetadataStream? Metadata => Get<MetadataStream>(Constants.DictionaryKeys.Form.Type1.Metadata);

        // TODO: page piece dictionary type
        /// <summary>
        /// (Optional; PDF 1.3) A page-piece dictionary associated with the form XObject (see 14.5, "Page-piece dictionaries").
        /// </summary>
        public Dictionary? PieceInfo => Get<Dictionary>(Constants.DictionaryKeys.Form.Type1.PieceInfo);

        /// <summary>
        /// (Required if PieceInfo is present; optional otherwise; PDF 1.3) The date and time (see 7.9.4, "Dates") 
        /// when the form XObject’s contents were most recently modified. If a page-piece dictionary (PieceInfo) 
        /// is present, the modification date shall be used to ascertain which of the application data dictionaries 
        /// it contains correspond to the current content of the form (see 14.5, "Page-piece dictionaries").
        /// </summary>
        public Date? LastModified => Get<Date>(Constants.DictionaryKeys.Form.Type1.LastModified);

        /// <summary>
        /// (Required if the form XObject is a structural content item; PDF 1.3) The integer key of the form 
        /// XObject’s entry in the structural parent tree (see 14.7.5.4, "Finding structure elements from content items").
        /// </summary>
        public Integer? StructParent => Get<Integer>(Constants.DictionaryKeys.Form.Type1.StructParent);

        /// <summary>
        /// <para>(Required if the form XObject contains marked-content sequences that are structural 
        /// content items; PDF 1.3) The integer key of the form XObject’s entry in the structural parent tree 
        /// (see 14.7.5.4, "Finding structure elements from content items").</para>
        /// <para>At most one of the entries StructParent or StructParents shall be present. 
        /// A form XObject shall be either a content item in its entirety or a container for marked-content 
        /// sequences that are content items, but not both.</para>
        /// </summary>
        public Integer? StructParents => Get<Integer>(Constants.DictionaryKeys.Form.Type1.StructParents);

        /// <summary>
        /// (Optional; PDF 1.2; deprecated in PDF 2.0) An OPI version dictionary for 
        /// the form XObject (see 14.11.7, "Open prepress interface (OPI)").
        /// </summary>
        public Dictionary? OPI => Get<Dictionary>(Constants.DictionaryKeys.Form.Type1.OPI);

        /// <summary>
        /// (Optional; PDF 1.5) An optional content group or optional content membership 
        /// dictionary (see 8.11, "Optional content") specifying the optional content properties 
        /// for the form XObject. Before the form is processed, its visibility shall be 
        /// determined based on this entry. If it is determined to be invisible, the entire 
        /// form shall be skipped, as if there were no Do operator to invoke it.
        /// </summary>
        public Dictionary? OC => Get<Dictionary>(Constants.DictionaryKeys.Form.Type1.OC);

        /// <summary>
        /// (Required in PDF 1.0; optional otherwise; deprecated in PDF 2.0) The name by which 
        /// this form XObject is referenced in the XObject subdictionary of the current resource 
        /// dictionary (see 7.8.3, "Resource dictionaries").
        /// </summary>
        public Name? Name => Get<Name>(Constants.DictionaryKeys.Form.Type1.Name);

        // TODO: file specification dictionary type
        /// <summary>
        /// (Optional; PDF 2.0) An array of one or more file specification dictionaries 
        /// (7.11.3, "File specification dictionaries") which denote the associated files 
        /// for this form XObject. See 14.13, "Associated files" and 
        /// 14.13.7, "Associated files linked to XObjects" for more details.
        /// </summary>
        public ArrayObject? AF => Get<ArrayObject>(Constants.DictionaryKeys.Form.Type1.AF);

        // TODO: measure dictionary
        /// <summary>
        /// (Optional; PDF 2.0) A measure dictionary (see "Table 266 — Entries in a measure dictionary") 
        /// that specifies the scale and units which shall apply to the form.
        /// </summary>
        public Dictionary? Measure => Get<Dictionary>(Constants.DictionaryKeys.Form.Type1.Measure);
    }
}
