namespace Bit.BlazorUI;

public class BitThemeBoxShadows
{
    public string? Callout { get; set; }
    public string? Callout2 { get; set; }
    public string? Sm { get; set; }
    public string? Nm { get; set; }
    public string? Md { get; set; }
    public string? Lg { get; set; }
    public string? Xl { get; set; }
    public string? Xxl { get; set; }
    public string? Inner { get; set; }
    public string? S1 { get; set; }
    public string? S2 { get; set; }
    public string? S3 { get; set; }
    public string? S4 { get; set; }
    public string? S5 { get; set; }
    public string? S6 { get; set; }
    public string? S7 { get; set; }
    public string? S8 { get; set; }
    public string? S9 { get; set; }
    public string? S10 { get; set; }
    public string? S11 { get; set; }
    public string? S12 { get; set; }
    public string? S13 { get; set; }
    public string? S14 { get; set; }
    public string? S15 { get; set; }
    public string? S16 { get; set; }
    public string? S17 { get; set; }
    public string? S18 { get; set; }
    public string? S19 { get; set; }
    public string? S20 { get; set; }
    public string? S21 { get; set; }
    public string? S22 { get; set; }
    public string? S23 { get; set; }
    public string? S24 { get; set; }

    /// <summary>
    /// The focus ring of every control that has not been given a focus color of its own (<c>--bit-shd-focus-ring</c>), so
    /// replacing it re-shapes the focus indicators of the whole library. A control that has been - a <c>Color</c> parameter,
    /// a <c>--bit-&lt;Component&gt;-focus-color</c> - composes the two layers itself in that color, and so do the rings that
    /// mean something (an invalid input's error color, a disabled control's) and the ones drawn flush against a cell; replacing
    /// this token leaves those alone. To restyle every ring, set <see cref="BitThemeShapes.FocusRingWidth"/>,
    /// <see cref="BitThemeShapes.FocusRingOffset"/> and the <c>--bit-clr-*-focus</c> colors, which reach both kinds.
    /// </summary>
    /// <remarks>
    /// This token is a composite of the page background, the primary focus color and the ring's width and offset, and a
    /// composite substitutes its <c>var()</c>s where it is declared: on <c>:root</c>, on a <c>[bit-theme]</c> scope, and on
    /// the element a <see cref="BitThemeProvider"/> or <see cref="BitThemeManager.ApplyBitThemeAsync"/> applies a theme to,
    /// both of which re-declare it whenever the theme re-values one of those inputs. App CSS that sets one of them on any
    /// other element has to re-declare <c>--bit-shd-focus-ring</c> there as well for the default ring to follow.
    /// </remarks>
    public string? FocusRing { get; set; }

    /// <summary>The resting elevation of a card (<c>--bit-shd-card</c>).</summary>
    public string? Card { get; set; }

    /// <summary>The elevation a card that reacts to the pointer lifts to while hovered (<c>--bit-shd-card-hover</c>).</summary>
    public string? CardHover { get; set; }

    /// <summary>The elevation of callouts, menus and dropdown lists (<c>--bit-shd-popup</c>).</summary>
    public string? Popup { get; set; }

    /// <summary>The elevation of dialogs and modals (<c>--bit-shd-dialog</c>).</summary>
    public string? Dialog { get; set; }

    /// <summary>The elevation of panels and edge-anchored sheets (<c>--bit-shd-sheet</c>).</summary>
    public string? Sheet { get; set; }

    /// <summary>The elevation of tooltips (<c>--bit-shd-tooltip</c>).</summary>
    public string? Tooltip { get; set; }

    /// <summary>The elevation of snackbars / toasts (<c>--bit-shd-snackbar</c>).</summary>
    public string? Snackbar { get; set; }

    /// <summary>The shadow an elevated header casts downwards (<c>--bit-shd-appbar-top</c>).</summary>
    public string? AppBarTop { get; set; }

    /// <summary>The shadow an elevated footer casts upwards (<c>--bit-shd-appbar-bottom</c>).</summary>
    public string? AppBarBottom { get; set; }
}
