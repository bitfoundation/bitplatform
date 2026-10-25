namespace Bit.BlazorUI;

public enum BitPivotHeaderType
{
    /// <summary>
    /// Renders pivot header items as Tab.
    /// </summary>
    Tab,

    /// <summary>
    /// Renders pivot header items as link.
    /// </summary>
    Link,

    /// <summary>
    /// Renders pivot header items as outlined (enclosed) tabs: the selected one is drawn as a bordered tab that
    /// opens onto the panel, with its outer edge in the pivot's color.
    /// </summary>
    Outline
}
