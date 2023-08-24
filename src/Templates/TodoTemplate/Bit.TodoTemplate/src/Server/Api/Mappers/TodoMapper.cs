using Riok.Mapperly.Abstractions;
using TodoTemplate.Server.Api.Models.Todo;
using TodoTemplate.Shared.Dtos.Todo;

namespace TodoTemplate.Server.Api.Mappers;

[Mapper(UseDeepCloning = true)]
public static partial class TodoMapper
{
    public static partial IQueryable<TodoItemDto> Project(this IQueryable<TodoItem> query);
    public static partial TodoItemDto Map(this TodoItem source);
    public static partial TodoItem Map(this TodoItemDto source);
    public static partial void Patch(this TodoItemDto source, TodoItem destination);
}
