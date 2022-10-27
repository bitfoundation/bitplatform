using AdminPanel.Server.Api.Models.Products;
using AdminPanel.Shared.Dtos.Products;

namespace AdminPanel.Server.Api.Mappers;

public class ProductMapperConfiguration : Profile
{
    public ProductMapperConfiguration()
    {
        CreateMap<Product, ProductDto>().ReverseMap();
    }
}
