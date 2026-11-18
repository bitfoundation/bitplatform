namespace Bit.Bmotion;

/// <summary>
/// Named animation states (variants) that can be referenced by name on any Bmotion component
/// via its <c>State</c>/<c>InitialState</c> parameters. Children automatically inherit the
/// active state name unless they set their own.
/// <para>
/// Declare with initializer syntax:
/// <code>
/// var variants = new BmotionVariants
/// {
///     ["hidden"]  = Bm.To(opacity: 0, y: 20),
///     ["visible"] = Bm.To(opacity: 1, y: 0),
/// };
/// </code>
/// A variant can embed its own transition (<c>Bm.To(..., transition: Bm.Spring())</c>), and
/// dynamic variants receive the component's <c>Custom</c> parameter:
/// <code>
/// variants.Add("visible", custom => Bm.To(x: 10 * (int)custom!));
/// </code>
/// </para>
/// </summary>
public class BmotionVariants
{
    // Entries hold either a BmotionProps or a Func<object?, BmotionProps>.
    private readonly Dictionary<string, object> _entries = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a variant's props (dynamic variants resolve with <c>null</c> custom data), or sets
    /// a static variant. Setting replaces an existing entry with the same (case-insensitive) name;
    /// setting <c>null</c> removes it.
    /// </summary>
    public BmotionProps? this[string name]
    {
        get => Get(name);
        set
        {
            ArgumentNullException.ThrowIfNull(name);
            if (value is null) _entries.Remove(name);
            else _entries[name] = value;
        }
    }

    /// <summary>Adds (or replaces) a static variant.</summary>
    public BmotionVariants Add(string name, BmotionProps props)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(props);
        _entries[name] = props;
        return this;
    }

    /// <summary>
    /// Adds (or replaces) a dynamic variant: a function of the component's <c>Custom</c>
    /// parameter, resolved independently per component.
    /// </summary>
    public BmotionVariants Add(string name, Func<object?, BmotionProps> factory)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(factory);
        _entries[name] = factory;
        return this;
    }

    /// <summary>
    /// Resolves a variant by name. Dynamic variants are invoked with <paramref name="custom"/>
    /// (the resolving component's <c>Custom</c> parameter).
    /// </summary>
    public BmotionProps? Get(string name, object? custom = null)
        => _entries.TryGetValue(name, out var entry)
            ? entry switch
            {
                BmotionProps props => props,
                Func<object?, BmotionProps> factory => factory(custom),
                _ => null,
            }
            : null;

    public bool Contains(string name) => _entries.ContainsKey(name);

    /// <summary>Tuple-based builder shorthand.</summary>
    public static BmotionVariants Create(params (string name, BmotionProps props)[] entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var v = new BmotionVariants();
        foreach (var (name, props) in entries)
            v.Add(name, props);
        return v;
    }
}
