namespace Bit.Brouter.Tests.E2E.Infrastructure;

/// <summary>
/// Everything the suite reads from the environment, in one place. Environment variables rather than
/// runsettings, which the Microsoft.Testing.Platform runner does not thread through reliably.
/// </summary>
public static class E2EEnvironment
{
    /// <summary>
    /// <c>BROUTER_E2E_FRAMEWORK</c>: the target framework the web harness host runs on
    /// (net10.0, net9.0 or net8.0). Defaults to net10.0. The hybrid host is always net10.0-windows.
    /// </summary>
    public static string Framework { get; } = Read("BROUTER_E2E_FRAMEWORK") ?? "net10.0";

    /// <summary>
    /// <c>BROUTER_E2E_CONFIGURATION</c>: the build configuration of the harness hosts. Defaults to the
    /// configuration this test assembly was built in.
    /// </summary>
    public static string Configuration { get; } = Read("BROUTER_E2E_CONFIGURATION") ??
#if DEBUG
        "Debug";
#else
        "Release";
#endif

    /// <summary>
    /// <c>BROUTER_E2E_PUBLISHED_HOST</c>: a folder holding a <c>dotnet publish</c> output of
    /// Bit.Brouter.Tests.Harness.Web. When set the suite runs that (trimmed, AOT-compiled, ...) build
    /// instead of the project's build output, and builds nothing.
    /// </summary>
    public static string? PublishedHost { get; } = Read("BROUTER_E2E_PUBLISHED_HOST");

    /// <summary><c>BROUTER_E2E_SKIP_BUILD=1</c>: the hosts were built beforehand (as CI does); do not rebuild them.</summary>
    public static bool SkipBuild { get; } = Read("BROUTER_E2E_SKIP_BUILD") == "1";

    /// <summary><c>BROUTER_E2E_CHANNEL</c>: e.g. chrome / msedge, to use an installed browser.</summary>
    public static string? Channel { get; } = Read("BROUTER_E2E_CHANNEL");

    /// <summary><c>BROUTER_E2E_EXECUTABLE</c>: full path to a chromium-family executable.</summary>
    public static string? Executable { get; } = Read("BROUTER_E2E_EXECUTABLE");

    /// <summary><c>BROUTER_E2E_HEADED=1</c>: show the browser.</summary>
    public static bool Headed { get; } = Read("BROUTER_E2E_HEADED") == "1";

    /// <summary>RendererInfo, which reports the renderer name, only exists from .NET 9 on.</summary>
    public static bool FrameworkReportsRendererName(string framework) => framework is not "net8.0";

    /// <summary>NavigationManager.NotFound, which lets static rendering answer 404, only exists from .NET 10 on.</summary>
    public static bool FrameworkHasNotFound(string framework) => framework is "net10.0";

    private static string? Read(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
