//+:cnd:noEmit
//#if (sample == "Admin")
using Boilerplate.Server.Api.Models.Categories;
using Boilerplate.Server.Api.Models.Products;
//#elif (sample == "Todo")
using Boilerplate.Server.Api.Models.Todo;
//#endif
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Boilerplate.Server.Api.Models.Identity;
using Boilerplate.Server.Api.Data.Configurations;

namespace Boilerplate.Server.Api.Data;

public partial class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<User, Role, Guid>(options), IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }

    //#if (sample == "Todo")
    public DbSet<TodoItem> TodoItems { get; set; }
    //#elif (sample == "Admin")
    public DbSet<Category> Categories { get; set; }
    public DbSet<Product> Products { get; set; }
    //#endif

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        //#if (database != "Cosmos")
        ConfigureIdentityTableNames(builder);
        //#else
        ConfigureContainers(builder);
        //#endif
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

    //#if (database != "Cosmos")
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
    //#endif

    //#if (database == "Cosmos")
    private void ConfigureContainers(ModelBuilder builder)
    {
        builder.Entity<User>()
            .ToContainer("Users").HasPartitionKey(e => e.Id);

        builder.Entity<Role>()
            .ToContainer("Roles").HasPartitionKey(e => e.Id);

        builder.Entity<IdentityRoleClaim<Guid>>()
            .ToContainer("RoleClaims").HasPartitionKey(e => e.RoleId);

        builder.Entity<IdentityUserToken<Guid>>()
            .ToContainer("UserTokens").HasPartitionKey(e => e.UserId);

        builder.Entity<IdentityUserClaim<Guid>>()
            .ToContainer("UserClaims").HasPartitionKey(e => e.UserId);

        builder.Entity<IdentityUserRole<Guid>>()
            .ToContainer("UserRoles").HasPartitionKey(e => e.UserId);

        builder.Entity<IdentityUserLogin<Guid>>()
            .ToContainer("UserLogins").HasPartitionKey(e => e.UserId);

        builder.Entity<DataProtectionKey>()
            .ToContainer("DataProtectionKeys").HasPartitionKey(e => e.Id);

        //#if (IsInsideProjectTemplate == true)
        if (Database.ProviderName!.EndsWith("Cosmos", StringComparison.InvariantCulture))
        {
            //#endif
            builder.Entity<DataProtectionKey>()
                .Property(p => p.Id).HasConversion(typeof(string));
            //#if (IsInsideProjectTemplate == true)
        }
        //#endif

        //#if (sample == "Todo")
        builder.Entity<TodoItem>()
            .ToContainer("TodoItems").HasPartitionKey(e => e.Id);

        //#elif (sample == "Admin")
        builder.Entity<Category>()
            .ToContainer("Categories").HasPartitionKey(e => e.Id);

        builder.Entity<Product>()
            .ToContainer("Products").HasPartitionKey(e => e.CategoryId);
        //#endif    
    }
    //#endif
}
