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
}
