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
    private static readonly string Development = nameof(Development);
    private static readonly string Test = nameof(Test);
    private static readonly string Staging = nameof(Staging);
    private static readonly string Production = nameof(Production);

    public static string Current { get; private set; } =
#if Development            // dotnet publish -c Debug
        Development;
#elif Test                 // dotnet publish -c Release -p:Environment=Test
        Test;
#elif Staging              // dotnet publish -c Release -p:Environment=Staging
        Staging;
#else                      // dotnet publish -c Release
        Production;
#endif

    public static bool IsDevelopment()
    {
        return Is(Development);
    }

    public static bool IsTest()
    {
        return Is(Test);
    }

    public static bool IsStaging()
    {
        return Is(Staging);
    }

    public static bool IsProduction()
    {
        return Is(Production);
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
