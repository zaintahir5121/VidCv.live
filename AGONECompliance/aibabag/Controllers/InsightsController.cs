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
    IFamousBirthdayService famousBirthdayService,
    IAiTextService aiTextService,
    ILogger<InsightsController> logger) : ControllerBase
{
    [HttpPost("generate")]
    public async Task<IActionResult> GenerateInsights([FromBody] InsightRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var user = await context.Users.FindAsync([userId.Value], cancellationToken);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            user.DateOfBirth = request.DateOfBirth;
            user.ZodiacSign = astrologyService.GetZodiacSign(request.DateOfBirth);
            user.ChineseZodiac = astrologyService.GetChineseZodiac(request.DateOfBirth);
            user.UpdatedAtUtc = DateTime.UtcNow;

            var photoSignal = string.IsNullOrWhiteSpace(request.PhotoHint)
                ? astrologyService.CreatePhotoSignal(user)
                : request.PhotoHint!;
            var personalityInsights = await astrologyService.CalculatePersonalityInsights(user, photoSignal, cancellationToken);
            var monthlyForecast = await astrologyService.GetMonthlyForecast(user.ZodiacSign, photoSignal, cancellationToken);

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
            await context.SaveChangesAsync(cancellationToken);

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

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(CancellationToken cancellationToken)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var user = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId.Value, cancellationToken);
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        var history = await context.AstrologyInsights
            .AsNoTracking()
            .Where(x => x.UserId == userId.Value)
            .OrderByDescending(x => x.CalculatedAt)
            .Take(12)
            .Select(x => new
            {
                x.Id,
                x.CalculatedAt,
                x.PersonalityTraits,
                x.HealthInsights,
                x.CareerInsights,
                x.LoveInsights,
                x.Element,
                x.LuckyColor,
                user.ZodiacSign
            })
            .ToListAsync(cancellationToken);

        return Ok(new { success = true, history });
    }

    [HttpPost("compatibility")]
    public async Task<IActionResult> CheckCompatibility([FromBody] CompatibilityRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var user = await context.Users.FindAsync([userId.Value], cancellationToken);
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
            await context.SaveChangesAsync(cancellationToken);

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

    [HttpGet("famous-by-birthday")]
    public async Task<IActionResult> FamousByBirthday([FromQuery] DateTime? dob, CancellationToken cancellationToken)
    {
        if (!dob.HasValue)
        {
            return BadRequest(new { message = "Query parameter 'dob' is required. Example: ?dob=1996-05-12" });
        }

        var people = await famousBirthdayService.GetByDateAsync(dob.Value, cancellationToken);
        return Ok(new { success = true, people });
    }

    [HttpPost("ai-fallback")]
    public async Task<IActionResult> GenerateAiFallback([FromBody] AIFallbackRequest request, CancellationToken cancellationToken)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId is null)
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var zodiac = string.IsNullOrWhiteSpace(request.ZodiacSign) ? "Leo" : request.ZodiacSign.Trim();
        var dobText = string.IsNullOrWhiteSpace(request.DateOfBirth) ? "unknown" : request.DateOfBirth.Trim();
        var prompt =
            $"You are AibabaG, an AI that writes concise personality guidance.\n" +
            $"User zodiac: {zodiac}\nBirthday: {dobText}\n" +
            "Return exactly these keys with short friendly lines:\n" +
            "summary: ...\n" +
            "animalLine: ...\n" +
            "foodLine: ...\n" +
            "travelLine: ...\n" +
            "careerLine: ...\n" +
            "loveLine: ...\n" +
            "familyLine: ...\n" +
            "futureLine: ...\n" +
            "moneyLine: ...\n" +
            "educationLine: ...\n" +
            "healthLine: ...\n" +
            "purposeLine: ...";

        var fallback =
            $"summary: You are a natural {zodiac} leader with balanced heart and bold ideas.\n" +
            "animalLine: Confident and protective with strong instincts.\n" +
            "foodLine: You enjoy vibrant flavors and expressive choices.\n" +
            "travelLine: New places recharge your mind and motivation.\n" +
            "careerLine: Leadership and ownership roles suit you well.\n" +
            "loveLine: You value loyalty and emotional honesty.\n" +
            "familyLine: You bring stability and warmth to your circle.\n" +
            "futureLine: Your future opens through consistent self-growth.\n" +
            "moneyLine: Smart planning helps you build durable wealth.\n" +
            "educationLine: You learn quickly with practical application.\n" +
            "healthLine: Daily routines keep your energy centered.\n" +
            "purposeLine: You are here to inspire and uplift people.";

        var aiResult = await aiTextService.GenerateAsync(prompt, fallback, cancellationToken);
        var parsed = ParseAiKeyValue(aiResult);

        return Ok(new
        {
            success = true,
            data = new
            {
                summary = GetOrFallback(parsed, "summary", $"You are a natural {zodiac} leader with balanced heart and bold ideas."),
                animalLine = GetOrFallback(parsed, "animalLine", "Confident and protective with strong instincts."),
                foodLine = GetOrFallback(parsed, "foodLine", "You enjoy vibrant flavors and expressive choices."),
                travelLine = GetOrFallback(parsed, "travelLine", "New places recharge your mind and motivation."),
                careerLine = GetOrFallback(parsed, "careerLine", "Leadership and ownership roles suit you well."),
                loveLine = GetOrFallback(parsed, "loveLine", "You value loyalty and emotional honesty."),
                familyLine = GetOrFallback(parsed, "familyLine", "You bring stability and warmth to your circle."),
                futureLine = GetOrFallback(parsed, "futureLine", "Your future opens through consistent self-growth."),
                moneyLine = GetOrFallback(parsed, "moneyLine", "Smart planning helps you build durable wealth."),
                educationLine = GetOrFallback(parsed, "educationLine", "You learn quickly with practical application."),
                healthLine = GetOrFallback(parsed, "healthLine", "Daily routines keep your energy centered."),
                purposeLine = GetOrFallback(parsed, "purposeLine", "You are here to inspire and uplift people.")
            }
        });
    }

    private static Dictionary<string, string> ParseAiKeyValue(string value)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(value))
        {
            return result;
        }

        var lines = value.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var line in lines)
        {
            var separatorIndex = line.IndexOf(':');
            if (separatorIndex <= 0 || separatorIndex >= line.Length - 1)
            {
                continue;
            }

            var key = line[..separatorIndex].Trim();
            var content = line[(separatorIndex + 1)..].Trim();
            if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(content))
            {
                result[key] = content;
            }
        }

        return result;
    }

    private static string GetOrFallback(Dictionary<string, string> source, string key, string fallback)
    {
        if (source.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return fallback;
    }
}

public sealed class InsightRequest
{
    public DateTime DateOfBirth { get; set; }
    public string? PhotoHint { get; set; }
}

public sealed class CompatibilityRequest
{
    public string TargetZodiacSign { get; set; } = string.Empty;
}

public sealed class AIFallbackRequest
{
    public string? ZodiacSign { get; set; }
    public string? DateOfBirth { get; set; }
}
