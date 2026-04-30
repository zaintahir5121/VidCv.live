using System.ComponentModel.DataAnnotations;

namespace aibabag.Models;

public sealed class User
{
    public int Id { get; set; }

    [MaxLength(128)]
    public string GoogleId { get; set; } = string.Empty;

    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(256)]
    public string FullName { get; set; } = string.Empty;

    public DateTime? DateOfBirth { get; set; }

    [MaxLength(1024)]
    public string ProfileImageUrl { get; set; } = string.Empty;

    public DateTime? GoogleBirthday { get; set; }

    [MaxLength(64)]
    public string BirthDateSource { get; set; } = string.Empty;

    [MaxLength(256)]
    public string BirthDateRawText { get; set; } = string.Empty;

    public byte[]? PhotoData { get; set; }

    [MaxLength(64)]
    public string ZodiacSign { get; set; } = string.Empty;

    [MaxLength(64)]
    public string ChineseZodiac { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<AstrologyInsight> Insights { get; set; } = [];
    public ICollection<CompatibilityMatch> CompatibilityMatches { get; set; } = [];
    public ICollection<DetailedAstrologyInsight> DetailedInsights { get; set; } = [];
}

