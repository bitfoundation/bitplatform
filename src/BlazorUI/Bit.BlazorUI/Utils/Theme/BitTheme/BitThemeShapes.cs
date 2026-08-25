namespace Bit.BlazorUI;

public class BitThemeShapes
{
    /// <summary>The default corner radius of the library (<c>--bit-shp-brd-radius</c>); every per-family radius in <see cref="Radius"/> falls back to it.</summary>
    public string? BorderRadius { get; set; }

    /// <summary>The hairline border width (<c>--bit-shp-brd-width</c>).</summary>
    public string? BorderWidth { get; set; }

    public string? BorderStyle { get; set; }

    /// <summary>
    /// The heavier stroke of the library (<c>--bit-shp-brd-width-thick</c>): underline focus rules,
    /// selection indicators, the slider thumb ring and every other border that must read as heavier
    /// than <see cref="BorderWidth"/>.
    /// </summary>
    public string? BorderWidthThick { get; set; }

    public string? FocusRingWidth { get; set; }
    public string? FocusRingOffset { get; set; }

    /// <summary>The radius scale and the per-family radii (<c>--bit-shp-radius-*</c>).</summary>
    public BitThemeShapeRadii Radius { get; set; } = new();
}

/// <summary>
/// The radius scale (<c>--bit-shp-radius-{none,xs,sm,md,lg,xl,2xl,full}</c>) and the per-family radii
/// (<c>--bit-shp-radius-{control,surface,popup,dialog,button,chip,selection}</c>) every component
/// takes its corners from.
/// </summary>
/// <remarks>
/// The family radii default to <see cref="BitThemeShapes.BorderRadius"/> in the shipped stylesheets
/// (and <see cref="Button"/>, <see cref="Chip"/> and <see cref="Selection"/> default to
/// <see cref="Control"/>), so a design system that rounds buttons, chips, cards, menus and dialogs
/// differently sets them individually while a single-radius theme keeps setting
/// <see cref="BitThemeShapes.BorderRadius"/>.
/// </remarks>
public class BitThemeShapeRadii
{
    public string? None { get; set; }
    public string? Xs { get; set; }
    public string? Sm { get; set; }
    public string? Md { get; set; }
    public string? Lg { get; set; }
    public string? Xl { get; set; }
    public string? Xxl { get; set; }

    /// <summary>The pill / circle radius (<c>9999px</c> by default).</summary>
    public string? Full { get; set; }

    /// <summary>Inputs, pickers, dropdown triggers, badges, pagination, icon chips - every control that is not a button, a chip or a selection box.</summary>
    public string? Control { get; set; }

    /// <summary>
    /// Buttons - <c>BitButton</c>, <c>BitActionButton</c>, <c>BitMenuButton</c>, <c>BitToggleButton</c>
    /// and the actions of a dialog (Material and Cupertino pill them). Falls back to <see cref="Control"/>,
    /// which <c>BitButtonGroup</c> stays on: it publishes its own <c>Rounded</c> parameter.
    /// </summary>
    public string? Button { get; set; }

    /// <summary>
    /// Chips - <c>BitTag</c> and the chips a <c>BitTagsInput</c> or a multi-select <c>BitDropdown</c>
    /// draws inside its field (Material's 8dp corner, Cupertino's capsule). Falls back to <see cref="Control"/>.
    /// </summary>
    public string? Chip { get; set; }

    /// <summary>
    /// The checkbox box (Material's 2dp corner); the radio ring is a circle under every design
    /// system. Falls back to <see cref="Control"/>.
    /// </summary>
    public string? Selection { get; set; }

    /// <summary>Cards, accordions, messages, images, list rows.</summary>
    public string? Surface { get; set; }

    /// <summary>Callouts, menus, dropdown lists, tooltips, snackbars.</summary>
    public string? Popup { get; set; }

    /// <summary>Dialogs and modals.</summary>
    public string? Dialog { get; set; }
}
