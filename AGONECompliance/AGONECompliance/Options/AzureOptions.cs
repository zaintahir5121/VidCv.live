namespace AGONECompliance.Options;

public sealed class AzureOptions
{
    public const string SectionName = "Azure";
    public BlobStorageOptions BlobStorage { get; set; } = new();
    public DocumentIntelligenceOptions DocumentIntelligence { get; set; } = new();
    public OpenAiOptions OpenAi { get; set; } = new();
    public AiSearchOptions AiSearch { get; set; } = new();
}

public sealed class BlobStorageOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "agone-documents";
}

public sealed class DocumentIntelligenceOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ModelId { get; set; } = "prebuilt-layout";
}

public sealed class OpenAiOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string DeploymentName { get; set; } = "gpt-4.1";
}

public sealed class AiSearchOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string IndexName { get; set; } = "agone-compliance-index";
}
