using Boilerplate.Server.Models.Products;
using Boilerplate.Shared.Dtos.Products;
using Riok.Mapperly.Abstractions;

namespace Boilerplate.Server.Mappers;

/// <summary>
/// More info at Server/Mappers/README.md
/// </summary>
[Mapper(UseDeepCloning = true)]
public static partial class ProductsMapper
{
    public static partial IQueryable<ProductDto> Project(this IQueryable<Product> query);
    public static partial ProductDto Map(this Product source);
    public static partial Product Map(this ProductDto source);
    public static partial void Patch(this ProductDto source, Product destination);
    [MapperIgnoreSource(nameof(Product.Category))]
    public static partial void Patch(this Product source, ProductDto destination);
}
