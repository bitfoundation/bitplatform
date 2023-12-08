using Boilerplate.Shared.Dtos.Todo;

namespace Boilerplate.Client.Core.Controllers.Todo;

public interface ITodoItemController : IAppControllerBase
{
    [HttpGet]
    Task<TodoItemDto> Get(int id, CancellationToken cancellationToken = default);

    [HttpPost]
    Task<TodoItemDto> Create(TodoItemDto body, CancellationToken cancellationToken = default);

    [HttpPut]
    Task<TodoItemDto> Update(TodoItemDto body, CancellationToken cancellationToken = default);

    [HttpDelete]
    Task Delete(int id, CancellationToken cancellationToken = default);

    [HttpGet]
    Task<IAsyncEnumerable<TodoItemDto>> Get(CancellationToken cancellationToken = default) => default!;
}
