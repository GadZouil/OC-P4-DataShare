using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DataShare.Api.Services;

namespace DataShare.Api.Tests.Infrastructure;

public sealed class TestFileStorage : IFileStorage
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "datashare-tests",
        Guid.NewGuid().ToString("N"));

    public async Task<string> SaveAsync(Stream stream, string fileName, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_root);

        var stored = $"{Guid.NewGuid():N}_{Sanitize(fileName)}";
        var fullPath = Path.Combine(_root, stored);

        await using var fs = File.Create(fullPath);
        await stream.CopyToAsync(fs, cancellationToken);

        return stored;
    }

    // ✅ Signature attendue par ton interface :
    public Task<(Stream Stream, string ContentType)> OpenReadAsync(
        string storedPath,
        string? contentType,
        CancellationToken cancellationToken)
    {
        var fullPath = Path.Combine(_root, storedPath);

        Stream s = File.OpenRead(fullPath);
        var ct = string.IsNullOrWhiteSpace(contentType)
            ? "application/octet-stream"
            : contentType!;

        return Task.FromResult((Stream: s, ContentType: ct));
    }

    public Task DeleteAsync(string storedPath, CancellationToken cancellationToken)
    {
        var fullPath = Path.Combine(_root, storedPath);
        if (File.Exists(fullPath)) File.Delete(fullPath);
        return Task.CompletedTask;
    }

    private static string Sanitize(string name) =>
        string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
}
