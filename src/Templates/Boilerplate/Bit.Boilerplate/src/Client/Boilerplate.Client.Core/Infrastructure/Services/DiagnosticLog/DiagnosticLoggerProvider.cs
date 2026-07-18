//+:cnd:noEmit

using Boilerplate.Client.Core.Components.Layout.Diagnostic;

namespace Boilerplate.Client.Core.Infrastructure.Services.DiagnosticLog;

// https://learn.microsoft.com/en-us/aspnet/core/blazor/hybrid/developer-tools

/// <summary>
/// Provides a custom logger that outputs log messages to the in memory store and allows for selective display of logs
/// within the application UI for enhanced diagnostic using <see cref="AppDiagnosticModal"/>
/// </summary>
[ProviderAlias("DiagnosticLogger")]
public partial class DiagnosticLoggerProvider(TimeProvider timeProvider) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
    {
        return new DiagnosticLogger(timeProvider)
        {
            Category = categoryName
        };
    }

    public void Dispose() { }
}
