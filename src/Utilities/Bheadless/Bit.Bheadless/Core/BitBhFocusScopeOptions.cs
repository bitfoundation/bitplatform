using Microsoft.AspNetCore.Components;

namespace Bit.Bheadless;

/// <summary>Options for <see cref="BitBhInterop.CreateFocusScopeAsync"/>.</summary>
public sealed class BitBhFocusScopeOptions
{
    /// <summary>Keep focus inside the container (Tab / Shift+Tab wrap, focus is pulled back).</summary>
    public bool Trap { get; set; }

    /// <summary>Wrap Tab / Shift+Tab at the edges without pulling focus back.</summary>
    public bool Loop { get; set; }

    /// <summary><c>"first"</c> (default), <c>"container"</c>, <c>"none"</c>, or the id of an element to focus.</summary>
    public string AutoFocus { get; set; } = "first";

    /// <summary>Restore focus to the previously focused element when the scope is destroyed.</summary>
    public bool RestoreFocus { get; set; } = true;

    /// <summary>Optional id of the element to receive focus when the scope is destroyed.</summary>
    public string? ReturnFocusId { get; set; }

    /// <summary>Optional element to focus first (takes precedence over <see cref="AutoFocus"/>).</summary>
    public ElementReference? AutoFocusElement { get; set; }

    /// <summary>Optional element to receive focus when the scope is destroyed (takes precedence over <see cref="ReturnFocusId"/>).</summary>
    public ElementReference? ReturnFocusElement { get; set; }

    /// <summary>
    /// Restore focus only if it went into the scope (or its layer) while the scope was alive: a surface opened without
    /// taking the focus - a menu the pointer opened - never pulls it to <see cref="ReturnFocusId"/> when it closes.
    /// </summary>
    internal bool RestoreFocusIfEntered { get; set; }

    internal Dictionary<string, object?> ToJs() => new()
    {
        ["trap"] = Trap,
        ["loop"] = Loop,
        ["autoFocus"] = AutoFocusElement is { Id: not null } af ? af : AutoFocus,
        ["restore"] = RestoreFocus,
        ["returnFocusId"] = ReturnFocusElement is { Id: not null } rf ? rf : ReturnFocusId,
        ["restoreIfEntered"] = RestoreFocusIfEntered,
    };
}
