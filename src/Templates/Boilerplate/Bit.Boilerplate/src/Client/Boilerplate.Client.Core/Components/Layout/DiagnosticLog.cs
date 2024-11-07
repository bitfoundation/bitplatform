using Microsoft.Extensions.Logging;

namespace Boilerplate.Client.Core.Components.Layout;

public class DiagnosticLog
{
    public string? Message { get; set; }

    public LogLevel Level { get; set; }
}
