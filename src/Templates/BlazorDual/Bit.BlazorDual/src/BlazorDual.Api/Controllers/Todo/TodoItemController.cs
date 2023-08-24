using BlazorDual.Api.Models.Todo;
using BlazorDual.Shared.Dtos.Todo;

namespace BlazorDual.Api.Controllers.Todo;

[Route("api/[controller]/[action]")]
[ApiController]
public partial class TodoItemController : AppControllerBase
{
    [HttpGet, EnableQuery]
    public IQueryable<TodoItemDto> Get()
    {
        var userId = UserInformationProvider.GetUserId();

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

    [HttpPost]
    public async Task<TodoItemDto> Create(TodoItemDto dto, CancellationToken cancellationToken)
    {
        var todoItemToAdd = dto.Map();

        todoItemToAdd.UserId = UserInformationProvider.GetUserId();

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

