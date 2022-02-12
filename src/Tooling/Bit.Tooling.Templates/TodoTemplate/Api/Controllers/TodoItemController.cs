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

    [HttpGet]
    public IQueryable<TodoItemDto> Get(CancellationToken cancellationToken)
    {
        return _dbContext.TodoItems
            .Where(t => t.UserId == User.GetUserId())
            .ProjectTo<TodoItemDto>(_mapper.ConfigurationProvider, cancellationToken);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TodoItemDto>> Get(int id, CancellationToken cancellationToken)
    {
        var todoItem = await Get(cancellationToken).FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (todoItem is null)
            return NotFound();

        return Ok(todoItem);
    }

    [HttpPost]
    public async Task Post(TodoItemDto dto, CancellationToken cancellationToken)
    {
        var todoItemToAdd = _mapper.Map<TodoItem>(dto);

        todoItemToAdd.UserId = User.GetUserId();

        await _dbContext.TodoItems.AddAsync(todoItemToAdd, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    [HttpPut]
    public async Task<IActionResult> Put(TodoItemDto dto, CancellationToken cancellationToken)
    {
        var todoItemToUpdate = await _dbContext.TodoItems.FirstOrDefaultAsync(t => t.Id == dto.Id, cancellationToken);

        if (todoItemToUpdate is null)
            return NotFound();

        var updatedTodoItem = _mapper.Map(dto, todoItemToUpdate);

        _dbContext.TodoItems.Update(updatedTodoItem);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        _dbContext.Remove(new TodoItem { Id = id });

        var affectedRows = await _dbContext.SaveChangesAsync(cancellationToken);

        if (affectedRows < 1)
            return NotFound();

        return Ok();
    }
}

