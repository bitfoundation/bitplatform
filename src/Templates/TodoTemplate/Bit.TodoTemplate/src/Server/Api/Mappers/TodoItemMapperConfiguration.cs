using TodoTemplate.Server.Api.Models.TodoItem;
using TodoTemplate.Shared.Dtos.TodoItem;

namespace TodoTemplate.Server.Api.Mappers;

public class TodoItemMapperConfiguration : Profile
{
    public TodoItemMapperConfiguration()
    {
        CreateMap<TodoItem, TodoItemDto>().ReverseMap();
    }
}
