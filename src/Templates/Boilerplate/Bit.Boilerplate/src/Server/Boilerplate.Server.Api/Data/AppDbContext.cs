//+:cnd:noEmit
//#if (module == "Admin" || module == "Sales")
using Boilerplate.Server.Api.Models.Products;
using Boilerplate.Server.Api.Models.Categories;
//#endif
//#if (sample == true)
using Boilerplate.Server.Api.Models.Todo;
//#endif
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Boilerplate.Server.Api.Models.Identity;
using Boilerplate.Server.Api.Data.Configurations;
//#if (notification == true)
using Boilerplate.Server.Api.Models.PushNotification;
//#endif
//#if (database == "Sqlite")
using System.Security.Cryptography;
//#endif

namespace Boilerplate.Server.Api.Data;

public partial class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<User, Role, Guid>(options), IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = default!;

    public DbSet<UserSession> UserSessions { get; set; } = default!;

    //#if (sample == true)
    public DbSet<TodoItem> TodoItems { get; set; } = default!;
    //#endif
    //#if (module == "Admin" || module == "Sales")
    public DbSet<Category> Categories { get; set; } = default!;
    public DbSet<Product> Products { get; set; } = default!;
    //#endif
    //#if (notification == true)
    public DbSet<PushNotificationSubscription> PushNotificationSubscriptions { get; set; } = default!;
    //#endif

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        //#if (IsInsideProjectTemplate == true)
        /*
        //#endif
        //#if (database == "PostgreSQL" || database == "SqlServer")
        modelBuilder.HasSequence<int>("ProductShortId")
            .StartsAt(10_000)
            .IncrementsBy(1);
        //#endif
        //#if (IsInsideProjectTemplate == true)
        */
        //#endif

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        ConfigureIdentityTableNames(modelBuilder);

        ConfigureConcurrencyStamp(modelBuilder);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        try
        {
            SetConcurrencyStamp();

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
            SetConcurrencyStamp();

            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConflictException(nameof(AppStrings.UpdateConcurrencyException), exception);
        }
    }

    private void SetConcurrencyStamp()
    {
        ChangeTracker.DetectChanges();

        foreach (var entityEntry in ChangeTracker.Entries().Where(e => e.State is EntityState.Modified or EntityState.Deleted))
        {
            if (entityEntry.CurrentValues.TryGetValue<object>("ConcurrencyStamp", out var currentConcurrencyStamp) is false
                || currentConcurrencyStamp is not byte[])
                continue;

            //#if (database != "Sqlite")
            // https://github.com/dotnet/efcore/issues/35443
            entityEntry.OriginalValues.SetValues(new Dictionary<string, object> { { "ConcurrencyStamp", currentConcurrencyStamp } });
            //#else
            entityEntry.CurrentValues.SetValues(new Dictionary<string, object> { { "ConcurrencyStamp", RandomNumberGenerator.GetBytes(8) } });
            //#endif
        }
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        //#if (IsInsideProjectTemplate == true)
        if (Database.ProviderName!.EndsWith("Sqlite", StringComparison.InvariantCulture))
        {
            //#endif
            //#if (database == "Sqlite")
            // SQLite does not support expressions of type 'DateTimeOffset' in ORDER BY clauses. Convert the values to a supported type:
            configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToBinaryConverter>();
            configurationBuilder.Properties<DateTimeOffset?>().HaveConversion<DateTimeOffsetToBinaryConverter>();
            //#endif
            //#if (IsInsideProjectTemplate == true)
        }
        //#endif

        //#if (IsInsideProjectTemplate == true)
        if (Database.ProviderName.EndsWith("PostgreSQL", StringComparison.InvariantCulture))
        {
            //#endif
            //#if (database == "PostgreSQL")
            // PostgreSQL does not support DateTimeOffset with offset other than Utc.
            configurationBuilder.Properties<DateTimeOffset>().HaveConversion<PostgresDateTimeOffsetConverter>();
            configurationBuilder.Properties<DateTimeOffset?>().HaveConversion<NullablePostgresDateTimeOffsetConverter>();
            //#endif
            //#if (IsInsideProjectTemplate == true)
        }
        //#endif

        //#if (IsInsideProjectTemplate == true)
        if (Database.ProviderName.EndsWith("SqlServer", StringComparison.InvariantCulture))
        {
            //#endif
            //#if (database == "SqlServer")
            configurationBuilder.Conventions.Add(_ => new SqlServerPrimaryKeySequentialGuidDefaultValueConvention());
            //#endif
            //#if (IsInsideProjectTemplate == true)
        }
        //#endif

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

    private void ConfigureConcurrencyStamp(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties()
                .Where(p => p.Name is "ConcurrencyStamp" && p.PropertyInfo?.PropertyType == typeof(byte[])))
            {
                var builder = new PropertyBuilder(property);

                //#if (database == "Sqlite")
                //#if (IsInsideProjectTemplate == true)
                if (Database.ProviderName!.EndsWith("Sqlite", StringComparison.InvariantCulture))
                {
                    //#endif
                    builder.IsConcurrencyToken();
                    //#if (IsInsideProjectTemplate == true)
                    continue;
                }
                //#endif
                //#else
                builder.IsConcurrencyToken()
                    .IsRowVersion();

                //#if (IsInsideProjectTemplate == true)
                if (Database.ProviderName!.EndsWith("PostgreSQL", StringComparison.InvariantCulture))
                {
                    //#endif
                    //#if (database == "PostgreSQL")
                    builder.HasConversion(new ValueConverter<byte[], uint>(
                        v => BitConverter.ToUInt32(v, 0),
                        v => BitConverter.GetBytes(v)));
                    //#endif
                    //#if (IsInsideProjectTemplate == true)
                }
                //#endif
                //#endif
            }
        }
    }
}
