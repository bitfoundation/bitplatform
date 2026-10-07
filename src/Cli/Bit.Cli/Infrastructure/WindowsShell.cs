using System.Text.RegularExpressions;

namespace Bit.Cli.Infrastructure;

public static partial class WindowsShell
{
    public static bool IsBatchFile(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".cmd", StringComparison.OrdinalIgnoreCase) || extension.Equals(".bat", StringComparison.OrdinalIgnoreCase);
    }

    public static string CommandLine(string batchFile, IEnumerable<string> arguments)
    {
        return $"/d /s /c \"{string.Join(' ', arguments.Select(EscapeArgument).Prepend(Escape(batchFile)))}\"";
    }

    public static string EscapeArgument(string argument)
    {
        argument = BackslashesBeforeQuoteRegex().Replace(argument, m => m.Groups[1].Value + m.Groups[1].Value + "\\\"");
        argument = TrailingBackslashesRegex().Replace(argument, m => m.Groups[1].Value + m.Groups[1].Value);
        return Escape($"\"{argument}\"");
    }

    private static string Escape(string value) => MetaCharacterRegex().Replace(value, "^$1");

    [GeneratedRegex(@"([()\][%!^""`<>&|;, *?])")]
    private static partial Regex MetaCharacterRegex();

    [GeneratedRegex(@"(\\*)""")]
    private static partial Regex BackslashesBeforeQuoteRegex();

    [GeneratedRegex(@"(\\*)$")]
    private static partial Regex TrailingBackslashesRegex();
}
