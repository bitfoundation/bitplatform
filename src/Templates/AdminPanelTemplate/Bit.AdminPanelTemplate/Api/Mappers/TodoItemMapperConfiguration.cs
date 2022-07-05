using AdminPanelTemplate.Api.Models.TodoItem;
using AdminPanelTemplate.Shared.Dtos.TodoItem;

namespace AdminPanelTemplate.Api.Mappers;

public class TodoItemMapperConfiguration : Profile
{
    public TodoItemMapperConfiguration()
    {
        CreateMap<TodoItem, TodoItemDto>().ReverseMap();
    }
}
