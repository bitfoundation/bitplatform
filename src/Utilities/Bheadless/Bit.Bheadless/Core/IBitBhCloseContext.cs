namespace Bit.Bheadless;

/// <summary>
/// A shared interface implemented by cascading contexts that support a "close"
/// action — dialogs, popovers, disclosures, collapsibles, menus … This enables
/// <see cref="BitBhCloseButton"/> to close the nearest enclosing closeable ancestor
/// without needing to know which specific component it's nested inside.
/// </summary>
public interface IBitBhCloseContext
{
    /// <summary>Closes the enclosing component.</summary>
    Task CloseAsync();
}
