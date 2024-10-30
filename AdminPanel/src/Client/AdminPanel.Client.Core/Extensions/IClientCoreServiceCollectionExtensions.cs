using BlazorApplicationInsights;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Components.WebAssembly.Services;
using AdminPanel.Client.Core;
using AdminPanel.Client.Core.Services.HttpMessageHandlers;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class IClientCoreServiceCollectionExtensions
{
    public static IServiceCollection AddClientCoreProjectServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Services being registered here can get injected in client side (Web, Android, iOS, Windows, macOS) and server side (during pre rendering)

        services.AddSharedProjectServices(configuration);

        services.AddOptions<ClientCoreSettings>()
            .Bind(configuration)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton(sp => configuration.Get<ClientCoreSettings>()!);

        services.AddSessioned<PubSubService>();
        services.AddSessioned<HttpClientHandler>();
        services.AddSessioned<ILocalHttpServer, NoopLocalHttpServer>();
        services.AddSessioned<ITelemetryContext, AppTelemetryContext>();

        services.AddTransient<ThemeService>();
        services.AddTransient<CultureService>();
        services.AddTransient<IAuthTokenProvider, ClientSideAuthTokenProvider>();
        services.AddTransient<IPrerenderStateService, NoopPrerenderStateService>();
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


        services.AddBlazorApplicationInsights(x =>
        {
            var connectionString = configuration.Get<ClientCoreSettings>()!.ApplicationInsights?.ConnectionString;
            if (string.IsNullOrEmpty(connectionString) is false)
            {
                x.ConnectionString = connectionString;
            }
        });

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
