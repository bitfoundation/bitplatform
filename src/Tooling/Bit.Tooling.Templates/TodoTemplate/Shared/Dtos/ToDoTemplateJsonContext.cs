using TodoTemplate.Shared.Dtos.Account;
using TodoTemplate.Shared.Dtos.TodoItem;

namespace TodoTemplate.Shared.Dtos;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(TodoItemDto))]
[JsonSerializable(typeof(List<TodoItemDto>))]
[JsonSerializable(typeof(UserDto))]
[JsonSerializable(typeof(List<UserDto>))]
[JsonSerializable(typeof(RestExceptionPayload))]
public partial class ToDoTemplateJsonContext : JsonSerializerContext
{
}
