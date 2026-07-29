//+:cnd:noEmit
using Boilerplate.Shared.Features.Todo;

namespace Boilerplate.Server.Api.Features.Todo;

// This controller is the plain REST surface over TodoItems. It is NOT emitted when offlineDb == true
// (see .template.config/template.json), because the offline database syncs through
// TodoItemTableController.cs instead, and a second non-Datasync-aware CRUD surface over the same table
// would disagree with the sync contract (hard delete vs. tombstone, and no Deleted filter on reads).

[ApiVersion(1)]
[ApiController, Route("api/v{v:apiVersion}/[controller]/[action]"),
    Authorize(Policy = AuthPolicies.PRIVILEGED_ACCESS),
    Authorize(Policy = AppFeatures.Todo.Todo_Manage_Self)]
public partial class TodoItemController : AppControllerBase, ITodoItemController
{
    [HttpGet, EnableQuery]
    public IQueryable<TodoItemDto> Get()
    {
        var userId = User.GetUserId();

        return DbContext.TodoItems
            .Where(t => t.UserId == userId)
            .Project();
    }

    [HttpGet]
    public async Task<PagedResponse<TodoItemDto>> GetTodoItems(ODataQueryOptions<TodoItemDto> odataQuery, CancellationToken cancellationToken)
    {
        var query = (IQueryable<TodoItemDto>)odataQuery.ApplyTo(Get(), ignoreQueryOptions: AllowedQueryOptions.Top | AllowedQueryOptions.Skip);

        var totalCount = await query.LongCountAsync(cancellationToken);

        query = query.SkipIf(odataQuery.Skip is not null, odataQuery.Skip?.Value)
                     .TakeIf(odataQuery.Top is not null, odataQuery.Top?.Value);

        return new PagedResponse<TodoItemDto>(await query.ToArrayAsync(cancellationToken), totalCount);
    }

    [HttpGet("{id}")]
    public async Task<TodoItemDto> Get(string id, CancellationToken cancellationToken)
    {
        var dto = await Get().FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new ResourceNotFoundException(Localizer[nameof(AppStrings.ToDoItemCouldNotBeFound)]);

        return dto;
    }

    [HttpPost]
    public async Task<TodoItemDto> Create(TodoItemDto dto, CancellationToken cancellationToken)
    {
        var entityToAdd = dto.Map();

        entityToAdd.UserId = User.GetUserId();

        entityToAdd.UpdatedAt = TimeProvider.GetUtcNow();

        await DbContext.TodoItems.AddAsync(entityToAdd, cancellationToken);

        await DbContext.SaveChangesAsync(cancellationToken);

        return entityToAdd.Map();
    }

    [HttpPut]
    public async Task<TodoItemDto> Update(TodoItemDto dto, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var entityToUpdate = await DbContext.TodoItems.FirstOrDefaultAsync(t => t.Id == dto.Id && t.UserId == userId, cancellationToken)
            ?? throw new ResourceNotFoundException(Localizer[nameof(AppStrings.ToDoItemCouldNotBeFound)]);

        dto.Patch(entityToUpdate);

        await DbContext.SaveChangesAsync(cancellationToken);

        return entityToUpdate.Map();
    }

    [HttpDelete("{id}")]
    public async Task Delete(string id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        // The UserId term is what scopes the delete to the caller; one round trip, same shape as CategoryController.Delete.
        if (await DbContext.TodoItems
            .Where(t => t.Id == id && t.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken) == 0)
        {
            throw new ResourceNotFoundException(Localizer[nameof(AppStrings.ToDoItemCouldNotBeFound)]);
        }
    }
}

