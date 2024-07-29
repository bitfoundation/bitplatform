//+:cnd:noEmit
//#if (sample == "Admin")
using Boilerplate.Server.Api.Models.Categories;
using Boilerplate.Server.Api.Models.Products;
//#elif (sample == "Todo")
using Boilerplate.Server.Api.Models.Todo;
//#endif
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Boilerplate.Server.Api.Models.Identity;
using Boilerplate.Server.Api.Data.Configurations;

namespace Boilerplate.Server.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<User, Role, Guid>(options), IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        ConfigureDatabaseStorageNames(builder);
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

    private void ConfigureDatabaseStorageNames(ModelBuilder builder)
    {
        builder.Entity<User>()
            //#if (database != "Cosmos")
            .ToContainer("Users").HasPartitionKey(e => e.Id)
            //#endif
            .ToTable("Users", "identity");

        builder.Entity<Role>()
            //#if (database != "Cosmos")
            .ToContainer("Roles").HasPartitionKey(e => e.Id)
            //#endif
            .ToTable("Roles", "identity");

        builder.Entity<IdentityUserRole<Guid>>()
            //#if (database != "Cosmos")
            .ToContainer("UserRoles").HasPartitionKey(e => e.RoleId)
            //#endif
            .ToTable("UserRoles", "identity");

        builder.Entity<IdentityRoleClaim<Guid>>()
            //#if (database != "Cosmos")
            .ToContainer("RoleClaims").HasPartitionKey(e => e.RoleId)
            //#endif
            .ToTable("RoleClaims", "identity");

        builder.Entity<IdentityUserLogin<Guid>>()
            //#if (database != "Cosmos")
            .ToContainer("UserLogins").HasPartitionKey(e => e.ProviderKey)
            //#endif
            .ToTable("UserLogins", "identity");

        builder.Entity<IdentityUserToken<Guid>>()
            //#if (database != "Cosmos")
            .ToContainer("UserTokens").HasPartitionKey(e => e.UserId)
            //#endif
            .ToTable("UserTokens", "identity");

        builder.Entity<IdentityUserClaim<Guid>>()
            //#if (database != "Cosmos")
            .ToContainer("UserClaims").HasPartitionKey(e => e.UserId)
            //#endif
            .ToTable("UserClaims", "identity");

        builder.Entity<DataProtectionKey>()
            //#if (database != "Cosmos")
            .ToContainer("DataProtectionKeys").HasPartitionKey(e => e.Id)
            //#endif
            .ToTable("DataProtectionKeys");

        //#if (IsInsideProjectTemplate == true)
        if (Database.ProviderName!.EndsWith("Cosmos", StringComparison.InvariantCulture))
        {
            //#endif
            //#if (database != "Cosmos")
            builder.Entity<DataProtectionKey>()
                .Property(p => p.Id).HasConversion(typeof(string));
            //#endif
            //#if (IsInsideProjectTemplate == true)
        }
        //#endif

        //#if (sample == "Todo")
        builder.Entity<TodoItem>()
            //#if (database != "Cosmos")
            .ToContainer("TodoItems").HasPartitionKey(e => e.Id)
            //#endif
            .ToTable("TodoItems");

        //#elif (sample == "Admin")
        builder.Entity<Product>()
            //#if (database != "Cosmos")
            .ToContainer("Products").HasPartitionKey(e => e.CategoryId)
            //#endif
            .ToTable("Products");

        builder.Entity<Category>()
            //#if (database != "Cosmos")
            .ToContainer("Categories").HasPartitionKey(e => e.Id)
            //#endif
            .ToTable("Categories");
        //#endif    
    }
}
