//using ZingPDF.Parsing.Parsers;
//using ZingPDF.Syntax.Objects;

//namespace ZingPDF.Linearization;

//internal class LinearizationParser
//{
//    //// Read the first 1024 bytes of the file into a string
//    //private static async Task<string> GetFirstKBOfFileAsync(StreamReader reader)
//    //{
//    //    // Read the first 1KB (or less if the stream is smaller) to find the linearization dictionary
//    //    const int bufferSize = 1024;
//    //    char[] buffer = new char[bufferSize];
//    //    int readBytes = await reader.ReadBlockAsync(buffer, 0, bufferSize);

//    //    return new string(buffer, 0, readBytes);
//    //}

//    //// Find the location of /Linearized in the content, if any
//    //private static int? FindLinearizationKey(string content)
//    //{
//    //    const string linearizationMarker = $"/{Constants.DictionaryKeys.LinearizationParameter.Linearized}";
//    //    int markerIndex = content.IndexOf(linearizationMarker, StringComparison.Ordinal);

//    //    if (markerIndex == -1)
//    //    {
//    //        return null; // No linearization dictionary found
//    //    }

//    //    return markerIndex;
//    //}

//    //// Backtrack to find the object start marker (e.g., "1 0 obj")
//    //private static int FindStartOfCurrentObject(string content, int currentPosition)
//    //{
//    //    int objStartIndex = content.LastIndexOf(Constants.ObjStart, currentPosition, StringComparison.Ordinal);
//    //    if (objStartIndex == -1)
//    //    {
//    //        throw new InvalidPdfException("Unable to find indirect object wrapper for linearization parameter dictionary");
//    //    }

//    //    return objStartIndex;
//    //}

//    //// Extract the dictionary at the current position, including the start and end markers
//    //private static string GetDictionaryAsString(string content, int currentPosition)
//    //{
//    //    int dictStartIndex = content.IndexOf("<<", currentPosition, StringComparison.Ordinal);

//    //    // Finding the end index in this way only works for the linearization dictionary
//    //    // as we know it contains no nested dictionaries.
//    //    int dictEndIndex = content.IndexOf(">>", dictStartIndex, StringComparison.Ordinal);

//    //    if (dictStartIndex == -1 || dictEndIndex == -1)
//    //    {
//    //        throw new InvalidPdfException("Invalid linearization parameter dictionary");
//    //    }

//    //    return content.Substring(dictStartIndex, dictEndIndex - dictStartIndex + 2);
//    //}

//    //// Break the string into a dictionary of strings.
//    //private static Dictionary<string, string> ParseToDictionaryOfStrings(string dictionaryContent)
//    //{
//    //    var result = new Dictionary<string, string>(StringComparer.Ordinal);

//    //    // Simple state machine for parsing PDF dictionary key-value pairs
//    //    int length = dictionaryContent.Length;
//    //    for (int i = 0; i < length;)
//    //    {
//    //        SkipWhitespace(dictionaryContent, ref i);
//    //        if (i >= length || dictionaryContent[i] == '>')
//    //        {
//    //            break;
//    //        }

//    //        if (dictionaryContent[i] == '<')
//    //        {
//    //            i++;
//    //            continue;
//    //        }

//    //        // Parse key
//    //        if (dictionaryContent[i] != '/')
//    //        {
//    //            throw new FormatException("Invalid dictionary key format.");
//    //        }

//    //        int keyStart = ++i;
//    //        while (i < length && !char.IsWhiteSpace(dictionaryContent[i]) && dictionaryContent[i] != '/')
//    //        {
//    //            i++;
//    //        }

//    //        string key = dictionaryContent[keyStart..i];

//    //        // Parse value
//    //        SkipWhitespace(dictionaryContent, ref i);
//    //        int valueStart = i;

//    //        SkipValue(dictionaryContent, ref i);

//    //        string value = dictionaryContent[valueStart..i];
//    //        result[key] = value;
//    //    }

//    //    return result;
//    //}

//    //private static LinearizationParameterDictionary ConvertToTypedLinearizationDictionary(Dictionary<string, string> stringDictionary)
//    //{
//    //    var linearizedProperty = new RealNumber(double.Parse(
//    //        stringDictionary[Constants.DictionaryKeys.LinearizationParameter.Linearized]
//    //        ));

//    //    var lengthProperty = new Integer(int.Parse(stringDictionary[Constants.DictionaryKeys.LinearizationParameter.L]));

//    //    var hintStreamInfo = new ArrayObject(
//    //        stringDictionary[Constants.DictionaryKeys.LinearizationParameter.H]
//    //            .Trim()
//    //            .TrimStart(Constants.LeftSquareBracket)
//    //            .TrimEnd(Constants.RightSquareBracket)
//    //            .Split(Constants.WhitespaceCharacters, StringSplitOptions.RemoveEmptyEntries)
//    //            .Select(x => new Integer(int.Parse(x)))
//    //            );

//    //    var firstPageObjectNumber = new Integer(int.Parse(stringDictionary[Constants.DictionaryKeys.LinearizationParameter.O]));
//    //    var firstPageEndOffset = new Integer(int.Parse(stringDictionary[Constants.DictionaryKeys.LinearizationParameter.E]));
//    //    var pageCount = new Integer(int.Parse(stringDictionary[Constants.DictionaryKeys.LinearizationParameter.N]));
//    //    var xrefOffset = new Integer(int.Parse(stringDictionary[Constants.DictionaryKeys.LinearizationParameter.T]));

//    //    var typedDictionary = new Dictionary<Name, IPdfObject>
//    //    {
//    //        { Constants.DictionaryKeys.LinearizationParameter.Linearized, linearizedProperty },
//    //        { Constants.DictionaryKeys.LinearizationParameter.L, lengthProperty },
//    //        { Constants.DictionaryKeys.LinearizationParameter.H, hintStreamInfo },
//    //        { Constants.DictionaryKeys.LinearizationParameter.O, firstPageObjectNumber },
//    //        { Constants.DictionaryKeys.LinearizationParameter.E, firstPageEndOffset },
//    //        { Constants.DictionaryKeys.LinearizationParameter.N, pageCount },
//    //        { Constants.DictionaryKeys.LinearizationParameter.T, xrefOffset }
//    //    };

//    //    if (stringDictionary.TryGetValue(Constants.DictionaryKeys.LinearizationParameter.P, out var firstPageNumberObject))
//    //    {
//    //        var firstPageNumber = new Integer(int.Parse(firstPageNumberObject.ToString()!));

//    //        typedDictionary[Constants.DictionaryKeys.LinearizationParameter.P] = firstPageNumber;
//    //    }

//    //    return LinearizationParameterDictionary.FromDictionary(typedDictionary);
//    //}

//    //private static int FindEndOfLinearizationDictionaryObject(string content, int currentPosition)
//    //{
//    //    var objEndIndex = content.IndexOf(Constants.ObjEnd, currentPosition, StringComparison.Ordinal);
//    //    if (objEndIndex == -1)
//    //    {
//    //        throw new InvalidPdfException("Unable to find end ofindirect object wrapper for linearization parameter dictionary");
//    //    }

//    //    return objEndIndex + Constants.ObjEnd.Length;
//    //}

//    //private static void SkipWhitespace(string content, ref int index)
//    //{
//    //    while (index < content.Length && char.IsWhiteSpace(content[index]))
//    //        index++;
//    //}

//    //private static void SkipValue(string content, ref int index)
//    //{
//    //    bool insideArray = false;

//    //    for (; index < content.Length; index++)
//    //    {
//    //        if (content[index] == Constants.LeftSquareBracket)
//    //        {
//    //            insideArray = true;
//    //        }

//    //        if (content[index] == Constants.RightSquareBracket)
//    //        {
//    //            insideArray = false;
//    //        }

//    //        if (IsDelimiter(content[index]) && !insideArray)
//    //        {
//    //            break;
//    //        }
//    //    }
//    //}

//    //private static bool IsDelimiter(char character)
//    //{
//    //    return new[] { Constants.Solidus, Constants.GreaterThan }.Contains(character)
//    //        || char.IsWhiteSpace(character);
//    //}
//}

//// Commenting this while I test if it works with the original parsers
////pdfInputStream.Position = 0;
////using var reader = new StreamReader(pdfInputStream, Encoding.ASCII, leaveOpen: true);

////var content = await GetFirstKBOfFileAsync(reader);

////var linearizationKeyPosition = FindLinearizationKey(content);

////var fileHasLinearizationDictionary = linearizationKeyPosition != null;
////if (fileHasLinearizationDictionary)
////{
////    var objStartIndex = FindStartOfCurrentObject(content, linearizationKeyPosition!.Value);

////    var dictionaryContent = GetDictionaryAsString(content, objStartIndex);

////    var stringDictionary = ParseToDictionaryOfStrings(dictionaryContent);

////    linearizationDictionary = ConvertToTypedLinearizationDictionary(stringDictionary);

////    var dictEnd = objStartIndex + dictionaryContent.Length;
////    var objEndIndex = FindEndOfLinearizationDictionaryObject(content, dictEnd);

////    pdfInputStream.Position = objEndIndex;
////}

////var items = await Parser.PdfObjectGroups.ParseAsync(new SubStream(pdfInputStream, 0, 1024), HoneyTrapIndirectObjectDictionary.Instance);

////var linearizationDictionaryObject = (IndirectObject?)items.Objects
////    .FirstOrDefault(x => x is IndirectObject o && o.Object is LinearizationParameterDictionary);

////linearizationDictionary = linearizationDictionaryObject?.Object as LinearizationParameterDictionary;