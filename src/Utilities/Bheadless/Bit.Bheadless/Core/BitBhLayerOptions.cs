namespace Bit.Bheadless;

/// <summary>Layer (dismissal) behaviour of a floating surface.</summary>
public sealed class BitBhLayerOptions
{
    /// <summary>Escape dismisses the layer when focus is outside every layer. Default: true.</summary>
    public bool Escape { get; set; } = true;

    /// <summary>Pointer-down outside dismisses the layer. Default: true.</summary>
    public bool PointerDownOutside { get; set; } = true;

    /// <summary>Focus moving outside dismisses the layer. Default: false.</summary>
    public bool FocusOutside { get; set; }

    /// <summary>A modal layer shields the layers below it from outside interaction.</summary>
    public bool Modal { get; set; }

    /// <summary>Hover-driven layer (tooltip, hover card): Escape always dismisses it first.</summary>
    public bool Hover { get; set; }

    /// <summary>Handle of the parent surface (nested menus, popovers opened from popovers).</summary>
    public int ParentHandle { get; set; }

    /// <summary>Extra element ids that belong to the layer (trigger, input, …).</summary>
    public List<string?> BranchIds { get; } = new();

    /// <summary>
    /// Element ids where a secondary-button press (the one that opens a context menu) is not "outside": the
    /// context menu it opens again moves the open one instead of closing and reopening it.
    /// </summary>
    public List<string?> SecondaryPressIds { get; } = new();

    internal Dictionary<string, object?> ToJs() => new()
    {
        ["escape"] = Escape,
        ["outside"] = PointerDownOutside,
        ["focusOutside"] = FocusOutside,
        ["modal"] = Modal,
        ["hover"] = Hover,
        ["parentFloating"] = ParentHandle,
        ["ids"] = BranchIds.Where(id => !string.IsNullOrEmpty(id)).ToArray(),
        ["secondaryIds"] = SecondaryPressIds.Where(id => !string.IsNullOrEmpty(id)).ToArray(),
    };
}
