//+:cnd:noEmit
//#if (module == "Admin" || module == "Sales")
using Boilerplate.Server.Api.Features.Products;
using Boilerplate.Server.Api.Features.Categories;
//#endif
//#if (sample == true || offlineDb == true)
using Boilerplate.Server.Api.Features.Todo;
//#endif
using Boilerplate.Server.Api.Features.Identity.Models;
//#if (database == "PostgreSQL" || database == "SqlServer")
using Boilerplate.Server.Api.Infrastructure.Data.Configurations;
//#endif
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
//#if (notification == true)
using Boilerplate.Server.Api.Features.PushNotification;
//#endif
using Hangfire.EntityFrameworkCore;
using Boilerplate.Server.Api.Features.Attachments;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
//#if (offlineDb == true)
using CommunityToolkit.Datasync.Server.EntityFrameworkCore;
//#endif

namespace Boilerplate.Server.Api.Infrastructure.Data;

public partial class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<User, Role, Guid, UserClaim, UserRole, UserLogin, RoleClaim, UserToken>(options), IDataProtectionKeyContext
{
    public DbSet<UserSession> UserSessions { get; set; } = default!;

    //#if (sample == true || offlineDb == true)
    public DbSet<TodoItem> TodoItems { get; set; } = default!;
    //#endif
    //#if (module == "Admin" || module == "Sales")
    public DbSet<Category> Categories { get; set; } = default!;
    public DbSet<Product> Products { get; set; } = default!;
    //#endif
    //#if (notification == true)
    public DbSet<PushNotificationSubscription> PushNotificationSubscriptions { get; set; } = default!;
    //#endif

    public DbSet<WebAuthnCredential> WebAuthnCredential { get; set; } = default!;

    //#if (signalR == true)
    public DbSet<SystemPrompt> SystemPrompts { get; set; } = default!;
    //#endif

    public DbSet<Attachment> Attachments { get; set; } = default!;

    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        //#if (IsInsideProjectTemplate == true)
        if (Database.ProviderName!.EndsWith("PostgreSQL", StringComparison.InvariantCulture))
        {
            //#endif
            //#if (database == "PostgreSQL")
            if (IsEmbeddingEnabled)
            {
                modelBuilder.HasPostgresExtension("vector");
            }
            //#endif
            //#if (IsInsideProjectTemplate == true)
        }
        //#endif

        modelBuilder.OnHangfireModelCreating("jobs");

        //#if (IsInsideProjectTemplate == true)
        /*
        //#endif
        //#if (database == "PostgreSQL" || database == "SqlServer")
        modelBuilder.HasSequence<int>("ProductShortId")
            .StartsAt(10_051) // There are 50 products added by ProductConfiguration.cs
            .IncrementsBy(1);
        //#endif
        //#if (IsInsideProjectTemplate == true)
        */
        //#endif

        //#if (IsInsideProjectTemplate == true)
        if (Database.ProviderName!.EndsWith("SqlServer", StringComparison.InvariantCulture))
        {
            //#endif
            //#if (database == "SqlServer")
            modelBuilder.HasDefaultSchema("dbo");
            //#endif
            //#if (IsInsideProjectTemplate == true)
        }
        //#endif

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        ConfigureIdentityTableNames(modelBuilder);

        ConfigureConcurrencyToken(modelBuilder);

        //#if (database != "SQLite")
        ConfigureRowVersion(modelBuilder);
        //#endif
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        try
        {
            OnSavingChanges();

#pragma warning disable NonAsyncEFCoreMethodsUsageAnalyzer
            return base.SaveChanges(acceptAllChangesOnSuccess);
#pragma warning restore NonAsyncEFCoreMethodsUsageAnalyzer
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
            OnSavingChanges();

            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConflictException(nameof(AppStrings.UpdateConcurrencyException), exception);
        }
    }

    private void OnSavingChanges()
    {
        ChangeTracker.DetectChanges();

        foreach (var entry in ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            if (entry.Properties.Any(p => p.Metadata.Name == "UpdatedAt"))
                entry.CurrentValues["UpdatedAt"] = DateTimeOffset.UtcNow;
        }

        foreach (var entityEntry in ChangeTracker.Entries().Where(e => e.State is EntityState.Modified or EntityState.Deleted))
        {
            // https://github.com/dotnet/efcore/issues/35443
            if (entityEntry.Properties.Any(p => p.Metadata.Name == "Version") && entityEntry.CurrentValues["Version"] is long currentVersion)
                entityEntry.OriginalValues["Version"] = currentVersion;
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

        configurationBuilder.Properties<decimal>().HavePrecision(18, 3);
        configurationBuilder.Properties<decimal?>().HavePrecision(18, 3);

        base.ConfigureConventions(configurationBuilder);
    }

    private void ConfigureIdentityTableNames(ModelBuilder builder)
    {
        builder.Entity<User>()
            .ToTable("Users");

        builder.Entity<Role>()
            .ToTable("Roles");

        builder.Entity<UserRole>()
            .ToTable("UserRoles");

        builder.Entity<RoleClaim>()
            .ToTable("RoleClaims");

        builder.Entity<UserClaim>()
            .ToTable("UserClaims");

        builder.Entity<UserLogin>()
            .ToTable("UserLogins");

        builder.Entity<UserToken>()
            .ToTable("UserTokens");
    }

    private void ConfigureConcurrencyToken(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            //#if (offlineDb == true)
            if (typeof(BaseEntityTableData).IsAssignableFrom(entityType.ClrType))
                continue; // No concurrency check for client side offline database sync entities
            //#endif

            foreach (var property in entityType.GetProperties()
                .Where(p => p.Name is "Version" && p.PropertyInfo?.PropertyType == typeof(long)))
            {
                var builder = new PropertyBuilder(property);
                builder.IsConcurrencyToken();
            }
        }
    }

    //#if (database != "SQLite")
    private void ConfigureRowVersion(ModelBuilder modelBuilder)
    {
        //#if (IsInsideProjectTemplate == true)
        if (Database.ProviderName!.EndsWith("Sqlite", StringComparison.InvariantCulture))
            return;
        //#endif

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            //#if (offlineDb == true)
            if (typeof(BaseEntityTableData).IsAssignableFrom(entityType.ClrType))
                continue; // No concurrency check for client side offline database sync entities
            //#endif

            foreach (var property in entityType.GetProperties()
                .Where(p => p.Name is "Version" && p.PropertyInfo?.PropertyType == typeof(long)))
            {
                var builder = new PropertyBuilder(property);

                builder.IsRowVersion();

                //#if (database == "PostgreSQL")
                //#if (IsInsideProjectTemplate == true)
                if (Database.ProviderName!.EndsWith("PostgreSQL", StringComparison.InvariantCulture))
                //#endif
                builder.HasConversion<uint>();
                //#else
                //#if (IsInsideProjectTemplate == true)
                if (Database.ProviderName!.EndsWith("PostgreSQL", StringComparison.InvariantCulture) is false) // SQL Server & MySQL
                //#endif
                builder.HasConversion<byte[]>();
                //#endif
            }
        }
    }
    //#endif

    //#if (database == "PostgreSQL" || database == "SqlServer")
    //#if (database == "PostgreSQL" && aspire == false)
    // In order to enable embedding, the `pgvector` extension must be installed in your PostgreSQL.
    // The following command runs the postgreSQL container with the `pgvector` extension:
    // docker run -d --name postgres -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=BoilerplateDb -p 5432:5432 -v pgdata:/var/lib/postgresql --restart unless-stopped pgvector/pgvector:pg18
    //#endif
    public static readonly bool IsEmbeddingEnabled = false;
    //#endif
}
