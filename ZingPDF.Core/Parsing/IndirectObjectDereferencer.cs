using ZingPdf.Core.Objects;
using ZingPdf.Core.Objects.ObjectGroups;
using ZingPdf.Core.Objects.Primitives.IndirectObjects;
using ZingPdf.Core.Objects.Primitives.Streams;

namespace ZingPdf.Core.Parsing
{
    internal class IndirectObjectDereferencer
    {
        /// <summary>
        /// Returns the latest Indirect Object matching the given reference.
        /// </summary>
        public async Task<IndirectObject> GetAsync(Stream stream, IndirectObjectReference reference)
        {
            var pdfTraversal = new StreamPdfTraversal(stream);
            var xrefs = await pdfTraversal.GetAggregateCrossReferencesAsync();
            
            var xref = xrefs[reference.Id.Index];

            var indirectObjectParser = Parser.For<IndirectObject>();

            if (xref.Compressed)
            {
                // TODO: cache this locally

                // Just parsing the whole object stream for now.
                // I started to write code to just parse the requested object.
                // TODO: compare performance of these 2 techniques.

                var objStreamIndirectObject = await GetAsync(stream, new IndirectObjectReference(new IndirectObjectId((int)xref.Value1, 0)));
                var objectStream = (objStreamIndirectObject.Children.First() as StreamObject)!;
                var objectStreamDict = (objectStream.Dictionary as ObjectStreamDictionary)!;

                var data = await objectStream.DecodeAsync();

                //var offsets = Encoding.ASCII.GetString(data[..objectStreamDict.First]).Split(Constants.Whitespace);

                //Dictionary<int, int> indexedOffsets = new();

                //for(var i = 0; i < objectStreamDict.N; i += 2)
                //{
                //    var objectNumber = Convert.ToInt32(offsets[i]);
                //    var byteOffset = Convert.ToInt32(offsets[i + 1]);

                //    indexedOffsets.Add(objectNumber, byteOffset);
                //}

                //var objectOffset = indexedOffsets[reference.Id.Index];

                using var ms = new MemoryStream(data[objectStreamDict.First..]);
                var allObjects = await Parser.For<PdfObjectGroup>().ParseAsync(ms);

                return new IndirectObject(reference.Id, allObjects.Objects[xref.Value2]);
            }

            stream.Position = xref.Value1;

            return await indirectObjectParser.ParseAsync(stream);
        }

        /// <summary>
        /// When you know the Indirect Object contains a single object of a specific type, 
        /// this method provides strongly typed access to it.
        /// </summary>
        public async Task<T> GetSingleAsync<T>(Stream stream, IndirectObjectReference reference) where T : PdfObject
            => (T)(await GetAsync(stream, reference)).Children.First();

        public async IAsyncEnumerable<IndirectObject> GetAllAsync(Stream stream)
        {
            var pdfTraversal = new StreamPdfTraversal(stream);
            var xrefs = await pdfTraversal.GetAggregateCrossReferencesAsync();

            foreach (var record in xrefs)
            {
                stream.Position = record.Value.Value1;

                yield return await Parser.For<IndirectObject>().ParseAsync(stream);
            }
        }
    }
}
