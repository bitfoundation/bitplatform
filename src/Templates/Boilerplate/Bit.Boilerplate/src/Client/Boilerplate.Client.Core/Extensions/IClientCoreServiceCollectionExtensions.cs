﻿//+:cnd:noEmit
//#if (offlineDb == true)
using Boilerplate.Client.Core.Data;
using Microsoft.EntityFrameworkCore;
//#endif
//#if (appInsights == true)
using BlazorApplicationInsights;
using BlazorApplicationInsights.Interfaces;
//#endif
using Boilerplate.Client.Core;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.WebAssembly.Services;
using Boilerplate.Client.Core.Services.HttpMessageHandlers;
//#if (signalR == true)
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Http.Connections;
//#endif

namespace Microsoft.Extensions.DependencyInjection;

public static partial class IClientCoreServiceCollectionExtensions
{
    public static IServiceCollection AddClientCoreProjectServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Services being registered here can get injected in client side (Web, Android, iOS, Windows, macOS) and server side (during pre rendering)
        services.AddSharedProjectServices(configuration);

        services.AddTransient<IPrerenderStateService, NoopPrerenderStateService>();

        services.AddScoped<ThemeService>();
        services.AddScoped<CultureService>();
        services.AddScoped<HttpClientHandler>();
        services.AddScoped<LazyAssemblyLoader>();
        services.AddScoped<IAuthTokenProvider, ClientSideAuthTokenProvider>();
        services.AddScoped<IExternalNavigationService, DefaultExternalNavigationService>();
        services.AddScoped<AbsoluteServerAddressProvider>(sp => new() { GetAddress = () => sp.GetRequiredService<HttpClient>().BaseAddress! /* Read AbsoluteServerAddressProvider's comments for more info. */ });

        // The following services must be unique to each app session.
        // Defining them as singletons would result in them being shared across all users in Blazor Server and during pre-rendering.
        // To address this, we use the AddSessioned extension method.
        // AddSessioned applies AddSingleton in BlazorHybrid and AddScoped in Blazor WebAssembly and Blazor Server, ensuring correct service lifetimes for each environment.
        services.AddSessioned<PubSubService>();
        services.AddSessioned<SnackBarService>();
        services.AddSessioned<MessageBoxService>();
        services.AddSessioned<ILocalHttpServer, NoopLocalHttpServer>();
        services.AddSessioned<ITelemetryContext, AppTelemetryContext>();
        services.AddSessioned(sp =>
        {
            var authenticationStateProvider = ActivatorUtilities.CreateInstance<AuthenticationManager>(sp);
            authenticationStateProvider.OnInit();
            return authenticationStateProvider;
        });
        services.AddSessioned(sp => (AuthenticationManager)sp.GetRequiredService<AuthenticationStateProvider>());

        services.AddSingleton(sp =>
        {
            ClientCoreSettings settings = new();
            configuration.Bind(settings);
            return settings;
        });

        services.AddOptions<ClientCoreSettings>()
            .Bind(configuration)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddBitButilServices();
        services.AddBitBlazorUIServices();

        // This code constructs a chain of HTTP message handlers. By default, it uses `HttpClientHandler` 
        // to send requests to the server. However, you can replace `HttpClientHandler` with other HTTP message 
        // handlers, such as ASP.NET Core's `HttpMessageHandler` from the Test Host, which is useful for integration tests.
        services.AddScoped<HttpMessageHandlersChainFactory>(serviceProvider => transportHandler =>
        {
            var constructedHttpMessageHandler = ActivatorUtilities.CreateInstance<LoggingDelegatingHandler>(serviceProvider,
                        [ActivatorUtilities.CreateInstance<RequestHeadersDelegatingHandler>(serviceProvider,
                        [ActivatorUtilities.CreateInstance<AuthDelegatingHandler>(serviceProvider,
                        [ActivatorUtilities.CreateInstance<RetryDelegatingHandler>(serviceProvider,
                        [ActivatorUtilities.CreateInstance<ExceptionDelegatingHandler>(serviceProvider, [transportHandler])])])])]);
            return constructedHttpMessageHandler;
        });
        services.AddScoped<AuthDelegatingHandler>();
        services.AddScoped<RetryDelegatingHandler>();
        services.AddScoped<ExceptionDelegatingHandler>();
        services.AddScoped<RequestHeadersDelegatingHandler>();
        services.AddScoped(serviceProvider =>
        {
            var transportHandler = serviceProvider.GetRequiredService<HttpClientHandler>();
            var constructedHttpMessageHandler = serviceProvider.GetRequiredService<HttpMessageHandlersChainFactory>().Invoke(transportHandler);
            return constructedHttpMessageHandler;
        });

        //#if (offlineDb == true)
        if (AppPlatform.IsBrowser)
        {
            AppContext.SetSwitch("Microsoft.EntityFrameworkCore.Issue31751", true);
        }
        services.AddBesqlDbContextFactory<OfflineDbContext>((optionsBuilder) =>
        {
            var isRunningInsideDocker = Directory.Exists("/container_volume"); // Blazor Server - Docker (It's supposed to be a mounted volume named /container_volume)
            var dirPath = isRunningInsideDocker ? "/container_volume"
                                                : AppPlatform.IsBlazorHybridOrBrowser ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AC87AA5B-4B37-4E52-8468-2D5DF24AF256")
                                                : Directory.GetCurrentDirectory(); // Blazor server (Non docker Linux, macOS or Windows)

            dirPath = Path.Combine(dirPath, "App_Data");

            Directory.CreateDirectory(dirPath);

            var dbPath = Path.Combine(dirPath, "Offline.db");

            optionsBuilder
                .UseSqlite($"Data Source={dbPath}");

            //#if (framework == 'net9.0')
            if (AppEnvironment.IsProd())
            {
                optionsBuilder.UseModel(OfflineDbContextModel.Instance);
            }
            //#endif

            optionsBuilder.EnableSensitiveDataLogging(AppEnvironment.IsDev())
                    .EnableDetailedErrors(AppEnvironment.IsDev());
        });
        //#endif

        //#if (appInsights == true)
        services.Add(ServiceDescriptor.Describe(typeof(IApplicationInsights), typeof(AppInsightsJsSdkService), AppPlatform.IsBrowser ? ServiceLifetime.Singleton : ServiceLifetime.Scoped));
        services.AddBlazorApplicationInsights(x =>
        {
            ClientCoreSettings settings = new();
            configuration.Bind(settings);
            x.ConnectionString = settings.ApplicationInsights?.ConnectionString;
        });
        //#endif

        services.AddTypedHttpClients();

        //#if (signalR == true)
        services.AddSingleton<SignalRInfinitiesRetryPolicy>();
        services.AddSessioned(sp =>
        {
            var absoluteServerAddressProvider = sp.GetRequiredService<AbsoluteServerAddressProvider>();
            var authTokenProvider = sp.GetRequiredService<IAuthTokenProvider>();
            var authenticationManager = sp.GetRequiredService<AuthenticationManager>();
            var hubConnection = new HubConnectionBuilder()
                .WithAutomaticReconnect(sp.GetRequiredService<SignalRInfinitiesRetryPolicy>())
                .WithUrl(new Uri(absoluteServerAddressProvider.GetAddress(), "app-hub"), options =>
                {
                    options.SkipNegotiation = true;
                    options.Transports = HttpTransportType.WebSockets;
                    // Avoid enabling long polling or Server-Sent Events. Focus on resolving the issue with WebSockets instead.
                    // WebSockets should be enabled on services like IIS or Cloudflare CDN, offering significantly better performance.
                    options.AccessTokenProvider = async () =>
                    {
                        var accessToken = await authTokenProvider.GetAccessToken();

                        if (string.IsNullOrEmpty(accessToken) is false &&
                            authTokenProvider.ParseAccessToken(accessToken, validateExpiry: true).IsAuthenticated() is false)
                        {
                            return await authenticationManager.RefreshToken(requestedBy: nameof(HubConnectionBuilder));
                        }

                        return accessToken;
                    };
                })
                .Build();
            return hubConnection;
        });
        //#endif

        return services;
    }

    internal static IServiceCollection AddSessioned<TService, TImplementation>(this IServiceCollection services)
        where TImplementation : class, TService
        where TService : class
    {
        if (AppPlatform.IsBlazorHybrid)
        {
            return services.AddSingleton<TService, TImplementation>();
        }
        else
        {
            return services.AddScoped<TService, TImplementation>();
        }
    }

    internal static IServiceCollection AddSessioned<TService>(this IServiceCollection services, Func<IServiceProvider, TService> implementationFactory)
        where TService : class
    {
        if (AppPlatform.IsBlazorHybrid)
        {
            services.Add(ServiceDescriptor.Singleton(implementationFactory));
        }
        else
        {
            services.Add(ServiceDescriptor.Scoped(implementationFactory));
        }

        return services;
    }

    internal static void AddSessioned<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TService>(this IServiceCollection services)
        where TService : class
    {
        if (AppPlatform.IsBlazorHybrid)
        {
            services.AddSingleton<TService, TService>();
        }
        else
        {
            services.AddScoped<TService, TService>();
        }
    }
}
