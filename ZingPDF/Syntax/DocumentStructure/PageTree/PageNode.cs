using ZingPDF.Elements;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Syntax.DocumentStructure.PageTree
{
    public abstract class PageNode : Dictionary
    {
        public PageNode(Dictionary dictionary) : base(dictionary)
        {
        }

        /// <summary>
        /// Required.<para></para>
        /// The page tree node that is the immediate parent of this page object.
        /// Objects of Type Template shall have no Parent key.
        /// </summary>
        public IndirectObjectReference Parent => Get<IndirectObjectReference>(Constants.DictionaryKeys.PageTree.Parent)!;

        /// <summary>
        /// (Required; inheritable)<para></para>
        /// A dictionary containing any resources required by the page contents (see 7.8.3, "Resource dictionaries").
        /// If the page requires no resources, the value of this entry shall be an empty dictionary.
        /// Omitting the entry entirely indicates that the resources shall be inherited from an ancestor 
        /// node in the page tree, but PDF writers should not use this method of sharing resources as 
        /// described in 7.8.3, "Resource dictionaries".
        /// </summary>
        public IPdfObject? Resources => Get<IPdfObject>(Constants.DictionaryKeys.PageTree.Resources);

        /// <summary>
        /// The boundaries of the physical medium on which the page shall be displayed or printed.
        /// </summary>
        public Rectangle? MediaBox => Get<Rectangle>(Constants.DictionaryKeys.PageTree.MediaBox);

        /// <summary>
        /// <para>(Optional; Inheritable) A rectangle, expressed in default user space units, that 
        /// shall define the visible region of default user space. When the page is displayed or 
        /// printed, its contents shall be clipped (cropped) to this rectangle (see 14.11.2, "Page boundaries"). 
        /// Default value: the value of MediaBox.</para>
        /// <para>NOTE 1 This clipped page output will often be placed (imposed) on the output medium 
        /// in some implementation-defined manner.</para>
        /// </summary>
        public Rectangle? CropBox => Get<Rectangle>(Constants.DictionaryKeys.PageTree.CropBox);

        /// <summary>
        /// The number of degrees by which the page shall be rotated when displayed or printed.
        /// </summary>
        public Integer? Rotate => Get<Integer>(Constants.DictionaryKeys.PageTree.Rotate);

        public void SetParent(IndirectObjectReference parent)
        {
            ArgumentNullException.ThrowIfNull(parent, nameof(parent));

            Set(Constants.DictionaryKeys.PageTree.Parent, parent);
        }

        public void SetRotation(Rotation rotation)
        {
            ArgumentNullException.ThrowIfNull(rotation, nameof(rotation));

            Set<Integer>(Constants.DictionaryKeys.PageTree.Rotate, rotation);
        }

        public void SetResources(ResourceDictionary resourceDictionary)
        {
            ArgumentNullException.ThrowIfNull(resourceDictionary, nameof(resourceDictionary));

            Set(Constants.DictionaryKeys.PageTree.Resources, resourceDictionary);
        }

        public async Task AddXObjectResourceAsync(
            Name name,
            IndirectObjectReference reference,
            IIndirectObjectDictionary indirectObjectDictionary
            )
        {
            ArgumentNullException.ThrowIfNull(name, nameof(name));
            ArgumentNullException.ThrowIfNull(reference, nameof(reference));
            ArgumentNullException.ThrowIfNull(indirectObjectDictionary, nameof(indirectObjectDictionary));

            // Resources can be null, a ResourceDictionary, or an indirect object reference to a ResourceDictionary

            var resources = Resources ?? Empty;

            if (resources is IndirectObjectReference resourceRef)
            {
                var resourcesIndirectObject = await indirectObjectDictionary.GetAsync(resourceRef);
                var resourceDict = (ResourceDictionary)resourcesIndirectObject.Object;

                resourceDict.AddXObject(name, reference);
            }
            else if (resources is ResourceDictionary resourceDict)
            {
                resourceDict.AddXObject(name, reference);
            }
            else if (resources is Dictionary dict)
            {
                var editableResourceDict = new Dictionary<Name, IPdfObject>(dict)
                {
                    { name, reference }
                };

                Set(Constants.DictionaryKeys.PageTree.Resources, new ResourceDictionary(xObject: editableResourceDict));
            }
        }
    }
}
