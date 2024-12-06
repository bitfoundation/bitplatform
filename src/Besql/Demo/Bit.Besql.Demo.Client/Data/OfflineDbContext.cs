using Bit.Besql.Demo.Client.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Bit.Besql.Demo.Client.Data;

public class OfflineDbContext(DbContextOptions<OfflineDbContext> options)
    : DbContext(options)
{
    public DbSet<WeatherForecast> WeatherForecasts { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OfflineDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite does not support expressions of type 'DateTimeOffset' in ORDER BY clauses. Convert the values to a supported type:
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToBinaryConverter>();
        configurationBuilder.Properties<DateTimeOffset?>().HaveConversion<DateTimeOffsetToBinaryConverter>();
    }
}
