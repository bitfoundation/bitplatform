using Microsoft.Extensions.Logging;

namespace Boilerplate.Shared.Dtos.Diagnostic;

public class DiagnosticLogDto
{
    public DateTimeOffset CreatedOn { get; set; }

    public LogLevel Level { get; set; }

    public string? Message { get; set; }

    public string? Category { get; set; }

    public string? ExceptionString { get; set; }

    public IDictionary<string, string?>? State { get; set; }

    public override string ToString()
    {
        return $@"Last Error Details:
CreatedOn Utc: {CreatedOn}
Message: {Message}
Category: {Category}
Exception: {ExceptionString}
State: {string.Join(", ", State?.Select(kv => $"{Environment.NewLine}{kv.Key}={kv.Value}") ?? [])}
";
    }
}
