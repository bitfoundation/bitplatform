using System.CommandLine;
using Bit.Cli.Templates;

namespace Bit.Cli.Commands;

public sealed class SharedOptions
{
    public Option<string[]> Platforms { get; } = new("--platforms")
    {
        Description = "Platforms to set up and build now: web (always), android, ios, macos, windows. Native ones take much longer; add them later with bit setup.",
        AllowMultipleArgumentsPerToken = true,
        HelpName = "web,android,..."
    };

    public Option<string[]> Tools { get; } = new("--tools")
    {
        Description = "Tools to install when missing, e.g. node,docker,vscode. Default: the ones the project needs. none installs nothing.",
        AllowMultipleArgumentsPerToken = true,
        HelpName = "node,docker,..."
    };

    public Option<bool> Yes { get; } = new("--yes", "-y") { Description = "Accept every default and never ask a question." };

    public Option<bool> NonInteractive { get; } = new("--non-interactive") { Description = "Never ask a question; fail when a required value is missing." };

    public Option<bool> NoTools { get; } = new("--no-tools") { Description = "Don't install any tool." };

    public Option<bool> NoWorkloads { get; } = new("--no-workloads") { Description = "Don't install .NET workloads." };

    public Option<bool> NoRestore { get; } = new("--no-restore") { Description = "Don't restore NuGet packages." };

    public Option<bool> NoBuild { get; } = new("--no-build") { Description = "Don't build." };

    public Option<bool> NoBrowsers { get; } = new("--no-browsers") { Description = "Don't install Playwright's browsers for the UI tests." };

    public Option<string[]> Properties { get; } = new("--property", "-p")
    {
        Description = "An MSBuild property for the restore and the build, e.g. -p:EnforceCodeStyleInBuild=true. Repeat it for more.",
        HelpName = "name=value"
    };

    public void AddTo(Command command)
    {
        foreach (var option in new Option[] { Platforms, Tools, Yes, NonInteractive, NoTools, NoWorkloads, NoRestore, NoBuild, NoBrowsers, Properties })
        {
            command.Options.Add(option);
        }
    }

    public static IReadOnlyList<string> SplitList(IEnumerable<string>? values)
    {
        return values is null
            ? []
            : [.. values.SelectMany(v => v.Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))];
    }

    public static (IReadOnlyList<string> Properties, string? Error) ParseProperties(IEnumerable<string>? values)
    {
        var properties = (values ?? []).Select(v => v.Trim()).Where(v => v.Length > 0).ToList();

        return properties.FirstOrDefault(p => p.IndexOf('=', StringComparison.Ordinal) is <= 0) is { } invalid
            ? (properties, $"'{invalid}' isn't an MSBuild property. Use -p:name=value.")
            : (properties, null);
    }

    public static (HashSet<Platform> Platforms, string? Error) ParsePlatforms(IEnumerable<string>? values, Infrastructure.HostOs os)
    {
        var platforms = new HashSet<Platform> { Platform.Web };
        var available = Templates.Platforms.AvailableOn(os);

        foreach (var value in SplitList(values))
        {
            if (Templates.Platforms.Parse(value) is not { } platform)
                return (platforms, $"'{value}' isn't a platform. Use web, android, ios, macos or windows.");

            if (available.Contains(platform) is false)
                return (platforms, $"{Templates.Platforms.Title(platform)} apps can't be built on this operating system.");

            platforms.Add(platform);
        }

        return (platforms, null);
    }
}
