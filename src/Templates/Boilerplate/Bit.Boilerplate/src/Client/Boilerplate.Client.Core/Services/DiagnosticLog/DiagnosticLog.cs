using Microsoft.Extensions.Logging;

namespace Boilerplate.Client.Core.Services.DiagnosticLog;

public class DiagnosticLog
{
    public LogLevel Level { get; set; }

    public string? Message { get; set; }

    public Exception? Exception { get; set; }

    public IDictionary<string, string?>? State { get; set; }
}
