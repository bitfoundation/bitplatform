using Microsoft.AspNetCore.Components.RenderTree;

namespace Bit.BevTools;

/// <summary>What a session attached to a running app could and could not hook into. The capability report reads it.</summary>
internal sealed class BevToolsRuntimeAttachment
{
    private WeakReference<Renderer>? _renderer;

    /// <summary>
    /// The app installed Bit.BevTools.Loader's startup hooks, which stand in for most of the reflection an attach needs
    /// otherwise: through <see cref="BevToolsLoader.OpenAsync()"/>, or a <see cref="BitBevTools.Attach"/> in that app.
    /// </summary>
    public bool ThroughLoader { get; init; }

    /// <summary>The loader's pass-through handlers sit in every <c>IHttpClientFactory</c> pipeline.</summary>
    public bool FactoryClientsHooked { get; init; }

    public bool LoggerProviderAdded { get; set; }

    /// <summary>
    /// What became of the log entries written before the attach (<c>BevToolsLoaderOptions.AutoStart</c>): handed over,
    /// or why not. Null when the app did not have Bit.BevTools.Loader capture them.
    /// </summary>
    public string? LogsBeforeAttach { get; set; }

    /// <summary>The session took over the entries the loader kept, which <see cref="LogsBeforeAttach"/> describes.</summary>
    public bool LogsBeforeAttachHandedOver { get; set; }

    /// <summary>
    /// The loader whose log entries a session attached in a process that keeps them per circuit takes over once a panel
    /// attaches it to its renderer: only then are the circuit's next entries the session's, not the loader's.
    /// </summary>
    public BevToolsLoaderContext? PendingLogsBeforeAttach { get; set; }

    public BevToolsHttpClientAttachment HttpClient { get; set; }

    /// <summary>Null until a panel attaches to its renderer; then whether the components created after are seen.</summary>
    public bool? ActivatorAttached { get; set; }

    /// <summary>True when the loader's activator hook carries them, false when the renderer's activator was replaced through reflection.</summary>
    public bool ActivatorHooked { get; set; }

    public bool? ExistingComponentsRead { get; set; }

    public Renderer? Renderer => _renderer is not null && _renderer.TryGetTarget(out var renderer) ? renderer : null;

    public bool TryClaim(Renderer renderer)
    {
        if (ReferenceEquals(Renderer, renderer))
        {
            return false;
        }

        _renderer = new WeakReference<Renderer>(renderer);
        return true;
    }
}
