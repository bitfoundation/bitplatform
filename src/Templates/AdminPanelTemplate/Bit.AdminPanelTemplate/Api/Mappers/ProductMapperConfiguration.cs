using AdminPanelTemplate.Api.Models.Products;
using AdminPanelTemplate.Shared.Dtos.Products;

namespace AdminPanelTemplate.Api.Mappers;

public class ProductMapperConfiguration : Profile
{
    public ProductMapperConfiguration()
    {
        CreateMap<Product, ProductDto>().ReverseMap();
    }
}
