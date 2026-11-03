using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ZingPDF.GoogleFonts;

/// <summary>
/// Client for the Google Fonts Developer API.
/// </summary>
public sealed class GoogleFontsClient
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;

    /// <summary>
    /// Creates a client that downloads TrueType variants through the public Google
    /// Fonts CSS API without an API key. Family metadata still requires the keyed constructor.
    /// Latin, extended Latin, Greek, Cyrillic and Vietnamese subsets are requested together.
    /// Font requests never contain document text. Character coverage must be checked before use.
    /// </summary>
    public GoogleFontsClient(HttpClient? httpClient = null)
    {
        ApiKey = "";
        _httpClient = httpClient ?? new HttpClient();
    }

    public GoogleFontsClient(string apiKey, HttpClient? httpClient = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);

        ApiKey = apiKey;
        _httpClient = httpClient ?? new HttpClient();
    }

    public string ApiKey { get; }

    /// <summary>
    /// Fetches metadata for a single Google Font family.
    /// </summary>
    public async Task<GoogleFontFamily> GetFamilyAsync(string family, bool preferVariableFont = false, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(family);
        if (ApiKey.Length == 0) throw new InvalidOperationException("Family metadata requires a Google Fonts Developer API key. Font downloads can use the keyless constructor.");

        var fontFamily = await TryGetFamilyAsync(family, preferVariableFont, cancellationToken);
        if (fontFamily is not null)
        {
            return fontFamily;
        }

        if (preferVariableFont)
        {
            return await TryGetFamilyAsync(family, preferVariableFont: false, cancellationToken)
                ?? throw new InvalidOperationException($"Unable to find Google Font family '{family}'.");
        }

        throw new InvalidOperationException($"Unable to find Google Font family '{family}'.");
    }

    /// <summary>
    /// Downloads the requested Google Font variant as a memory stream.
    /// </summary>
    public async Task<MemoryStream> DownloadFontAsync(GoogleFontRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (ApiKey.Length == 0) return await DownloadCssFontAsync(request, cancellationToken);

        var family = await GetFamilyAsync(request.Family, request.PreferVariableFont, cancellationToken);
        var variantKey = ResolveVariantKey(family, request.Variant);
        var downloadUri = family.Variants[variantKey];

        using var response = await _httpClient.GetAsync(downloadUri, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var output = new MemoryStream();
        await responseStream.CopyToAsync(output, cancellationToken);
        output.Position = 0;

        return output;
    }

    private async Task<MemoryStream> DownloadCssFontAsync(GoogleFontRequest request, CancellationToken token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Family);
        if (request.Family.Length > 120 || request.Family.Any(c => !(char.IsLetterOrDigit(c) || c is ' ' or '-')))
            throw new ArgumentException("Use a Google Fonts family name containing letters, numbers, spaces or hyphens.", nameof(request));
        var variant = request.Variant.ToLowerInvariant() switch { "regular" => "400", "italic" => "400italic", var v => v };
        if (!Regex.IsMatch(variant, "^[1-9]00(italic)?$", RegexOptions.CultureInvariant))
            throw new ArgumentException("Choose a font weight from 100 to 900, optionally followed by italic.", nameof(request));
        var uri = new Uri("https://fonts.googleapis.com/css?family=" + Uri.EscapeDataString(request.Family).Replace("%20", "+") + ":" + variant + "&subset=latin,latin-ext,greek,greek-ext,cyrillic,cyrillic-ext,vietnamese");
        using var message = new HttpRequestMessage(HttpMethod.Get, uri);
        // The legacy CSS response for this generic agent exposes one combined TrueType
        // program for the requested scripts instead of browser-specific WOFF2 ranges.
        message.Headers.UserAgent.ParseAdd("Mozilla/5.0");
        using var response = await _httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, token);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength > 128 * 1024) throw new InvalidOperationException("Google Fonts returned an unexpectedly large stylesheet.");
        var css = await response.Content.ReadAsStringAsync(token);
        if (css.Length > 128 * 1024) throw new InvalidOperationException("Google Fonts returned an unexpectedly large stylesheet.");
        var matches = Regex.Matches(css, "url\\(\\s*['\"]?(https://[^)'\"\\s]+)['\"]?\\s*\\)\\s*format\\(['\"]truetype['\"]\\)", RegexOptions.CultureInvariant);
        var urls = matches.Select(m => m.Groups[1].Value).Distinct(StringComparer.Ordinal).ToArray();
        if (urls.Length != 1 || !Uri.TryCreate(urls[0], UriKind.Absolute, out var fontUri) || fontUri.Host != "fonts.gstatic.com")
            throw new NotSupportedException("Google Fonts did not provide one combined TrueType file for this variant. Choose another font or variant.");
        using var fontResponse = await _httpClient.GetAsync(fontUri, HttpCompletionOption.ResponseHeadersRead, token);
        fontResponse.EnsureSuccessStatusCode();
        const int maximumBytes = 32 * 1024 * 1024;
        if (fontResponse.Content.Headers.ContentLength > maximumBytes) throw new NotSupportedException("This font exceeds the 32 MB download limit.");
        await using var source = await fontResponse.Content.ReadAsStreamAsync(token);
        var output = new MemoryStream();
        try
        {
            var buffer = new byte[65536];
            int count;
            while ((count = await source.ReadAsync(buffer, token)) != 0)
            {
                if (output.Length + count > maximumBytes) throw new NotSupportedException("This font exceeds the 32 MB download limit.");
                await output.WriteAsync(buffer.AsMemory(0, count), token);
            }
            output.Position = 0;
            return output;
        }
        catch { output.Dispose(); throw; }
    }

    internal static string ResolveVariantKey(GoogleFontFamily family, string requestedVariant)
    {
        ArgumentNullException.ThrowIfNull(family);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedVariant);

        var variant = requestedVariant.Trim().ToLowerInvariant();
        string[] candidates = variant switch
        {
            "400" => ["regular"],
            "400italic" => ["italic"],
            _ => [variant]
        };

        foreach (var candidate in candidates)
        {
            var key = family.Variants.Keys.FirstOrDefault(x => string.Equals(x, candidate, StringComparison.OrdinalIgnoreCase));
            if (key is not null)
            {
                return key;
            }
        }

        throw new InvalidOperationException($"Google Font '{family.Family}' does not expose variant '{requestedVariant}'.");
    }

    private async Task<GoogleFontFamily?> TryGetFamilyAsync(string family, bool preferVariableFont, CancellationToken cancellationToken)
    {
        var uri = BuildMetadataUri(family, preferVariableFont);

        using var response = await _httpClient.GetAsync(uri, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<GoogleWebfontsResponse>(
            responseStream,
            _jsonOptions,
            cancellationToken);

        var familyItem = payload?.Items?.FirstOrDefault(x => string.Equals(x.Family, family, StringComparison.OrdinalIgnoreCase));
        if (familyItem is null || familyItem.Files is null || familyItem.Files.Count == 0)
        {
            return null;
        }

        return new GoogleFontFamily
        {
            Family = familyItem.Family ?? family,
            Category = familyItem.Category,
            Variants = familyItem.Files
                .Where(x => Uri.TryCreate(x.Value, UriKind.Absolute, out _))
                .ToDictionary(x => x.Key, x => new Uri(x.Value, UriKind.Absolute), StringComparer.OrdinalIgnoreCase)
        };
    }

    private Uri BuildMetadataUri(string family, bool preferVariableFont)
    {
        var builder = new UriBuilder("https://www.googleapis.com/webfonts/v1/webfonts");
        var query = new List<string>
        {
            $"key={Uri.EscapeDataString(ApiKey)}",
            $"family={Uri.EscapeDataString(family)}"
        };

        if (preferVariableFont)
        {
            query.Add("capability=VF");
        }

        builder.Query = string.Join("&", query);
        return builder.Uri;
    }

    private sealed class GoogleWebfontsResponse
    {
        public List<GoogleWebfontItem>? Items { get; set; }
    }

    private sealed class GoogleWebfontItem
    {
        public string? Family { get; set; }
        public string? Category { get; set; }
        public Dictionary<string, string>? Files { get; set; }
    }
}
