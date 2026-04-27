using Experion.Backend.Models;

namespace Experion.Backend.Services;

public sealed class InMemoryActionKnowledgeBaseRepository : IActionKnowledgeBaseRepository
{
    private static readonly List<ActionKnowledgeEntry> SeedEntries =
    [
        new()
        {
            Id = Guid.NewGuid(),
            ActionCode = "work.billing.update-payment-terms",
            ProductCode = "work",
            ApiRoute = "/api/billing/payment-terms",
            HttpMethod = "POST",
            TokenPattern = "payment terms,net 30,invoice,billing",
            Description = "Update payment terms for billing workflows."
        },
        new()
        {
            Id = Guid.NewGuid(),
            ActionCode = "safe.compliance.create-checklist",
            ProductCode = "safe",
            ApiRoute = "/api/compliance/checklists",
            HttpMethod = "POST",
            TokenPattern = "checklist,compliance,safety,inspection",
            Description = "Create compliance checklist from assistant context."
        },
        new()
        {
            Id = Guid.NewGuid(),
            ActionCode = "hire.candidate.schedule-interview",
            ProductCode = "hire",
            ApiRoute = "/api/candidates/interviews",
            HttpMethod = "POST",
            TokenPattern = "candidate,interview,schedule,recruitment",
            Description = "Schedule interview for selected candidate."
        },
        new()
        {
            Id = Guid.NewGuid(),
            ActionCode = "work.social.post-facebook",
            ProductCode = "work",
            ApiRoute = "/api/experion/actions/facebook-post",
            HttpMethod = "POST",
            TokenPattern = "facebook,post,social,share,publish",
            Description = "Publish topic post to Facebook page using configured API credentials."
        }
    ];

    public Task<IReadOnlyList<ActionKnowledgeEntry>> GetActiveEntriesAsync(
        string productCode,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedProduct = (productCode ?? string.Empty).Trim().ToLowerInvariant();
        var entries = SeedEntries
            .Where(x => x.IsActive && (x.ProductCode.Equals(normalizedProduct, StringComparison.OrdinalIgnoreCase) || x.ProductCode == "*"))
            .ToList();
        return Task.FromResult<IReadOnlyList<ActionKnowledgeEntry>>(entries);
    }

    public Task<ActionKnowledgeEntry?> ResolveBestMatchAsync(
        string productCode,
        IReadOnlyCollection<string> normalizedTokens,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var tokenSet = new HashSet<string>(normalizedTokens ?? [], StringComparer.OrdinalIgnoreCase);
        if (tokenSet.Count == 0)
        {
            return Task.FromResult<ActionKnowledgeEntry?>(null);
        }

        var entries = SeedEntries
            .Where(x => x.IsActive
                        && (x.ProductCode.Equals(productCode, StringComparison.OrdinalIgnoreCase)
                            || x.ProductCode == "*"))
            .ToList();

        ActionKnowledgeEntry? selected = null;
        var bestScore = 0;
        foreach (var entry in entries)
        {
            var patterns = entry.TokenPattern
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => x.ToLowerInvariant())
                .ToList();
            var score = patterns.Count(tokenSet.Contains);
            if (score > bestScore)
            {
                bestScore = score;
                selected = entry;
            }
        }

        return Task.FromResult(bestScore > 0 ? selected : null);
    }
}
