using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Experion.Backend.Models;
using Experion.Backend.Options;
using Microsoft.Extensions.Options;

namespace Experion.Backend.Services;

public sealed class FacebookActionExecutor(
    IHttpClientFactory httpClientFactory,
    IOptions<ExperionModuleOptions> options,
    ILogger<FacebookActionExecutor> logger) : IActionExecutor
{
    private readonly ExperionModuleOptions _options = options.Value;

    public async Task<ActionExecutionResult> ExecuteAsync(
        string actionCode,
        string productCode,
        Dictionary<string, string> parameters,
        CancellationToken cancellationToken)
    {
        _ = productCode;
        if (!string.Equals(actionCode, "work.social.post-facebook", StringComparison.OrdinalIgnoreCase))
        {
            return new ActionExecutionResult
            {
                ActionCode = actionCode,
                Status = "skipped",
                Message = "Unsupported action for Facebook executor."
            };
        }

        if (string.IsNullOrWhiteSpace(_options.Facebook.PageId)
            || string.IsNullOrWhiteSpace(_options.Facebook.PageAccessToken))
        {
            return new ActionExecutionResult
            {
                ActionCode = actionCode,
                Status = "failed",
                Message = "Facebook configuration missing PageId/PageAccessToken."
            };
        }

        var message = parameters.TryGetValue("message", out var providedMessage)
            ? providedMessage
            : string.Empty;
        if (string.IsNullOrWhiteSpace(message))
        {
            return new ActionExecutionResult
            {
                ActionCode = actionCode,
                Status = "failed",
                Message = "Facebook post message is required."
            };
        }

        var endpoint = $"{_options.Facebook.GraphApiBaseUrl.TrimEnd('/')}/{_options.Facebook.PageId}/feed";
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.Facebook.PageAccessToken);
        httpRequest.Content = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["message"] = message
            });

        var client = httpClientFactory.CreateClient(nameof(FacebookActionExecutor));
        using var response = await client.SendAsync(httpRequest, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Facebook post failed: {Status} - {Body}", response.StatusCode, body);
            return new ActionExecutionResult
            {
                ActionCode = actionCode,
                Status = "failed",
                Message = "Facebook API returned failure.",
                ActionResult = body
            };
        }

        var parsed = TryParseFacebookResponse(body);
        return new ActionExecutionResult
        {
            ActionCode = actionCode,
            Status = "succeeded",
            Message = "Facebook post published successfully.",
            ActionResult = parsed?.Id ?? body
        };
    }

    private static FacebookPublishResponse? TryParseFacebookResponse(string rawJson)
    {
        try
        {
            return JsonSerializer.Deserialize<FacebookPublishResponse>(rawJson);
        }
        catch
        {
            return null;
        }
    }

    private sealed class FacebookPublishResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }
}
