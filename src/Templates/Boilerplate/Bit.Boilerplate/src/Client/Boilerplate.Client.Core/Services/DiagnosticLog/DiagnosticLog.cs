namespace Boilerplate.Client.Core.Services.DiagnosticLog;

public class DiagnosticLog
{
    public DateTimeOffset CreatedOn { get; set; }

    public LogLevel Level { get; set; }

    public string? Message { get; set; }

    public string? Category { get; set; }

    public Exception? Exception { get; set; }

    public IDictionary<string, string?>? State { get; set; }
}
