using System.ComponentModel.DataAnnotations;

namespace DataShare.Api.Models;

public class FileItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid OwnerId { get; set; }
    public AppUser? Owner { get; set; }

    // Nom original (affichage)
    [Required]
    [MaxLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;

    // Nom stocké sur disque (uuid.ext)
    [Required]
    [MaxLength(255)]
    public string StoredFileName { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? ContentType { get; set; }

    public long SizeBytes { get; set; }

    // Lien public (token)
    [Required]
    [MaxLength(128)]
    public string Token { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; }

    // Password optionnel (hashé)
    public string? PasswordHash { get; set; }

    // Tags optionnels (Postgres: text[])
    public string[] Tags { get; set; } = Array.Empty<string>();

    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
    public bool IsPasswordProtected => !string.IsNullOrWhiteSpace(PasswordHash);
}
