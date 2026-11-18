using AngleSharp.Dom;

namespace Bit.Bheadless;

/// <summary>One stop of the sequential (Tab) focus order.</summary>
/// <param name="Element">The focusable element.</param>
/// <param name="Role">Its role.</param>
/// <param name="Name">Its accessible name.</param>
public sealed record BitBhTabStop(IElement Element, string? Role, string Name)
{
    /// <inheritdoc />
    public override string ToString() => $"{Role ?? Element.LocalName} \"{Name}\"";
}
