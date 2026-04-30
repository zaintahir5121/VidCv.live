using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace aibabag.Services;

public sealed class SocialPublisherService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<SocialPublisherService> logger) : ISocialPublisherService
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly string _facebookPageId = configuration["Social:FacebookPageId"] ?? string.Empty;
    private readonly string _facebookAccessToken = configuration["Social:FacebookAccessToken"] ?? string.Empty;
    private readonly string _linkedInPersonUrn = configuration["Social:LinkedInPersonUrn"] ?? string.Empty;
    private readonly string _linkedInAccessToken = configuration["Social:LinkedInAccessToken"] ?? string.Empty;
    private readonly ILogger<SocialPublisherService> _logger = logger;

    public async Task<SocialPostResult> PostToFacebookAsync(string accessToken, string pageId, string message, CancellationToken cancellationToken = default)
    {
        var token = string.IsNullOrWhiteSpace(accessToken) ? _facebookAccessToken : accessToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            return new SocialPostResult
            {
                Success = false,
                Message = "Facebook access token missing."
            };
        }

        try
        {
            var effectivePageId = string.IsNullOrWhiteSpace(pageId) ? _facebookPageId : pageId;
            var endpoint = string.IsNullOrWhiteSpace(effectivePageId)
                ? "https://graph.facebook.com/v19.0/me/feed"
                : $"https://graph.facebook.com/v19.0/{effectivePageId}/feed";
            var payload = new Dictionary<string, string>
            {
                ["message"] = message,
                ["access_token"] = token
            };

            var httpClient = _httpClientFactory.CreateClient();
            using var response = await httpClient.PostAsync(endpoint, new FormUrlEncodedContent(payload), cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Facebook publish failed: {StatusCode} {Body}", response.StatusCode, responseBody);
                return new SocialPostResult
                {
                    Success = false,
                    Message = $"Facebook publish failed: {response.StatusCode}"
                };
            }

            string? providerPostId = null;
            try
            {
                using var document = JsonDocument.Parse(responseBody);
                if (document.RootElement.TryGetProperty("id", out var idNode))
                {
                    providerPostId = idNode.GetString();
                }
            }
            catch
            {
                // Ignore parse errors and return success without provider id.
            }

            return new SocialPostResult
            {
                Success = true,
                Message = "Posted to Facebook successfully.",
                ProviderPostId = providerPostId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Facebook publish exception");
            return new SocialPostResult
            {
                Success = false,
                Message = "Facebook publish exception."
            };
        }
    }

    public async Task<SocialPostResult> PostToLinkedInAsync(string accessToken, string linkedInPersonUrn, string message, CancellationToken cancellationToken = default)
    {
        var token = string.IsNullOrWhiteSpace(accessToken) ? _linkedInAccessToken : accessToken;
        var authorUrn = string.IsNullOrWhiteSpace(linkedInPersonUrn) ? _linkedInPersonUrn : linkedInPersonUrn;
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(authorUrn))
        {
            return new SocialPostResult
            {
                Success = false,
                Message = "LinkedIn access token or person URN missing."
            };
        }

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.linkedin.com/v2/ugcPosts");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("X-Restli-Protocol-Version", "2.0.0");

            var payload = new
            {
                author = authorUrn,
                lifecycleState = "PUBLISHED",
                specificContent = new
                {
                    comLinkedinUgcShareContent = new
                    {
                        shareCommentary = new { text = message },
                        shareMediaCategory = "NONE"
                    }
                },
                visibility = new
                {
                    comLinkedinUgcMemberNetworkVisibility = "PUBLIC"
                }
            };

            var json = JsonSerializer.Serialize(payload);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await httpClient.SendAsync(request, cancellationToken);

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("LinkedIn publish failed: {StatusCode} {Body}", response.StatusCode, responseBody);
                return new SocialPostResult
                {
                    Success = false,
                    Message = $"LinkedIn publish failed: {response.StatusCode}"
                };
            }

            string? providerPostId = response.Headers.Location?.ToString();
            return new SocialPostResult
            {
                Success = true,
                Message = "Posted to LinkedIn successfully.",
                ProviderPostId = providerPostId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LinkedIn publish exception");
            return new SocialPostResult
            {
                Success = false,
                Message = "LinkedIn publish exception."
            };
        }
    }
}
