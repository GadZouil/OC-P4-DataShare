using DataShare.Api.Services;
using System.Collections.Concurrent;

namespace DataShare.Api.Tests.Helpers;

public class FakeFileStorage : IFileStorage
{
    private readonly ConcurrentDictionary<string, byte[]> _memoryStorage = new();

    public Task<string> SaveAsync(Stream content, string originalFileName, CancellationToken ct)
    {
        var ext = Path.GetExtension(originalFileName);
        var storedName = $"{Guid.NewGuid():N}{ext}";

        using var ms = new MemoryStream();
        content.CopyTo(ms);
        _memoryStorage[storedName] = ms.ToArray();

        return Task.FromResult(storedName);
    }

    public Task DeleteAsync(string storedFileName, CancellationToken ct)
    {
        _memoryStorage.TryRemove(storedFileName, out _);
        return Task.CompletedTask;
    }

    public Task<(Stream Stream, string ContentType)> OpenReadAsync(string storedFileName, string? contentType, CancellationToken ct)
    {
        if (_memoryStorage.TryGetValue(storedFileName, out var bytes))
        {
            var stream = new MemoryStream(bytes);
            return Task.FromResult((Stream: (Stream)stream, ContentType: contentType ?? "application/octet-stream"));
        }
        
        throw new FileNotFoundException("Fichier introuvable dans le FakeStorage");
    }
}
