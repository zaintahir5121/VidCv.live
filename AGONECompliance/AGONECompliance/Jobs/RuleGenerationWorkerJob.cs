using AGONECompliance.Services;
using Quartz;

namespace AGONECompliance.Jobs;

[DisallowConcurrentExecution]
public sealed class RuleGenerationWorkerJob(
    IRuleGenerationOrchestrator orchestrator,
    ILogger<RuleGenerationWorkerJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogDebug("Rule generation worker tick at {UtcNow}", DateTimeOffset.UtcNow);
        await orchestrator.ProcessNextPendingJobAsync(context.CancellationToken);
    }
}
