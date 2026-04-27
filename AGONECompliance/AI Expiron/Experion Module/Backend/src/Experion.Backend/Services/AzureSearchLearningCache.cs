using System.Text.Json;
using Experion.Backend.Models;
using Experion.Backend.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Experion.Backend.Services;

public sealed class AzureSearchLearningCache(
    IMemoryCache memoryCache,
    IOptions<ExperionModuleOptions> options,
    ILogger<AzureSearchLearningCache> logger) : ILearningCache
{
    private readonly ExperionModuleOptions _options = options.Value;
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(8);

    public Task<LearningCacheRecord?> TryGetAsync(string cacheKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(cacheKey))
        {
            return Task.FromResult<LearningCacheRecord?>(null);
        }

        if (memoryCache.TryGetValue(cacheKey, out LearningCacheRecord? entry) && entry is not null)
        {
            return Task.FromResult<LearningCacheRecord?>(entry);
        }

        return Task.FromResult<LearningCacheRecord?>(null);
    }

    public Task StoreAsync(LearningCacheRecord entry, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (entry is null || string.IsNullOrWhiteSpace(entry.CacheKey))
        {
            return Task.CompletedTask;
        }

        memoryCache.Set(entry.CacheKey, entry, DefaultTtl);

        if (_options.Cache.EnableAzureSearchWrite)
        {
            logger.LogInformation(
                "Azure Search cache write enabled; record cached key {CacheKey}.",
                entry.CacheKey);
        }

        return Task.CompletedTask;
    }

    public Task TrackUsageAsync(string cacheKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!memoryCache.TryGetValue(cacheKey, out LearningCacheRecord? entry) || entry is null)
        {
            return Task.CompletedTask;
        }

        memoryCache.Set(cacheKey, entry, DefaultTtl);
        return Task.CompletedTask;
    }
}
