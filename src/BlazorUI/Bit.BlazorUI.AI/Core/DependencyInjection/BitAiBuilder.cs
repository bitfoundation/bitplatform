using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bit.BlazorUI;

/// <summary>
/// Configures the AI package during startup.
/// </summary>
/// <remarks>
/// <para>
/// Handed to the configure callback of
/// <see cref="IBlazorUIAIServiceCollectionExtensions.AddBitBlazorUIAIServices(IServiceCollection, Action{BitAiBuilder}?, bool)"/>,
/// so registration reads as one fluent statement instead of a dozen unrelated service registrations.
/// </para>
/// <para>
/// That callback is the only way to get one. To configure more after an earlier call (from a module of the app, or a
/// test), call <c>AddBitBlazorUIAIServices</c> again with a callback: it registers nothing twice, and the builder it
/// hands over adds to what is already there.
/// </para>
/// </remarks>
public sealed class BitAiBuilder
{
    internal BitAiBuilder(IServiceCollection services) => Services = services;

    /// <summary>The service collection being configured.</summary>
    public IServiceCollection Services { get; }

    /// <summary>
    /// Registers the transport that produces responses.
    /// </summary>
    /// <typeparam name="TTransport">The transport implementation.</typeparam>
    /// <returns>The builder, for chaining.</returns>
    public BitAiBuilder UseTransport<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TTransport>() where TTransport : class, IBitAiTransport
    {
        Services.AddScoped<IBitAiTransport, TTransport>();
        return this;
    }

    /// <summary>
    /// Registers a transport instance.
    /// </summary>
    /// <param name="transport">The transport.</param>
    /// <returns>The builder, for chaining.</returns>
    public BitAiBuilder UseTransport(IBitAiTransport transport)
    {
        ArgumentNullException.ThrowIfNull(transport);
        Services.AddSingleton(transport);
        return this;
    }

    /// <summary>
    /// Registers a transport built from services.
    /// </summary>
    /// <param name="factory">Creates the transport.</param>
    /// <returns>The builder, for chaining.</returns>
    public BitAiBuilder UseTransport(Func<IServiceProvider, IBitAiTransport> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        Services.AddScoped(factory);
        return this;
    }

    /// <summary>
    /// Adds a tool the model may call.
    /// </summary>
    /// <param name="tool">The tool.</param>
    /// <returns>The builder, for chaining.</returns>
    public BitAiBuilder AddTool(IBitAiTool tool)
    {
        ArgumentNullException.ThrowIfNull(tool);
        Services.AddSingleton(tool);
        return this;
    }

    /// <summary>
    /// Adds a tool resolved from services, for tools that need dependencies.
    /// </summary>
    /// <typeparam name="TTool">The tool implementation.</typeparam>
    /// <returns>The builder, for chaining.</returns>
    public BitAiBuilder AddTool<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TTool>() where TTool : class, IBitAiTool
    {
        Services.AddSingleton<IBitAiTool, TTool>();
        return this;
    }

    /// <summary>
    /// Replaces the policy that decides which tool calls need a human decision.
    /// </summary>
    /// <typeparam name="TPolicy">The policy implementation.</typeparam>
    /// <returns>The builder, for chaining.</returns>
    public BitAiBuilder UseApprovalPolicy<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TPolicy>() where TPolicy : class, IBitAiToolApprovalPolicy
    {
        // Scoped so a policy can depend on per-user services such as the current user.
        Services.RemoveAll<IBitAiToolApprovalPolicy>();
        Services.AddScoped<IBitAiToolApprovalPolicy, TPolicy>();
        return this;
    }

    /// <summary>
    /// Registers the models the user can choose between.
    /// </summary>
    /// <param name="models">The models.</param>
    /// <param name="providers">The providers serving them.</param>
    /// <param name="defaultModelId">The model selected by default.</param>
    /// <returns>The builder, for chaining.</returns>
    public BitAiBuilder AddModels(
        IEnumerable<BitAiModel> models,
        IEnumerable<BitAiProvider>? providers = null,
        string? defaultModelId = null)
    {
        ArgumentNullException.ThrowIfNull(models);
        Services.RemoveAll<IBitAiModelCatalog>();
        Services.AddSingleton<IBitAiModelCatalog>(new BitAiModelCatalog(models, providers, defaultModelId));
        return this;
    }

    /// <summary>
    /// Registers persistence for conversations.
    /// </summary>
    /// <typeparam name="TStore">The store implementation.</typeparam>
    /// <returns>The builder, for chaining.</returns>
    public BitAiBuilder UseConversationStore<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TStore>() where TStore : class, IBitAiConversationStore
    {
        Services.RemoveAll<IBitAiConversationStore>();
        Services.AddScoped<IBitAiConversationStore, TStore>();
        return this;
    }

    /// <summary>
    /// Sets the default options every session starts with.
    /// </summary>
    /// <param name="configure">
    /// Receives the current defaults and returns the ones to use. A transform rather than a
    /// mutation, because the options are an immutable record, which is what makes one instance safe
    /// to share across every session.
    /// </param>
    /// <returns>The builder, for chaining.</returns>
    /// <example>
    /// <code>
    /// ai.ConfigureSessions(options =&gt; options with { MaxToolRoundtrips = 5 });
    /// </code>
    /// </example>
    public BitAiBuilder ConfigureSessions(Func<BitAiChatSessionOptions, BitAiChatSessionOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var current = (BitAiChatSessionOptions?)Services
            .LastOrDefault(d => d.ServiceType == typeof(BitAiChatSessionOptions))?.ImplementationInstance;

        Services.RemoveAll<BitAiChatSessionOptions>();
        Services.AddSingleton(configure(current ?? new BitAiChatSessionOptions()));
        return this;
    }

    /// <summary>
    /// Sets the presentation defaults of the components (what a message shows, the strings, the render interval, ...),
    /// which a cascaded <see cref="BitAiUiOptions"/> still overrides.
    /// </summary>
    /// <param name="configure">
    /// Receives the current defaults and returns the ones to use. Calls build on each other: a second one receives
    /// what the first returned.
    /// </param>
    /// <returns>The builder, for chaining.</returns>
    /// <example>
    /// <code>
    /// ai.ConfigureUi(options =&gt; options with
    /// {
    ///     ShowUsage = true,
    ///     ShowTimestamps = true,
    /// });
    /// </code>
    /// </example>
    public BitAiBuilder ConfigureUi(Func<BitAiUiOptions, BitAiUiOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var current = Services
            .LastOrDefault(d => d.ServiceType == typeof(BitAiUiOptions))?.ImplementationInstance as BitAiUiOptions;

        Services.RemoveAll<BitAiUiOptions>();
        Services.AddSingleton(configure(current ?? BitAiUiOptions.Default));
        return this;
    }

    /// <summary>
    /// Registers custom renderers for parts, tools, artifacts, structured data, or citations.
    /// </summary>
    /// <param name="configure">Receives the registry. Every call is applied, in order, when the registry is first resolved.</param>
    /// <returns>The builder, for chaining.</returns>
    public BitAiBuilder ConfigureRenderers(Action<BitAiRendererRegistry> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        Services.AddSingleton(new RendererConfiguration(configure));
        Services.Replace(ServiceDescriptor.Singleton(static sp =>
        {
            var registry = new BitAiRendererRegistry();
            foreach (var configuration in sp.GetServices<RendererConfiguration>()) configuration.Apply(registry);
            return registry;
        }));

        return this;
    }

    /// <summary>
    /// Registers components that model output may instantiate.
    /// </summary>
    /// <param name="configure">Receives the registry. Every call is applied, in order, when the registry is first resolved.</param>
    /// <returns>The builder, for chaining.</returns>
    /// <remarks>
    /// Registering a component reads its parameter types by reflection (see <see cref="BitAiGenerativeUiRegistry"/>),
    /// so an app that trims is warned where it calls <c>Register</c> in the callback.
    /// </remarks>
    /// <example>
    /// <code>
    /// ai.ConfigureGenerativeUi(registry =&gt;
    ///     registry.Register&lt;WeatherCard&gt;("weather_card", "Shows a forecast.", "City"));
    /// </code>
    /// </example>
    public BitAiBuilder ConfigureGenerativeUi(Action<BitAiGenerativeUiRegistry> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        Services.AddSingleton(new GenerativeConfiguration(configure));
        Services.Replace(ServiceDescriptor.Singleton(static sp =>
        {
            var registry = new BitAiGenerativeUiRegistry();
            foreach (var configuration in sp.GetServices<GenerativeConfiguration>()) configuration.Apply(registry);
            return registry;
        }));

        return this;
    }

    private sealed class RendererConfiguration(Action<BitAiRendererRegistry> configure)
    {
        public void Apply(BitAiRendererRegistry registry) => configure(registry);
    }

    private sealed class GenerativeConfiguration(Action<BitAiGenerativeUiRegistry> configure)
    {
        public void Apply(BitAiGenerativeUiRegistry registry) => configure(registry);
    }
}
