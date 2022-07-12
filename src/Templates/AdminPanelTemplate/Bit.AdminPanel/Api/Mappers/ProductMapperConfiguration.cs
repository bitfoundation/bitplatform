using AdminPanel.Api.Models.Products;
using AdminPanel.Shared.Dtos.Products;

namespace AdminPanel.Api.Mappers;

public class ProductMapperConfiguration : Profile
{
    public ProductMapperConfiguration()
    {
        CreateMap<Product, ProductDto>().ReverseMap();
    }
}
