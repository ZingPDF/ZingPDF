using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF.Syntax.FileStructure.ObjectStreams
{
    internal class ObjectStreamDictionary : StreamDictionary
    {
        private ObjectStreamDictionary(Dictionary objectStreamDictionary) : base(objectStreamDictionary) { }

        /// <summary>
        /// The number of indirect objects stored in the stream.
        /// </summary>
        public Integer N { get => Get<Integer>(Constants.DictionaryKeys.ObjectStream.N)!; }

        /// <summary>
        /// The byte offset in the decoded stream of the first compressed object.
        /// </summary>
        public Integer First { get => Get<Integer>(Constants.DictionaryKeys.ObjectStream.First)!; }

        /// <summary>
        /// reference to another object stream, of which the current object stream is an extension.
        /// Both streams are considered part of a collection of object streams (see below).
        /// A given collection consists of a set of streams whose Extends links form a directed acyclic graph.
        /// </summary>
        public IndirectObjectReference? Extends { get => Get<IndirectObjectReference>(Constants.DictionaryKeys.ObjectStream.Extends); }

        new public static ObjectStreamDictionary FromDictionary(Dictionary objectStreamDictionary)
        {
            ArgumentNullException.ThrowIfNull(objectStreamDictionary);

            if (!objectStreamDictionary.TryGetValue(Constants.DictionaryKeys.Type, out IPdfObject? type) || (Name)type != Constants.DictionaryTypes.ObjStm)
            {
                throw new ArgumentException("Supplied argument is not a cross reference stream dictionary.", nameof(objectStreamDictionary));
            }

            return new(objectStreamDictionary);
        }
    }
}
