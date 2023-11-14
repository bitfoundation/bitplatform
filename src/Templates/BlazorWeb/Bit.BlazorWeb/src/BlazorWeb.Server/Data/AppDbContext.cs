//+:cnd:noEmit
using BlazorWeb.Server.Models.Identity;
//#if (sample == "Todo")
using BlazorWeb.Server.Models.Todo;
//#elif (sample == "AdminPanel")
using BlazorWeb.Server.Models.Categories;
using BlazorWeb.Server.Models.Products;
//#endif
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BlazorWeb.Server.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) 
    : IdentityDbContext<User, Role, int>(options)
{

    //#if (sample == "Todo")
    public DbSet<TodoItem> TodoItems { get; set; }
    //#elif (sample == "AdminPanel")
    public DbSet<Category> Categories { get; set; }
    public DbSet<Product> Products { get; set; }
    //#endif

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        ConfigIdentityTables(builder);
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
        if (Database.ProviderName!.EndsWith("Sqlite", StringComparison.InvariantCulture))
        {
            configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToBinaryConverter>();
            configurationBuilder.Properties<DateTimeOffset?>().HaveConversion<DateTimeOffsetToBinaryConverter>();
        }
    }
    //#endif

    private void ConfigIdentityTables(ModelBuilder builder)
    {
        //Config Asp Identity table name
        builder.Entity<User>().ToTable("Users");
        builder.Entity<Role>().ToTable("Roles");
        builder.Entity<IdentityUserRole<int>>().ToTable("UserRoles");
        builder.Entity<IdentityRoleClaim<int>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserLogin<int>>().ToTable("UserLogins");
        builder.Entity<IdentityUserToken<int>>().ToTable("UserTokens");
        builder.Entity<IdentityUserClaim<int>>().ToTable("UserClaims");
    }
}
