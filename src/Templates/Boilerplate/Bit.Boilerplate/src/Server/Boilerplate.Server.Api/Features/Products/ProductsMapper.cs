//+:cnd:noEmit
using Boilerplate.Shared.Features.Products;

namespace Boilerplate.Server.Api.Features.Products;

/// <summary>
/// More info at src/Server/Boilerplate.Server.Api/Features/Mappers.md
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
    // ShortId is allocated by the server (See Product.ShortId) and HasPrimaryImage is written by AttachmentController
    // and by ProductController.Create, so neither may come from the client. PrimaryImageAltText is deliberately NOT in
    // that list: the admin form offers it as an editable field, seeded from the image analysis agent when one runs.
    [MapperIgnoreSource(nameof(Product.ShortId))]
    [MapperIgnoreTarget(nameof(Product.HasPrimaryImage))]
    public static partial Product Map(this ProductDto source);

    [MapperIgnoreSource(nameof(Product.ShortId))]
    [MapperIgnoreTarget(nameof(Product.HasPrimaryImage))]
    public static partial void Patch(this ProductDto source, Product destination);
    //#endif
}
