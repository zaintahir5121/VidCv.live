using System.Net.Http.Headers;
using System.Text.Json;

namespace aibabag.Services;

public sealed class WikimediaFamousBirthdayService(
    HttpClient httpClient,
    ILogger<WikimediaFamousBirthdayService> logger) : IFamousBirthdayService
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<WikimediaFamousBirthdayService> _logger = logger;

    public async Task<IReadOnlyList<FamousPersonality>> GetByDateAsync(DateTime dateOfBirth, CancellationToken cancellationToken = default)
    {
        var endpoint = $"https://api.wikimedia.org/feed/v1/wikipedia/en/onthisday/births/{dateOfBirth.Month}/{dateOfBirth.Day}";
        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.UserAgent.ParseAdd("Aibabag/1.0");
        request.Headers.TryAddWithoutValidation("Api-User-Agent", "Aibabag/1.0");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Wikimedia birthdays request failed with status {StatusCode}", response.StatusCode);
                return [];
            }

            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(raw);
            if (!doc.RootElement.TryGetProperty("births", out var births) || births.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var people = new List<FamousPersonality>();
            foreach (var birth in births.EnumerateArray())
            {
                if (!birth.TryGetProperty("pages", out var pages) || pages.ValueKind != JsonValueKind.Array || pages.GetArrayLength() == 0)
                {
                    continue;
                }

                var page = pages[0];
                var imageUrl = TryGetImageUrl(page);
                if (string.IsNullOrWhiteSpace(imageUrl))
                {
                    continue;
                }

                var title = GetString(page, "normalizedtitle");
                var extract = GetString(page, "extract");
                var article = GetString(page, "content_urls", "desktop", "page");
                if (string.IsNullOrWhiteSpace(title))
                {
                    continue;
                }

                people.Add(new FamousPersonality
                {
                    Name = title,
                    Summary = TrimSummary(string.IsNullOrWhiteSpace(extract) ? GetString(birth, "text") : extract),
                    ImageUrl = imageUrl,
                    ArticleUrl = article
                });

                if (people.Count >= 10)
                {
                    break;
                }
            }

            return people;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Wikimedia birthdays request failed.");
            return [];
        }
    }

    private static string TryGetImageUrl(JsonElement page)
    {
        if (!page.TryGetProperty("thumbnail", out var thumbnail) || thumbnail.ValueKind != JsonValueKind.Object)
        {
            return string.Empty;
        }

        return GetString(thumbnail, "source");
    }

    private static string GetString(JsonElement source, params string[] path)
    {
        var current = source;
        foreach (var segment in path)
        {
            if (!current.TryGetProperty(segment, out current))
            {
                return string.Empty;
            }
        }

        return current.ValueKind == JsonValueKind.String
            ? current.GetString() ?? string.Empty
            : string.Empty;
    }

    private static string TrimSummary(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Born on your day. Cosmic overlap unlocked.";
        }

        var normalized = value.Trim().Replace("\n", " ");
        return normalized.Length > 140 ? $"{normalized[..137]}..." : normalized;
    }
}
