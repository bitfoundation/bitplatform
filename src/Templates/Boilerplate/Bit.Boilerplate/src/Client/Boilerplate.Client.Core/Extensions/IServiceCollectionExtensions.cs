//+:cnd:noEmit
//#if (offlineDb == true)
using Boilerplate.Client.Core.Data;
using Microsoft.EntityFrameworkCore;
//#endif
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.WebAssembly.Services;
using Boilerplate.Client.Core.Services.HttpMessageHandlers;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class IServiceCollectionExtensions
{
    public static IServiceCollection AddClientCoreProjectServices(this IServiceCollection services)
    {
        // Services being registered here can get injected in client side (Web, Android, iOS, Windows, macOS) and server side (during pre rendering)

        services.TryAddTransient<IPrerenderStateService, PrerenderStateService>();

        services.TryAddSessioned<IPubSubService, PubSubService>();
        services.TryAddTransient<IAuthTokenProvider, ClientSideAuthTokenProvider>();
        services.TryAddTransient<IStorageService, BrowserStorageService>();
        services.TryAddSingleton<ILocalHttpServer, NoopLocalHttpServer>();
        services.TryAddTransient<IExternalNavigationService, DefaultExternalNavigationService>();

        services.TryAddKeyedTransient<DelegatingHandler, RequestHeadersDelegationHandler>("DefaultMessageHandler");
        services.TryAddTransient<AuthDelegatingHandler>();
        services.TryAddTransient<RetryDelegatingHandler>();
        services.TryAddTransient<ExceptionDelegatingHandler>();
        services.TryAddSessioned<HttpClientHandler>();

        services.AddSessioned<AuthenticationStateProvider, AuthenticationManager>(); // Use 'Add' instead of 'TryAdd' to override the aspnetcore's default AuthenticationStateProvider.
        services.TryAddSessioned(sp => (AuthenticationManager)sp.GetRequiredService<AuthenticationStateProvider>());

        services.TryAddSessioned<MessageBoxService>();
        services.TryAddTransient<LazyAssemblyLoader>();

        services.AddBitButilServices();
        services.AddBitBlazorUIServices();

        //#if (offlineDb == true)
        services.AddBesqlDbContextFactory<OfflineDbContext>(options =>
        {
            var isRunningInsideDocker = Directory.Exists("/container_volume"); // Blazor Server - Docker (It's supposed to be a mounted volume named /container_volume)
            var dirPath = isRunningInsideDocker ? "/container_volume"
                                                : AppPlatform.IsBlazorHybridOrBrowser ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AC87AA5B-4B37-4E52-8468-2D5DF24AF256")
                                                : Directory.GetCurrentDirectory(); // Blazor server (Non docker Linux, macOS or Windows)

            dirPath = Path.Combine(dirPath, "App_Data");

            Directory.CreateDirectory(dirPath);

            var dbPath = Path.Combine(dirPath, "Offline.db");

            options
                // .UseModel(OfflineDbContextModel.Instance)
                .UseSqlite($"Data Source={dbPath}");

            options.EnableSensitiveDataLogging(AppEnvironment.IsDev())
                    .EnableDetailedErrors(AppEnvironment.IsDev());
        });
        //#endif

        services.AddTypedHttpClients();

        services.AddSharedProjectServices();
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
        if (AppPlatform.IsBlazorHybridOrBrowser)
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
        if (AppPlatform.IsBlazorHybridOrBrowser)
        {
            services.TryAddSingleton<TService, TImplementation>();
        }
        else
        {
            services.TryAddScoped<TService, TImplementation>();
        }

        return services;
    }

    /// <summary>
    /// <inheritdoc cref="AddSessioned{TService, TImplementation}(IServiceCollection)"/>
    /// </summary>
    public static IServiceCollection TryAddSessioned<TService>(this IServiceCollection services, Func<IServiceProvider, TService> implementationFactory)
        where TService : class
    {
        if (AppPlatform.IsBlazorHybridOrBrowser)
        {
            services.TryAdd(ServiceDescriptor.Singleton(implementationFactory));
        }
        else
        {
            services.TryAdd(ServiceDescriptor.Scoped(implementationFactory));
        }

        return services;
    }

    /// <summary>
    /// <inheritdoc cref="AddSessioned{TService, TImplementation}(IServiceCollection)"/>
    /// </summary>
    public static void TryAddSessioned<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TService>(this IServiceCollection services)
        where TService : class
    {
        if (AppPlatform.IsBlazorHybridOrBrowser)
        {
            services.TryAddSingleton<TService, TService>();
        }
        else
        {
            services.TryAddScoped<TService, TService>();
        }
    }
}
