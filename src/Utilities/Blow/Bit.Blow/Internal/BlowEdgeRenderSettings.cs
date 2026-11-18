using Microsoft.AspNetCore.Components;

namespace Bit.Blow;

/// <summary>
/// Infrastructure: what <see cref="BlowCanvas"/> tells every <see cref="BlowEdgeRenderer"/> alike; see
/// <see cref="BlowNodeRenderSettings"/>. Not meant to be used directly.
/// </summary>
public sealed class BlowEdgeRenderSettings
{
    /// <summary>The canvas's <c>EdgeTemplate</c>, or null for the built-in edge.</summary>
    public RenderFragment<BlowEdgeContext>? Template { get; init; }

    /// <summary>True when the canvas draws labels in its HTML label layer instead.</summary>
    public bool SuppressLabel { get; init; }

    /// <summary>The canvas's graph version; see <see cref="BlowCascadingNames.GraphVersion"/>.</summary>
    public int RenderEpoch { get; init; }

    /// <summary>False takes every edge out of the tab order.</summary>
    public bool Focusable { get; init; } = true;

    /// <summary>The id of the element holding the keyboard instructions for an edge, or null for none.</summary>
    public string? DescribedBy { get; init; }

    /// <summary>The canvas's accessible-name strings.</summary>
    public BlowAriaLabels AriaLabels { get; init; } = BlowAriaLabels.Default;

    /// <summary>The key that picks up an end of a focused edge to move it, or null when that cannot be done from the keyboard.</summary>
    public string? ConnectionKey { get; init; }

    /// <summary>An edge's state - validation issues - as of this graph version.</summary>
    internal Func<BlowEdge, BlowEdgeState>? State { get; init; }

    /// <summary>The name a node is announced by, from its id: its aria label, label or id.</summary>
    internal Func<string, string>? NodeName { get; init; }

    internal bool SameAs(RenderFragment<BlowEdgeContext>? template, bool suppressLabel, int epoch, bool focusable,
        string? describedBy, BlowAriaLabels ariaLabels, string? connectionKey)
        => ReferenceEquals(Template, template) && SuppressLabel == suppressLabel && RenderEpoch == epoch
           && Focusable == focusable && DescribedBy == describedBy && ReferenceEquals(AriaLabels, ariaLabels)
           && ConnectionKey == connectionKey;
}
