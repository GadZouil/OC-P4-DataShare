using System.Security.Claims;
using System.Security.Cryptography;
using DataShare.Api.Data;
using DataShare.Api.Models;
using DataShare.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace DataShare.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FilesController : ControllerBase
{
    private const long MaxBytes = 1_073_741_824; // 1 Go
    private readonly DataShareDbContext _db;
    private readonly IFileStorage _storage;
    private readonly UserManager<AppUser> _users;

    public FilesController(DataShareDbContext db, IFileStorage storage, UserManager<AppUser> users)
    {
        _db = db;
        _storage = storage;
        _users = users;
    }

    public class UploadRequest
    {
        public IFormFile? File { get; set; }
        public int ExpiresInDays { get; set; } = 7;
        public string? Password { get; set; }
        public string[]? Tags { get; set; }
    }

    [HttpPost]
    [RequestSizeLimit(MaxBytes)]
    public async Task<IActionResult> Upload([FromForm] UploadRequest req, CancellationToken ct)
    {
        if (req.File is null || req.File.Length <= 0)
            return BadRequest("File is required.");

        if (req.File.Length > MaxBytes)
            return BadRequest("File exceeds 1 GB.");

        if (req.ExpiresInDays is < 1 or > 7)
            return BadRequest("ExpiresInDays must be between 1 and 7.");

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();

        // Token URL-safe
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var token = WebEncoders.Base64UrlEncode(tokenBytes);

        // Stockage disque
        var storedName = await _storage.SaveAsync(req.File.OpenReadStream(), req.File.FileName, ct);

        var item = new FileItem
        {
            OwnerId = userId,
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
            item.PasswordHash = hasher.HashPassword(item, req.Password);
        }

        _db.Files.Add(item);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = item.Id }, new
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

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var item = await _db.Files
            .AsNoTracking()
            .Where(x => x.Id == id && x.OwnerId == userId)
            .Select(x => new
            {
                x.Id,
                x.OriginalFileName,
                x.SizeBytes,
                x.ContentType,
                x.CreatedAt,
                x.ExpiresAt,
                x.Token,
                passwordRequired = x.PasswordHash != null,
                x.Tags
            })
            .FirstOrDefaultAsync(ct);

        return item is null ? NotFound() : Ok(item);
    }
}
