using Bit.Cli.Infrastructure;

namespace Bit.Cli.Templates;

public enum Platform
{
    Web,
    Android,
    Ios,
    MacOS,
    Windows
}

public static class Platforms
{
    public static string Name(Platform platform) => platform switch
    {
        Platform.Web => "web",
        Platform.Android => "android",
        Platform.Ios => "ios",
        Platform.MacOS => "macos",
        _ => "windows"
    };

    public static string Title(Platform platform) => platform switch
    {
        Platform.Web => "Web",
        Platform.Android => "Android",
        Platform.Ios => "iOS",
        Platform.MacOS => "macOS",
        _ => "Windows"
    };

    public static Platform? Parse(string value) => value.Trim().ToLowerInvariant() switch
    {
        "web" or "wasm" or "blazor" => Platform.Web,
        "android" => Platform.Android,
        "ios" or "iphone" => Platform.Ios,
        "macos" or "mac" or "maccatalyst" => Platform.MacOS,
        "windows" or "win" => Platform.Windows,
        _ => null
    };

    public static IReadOnlyList<Platform> AvailableOn(HostOs os) => os switch
    {
        HostOs.Windows => [Platform.Web, Platform.Android, Platform.Ios, Platform.MacOS, Platform.Windows],
        HostOs.MacOS => [Platform.Web, Platform.Android, Platform.Ios, Platform.MacOS],
        _ => [Platform.Web, Platform.Android]
    };

    public static IReadOnlyList<Platform> RunnableOn(HostOs os) => os switch
    {
        HostOs.Windows => [Platform.Web, Platform.Android, Platform.Windows],
        HostOs.MacOS => [Platform.Web, Platform.Android, Platform.Ios, Platform.MacOS],
        _ => [Platform.Web, Platform.Android]
    };

    public const string WebAssemblyWorkload = "wasm-tools";

    public static IReadOnlyList<string> Workloads(IEnumerable<Platform> platforms, HostOs os) => [WebAssemblyWorkload, .. NativeWorkloads(platforms, os)];

    public static IReadOnlyList<string> NativeWorkloads(IEnumerable<Platform> platforms, HostOs os) => BuildsSolution(platforms) ? [MauiWorkload(os)] : [];

    public static string MauiWorkload(HostOs os) => os is HostOs.Linux ? "maui-android" : "maui";

    public static string? MauiTarget(Platform platform) => platform switch
    {
        Platform.Android => "android",
        Platform.Ios => "ios",
        Platform.MacOS => "maccatalyst",
        _ => null
    };

    public static string BuildSupport(IEnumerable<Platform> platforms) => Support(["WebAssembly", .. NativeTitles(platforms)]);

    public static string NativeBuildSupport(IEnumerable<Platform> platforms) => Support(NativeTitles(platforms));

    private static string[] NativeTitles(IEnumerable<Platform> platforms) => [.. platforms.Where(p => p is not Platform.Web).Order().Select(Title)];

    private static string Support(string[] names) => $"{(names.Length == 1 ? names[0] : $"{string.Join(", ", names[..^1])} and {names[^1]}")} build support";

    public static bool BuildsSolution(IEnumerable<Platform> platforms) => platforms.Any(p => p is not Platform.Web);

    public static string MauiProject(string projectName) => $"src/Client/{projectName}.Client.Maui/{projectName}.Client.Maui.csproj";

    public static string WindowsProject(string projectName) => $"src/Client/{projectName}.Client.Windows/{projectName}.Client.Windows.csproj";

    public static string WorkbenchProject(string projectName) => $"src/Workbench/{projectName}.Workbench.csproj";
}
