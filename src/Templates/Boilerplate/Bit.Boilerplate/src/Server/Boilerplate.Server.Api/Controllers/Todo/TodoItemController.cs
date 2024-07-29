using Boilerplate.Shared.Dtos.Todo;
using Boilerplate.Server.Api.Models.Todo;
using Boilerplate.Shared.Controllers.Todo;

namespace Boilerplate.Server.Api.Controllers.Todo;

[ApiController, Route("api/[controller]/[action]")]
public partial class TodoItemController : AppControllerBase, ITodoItemController
{
    [HttpGet, EnableQuery]
    public IQueryable<TodoItemDto> Get()
    {
        var userId = User.GetUserId();

        return DbContext.Set<TodoItem>()
            .Where(t => t.UserId == userId)
            .Project();
    }

    [HttpGet]
    public async Task<PagedResult<TodoItemDto>> GetTodoItems(ODataQueryOptions<TodoItemDto> odataQuery, CancellationToken cancellationToken)
    {
        var query = (IQueryable<TodoItemDto>)odataQuery.ApplyTo(Get(), ignoreQueryOptions: AllowedQueryOptions.Top | AllowedQueryOptions.Skip);

        var totalCount = await query.LongCountAsync(cancellationToken);

        if (odataQuery.Skip is not null)
            query = query.Skip(odataQuery.Skip.Value);

        if (odataQuery.Top is not null)
            query = query.Take(odataQuery.Top.Value);

        return new PagedResult<TodoItemDto>(await query.ToArrayAsync(cancellationToken), totalCount);
    }

    [HttpGet("{id}")]
    public async Task<TodoItemDto> Get(Guid id, CancellationToken cancellationToken)
    {
        var dto = await Get().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (dto is null)
            throw new ResourceNotFoundException(Localizer[nameof(AppStrings.ToDoItemCouldNotBeFound)]);

        return dto;
    }

    [HttpPost]
    public async Task<TodoItemDto> Create(TodoItemDto dto, CancellationToken cancellationToken)
    {
        var entityToAdd = dto.Map();

        entityToAdd.UserId = User.GetUserId();

        entityToAdd.Date = DateTimeOffset.UtcNow;

        await DbContext.Set<TodoItem>().AddAsync(entityToAdd, cancellationToken);

        await DbContext.SaveChangesAsync(cancellationToken);

        return entityToAdd.Map();
    }

    [HttpPut]
    public async Task<TodoItemDto> Update(TodoItemDto dto, CancellationToken cancellationToken)
    {
        var entityToUpdate = await DbContext.Set<TodoItem>().FirstOrDefaultAsync(t => t.Id == dto.Id, cancellationToken);

        if (entityToUpdate is null)
            throw new ResourceNotFoundException(Localizer[nameof(AppStrings.ToDoItemCouldNotBeFound)]);

        dto.Patch(entityToUpdate);

        await DbContext.SaveChangesAsync(cancellationToken);

        return entityToUpdate.Map();
    }

    [HttpDelete("{id}")]
    public async Task Delete(Guid id, CancellationToken cancellationToken)
    {
        DbContext.Set<TodoItem>().Remove(new() { Id = id });

        var affectedRows = await DbContext.SaveChangesAsync(cancellationToken);

        if (affectedRows < 1)
            throw new ResourceNotFoundException(Localizer[nameof(AppStrings.ToDoItemCouldNotBeFound)]);
    }
}

