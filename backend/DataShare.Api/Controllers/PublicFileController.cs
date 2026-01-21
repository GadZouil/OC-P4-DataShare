using DataShare.Api.Data;
using DataShare.Api.Models;
using DataShare.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace DataShare.Api.Controllers;

[ApiController]
[Route("api/public/files")]
public class PublicFilesController : ControllerBase
{
    private readonly DataShareDbContext _db;
    private readonly IFileStorage _storage;
    private const long MaxBytes = 1_073_741_824; // 1 Go


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
    public class UploadRequest
    {
        public IFormFile? File { get; set; }
        public int ExpiresInDays { get; set; } = 7;
        public string? Password { get; set; }
        public string[]? Tags { get; set; }
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
    
    // POST /api/public/files
    [HttpPost]
    [RequestSizeLimit(MaxBytes)]
    public async Task<IActionResult> UploadAnonymous([FromForm] UploadRequest req, CancellationToken ct)
    {
        // US07: uniquement non-authentifiés
        if (User?.Identity?.IsAuthenticated == true)
            return Forbid();

        if (req.File is null || req.File.Length <= 0)
            return BadRequest("File is required.");

        if (req.File.Length > MaxBytes)
            return BadRequest("File exceeds 1 GB.");

        if (req.ExpiresInDays is < 1 or > 7)
            return BadRequest("ExpiresInDays must be between 1 and 7.");

        if (!string.IsNullOrWhiteSpace(req.Password) && req.Password.Trim().Length < 6)
            return BadRequest("Password must be at least 6 characters.");

        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var token = WebEncoders.Base64UrlEncode(tokenBytes);

        var storedName = await _storage.SaveAsync(req.File.OpenReadStream(), req.File.FileName, ct);

        var item = new FileItem
        {
            OwnerId = null,
            OriginalFileName = req.File.FileName,
            StoredFileName = storedName,
            ContentType = req.File.ContentType,
            SizeBytes = req.File.Length,
            Token = token,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(req.ExpiresInDays),
            Tags = (req.Tags ?? Array.Empty<string>())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(20)
                .ToArray()
        };

        if (!string.IsNullOrWhiteSpace(req.Password))
        {
            var hasher = new PasswordHasher<FileItem>();
            item.PasswordHash = hasher.HashPassword(item, req.Password.Trim());
        }

        _db.Files.Add(item);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetMeta), new { token = item.Token }, new
        {
            item.Id,
            item.OriginalFileName,
            item.SizeBytes,
            item.ContentType,
            item.CreatedAt,
            item.ExpiresAt,
            item.Token,
            passwordRequired = item.IsPasswordProtected,
            item.Tags
        });
    }

}
