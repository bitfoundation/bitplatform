using BlazorDual.Api.Models.TodoItem;
using BlazorDual.Shared.Dtos.TodoItem;

namespace BlazorDual.Api.Mappers;

public class TodoItemMapperConfiguration : Profile
{
    public TodoItemMapperConfiguration()
    {
        CreateMap<TodoItem, TodoItemDto>().ReverseMap();
    }
}
