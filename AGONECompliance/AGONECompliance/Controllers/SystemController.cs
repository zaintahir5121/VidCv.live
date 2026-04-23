using AGONECompliance.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AGONECompliance.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class SystemController(IOptions<AzureOptions> azureOptions) : ControllerBase
{
    [HttpGet("configuration")]
    public ActionResult<object> GetConfiguration()
    {
        var options = azureOptions.Value;
        return Ok(new
        {
            azureBlobConfigured = !string.IsNullOrWhiteSpace(options.BlobStorage.ConnectionString),
            documentIntelligenceConfigured = !string.IsNullOrWhiteSpace(options.DocumentIntelligence.Endpoint)
                                             && !string.IsNullOrWhiteSpace(options.DocumentIntelligence.ApiKey),
            documentIntelligenceModelId = options.DocumentIntelligence.ModelId,
            openAiConfigured = !string.IsNullOrWhiteSpace(options.OpenAi.Endpoint)
                               && !string.IsNullOrWhiteSpace(options.OpenAi.ApiKey)
                               && !string.IsNullOrWhiteSpace(options.OpenAi.DeploymentName),
            aiSearchConfigured = !string.IsNullOrWhiteSpace(options.AiSearch.Endpoint)
                                 && !string.IsNullOrWhiteSpace(options.AiSearch.ApiKey)
                                 && !string.IsNullOrWhiteSpace(options.AiSearch.IndexName),
            openAiDeployment = options.OpenAi.DeploymentName
        });
    }
}
