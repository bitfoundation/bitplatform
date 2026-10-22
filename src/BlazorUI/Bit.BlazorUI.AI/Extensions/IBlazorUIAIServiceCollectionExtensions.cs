using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bit.BlazorUI;

[BitAssetTrimmingRegistrar]
public static class IBlazorUIAIServiceCollectionExtensions
{
    /// <summary>
    /// Registers required services of the AI package of the BitBlazorUI components (and the core services they build
    /// on), then configures them.
    /// </summary>
    /// <param name="services">The service collection to add the services to.</param>
    /// <param name="configure">
    /// Configures the package through a <see cref="BitAiBuilder"/>: the transport responses stream from, the tools the
    /// model may call, the models to choose between, the session and presentation defaults, custom renderers and the
    /// components generative UI may render. Calling this method again with another callback adds to what an earlier
    /// call set up, since nothing it registers is registered twice.
    /// </param>
    /// <param name="trySingleton">
    /// Tries to register the eligible services as singleton instead of scoped.
    /// Only enable this for single-user hosting models (Blazor WebAssembly and Hybrid/MAUI).
    /// Do NOT enable it on Blazor Server: the default tool approval policy (<see cref="BitAiDefaultToolApprovalPolicy"/>)
    /// remembers the tools a user approved with "always", and a singleton would be shared across circuits, carrying one
    /// user's approvals over to every other user. The flag is handed on to
    /// <see cref="IBitBlazorUIServiceCollectionExtensions.AddBitBlazorUIServices(IServiceCollection, bool)"/> as well.
    /// </param>
    /// <returns>The service collection, for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Services.AddBitBlazorUIAIServices(ai =&gt; ai
    ///     .UseTransport&lt;MyTransport&gt;()
    ///     .AddTool(BitAiTool.Create("get_weather", "Look up the weather.", GetWeatherAsync))
    ///     .AddModels(models, defaultModelId: "my-model")
    ///     .ConfigureSessions(o =&gt; o with { MaxToolRoundtrips = 5 })
    ///     .ConfigureUi(o =&gt; o with { ShowUsage = true }));
    /// </code>
    /// </example>
    public static IServiceCollection AddBitBlazorUIAIServices(this IServiceCollection services, Action<BitAiBuilder>? configure = null, bool trySingleton = false)
    {
        services.AddBitBlazorUIServices(trySingleton);

        // The one eligible service: the default policy holds nothing but the "always" decisions of the user it serves,
        // which in a single-user host is the whole app. Scoped otherwise - on Blazor Server a singleton would carry one
        // user's decision over to every other user. A policy registered through UseApprovalPolicy stays scoped either
        // way, as it may depend on per-user services.
        if (trySingleton)
        {
            services.TryAddSingleton<IBitAiToolApprovalPolicy, BitAiDefaultToolApprovalPolicy>();
        }
        else
        {
            services.TryAddScoped<IBitAiToolApprovalPolicy, BitAiDefaultToolApprovalPolicy>();
        }

        // Singletons in every host: what they hold is the startup configuration (the tools, the defaults, the
        // renderers and the components generative UI may render), the same for every user.
        services.TryAddSingleton<IBitAiToolRegistry>(sp => new BitAiToolRegistry(sp.GetServices<IBitAiTool>()));
        services.TryAddSingleton(new BitAiChatSessionOptions());
        services.TryAddSingleton(BitAiUiOptions.Default);
        services.TryAddSingleton<BitAiRendererRegistry>();
        services.TryAddSingleton<BitAiGenerativeUiRegistry>();
        services.TryAddSingleton<IBitAiGenerativeUiRegistry>(sp => sp.GetRequiredService<BitAiGenerativeUiRegistry>());

        // Never singleton, regardless of trySingleton. The session factory hands its own service provider to the tools
        // it runs and takes the transport and the approval policy, which are scoped: a singleton would capture the root
        // scope's instances and give every tool call root services.
        services.TryAddScoped<BitAiChatSessionFactory>();

        configure?.Invoke(new BitAiBuilder(services));

        return services;
    }
}
