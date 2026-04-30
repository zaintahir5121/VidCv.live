namespace aibabag.Models;

public class AstrologyInsight
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

    public string PersonalityTraits { get; set; } = string.Empty;
    public string LuckyNumbers { get; set; } = string.Empty;
    public string LuckyColor { get; set; } = string.Empty;
    public string Element { get; set; } = string.Empty;
    public string HealthInsights { get; set; } = string.Empty;
    public string CareerInsights { get; set; } = string.Empty;
    public string LoveInsights { get; set; } = string.Empty;
    public string MonthlyForecast { get; set; } = string.Empty;
}
