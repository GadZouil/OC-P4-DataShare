using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using DataShare.Api.Models;

namespace DataShare.Api.Data;

public class DataShareDbContext : IdentityDbContext<AppUser, Microsoft.AspNetCore.Identity.IdentityRole<Guid>, Guid>
{
    public DataShareDbContext(DbContextOptions<DataShareDbContext> options) : base(options) { }
}
