using System.CommandLine;
using System.Text.Json;
using System.Text.RegularExpressions;
using Bit.Cli.Infrastructure;
using Spectre.Console;

namespace Bit.Cli.Commands;

public static partial class DecodeCommand
{
    public static Command Create(Func<CliServices> services)
    {
        var mapArgument = new Argument<string?>("map")
        {
            Description = "The bit-minifier.map a publish wrote. Without it, the newest one under obj/ in the current folder is used.",
            Arity = ArgumentArity.ZeroOrOne
        };

        var traceArgument = new Argument<string?>("trace")
        {
            Description = "A file with the stack trace. Without it, the trace is read from standard input.",
            Arity = ArgumentArity.ZeroOrOne
        };

        var command = new Command("decode", "Read a stack trace of an app minified by Bit.Minifier back into the names its source has.")
        {
            mapArgument,
            traceArgument
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var cli = services();
            var map = parseResult.GetValue(mapArgument);
            var trace = parseResult.GetValue(traceArgument);

            if (map is not null && trace is null && map.EndsWith(".map", StringComparison.OrdinalIgnoreCase) is false && File.Exists(map))
            {
                (map, trace) = (null, map);
            }

            if (map is null)
            {
                map = FindMap(cli.Environment.CurrentDirectory);

                if (map is null)
                {
                    cli.Console.Fail("No bit-minifier.map found under obj/ here. Pass the map's path: bit decode <map> [<trace>]");
                    return CliApp.ExitUsage;
                }

                cli.Console.Error.MarkupLine($"[grey]Using {Markup.Escape(Path.GetRelativePath(cli.Environment.CurrentDirectory, map))}[/]");
            }

            map = Path.GetFullPath(map, cli.Environment.CurrentDirectory);
            trace = trace is null ? null : Path.GetFullPath(trace, cli.Environment.CurrentDirectory);

            if (File.Exists(map) is false)
            {
                cli.Console.Fail($"No map at {map}.");
                return CliApp.ExitUsage;
            }

            if (ReadMinifierVersion(map) is not { } version)
            {
                cli.Console.Fail($"{map} doesn't name the Bit.Minifier that wrote it: its first line should be \"# Bit.Minifier <version>\".");
                return CliApp.ExitUsage;
            }

            if ((FindMinifier(cli.Environment, version) ?? await RestoreMinifierAsync(cli, version, cancellationToken)) is not { } minifier)
                return CliApp.ExitFailed;

            var result = await cli.Runner.RunAsync(new ProcessSpec
            {
                FileName = "dotnet",
                Arguments = trace is null ? [minifier, "--decode", map] : [minifier, "--decode", map, trace],
                Interactive = true
            }, cancellationToken);

            if (result.NotFound)
            {
                cli.Console.Fail("dotnet isn't on PATH.");
                return CliApp.ExitFailed;
            }

            return result.ExitCode;
        });

        return command;
    }

    public static string? FindMap(string directory)
    {
        try
        {
            return Directory.EnumerateFiles(directory, "bit-minifier.map", new EnumerationOptions { RecurseSubdirectories = true, MaxRecursionDepth = 8, IgnoreInaccessible = true })
                .Where(path => path.Replace('\\', '/').Contains("/obj/", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();
        }
        catch (Exception exp) when (exp is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    public static string? ReadMinifierVersion(string map)
    {
        using var reader = new StreamReader(map);
        return MinifierHeaderRegex().Match(reader.ReadLine() ?? "") is { Success: true } match ? match.Groups["version"].Value : null;
    }

    public static string? FindMinifier(CliEnvironment environment, string version)
    {
        return MinifierIn(environment.GetVariable("NUGET_PACKAGES") ?? Path.Combine(environment.HomeDirectory, ".nuget", "packages"), version);
    }

    private static string? MinifierIn(string packages, string version)
    {
        var dll = Path.Combine(packages, "bit.minifier", version.ToLowerInvariant(), "tools", "Bit.Minifier.dll");
        return File.Exists(dll) ? dll : null;
    }

    private static async Task<string?> RestoreMinifierAsync(CliServices cli, string version, CancellationToken cancellationToken)
    {
        cli.Console.Error.MarkupLine($"[grey]Restoring Bit.Minifier {Markup.Escape(version)}[/]");
        var folder = Directory.CreateTempSubdirectory("bit-decode-").FullName;

        try
        {
            File.WriteAllText(Path.Combine(folder, "restore.csproj"), $"""
                <Project Sdk="Microsoft.NET.Sdk">
                    <PropertyGroup>
                        <TargetFramework>net$(BundledNETCoreAppTargetFrameworkVersion)</TargetFramework>
                    </PropertyGroup>
                    <ItemGroup>
                        <PackageDownload Include="Bit.Minifier" Version="[{version}]" />
                    </ItemGroup>
                </Project>
                """);

            var result = await cli.Runner.RunAsync(new ProcessSpec
            {
                FileName = "dotnet",
                Arguments = ["restore", "restore.csproj", $"-p:RestoreRootConfigDirectory={cli.Environment.CurrentDirectory}"],
                WorkingDirectory = folder,
                Timeout = TimeSpan.FromMinutes(5)
            }, cancellationToken);

            if (result.Succeeded && RestoredMinifier(folder, version) is { } dll)
                return dll;

            cli.Console.Fail($"Couldn't restore Bit.Minifier {version} from this machine's NuGet sources.");

            foreach (var line in result.OutputLines.Where(l => l.Contains("error", StringComparison.OrdinalIgnoreCase)))
            {
                cli.Console.Error.WriteLine(line[line.IndexOf("error", StringComparison.OrdinalIgnoreCase)..].Trim());
            }

            return null;
        }
        finally
        {
            try
            {
                Directory.Delete(folder, recursive: true);
            }
            catch (Exception exp) when (exp is IOException or UnauthorizedAccessException)
            {
            }
        }
    }

    private static string? RestoredMinifier(string folder, string version)
    {
        var assets = Path.Combine(folder, "obj", "project.assets.json");

        if (File.Exists(assets) is false)
            return null;

        using var json = JsonDocument.Parse(File.ReadAllText(assets));

        return json.RootElement.TryGetProperty("packageFolders", out var folders)
            ? folders.EnumerateObject().Select(f => MinifierIn(f.Name, version)).FirstOrDefault(dll => dll is not null)
            : null;
    }

    [GeneratedRegex(@"^# Bit\.Minifier (?<version>\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?)$")]
    private static partial Regex MinifierHeaderRegex();
}
