using AdminPanel.Server.Api.Models.Categories;
using AdminPanel.Shared.Dtos.Categories;
using Riok.Mapperly.Abstractions;

namespace AdminPanel.Server.Api.Mappers;

[Mapper(UseDeepCloning = true)]
public static partial class CategoriesMapper
{
    public static partial IQueryable<CategoryDto> Project(this IQueryable<Category> query);
    public static partial CategoryDto Map(this Category source);
    public static partial Category Map(this CategoryDto source);
    public static partial void Patch(this CategoryDto source, Category destination);
    public static partial void Patch(this Category source, CategoryDto destination);
}
