using MorseCode.ITask;
using ZingPDF.IncrementalUpdates;
using ZingPDF.Logging;
using ZingPDF.Syntax;

namespace ZingPDF.Parsing.Parsers
{
    internal class PdfObjectGroupParser : IObjectParser<PdfObjectGroup>
    {
        private readonly IPdfEditor? _pdfEditor;

        /// <summary>
        /// This constructor is potentially dangerous, use with care. Objects parsed with this constructor will not 
        /// contain an internal reference to the <see cref="IPdfEditor"/>. This will cause issues when trying to 
        /// access any indirect references. This version of the parser should only be used during initial parsing of 
        /// cross reference streams when building object indexes, or when it is certain that any contained indirect 
        /// object properties downstream in the object heirarchy are not accessed through their 
        /// <see cref="Syntax.Objects.Dictionaries.DictionaryProperty{T}.GetAsync"/> or 
        /// <see cref="Syntax.Objects.Dictionaries.DictionaryMultiProperty{T1, T2}.GetAsync"/> methods.
        /// </summary>
        internal PdfObjectGroupParser()
        {
        }

        public PdfObjectGroupParser(IPdfEditor pdfEditor)
        {
            _pdfEditor = pdfEditor;
        }

        public async ITask<PdfObjectGroup> ParseAsync(Stream stream)
        {
            Logger.Log(LogLevel.Trace, $"Parsing PdfObjectGroup from {stream.GetType().Name} at offset: {stream.Position}.");

            var items = new List<IPdfObject>();

            while (stream.Position < stream.Length)
            {
                var type = await TokenTypeIdentifier.TryIdentifyAsync(stream);

                if (type != null)
                {
                    try
                    {
                        items.Add(await Parser.For(type, _pdfEditor).ParseAsync(stream));
                    }
                    catch
                    {
                        // If any exception is thrown, gracefully exit.
                        // The sub-object could be invalid or not understood by this library.
                        // There are also scenarios where we don't have complete data, but want to parse what we can anyway,
                        // such as reading a fixed size chunk from the beginning of the file to find the linearization dictionary.
                        break;
                    }
                }
                else
                {
                    stream.Position += 1;
                }
            }

            return items.ToArray();
        }
    }
}