namespace aibabag.Services;

public interface IAiTextService
{
    Task<string> GenerateAsync(string prompt, string fallback, CancellationToken cancellationToken = default);
}
