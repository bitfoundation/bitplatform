using Riok.Mapperly.Abstractions;
using WebTemplate.Api.Models.Todo;
using WebTemplate.Shared.Dtos.Todo;

namespace WebTemplate.Api.Mappers;

/// <summary>
/// More info at Server/Api/Mappers/README.md
/// </summary>
[Mapper(UseDeepCloning = true)]
public static partial class TodoMapper
{
    public static partial IQueryable<TodoItemDto> Project(this IQueryable<TodoItem> query);
    public static partial TodoItemDto Map(this TodoItem source);
    public static partial TodoItem Map(this TodoItemDto source);
    public static partial void Patch(this TodoItemDto source, TodoItem destination);
}
