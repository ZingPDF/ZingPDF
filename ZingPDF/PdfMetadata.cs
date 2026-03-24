using ZingPDF.Extensions;
using ZingPDF.Syntax;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.FileStructure.Trailer;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.Dictionaries.PropertyWrappers;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Strings;
using System.Globalization;

namespace ZingPDF;

/// <summary>
/// Represents editable document metadata backed by the trailer <c>Info</c> dictionary.
/// </summary>
public sealed class PdfMetadata
{
    /// <summary>
    /// The producer value written by ZingPDF during save.
    /// </summary>
    public const string ProducerName = "ZingPDF";

    private readonly IPdf _pdf;
    private IndirectObject? _infoIndirectObject;
    private DocumentInformationDictionary? _infoDictionary;
    private bool _loaded;

    private PdfMetadata(IPdf pdf)
    {
        _pdf = pdf;
    }

    /// <summary>
    /// Gets or sets the document title.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the document author.
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// Gets or sets the document subject.
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Gets or sets document keywords.
    /// </summary>
    public string? Keywords { get; set; }

    /// <summary>
    /// Gets or sets the application or person that originally created the document.
    /// </summary>
    public string? Creator { get; set; }

    /// <summary>
    /// Gets the producer value that will be written by ZingPDF on save.
    /// </summary>
    /// <remarks>
    /// This value is managed by ZingPDF and is refreshed during <c>SaveAsync(...)</c>.
    /// </remarks>
    public string? Producer { get; private set; }

    /// <summary>
    /// Gets or sets the original document creation date.
    /// </summary>
    /// <remarks>
    /// Existing documents may store this as either a PDF date object or a raw string; both are supported when loading.
    /// </remarks>
    public DateTimeOffset? CreationDate { get; set; }

    /// <summary>
    /// Gets the modification date that will be written on save.
    /// </summary>
    /// <remarks>
    /// This value is managed by ZingPDF and is refreshed during <c>SaveAsync(...)</c>.
    /// </remarks>
    public DateTimeOffset? ModifiedDate { get; private set; }

    internal IndirectObjectReference? InfoReference => _infoIndirectObject?.Reference;

    internal static async Task<PdfMetadata> LoadAsync(IPdf pdf)
    {
        var metadata = new PdfMetadata(pdf);
        await metadata.EnsureLoadedAsync();
        return metadata;
    }

    internal async Task UpdateAsync()
    {
        await EnsureLoadedAsync();

        _infoDictionary ??= DocumentInformationDictionary.CreateNew(_pdf, ObjectContext.UserCreated);

        SetText(Constants.DictionaryKeys.DocumentInformation.Title, Title);
        SetText(Constants.DictionaryKeys.DocumentInformation.Author, Author);
        SetText(Constants.DictionaryKeys.DocumentInformation.Subject, Subject);
        SetText(Constants.DictionaryKeys.DocumentInformation.Keywords, Keywords);
        SetText(Constants.DictionaryKeys.DocumentInformation.Creator, Creator);

        Producer = ProducerName;
        ModifiedDate = DateTimeOffset.Now;

        SetText(Constants.DictionaryKeys.DocumentInformation.Producer, Producer);
        SetDate(Constants.DictionaryKeys.DocumentInformation.CreationDate, CreationDate);
        SetDate(Constants.DictionaryKeys.DocumentInformation.ModDate, ModifiedDate);

        if (_infoIndirectObject is null)
        {
            _infoIndirectObject = await _pdf.Objects.AddAsync(_infoDictionary);
            return;
        }

        _pdf.Objects.Update(new IndirectObject(_infoIndirectObject.Id, _infoDictionary));
    }

    private async Task EnsureLoadedAsync()
    {
        if (_loaded)
        {
            return;
        }

        var trailerDictionary = await _pdf.Objects.GetLatestTrailerDictionaryAsync();

        if (trailerDictionary.Info is not null)
        {
            _infoIndirectObject = await _pdf.Objects.GetAsync(trailerDictionary.Info);

            _infoDictionary = _infoIndirectObject.Object switch
            {
                DocumentInformationDictionary infoDictionary => infoDictionary,
                Dictionary dictionary => DocumentInformationDictionary.FromDictionary(dictionary.InnerDictionary, _pdf, dictionary.Context),
                _ => throw new InvalidPdfException("Trailer Info entry did not resolve to a dictionary.")
            };

            Title = await DecodeTextAsync(_infoDictionary.Title);
            Author = await DecodeTextAsync(_infoDictionary.Author);
            Subject = await DecodeTextAsync(_infoDictionary.Subject);
            Keywords = await DecodeTextAsync(_infoDictionary.Keywords);
            Creator = await DecodeTextAsync(_infoDictionary.Creator);
            Producer = await DecodeTextAsync(_infoDictionary.Producer);
            CreationDate = await GetDateAsync(Constants.DictionaryKeys.DocumentInformation.CreationDate);
            ModifiedDate = await GetDateAsync(Constants.DictionaryKeys.DocumentInformation.ModDate);
        }

        _loaded = true;
    }

    private static async Task<string?> DecodeTextAsync(OptionalProperty<PdfString> property)
        => (await property.GetAsync())?.Decode();

    private void SetText(string key, string? value)
    {
        _infoDictionary!.Set(key, string.IsNullOrWhiteSpace(value)
            ? null
            : PdfString.FromTextAuto(value, ObjectContext.UserCreated));
    }

    private void SetDate(string key, DateTimeOffset? value)
    {
        _infoDictionary!.Set(key, value is null ? null : new Date(value.Value, ObjectContext.UserCreated));
    }

    private async Task<DateTimeOffset?> GetDateAsync(string key)
    {
        if (_infoDictionary is null)
        {
            return null;
        }

        var value = _infoDictionary.GetAs<IPdfObject>(key);

        if (value is null)
        {
            return null;
        }

        return value switch
        {
            Date date => date.DateTimeOffset,
            PdfString pdfString => ParsePdfDate(pdfString.Decode()),
            _ => throw new InvalidPdfException($"Document information entry '{key}' must be a date or string.")
        };
    }

    private static DateTimeOffset ParsePdfDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new FormatException("Document information date value cannot be empty.");
        }

        var normalized = value.Trim();

        if (normalized.StartsWith("D:", StringComparison.Ordinal))
        {
            normalized = normalized[2..];
        }

        normalized = normalized.Replace("'", string.Empty, StringComparison.Ordinal);

        string[] formats =
        [
            "yyyyMMddHHmmsszzz",
            "yyyyMMddHHmmzzz",
            "yyyyMMddHHzzz",
            "yyyyMMddzzz",
            "yyyyMMzzz",
            "yyyyzzz",
            "yyyyMMddHHmmss",
            "yyyyMMddHHmm",
            "yyyyMMddHH",
            "yyyyMMdd",
            "yyyyMM",
            "yyyy",
        ];

        if (DateTimeOffset.TryParseExact(normalized, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return parsed;
        }

        throw new FormatException($"Invalid document information date format: '{value}'.");
    }
}
