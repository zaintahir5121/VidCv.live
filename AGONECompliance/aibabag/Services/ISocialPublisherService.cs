namespace aibabag.Services;

public interface ISocialPublisherService
{
    Task<SocialPostResult> PostToFacebookAsync(string accessToken, string pageId, string message, CancellationToken cancellationToken = default);
    Task<SocialPostResult> PostToLinkedInAsync(string accessToken, string linkedInPersonUrn, string message, CancellationToken cancellationToken = default);
}

public sealed class SocialPostResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ProviderPostId { get; set; }
}
