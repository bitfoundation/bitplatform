using CommunityToolkit.Datasync.Server;
using Boilerplate.Server.Api.Models.Todo;
using CommunityToolkit.Datasync.Server.EntityFrameworkCore;

namespace Boilerplate.Server.Api.Controllers.Todo;

/// <summary>
/// Leverages CommunityToolkit.Datasync.Server to provide a TableController for TodoItem entities,
/// that provides standard CRUD operations for Client Offline Database Sync.
/// </summary>
[Route("api/[controller]/"),
    Authorize(Policy = AuthPolicies.PRIVILEGED_ACCESS),
    Authorize(Policy = AppFeatures.Todo.ManageTodo)]
public class TodoItemTableController : TableController<TodoItem>
{
    public TodoItemTableController(TodoItemTableRepository repository, ILogger<TodoItemTableController> logger) : base(repository)
    {
        Logger = logger;
        Options = new()
        {
            EnableSoftDelete = true
        };
    }
}

/// <summary>
/// Hooks for overriding the default behavior of the EntityTableRepository for TodoItem entities,
/// </summary>
public class TodoItemTableRepository(IHttpContextAccessor httpContextAccessor, AppDbContext dbContext) : EntityTableRepository<TodoItem>(dbContext)
{
    public override async ValueTask<IQueryable<TodoItem>> AsQueryableAsync(CancellationToken cancellationToken)
    {
        var result = await base.AsQueryableAsync(cancellationToken);

        return result.Where(item => item.UserId == httpContextAccessor.HttpContext!.User.GetUserId());
    }

    public override ValueTask CreateAsync(TodoItem entity, CancellationToken cancellationToken)
    {
        entity.UserId = httpContextAccessor.HttpContext!.User.GetUserId();

        return base.CreateAsync(entity, cancellationToken);
    }

    public override ValueTask ReplaceAsync(TodoItem entity, byte[]? version = null, CancellationToken cancellationToken = default)
    {
        entity.UserId = httpContextAccessor.HttpContext!.User.GetUserId();

        return base.ReplaceAsync(entity, version, cancellationToken);
    }
}
