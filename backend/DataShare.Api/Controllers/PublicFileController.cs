using DataShare.Api.Data;
using DataShare.Api.Models;
using DataShare.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DataShare.Api.Controllers;

[ApiController]
[Route("api/public/files")]
public class PublicFilesController : ControllerBase
{
    private readonly DataShareDbContext _db;
    private readonly IFileStorage _storage;

    public PublicFilesController(DataShareDbContext db, IFileStorage storage)
    {
        _db = db;
        _storage = storage;
    }

    [HttpGet("{token}")]
    public async Task<IActionResult> GetMeta(string token, CancellationToken ct)
    {
        var item = await _db.Files.AsNoTracking()
            .Where(x => x.Token == token)
            .Select(x => new
            {
                x.Id,
                x.OriginalFileName,
                x.SizeBytes,
                x.ContentType,
                x.CreatedAt,
                x.ExpiresAt,
                passwordRequired = x.PasswordHash != null
            })
            .FirstOrDefaultAsync(ct);

        if (item is null) return NotFound();

        if (DateTimeOffset.UtcNow >= item.ExpiresAt)
            return StatusCode(StatusCodes.Status410Gone, new { message = "Link expired." });

        return Ok(item);
    }

    public class DownloadRequest
    {
        public string? Password { get; set; }
    }

    [HttpPost("{token}/download")]
    public async Task<IActionResult> Download(string token, [FromBody] DownloadRequest req, CancellationToken ct)
    {
        var file = await _db.Files.FirstOrDefaultAsync(x => x.Token == token, ct);
        if (file is null) return NotFound();

        if (DateTimeOffset.UtcNow >= file.ExpiresAt)
            return StatusCode(StatusCodes.Status410Gone, new { message = "Link expired." });

        if (!string.IsNullOrWhiteSpace(file.PasswordHash))
        {
            if (string.IsNullOrWhiteSpace(req.Password))
                return Unauthorized(new { message = "Password required." });

            var hasher = new PasswordHasher<FileItem>();
            var result = hasher.VerifyHashedPassword(file, file.PasswordHash, req.Password);
            if (result == PasswordVerificationResult.Failed)
                return Unauthorized(new { message = "Invalid password." });
        }

        var (stream, contentType) = await _storage.OpenReadAsync(file.StoredFileName, file.ContentType, ct);
        return File(stream, contentType, file.OriginalFileName);
    }
}
