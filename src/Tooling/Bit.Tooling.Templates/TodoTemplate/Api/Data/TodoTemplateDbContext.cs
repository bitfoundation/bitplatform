using TodoTemplate.Api.Models.Account;
using TodoTemplate.Api.Models.TodoItem;

namespace TodoTemplate.Api.Data;

public class TodoTemplateDbContext : IdentityDbContext<User, Role, int>
{
    public TodoTemplateDbContext(DbContextOptions<TodoTemplateDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(TodoTemplateDbContext).Assembly);

        ConfigIdentityTables(builder);
    }

    public DbSet<TodoItem> TodoItems { get; set; }

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
