using AdminPanelTemplate.Api.Models.Categories;
using AdminPanelTemplate.Shared.Dtos.Categories;

namespace AdminPanelTemplate.Api.Mappers;

public class CategoryMapperConfiguration : Profile
{
    public CategoryMapperConfiguration()
    {
        CreateMap<Category, CategoryDto>().ReverseMap();
    }
}
