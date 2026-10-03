using System.CommandLine;
using Bit.Minifier;
using Spectre.Console;

namespace Bit.Cli.Commands;

public static class DecodeCommand
{
    private const string Usage = "usage: bit decode [<map file>] [<stack trace file>]";

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

        command.SetAction(parseResult =>
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

            return Decode.Run(Usage, trace is null ? ["--decode", map] : ["--decode", map, trace]);
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
}
