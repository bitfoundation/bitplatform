using Boilerplate.Shared.Dtos.Todo;

namespace Boilerplate.Shared.Controllers.Todo;

[Route("api/[controller]/[action]/")]
public interface ITodoItemController : IAppController
{
    [HttpGet("{id}")]
    Task<TodoItemDto> Get(int id, CancellationToken cancellationToken);

    [HttpPost]
    Task<TodoItemDto> Create(TodoItemDto dto, CancellationToken cancellationToken);

    [HttpPut]
    Task<TodoItemDto> Update(TodoItemDto dto, CancellationToken cancellationToken);

    [HttpDelete("{id}")]
    Task Delete(int id, CancellationToken cancellationToken);

    [HttpGet]
    Task<List<TodoItemDto>> Get(CancellationToken cancellationToken) => default!;
}
