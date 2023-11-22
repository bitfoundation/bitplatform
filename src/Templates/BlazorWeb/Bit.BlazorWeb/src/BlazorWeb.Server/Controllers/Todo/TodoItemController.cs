using BlazorWeb.Server.Models.Todo;
using BlazorWeb.Shared.Dtos.Todo;

namespace BlazorWeb.Server.Controllers.Todo;

[Route("api/[controller]/[action]")]
[ApiController]
public partial class TodoItemController : AppControllerBase
{
    [HttpGet, EnableQuery]
    public IQueryable<TodoItemDto> Get()
    {
        var userId = User.GetUserId();

        return DbContext.TodoItems
            .Where(t => t.UserId == userId)
            .Project();
    }

    [HttpGet("{id:int}")]
    public async Task<TodoItemDto> Get(int id, CancellationToken cancellationToken)
    {
        var todoItem = await Get().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (todoItem is null)
            throw new ResourceNotFoundException(Localizer[nameof(AppStrings.ToDoItemCouldNotBeFound)]);

        return todoItem;
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

        return new PagedResult<TodoItemDto>(query.AsAsyncEnumerable(), totalCount);
    }

    [HttpPost]
    public async Task<TodoItemDto> Create(TodoItemDto dto, CancellationToken cancellationToken)
    {
        var todoItemToAdd = dto.Map();

        todoItemToAdd.UserId = User.GetUserId();

        todoItemToAdd.Date = DateTimeOffset.UtcNow;

        await DbContext.TodoItems.AddAsync(todoItemToAdd, cancellationToken);

        await DbContext.SaveChangesAsync(cancellationToken);

        return todoItemToAdd.Map();
    }

    [HttpPut]
    public async Task<TodoItemDto> Update(TodoItemDto dto, CancellationToken cancellationToken)
    {
        var todoItemToUpdate = await DbContext.TodoItems.FirstOrDefaultAsync(t => t.Id == dto.Id, cancellationToken);

        if (todoItemToUpdate is null)
            throw new ResourceNotFoundException(Localizer[nameof(AppStrings.ToDoItemCouldNotBeFound)]);

        dto.Patch(todoItemToUpdate);

        await DbContext.SaveChangesAsync(cancellationToken);

        return todoItemToUpdate.Map();
    }

    [HttpDelete("{id:int}")]
    public async Task Delete(int id, CancellationToken cancellationToken)
    {
        DbContext.Remove(new TodoItem { Id = id });

        var affectedRows = await DbContext.SaveChangesAsync(cancellationToken);

        if (affectedRows < 1)
            throw new ResourceNotFoundException(Localizer[nameof(AppStrings.ToDoItemCouldNotBeFound)]);
    }
}

