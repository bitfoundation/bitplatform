using AdminPanel.Api.Models.Categories;
using AdminPanel.Shared.Dtos.Categories;

namespace AdminPanel.Api.Mappers;

public class CategoryMapperConfiguration : Profile
{
    public CategoryMapperConfiguration()
    {
        CreateMap<Category, CategoryDto>().ReverseMap();
    }
}
