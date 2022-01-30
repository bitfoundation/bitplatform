using TodoTemplate.Api.Data.Models.TodoItem;
using TodoTemplate.Shared.Dtos.TodoItem;

namespace TodoTemplate.Api.Data.Mappers
{
    public class TodoItemMapperConfiguration : Profile
    {
        public TodoItemMapperConfiguration()
        {
            CreateMap<TodoItem, TodoItemDto>().ReverseMap();
        }
    }
}
