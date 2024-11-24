using Boilerplate.Shared.Dtos.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Boilerplate.Client.Core.Data;

public partial class OfflineDbContext(DbContextOptions<OfflineDbContext> options) : DbContext(options)
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
}
