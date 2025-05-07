using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF.Syntax.FileStructure.ObjectStreams
{
    internal class ObjectStreamDictionary : StreamDictionary
    {
        public ObjectStreamDictionary(Dictionary objectStreamDictionary)
            : base(objectStreamDictionary) { }

        private ObjectStreamDictionary(Dictionary<string, IPdfObject> objectStreamDictionary, IPdfContext pdfContext, ObjectOrigin objectOrigin)
            : base(objectStreamDictionary, pdfContext, objectOrigin) { }

        /// <summary>
        /// (Required) The number of indirect objects stored in the stream.
        /// </summary>
        public Number N => GetAs<Number>(Constants.DictionaryKeys.ObjectStream.N)!;

        /// <summary>
        /// (Required) The byte offset in the decoded stream of the first compressed object.
        /// </summary>
        public Number First => GetAs<Number>(Constants.DictionaryKeys.ObjectStream.First)!;

        /// <summary>
        /// (Optional) A reference to another object stream, of which the current object stream is an extension. 
        /// Both streams are considered part of a collection of object streams (see below). A given collection 
        /// consists of a set of streams whose Extends links form a directed acyclic graph.
        /// </summary>
        public IndirectObjectReference? Extends => GetAs<IndirectObjectReference>(Constants.DictionaryKeys.ObjectStream.Extends);

        new public static ObjectStreamDictionary FromDictionary(Dictionary<string, IPdfObject> objectStreamDictionary, IPdfContext pdfContext, ObjectOrigin objectOrigin)
        {
            ArgumentNullException.ThrowIfNull(objectStreamDictionary);

            if (!objectStreamDictionary.TryGetValue(Constants.DictionaryKeys.Type, out IPdfObject? type) || (Name)type != Constants.DictionaryTypes.ObjStm)
            {
                throw new ArgumentException("Supplied argument is not a cross reference stream dictionary.", nameof(objectStreamDictionary));
            }

            return new(objectStreamDictionary, pdfContext, objectOrigin);
        }
    }
}
