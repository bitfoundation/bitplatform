using Bit.Blex;
using Bit.Blex.Blazor;
using Bit.Blex.Samples.Core.Services;
using Bit.Blex.Samples.Core.Stores;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    /// <summary>
    /// Everything the sample pages need except persistence, the one piece that differs per host: the
    /// browser hosts persist to localStorage (<see cref="AddWebServices"/>), the MAUI app to OS-native
    /// Preferences. Call it in EVERY DI container that renders the pages - a Blazor Web App with an
    /// interactive WebAssembly client has two of them.
    /// </summary>
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register Blex with a console-logging middleware, then add the stores.
        services.AddBitBlexServices(options =>
        {
            options.DevToolsName = "Blex Sample";
            options.UseMiddleware(ctx =>
                Console.WriteLine($"[blex] {ctx.QualifiedName} #{ctx.Sequence}"));
            // Demonstrate the DevTools state sanitizer (display-only redaction).
            options.RedactDevToolsKeys("secret");
            // Route isolated pipeline errors (persistence writes, throwing subscribers, ...) somewhere visible.
            options.OnError = error =>
                Console.Error.WriteLine($"[blex:{error.Source}] {error.Detail}: {error.Exception.Message}");
        });
        services.AddBitBlexStore<CounterStore>();
        services.AddBitBlexStore<TodoStore>();
        services.AddBitBlexStore<WeatherStore>();

        // In-app undo/redo.
        services.AddBitBlexHistory();

        services.AddSingleton<WeatherService>();

        return services;
    }

    /// <summary>
    /// The browser hosts (Server, WebAssembly, Auto and standalone WebAssembly): the core services, with
    /// [BlexStoreAttribute(Persist = true)] stores persisted to localStorage - debounced, so bursts of
    /// clicks coalesce into one write.
    /// </summary>
    public static IServiceCollection AddWebServices(this IServiceCollection services)
    {
        services.AddCoreServices();

        services.AddBitBlexLocalStoragePersistence(options =>
            options.DebounceInterval = TimeSpan.FromMilliseconds(300));

        return services;
    }
}
