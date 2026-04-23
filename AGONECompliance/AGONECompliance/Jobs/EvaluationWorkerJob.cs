using AGONECompliance.Services;
using Quartz;

namespace AGONECompliance.Jobs;

[DisallowConcurrentExecution]
public sealed class EvaluationWorkerJob(IEvaluationOrchestrator orchestrator, ILogger<EvaluationWorkerJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogDebug("Evaluation worker tick at {UtcNow}", DateTimeOffset.UtcNow);
        await orchestrator.ProcessNextPendingRunAsync(context.CancellationToken);
    }
}
