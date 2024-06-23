using Riok.Mapperly.Abstractions;

namespace Bit.BlazorUI.Demo.Shared;

/// <summary>
/// Patching methods help you patch the DTO you have received from the server (for example, after calling an Update api) 
/// onto the DTO you have bound to the UI. This way, the UI gets updated with the latest saved changes,
/// and there's no need to re-fetch that specific data from the server.
/// For complete end to end sample you can check EditProfilePage.razor.cs
/// You can add as many as Patch methods you want for other DTO classes here.
/// For more information and to learn about customizing the mapping process, visit the website below:
/// https://mapperly.riok.app/docs/intro/
/// </summary>
[Mapper(UseDeepCloning = true)]
public static partial class Mapper
{
}
