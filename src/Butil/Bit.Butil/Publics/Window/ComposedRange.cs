namespace Bit.Butil;

/// <summary>
/// One range of a selection as it really is, including boundary points inside shadow trees - an
/// entry from <see cref="Window.GetComposedRanges"/>.
/// </summary>
/// <remarks>
/// An ordinary <see cref="WindowSelection"/> can only describe boundaries in the document tree: a
/// selection that starts or ends inside a shadow root is reported against the host element instead,
/// which loses exactly the information a component library needs.
/// <br/>
/// The boundary <i>nodes</i> can't cross interop, so what identifies them is reported instead.
/// </remarks>
public class ComposedRange
{
    /// <summary>Offset of the start boundary within its container.</summary>
    public int StartOffset { get; set; }

    /// <summary>Offset of the end boundary within its container.</summary>
    public int EndOffset { get; set; }

    /// <summary>True when the range is a caret rather than a selection.</summary>
    public bool Collapsed { get; set; }

    /// <summary>The start container's node name, lower-cased - or <c>"#text"</c> for a text node.</summary>
    public string StartContainerName { get; set; } = string.Empty;

    /// <summary>The end container's node name, lower-cased - or <c>"#text"</c> for a text node.</summary>
    public string EndContainerName { get; set; } = string.Empty;

    /// <summary>
    /// True when a boundary lies inside one of the shadow roots that were passed in - i.e. when this
    /// range says something an ordinary selection could not.
    /// </summary>
    public bool CrossesShadowBoundary { get; set; }
}
