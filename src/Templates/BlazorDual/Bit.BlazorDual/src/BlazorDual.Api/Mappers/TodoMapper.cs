using Riok.Mapperly.Abstractions;
using BlazorDual.Api.Models.Todo;
using BlazorDual.Shared.Dtos.Todo;

namespace BlazorDual.Api.Mappers;

[Mapper(UseDeepCloning = true)]
public static partial class TodoMapper
{
    public static partial IQueryable<TodoItemDto> Project(this IQueryable<TodoItem> query);
    public static partial TodoItemDto Map(this TodoItem source);
    public static partial TodoItem Map(this TodoItemDto source);
    public static partial void Patch(this TodoItemDto source, TodoItem destination);
}
