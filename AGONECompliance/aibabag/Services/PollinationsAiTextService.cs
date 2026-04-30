using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace aibabag.Services;

public sealed class PollinationsAiTextService(
    HttpClient httpClient,
    IOptions<AiProviderOptions> options,
    ILogger<PollinationsAiTextService> logger) : IAiTextService
{
    private const int PromptMaxLength = 1000;
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<PollinationsAiTextService> _logger = logger;
    private readonly AiProviderOptions _options = options.Value;

    public async Task<string> GenerateAsync(string prompt, string fallback, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            return fallback;
        }

        var trimmedPrompt = prompt.Length > PromptMaxLength
            ? prompt[..PromptMaxLength]
            : prompt;

        var encodedPrompt = Uri.EscapeDataString(trimmedPrompt);
        var requestUri = $"{_options.BaseUrl.TrimEnd('/')}/{encodedPrompt}";

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linkedCts.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

        try
        {
            using var response = await _httpClient.GetAsync(requestUri, linkedCts.Token);
            if (response.StatusCode == HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500)
            {
                _logger.LogWarning("Free AI provider temporary error ({StatusCode}); using fallback text.", response.StatusCode);
                return fallback;
            }

            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync(linkedCts.Token);
            var normalized = NormalizeText(content);
            return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Free AI provider call failed; using fallback text.");
            return fallback;
        }
    }

    private static string NormalizeText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var cleaned = value.Trim();
        if ((cleaned.StartsWith("{") && cleaned.EndsWith("}")) ||
            (cleaned.StartsWith("[") && cleaned.EndsWith("]")))
        {
            try
            {
                using var doc = JsonDocument.Parse(cleaned);
                if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                    doc.RootElement.TryGetProperty("text", out var textNode) &&
                    textNode.ValueKind == JsonValueKind.String)
                {
                    return textNode.GetString()?.Trim() ?? string.Empty;
                }
            }
            catch
            {
                // Plain response body handled below.
            }
        }

        return cleaned.Replace('\n', ' ').Replace('\r', ' ').Trim();
    }
}
