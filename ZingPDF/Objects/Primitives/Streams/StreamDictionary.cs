using ZingPDF.Objects;

namespace ZingPDF.Objects.Primitives.Streams
{
    internal class StreamDictionary : Dictionary, IStreamDictionary
    {
        public static class DictionaryKeys
        {
            public const string Length = "Length";
            public const string Filter = "Filter";
            public const string DecodeParms = "DecodeParms";
            public const string F = "F";
            public const string FFilter = "FFilter";
            public const string FDecodeParms = "FDecodeParms";
            public const string DL = "DL";
        }

        private StreamDictionary(
            Integer length,
            IPdfObject? filter,
            IPdfObject? decodeParms,
            Dictionary? f,
            IPdfObject? fFilter,
            IPdfObject? fDecodeParms,
            Integer? dL
            )
        {
            Length = length;
            Filter = filter;
            DecodeParms = decodeParms;
            F = f;
            FFilter = fFilter;
            FDecodeParms = fDecodeParms;
            DL = dL;
        }

        private StreamDictionary(Dictionary streamDictionary) : base(streamDictionary) { }

        /// <summary>
        /// The number of bytes from the beginning of the line following the keyword stream to the last byte just before the keyword endstream.
        /// (There may be an additional EOL marker, preceding endstream, that is not included in the count and is not logically part of the stream data.)
        /// See 7.3.8.2, "Stream extent", for further discussion.
        /// </summary>
        public Integer Length { get => Get<Integer>(DictionaryKeys.Length)!; private set => Set(DictionaryKeys.Length, value); }

        /// <summary>
        /// The name, or an array of zero, one or several names, of filter(s) that shall be applied
        /// in processing the stream data found between the keywords stream and endstream.
        /// Multiple filters shall be specified in the order in which they are to be applied.
        /// </summary>
        /// <remarks>
        /// NOTE It is not recommended to include the same filter more than once in a Filter array.
        /// </remarks>
        public IPdfObject? Filter { get => Get<IPdfObject>(DictionaryKeys.Filter); private set => Set(DictionaryKeys.Filter, value!); }

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
        public IPdfObject? DecodeParms { get => Get<IPdfObject>(DictionaryKeys.DecodeParms); private set => Set(DictionaryKeys.DecodeParms, value!); }

        /// <summary>
        /// The file containing the stream data. If this entry is present, the bytes between stream and endstream shall be ignored.
        /// However, the Length entry should still specify the number of those bytes (usually, there are no bytes and Length is 0).
        /// The filters that are applied to the file data shall be specified by FFilter and the filter parameters shall be specified by FDecodeParms.
        /// </summary>
        public Dictionary? F { get => Get<Dictionary>(DictionaryKeys.F); private set => Set(DictionaryKeys.F, value!); }

        /// <summary>
        /// The name of a filter to be applied in processing the data found in the stream’s external file, or an array of zero, one or several such names.
        /// The same rules apply as for Filter.
        /// </summary>
        public IPdfObject? FFilter { get => Get<IPdfObject>(DictionaryKeys.FFilter); private set => Set(DictionaryKeys.FFilter, value!); }

        /// <summary>
        /// A parameter dictionary, or an array of such dictionaries, used by the filters specified by FFilter, respectively.
        /// The same rules apply as for DecodeParms.
        /// </summary>
        public IPdfObject? FDecodeParms { get => Get<IPdfObject>(DictionaryKeys.FDecodeParms); private set => Set(DictionaryKeys.FDecodeParms, value!); }

        /// <summary>
        /// A non-negative integer representing the number of bytes in the decoded (defiltered) stream.
        /// This value is only a hint; for some stream filters, it may not be possible to determine this value precisely.
        /// </summary>
        public Integer? DL { get => Get<Integer>(DictionaryKeys.DL); private set => Set(DictionaryKeys.DL, value!); }

        public static StreamDictionary CreateNew(
            Integer length,
            IPdfObject? filter,
            IPdfObject? decodeParms,
            Dictionary? f,
            IPdfObject? fFilter,
            IPdfObject? fDecodeParms,
            Integer? dL
            )
        {
            return new(length, filter, decodeParms, f, fFilter, fDecodeParms, dL);
        }

        public static StreamDictionary FromDictionary(Dictionary streamDictionary)
        {
            if (streamDictionary is null) throw new ArgumentNullException(nameof(streamDictionary));

            return new(streamDictionary);
        }
    }
}
