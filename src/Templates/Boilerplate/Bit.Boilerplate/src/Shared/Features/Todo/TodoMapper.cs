//+:cnd:noEmit

namespace Boilerplate.Shared.Features.Todo;

[Mapper(UseDeepCloning = true)]
public static partial class TodoMapper
{
    public static partial void Patch(this TodoItemDto source, TodoItemDto destination);
}
