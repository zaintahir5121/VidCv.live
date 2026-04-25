namespace AGONECompliance.Options;

public sealed class ExperionOptions
{
    public const string SectionName = "Experion";

    public bool EnforceSourceValidation { get; set; } = false;
    public List<string> AllowedProducts { get; set; } =
    [
        "work",
        "learn",
        "safe",
        "hire",
        "spot",
        "sentiment"
    ];

    public List<string> AllowedOrigins { get; set; } = [];
}
