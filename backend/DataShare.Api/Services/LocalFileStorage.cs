using System.Security.Cryptography;

namespace DataShare.Api.Services;

public class LocalFileStorage : IFileStorage
{
    private readonly string _root;

    public LocalFileStorage(IWebHostEnvironment env)
    {
        _root = Path.Combine(env.ContentRootPath, "Storage", "Uploads");
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(Stream content, string originalFileName, CancellationToken ct)
    {
        var ext = Path.GetExtension(originalFileName);
        if (ext.Length > 20) ext = ""; // sécurité simple

        var stored = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(_root, stored);

        await using var fs = File.Create(fullPath);
        await content.CopyToAsync(fs, ct);

        return stored;
    }

    public Task DeleteAsync(string storedFileName, CancellationToken ct)
    {
        var fullPath = Path.Combine(_root, storedFileName);
        if (File.Exists(fullPath))
            File.Delete(fullPath);

        return Task.CompletedTask;
    }
}
