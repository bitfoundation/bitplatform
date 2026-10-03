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

public sealed record BuildTarget(Platform Platform, string ProjectPath, string? TargetFramework);

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

        if (selected.Contains(Platform.Android) || selected.Contains(Platform.Ios) || selected.Contains(Platform.MacOS))
        {
            workloads.Add(os is HostOs.Linux ? "maui-android" : "maui");
        }

        return workloads;
    }

    public static IReadOnlyList<BuildTarget> BuildTargets(string projectName, IEnumerable<Platform> platforms, string targetFrameworkVersion)
    {
        var targets = new List<BuildTarget>();
        var maui = $"src/Client/{projectName}.Client.Maui/{projectName}.Client.Maui.csproj";

        foreach (var platform in platforms.Distinct().Order())
        {
            targets.Add(platform switch
            {
                Platform.Web => new BuildTarget(platform, $"{projectName}.Web.slnf", null),
                Platform.Android => new BuildTarget(platform, maui, $"{targetFrameworkVersion}-android"),
                Platform.Ios => new BuildTarget(platform, maui, $"{targetFrameworkVersion}-ios"),
                Platform.MacOS => new BuildTarget(platform, maui, $"{targetFrameworkVersion}-maccatalyst"),
                _ => new BuildTarget(platform, $"src/Client/{projectName}.Client.Windows/{projectName}.Client.Windows.csproj", null)
            });
        }

        return targets;
    }
}
