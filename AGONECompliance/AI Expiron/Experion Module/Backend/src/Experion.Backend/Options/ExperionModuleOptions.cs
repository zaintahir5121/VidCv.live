namespace Experion.Backend.Options;

public sealed class ExperionModuleOptions
{
    public const string SectionName = "ExperionModule";

    public SecurityOptions Security { get; set; } = new();
    public OpenAiOptions OpenAi { get; set; } = new();
    public FacebookOptions Facebook { get; set; } = new();
    public CacheOptions Cache { get; set; } = new();
}

public sealed class SecurityOptions
{
    public bool RequireUserIdentity { get; set; } = true;
    public bool AllowAnonymousWithSourceToken { get; set; }
}

public sealed class OpenAiOptions
{
    public bool Enabled { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string DeploymentName { get; set; } = "gpt-4.1";
}

public sealed class FacebookOptions
{
    public bool Enabled { get; set; }
    public string GraphApiBaseUrl { get; set; } = "https://graph.facebook.com/v20.0";
    public string PageId { get; set; } = string.Empty;
    public string PageAccessToken { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 25;
}

public sealed class CacheOptions
{
    public string CacheMode { get; set; } = "InMemory";
    public string AzureSearchEndpoint { get; set; } = string.Empty;
    public string AzureSearchApiKey { get; set; } = string.Empty;
    public string AzureSearchIndexName { get; set; } = "experion-learning-cache";
    public bool EnableAzureSearchWrite { get; set; }
}
