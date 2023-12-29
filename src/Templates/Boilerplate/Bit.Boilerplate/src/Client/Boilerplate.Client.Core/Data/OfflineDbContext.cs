using Boilerplate.Shared.Dtos.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Boilerplate.Client.Core.Data;

public class OfflineDbContext(DbContextOptions<OfflineDbContext> options) : DbContext(options)
{
    static OfflineDbContext()
    {
        AppContext.SetSwitch("Microsoft.EntityFrameworkCore.Issue31751", true);
    }

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
        optionsBuilder
            // .UseModel(OfflineDbContextModel.Instance)
            .UseSqlite("Data Source=Boilerplate-ClientDb.db");

        base.OnConfiguring(optionsBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToBinaryConverter>();
        configurationBuilder.Properties<DateTimeOffset?>().HaveConversion<DateTimeOffsetToBinaryConverter>();
    }
}
