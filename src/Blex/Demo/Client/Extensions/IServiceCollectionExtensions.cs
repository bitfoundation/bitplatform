using Bit.Blex;
using Bit.Blex.Blazor;
using Bit.Blex.Demo.Client.Services;
using Bit.Blex.Demo.Client.Stores;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// The services shared by the WebAssembly client and the prerendering host.
/// Both containers have to register the same set: every component is instantiated once on the
/// server for the prerender pass and again in the browser, so anything a page injects must
/// resolve in both places or prerendering fails with a missing-service exception.
/// </summary>
public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddDemoServices(this IServiceCollection services)
    {
        // --- Site services (not part of Blex; they power the live panels) ----------------------
        services.AddScoped<ActionFeed>();
        services.AddScoped<DemoGuard>();
        services.AddScoped<ThemeService>();

        // OnError is captured on the singleton options object, so the sink it writes to must outlive
        // any single scope. In a Blazor Server app you would forward to ILogger instead.
        var errorLog = new ErrorLog();
        services.AddSingleton(errorLog);

        // Same reasoning for the action-failure sink: OnActionError is captured on the singleton options.
        var failureLog = new FailureLog();
        services.AddSingleton(failureLog);

        // --- Blex ------------------------------------------------------------------------------
        services.AddBitBlexServices(options =>
        {
            options.DevToolsName = "Blex Docs";

            // Observing middleware, resolved from DI so it can take scoped dependencies.
            options.UseMiddleware<ActionFeedMiddleware>();

            // Veto middleware: cancels actions while the demo's read-only guard is on.
            options.UseMiddleware<GuardMiddleware>();

            // Display-only redaction: the settings store's Token never reaches the DevTools monitor.
            options.RedactDevToolsKeys("Token");

            // Every non-fatal failure Blex isolates from the pipeline lands here.
            options.OnError = errorLog.Add;

            // ...and every action body that throws lands here. Distinct on purpose: this one is the app's
            // own code failing, and the exception still propagates after the hook has seen it.
            options.OnActionError = failureLog.Add;
        });

        services.AddBitBlexStore<CounterStore>();
        services.AddBitBlexStore<CartStore>();
        services.AddBitBlexStore<EqualityDemoStore>();
        services.AddBitBlexStore<EffectLabStore>();
        services.AddBitBlexStore<SearchStore>();
        services.AddBitBlexStore<ContactsStore>();
        services.AddBitBlexStore<SettingsStore>();
        services.AddBitBlexStore<OrdersStore>();
        services.AddBitBlexStore<NotificationsStore>();

        // Durable persistence for [BlexStoreAttribute(Persist = true)] stores, with debounced writes and a
        // versioned envelope so the shape can evolve without stranding visitors on old payloads.
        // During the prerender pass browser storage is unreachable; <BlexProvider> hydrates as soon as
        // the WebAssembly runtime takes over.
        services.AddBitBlexLocalStoragePersistence(options =>
        {
            options.KeyPrefix = "blex-docs:";
            options.DebounceInterval = TimeSpan.FromMilliseconds(300);
            options.DebounceMaxDelay = TimeSpan.FromSeconds(2);
            options.Version = 1;
            options.Migrate = (storeName, fromVersion, state) =>
            {
                // v0 payloads predate the versioned envelope; take them as-is.
                if (fromVersion == 0)
                    return state;

                // Anything else is from a future build of the site: discard rather than guess.
                return null;
            };

            // Open this site in two tabs and change the theme in one: without this, whichever tab acted
            // last would silently win and the other would only catch up on its next reload.
            options.SyncAcrossTabs = true;
        });

        // In-app undo/redo across every store. <BlexProvider> calls Start() once rehydration finishes.
        // No filter here: the docs demo is more useful when every action is its own step.
        services.AddBitBlexHistory(maxEntries: 50);

        return services;
    }
}
