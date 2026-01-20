//+:cnd:noEmit
namespace Boilerplate.Shared.Features.Categories;

[Mapper(UseDeepCloning = true)]
public static partial class CategoriesMapper
{
    public static partial void Patch(this CategoryDto source, CategoryDto destination);
}
