//+:cnd:noEmit
namespace Boilerplate.Shared.Features.Products;

[Mapper(UseDeepCloning = true)]
public static partial class ProductsMapper
{
    public static partial void Patch(this ProductDto source, ProductDto destination);
}
