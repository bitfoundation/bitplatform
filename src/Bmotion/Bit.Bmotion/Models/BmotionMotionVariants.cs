namespace Bit.Bmotion;
/// <summary>
/// A named set of animation states (variants) that can be referenced by name on
/// any Bmotion component. Children automatically inherit the active variant name
/// unless they define their own.
/// </summary>
public class BmotionMotionVariants
{
    private readonly Dictionary<string, BmotionAnimationProps> _variants = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Adds a variant by name. If a variant with the same name already exists
    /// (case-insensitive) it is <b>replaced</b>.
    /// </summary>
    public BmotionMotionVariants Add(string name, BmotionAnimationProps props)
    {
        ArgumentNullException.ThrowIfNull(props);
        _variants[name] = props;
        return this;
    }

    public BmotionAnimationProps? Get(string name)
        => _variants.TryGetValue(name, out var v) ? v : null;

    public bool Contains(string name) => _variants.ContainsKey(name);

    public BmotionAnimationProps? this[string name] => Get(name);

    // ── Builder shorthand ─────────────────────────────────────────────────────
    public static BmotionMotionVariants Create(params (string name, BmotionAnimationProps props)[] entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var mv = new BmotionMotionVariants();
        foreach (var (name, props) in entries)
            mv.Add(name, props);
        return mv;
    }
}
