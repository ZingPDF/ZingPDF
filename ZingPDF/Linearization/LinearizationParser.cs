using System.Text;
using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects;

namespace ZingPDF.Linearization;

internal class LinearizationParser
{
    public async Task<LinearizationParameterDictionary?> GetLinearizationDictionaryAsync(Stream pdfInputStream)
    {
        pdfInputStream.Position = 0;

        using var reader = new StreamReader(pdfInputStream, Encoding.ASCII, leaveOpen: true);

        // Read the first 1KB (or less if the stream is smaller) to find the linearization dictionary
        const int bufferSize = 1024;
        char[] buffer = new char[bufferSize];
        int readBytes = await reader.ReadBlockAsync(buffer, 0, bufferSize);
        var content = new string(buffer, 0, readBytes);

        // Look for the linearization number and dictionary opening "obj"
        const string linearizationMarker = $"/{Constants.DictionaryKeys.LinearizationParameter.Linearized}";
        int markerIndex = content.IndexOf(linearizationMarker, StringComparison.Ordinal);

        if (markerIndex == -1)
        {
            return null; // No linearization dictionary found
        }

        // Backtrack to find the object start (e.g., "1 0 obj")
        int objStartIndex = content.LastIndexOf(Constants.ObjStart, markerIndex, StringComparison.Ordinal);
        if (objStartIndex == -1)
        {
            return null; // Malformed PDF structure
        }

        // Parse the dictionary content
        int dictStartIndex = content.IndexOf("<<", objStartIndex, StringComparison.Ordinal);
        int dictEndIndex = content.IndexOf(">>", dictStartIndex, StringComparison.Ordinal);
        if (dictStartIndex == -1 || dictEndIndex == -1)
        {
            return null; // Malformed dictionary
        }

        string dictionaryContent = content.Substring(dictStartIndex, dictEndIndex - dictStartIndex + 2);

        var endObj = content.IndexOf(Constants.ObjEnd);

        // This part is important, so that parsing can continue from the end of the dictionary
        pdfInputStream.Position = Encoding.ASCII.GetByteCount(content[..(endObj + Constants.ObjEnd.Length)]);

        var objectDictionary = ParsePdfDictionary(dictionaryContent);

        var linearizedProperty = new RealNumber(double.Parse(
            objectDictionary[Constants.DictionaryKeys.LinearizationParameter.Linearized].ToString()!
            ));

        var lengthProperty = new Integer(int.Parse(
            objectDictionary[Constants.DictionaryKeys.LinearizationParameter.L].ToString()!
            ));

        var hintStreamInfo = new ArrayObject(
            objectDictionary[Constants.DictionaryKeys.LinearizationParameter.H].ToString()!
                .Trim()
                .TrimStart(Constants.LeftSquareBracket)
                .TrimEnd(Constants.RightSquareBracket)
                .Split(Constants.WhitespaceCharacters, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => new Integer(int.Parse(x)))
                );

        var firstPageObjectNumber = new Integer(int.Parse(
            objectDictionary[Constants.DictionaryKeys.LinearizationParameter.O].ToString()!
            ));

        var firstPageEndOffset = new Integer(int.Parse(
            objectDictionary[Constants.DictionaryKeys.LinearizationParameter.E].ToString()!
            ));

        var pageCount = new Integer(int.Parse(
            objectDictionary[Constants.DictionaryKeys.LinearizationParameter.N].ToString()!
            ));

        var xrefOffset = new Integer(int.Parse(
            objectDictionary[Constants.DictionaryKeys.LinearizationParameter.T].ToString()!
            ));

        var typedDictionary = new Dictionary<Name, IPdfObject>
        {
            { Constants.DictionaryKeys.LinearizationParameter.Linearized, linearizedProperty },
            { Constants.DictionaryKeys.LinearizationParameter.L, lengthProperty },
            { Constants.DictionaryKeys.LinearizationParameter.H, hintStreamInfo },
            { Constants.DictionaryKeys.LinearizationParameter.O, firstPageObjectNumber },
            { Constants.DictionaryKeys.LinearizationParameter.E, firstPageEndOffset },
            { Constants.DictionaryKeys.LinearizationParameter.N, pageCount },
            { Constants.DictionaryKeys.LinearizationParameter.T, xrefOffset }
        };

        if (objectDictionary.TryGetValue(Constants.DictionaryKeys.LinearizationParameter.P, out var item))
        {
            var firstPageNumber = new Integer(int.Parse(
                objectDictionary[Constants.DictionaryKeys.LinearizationParameter.P].ToString()!
                ));

            typedDictionary[Constants.DictionaryKeys.LinearizationParameter.P] = firstPageNumber;
        }

        return LinearizationParameterDictionary.FromDictionary(typedDictionary);
    }

    public bool IsLinearized(LinearizationParameterDictionary linearizationDictionary, Stream pdfInputStream)
        => linearizationDictionary.L == pdfInputStream.Length;

    private static Dictionary<string, object> ParsePdfDictionary(string dictionaryContent)
    {
        var result = new Dictionary<string, object>(StringComparer.Ordinal);

        // Simple state machine for parsing PDF dictionary key-value pairs
        int length = dictionaryContent.Length;
        for (int i = 0; i < length;)
        {
            SkipWhitespace(dictionaryContent, ref i);
            if (i >= length || dictionaryContent[i] == '>')
            {
                break;
            }

            if (dictionaryContent[i] == '<')
            {
                i++;
                continue;
            }

            // Parse key
            if (dictionaryContent[i] != '/')
            {
                throw new FormatException("Invalid dictionary key format.");
            }

            int keyStart = ++i;
            while (i < length && !char.IsWhiteSpace(dictionaryContent[i]) && dictionaryContent[i] != '/')
            {
                i++;
            }

            string key = dictionaryContent[keyStart..i];

            // Parse value
            SkipWhitespace(dictionaryContent, ref i);
            int valueStart = i;

            SkipValue(dictionaryContent, ref i);

            string value = dictionaryContent[valueStart..i];
            result[key] = value;
        }

        return result;
    }

    private static void SkipWhitespace(string content, ref int index)
    {
        while (index < content.Length && char.IsWhiteSpace(content[index]))
            index++;
    }

    private static void SkipValue(string content, ref int index)
    {
        bool insideArray = false;

        for(; index < content.Length; index++)
        {
            if (content[index] == Constants.LeftSquareBracket)
            {
                insideArray = true;
            }

            if (content[index] == Constants.RightSquareBracket)
            {
                insideArray = false;
            }

            if (IsDelimiter(content[index]) && !insideArray)
            {
                break;
            }
        }
    }

    private static bool IsDelimiter(char character)
    {
        return new[] { Constants.Solidus, Constants.GreaterThan }.Contains(character)
            || char.IsWhiteSpace(character);
    }
}
