namespace aibabag.Models;

public class CompatibilityMatch
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    public string TargetZodiacSign { get; set; } = string.Empty;
    public int CompatibilityPercentage { get; set; }
    public string CompatibilityDescription { get; set; } = string.Empty;
    public string RelationshipTips { get; set; } = string.Empty;
}
