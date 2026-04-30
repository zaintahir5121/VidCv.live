using aibabag.Models;
using System.Security.Cryptography;

namespace aibabag.Services;

public interface IAstrologyService
{
    string GetZodiacSign(DateTime dateOfBirth);
    string GetChineseZodiac(DateTime dateOfBirth);
    Task<Dictionary<string, object>> CalculatePersonalityInsights(User user, string photoSignal, CancellationToken cancellationToken = default);
    int CalculateCompatibility(string zodiacSign1, string zodiacSign2);
    Task<Dictionary<string, string>> GetMonthlyForecast(string zodiacSign, string photoSignal, CancellationToken cancellationToken = default);
    string CreatePhotoSignal(User user);
}

public sealed class AstrologyService(
    IAiTextService aiTextService,
    ILogger<AstrologyService> logger) : IAstrologyService
{
    public string GetZodiacSign(DateTime dateOfBirth)
    {
        var month = dateOfBirth.Month;
        var day = dateOfBirth.Day;
        return (month, day) switch
        {
            (3, >= 21) or (4, <= 19) => "Aries",
            (4, >= 20) or (5, <= 20) => "Taurus",
            (5, >= 21) or (6, <= 20) => "Gemini",
            (6, >= 21) or (7, <= 22) => "Cancer",
            (7, >= 23) or (8, <= 22) => "Leo",
            (8, >= 23) or (9, <= 22) => "Virgo",
            (9, >= 23) or (10, <= 22) => "Libra",
            (10, >= 23) or (11, <= 21) => "Scorpio",
            (11, >= 22) or (12, <= 21) => "Sagittarius",
            (12, >= 22) or (1, <= 19) => "Capricorn",
            (1, >= 20) or (2, <= 18) => "Aquarius",
            _ => "Pisces"
        };
    }

    public string GetChineseZodiac(DateTime dateOfBirth)
    {
        var zodiacs = new[]
        {
            "Rat", "Ox", "Tiger", "Rabbit", "Dragon", "Snake",
            "Horse", "Goat", "Monkey", "Rooster", "Dog", "Pig"
        };
        var index = (dateOfBirth.Year - 1900) % 12;
        return zodiacs[index];
    }

    public async Task<Dictionary<string, object>> CalculatePersonalityInsights(User user, string photoSignal, CancellationToken cancellationToken = default)
    {
        var zodiac = user.ZodiacSign;
        var baselineTraits = GetTraits(zodiac);
        var baselineLuckyNumbers = GetLuckyNumbers(zodiac);
        var baselineLuckyColor = GetLuckyColor(zodiac);
        var baselineElement = GetElement(zodiac);

        var aiSummary = await aiTextService.GenerateAsync(
            $"You are an astrology assistant. For zodiac sign {zodiac} with visual signal '{photoSignal}', provide 3 concise lines in this exact format:\n" +
            "healthInsights: ...\ncareerInsights: ...\nloveInsights: ...",
            $"healthInsights: As a {zodiac}, prioritize consistent routines and healthy recovery.\n" +
            $"careerInsights: {zodiac} strengths support leadership, communication, and focused execution.\n" +
            $"loveInsights: {zodiac} energy supports meaningful and emotionally honest relationships.",
            cancellationToken);

        var parsedSummary = ParseKeyValueLines(aiSummary);

        return new Dictionary<string, object>
        {
            ["zodiacSign"] = zodiac,
            ["personalityTraits"] = baselineTraits,
            ["luckyNumbers"] = baselineLuckyNumbers,
            ["luckyColor"] = baselineLuckyColor,
            ["element"] = baselineElement,
            ["healthInsights"] = GetOrFallback(parsedSummary, "healthInsights", $"As a {zodiac}, prioritize consistent routines and healthy recovery."),
            ["careerInsights"] = GetOrFallback(parsedSummary, "careerInsights", $"{zodiac} strengths support leadership, communication, and focused execution."),
            ["loveInsights"] = GetOrFallback(parsedSummary, "loveInsights", $"{zodiac} energy supports meaningful and emotionally honest relationships.")
        };
    }

    public int CalculateCompatibility(string zodiacSign1, string zodiacSign2)
    {
        if (string.Equals(zodiacSign1, zodiacSign2, StringComparison.OrdinalIgnoreCase))
        {
            return 100;
        }

        var fire = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Aries", "Leo", "Sagittarius" };
        var earth = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Taurus", "Virgo", "Capricorn" };
        var air = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Gemini", "Libra", "Aquarius" };
        var water = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Cancer", "Scorpio", "Pisces" };

        if ((fire.Contains(zodiacSign1) && air.Contains(zodiacSign2)) ||
            (air.Contains(zodiacSign1) && fire.Contains(zodiacSign2)) ||
            (earth.Contains(zodiacSign1) && water.Contains(zodiacSign2)) ||
            (water.Contains(zodiacSign1) && earth.Contains(zodiacSign2)))
        {
            return 85;
        }

        return 68;
    }

    public async Task<Dictionary<string, string>> GetMonthlyForecast(string zodiacSign, string photoSignal, CancellationToken cancellationToken = default)
    {
        var aiForecast = await aiTextService.GenerateAsync(
            $"Give a monthly forecast for zodiac sign {zodiacSign} with visual signal '{photoSignal}'. Return exactly 4 lines in this exact format:\n" +
            "love: ...\ncareer: ...\nhealth: ...\nfinance: ...",
            $"love: This month supports emotional clarity and balanced expectations for {zodiacSign}.\n" +
            "career: Steady progress comes from consistency and practical decisions.\n" +
            "health: Protect energy with better sleep, hydration, and sustainable routines.\n" +
            "finance: Controlled spending and long-term planning are favored.",
            cancellationToken);

        var parsed = ParseKeyValueLines(aiForecast);

        return new Dictionary<string, string>
        {
            ["love"] = GetOrFallback(parsed, "love", $"This month supports emotional clarity and balanced expectations for {zodiacSign}."),
            ["career"] = GetOrFallback(parsed, "career", "Steady progress comes from consistency and practical decisions."),
            ["health"] = GetOrFallback(parsed, "health", "Protect energy with better sleep, hydration, and sustainable routines."),
            ["finance"] = GetOrFallback(parsed, "finance", "Controlled spending and long-term planning are favored.")
        };
    }

    public string CreatePhotoSignal(User user)
    {
        if (user.PhotoData is null || user.PhotoData.Length == 0)
        {
            return "photo-unavailable";
        }

        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(user.PhotoData);
        var token = Convert.ToHexString(hash)[..12].ToLowerInvariant();
        var brightnessBucket = user.PhotoData.Take(1000).DefaultIfEmpty((byte)0).Average(v => v);
        var tone = brightnessBucket switch
        {
            < 85 => "moody",
            < 170 => "balanced",
            _ => "bright"
        };
        return $"face-{tone}-{token}";
    }

    private static string GetTraits(string zodiac) => zodiac switch
    {
        "Aries" => "Courageous, Energetic, Direct",
        "Taurus" => "Reliable, Practical, Patient",
        "Gemini" => "Curious, Communicative, Adaptive",
        "Cancer" => "Caring, Protective, Intuitive",
        "Leo" => "Confident, Creative, Generous",
        "Virgo" => "Analytical, Precise, Helpful",
        "Libra" => "Diplomatic, Fair, Social",
        "Scorpio" => "Passionate, Strategic, Determined",
        "Sagittarius" => "Optimistic, Adventurous, Honest",
        "Capricorn" => "Ambitious, Disciplined, Responsible",
        "Aquarius" => "Independent, Visionary, Intellectual",
        _ => "Compassionate, Artistic, Sensitive"
    };

    private static string GetLuckyNumbers(string zodiac) => zodiac switch
    {
        "Aries" => "1, 9, 10",
        "Taurus" => "2, 6, 9",
        "Gemini" => "3, 5, 6",
        "Cancer" => "2, 7, 8",
        "Leo" => "1, 4, 5",
        "Virgo" => "5, 6, 7",
        "Libra" => "2, 7, 9",
        "Scorpio" => "2, 4, 8",
        "Sagittarius" => "3, 9, 12",
        "Capricorn" => "4, 8, 10",
        "Aquarius" => "4, 7, 11",
        _ => "3, 7, 12"
    };

    private static string GetLuckyColor(string zodiac) => zodiac switch
    {
        "Aries" => "Red",
        "Taurus" => "Green",
        "Gemini" => "Yellow",
        "Cancer" => "Silver",
        "Leo" => "Gold",
        "Virgo" => "Olive",
        "Libra" => "Blue",
        "Scorpio" => "Maroon",
        "Sagittarius" => "Purple",
        "Capricorn" => "Brown",
        "Aquarius" => "Teal",
        _ => "Sea Green"
    };

    private static string GetElement(string zodiac) => zodiac switch
    {
        "Aries" or "Leo" or "Sagittarius" => "Fire",
        "Taurus" or "Virgo" or "Capricorn" => "Earth",
        "Gemini" or "Libra" or "Aquarius" => "Air",
        _ => "Water"
    };

    private static Dictionary<string, string> ParseKeyValueLines(string input)
    {
        var output = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(input))
        {
            return output;
        }

        var lines = input.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var line in lines)
        {
            var separatorIndex = line.IndexOf(':');
            if (separatorIndex <= 0 || separatorIndex >= line.Length - 1)
            {
                continue;
            }

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();
            if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
            {
                output[key] = value;
            }
        }

        return output;
    }

    private string GetOrFallback(Dictionary<string, string> parsed, string key, string fallback)
    {
        if (parsed.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        logger.LogWarning("Free AI response missing key '{Key}', using fallback.", key);
        return fallback;
    }
}
