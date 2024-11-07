//+:cnd:noEmit
using Microsoft.Extensions.Logging;

namespace Boilerplate.Client.Core.Services;

// https://learn.microsoft.com/en-us/aspnet/core/blazor/hybrid/developer-tools

/// <summary>
/// Provides a custom logger that outputs log messages to the browser's console and allows for selective display of logs
/// within the application UI for enhanced diagnostics.
/// </summary>
[ProviderAlias("DevInsights")]
public partial class DevInsightsLoggerProvider : ILoggerProvider, ILogger, IDisposable
{
    private static IJSRuntime? jsRuntime;
    private static Bit.Butil.Console? console;
    private static NavigationManager? navigationManager;
    private static readonly ConcurrentQueue<IDictionary<string, object?>> states = new();

    public static void SetServiceProvider(IServiceProvider serviceProvider)
    {
        if (IsSupported() is false)
            throw new InvalidOperationException($"{nameof(DevInsightsLoggerProvider)} is supported.");

        DevInsightsLoggerProvider.jsRuntime = serviceProvider.GetRequiredService<IJSRuntime>();
        DevInsightsLoggerProvider.console = serviceProvider.GetRequiredService<Bit.Butil.Console>();
        DevInsightsLoggerProvider.navigationManager = serviceProvider.GetRequiredService<NavigationManager>();
    }

    [JSInvokable(nameof(ShowLogs))]
    public static void ShowLogs()
    {
        if (IsSupported() is false)
            throw new InvalidOperationException($"{nameof(DevInsightsLoggerProvider)} is supported.");

        if (CheckIfBrowserContextIsReady() is false)
            return;

        // DotNet.invokeMethodAsync('Boilerplate.Client.Core', 'ShowLogs');

        DevInsightsLoggerProvider.navigationManager!.NavigateTo(Urls.SettingsPage);
    }

    public string? CategoryName { get; init; }

    public ILogger CreateLogger(string categoryName)
    {
        return new DevInsightsLoggerProvider
        {
            CategoryName = categoryName
        };
    }

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        if (state is IDictionary<string, object?> data)
        {
            data[nameof(CategoryName)] = CategoryName;
            states.Enqueue(data);
        }

        return this;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return IsSupported() && logLevel != LogLevel.None;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (IsEnabled(logLevel) is false) return;

        var message = formatter(state, exception);

        states.TryDequeue(out var currentState);

        // Store logs in the memory to be shown later.

        if (CheckIfBrowserContextIsReady() is false)
            return;

        switch (logLevel)
        {
            case LogLevel.Trace:
            case LogLevel.Debug:
                console!.Log(message, $"{Environment.NewLine}Category:", CategoryName, $"{Environment.NewLine}State:", currentState);
                break;
            case LogLevel.Information:
                console!.Info(message, $"{Environment.NewLine}Category:", CategoryName, $"{Environment.NewLine}State:", currentState);
                break;
            case LogLevel.Warning:
                console!.Warn(message, $"{Environment.NewLine}Category:", CategoryName, $"{Environment.NewLine}State:", currentState);
                break;
            case LogLevel.Error:
            case LogLevel.Critical:
                console!.Error(message, $"{Environment.NewLine}Category:", CategoryName, $"{Environment.NewLine}State:", currentState);
                break;
            case LogLevel.None:
                break;
            default:
                console!.Log(message, $"{Environment.NewLine}Category:", CategoryName, $"{Environment.NewLine}State:", currentState);
                break;
        }
    }

    private static bool CheckIfBrowserContextIsReady()
    {
        if (console is null)
            return false;

        if (jsRuntime!.IsInitialized() is false)
        {
            // DevInsightsLogger is designed for use in blazor wasm or hybrid applications.
            // However, we’ve enabled it to work in Blazor Server in the development environment as well.
            // Note that logs will display in the most recently opened browser tab.
            // If you close the browser, logs will stop displaying until a new tab is opened or an existing tab is refreshed.
            if (AppEnvironment.IsDev())
            {
                System.Console.WriteLine("DevInsightsLogger is in detached state.");
            }

            return false;
        }

        return true;
    }

    public void Dispose()
    {
    }

    private static bool IsSupported() => AppPlatform.IsBlazorHybridOrBrowser || AppEnvironment.IsDev();
}
