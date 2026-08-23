using IPad.Model;
using Microsoft.EntityFrameworkCore;

namespace Token2Core.Data;

public class TokenDbContext : DbContext
{
    public TokenDbContext(DbContextOptions<TokenDbContext> options)
        : base(options)
    {
    }

    public DbSet<RefreshTokem> RefreshTokems { get; set; }
    public DbSet<User> Users { get; set; }
}