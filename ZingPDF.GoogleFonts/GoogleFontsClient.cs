using System.Net.Http;
using System.Text.Json;

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
