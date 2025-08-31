using ZingPDF.Elements;
using ZingPDF.Parsing.Parsers.Objects.Dictionaries;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.Dictionaries.PropertyWrappers;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Syntax.DocumentStructure.PageTree
{
    public abstract class PageNode : Dictionary
    {
        public PageNode(Dictionary dictionary)
        : base(dictionary) { }

        public PageNode(Dictionary<string, IPdfObject> dictionary, IPdf pdf, ObjectContext context)
            : base(dictionary, pdf, context) { } 

        /// <summary>
        /// (Required except in root node; not permitted in the root node; shall be an indirect reference) The page tree node that is the immediate parent of this one.
        /// </summary>
        public OptionalProperty<PageTreeNodeDictionary> Parent => GetOptionalProperty<PageTreeNodeDictionary>(Constants.DictionaryKeys.Parent);

        /// <summary>
        /// <para>
        /// (Required; inheritable) A dictionary containing any resources required by the page contents (see 7.8.3, "Resource dictionaries").
        /// If the page requires no resources, the value of this entry shall be an empty dictionary.
        /// Omitting the entry entirely indicates that the resources shall be inherited from an ancestor 
        /// node in the page tree, but PDF writers should not use this method of sharing resources as 
        /// described in 7.8.3, "Resource dictionaries".
        /// </para>
        /// </summary>
        [Inheritable]
        public RequiredProperty<Dictionary> Resources => GetRequiredProperty<Dictionary>(Constants.DictionaryKeys.PageTree.Resources);

        /// <summary>
        /// (Required; inheritable) A rectangle (see 7.9.5, "Rectangles"), expressed in default user space units, that shall define the boundaries 
        /// of the physical medium on which the page shall be displayed or printed (see 14.11.2, "Page boundaries").
        /// </summary>
        [Inheritable]
        public RequiredProperty<Rectangle> MediaBox => GetRequiredProperty<Rectangle>(Constants.DictionaryKeys.PageTree.MediaBox);

        /// <summary>
        /// <para>
        /// (Optional; Inheritable) A rectangle, expressed in default user space units, that 
        /// shall define the visible region of default user space. When the page is displayed or 
        /// printed, its contents shall be clipped (cropped) to this rectangle (see 14.11.2, "Page boundaries"). 
        /// Default value: the value of MediaBox.
        /// </para>
        /// <para>
        /// NOTE 1 This clipped page output will often be placed (imposed) on the output medium 
        /// in some implementation-defined manner.
        /// </para>
        /// </summary>
        [Inheritable]
        public OptionalProperty<Rectangle> CropBox => GetOptionalProperty<Rectangle>(Constants.DictionaryKeys.PageTree.CropBox);

        /// <summary>
        /// (Optional; inheritable) The number of degrees by which the page shall be rotated clockwise when displayed or printed. 
        /// The value shall be a multiple of 90. Default value: 0.
        /// </summary>
        [Inheritable]
        public OptionalProperty<Number> Rotate => GetOptionalProperty<Number>(Constants.DictionaryKeys.PageTree.Rotate);

        public void SetParent(IndirectObjectReference parent)
        {
            ArgumentNullException.ThrowIfNull(parent, nameof(parent));

            Set(Constants.DictionaryKeys.Parent, parent);
        }

        public void SetRotation(Rotation rotation)
        {
            ArgumentNullException.ThrowIfNull(rotation, nameof(rotation));

            Set<Number>(Constants.DictionaryKeys.PageTree.Rotate, rotation.Value);
        }

        public void SetResources(ResourceDictionary resourceDictionary)
        {
            ArgumentNullException.ThrowIfNull(resourceDictionary, nameof(resourceDictionary));

            Set(Constants.DictionaryKeys.PageTree.Resources, resourceDictionary);
        }

        public async Task AddXObjectResourceAsync(
            string name,
            IndirectObjectReference reference,
            IPdf pdf
            )
        {
            ArgumentNullException.ThrowIfNull(name, nameof(name));
            ArgumentNullException.ThrowIfNull(reference, nameof(reference));
            ArgumentNullException.ThrowIfNull(pdf, nameof(pdf));

            var resources = await Resources.GetAsync();
            if (resources == null)
            {
                Set(
                    Constants.DictionaryKeys.PageTree.Resources,
                    new ResourceDictionary(pdf, ObjectContext.UserCreated, xObject: new Dictionary<string, IPdfObject>() { { name, reference } })
                    );

                return;
            }

            await ResourceDictionary.FromDictionary(resources).AddXObjectAsync(name, reference, pdf);
        }
    }
}
