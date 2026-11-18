using Microsoft.Extensions.Logging;

namespace Bit.BevTools;

/// <summary>Log levels as BevTools names and parses them.</summary>
public static class BevToolsLogLevels
{
    public static string Label(LogLevel level) => level switch
    {
        LogLevel.Trace => "trce",
        LogLevel.Debug => "dbug",
        LogLevel.Information => "info",
        LogLevel.Warning => "warn",
        LogLevel.Error => "fail",
        LogLevel.Critical => "crit",
        _ => "none",
    };

    /// <summary>Reads "warning", "warn", "Error", "fail", "info", "4"… (what a filter's <c>level:</c> accepts); null for anything else.</summary>
    public static LogLevel? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        text = text.Trim();
        if (int.TryParse(text, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var number))
        {
            return number is >= 0 and <= 5 ? (LogLevel)number : null;
        }

        foreach (var level in Enum.GetValues<LogLevel>())
        {
            if (level != LogLevel.None && (Label(level).Equals(text, StringComparison.OrdinalIgnoreCase) || level.ToString().StartsWith(text, StringComparison.OrdinalIgnoreCase)))
            {
                return level;
            }
        }

        return null;
    }
}
