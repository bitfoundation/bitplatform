using Boilerplate.Server.Api.Models.Todo;
using Boilerplate.Shared.Dtos.Todo;
using CommunityToolkit.Datasync.Server;
using CommunityToolkit.Datasync.Server.EntityFrameworkCore;

namespace Boilerplate.Server.Api.Controllers.Todo;

/// <summary>
/// Leverages CommunityToolkit.Datasync.Server to provide a TableController for TodoItem entities,
/// that provides standard CRUD operations for Client Offline Database Sync.
/// </summary>
[Route("api/[controller]/"),
    Authorize(Policy = AuthPolicies.PRIVILEGED_ACCESS),
    Authorize(Policy = AppFeatures.Todo.ManageTodo)]
public class TodoItemTableController : TableController<TodoItemDto>
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

public class TodoItemTableRepository(IHttpContextAccessor httpContextAccessor, AppDbContext dbContext)
    : IRepository<TodoItemDto>
{
    private EntityTableRepository<TodoItem> Repository => field ??= new(dbContext);

    public async ValueTask<IQueryable<TodoItemDto>> AsQueryableAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.TodoItems
            .Where(i => i.UserId == httpContextAccessor.HttpContext!.User.GetUserId())
            .Project();
    }

    public async ValueTask CreateAsync(TodoItemDto dto, CancellationToken cancellationToken = default)
    {
        var entity = dto.Map();
        entity.UserId = httpContextAccessor.HttpContext!.User.GetUserId();
        await Repository.CreateAsync(entity, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask DeleteAsync(string id, byte[]? version = null, CancellationToken cancellationToken = default)
    {
        await Repository.DeleteAsync(id, version, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<TodoItemDto> ReadAsync(string id, CancellationToken cancellationToken = default)
    {
        var entity = await Repository.ReadAsync(id, cancellationToken).ConfigureAwait(false);
        return entity.Map();
    }

    public async ValueTask ReplaceAsync(TodoItemDto dto, byte[]? version = null, CancellationToken cancellationToken = default)
    {
        var entity = dto.Map();
        entity.UserId = httpContextAccessor.HttpContext!.User.GetUserId();
        await Repository.ReplaceAsync(entity, version, cancellationToken);
    }
}
