using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF.Syntax.FileStructure.ObjectStreams
{
    internal class ObjectStreamDictionary : Dictionary, IStreamDictionary
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

        #region IStreamDictionary

        /// <summary>
        /// The number of bytes from the beginning of the line following the keyword stream to the last byte just before the keyword endstream.
        /// (There may be an additional EOL marker, preceding endstream, that is not included in the count and is not logically part of the stream data.)
        /// See 7.3.8.2, "Stream extent", for further discussion.
        /// </summary>
        public Integer Length { get => Get<Integer>(Constants.DictionaryKeys.Stream.Length)!; }

        /// <summary>
        /// The name, or an array of zero, one or several names, of filter(s) that shall be applied
        /// in processing the stream data found between the keywords stream and endstream.
        /// Multiple filters shall be specified in the order in which they are to be applied.
        /// </summary>
        /// <remarks>
        /// NOTE It is not recommended to include the same filter more than once in a Filter array.
        /// </remarks>
        public IPdfObject? Filter { get => Get<IPdfObject>(Constants.DictionaryKeys.Stream.Filter); }

        /// <summary>
        /// A parameter dictionary or an array of such dictionaries, used by the filters specified by Filter, respectively.
        /// If there is only one filter and that filter has parameters, DecodeParms shall be set to the filter's parameter
        /// dictionary unless all the filter’s parameters have their default values, in which case the DecodeParms entry may be omitted.
        /// If there are multiple filters and any of the filters has parameters set to nondefault values,
        /// DecodeParms shall be an array with one entry for each filter in the same order as the Filter array:
        /// either the parameter dictionary for that filter, or the null object if that filter has no parameters
        /// (or if all of its parameters have their default values). If none of the filters have parameters, or if
        /// all their parameters have default values, the DecodeParms entry may be omitted.
        /// </summary>
        public IPdfObject? DecodeParms { get => Get<IPdfObject>(Constants.DictionaryKeys.Stream.DecodeParms); }

        /// <summary>
        /// The file containing the stream data. If this entry is present, the bytes between stream and endstream shall be ignored.
        /// However, the Length entry should still specify the number of those bytes (usually, there are no bytes and Length is 0).
        /// The filters that are applied to the file data shall be specified by FFilter and the filter parameters shall be specified by FDecodeParms.
        /// </summary>
        public Dictionary? F { get => Get<Dictionary>(Constants.DictionaryKeys.Stream.F); }

        /// <summary>
        /// The name of a filter to be applied in processing the data found in the stream’s external file, or an array of zero, one or several such names.
        /// The same rules apply as for Filter.
        /// </summary>
        public IPdfObject? FFilter { get => Get<IPdfObject>(Constants.DictionaryKeys.Stream.FFilter); }

        /// <summary>
        /// A parameter dictionary, or an array of such dictionaries, used by the filters specified by FFilter, respectively.
        /// The same rules apply as for DecodeParms.
        /// </summary>
        public IPdfObject? FDecodeParms { get => Get<IPdfObject>(Constants.DictionaryKeys.Stream.FDecodeParms); }

        /// <summary>
        /// A non-negative integer representing the number of bytes in the decoded (defiltered) stream.
        /// This value is only a hint; for some stream filters, it may not be possible to determine this value precisely.
        /// </summary>
        public Integer? DL { get => Get<Integer>(Constants.DictionaryKeys.Stream.DL); }

        #endregion

        public static ObjectStreamDictionary FromDictionary(Dictionary objectStreamDictionary)
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
