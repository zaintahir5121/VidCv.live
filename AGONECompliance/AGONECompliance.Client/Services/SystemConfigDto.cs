namespace AGONECompliance.Client.Services;

public sealed class SystemConfigDto
{
    public bool AzureBlobConfigured { get; set; }
    public bool DocumentIntelligenceConfigured { get; set; }
    public string DocumentIntelligenceModelId { get; set; } = string.Empty;
    public bool OpenAiConfigured { get; set; }
    public bool AiSearchConfigured { get; set; }
    public string OpenAiDeployment { get; set; } = string.Empty;
}
