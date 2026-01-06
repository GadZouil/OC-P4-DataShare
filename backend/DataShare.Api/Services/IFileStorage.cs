namespace DataShare.Api.Services;

public interface IFileStorage
{
    Task<string> SaveAsync(Stream content, string originalFileName, CancellationToken ct);
    Task DeleteAsync(string storedFileName, CancellationToken ct);
    Task<(Stream Stream, string ContentType)> OpenReadAsync(string storedFileName, string? contentType, CancellationToken ct);

}
