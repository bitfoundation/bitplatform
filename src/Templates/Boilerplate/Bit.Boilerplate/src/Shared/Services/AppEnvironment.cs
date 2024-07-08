//-:cnd:noEmit
namespace Boilerplate.Shared.Services;

/// <summary>
/// Unlike ASP.NET Core, which allows environment configuration via environment variables, 
/// Android, iOS, Windows, and macOS do not support the exact same concept. 
/// To maintain consistency, we introduced <see cref="AppEnvironment"/>.
/// The environment name is synchronized with ASP.NET Core environment's name in the API, 
/// Blazor Server, and Blazor WebAssembly (WASM).
/// Additionally, in Blazor Hybrid, it stays in sync with the build configuration (Debug, Release).
/// </summary>
public static class AppEnvironment
{
    public static string Name { get; set; } =
#if Development         // dotnet publish -c Debug
        "Development";
#elif Staging           // dotnet publish -c Release -p:Environment=Staging
        "Staging";
#else                   // dotnet publish -c Release
        "Production";
#endif

    public static bool IsDevelopment()
    {
        return Name == "Development";
    }

    public static bool IsProduction()
    {
        return Name == "Production";
    }

    public static bool IsStaging()
    {
        return Name == "Staging";
    }

    public static bool IsEnvironment(string name)
    {
        return Name == name;
    }
}
