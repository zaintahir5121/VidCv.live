namespace aibabag.Services;

public sealed class AiProviderOptions
{
    public string Provider { get; set; } = "Pollinations";
    public string BaseUrl { get; set; } = "https://text.pollinations.ai";
    public int TimeoutSeconds { get; set; } = 20;
    public bool Enabled { get; set; } = true;
}
