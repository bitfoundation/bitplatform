using TodoTemplate.Api.Models.TodoItem;
using TodoTemplate.Shared.Dtos.TodoItem;

namespace TodoTemplate.Api.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public partial class TodoItemController : AppControllerBase
{
    [HttpGet, EnableQuery]
    public IQueryable<TodoItemDto> Get(CancellationToken cancellationToken)
    {
        return dbContext.TodoItems
            .Where(t => t.UserId == User.GetUserId())
            .ProjectTo<TodoItemDto>(mapper.ConfigurationProvider, cancellationToken);
    }

    [HttpGet("{id:int}")]
    public async Task<TodoItemDto> Get(int id, CancellationToken cancellationToken)
    {
        var todoItem = await Get(cancellationToken).FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (todoItem is null)
            throw new ResourceNotFoundException(nameof(ErrorStrings.ToDoItemCouldNotBeFound));

        return todoItem;
    }

    [HttpPost]
    public async Task Create(TodoItemDto dto, CancellationToken cancellationToken)
    {
        var todoItemToAdd = mapper.Map<TodoItem>(dto);

        todoItemToAdd.UserId = User.GetUserId();

        await dbContext.TodoItems.AddAsync(todoItemToAdd, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    [HttpPut]
    public async Task Update(TodoItemDto dto, CancellationToken cancellationToken)
    {
        var todoItemToUpdate = await dbContext.TodoItems.FirstOrDefaultAsync(t => t.Id == dto.Id, cancellationToken);

        if (todoItemToUpdate is null)
            throw new ResourceNotFoundException(nameof(ErrorStrings.ToDoItemCouldNotBeFound));

        var updatedTodoItem = mapper.Map(dto, todoItemToUpdate);

        dbContext.TodoItems.Update(updatedTodoItem);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    [HttpDelete("{id:int}")]
    public async Task Delete(int id, CancellationToken cancellationToken)
    {
        dbContext.Remove(new TodoItem { Id = id });

        var affectedRows = await dbContext.SaveChangesAsync(cancellationToken);

        if (affectedRows < 1)
            throw new ResourceNotFoundException(nameof(ErrorStrings.ToDoItemCouldNotBeFound));
    }
}

