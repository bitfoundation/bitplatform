using TodoTemplate.Server.Api.Models.TodoItem;
using TodoTemplate.Shared.Dtos.TodoItem;

namespace TodoTemplate.Server.Api.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public partial class TodoItemController : AppControllerBase
{
    [HttpGet, EnableQuery]
    public IQueryable<TodoItemDto> Get(CancellationToken cancellationToken)
    {
        var userId = UserInformationProvider.GetUserId();

        return DbContext.TodoItems
            .Where(t => t.UserId == userId)
            .ProjectTo<TodoItemDto>(Mapper.ConfigurationProvider, cancellationToken);
    }

    [HttpGet("{id:int}")]
    public async Task<TodoItemDto> Get(int id, CancellationToken cancellationToken)
    {
        var todoItem = await Get(cancellationToken).FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (todoItem is null)
            throw new ResourceNotFoundException(Localizer[nameof(AppStrings.ToDoItemCouldNotBeFound)]);

        return todoItem;
    }

    [HttpPost]
    public async Task Create(TodoItemDto dto, CancellationToken cancellationToken)
    {
        var todoItemToAdd = Mapper.Map<TodoItem>(dto);

        todoItemToAdd.UserId = UserInformationProvider.GetUserId();

        await DbContext.TodoItems.AddAsync(todoItemToAdd, cancellationToken);

        await DbContext.SaveChangesAsync(cancellationToken);
    }

    [HttpPut]
    public async Task Update(TodoItemDto dto, CancellationToken cancellationToken)
    {
        var todoItemToUpdate = await DbContext.TodoItems.FirstOrDefaultAsync(t => t.Id == dto.Id, cancellationToken);

        if (todoItemToUpdate is null)
            throw new ResourceNotFoundException(Localizer[nameof(AppStrings.ToDoItemCouldNotBeFound)]);

        var updatedTodoItem = Mapper.Map(dto, todoItemToUpdate);

        DbContext.TodoItems.Update(updatedTodoItem);

        await DbContext.SaveChangesAsync(cancellationToken);
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

