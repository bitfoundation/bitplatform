using Boilerplate.Server.Api.Models.Products;
using Boilerplate.Shared.Dtos.Products;
using Riok.Mapperly.Abstractions;

namespace Boilerplate.Server.Api.Mappers;

/// <summary>
/// More info at Server/Mappers/README.md
/// </summary>
[Mapper(UseDeepCloning = true)]
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
    public static partial Product Map(this ProductDto source);
    public static partial void Patch(this ProductDto source, Product destination);

    // It is important to note that when a ProductDto is sent to the server,
    // it may contain a 'CategoryName'. However, while Mappingly's automatic conventions may fill Category property of product model
    // with only its Name property populated by the 'CategoryName' value while the Id remains 0,
    // this oversight could lead to ef core mistakenly assuming a new category is being added to the database during product saving,
    // resulting in unintended data persistence. That's why we need to ignore 'CategoryName' during Patching and Mapping manually.
    [MapperIgnoreSource(nameof(Product.Category))]
    public static partial void Patch(this Product source, ProductDto destination);
}
