using System.Net.Http.Headers;
using System.Text.Json;

namespace aibabag.Services;

public interface IGooglePeopleProfileService
{
    Task<DateTime?> TryGetBirthdayAsync(string? accessToken, CancellationToken cancellationToken = default);
}

public sealed class GooglePeopleProfileService(
    HttpClient httpClient,
    ILogger<GooglePeopleProfileService> logger) : IGooglePeopleProfileService
{
    private const string PeopleApiUrl = "https://people.googleapis.com/v1/people/me?personFields=birthdays";
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<GooglePeopleProfileService> _logger = logger;

    public async Task<DateTime?> TryGetBirthdayAsync(string? accessToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, PeopleApiUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Google People API returned {StatusCode}", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(content);
            if (!document.RootElement.TryGetProperty("birthdays", out var birthdaysElement) ||
                birthdaysElement.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            foreach (var birthday in birthdaysElement.EnumerateArray())
            {
                if (!birthday.TryGetProperty("date", out var dateElement))
                {
                    continue;
                }

                var year = dateElement.TryGetProperty("year", out var yearElement) ? yearElement.GetInt32() : 2000;
                var month = dateElement.TryGetProperty("month", out var monthElement) ? monthElement.GetInt32() : 0;
                var day = dateElement.TryGetProperty("day", out var dayElement) ? dayElement.GetInt32() : 0;
                if (month <= 0 || day <= 0)
                {
                    continue;
                }

                try
                {
                    return new DateTime(year, month, day);
                }
                catch
                {
                    // Ignore malformed birthday values and try next.
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch birthday from Google People API.");
        }

        return null;
    }
}
