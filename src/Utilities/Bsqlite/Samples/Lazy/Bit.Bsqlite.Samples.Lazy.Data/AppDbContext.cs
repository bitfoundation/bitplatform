using Bit.Bsqlite.Samples.Lazy.Data.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Bit.Bsqlite.Samples.Lazy.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfiguration(new ProductConfiguration());
}
