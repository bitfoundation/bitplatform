using TodoTemplate.Api.Data.Models.Account;
using TodoTemplate.Api.Data.Models.Todo;

namespace TodoTemplate.Api.Data.Context;

public class TodoTemplateDbContext : IdentityDbContext<User, Role, int>
{
    public TodoTemplateDbContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(TodoTemplateDbContext).Assembly);
    }
    public DbSet<Todo> Todos { get; set; }
}
