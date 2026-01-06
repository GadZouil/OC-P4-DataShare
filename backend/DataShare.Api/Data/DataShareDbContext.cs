using DataShare.Api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DataShare.Api.Data;

public class DataShareDbContext : IdentityDbContext<AppUser, Microsoft.AspNetCore.Identity.IdentityRole<Guid>, Guid>
{
    public DataShareDbContext(DbContextOptions<DataShareDbContext> options) : base(options) { }

    public DbSet<FileItem> Files => Set<FileItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<FileItem>(e =>
        {
            e.HasIndex(x => x.Token).IsUnique();
            e.Property(x => x.Tags).HasColumnType("text[]");
        });
    }
}
