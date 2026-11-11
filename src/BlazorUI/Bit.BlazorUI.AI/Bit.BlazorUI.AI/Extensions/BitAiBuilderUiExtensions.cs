using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bit.BlazorUI;

/// <summary>
/// What <see cref="BitAiBuilder"/> configures of the components: their presentation defaults, custom renderers and the
/// components generative UI may render.
/// </summary>
/// <remarks>
/// Extension methods rather than members of the builder, since the builder belongs to Bit.BlazorUI.AI.Core, which has
/// no UI: a server configures sessions and transports with the same builder and never sees these.
/// </remarks>
[BitAssetTrimmingRegistrar]
public static class BitAiBuilderUiExtensions
{
    /// <summary>
    /// Sets the presentation defaults of the components (what a message shows, the strings, the render interval, ...),
    /// which a cascaded <see cref="BitAiUiOptions"/> still overrides.
    /// </summary>
    /// <param name="builder">The builder.</param>
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
    public static BitAiBuilder ConfigureUi(this BitAiBuilder builder, Func<BitAiUiOptions, BitAiUiOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var current = builder.Services
            .LastOrDefault(d => d.ServiceType == typeof(BitAiUiOptions))?.ImplementationInstance as BitAiUiOptions;

        builder.Services.RemoveAll<BitAiUiOptions>();
        builder.Services.AddSingleton(configure(current ?? BitAiUiOptions.Default));
        return builder;
    }

    /// <summary>
    /// Registers custom renderers for parts, tools, artifacts, structured data, or citations.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="configure">Receives the registry. Every call is applied, in order, when the registry is first resolved.</param>
    /// <returns>The builder, for chaining.</returns>
    public static BitAiBuilder ConfigureRenderers(this BitAiBuilder builder, Action<BitAiRendererRegistry> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        builder.Services.AddSingleton(new RendererConfiguration(configure));
        builder.Services.Replace(ServiceDescriptor.Singleton(static sp =>
        {
            var registry = new BitAiRendererRegistry();
            foreach (var configuration in sp.GetServices<RendererConfiguration>()) configuration.Apply(registry);
            return registry;
        }));

        return builder;
    }

    /// <summary>
    /// Registers components that model output may instantiate.
    /// </summary>
    /// <param name="builder">The builder.</param>
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
    public static BitAiBuilder ConfigureGenerativeUi(this BitAiBuilder builder, Action<BitAiGenerativeUiRegistry> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        builder.Services.AddSingleton(new GenerativeConfiguration(configure));
        builder.Services.Replace(ServiceDescriptor.Singleton(static sp =>
        {
            var registry = new BitAiGenerativeUiRegistry();
            foreach (var configuration in sp.GetServices<GenerativeConfiguration>()) configuration.Apply(registry);
            return registry;
        }));

        return builder;
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
