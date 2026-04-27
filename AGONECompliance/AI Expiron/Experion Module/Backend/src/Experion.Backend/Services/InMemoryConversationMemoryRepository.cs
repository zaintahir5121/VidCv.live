using Experion.Backend.Models;

namespace Experion.Backend.Services;

public sealed class InMemoryConversationMemoryRepository : IConversationMemoryRepository
{
    private readonly List<ConversationMemoryRecord> _records = [];
    private readonly object _sync = new();

    public Task SaveAsync(ConversationMemoryRecord record, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            _records.Add(Clone(record));
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ConversationHistoryMessage>> GetConversationHistoryAsync(
        string userId,
        string productCode,
        string workspaceId,
        Guid? conversationId,
        int take,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var limit = Math.Clamp(take, 1, 200);
        lock (_sync)
        {
            var filtered = _records
                .Where(x => x.UserId.Equals(userId, StringComparison.OrdinalIgnoreCase))
                .Where(x => x.ProductCode.Equals(productCode, StringComparison.OrdinalIgnoreCase))
                .Where(x => x.WorkspaceId.Equals(workspaceId, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.OccurredAtUtc)
                .ToList();
            var selectedConversationId = conversationId.HasValue && conversationId.Value != Guid.Empty
                ? conversationId.Value
                : filtered.LastOrDefault()?.ConversationId ?? Guid.Empty;

            if (selectedConversationId != Guid.Empty)
            {
                filtered = filtered
                    .Where(x => x.ConversationId == selectedConversationId)
                    .ToList();
            }

            if (filtered.Count > limit)
            {
                filtered = filtered.Skip(filtered.Count - limit).ToList();
            }

            var messages = new List<ConversationHistoryMessage>(filtered.Count * 2);
            foreach (var item in filtered)
            {
                messages.Add(new ConversationHistoryMessage
                {
                    Id = item.Id,
                    ConversationId = item.ConversationId,
                    SessionId = item.SessionId,
                    Role = "user",
                    Content = item.UserPrompt,
                    OccurredAtUtc = item.OccurredAtUtc
                });
                messages.Add(new ConversationHistoryMessage
                {
                    Id = DeriveGuid(item.Id, 67),
                    ConversationId = item.ConversationId,
                    SessionId = item.SessionId,
                    Role = "assistant",
                    Content = item.AssistantResponse,
                    OccurredAtUtc = item.OccurredAtUtc
                });
            }

            return Task.FromResult<IReadOnlyList<ConversationHistoryMessage>>(messages);
        }
    }

    public Task<IReadOnlyList<ConversationThreadSummary>> GetConversationSummaryAsync(
        string userId,
        string productCode,
        string workspaceId,
        int take,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var limit = Math.Clamp(take, 1, 100);
        lock (_sync)
        {
            var threads = _records
                .Where(x => x.UserId.Equals(userId, StringComparison.OrdinalIgnoreCase))
                .Where(x => x.ProductCode.Equals(productCode, StringComparison.OrdinalIgnoreCase))
                .Where(x => x.WorkspaceId.Equals(workspaceId, StringComparison.OrdinalIgnoreCase))
                .GroupBy(x => x.ConversationId)
                .Select(group =>
                {
                    var latest = group.OrderByDescending(x => x.OccurredAtUtc).First();
                    return new ConversationThreadSummary
                    {
                        ConversationId = latest.ConversationId,
                        Title = BuildTitle(latest.UserPrompt),
                        LastOccurredAtUtc = latest.OccurredAtUtc,
                        MessageCount = group.Count() * 2
                    };
                })
                .OrderByDescending(x => x.LastOccurredAtUtc)
                .Take(limit)
                .ToList();

            return Task.FromResult<IReadOnlyList<ConversationThreadSummary>>(threads);
        }
    }

    private static string BuildTitle(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            return "New chat";
        }

        var normalized = prompt.ReplaceLineEndings(" ").Trim();
        return normalized.Length <= 52 ? normalized : $"{normalized[..52].Trim()}...";
    }

    private static ConversationMemoryRecord Clone(ConversationMemoryRecord source)
    {
        return new ConversationMemoryRecord
        {
            Id = source.Id,
            ConversationId = source.ConversationId,
            SessionId = source.SessionId,
            UserId = source.UserId,
            ProductCode = source.ProductCode,
            WorkspaceId = source.WorkspaceId,
            UserPrompt = source.UserPrompt,
            CleanedDom = source.CleanedDom,
            ContextType = source.ContextType,
            ActionCode = source.ActionCode,
            ActionPayloadJson = source.ActionPayloadJson,
            ActionResultJson = source.ActionResultJson,
            AssistantResponse = source.AssistantResponse,
            ResponseLayer = source.ResponseLayer,
            PromptTokens = source.PromptTokens,
            DomTokens = source.DomTokens,
            CompletionTokens = source.CompletionTokens,
            CacheKey = source.CacheKey,
            OccurredAtUtc = source.OccurredAtUtc
        };
    }

    private static Guid DeriveGuid(Guid value, byte salt)
    {
        var bytes = value.ToByteArray();
        bytes[0] = (byte)(bytes[0] ^ salt);
        return new Guid(bytes);
    }
}
