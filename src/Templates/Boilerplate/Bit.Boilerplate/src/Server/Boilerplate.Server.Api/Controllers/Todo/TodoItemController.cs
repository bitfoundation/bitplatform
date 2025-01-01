using Boilerplate.Shared.Dtos.Todo;
using Boilerplate.Shared.Controllers.Todo;

namespace Boilerplate.Server.Api.Controllers.Todo;

[ApiController, Route("api/[controller]/[action]"), Authorize(Policy = AuthPolicies.PRIVILEGED_ACCESS)]
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
    public async Task<PagedResult<TodoItemDto>> GetTodoItems(ODataQueryOptions<TodoItemDto> odataQuery, CancellationToken cancellationToken)
    {
        var query = (IQueryable<TodoItemDto>)odataQuery.ApplyTo(Get(), ignoreQueryOptions: AllowedQueryOptions.Top | AllowedQueryOptions.Skip);

        var totalCount = await query.LongCountAsync(cancellationToken);

        query = query.SkipIf(odataQuery.Skip is not null, odataQuery.Skip?.Value)
                     .TakeIf(odataQuery.Top is not null, odataQuery.Top?.Value);

        return new PagedResult<TodoItemDto>(await query.ToArrayAsync(cancellationToken), totalCount);
    }

    [HttpGet("{id}")]
    public async Task<TodoItemDto> Get(Guid id, CancellationToken cancellationToken)
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

        entityToAdd.Date = DateTimeOffset.UtcNow;

        await DbContext.TodoItems.AddAsync(entityToAdd, cancellationToken);

        await DbContext.SaveChangesAsync(cancellationToken);

        return entityToAdd.Map();
    }

    [HttpPut]
    public async Task<TodoItemDto> Update(TodoItemDto dto, CancellationToken cancellationToken)
    {
        var entityToUpdate = await DbContext.TodoItems.FirstOrDefaultAsync(t => t.Id == dto.Id, cancellationToken)
            ?? throw new ResourceNotFoundException(Localizer[nameof(AppStrings.ToDoItemCouldNotBeFound)]);

        dto.Patch(entityToUpdate);

        await DbContext.SaveChangesAsync(cancellationToken);

        return entityToUpdate.Map();
    }

    [HttpDelete("{id}")]
    public async Task Delete(Guid id, CancellationToken cancellationToken)
    {
        DbContext.TodoItems.Remove(new() { Id = id });

        var affectedRows = await DbContext.SaveChangesAsync(cancellationToken);

        if (affectedRows < 1)
            throw new ResourceNotFoundException(Localizer[nameof(AppStrings.ToDoItemCouldNotBeFound)]);
    }
}

