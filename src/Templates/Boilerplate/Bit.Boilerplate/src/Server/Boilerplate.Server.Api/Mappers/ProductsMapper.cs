//+:cnd:noEmit
using Riok.Mapperly.Abstractions;
using Boilerplate.Shared.Dtos.Products;
using Boilerplate.Server.Api.Models.Products;

namespace Boilerplate.Server.Api.Mappers;

/// <summary>
/// More info at Server/Mappers/README.md
/// </summary>
[Mapper]
public static partial class ProductsMapper
{
    public static partial IQueryable<ProductDto> Project(this IQueryable<Product> query);

    // In reality, the utilization of [MapProperty] is unnecessary in this context.
    // This is because the 'Product' model already possesses a 'Category' property, and the 'Category' property, in turn,
    // includes a 'Name' property. By concatenating these properties, we naturally obtain 'CategoryName,'
    // thereby leveraging automatic functionality through mapperly conventions.
    // Nevertheless, we employ MapProperty in this instance to illustrate its usage
    [MapProperty(nameof(@Product.Category.Name), nameof(@ProductDto.CategoryName))]
    public static partial ProductDto Map(this Product source);
    //#if(module == "Admin")
    public static partial Product Map(this ProductDto source);
    public static partial void Patch(this ProductDto source, Product dest);
    //#endif
}
