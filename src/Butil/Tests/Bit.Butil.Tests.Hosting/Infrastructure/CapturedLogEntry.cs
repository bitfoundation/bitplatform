using Microsoft.Extensions.Logging;

namespace ButilTests.Hosting.Infrastructure;

public sealed record CapturedLogEntry(string Category, LogLevel Level, string Message, Exception? Exception)
{
    public override string ToString() => $"[{Level}] {Category}: {Message}{(Exception is null ? "" : " - " + Exception.GetType().Name + ": " + Exception.Message)}";
}
