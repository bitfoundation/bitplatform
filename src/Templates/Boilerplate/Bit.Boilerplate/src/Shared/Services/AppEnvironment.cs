//-:cnd:noEmit
namespace Boilerplate.Shared.Services;

/// <summary>
/// Unlike ASP.NET Core, which allows environment configuration via environment variables, 
/// Android, iOS, Windows, and macOS do not support the exact same concept. 
/// To maintain consistency, we introduced <see cref="AppEnvironment"/>.
/// The environment name is synchronized with ASP.NET Core environment's name in the API, Blazor Server, and Blazor WebAssembly (WASM).
/// Additionally, in Blazor Hybrid, it stays in sync with the build configuration (Debug, Release).
/// </summary>
public static partial class AppEnvironment
{
    const string DEV = "Development";
    const string STAGING = "Staging";
    const string PROD = "Production";

    public static string Current { get; private set; } =
#if Development     // dotnet publish -c Debug
        DEV;
#elif Staging       // dotnet publish -c Release -p:Environment=Staging
        STAGING;
#else               // dotnet publish -c Release
        PROD;
#endif

    public static bool IsDev()
    {
        return Is(DEV);
    }

    public static bool IsProd()
    {
        return Is(PROD);
    }

    public static bool IsStaging()
    {
        return Is(STAGING);
    }

    public static bool Is(string name)
    {
        return Current == name;
    }

    public static void Set(string name)
    {
        Current = name;
    }
}
