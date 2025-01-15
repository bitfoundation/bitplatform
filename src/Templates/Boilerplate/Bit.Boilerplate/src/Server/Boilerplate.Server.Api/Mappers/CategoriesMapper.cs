//+:cnd:noEmit
using Riok.Mapperly.Abstractions;
using Boilerplate.Shared.Dtos.Categories;
using Boilerplate.Server.Api.Models.Categories;

namespace Boilerplate.Server.Api.Mappers;

/// <summary>
/// More info at Server/Mappers/README.md
/// </summary>
[Mapper]
public static partial class CategoriesMapper
{
    public static partial IQueryable<CategoryDto> Project(this IQueryable<Category> query);

    // In reality, the utilization of [MapProperty] is unnecessary in this context.
    // This is because the 'Category' model already possesses a 'Products' property, and the 'Products' property, in turn,
    // includes a 'Count' property. By concatenating these properties, we naturally obtain 'ProductsCount,'
    // thereby leveraging automatic functionality through mapperly conventions.
    // Nevertheless, we employ MapProperty in this instance to illustrate its usage
    [MapProperty(nameof(@Category.Products.Count), nameof(@CategoryDto.ProductsCount))]
    public static partial CategoryDto Map(this Category source);
    //#if(module == "Admin")
    public static partial Category Map(this CategoryDto source);
    public static partial void Patch(this CategoryDto source, Category dest);
    //#endif
}
