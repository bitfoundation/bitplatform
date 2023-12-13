using Boilerplate.Shared.Dtos.Identity;
using Microsoft.EntityFrameworkCore;

namespace Boilerplate.Client.Core.Data;

public class ClientDbContext(DbContextOptions<ClientDbContext> options) : DbContext(options)
{
    public virtual DbSet<UserDto> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserDto>()
            .HasData([new()
            {
                Id = 1,
                UserName= "test@bitplatform.dev",
                Email = "test@bitplatform.dev",
                Password = "123456",
                FullName = "Boilerplate test account"
            }]);

        base.OnModelCreating(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=Boilerplate-ClientDb.db");

        base.OnConfiguring(optionsBuilder);
    }
}
