//#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

//using Nito.AsyncEx;
//using ZingPDF.Extensions;
//using ZingPDF.Linearization;
//using ZingPDF.Logging;
//using ZingPDF.ObjectModel;
//using ZingPDF.ObjectModel.DocumentStructure;
//using ZingPDF.ObjectModel.DocumentStructure.PageTree;
//using ZingPDF.ObjectModel.FileStructure.CrossReferences;
//using ZingPDF.ObjectModel.FileStructure.CrossReferences.CrossReferenceStreams;
//using ZingPDF.ObjectModel.FileStructure.Trailer;
//using ZingPDF.ObjectModel.Objects;
//using ZingPDF.ObjectModel.Objects.IndirectObjects;
//using ZingPDF.ObjectModel.Objects.Streams;
//using ZingPDF.Parsing.Parsers;

//namespace ZingPDF.Parsing;

///// <summary>
///// This class provides access to elements within a PDF file.
///// </summary>
//internal class PdfFileNavigator : IPdfNavigator
//{
//    private readonly Stream _stream;

//    /// <summary>
//    /// Contains the object at the offset specified by startxref.
//    /// It could be a cross-reference stream (indirect object containing a dictionary and stream)
//    /// or an xref keyword followed by the cross reference table.
//    /// </summary>
//    private AsyncLazy<IPdfObject> _xrefObject;

//    /// <summary>
//    /// The position of the root xref table or stream.
//    /// </summary>
//    private AsyncLazy<int> _startXref;

//    private AsyncLazy<LinearizationParameterDictionary?> _linearizationParameters;
//    private AsyncLazy<Trailer?> _rootTrailer;
//    private AsyncLazy<ITrailerDictionary> _rootTrailerDictionary;
//    private AsyncLazy<IndirectObject> _rootPageTreeNode;
//    private AsyncLazy<IEnumerable<IndirectObject>> _pages;
//    private AsyncLazy<Dictionary<int, CrossReferenceEntry>> _xrefs;

//    public PdfFileNavigator(Stream stream)
//    {
//        _stream = stream ?? throw new ArgumentNullException(nameof(stream));

//        SetupLazyProperties();
//    }

//    public bool UsingXrefTables { get; private set; }
//    public bool UsingXrefStreams { get; private set; }

//    public Task<int> GetStartXrefAsync() => _startXref.Task;

//    public async Task<LinearizationParameterDictionary?> GetLinearizationDictionaryAsync()
//    {
//        Logger.Log(LogLevel.Trace, $"Searching for linearisation dictionary");

//        _stream.Position = 0;

//        static bool isLinearizationDictionary(IndirectObject o) =>
//            o.Children.FirstOrDefault() is LinearizationParameterDictionary;

//        List<PdfObject> items = [];

//        var limit = Math.Min(1024, _stream.Length);

//        while (_stream.Position < limit)
//        {
//            var type = await TokenTypeIdentifier.TryIdentifyAsync(_stream);
//            if (type is null)
//            {
//                // TODO: is this a valid scenario?
//                break;
//            }

//            var item = await Parser.For(type).ParseAsync(_stream);

//            if (item is IndirectObject o && isLinearizationDictionary(o))
//            {
//                Logger.Log(LogLevel.Trace, $"Found linearisation dictionary");

//                return o.Children.First()! as LinearizationParameterDictionary;
//            }
//        }

//        Logger.Log(LogLevel.Trace, $"No linearisation dictionary found");

//        return null;
//    }

//    /// <summary>
//    /// Get the latest trailer.
//    /// </summary>
//    /// <remarks>
//    /// A PDF which stores its cross reference information in streams will not have a trailer.<para></para>
//    /// In such a file, the cross reference stream dictionary contains the trailer data.<para></para>
//    /// </remarks>
//    public Task<Trailer?> GetRootTrailerAsync() => _rootTrailer.Task;

//    /// <summary>
//    /// Get the latest trailer dictionary.
//    /// </summary>
//    /// <remarks>
//    /// For PDFs which store their cross reference information in streams, this method will return the cross reference stream dictionary.<para></para>
//    /// </remarks>
//    public Task<ITrailerDictionary> GetRootTrailerDictionaryAsync() => _rootTrailerDictionary.Task;

//    public Task<IEnumerable<IndirectObject>> GetPagesAsync() => _pages.Task;

//    public Task<IndirectObject> GetRootPageTreeNodeAsync() => _rootPageTreeNode.Task;

//    private void SetupLazyProperties()
//    {
//        _xrefObject = SetupLazyXrefObject();
//        _startXref = SetupLazyStartXref();

//        _rootTrailer = SetupLazyRootTrailer();
//        _rootTrailerDictionary = SetupLazyRootTrailerDictionary();
//        _rootPageTreeNode = SetupLazyRootPageTreeNode();
//        _pages = SetupLazyPages();
//    }

//    /// <summary>
//    /// Recursively get all descendant subpages from the supplied <see cref="PageTreeNode"/>.
//    /// </summary>
//    private async Task<IEnumerable<IndirectObject>> GetSubPagesAsync(PageTreeNode pageTreeNode)
//    {
//        // TODO: check page ordering, should mimic whatever Acrobat Reader infers

//        List<IndirectObject> pages = [];

//        foreach (var refObj in pageTreeNode.Kids)
//        {
//            var ior = (IndirectObjectReference)refObj;

//            var obj = await DereferenceIndirectObjectAsync(ior);

//            if (obj.Children.First() is Page)
//            {
//                pages.Add(obj);
//            }
//            else if (obj.Children.First() is PageTreeNode ptn)
//            {
//                pages.AddRange(await GetSubPagesAsync(ptn));
//            }
//        }

//        return pages;
//    }

//    private AsyncLazy<Trailer?> SetupLazyRootTrailer()
//    {
//        return new AsyncLazy<Trailer?>(async () =>
//        {
//            Logger.Log(LogLevel.Trace, $"Searching for root trailer");

//            var xrefObject = await _xrefObject;

//            if (xrefObject is IndirectObject io
//                && io.Children.First() is IStreamObject<IStreamDictionary> so
//                && so.Dictionary is CrossReferenceStreamDictionary dict)
//            {
//                Logger.Log(LogLevel.Trace, $"Cross reference stream found instead of trailer");

//                return null;
//            }

//            if (xrefObject is Keyword k && k == Constants.Xref)
//            {
//                var objectFinder = new ObjectFinder();

//                var trailerOffset = await objectFinder.FindAsync(_stream, Constants.Trailer, forwards: false);

//                if (trailerOffset is not null)
//                {
//                    _stream.Position = trailerOffset.Value;

//                    var trailer = await Parser.For<Trailer>().ParseAsync(_stream);

//                    return trailer;
//                }
//            }

//            throw new InvalidOperationException("Unable to find PDF trailer information.");
//        });
//    }

//    private AsyncLazy<int> SetupLazyStartXref()
//    {
//        return new AsyncLazy<int>(async () =>
//        {
//            var objectFinder = new ObjectFinder();

//            // First, find the startxref keyword
//            var offset = await objectFinder.FindAsync(_stream, Constants.StartXref, forwards: false)
//                ?? throw new InvalidOperationException($"{Constants.StartXref} not found.");

//            _stream.Position = offset;

//            _ = await Parser.For<Keyword>().ParseAsync(_stream);
//            return await Parser.For<Integer>().ParseAsync(_stream);
//        });
//    }

//    private AsyncLazy<IPdfObject> SetupLazyXrefObject()
//    {
//        return new AsyncLazy<IPdfObject>(async () =>
//        {
//            var xrefOffset = await GetStartXrefAsync();

//            _stream.Position = xrefOffset;

//            var type = await TokenTypeIdentifier.TryIdentifyAsync(_stream);

//            var item = await Parser.For(type).ParseAsync(_stream);

//            return item;
//        });
//    }

//    private AsyncLazy<ITrailerDictionary> SetupLazyRootTrailerDictionary()
//    {
//        return new AsyncLazy<ITrailerDictionary>(async () =>
//        {
//            Logger.Log(LogLevel.Trace, $"Searching for root trailer dictionary");

//            var trailer = await GetRootTrailerAsync();
//            if (trailer is not null)
//            {
//                Logger.Log(LogLevel.Trace, $"Found trailer, returning dictionary");

//                return trailer.Dictionary;
//            }

//            var xrefObject = await _xrefObject;

//            if (xrefObject is IndirectObject io
//                && io.Children.First() is IStreamObject<IStreamDictionary> so
//                && so.Dictionary is CrossReferenceStreamDictionary dict)
//            {
//                Logger.Log(LogLevel.Trace, $"Found cross reference stream dictionary");

//                return dict;
//            }

//            throw new InvalidOperationException("Unable to find PDF trailer information.");
//        });
//    }

//    //private AsyncLazy<IndirectObject> SetupLazyRootPageTreeNode()
//    //{
//    //    return new AsyncLazy<IndirectObject>(async () =>
//    //    {
//    //        Logger.Log(LogLevel.Trace, $"Searching for root page tree node");

//    //        var trailerDictionary = await GetRootTrailerDictionaryAsync();

//    //        var documentCatalog = await DereferenceIndirectObjectAsync<DocumentCatalogDictionary>(trailerDictionary.Root);

//    //        var xrefs = await GetAggregateCrossReferencesAsync();
//    //        var xref = xrefs[documentCatalog.Pages.Id.Index];

//    //        return await DereferenceIndirectObjectAsync(documentCatalog.Pages);
//    //    });
//    //}

//    private AsyncLazy<IEnumerable<IndirectObject>> SetupLazyPages()
//    {
//        return new AsyncLazy<IEnumerable<IndirectObject>>(async () =>
//        {
//            var rootPageTreeNodeIndirectObject = await GetRootPageTreeNodeAsync();

//            var rootPageTreeNode = PageTreeNode.FromDictionary((rootPageTreeNodeIndirectObject.Children.First() as Dictionary)!);

//            return await GetSubPagesAsync(rootPageTreeNode);
//        });
//    }
//}

//#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.