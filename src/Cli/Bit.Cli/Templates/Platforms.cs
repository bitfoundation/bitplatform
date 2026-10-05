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
        HostOs.Windows => [Platform.Web, Platform.Android, Platform.Windows],
        HostOs.MacOS => [Platform.Web, Platform.Android, Platform.Ios, Platform.MacOS],
        _ => [Platform.Web, Platform.Android]
    };

    public static IReadOnlyList<string> Workloads(IEnumerable<Platform> platforms, HostOs os)
    {
        var selected = platforms.ToHashSet();
        var workloads = new List<string> { "wasm-tools" };

        if (BuildsSolution(selected))
        {
            workloads.Add(os is HostOs.Linux ? "maui-android" : "maui");
        }

        return workloads;
    }

    public static bool BuildsSolution(IEnumerable<Platform> platforms) => platforms.Any(p => p is not Platform.Web);

    public static string BuildPath(string projectName, IEnumerable<Platform> platforms) => BuildsSolution(platforms) ? $"{projectName}.slnx" : $"{projectName}.Web.slnf";

    public static string MauiProject(string projectName) => $"src/Client/{projectName}.Client.Maui/{projectName}.Client.Maui.csproj";
}
