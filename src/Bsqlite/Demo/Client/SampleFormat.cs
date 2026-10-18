namespace Bit.Bsqlite.Demo.Client;

internal static class SampleFormat
{
    public static string YesNo(bool value) => value ? "yes" : "no";

    public static string Bytes(long? value)
        => value is null ? "unknown" : Bytes(value.Value);

    public static string Bytes(long value)
        => value switch
        {
            < 1024 => $"{value} B",
            < 1024 * 1024 => $"{value / 1024.0:0.#} KB",
            _ => $"{value / (1024.0 * 1024.0):0.#} MB",
        };

    public static string Money(decimal value) => value.ToString("N2");

    public static string When(DateTime value) => value.ToString("yyyy-MM-dd HH:mm");

    public static string When(DateTimeOffset value) => value.ToString("yyyy-MM-dd HH:mm zzz");

    public static string Day(DateOnly? value) => value?.ToString("yyyy-MM-dd") ?? "-";

    public static string Day(DateOnly value) => value.ToString("yyyy-MM-dd");

    public static string Lead(TimeSpan value)
        => value.TotalDays >= 1 && value.TotalDays == Math.Floor(value.TotalDays)
            ? $"{(int)value.TotalDays}d"
            : value.ToString("c");
}
