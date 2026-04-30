using aibabag.Models;

namespace aibabag.Services;

public interface IAstrologyService
{
    string GetZodiacSign(DateTime dateOfBirth);
    string GetChineseZodiac(DateTime dateOfBirth);
    Dictionary<string, object> CalculatePersonalityInsights(User user);
    int CalculateCompatibility(string zodiacSign1, string zodiacSign2);
    Dictionary<string, string> GetMonthlyForecast(string zodiacSign);
}

public class AstrologyService : IAstrologyService
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

    public Dictionary<string, object> CalculatePersonalityInsights(User user)
    {
        return new Dictionary<string, object>
        {
            ["zodiacSign"] = user.ZodiacSign,
            ["personalityTraits"] = GetTraits(user.ZodiacSign),
            ["luckyNumbers"] = GetLuckyNumbers(user.ZodiacSign),
            ["luckyColor"] = GetLuckyColor(user.ZodiacSign),
            ["element"] = GetElement(user.ZodiacSign),
            ["healthInsights"] = $"As a {user.ZodiacSign}, focus on consistency and rest.",
            ["careerInsights"] = $"Your {user.ZodiacSign} strengths support leadership and growth.",
            ["loveInsights"] = $"{user.ZodiacSign} energy attracts deep and meaningful connections."
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

    public Dictionary<string, string> GetMonthlyForecast(string zodiacSign)
    {
        return new Dictionary<string, string>
        {
            ["love"] = $"This month supports emotional clarity for {zodiacSign}.",
            ["career"] = "You may see progress through focused execution.",
            ["health"] = "Protect your energy and keep routines stable.",
            ["finance"] = "Budget discipline and steady decisions are favored."
        };
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
}
