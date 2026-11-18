namespace Bit.Bheadless;

/// <summary>A component that owns a menu tree: dropdown menu, context menu, menubar menu.</summary>
internal interface IBitBhMenuRoot
{
    bool IsOpen { get; }
    bool Disabled { get; }
    BitBhDirection Dir { get; }
    string? TriggerId { get; }
    BitBhMenubarContext? Menubar { get; }
    Task CloseAsync();

    /// <summary>
    /// Open because the pointer rested on the trigger, and neither clicked nor focused since (<c>OpenOnHover</c>):
    /// the pointer highlights items without moving the focus, and the menu's surfaces are hover layers.
    /// </summary>
    bool OpenedByHover => false;

    /// <summary>Whether the root follows the pointer over its panels (<c>OpenOnHover</c>).</summary>
    bool TracksHover => false;

    /// <summary>The pointer entered (<paramref name="inside"/>) or left one of the root's panels.</summary>
    void PanelHoverChanged(string contentId, bool inside) { }

    /// <summary>The focus moved into one of the root's panels.</summary>
    void FocusEntered() { }
}
