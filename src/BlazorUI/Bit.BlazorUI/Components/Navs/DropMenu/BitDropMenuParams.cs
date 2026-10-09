namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitDropMenu"/> component.
/// </summary>
/// <remarks>
/// The open state (<c>IsOpen</c> and <c>DefaultIsOpen</c>), the content, the events and the per-instance ids
/// (<c>ScrollContainerId</c>) are deliberately left out: each of them belongs to one drop menu, and a value
/// cascaded to every drop menu underneath a <see cref="BitParams"/> would open, fill or wire them all at once.
/// </remarks>
public class BitDropMenuParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitDropMenu"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitDropMenu value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitDropMenu)}";



    public string Name => ParamName;



    /// <summary>
    /// How the callout is lined up with the button across the side it opens on.
    /// </summary>
    public BitPlacement? Alignment { get; set; }

    /// <summary>
    /// The description of the drop menu for the benefit of screen readers, read after the name of the button.
    /// </summary>
    public string? AriaDescription { get; set; }

    /// <summary>
    /// If true, adds an aria-hidden attribute instructing screen readers to ignore the button of the drop menu.
    /// </summary>
    public bool? AriaHidden { get; set; }

    /// <summary>
    /// Closes the callout as soon as a click lands anywhere inside it.
    /// </summary>
    public bool? AutoClose { get; set; }

    /// <summary>
    /// Moves the focus into the callout as soon as it opens.
    /// </summary>
    public bool? AutoFocus { get; set; }

    /// <summary>
    /// The color kind of the background of the callout of the drop menu.
    /// </summary>
    public BitColorKind? Background { get; set; }

    /// <summary>
    /// The color kind of the border of the callout of the drop menu.
    /// </summary>
    public BitColorKind? Border { get; set; }

    /// <summary>
    /// The icon for the chevron down part of the drop menu using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="ChevronDownIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? ChevronDownIcon { get; set; }

    /// <summary>
    /// The icon name for the chevron down part of the drop menu from the built-in Fluent UI icons.
    /// </summary>
    public string? ChevronDownIconName { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the drop menu.
    /// </summary>
    public BitDropMenuClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the button of the drop menu.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// Determines the allowed drop directions of the callout of the drop menu.
    /// </summary>
    public BitDropDirection? DropDirection { get; set; }

    /// <summary>
    /// Expands the drop menu width to 100% of the available width.
    /// </summary>
    public bool? FullWidth { get; set; }

    /// <summary>
    /// The distance in pixels between the button and the callout.
    /// </summary>
    public int? Gap { get; set; }

    /// <summary>
    /// The delay in milliseconds before the callout closes once the pointer leaves the drop menu in the OpenOnHover mode.
    /// </summary>
    public int? HoverCloseDelay { get; set; }

    /// <summary>
    /// The delay in milliseconds before the callout opens once the pointer enters the drop menu in the OpenOnHover mode.
    /// </summary>
    public int? HoverOpenDelay { get; set; }

    /// <summary>
    /// The icon to display inside the header of the drop menu using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="IconName"/> when both are set.
    /// </summary>
    public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// The name of the icon to display inside the header of the drop menu from the built-in Fluent UI icons.
    /// </summary>
    public string? IconName { get; set; }

    /// <summary>
    /// Determines whether the drop menu is in the loading state.
    /// </summary>
    public bool? IsLoading { get; set; }

    /// <summary>
    /// Keeps the content of the callout out of the page until the callout is opened for the first time.
    /// </summary>
    public bool? LazyRender { get; set; }

    /// <summary>
    /// Expands the callout of the drop menu to at least the width of the button of the drop menu.
    /// </summary>
    public bool? MatchWidth { get; set; }

    /// <summary>
    /// The maximum height of the callout of the drop menu as a CSS value, beyond which its content scrolls.
    /// </summary>
    public string? MaxHeight { get; set; }

    /// <summary>
    /// The maximum width of the callout of the drop menu as a CSS value, beyond which its content wraps.
    /// </summary>
    public string? MaxWidth { get; set; }

    /// <summary>
    /// The minimum width of the callout of the drop menu as a CSS value.
    /// </summary>
    public string? MinWidth { get; set; }

    /// <summary>
    /// Removes the chevron-down icon from the button of the drop menu.
    /// </summary>
    public bool? NoChevron { get; set; }

    /// <summary>
    /// Removes the box-shadow from the callout of the drop menu.
    /// </summary>
    public bool? NoShadow { get; set; }

    /// <summary>
    /// Opens the callout when the pointer enters the drop menu and closes it when the pointer leaves it.
    /// </summary>
    public bool? OpenOnHover { get; set; }

    /// <summary>
    /// The position of the responsive panel to show on the screen.
    /// </summary>
    public BitPlacement? PanelPlacement { get; set; }

    /// <summary>
    /// Renders the drop menu in responsive mode on small screens.
    /// </summary>
    public bool? Responsive { get; set; }

    /// <summary>
    /// The side of the button the callout opens on when there is room for it there.
    /// </summary>
    public BitPlacement? Placement { get; set; }

    /// <summary>
    /// The size of the button of the drop menu.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the drop menu.
    /// </summary>
    public BitDropMenuClassStyles? Styles { get; set; }

    /// <summary>
    /// The tooltip to show when the mouse is placed on the button of the drop menu.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Makes the background of the header of the drop menu transparent.
    /// </summary>
    public bool? Transparent { get; set; }

    /// <summary>
    /// Keeps the keyboard inside the callout while it is open.
    /// </summary>
    public bool? TrapFocus { get; set; }

    /// <summary>
    /// The visual variant of the button of the drop menu: filled, outlined, or text only.
    /// </summary>
    public BitVariant? Variant { get; set; }

    /// <summary>
    /// The width of the callout of the drop menu as a CSS value.
    /// </summary>
    public string? Width { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitDropMenu"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitDropMenu"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitDropMenu"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitDropMenu"/>.
    /// </remarks>
    /// <param name="bitDropMenu">
    /// The <see cref="BitDropMenu"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitDropMenu bitDropMenu)
    {
        if (bitDropMenu is null) return;

        UpdateBaseParameters(bitDropMenu);

        if (Alignment.HasValue)
        {
            bitDropMenu.TakeFromCascade(nameof(Alignment), Alignment.Value, static d => d.Alignment, static (d, v) => d.Alignment = v);
        }

        if (AriaDescription.HasValue())
        {
            bitDropMenu.TakeFromCascade(nameof(AriaDescription), AriaDescription, static d => d.AriaDescription, static (d, v) => d.AriaDescription = v);
        }

        if (AriaHidden.HasValue)
        {
            bitDropMenu.TakeFromCascade(nameof(AriaHidden), AriaHidden.Value, static d => d.AriaHidden, static (d, v) => d.AriaHidden = v);
        }

        if (AutoClose.HasValue)
        {
            bitDropMenu.TakeFromCascade(nameof(AutoClose), AutoClose.Value, static d => d.AutoClose, static (d, v) => d.AutoClose = v);
        }

        if (AutoFocus.HasValue)
        {
            bitDropMenu.TakeFromCascade(nameof(AutoFocus), AutoFocus.Value, static d => d.AutoFocus, static (d, v) => d.AutoFocus = v);
        }

        if (Background.HasValue)
        {
            bitDropMenu.TakeFromCascade(nameof(Background), Background.Value, static d => d.Background, static (d, v) => d.Background = v);
        }

        if (Border.HasValue)
        {
            bitDropMenu.TakeFromCascade(nameof(Border), Border.Value, static d => d.Border, static (d, v) => d.Border = v);
        }

        if (ChevronDownIcon is not null)
        {
            bitDropMenu.TakeFromCascade(nameof(ChevronDownIcon), ChevronDownIcon, static d => d.ChevronDownIcon, static (d, v) => d.ChevronDownIcon = v);
        }

        if (ChevronDownIconName.HasValue())
        {
            bitDropMenu.TakeFromCascade(nameof(ChevronDownIconName), ChevronDownIconName, static d => d.ChevronDownIconName, static (d, v) => d.ChevronDownIconName = v);
        }

        if (Classes is not null)
        {
            bitDropMenu.TakeFromCascade(nameof(Classes), Classes, static d => d.Classes, static (d, v) => d.Classes = v);
        }

        if (Color.HasValue)
        {
            bitDropMenu.TakeFromCascade(nameof(Color), Color.Value, static d => d.Color, static (d, v) => d.Color = v);
        }

        if (DropDirection.HasValue)
        {
            bitDropMenu.TakeFromCascade(nameof(DropDirection), DropDirection.Value, static d => d.DropDirection, static (d, v) => d.DropDirection = v);
        }

        if (FullWidth.HasValue)
        {
            bitDropMenu.TakeFromCascade(nameof(FullWidth), FullWidth.Value, static d => d.FullWidth, static (d, v) => d.FullWidth = v);
        }

        if (Gap.HasValue)
        {
            bitDropMenu.TakeFromCascade(nameof(Gap), Gap.Value, static d => d.Gap, static (d, v) => d.Gap = v);
        }

        if (HoverCloseDelay.HasValue)
        {
            bitDropMenu.TakeFromCascade(nameof(HoverCloseDelay), HoverCloseDelay.Value, static d => d.HoverCloseDelay, static (d, v) => d.HoverCloseDelay = v);
        }

        if (HoverOpenDelay.HasValue)
        {
            bitDropMenu.TakeFromCascade(nameof(HoverOpenDelay), HoverOpenDelay.Value, static d => d.HoverOpenDelay, static (d, v) => d.HoverOpenDelay = v);
        }

        if (Icon is not null)
        {
            bitDropMenu.TakeFromCascade(nameof(Icon), Icon, static d => d.Icon, static (d, v) => d.Icon = v);
        }

        if (IconName.HasValue())
        {
            bitDropMenu.TakeFromCascade(nameof(IconName), IconName, static d => d.IconName, static (d, v) => d.IconName = v);
        }

        if (IsLoading.HasValue)
        {
            bitDropMenu.TakeFromCascade(nameof(IsLoading), IsLoading.Value, static d => d.IsLoading, static (d, v) => d.IsLoading = v);
        }

        if (LazyRender.HasValue)
        {
            bitDropMenu.TakeFromCascade(nameof(LazyRender), LazyRender.Value, static d => d.LazyRender, static (d, v) => d.LazyRender = v);
        }

        if (MatchWidth.HasValue)
        {
            bitDropMenu.TakeFromCascade(nameof(MatchWidth), MatchWidth.Value, static d => d.MatchWidth, static (d, v) => d.MatchWidth = v);
        }

        if (MaxHeight.HasValue())
        {
            bitDropMenu.TakeFromCascade(nameof(MaxHeight), MaxHeight, static d => d.MaxHeight, static (d, v) => d.MaxHeight = v);
        }

        if (MaxWidth.HasValue())
        {
            bitDropMenu.TakeFromCascade(nameof(MaxWidth), MaxWidth, static d => d.MaxWidth, static (d, v) => d.MaxWidth = v);
        }

        if (MinWidth.HasValue())
        {
            bitDropMenu.TakeFromCascade(nameof(MinWidth), MinWidth, static d => d.MinWidth, static (d, v) => d.MinWidth = v);
        }

        if (NoChevron.HasValue)
        {
            bitDropMenu.TakeFromCascade(nameof(NoChevron), NoChevron.Value, static d => d.NoChevron, static (d, v) => d.NoChevron = v);
        }

        if (NoShadow.HasValue)
        {
            bitDropMenu.TakeFromCascade(nameof(NoShadow), NoShadow.Value, static d => d.NoShadow, static (d, v) => d.NoShadow = v);
        }

        if (OpenOnHover.HasValue)
        {
            bitDropMenu.TakeFromCascade(nameof(OpenOnHover), OpenOnHover.Value, static d => d.OpenOnHover, static (d, v) => d.OpenOnHover = v);
        }

        if (PanelPlacement.HasValue)
        {
            bitDropMenu.TakeFromCascade(nameof(PanelPlacement), PanelPlacement.Value, static d => d.PanelPlacement, static (d, v) => d.PanelPlacement = v);
        }

        if (Responsive.HasValue)
        {
            bitDropMenu.TakeFromCascade(nameof(Responsive), Responsive.Value, static d => d.Responsive, static (d, v) => d.Responsive = v);
        }

        if (Placement.HasValue)
        {
            bitDropMenu.TakeFromCascade(nameof(Placement), Placement.Value, static d => d.Placement, static (d, v) => d.Placement = v);
        }

        if (Size.HasValue)
        {
            bitDropMenu.TakeFromCascade(nameof(Size), Size.Value, static d => d.Size, static (d, v) => d.Size = v);
        }

        if (Styles is not null)
        {
            bitDropMenu.TakeFromCascade(nameof(Styles), Styles, static d => d.Styles, static (d, v) => d.Styles = v);
        }

        if (Title.HasValue())
        {
            bitDropMenu.TakeFromCascade(nameof(Title), Title, static d => d.Title, static (d, v) => d.Title = v);
        }

        if (Transparent.HasValue)
        {
            bitDropMenu.TakeFromCascade(nameof(Transparent), Transparent.Value, static d => d.Transparent, static (d, v) => d.Transparent = v);
        }

        if (TrapFocus.HasValue)
        {
            bitDropMenu.TakeFromCascade(nameof(TrapFocus), TrapFocus.Value, static d => d.TrapFocus, static (d, v) => d.TrapFocus = v);
        }

        if (Variant.HasValue)
        {
            bitDropMenu.TakeFromCascade(nameof(Variant), Variant.Value, static d => d.Variant, static (d, v) => d.Variant = v);
        }

        if (Width.HasValue())
        {
            bitDropMenu.TakeFromCascade(nameof(Width), Width, static d => d.Width, static (d, v) => d.Width = v);
        }
    }
}
