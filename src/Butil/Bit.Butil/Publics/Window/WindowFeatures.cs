using System.Linq;

namespace Bit.Butil;

/// <summary>
/// The window features a <c>window.open</c> call is made with, rendered by
/// <see cref="ToString"/> into the comma-separated string the browser expects. Sizes and positions
/// are requests: engines clamp them to the screen, ignore them for a tab, and ignore them entirely
/// unless the call came from a user gesture.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/open#windowfeatures">Window.open() windowFeatures</see>
/// </summary>
public class WindowFeatures
{
    /// <summary>
    /// Ask for a popup window rather than a tab. The size and position members below only mean
    /// anything when this is set, since a tab has neither.
    /// </summary>
    public bool Popup { get; set; }

    /// <summary>The content width in CSS pixels. Values under 100 are dropped, as browsers clamp them anyway.</summary>
    public uint Width { get; set; }

    /// <summary>The content height in CSS pixels. Values under 100 are dropped, as browsers clamp them anyway.</summary>
    public uint Height { get; set; }

    /// <summary>The distance from the left edge of the screen. -1, the default, means "don't ask".</summary>
    public int Left { get; set; } = -1;

    /// <summary>The distance from the top edge of the screen. -1, the default, means "don't ask".</summary>
    public int Top { get; set; } = -1;

    /// <summary>
    /// Open without a usable <c>window.opener</c>, so the new document cannot reach back into this
    /// one. Worth setting for anything untrusted.
    /// </summary>
    public bool NoOpener { get; set; }

    /// <summary>Open without sending a <c>Referer</c> header. Implies <see cref="NoOpener"/> in every engine.</summary>
    public bool NoReferrer { get; set; }

    /// <summary>Renders the features set here into the comma-separated string <c>window.open</c> parses.</summary>
    public override string ToString()
    {
        var list = new[] {
            Popup ? "popup=true" : null,
            Width >= 100 ? $"width={Width}" : null,
            Height >= 100 ? $"height={Height}" : null,
            Left > -1 ? $"left={Left}" : null,
            Top > -1 ? $"top={Top}" : null,
            NoOpener ? "noopener=true" : null,
            NoReferrer ? "noreferrer=true" : null,
        };
        return string.Join(',', list.Where(i => i is not null));
    }
}
