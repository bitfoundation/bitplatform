//+:cnd:noEmit

namespace Boilerplate.Client.Core.Services.DiagnosticLog;

// https://learn.microsoft.com/en-us/aspnet/core/blazor/hybrid/developer-tools

/// <summary>
/// Provides a custom logger that outputs log messages to the in memory store and allows for selective display of logs
/// within the application UI for enhanced diagnostics using <see cref="DiagnosticModal"/>
/// </summary>
[ProviderAlias("DiagnosticLogger")]
public partial class DiagnosticLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
    {
        return new DiagnosticLogger()
        {
            Category = categoryName
        };
    }

    public void Dispose() { }
}
