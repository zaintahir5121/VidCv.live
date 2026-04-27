namespace Experion.Backend.Models;

public sealed class ConversationThreadSummary
{
    public Guid ConversationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int MessageCount { get; set; }
    public DateTimeOffset LastOccurredAtUtc { get; set; }
}

public sealed class ConversationHistoryMessage
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid SessionId { get; set; }
    public string Role { get; set; } = "assistant";
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; set; }
}
