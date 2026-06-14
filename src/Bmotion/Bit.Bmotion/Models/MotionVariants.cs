namespace Bit.Bmotion.Models;

/// <summary>
/// A named set of animation states (variants) that can be referenced by name on
/// any Motion component. Children automatically inherit the active variant name
/// unless they define their own.
/// </summary>
public class MotionVariants
{
    private readonly Dictionary<string, AnimationProps> _variants = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Adds a variant by name. If a variant with the same name already exists
    /// (case-insensitive) it is <b>replaced</b>.
    /// </summary>
    public MotionVariants Add(string name, AnimationProps props)
    {
        _variants[name] = props;
        return this;
    }

    public AnimationProps? Get(string name)
        => _variants.TryGetValue(name, out var v) ? v : null;

    public bool Contains(string name) => _variants.ContainsKey(name);

    public AnimationProps? this[string name] => Get(name);

    // ── Builder shorthand ─────────────────────────────────────────────────────
    public static MotionVariants Create(params (string name, AnimationProps props)[] entries)
    {
        var mv = new MotionVariants();
        foreach (var (name, props) in entries)
            mv.Add(name, props);
        return mv;
    }
}
