//+:cnd:noEmit
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
        // Services being registered here can get injected in client side (WebAssembly, Android, iOS, Windows and macOS) + server side (during pre-rendering and Blazor Server)
        services.AddSharedProjectServices(configuration);

        services.AddTransient<IPrerenderStateService, NoOpPrerenderStateService>();

        services.AddScoped<ThemeService>();
        services.AddScoped<CultureService>();
        services.AddScoped<LazyAssemblyLoader>();
        services.AddScoped<SignInModalService>();
        services.AddScoped<IAuthTokenProvider, ClientSideAuthTokenProvider>();
        services.AddScoped<IExternalNavigationService, DefaultExternalNavigationService>();
        //#if (ads == true)
        services.AddScoped<IAdsService, AdsService>();
        //#endif

        if (Uri.TryCreate(configuration.GetServerAddress(), UriKind.Absolute, out var serverAddress))
        {
            services.AddScoped<AbsoluteServerAddressProvider>(sp => new() { GetAddress = () => serverAddress });
        }
        else
        {
            services.AddScoped<AbsoluteServerAddressProvider>(sp => new()
            {
                /* Read AbsoluteServerAddressProvider's comments for more info. */
                GetAddress = () => sp.GetRequiredService<HttpClient>().BaseAddress!
            });
        }

        // The following services must be unique to each app session.
        // Defining them as singletons would result in them being shared across all users in Blazor Server and during pre-rendering.
        // To address this, we use the AddSessioned extension method.
        // AddSessioned applies AddSingleton in BlazorHybrid and AddScoped in Blazor WebAssembly and Blazor Server, ensuring correct service lifetimes for each environment.
        services.AddSessioned<PubSubService>();
        services.AddSessioned<PromptService>();
        services.AddSessioned<SnackBarService>();
        services.AddSessioned<ILocalHttpServer, NoOpLocalHttpServer>();
        services.AddSessioned<ITelemetryContext, AppTelemetryContext>();
        services.AddSessioned<AuthenticationStateProvider>(sp =>
        {
            var authenticationStateProvider = ActivatorUtilities.CreateInstance<AuthManager>(sp);
            authenticationStateProvider.OnInit();
            return authenticationStateProvider;
        });
        services.AddSessioned(sp => (AuthManager)sp.GetRequiredService<AuthenticationStateProvider>());

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
        services.AddBitBlazorUIExtrasServices(trySingleton: AppPlatform.IsBlazorHybrid);

        // Read HttpMessageHandlersChainFactory comments for more info.
        services.AddScoped<HttpMessageHandlersChainFactory>(serviceProvider => transportHandler =>
        {
            transportHandler ??= AppPlatform.IsBrowser ? new HttpClientHandler() : new SocketsHttpHandler() // SocketsHttpHandler doesn't work in BlazorWebAssembly.
            {
                EnableMultipleHttp2Connections = true,
                EnableMultipleHttp3Connections = true,
                PooledConnectionLifetime = TimeSpan.FromMinutes(15),
                AutomaticDecompression = System.Net.DecompressionMethods.All,
                SslOptions = new()
                {
                    EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13
                }
            };

            var constructedHttpMessageHandler = ActivatorUtilities.CreateInstance<LoggingDelegatingHandler>(serviceProvider,
                        [ActivatorUtilities.CreateInstance<CacheDelegatingHandler>(serviceProvider,
                        [ActivatorUtilities.CreateInstance<RequestHeadersDelegatingHandler>(serviceProvider,
                        [ActivatorUtilities.CreateInstance<AuthDelegatingHandler>(serviceProvider,
                        [ActivatorUtilities.CreateInstance<RetryDelegatingHandler>(serviceProvider,
                        [ActivatorUtilities.CreateInstance<ExceptionDelegatingHandler>(serviceProvider, [transportHandler!])])])])])]);
            return constructedHttpMessageHandler;
        });

        //#if (offlineDb == true)
        services.AddBesqlDbContextFactory<AppOfflineDbContext>((sp, optionsBuilder) =>
        {
            var isRunningInsideDocker = Directory.Exists("/container_volume"); // Blazor Server - Docker (It's supposed to be a mounted volume named /container_volume)
            var dirPath = isRunningInsideDocker ? "/container_volume"
                                                : AppPlatform.IsBlazorHybrid ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AC87AA5B-4B37-4E52-8468-2D5DF24AF256")
                                                : Directory.GetCurrentDirectory(); // Blazor server (Non docker Linux, macOS or Windows) OR Blazor WASM

            dirPath = Path.Combine(dirPath, "App_Data");

            Directory.CreateDirectory(dirPath);

            var dbPath = Path.Combine(dirPath, "AppOffline.db");

            optionsBuilder.UseSqlite($"Data Source={dbPath}", dbOptions =>
            {

            });

            optionsBuilder.EnableSensitiveDataLogging(AppEnvironment.IsDevelopment())
                    .EnableDetailedErrors(AppEnvironment.IsDevelopment());

        }
        , dbContextInitializer: async (_, dbContext) =>
        {
            if (AppEnvironment.IsDevelopment() is false && dbContext.Model.GetType() == typeof(EntityFrameworkCore.Metadata.RuntimeModel))
                throw new InvalidOperationException("DbContext has not been optimized"); // Checkout Boilerplate.Client.Core/Data/README.md for more info about Optimize-DbContext command.

            await Task.Run(async () => await dbContext.Database.MigrateAsync());
        }
        , lifetime: ServiceLifetime.Scoped);
        services.AddScoped<SyncService>();
        //#endif

        //#if (appInsights == true)
        services.Add(ServiceDescriptor.Describe(typeof(IApplicationInsights), typeof(AppInsightsJsSdkService), AppPlatform.IsBrowser ? ServiceLifetime.Singleton : ServiceLifetime.Scoped));
        services.AddBlazorApplicationInsights(options =>
        {
            configuration.GetRequiredSection("ApplicationInsights").Bind(options);
        }, loggingOptions: options => configuration.GetRequiredSection("Logging:ApplicationInsightsLoggerProvider").Bind(options));
        //#endif

        services.AddTypedHttpClients(); // See Boilerplate.Shared/Controllers/Readme.md

        //#if (signalR == true)
        services.AddScoped<IRetryPolicy, SignalRInfiniteRetryPolicy>();
        services.AddSessioned(sp =>
        {
            var authManager = sp.GetRequiredService<AuthManager>();
            var authTokenProvider = sp.GetRequiredService<IAuthTokenProvider>();
            var absoluteServerAddressProvider = sp.GetRequiredService<AbsoluteServerAddressProvider>();

            var hubConnection = new HubConnectionBuilder()
                .WithStatefulReconnect()
                .AddJsonProtocol(options =>
                {
                    foreach (var chain in sp.GetRequiredService<JsonSerializerOptions>().TypeInfoResolverChain)
                    {
                        options.PayloadSerializerOptions.TypeInfoResolverChain.Add(chain);
                    }
                })
                .WithAutomaticReconnect(sp.GetRequiredService<IRetryPolicy>())
                .WithUrl(new Uri(absoluteServerAddressProvider.GetAddress(), "app-hub"), options =>
                {
                    options.SkipNegotiation = false; // Required for Azure SignalR.
                    options.Transports = HttpTransportType.WebSockets;
                    // Avoid enabling long polling or Server-Sent Events. Focus on resolving the issue with WebSockets instead.
                    // WebSockets should be enabled on services like IIS or Cloudflare CDN, offering significantly better performance.
                    options.HttpMessageHandlerFactory = httpClientHandler => sp.GetRequiredService<HttpMessageHandlersChainFactory>().Invoke(httpClientHandler);
                    options.AccessTokenProvider = async () =>
                    {
                        return await authManager.GetFreshAccessToken(requestedBy: nameof(HubConnection),
                            ignoreServerConnectionException: true); // ignoreServerConnectionException: If the client is disconnected and the access token is expired, this code will execute repeatedly every few seconds, causing an annoying error message to be displayed to the user.
                    };
                })
                .Build();
            return hubConnection;
        });
        //#endif

        return services;
    }

    /// <summary>
    /// Blazor Server creates one scope for each connected user's client app.
    /// Blazor WebAssembly has only one scope for the client app.
    /// MAUI + Blazor Hybrid would create two scopes for the client app: one for the native part and one for the Blazor's WebView.
    /// So, in Blazor Hybrid, we register sessioned services as singletons to share them between the two scopes.
    /// And in Blazor Server we **MUST** register sessioned services as scoped to avoid sharing them between different users.
    /// And in Blazor WebAssembly it doesn't matter if we register sessioned services as scoped or singleton because there's only one scope per client app.
    /// </summary>
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

    /// <summary>
    /// <inheritdoc cref="AddSessioned{TService, TImplementation}(IServiceCollection)"/>
    /// </summary>
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

    /// <summary>
    /// <inheritdoc cref="AddSessioned{TService, TImplementation}(IServiceCollection)"/>
    /// </summary>
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
