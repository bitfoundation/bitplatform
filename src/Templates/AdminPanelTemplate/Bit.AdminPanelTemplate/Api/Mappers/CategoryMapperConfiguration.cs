using AdminPanelTemplate.Api.Models.Categories;
using AdminPanelTemplate.Api.Models.TodoItem;
using AdminPanelTemplate.Shared.Dtos.Categories;
using AdminPanelTemplate.Shared.Dtos.TodoItem;

namespace AdminPanelTemplate.Api.Mappers;

public class CategoryMapperConfiguration : Profile
{
    public CategoryMapperConfiguration()
    {
        CreateMap<Category, CategoryDto>().ReverseMap();
    }
}
