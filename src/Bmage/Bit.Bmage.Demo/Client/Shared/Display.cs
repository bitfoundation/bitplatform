namespace Bit.Bmage.Demo.Client.Shared;

/// <summary>Formatting shared by the demo pages.</summary>
public static class Display
{
    /// <summary>A byte count in the largest unit that keeps it readable.</summary>
    public static string Bytes(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:0.#} KB",
        _ => $"{bytes / (1024.0 * 1024):0.##} MB",
    };

    /// <summary>
    /// How much smaller (or larger) the output is than the input. Re-encoding an already tiny or heavily compressed
    /// file can grow it, and a demo that showed "-280% saved" in green would be lying about it.
    /// </summary>
    public static string Change(long before, long after)
    {
        if (before <= 0) return "—";
        var percent = Math.Round(100 - after * 100.0 / before);
        return percent >= 0 ? $"{percent:0}% smaller" : $"{-percent:0}% larger";
    }

    /// <summary>True when the output is smaller than the input, for styling <see cref="Change"/>.</summary>
    public static bool IsSmaller(long before, long after) => before > 0 && after < before;
}
