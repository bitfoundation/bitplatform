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
    }

    public DbSet<TodoItem> TodoItems { get; set; }
}
