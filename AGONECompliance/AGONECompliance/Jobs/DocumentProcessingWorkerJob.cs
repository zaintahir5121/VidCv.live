using AGONECompliance.Services;
using Quartz;

namespace AGONECompliance.Jobs;

[DisallowConcurrentExecution]
public sealed class DocumentProcessingWorkerJob(
    IDocumentProcessingOrchestrator orchestrator,
    ILogger<DocumentProcessingWorkerJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogDebug("Document processing worker tick at {UtcNow}", DateTimeOffset.UtcNow);
        await orchestrator.ProcessNextPendingJobAsync(context.CancellationToken);
    }
}
