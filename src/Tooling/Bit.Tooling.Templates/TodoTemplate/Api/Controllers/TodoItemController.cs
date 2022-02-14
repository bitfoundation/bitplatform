using TodoTemplate.Api.Models.TodoItem;
using TodoTemplate.Shared.Dtos.TodoItem;

namespace TodoTemplate.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TodoItemController : ControllerBase
{
    private readonly TodoTemplateDbContext _dbContext;

    private readonly IMapper _mapper;

    public TodoItemController(TodoTemplateDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    [HttpGet, EnableQuery]
    public IQueryable<TodoItemDto> Get(CancellationToken cancellationToken)
    {
        return _dbContext.TodoItems
            .Where(t => t.UserId == User.GetUserId())
            .ProjectTo<TodoItemDto>(_mapper.ConfigurationProvider, cancellationToken);
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
    public async Task<TodoItemDto> Post(TodoItemDto dto, CancellationToken cancellationToken)
    {
        var todoItemToAdd = _mapper.Map<TodoItem>(dto);

        todoItemToAdd.UserId = User.GetUserId();

        await _dbContext.TodoItems.AddAsync(todoItemToAdd, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await Get(todoItemToAdd.Id, cancellationToken);
    }

    [HttpPut]
    public async Task<TodoItemDto> Put(TodoItemDto dto, CancellationToken cancellationToken)
    {
        var todoItemToUpdate = await _dbContext.TodoItems.FirstOrDefaultAsync(t => t.Id == dto.Id, cancellationToken);

        if (todoItemToUpdate is null)
            throw new ResourceNotFoundException(nameof(ErrorStrings.ToDoItemCouldNotBeFound));

        var updatedTodoItem = _mapper.Map(dto, todoItemToUpdate);

        _dbContext.TodoItems.Update(updatedTodoItem);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await Get(updatedTodoItem.Id, cancellationToken);
    }

    [HttpDelete("{id:int}")]
    public async Task Delete(int id, CancellationToken cancellationToken)
    {
        _dbContext.Remove(new TodoItem { Id = id });

        var affectedRows = await _dbContext.SaveChangesAsync(cancellationToken);

        if (affectedRows < 1)
            throw new ResourceNotFoundException(nameof(ErrorStrings.ToDoItemCouldNotBeFound));
    }
}

