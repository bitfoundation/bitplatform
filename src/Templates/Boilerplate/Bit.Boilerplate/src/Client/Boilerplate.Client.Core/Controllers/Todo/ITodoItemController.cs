using Boilerplate.Shared.Dtos.Todo;

namespace Boilerplate.Client.Core.Controllers.Todo;

[Route("api/[controller]/[action]/")]
public interface ITodoItemController : IAppController
{
    [HttpGet("{id}")]
    Task<TodoItemDto> Get(int id, CancellationToken cancellationToken = default);

    [HttpPost]
    Task<TodoItemDto> Create(TodoItemDto dto, CancellationToken cancellationToken = default);

    [HttpPut]
    Task<TodoItemDto> Update(TodoItemDto dto, CancellationToken cancellationToken = default);

    [HttpDelete("{id}")]
    Task Delete(int id, CancellationToken cancellationToken = default);

    [HttpGet]
    Task<List<TodoItemDto>> Get(CancellationToken cancellationToken = default) => default!;
}
