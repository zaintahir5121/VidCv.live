using aibabag.Services;
using Microsoft.AspNetCore.Mvc;

namespace aibabag.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class SocialController(ISocialPublisherService socialPublisherService) : ControllerBase
{
    [HttpPost("post")]
    public async Task<IActionResult> Post([FromBody] SocialPublishRequest request, CancellationToken cancellationToken)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { message = "Message is required." });
        }

        if (string.IsNullOrWhiteSpace(request.Provider))
        {
            return BadRequest(new { message = "Provider is required (facebook/linkedin)." });
        }

        SocialPostResult response;
        if (request.Provider.Equals("facebook", StringComparison.OrdinalIgnoreCase))
        {
            response = await socialPublisherService.PostToFacebookAsync(
                request.AccessToken ?? string.Empty,
                request.FacebookPageId ?? string.Empty,
                request.Message,
                cancellationToken);
        }
        else if (request.Provider.Equals("linkedin", StringComparison.OrdinalIgnoreCase))
        {
            response = await socialPublisherService.PostToLinkedInAsync(
                request.AccessToken ?? string.Empty,
                request.LinkedInPersonUrn ?? string.Empty,
                request.Message,
                cancellationToken);
        }
        else
        {
            return BadRequest(new { message = "Unsupported provider. Use facebook or linkedin." });
        }

        return response.Success ? Ok(response) : BadRequest(response);
    }
}

public sealed class SocialPublishRequest
{
    public string Provider { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? AccessToken { get; set; }
    public string? FacebookPageId { get; set; }
    public string? LinkedInPersonUrn { get; set; }
}
