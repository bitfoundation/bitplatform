namespace Bit.Bink;

/// <summary>
/// Maps serialized brush kinds to factories so documents containing custom brushes can be loaded.
/// The built-in brushes are registered by default. Registration is thread-safe.
/// </summary>
public sealed class BinkBrushRegistry
{
    private readonly Dictionary<string, Func<BinkBrush>> _factories = new(StringComparer.Ordinal);
    // One default instance per kind, for the serializer, which consults it for every stroke it writes. Brushes are
    // immutable, so a cached instance can be shared.
    private readonly Dictionary<string, BinkBrush> _defaults = new(StringComparer.Ordinal);
    private readonly object _gate = new();

    /// <summary>The shared default registry containing the built-in brushes.</summary>
    public static BinkBrushRegistry Default { get; } = CreateDefault();

    /// <summary>Creates an empty registry.</summary>
    public BinkBrushRegistry() { }

    private static BinkBrushRegistry CreateDefault()
    {
        var registry = new BinkBrushRegistry();
        registry.Register(BinkPenBrush.KindName, static () => new BinkPenBrush());
        registry.Register(BinkPencilBrush.KindName, static () => new BinkPencilBrush());
        registry.Register(BinkMarkerBrush.KindName, static () => new BinkMarkerBrush());
        registry.Register(BinkHighlighterBrush.KindName, static () => new BinkHighlighterBrush());
        registry.Register(BinkPixelEraserBrush.KindName, static () => new BinkPixelEraserBrush());
        return registry;
    }

    /// <summary>Registers (or replaces) a factory for a kind. The factory returns a brush with default values;
    /// serialized properties are applied on top of it.</summary>
    public void Register(string kind, Func<BinkBrush> factory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);
        ArgumentNullException.ThrowIfNull(factory);
        lock (_gate)
        {
            _factories[kind] = factory;
            _defaults.Remove(kind);
        }
    }

    /// <summary>Registers a brush type by its parameterless constructor.</summary>
    public void Register<TBrush>() where TBrush : BinkBrush, new()
    {
        var sample = new TBrush();
        Register(sample.Kind, static () => new TBrush());
    }

    /// <summary>Tries to create a default instance of the brush for a kind.</summary>
    public bool TryCreate(string kind, out BinkBrush? brush)
    {
        Func<BinkBrush>? factory;
        lock (_gate) _factories.TryGetValue(kind, out factory);
        brush = factory?.Invoke();
        return brush is not null;
    }

    /// <summary>
    /// The default instance for a kind, created once and cached until the kind is registered again, or null when
    /// the kind is unknown.
    /// </summary>
    internal BinkBrush? GetDefault(string kind)
    {
        Func<BinkBrush>? factory;
        lock (_gate)
        {
            if (_defaults.TryGetValue(kind, out BinkBrush? cached)) return cached;
            if (!_factories.TryGetValue(kind, out factory)) return null;
        }
        // The factory is application code, so it runs outside the lock.
        BinkBrush? created = factory();
        if (created is null) return null;
        lock (_gate)
        {
            // A registration that replaced the factory meanwhile has dropped the entry; do not cache the old one.
            if (_factories.TryGetValue(kind, out Func<BinkBrush>? current) && ReferenceEquals(current, factory)) _defaults[kind] = created;
        }
        return created;
    }

    /// <summary>True when the kind is known.</summary>
    public bool Contains(string kind)
    {
        lock (_gate) return _factories.ContainsKey(kind);
    }

    /// <summary>Registered kinds.</summary>
    public IReadOnlyCollection<string> Kinds
    {
        get { lock (_gate) return _factories.Keys.ToArray(); }
    }
}
