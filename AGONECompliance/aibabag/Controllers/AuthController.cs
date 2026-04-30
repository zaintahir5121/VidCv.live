using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using aibabag.Data;
using aibabag.Models;
using aibabag.Services;

namespace aibabag.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(
    ApplicationDbContext context,
    IGooglePeopleProfileService googlePeopleProfileService,
    ILogger<AuthController> logger) : ControllerBase
{
    [HttpGet("status")]
    public async Task<IActionResult> Status(CancellationToken cancellationToken)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            return Ok(new { isAuthenticated = false });
        }

        var user = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId.Value, cancellationToken);

        return Ok(new
        {
            isAuthenticated = user is not null,
            userId = user?.Id,
            userName = user?.FullName,
            email = user?.Email
        });
    }

    [HttpGet("login")]
    public async Task Login(string? returnUrl = "/", CancellationToken cancellationToken = default)
    {
        var redirectUri = Url.Action(
            action: nameof(GoogleResponse),
            controller: "Auth",
            values: new { returnUrl = returnUrl ?? "/" }) ?? "/api/auth/google-response";

        await HttpContext.ChallengeAsync(
            GoogleDefaults.AuthenticationScheme,
            new AuthenticationProperties { RedirectUri = redirectUri });
    }

    [HttpGet("google-response")]
    public async Task<IActionResult> GoogleResponse(string? returnUrl = "/", CancellationToken cancellationToken = default)
    {
        try
        {
            var authResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!authResult.Succeeded)
            {
                return RedirectToAction("Index", "Home");
            }

            var claims = authResult.Principal?.Claims ?? [];
            var googleId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var fullName = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? "User";
            var profileImageUrl = claims.FirstOrDefault(c => c.Type == "picture")?.Value;

            if (string.IsNullOrWhiteSpace(googleId) || string.IsNullOrWhiteSpace(email))
            {
                return BadRequest("Unable to retrieve Google profile information.");
            }

            var existing = await context.Users
                .FirstOrDefaultAsync(u => u.GoogleId == googleId, cancellationToken);

            if (existing is null)
            {
                existing = new User
                {
                    GoogleId = googleId,
                    Email = email,
                    FullName = fullName,
                    ProfileImageUrl = profileImageUrl,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                };
                context.Users.Add(existing);
            }
            else
            {
                existing.Email = email;
                existing.FullName = fullName;
                existing.ProfileImageUrl = profileImageUrl;
                existing.UpdatedAtUtc = DateTime.UtcNow;
                context.Users.Update(existing);
            }

            await context.SaveChangesAsync(cancellationToken);
            HttpContext.Session.SetInt32("UserId", existing.Id);

            return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Google authentication callback failed.");
            return BadRequest("Authentication failed.");
        }
    }

    [HttpGet("google-birthday")]
    public async Task<IActionResult> GetGoogleBirthday(CancellationToken cancellationToken)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            return Unauthorized(new { message = "User not authenticated." });
        }

        var user = await context.Users.FirstOrDefaultAsync(x => x.Id == userId.Value, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "User not found." });
        }

        var accessToken = await HttpContext.GetTokenAsync("access_token");
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return Ok(new
            {
                success = false,
                message = "Google access token unavailable for profile sync."
            });
        }

        var birthday = await googlePeopleProfileService.TryGetBirthdayAsync(accessToken, cancellationToken);
        if (!birthday.HasValue)
        {
            return Ok(new
            {
                success = false,
                message = "Could not fetch Google birthday."
            });
        }

        user.GoogleBirthday = birthday;
        user.DateOfBirth ??= birthday;
        user.BirthDateSource = "google-people-api";
        user.BirthDateRawText = birthday.Value.ToString("yyyy-MM-dd");
        user.UpdatedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            success = true,
            birthDate = user.DateOfBirth?.ToString("yyyy-MM-dd"),
            source = user.BirthDateSource
        });
    }

    [HttpPost("upload-photo")]
    public async Task<IActionResult> UploadPhoto([FromBody] PhotoUploadRequest request, CancellationToken cancellationToken)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            return Unauthorized("User not authenticated.");
        }

        var user = await context.Users.FirstOrDefaultAsync(x => x.Id == userId.Value, cancellationToken);
        if (user is null)
        {
            return NotFound("User not found.");
        }

        if (string.IsNullOrWhiteSpace(request.PhotoData))
        {
            return BadRequest("No photo provided.");
        }

        try
        {
            var base64 = request.PhotoData.Contains(',')
                ? request.PhotoData.Split(',')[1]
                : request.PhotoData;
            user.PhotoData = Convert.FromBase64String(base64);
            user.UpdatedAtUtc = DateTime.UtcNow;
            context.Users.Update(user);
            await context.SaveChangesAsync(cancellationToken);

            return Ok(new { success = true, userId = user.Id });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to upload user photo.");
            return BadRequest("Invalid image payload.");
        }
    }

    [HttpGet("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        HttpContext.Session.Clear();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }
}

public sealed class PhotoUploadRequest
{
    public string PhotoData { get; set; } = string.Empty;
}
