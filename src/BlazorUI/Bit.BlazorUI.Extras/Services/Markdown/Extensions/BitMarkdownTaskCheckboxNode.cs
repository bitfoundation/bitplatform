using Microsoft.AspNetCore.Components;

namespace Bit.BlazorUI;

/// <summary>A GitHub task-list checkbox at the start of a list item.</summary>
public sealed class BitMarkdownTaskCheckboxNode : BitMarkdownNode
{
    /// <summary>Whether the box was written as ticked (<c>[x]</c>).</summary>
    public bool Checked { get; set; }

    /// <summary>
    /// The checkbox's position in the document, counted from 0 in reading order. It is what names
    /// the box to a host storing what a reader ticked, since two items may read identically.
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// The document line the <c>[ ]</c> marker was written on, counted from 0, or <c>-1</c> when
    /// the box cannot be traced back to the source. This is the line
    /// <see cref="BitMarkdownTaskList.Toggle(string, BitMarkdownTaskCheckboxNode, bool)"/> rewrites,
    /// so the box a reader ticked and the marker that is edited are the same one by construction.
    /// </summary>
    public int SourceLine { get; set; } = -1;

    /// <summary>
    /// Set by the viewer when the host is listening for task changes. A checkbox with a handler is
    /// rendered enabled and reports its new state here; one without stays the read-only box GitHub
    /// renders, so a document nobody is editing cannot be half-edited.
    /// </summary>
    public EventCallback<ChangeEventArgs>? OnChange { get; set; }
}
