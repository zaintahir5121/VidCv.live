using aibabag.Data;
using aibabag.Models;
using aibabag.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace aibabag.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class InsightsController(
    ApplicationDbContext context,
    IAstrologyService astrologyService,
    ILogger<InsightsController> logger) : ControllerBase
{
    [HttpPost("generate")]
    public async Task<IActionResult> GenerateInsights([FromBody] InsightRequest request)
    {
        try
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var user = await context.Users.FindAsync(userId.Value);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            user.DateOfBirth = request.DateOfBirth;
            user.ZodiacSign = astrologyService.GetZodiacSign(request.DateOfBirth);
            user.ChineseZodiac = astrologyService.GetChineseZodiac(request.DateOfBirth);
            user.UpdatedAtUtc = DateTime.UtcNow;

            var personalityInsights = await astrologyService.CalculatePersonalityInsights(user);
            var monthlyForecast = await astrologyService.GetMonthlyForecast(user.ZodiacSign);

            var insight = new AstrologyInsight
            {
                UserId = user.Id,
                PersonalityTraits = personalityInsights["personalityTraits"].ToString() ?? string.Empty,
                LuckyNumbers = personalityInsights["luckyNumbers"].ToString() ?? string.Empty,
                LuckyColor = personalityInsights["luckyColor"].ToString() ?? string.Empty,
                Element = personalityInsights["element"].ToString() ?? string.Empty,
                HealthInsights = personalityInsights["healthInsights"].ToString() ?? string.Empty,
                CareerInsights = personalityInsights["careerInsights"].ToString() ?? string.Empty,
                LoveInsights = personalityInsights["loveInsights"].ToString() ?? string.Empty,
                MonthlyForecast = JsonSerializer.Serialize(monthlyForecast),
                CalculatedAt = DateTime.UtcNow
            };

            user.Insights.Add(insight);
            context.AstrologyInsights.Add(insight);
            await context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                data = new
                {
                    userId = user.Id,
                    zodiacSign = user.ZodiacSign,
                    chineseZodiac = user.ChineseZodiac,
                    insights = personalityInsights,
                    monthlyForecast
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating insights.");
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("compatibility")]
    public async Task<IActionResult> CheckCompatibility([FromBody] CompatibilityRequest request)
    {
        try
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var user = await context.Users.FindAsync(userId.Value);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var compatibility = astrologyService.CalculateCompatibility(user.ZodiacSign, request.TargetZodiacSign);

            var match = new CompatibilityMatch
            {
                UserId = user.Id,
                TargetZodiacSign = request.TargetZodiacSign,
                CompatibilityPercentage = compatibility,
                CompatibilityDescription = $"Compatibility between {user.ZodiacSign} and {request.TargetZodiacSign} is {compatibility}%",
                RelationshipTips = "Communicate openly and respect each other's space.",
                CalculatedAt = DateTime.UtcNow
            };

            context.CompatibilityMatches.Add(match);
            await context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                compatibility,
                yourZodiac = user.ZodiacSign,
                targetZodiac = request.TargetZodiacSign
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking compatibility.");
            return BadRequest(new { message = ex.Message });
        }
    }
}

public sealed class InsightRequest
{
    public DateTime DateOfBirth { get; set; }
}

public sealed class CompatibilityRequest
{
    public string TargetZodiacSign { get; set; } = string.Empty;
}
