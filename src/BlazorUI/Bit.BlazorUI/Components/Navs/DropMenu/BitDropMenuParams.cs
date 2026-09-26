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
    public BitCalloutAlignment? Alignment { get; set; }

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
    public BitPanelPosition? PanelPosition { get; set; }

    /// <summary>
    /// Renders the drop menu in responsive mode on small screens.
    /// </summary>
    public bool? Responsive { get; set; }

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

        if (Alignment.HasValue && bitDropMenu.HasNotBeenSet(nameof(Alignment)))
        {
            bitDropMenu.Alignment = Alignment.Value;
        }

        if (AriaDescription.HasValue() && bitDropMenu.HasNotBeenSet(nameof(AriaDescription)))
        {
            bitDropMenu.AriaDescription = AriaDescription;
        }

        if (AriaHidden.HasValue && bitDropMenu.HasNotBeenSet(nameof(AriaHidden)))
        {
            bitDropMenu.AriaHidden = AriaHidden.Value;
        }

        if (AutoClose.HasValue && bitDropMenu.HasNotBeenSet(nameof(AutoClose)))
        {
            bitDropMenu.AutoClose = AutoClose.Value;
        }

        if (AutoFocus.HasValue && bitDropMenu.HasNotBeenSet(nameof(AutoFocus)))
        {
            bitDropMenu.AutoFocus = AutoFocus.Value;
        }

        if (Background.HasValue && bitDropMenu.HasNotBeenSet(nameof(Background)))
        {
            bitDropMenu.Background = Background.Value;
        }

        if (Border.HasValue && bitDropMenu.HasNotBeenSet(nameof(Border)))
        {
            bitDropMenu.Border = Border.Value;
        }

        if (ChevronDownIcon is not null && bitDropMenu.HasNotBeenSet(nameof(ChevronDownIcon)))
        {
            bitDropMenu.ChevronDownIcon = ChevronDownIcon;
        }

        if (ChevronDownIconName.HasValue() && bitDropMenu.HasNotBeenSet(nameof(ChevronDownIconName)))
        {
            bitDropMenu.ChevronDownIconName = ChevronDownIconName;
        }

        if (Classes is not null && bitDropMenu.HasNotBeenSet(nameof(Classes)))
        {
            bitDropMenu.Classes = Classes;

            bitDropMenu.ClassBuilder.Reset();
        }

        if (Color.HasValue && bitDropMenu.HasNotBeenSet(nameof(Color)))
        {
            bitDropMenu.Color = Color.Value;

            bitDropMenu.ClassBuilder.Reset();
        }

        if (DropDirection.HasValue && bitDropMenu.HasNotBeenSet(nameof(DropDirection)))
        {
            bitDropMenu.DropDirection = DropDirection.Value;
        }

        if (FullWidth.HasValue && bitDropMenu.HasNotBeenSet(nameof(FullWidth)))
        {
            bitDropMenu.FullWidth = FullWidth.Value;

            bitDropMenu.ClassBuilder.Reset();
        }

        if (HoverCloseDelay.HasValue && bitDropMenu.HasNotBeenSet(nameof(HoverCloseDelay)))
        {
            bitDropMenu.HoverCloseDelay = HoverCloseDelay.Value;
        }

        if (HoverOpenDelay.HasValue && bitDropMenu.HasNotBeenSet(nameof(HoverOpenDelay)))
        {
            bitDropMenu.HoverOpenDelay = HoverOpenDelay.Value;
        }

        if (Icon is not null && bitDropMenu.HasNotBeenSet(nameof(Icon)))
        {
            bitDropMenu.Icon = Icon;
        }

        if (IconName.HasValue() && bitDropMenu.HasNotBeenSet(nameof(IconName)))
        {
            bitDropMenu.IconName = IconName;
        }

        if (IsLoading.HasValue && bitDropMenu.HasNotBeenSet(nameof(IsLoading)))
        {
            bitDropMenu.IsLoading = IsLoading.Value;

            bitDropMenu.ClassBuilder.Reset();
        }

        if (LazyRender.HasValue && bitDropMenu.HasNotBeenSet(nameof(LazyRender)))
        {
            bitDropMenu.LazyRender = LazyRender.Value;
        }

        if (MatchWidth.HasValue && bitDropMenu.HasNotBeenSet(nameof(MatchWidth)))
        {
            bitDropMenu.MatchWidth = MatchWidth.Value;
        }

        if (MaxHeight.HasValue() && bitDropMenu.HasNotBeenSet(nameof(MaxHeight)))
        {
            bitDropMenu.MaxHeight = MaxHeight;
        }

        if (MaxWidth.HasValue() && bitDropMenu.HasNotBeenSet(nameof(MaxWidth)))
        {
            bitDropMenu.MaxWidth = MaxWidth;
        }

        if (MinWidth.HasValue() && bitDropMenu.HasNotBeenSet(nameof(MinWidth)))
        {
            bitDropMenu.MinWidth = MinWidth;
        }

        if (NoChevron.HasValue && bitDropMenu.HasNotBeenSet(nameof(NoChevron)))
        {
            bitDropMenu.NoChevron = NoChevron.Value;
        }

        if (NoShadow.HasValue && bitDropMenu.HasNotBeenSet(nameof(NoShadow)))
        {
            bitDropMenu.NoShadow = NoShadow.Value;
        }

        if (OpenOnHover.HasValue && bitDropMenu.HasNotBeenSet(nameof(OpenOnHover)))
        {
            bitDropMenu.OpenOnHover = OpenOnHover.Value;
        }

        if (PanelPosition.HasValue && bitDropMenu.HasNotBeenSet(nameof(PanelPosition)))
        {
            bitDropMenu.PanelPosition = PanelPosition.Value;
        }

        if (Responsive.HasValue && bitDropMenu.HasNotBeenSet(nameof(Responsive)))
        {
            bitDropMenu.Responsive = Responsive.Value;
        }

        if (Size.HasValue && bitDropMenu.HasNotBeenSet(nameof(Size)))
        {
            bitDropMenu.Size = Size.Value;

            bitDropMenu.ClassBuilder.Reset();
        }

        if (Styles is not null && bitDropMenu.HasNotBeenSet(nameof(Styles)))
        {
            bitDropMenu.Styles = Styles;

            bitDropMenu.StyleBuilder.Reset();
        }

        if (Title.HasValue() && bitDropMenu.HasNotBeenSet(nameof(Title)))
        {
            bitDropMenu.Title = Title;
        }

        if (Transparent.HasValue && bitDropMenu.HasNotBeenSet(nameof(Transparent)))
        {
            bitDropMenu.Transparent = Transparent.Value;

            bitDropMenu.ClassBuilder.Reset();
        }

        if (TrapFocus.HasValue && bitDropMenu.HasNotBeenSet(nameof(TrapFocus)))
        {
            bitDropMenu.TrapFocus = TrapFocus.Value;
        }

        if (Variant.HasValue && bitDropMenu.HasNotBeenSet(nameof(Variant)))
        {
            bitDropMenu.Variant = Variant.Value;

            bitDropMenu.ClassBuilder.Reset();
        }

        if (Width.HasValue() && bitDropMenu.HasNotBeenSet(nameof(Width)))
        {
            bitDropMenu.Width = Width;
        }
    }
}
