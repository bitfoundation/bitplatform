//+:cnd:noEmit
//#if (offlineDb == true)
using Boilerplate.Client.Core.Data;
using Microsoft.EntityFrameworkCore;
//#endif
//#if (appInsights == true)
using BlazorApplicationInsights;
//#endif
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.WebAssembly.Services;
using Boilerplate.Client.Core;
using Boilerplate.Client.Core.Components;
using Boilerplate.Client.Core.Services.HttpMessageHandlers;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class IServiceCollectionExtensions
{
    public static IServiceCollection AddClientCoreProjectServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Services being registered here can get injected in client side (Web, Android, iOS, Windows, macOS) and server side (during pre rendering)

        services.AddSharedProjectServices(configuration);

        services.AddSessioned<IPubSubService, PubSubService>();
        services.AddSessioned<ILocalHttpServer, NoopLocalHttpServer>();
        services.AddSessioned<HttpClientHandler>();

        services.AddTransient<IPrerenderStateService, PrerenderStateService>();
        services.AddTransient<ICultureService, CultureService>();
        services.AddTransient<IThemeService, ThemeService>();
        services.AddTransient<IStorageService, BrowserStorageService>();
        services.AddTransient<IAuthTokenProvider, ClientSideAuthTokenProvider>();
        services.AddTransient<IExternalNavigationService, DefaultExternalNavigationService>();
        services.AddTransient<RequestHeadersDelegationHandler>();
        services.AddTransient<AuthDelegatingHandler>();
        services.AddTransient<RetryDelegatingHandler>();
        services.AddTransient<ExceptionDelegatingHandler>();

        // This code constructs a chain of HTTP message handlers. By default, it uses `HttpClientHandler` 
        // to send requests to the server. However, you can replace `HttpClientHandler` with other HTTP message 
        // handlers, such as ASP.NET Core's `HttpMessageHandler` from the Test Host, which is useful for integration tests.
        services.AddTransient<Func<HttpMessageHandler, HttpMessageHandler>>(serviceProvider => underlyingHttpMessageHandler =>
        {
            var constructedHttpMessageHandler = ActivatorUtilities.CreateInstance<RequestHeadersDelegationHandler>(serviceProvider,
                        [ActivatorUtilities.CreateInstance<AuthDelegatingHandler>(serviceProvider,
                        [ActivatorUtilities.CreateInstance<RetryDelegatingHandler>(serviceProvider,
                        [ActivatorUtilities.CreateInstance<ExceptionDelegatingHandler>(serviceProvider, [underlyingHttpMessageHandler])])])]);
            return constructedHttpMessageHandler;
        });
        services.AddTransient(serviceProvider =>
        {
            var underlyingHttpMessageHandler = serviceProvider.GetRequiredService<HttpClientHandler>();
            var constructedHttpMessageHandler = serviceProvider.GetRequiredService<Func<HttpMessageHandler, HttpMessageHandler>>().Invoke(underlyingHttpMessageHandler);
            return constructedHttpMessageHandler;
        });

        services.AddSessioned<AuthenticationStateProvider, AuthenticationManager>();
        services.AddSessioned(sp => (AuthenticationManager)sp.GetRequiredService<AuthenticationStateProvider>());

        services.AddSessioned<SnackBarService>();
        services.AddSessioned<MessageBoxService>();
        services.AddTransient<LazyAssemblyLoader>();

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

        //#if (appInsights == true)
        services.AddBlazorApplicationInsights(x =>
        {
            var connectionString = configuration.Get<ClientAppSettings>()!.ApplicationInsights?.ConnectionString;
            if (string.IsNullOrEmpty(connectionString) is false)
            {
                x.ConnectionString = connectionString;
            }
        });
        //#endif

        services.AddTypedHttpClients();

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
    public static IServiceCollection AddSessioned<TService>(this IServiceCollection services, Func<IServiceProvider, TService> implementationFactory)
        where TService : class
    {
        if (AppPlatform.IsBlazorHybridOrBrowser)
        {
            services.Add(ServiceDescriptor.Singleton(implementationFactory));
        }
        else
        {
            services.Add(ServiceDescriptor.Scoped(implementationFactory));
        }

        return services;
    }

    /// <summary>
    /// <inheritdoc cref="AddSessioned{TService, TImplementation}(IServiceCollection)"/>
    /// </summary>
    public static void AddSessioned<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TService>(this IServiceCollection services)
        where TService : class
    {
        if (AppPlatform.IsBlazorHybridOrBrowser)
        {
            services.AddSingleton<TService, TService>();
        }
        else
        {
            services.AddScoped<TService, TService>();
        }
    }
}
