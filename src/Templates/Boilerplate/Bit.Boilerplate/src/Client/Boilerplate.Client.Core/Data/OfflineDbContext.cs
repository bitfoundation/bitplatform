using Boilerplate.Shared.Dtos.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Options;

namespace Boilerplate.Client.Core.Data;

public partial class OfflineDbContext : DbContext
{
    public virtual DbSet<UserDto> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserDto>()
            .HasData([new()
            {
                Id = Guid.Parse("8ff71671-a1d6-4f97-abb9-d87d7b47d6e7"),
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

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite does not support expressions of type 'DateTimeOffset' in ORDER BY clauses. Convert the values to a supported type:
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToBinaryConverter>();
        configurationBuilder.Properties<DateTimeOffset?>().HaveConversion<DateTimeOffsetToBinaryConverter>();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var isRunningInsideDocker = Directory.Exists("/container_volume"); // Blazor Server - Docker (It's supposed to be a mounted volume named /container_volume)
        var dirPath = isRunningInsideDocker ? "/container_volume"
                                            : AppPlatform.IsBlazorHybridOrBrowser ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AC87AA5B-4B37-4E52-8468-2D5DF24AF256")
                                            : Directory.GetCurrentDirectory(); // Blazor server (Non docker Linux, macOS or Windows)

        dirPath = Path.Combine(dirPath, "App_Data");

        Directory.CreateDirectory(dirPath);

        var dbPath = Path.Combine(dirPath, "Offline.db");

        optionsBuilder
            .UseSqlite($"Data Source={dbPath}");

        if (AppEnvironment.IsProd())
        {
            optionsBuilder.UseModel(OfflineDbContextModel.Instance);
        }

        optionsBuilder.EnableSensitiveDataLogging(AppEnvironment.IsDev())
                .EnableDetailedErrors(AppEnvironment.IsDev());

        base.OnConfiguring(optionsBuilder);
    }
}
