using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Hosting;

namespace Bit.Blex.Maui;

/// <summary>
/// Runs when <c>MauiApp.Build()</c> executes, performing the startup work that
/// <c>&lt;BlexProvider&gt;</c> does in Blazor hosts: register every store with the manager,
/// rehydrate persisted state, and start undo/redo recording.
/// </summary>
internal sealed class BlexMauiInitializer : IMauiInitializeService
{
    public void Initialize(IServiceProvider services)
    {
        // Resolving the stores is what attaches them to the manager (see AddBitBlexStore); do it
        // before hydration so every persistent store is known to the persistor.
        //
        // This runs against the root provider. When Blex was registered with the core (scoped)
        // defaults *and* the container was built with ValidateScopes, that resolution is rejected.
        // Startup must not die over it: report something actionable and let the app run with
        // lazily-registered stores and default state.
        try
        {
            _ = services.GetServices<IBlexStore>().ToList();
        }
        catch (InvalidOperationException ex)
        {
            Report(services, ex, "resolving stores at startup; register Blex for MAUI with builder.UseBitBlex() and builder.AddBitBlexStore<T>() so the services are singletons");
            return;
        }

        BlexStatePersistor? persistor;
        try
        {
            persistor = services.GetService<BlexStatePersistor>();
        }
        catch (InvalidOperationException ex)
        {
            Report(services, ex, "resolving persistence at startup; use builder.AddBitBlexPreferencesPersistence() so the persistor is a singleton");
            return;
        }

        if (persistor is not null)
        {
            try
            {
                // BlexPreferencesStorage completes synchronously, so this never actually blocks.
                // A custom asynchronous IBlexStorage should be hydrated from app code instead
                // (await persistor.StartAsync()) rather than rely on this synchronous wait.
                persistor.StartAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                // Unreadable storage must not crash app startup: report through OnError and run
                // with default state, mirroring <BlexProvider>'s containment. This is a
                // persistence failure, not a wiring one, so it keeps the "persistence" source.
                Report(services, ex, "hydrating at startup", source: "persistence");
            }
        }

        // Start recording history only after rehydration so the baseline (the state Undo
        // ultimately returns to) is the hydrated state, not the pre-hydration defaults.
        try
        {
            services.GetService<BlexHistory>()?.Start();
        }
        catch (InvalidOperationException ex)
        {
            Report(services, ex, "starting undo/redo at startup; use builder.AddBitBlexHistory() so the history is a singleton");
        }
    }

    /// <summary>
    /// Routes a startup failure to <see cref="BlexManager.OnError"/>. The manager itself may be
    /// unreachable (it is the very thing whose lifetime went wrong), so this falls back to stderr
    /// rather than throwing over a diagnostic.
    /// </summary>
    private static void Report(IServiceProvider services, Exception exception, string detail, string source = "startup")
    {
        try
        {
            services.GetRequiredService<BlexManager>().ReportError(source, exception, detail);
            return;
        }
        catch (Exception)
        {
            // Fall through to stderr below.
        }

        Console.Error.WriteLine($"[Blex] {source} failed ({detail}): {exception.Message}");
    }
}
