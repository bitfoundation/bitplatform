//+:cnd:noEmit
namespace Boilerplate.Shared.Features.Todo;

//#if (offlineDb == true)
// The following controller is not required when using the offline database.
// The controller that works with the offline database is implemented in TodoItemTableController.cs
//#endif

[Route("api/v1/[controller]/[action]/"), AuthorizedApi]
public interface ITodoItemController : IAppController
{
    [HttpGet("{id}")]
    Task<TodoItemDto> Get(string id, CancellationToken cancellationToken);

    [HttpPost]
    Task<TodoItemDto> Create(TodoItemDto dto, CancellationToken cancellationToken);

    [HttpPut]
    Task<TodoItemDto> Update(TodoItemDto dto, CancellationToken cancellationToken);

    [HttpDelete("{id}")]
    Task Delete(string id, CancellationToken cancellationToken);

    [HttpGet]
    Task<List<TodoItemDto>> Get(CancellationToken cancellationToken) => default!;
}
