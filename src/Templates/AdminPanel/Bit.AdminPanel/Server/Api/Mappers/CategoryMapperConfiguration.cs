using AdminPanel.Server.Api.Models.Categories;
using AdminPanel.Shared.Dtos.Categories;

namespace AdminPanel.Server.Api.Mappers;

public class CategoryMapperConfiguration : Profile
{
    public CategoryMapperConfiguration()
    {
        CreateMap<Category, CategoryDto>().ReverseMap();
    }
}
