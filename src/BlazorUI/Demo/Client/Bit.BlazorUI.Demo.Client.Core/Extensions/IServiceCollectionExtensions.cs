using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Components.WebAssembly.Services;
using Bit.BlazorUI.Demo.Client.Core.Services.HttpMessageHandlers;
using Bit.BlazorUI.Legacy;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddClientSharedServices(this IServiceCollection services)
    {
        // Services registered in this class can be injected in client side (Web, Android, iOS, Windows, macOS)

        services.TryAddTransient<IPrerenderStateService, PrerenderStateService>();

        services.TryAddSessioned<IPubSubService, PubSubService>();

        services.TryAddTransient<RequestHeadersDelegationHandler>();
        services.TryAddTransient<RetryDelegatingHandler>();
        services.TryAddTransient<ExceptionDelegatingHandler>();
        services.TryAddTransient<HttpClientHandler>();

        services.TryAddTransient<LazyAssemblyLoader>();

        services.AddBitBlazorUIServices(trySingleton: AppRenderMode.IsBlazorHybrid);
        // The app-wide accent configuration, stated once here - this method runs in the server and
        // in every client flavor, so the BitAccentColorHead in the host page (App.razor in the
        // Server project) and the AccentColorSwitcher chrome instances all resolve the same values.
        // StoredCss + All explicitly: the BitAccentColorConfig defaults persist nothing and skip
        // first paint.
        services.AddBitBlazorUIExtrasServices(trySingleton: AppRenderMode.IsBlazorHybrid, accentColor: options =>
        {
            options.FirstPaintStrategy = BitAccentColorFirstPaintStrategy.StoredCss;
            options.Persistence = BitAccentColorPersistence.All;
        });
        services.AddBitBlazorUILegacyServices(trySingleton: AppRenderMode.IsBlazorHybrid);
        services.AddSharedServices();

        services.AddScoped(sp =>
        {
            var baseAddress = sp.GetRequiredService<HttpClient>().BaseAddress!;

            var hubConnection = new HubConnectionBuilder()
                .WithStatefulReconnect()
                .WithAutomaticReconnect()
                .WithUrl(new Uri(baseAddress, "/app-hub"), options =>
                {
                    options.SkipNegotiation = true;
                    options.Transports = HttpTransportType.WebSockets;
                })
                .Build();
            return hubConnection;
        });

        return services;
    }

    /// <summary>
    /// Utilizing the AddSessioned method seamlessly configures the service to function as a singleton in BlazorHybrid, and BlazorWebAssembly
    /// environments. Simultaneously, it employs per-scope registration for pre-rendering and BlazorServer scenarios
    /// </summary>
    public static IServiceCollection AddSessioned<TService, TImplementation>(this IServiceCollection services)
        where TImplementation : class, TService
        where TService : class
    {
        if (AppRenderMode.IsBlazorHybrid || OperatingSystem.IsBrowser())
        {
            return services.AddSingleton<TService, TImplementation>();
        }
        else
        {
            return services.AddScoped<TService, TImplementation>();
        }
    }

    /// <summary>
    /// <inheritdoc cref="AddSessioned{TService, TImplementation}(IServiceCollection)"/>
    /// </summary>
    public static IServiceCollection TryAddSessioned<TService, TImplementation>(this IServiceCollection services)
        where TImplementation : class, TService
        where TService : class
    {
        if (AppRenderMode.IsBlazorHybrid || OperatingSystem.IsBrowser())
        {
            services.TryAddSingleton<TService, TImplementation>();
        }
        else
        {
            services.TryAddScoped<TService, TImplementation>();
        }

        return services;
    }
}
