//+:cnd:noEmit
//#if (appInsights == true)
using BlazorApplicationInsights;
using BlazorApplicationInsights.Interfaces;
//#endif
using Bit.BevTools;
using Boilerplate.Client.Core;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.WebAssembly.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class IClientCoreServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddClientCoreProjectServices(IConfiguration configuration)
        {
            // Services being registered here can get injected in client side (WebAssembly, Android, iOS, Windows and macOS) + server side (during pre-rendering and Blazor Server)
            services.AddSharedProjectServices(configuration);

            services.AddTransient<IPrerenderStateService, NoOpPrerenderStateService>();

            services.AddScoped<ThemeService>();
            services.AddScoped<CultureService>();
            services.AddScoped<TimeZoneService>();
            services.AddScoped<LazyAssemblyLoader>();
            services.AddBitBevToolsLoaderServices(options =>
            {
                options.RegisterAtStartup = AppEnvironment.IsDevelopment() || AppPlatform.IsBlazorHybrid;
                options.ShowBadge = false;
            });
            services.AddScoped<SignInModalService>();
            services.AddScoped<ImageEditorService>();
            services.AddScoped<IAuthTokenProvider, ClientSideAuthTokenProvider>();
            services.AddScoped<IExternalNavigationService, DefaultExternalNavigationService>();
            services.AddScoped<FileSaveService>();
            services.AddScoped<ConsentService>();
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
            services.AddSessioned<SnackBarService>();
            services.AddSessioned<ElevatedAccessService>();
            //#if (signalR == true || notification == true)
            services.AddSessioned<NotificationPreferenceService>();
            //#endif
            services.AddSessioned<ILocalHttpServer, NoOpLocalHttpServer>();
            services.AddSessioned<ITelemetryContext, AppTelemetryContext>();
            services.AddSessioned<AuthenticationStateProvider>(sp =>
            {
                var authenticationStateProvider = ActivatorUtilities.CreateInstance<AuthManager>(sp);
                authenticationStateProvider.OnInit();
                return authenticationStateProvider;
            });
            services.AddSessioned(sp => (AuthManager)sp.GetRequiredService<AuthenticationStateProvider>());

            services.AddSingleton(sp => sp.GetRequiredService<IOptions<ClientCoreSettings>>().Value);

            services.AddSingleton<IValidateOptions<ClientCoreSettings>, ClientCoreSettingsValidator>();
            services.AddOptions<ClientCoreSettings>()
                .Bind(configuration)
                .ValidateOnStart();

            services.AddBitButilServices();
            //#if (brouter == true)
            services.AddBitBrouterServices();
            //#endif

            services.AddBitBlazorUIExtrasServices(trySingleton: AppPlatform.IsBlazorHybrid, accentColor: options =>
            {
                options.FirstPaintStrategy = BitAccentColorFirstPaintStrategy.StoredCss;
                options.Persistence = BitAccentColorPersistence.All;
            });

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
                    throw new InvalidOperationException("AppOfflineDbContext has not been optimized. Run 'dnx dotnet-ef@10.0.14 -- dbcontext optimize --context AppOfflineDbContext' before publishing, and re-run it after every model or migration change. See Boilerplate.Client.Core/Infrastructure/Data/README.md.");

                await Task.Run(async () => await dbContext.Database.MigrateAsync());
            }
            , lifetime: ServiceLifetime.Scoped);
            services.AddScoped<SyncService>();
            //#endif

            //#if (appInsights == true)
            services.Add(ServiceDescriptor.Describe(typeof(IApplicationInsights), typeof(AppInsightsJsSdkService), AppPlatform.IsBrowser ? ServiceLifetime.Singleton : ServiceLifetime.Scoped));
            services.AddBlazorApplicationInsights(options =>
            {
                configuration.Bind("ApplicationInsights", options);
            }, loggingOptions: options => configuration.Bind("Logging:ApplicationInsightsLoggerProvider", options));
            //#endif

            services.AddTypedHttpClients(); // See Boilerplate.Shared/Controllers/Readme.md

            //#if (signalR == true)
            services.AddScoped<IRetryPolicy, SignalRInfiniteRetryPolicy>();
            services.AddSessioned<AppHubConnection>();
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
        internal IServiceCollection AddSessioned<TService, TImplementation>()
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
        internal IServiceCollection AddSessioned<TService>(Func<IServiceProvider, TService> implementationFactory)
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
        internal void AddSessioned<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TService>()
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
}
