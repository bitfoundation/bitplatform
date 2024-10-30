using AdminPanel.Server.Api.Models.Categories;
using AdminPanel.Server.Api.Models.Products;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using AdminPanel.Server.Api.Models.Identity;
using AdminPanel.Server.Api.Data.Configurations;
using AdminPanel.Server.Api.Models.PushNotification;

namespace AdminPanel.Server.Api.Data;

public partial class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<User, Role, Guid>(options), IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = default!;

    public DbSet<Category> Categories { get; set; } = default!;
    public DbSet<Product> Products { get; set; } = default!;
    public DbSet<DeviceInstallation> DeviceInstallations { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        ConfigureIdentityTableNames(modelBuilder);

        ConcurrencyStamp(modelBuilder);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        try
        {
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConflictException(nameof(AppStrings.UpdateConcurrencyException), exception);
        }
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = new CancellationToken())
    {
        try
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConflictException(nameof(AppStrings.UpdateConcurrencyException), exception);
        }
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {

            // PostgreSQL does not support DateTimeOffset with offset other than Utc.
            configurationBuilder.Properties<DateTimeOffset>().HaveConversion<PostgresDateTimeOffsetConverter>();
            configurationBuilder.Properties<DateTimeOffset?>().HaveConversion<NullablePostgresDateTimeOffsetConverter>();


        base.ConfigureConventions(configurationBuilder);
    }

    private void ConfigureIdentityTableNames(ModelBuilder builder)
    {
        builder.Entity<User>()
            .ToTable("Users");

        builder.Entity<Role>()
            .ToTable("Roles");

        builder.Entity<IdentityUserRole<Guid>>()
            .ToTable("UserRoles");

        builder.Entity<IdentityUserLogin<Guid>>()
            .ToTable("UserLogins");

        builder.Entity<IdentityUserToken<Guid>>()
            .ToTable("UserTokens");

        builder.Entity<IdentityRoleClaim<Guid>>()
            .ToTable("RoleClaims");

        builder.Entity<IdentityUserClaim<Guid>>()
            .ToTable("UserClaims");
    }


    private void ConcurrencyStamp(ModelBuilder modelBuilder)
    {

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties()
                .Where(p => p.Name is "ConcurrencyStamp"))
            {
                var builder = new PropertyBuilder(property);
                builder.IsConcurrencyToken()
                    .IsRowVersion();

                    if (property.ClrType == typeof(byte[]))
                    {
                        builder.HasConversion(new ValueConverter<byte[], uint>(
                            v => BitConverter.ToUInt32(v, 0),
                            v => BitConverter.GetBytes(v)));
                    }
            }
        }
    }
}
