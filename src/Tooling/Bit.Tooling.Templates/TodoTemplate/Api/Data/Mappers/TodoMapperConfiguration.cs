using TodoTemplate.Api.Data.Models.Todo;
using TodoTemplate.Shared.Dtos.Todo;

namespace TodoTemplate.Api.Data.Mappers
{
    public class TodoMapperConfiguration : Profile
    {
        public TodoMapperConfiguration()
        {
            CreateMap<Todo, TodoDto>().ReverseMap();
        }
    }
}
