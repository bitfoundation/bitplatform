//+:cnd:noEmit
//#if (sample == "Admin")
using Boilerplate.Server.Api.Models.Categories;
using Boilerplate.Server.Api.Models.Products;
//#elif (sample == "Todo")
using Boilerplate.Server.Api.Models.Todo;
//#endif
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Boilerplate.Server.Api.Models.Identity;
using Boilerplate.Server.Api.Data.Configurations;

namespace Boilerplate.Server.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<User, Role, int>(options), IDataProtectionKeyContext
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

        ConfigureIdentityTables(builder);
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

    //#if (database == "Sqlite")
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        //#if (IsInsideProjectTemplate == true)
        if (Database.ProviderName!.EndsWith("Sqlite", StringComparison.InvariantCulture))
        {
        //#endif
        // SQLite does not support expressions of type 'DateTimeOffset' in ORDER BY clauses. Convert the values to a supported type:
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToBinaryConverter>();
        configurationBuilder.Properties<DateTimeOffset?>().HaveConversion<DateTimeOffsetToBinaryConverter>();
        //#if (IsInsideProjectTemplate == true)
        }
        //#endif

        //#if (IsInsideProjectTemplate == true)
        if (Database.ProviderName.EndsWith("PostgreSQL", StringComparison.InvariantCulture))
        {
            //#endif
            // SQLite does not support expressions of type 'DateTimeOffset' in ORDER BY clauses. Convert the values to a supported type:
            configurationBuilder.Properties<DateTimeOffset>().HaveConversion<PostgresDateTimeOffsetConverter>();
            configurationBuilder.Properties<DateTimeOffset?>().HaveConversion<NullablePostgresDateTimeOffsetConverter>();
            //#if (IsInsideProjectTemplate == true)
        }
        //#endif

        base.ConfigureConventions(configurationBuilder);
    }
    //#endif

    private void ConfigureIdentityTables(ModelBuilder builder)
    {
        builder.Entity<User>().ToTable("Users", "identity");
        builder.Entity<Role>().ToTable("Roles", "identity");
        builder.Entity<IdentityUserRole<int>>().ToTable("UserRoles", "identity");
        builder.Entity<IdentityRoleClaim<int>>().ToTable("RoleClaims", "identity");
        builder.Entity<IdentityUserLogin<int>>().ToTable("UserLogins", "identity");
        builder.Entity<IdentityUserToken<int>>().ToTable("UserTokens", "identity");
        builder.Entity<IdentityUserClaim<int>>().ToTable("UserClaims", "identity");
    }
}
