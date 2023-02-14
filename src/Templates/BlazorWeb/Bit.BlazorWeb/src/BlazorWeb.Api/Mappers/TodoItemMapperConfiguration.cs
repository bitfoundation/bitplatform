using BlazorWeb.Api.Models.TodoItem;
using BlazorWeb.Shared.Dtos.TodoItem;

namespace BlazorWeb.Api.Mappers;

public class TodoItemMapperConfiguration : Profile
{
    public TodoItemMapperConfiguration()
    {
        CreateMap<TodoItem, TodoItemDto>().ReverseMap();
    }
}
