using AdminPanel.Shared.Dtos.Categories;
using AdminPanel.Shared.Dtos.Products;
using Riok.Mapperly.Abstractions;

namespace AdminPanel.Shared;

/// <summary>
/// Patching methods help you patch the DTO you have received from the server (for example, after calling an Update api) 
/// onto the DTO you have bound to the UI. This way, the UI gets updated with the latest saved changes,
/// and there's no need to re-fetch that specific data from the server.
/// You can add as many as Patch methods you want for other DTO classes here.
/// Currently, although the server-side AdminPanel controllers return the latest saved changes, 
/// the returned response is not yet used when saving the Product / Category in client side,
/// instead, a few records re-fetched from the server. To see a complete sample code, you can check the TodoPage.razor.cs code in the TodoTemplate.
/// For more information and to learn about customizing the mapping process, visit the website below:
/// https://mapperly.riok.app/docs/intro/
/// </summary>
[Mapper(UseDeepCloning = true)]
public static partial class Mapper
{
    public static partial void Patch(this CategoryDto source, CategoryDto destination);
    public static partial void Patch(this ProductDto source, ProductDto destination);
}
