using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Components.WebAssembly.Services;
using Bit.BlazorUI.Demo.Client.Core.Components;
using Bit.BlazorUI.Demo.Client.Core.Components.AI;
using Bit.BlazorUI.Demo.Client.Core.Services.AI;
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

        // Scoped, so that "a page has already been painted" is asked of this reader rather than of
        // the whole server - see the type for why that distinction matters.
        services.TryAddScoped<DemoContentDeferral>();

        services.TryAddTransient<RequestHeadersDelegationHandler>();
        services.TryAddTransient<RetryDelegatingHandler>();
        services.TryAddTransient<ExceptionDelegatingHandler>();
        services.TryAddTransient<HttpClientHandler>();

        services.TryAddTransient<LazyAssemblyLoader>();

        services.AddBitBlazorUIServices(trySingleton: AppRenderMode.IsBlazorHybrid);
        // The app-wide accent configuration, stated once here - this method runs in the server and
        // in every client flavor, so the BitAccentColorHead in the host page (App.razor in the
        // Server project) and the AccentColorSwitcher chrome instances all resolve the same values.
        // StoredCss + LocalStorage explicitly: the BitAccentColorConfig defaults persist nothing and
        // skip first paint. No cookie: the host page is cached by the CDN and served to everyone, so
        // the server must never paint one visitor's accent (see App.razor).
        services.AddBitBlazorUIExtrasServices(trySingleton: AppRenderMode.IsBlazorHybrid, accentColor: options =>
        {
            options.FirstPaintStrategy = BitAccentColorFirstPaintStrategy.StoredCss;
            options.Persistence = BitAccentColorPersistence.LocalStorage;
        });
        // The AI package, set up the way the AI scenarios (/ai/scenarios/...) and the AI component pages use it. No
        // model behind it: every reply comes from a scripted IBitAiTransport (Services/AI), so the pages need no API
        // key and nothing downstream of the transport can tell the difference. The registered transport is the echo
        // most pages start from; a scenario that needs another script builds its own session with it.
        services.AddBitBlazorUIAIServices(ai =>
        {
            ai.UseTransport(AiDemoTransports.Echo());
            ai.AddModels(AiDemoCatalog.Models, AiDemoCatalog.Providers, AiDemoCatalog.DefaultModelId);
            ai.ConfigureSessions(options => options with { MaxToolRoundtrips = 6 });

            // Scoped, so on the server each visitor gets their own threads rather than everyone's.
            ai.UseConversationStore<BitAiInMemoryConversationStore>();

            foreach (var tool in AiDemoCatalog.CreateTools()) ai.AddTool(tool);

            ai.ConfigureUi(options => options with
            {
                ShowUsage = true,
                ShowModelName = true,
                ShowTimestamps = false,
            });

            // Custom renderers, which replace the default view wherever that tool or schema appears.
            ai.ConfigureRenderers(renderers =>
            {
                renderers.RegisterTool<SearchResultCard>("search_docs");

                // A schema the model fills in becomes a form the user checks, instead of raw JSON.
                renderers.RegisterStructured("booking_form", context => builder =>
                {
                    builder.OpenComponent<BookingForm>(0);
                    builder.AddComponentParameter(1, nameof(BookingForm.Part), context.Part);
                    builder.CloseComponent();
                });
            });

            // The components model output is allowed to instantiate, and the properties it may set.
            ai.ConfigureGenerativeUi(RegisterGenerativeComponents);
        }, trySingleton: AppRenderMode.IsBlazorHybrid);
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
    /// The components the AI generative UI scenario lets model output render.
    /// </summary>
    /// <remarks>
    /// Registering a component reads its parameters by reflection, which the registry marks as unsafe to trim. It is safe
    /// here: <c>BitAiGenerativeUiRegistry.Register&lt;TComponent&gt;</c> keeps the component
    /// type whole through its annotation, and the parameters it binds are strings and integers, which have no members of
    /// their own for the trimmer to take away.
    /// </remarks>
    [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode", Justification = "ForecastCard is kept whole by the annotation of Register, and its bindable parameters are strings and integers.")]
    [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode", Justification = "ForecastCard's bindable parameters are strings and integers, which need no code generated at run time.")]
    private static void RegisterGenerativeComponents(BitAiGenerativeUiRegistry registry)
    {
        registry.Register<ForecastCard>("forecast_card", "Shows the current weather for one city.", "City");
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
