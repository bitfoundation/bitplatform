namespace Bit.Bmotion;

/// <summary>Cascaded by <see cref="BmotionLayoutGroup"/> to namespace descendant LayoutIds.</summary>
internal sealed class BmotionLayoutGroupContext
{
    /// <summary>Prefix applied to descendant <c>LayoutId</c>s (<c>"{Name}:{LayoutId}"</c>).</summary>
    public string? Name { get; init; }
}
