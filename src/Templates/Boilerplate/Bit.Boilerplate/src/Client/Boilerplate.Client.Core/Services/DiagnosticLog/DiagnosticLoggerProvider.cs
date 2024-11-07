//+:cnd:noEmit
using Microsoft.Extensions.Logging;

namespace Boilerplate.Client.Core.Services.DiagnosticLog;

// https://learn.microsoft.com/en-us/aspnet/core/blazor/hybrid/developer-tools

/// <summary>
/// Provides a custom logger that outputs log messages to the browser's console and allows for selective display of logs
/// within the application UI for enhanced diagnostics.
/// </summary>
[ProviderAlias("Diagnostic")]
public partial class DiagnosticLoggerProvider : ILoggerProvider
{
    [AutoInject] private CurrentScopeProvider scopeProvider = default!;

    public ILogger CreateLogger(string categoryName)
    {
        return new DiagnosticLogger(scopeProvider)
        {
            CategoryName = categoryName
        };
    }

    public void Dispose()
    {

    }
}
