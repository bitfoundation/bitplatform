using Boilerplate.Shared.Dtos.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Boilerplate.Client.Core.Data;

public class OfflineDbContext(DbContextOptions<OfflineDbContext> options) : DbContext(options)
{
    static OfflineDbContext()
    {
        if (OperatingSystem.IsBrowser())
        {
            AppContext.SetSwitch("Microsoft.EntityFrameworkCore.Issue31751", true);
        }
    }

    public virtual DbSet<UserDto> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserDto>()
            .HasData([new()
            {
                Id = 1,
                Email = "test@bitplatform.dev",
                UserName = "test",
                PhoneNumber = "+31684207362",
                BirthDate = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero),
                Gender = Gender.Other,
                Password = "123456",
                FullName = "Boilerplate test account"
            }]);

        base.OnModelCreating(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var dirPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Boilerplate-Data");

        Directory.CreateDirectory(dirPath);

        var dbPath = Path.Combine(dirPath, "ClientDb.db");

        optionsBuilder
            // .UseModel(OfflineDbContextModel.Instance)
            .UseSqlite($"Data Source={dbPath}");

        base.OnConfiguring(optionsBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite does not support expressions of type 'DateTimeOffset' in ORDER BY clauses. Convert the values to a supported type:
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToBinaryConverter>();
        configurationBuilder.Properties<DateTimeOffset?>().HaveConversion<DateTimeOffsetToBinaryConverter>();
    }
}
