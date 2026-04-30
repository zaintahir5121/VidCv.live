namespace aibabag.Services;

public interface IFamousBirthdayService
{
    Task<IReadOnlyList<FamousPersonality>> GetByDateAsync(DateTime dateOfBirth, CancellationToken cancellationToken = default);
}

public sealed class FamousPersonality
{
    public string Name { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string ArticleUrl { get; set; } = string.Empty;
}
