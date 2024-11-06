using Microsoft.Extensions.Logging;

namespace Boilerplate.Client.Core.Components.Pages.Diagnostic;

public class DiagnosticLog
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string? Message { get; set; }

    public LogLevel Level { get; set; }
}
