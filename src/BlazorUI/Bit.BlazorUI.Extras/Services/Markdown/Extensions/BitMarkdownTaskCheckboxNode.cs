using Microsoft.AspNetCore.Components;

namespace Bit.BlazorUI;

/// <summary>A GitHub task-list checkbox at the start of a list item.</summary>
public sealed class BitMarkdownTaskCheckboxNode : BitMarkdownNode
{
    /// <summary>Whether the box was written as ticked (<c>[x]</c>).</summary>
    public bool Checked { get; set; }

    /// <summary>
    /// The checkbox's position in the document, counted from 0 in reading order. It is what
    /// identifies the marker to toggle in the source, since two items may read identically.
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// Set by the viewer when the host is listening for task changes. A checkbox with a handler is
    /// rendered enabled and reports its new state here; one without stays the read-only box GitHub
    /// renders, so a document nobody is editing cannot be half-edited.
    /// </summary>
    public EventCallback<ChangeEventArgs>? OnChange { get; set; }
}
