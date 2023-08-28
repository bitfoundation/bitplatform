using AdminPanel.Shared.Dtos.Categories;
using AdminPanel.Shared.Dtos.Identity;
using AdminPanel.Shared.Dtos.Products;
using Riok.Mapperly.Abstractions;

namespace AdminPanel.Shared;

/// <summary>
/// Patching methods help you patch the DTO you have received from the server (for example, after calling an Update api) 
/// onto the DTO you have bound to the UI. This way, the UI gets updated with the latest saved changes,
/// and there's no need to re-fetch that specific data from the server.
/// For complete end to end sample you can checkout EditProfilePage.razor.cs
/// You can add as many as Patch methods you want for other DTO classes here.
/// For more information and to learn about customizing the mapping process, visit the website below:
/// https://mapperly.riok.app/docs/intro/
/// </summary>
[Mapper(UseDeepCloning = true)]
public static partial class Mapper
{
    public static partial void Patch(this CategoryDto source, CategoryDto destination);
    public static partial void Patch(this ProductDto source, ProductDto destination);
    public static partial void Patch(this UserDto source, UserDto destination);
}
