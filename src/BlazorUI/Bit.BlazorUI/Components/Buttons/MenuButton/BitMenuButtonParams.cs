namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitMenuButton{TItem}"/> component.
/// </summary>
/// <remarks>
/// The class is not generic although the component is: everything it carries is independent of the item type,
/// so one params object reaches every menu button under the <see cref="BitParams"/> regardless of what each of
/// them is bound to. The parameters that do name the item type (Items, SelectedItem, NameSelectors, the
/// templates and the callbacks) belong to a single menu button rather than to a section of a page, and are left
/// to the markup of each one.
/// </remarks>
public class BitMenuButtonParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitMenuButton{TItem}"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitMenuButton value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitMenuButton<object>)}";



    public string Name => ParamName;



    /// <summary>
    /// Detailed description of the menu button for the benefit of screen readers (rendered into <c>aria-describedby</c>).
    /// </summary>
    public string? AriaDescription { get; set; }

    /// <summary>
    /// If true, adds an <c>aria-hidden</c> attribute instructing screen readers to ignore the menu button.
    /// </summary>
    public bool? AriaHidden { get; set; }

    /// <summary>
    /// If true, the header button automatically receives focus when the page renders (rendered as the <c>autofocus</c> attribute).
    /// </summary>
    public bool? AutoFocus { get; set; }

    /// <summary>
    /// If true, the menu button enters the loading state automatically while awaiting its OnClick event and
    /// ignores further clicks of the header button until it returns.
    /// </summary>
    public bool? AutoLoading { get; set; }

    /// <summary>
    /// The background color kind of the callout.
    /// </summary>
    public BitColorKind? Background { get; set; }

    /// <summary>
    /// The value of the type attribute of the menu button.
    /// </summary>
    public BitButtonType? ButtonType { get; set; }

    /// <summary>
    /// The icon of the check mark shown on a checked item, using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="CheckIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? CheckIcon { get; set; }

    /// <summary>
    /// The name of the icon of the check mark shown on a checked item.
    /// </summary>
    public string? CheckIconName { get; set; }

    /// <summary>
    /// The aria-label of the chevron down button of the split menu button for the benefit of screen readers.
    /// </summary>
    public string? ChevronDownAriaLabel { get; set; }

    /// <summary>
    /// The icon for the chevron down part of the menu button.
    /// </summary>
    public BitIconInfo? ChevronDownIcon { get; set; }

    /// <summary>
    /// The icon name of the chevron down part of the menu button.
    /// </summary>
    public string? ChevronDownIconName { get; set; }

    /// <summary>
    /// The tooltip to show when the mouse is placed on the chevron down button of the split menu button.
    /// </summary>
    public string? ChevronDownTitle { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the menu button.
    /// </summary>
    public BitMenuButtonClassStyles? Classes { get; set; }

    /// <summary>
    /// Closes the callout when an item is clicked, which is what a menu of one-off commands wants.
    /// Turn it off for a menu the user works inside of, such as a set of checkable items.
    /// </summary>
    public bool? CloseOnItemClick { get; set; }

    /// <summary>
    /// The general color of the menu button.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// Default value of the IsToggled parameter in toggle mode.
    /// </summary>
    public bool? DefaultIsToggled { get; set; }

    /// <summary>
    /// Keeps a disabled menu button, and the disabled items of any menu button, focusable: the disabled state is
    /// conveyed with the <c>aria-disabled</c> attribute instead of the native <c>disabled</c> one, so the button
    /// stays in the tab order while its actions remain suppressed.
    /// </summary>
    public bool? DisabledInteractive { get; set; }

    /// <summary>
    /// Determines the allowed drop directions of the callout.
    /// </summary>
    public BitDropDirection? DropDirection { get; set; }

    /// <summary>
    /// The id of the form element the menu button is associated with (rendered as the <c>form</c> attribute of
    /// the header button and of the items).
    /// </summary>
    public string? FormId { get; set; }

    /// <summary>
    /// Expands the menu button width to 100% of the available width.
    /// </summary>
    public bool? FullWidth { get; set; }

    /// <summary>
    /// Gets or sets the icon to display using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="IconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Use this property to render icons from external libraries like FontAwesome, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="IconName"/> instead.
    /// </remarks>
    public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// The icon to show inside the header of menu button.
    /// </summary>
    /// <remarks>
    /// Browse available names in <c>BitIconName</c> of the <c>Bit.BlazorUI.Icons</c> nuget package or the gallery:
    /// <see href="https://blazorui.bitplatform.dev/iconography"/>.
    /// <br />
    /// For external icon libraries, use <see cref="Icon"/> instead.
    /// </remarks>
    public string? IconName { get; set; }

    /// <summary>
    /// Renders the header button as its icon alone: the text and the chevron beside it are dropped and the
    /// button becomes a square of the control's own height.
    /// </summary>
    public bool? IconOnly { get; set; }

    /// <summary>
    /// Determines whether the menu button is in the loading state.
    /// </summary>
    public bool? IsLoading { get; set; }

    /// <summary>
    /// The delay in milliseconds before the spinner appears after the menu button enters the loading state.
    /// </summary>
    public int? LoadingDelay { get; set; }

    /// <summary>
    /// The text to show beside the spinner while the menu button is in the loading state, replacing the text of
    /// the header button.
    /// </summary>
    public string? LoadingLabel { get; set; }

    /// <summary>
    /// The tallest the callout grows before its items start to scroll, as a CSS length (e.g. <c>12rem</c>).
    /// </summary>
    public string? MaxHeight { get; set; }

    /// <summary>
    /// The text the items opening a new tab are announced with. The default is "(opens in a new tab)"; an empty
    /// value takes the announcement off.
    /// </summary>
    public string? NewTabHint { get; set; }

    /// <summary>
    /// If true, removes the icon from the header button.
    /// </summary>
    public bool? NoIcon { get; set; }

    /// <summary>
    /// Stops the items opening a new tab from announcing that they do.
    /// </summary>
    public bool? NoNewTabHint { get; set; }

    /// <summary>
    /// The icon of the bullet shown on a checked single-choice item, using custom CSS classes for external
    /// icon libraries. Takes precedence over <see cref="RadioIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? RadioIcon { get; set; }

    /// <summary>
    /// The name of the icon of the bullet shown on a checked single-choice item.
    /// </summary>
    public string? RadioIconName { get; set; }

    /// <summary>
    /// Enables re-clicking the header button while the menu button is in the loading state.
    /// </summary>
    public bool? Reclickable { get; set; }

    /// <summary>
    /// The size of the menu button.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// If true, the menu button renders as a split button.
    /// </summary>
    public bool? Split { get; set; }

    /// <summary>
    /// If true, the selected item is going to change the header item.
    /// </summary>
    public bool? Sticky { get; set; }

    /// <summary>
    /// If true, stops the propagation of the click event of the menu button to the parent elements.
    /// </summary>
    public bool? StopPropagation { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the menu button.
    /// </summary>
    public BitMenuButtonClassStyles? Styles { get; set; }

    /// <summary>
    /// The icon of the chevron a submenu item carries, using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="SubmenuIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? SubmenuIcon { get; set; }

    /// <summary>
    /// The name of the icon of the chevron an item that opens a submenu carries.
    /// </summary>
    public string? SubmenuIconName { get; set; }

    /// <summary>
    /// The text to show inside the header of menu button.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// The tooltip to show when the mouse is placed on the header button.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// If true, enables the toggling behavior on the header button in split mode.
    /// </summary>
    public bool? Toggle { get; set; }

    /// <summary>
    /// The visual variant of the menu button.
    /// </summary>
    public BitVariant? Variant { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitMenuButton{TItem}"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitMenuButton{TItem}"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitMenuButton"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitMenuButton"/>.
    /// </remarks>
    /// <param name="bitMenuButton">
    /// The <see cref="BitMenuButton{TItem}"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters<TItem>(BitMenuButton<TItem> bitMenuButton) where TItem : class
    {
        if (bitMenuButton is null) return;

        UpdateBaseParameters(bitMenuButton);

        if (AriaDescription.HasValue())
        {
            bitMenuButton.TakeFromCascade(nameof(AriaDescription), AriaDescription, static m => m.AriaDescription, static (m, v) => m.AriaDescription = v);
        }

        if (AriaHidden.HasValue)
        {
            bitMenuButton.TakeFromCascade(nameof(AriaHidden), AriaHidden.Value, static m => m.AriaHidden, static (m, v) => m.AriaHidden = v);
        }

        if (AutoFocus.HasValue)
        {
            bitMenuButton.TakeFromCascade(nameof(AutoFocus), AutoFocus.Value, static m => m.AutoFocus, static (m, v) => m.AutoFocus = v);
        }

        if (AutoLoading.HasValue)
        {
            bitMenuButton.TakeFromCascade(nameof(AutoLoading), AutoLoading.Value, static m => m.AutoLoading, static (m, v) => m.AutoLoading = v);
        }

        if (Background.HasValue)
        {
            bitMenuButton.TakeFromCascade(nameof(Background), Background.Value, static m => m.Background, static (m, v) => m.Background = v);
        }

        if (ButtonType.HasValue)
        {
            bitMenuButton.TakeFromCascade(nameof(ButtonType), ButtonType.Value, static m => m.ButtonType, static (m, v) => m.ButtonType = v);
        }


        if (CheckIcon is not null)
        {
            bitMenuButton.TakeFromCascade(nameof(CheckIcon), CheckIcon, static m => m.CheckIcon, static (m, v) => m.CheckIcon = v);
        }

        if (CheckIconName.HasValue())
        {
            bitMenuButton.TakeFromCascade(nameof(CheckIconName), CheckIconName, static m => m.CheckIconName, static (m, v) => m.CheckIconName = v);
        }

        if (ChevronDownAriaLabel.HasValue())
        {
            bitMenuButton.TakeFromCascade(nameof(ChevronDownAriaLabel), ChevronDownAriaLabel, static m => m.ChevronDownAriaLabel, static (m, v) => m.ChevronDownAriaLabel = v);
        }


        if (ChevronDownIcon is not null)
        {
            bitMenuButton.TakeFromCascade(nameof(ChevronDownIcon), ChevronDownIcon, static m => m.ChevronDownIcon, static (m, v) => m.ChevronDownIcon = v);
        }

        if (ChevronDownIconName.HasValue())
        {
            bitMenuButton.TakeFromCascade(nameof(ChevronDownIconName), ChevronDownIconName, static m => m.ChevronDownIconName, static (m, v) => m.ChevronDownIconName = v);
        }

        if (ChevronDownTitle.HasValue())
        {
            bitMenuButton.TakeFromCascade(nameof(ChevronDownTitle), ChevronDownTitle, static m => m.ChevronDownTitle, static (m, v) => m.ChevronDownTitle = v);
        }

        if (Classes is not null)
        {
            bitMenuButton.TakeFromCascade(nameof(Classes), Classes, static m => m.Classes, static (m, v) => m.Classes = v);
        }

        if (CloseOnItemClick.HasValue)
        {
            bitMenuButton.TakeFromCascade(nameof(CloseOnItemClick), CloseOnItemClick.Value, static m => m.CloseOnItemClick, static (m, v) => m.CloseOnItemClick = v);
        }

        if (Color.HasValue)
        {
            bitMenuButton.TakeFromCascade(nameof(Color), Color.Value, static m => m.Color, static (m, v) => m.Color = v);
        }

        if (DefaultIsToggled.HasValue)
        {
            bitMenuButton.TakeFromCascade(nameof(DefaultIsToggled), DefaultIsToggled.Value, static m => m.DefaultIsToggled, static (m, v) => m.DefaultIsToggled = v);
        }

        if (DisabledInteractive.HasValue)
        {
            bitMenuButton.TakeFromCascade(nameof(DisabledInteractive), DisabledInteractive.Value, static m => m.DisabledInteractive, static (m, v) => m.DisabledInteractive = v);
        }

        if (DropDirection.HasValue)
        {
            bitMenuButton.TakeFromCascade(nameof(DropDirection), DropDirection.Value, static m => m.DropDirection, static (m, v) => m.DropDirection = v);
        }

        if (FormId.HasValue())
        {
            bitMenuButton.TakeFromCascade(nameof(FormId), FormId, static m => m.FormId, static (m, v) => m.FormId = v);
        }

        if (FullWidth.HasValue)
        {
            bitMenuButton.TakeFromCascade(nameof(FullWidth), FullWidth.Value, static m => m.FullWidth, static (m, v) => m.FullWidth = v);
        }


        if (Icon is not null)
        {
            bitMenuButton.TakeFromCascade(nameof(Icon), Icon, static m => m.Icon, static (m, v) => m.Icon = v);
        }

        if (IconName.HasValue())
        {
            bitMenuButton.TakeFromCascade(nameof(IconName), IconName, static m => m.IconName, static (m, v) => m.IconName = v);
        }

        if (IconOnly.HasValue)
        {
            bitMenuButton.TakeFromCascade(nameof(IconOnly), IconOnly.Value, static m => m.IconOnly, static (m, v) => m.IconOnly = v);
        }

        if (IsLoading.HasValue)
        {
            bitMenuButton.TakeFromCascade(nameof(IsLoading), IsLoading.Value, static m => m.IsLoading, static (m, v) => m.IsLoading = v);
        }

        if (LoadingDelay.HasValue)
        {
            bitMenuButton.TakeFromCascade(nameof(LoadingDelay), LoadingDelay.Value, static m => m.LoadingDelay, static (m, v) => m.LoadingDelay = v);
        }

        if (LoadingLabel.HasValue())
        {
            bitMenuButton.TakeFromCascade(nameof(LoadingLabel), LoadingLabel, static m => m.LoadingLabel, static (m, v) => m.LoadingLabel = v);
        }

        if (MaxHeight.HasValue())
        {
            bitMenuButton.TakeFromCascade(nameof(MaxHeight), MaxHeight, static m => m.MaxHeight, static (m, v) => m.MaxHeight = v);
        }

        // an empty hint is a value of its own - the one that takes the announcement off - so only null is
        // what leaves the component to its default.
        if (NewTabHint is not null)
        {
            bitMenuButton.TakeFromCascade(nameof(NewTabHint), NewTabHint, static m => m.NewTabHint, static (m, v) => m.NewTabHint = v);
        }

        if (NoNewTabHint.HasValue)
        {
            bitMenuButton.TakeFromCascade(nameof(NoNewTabHint), NoNewTabHint.Value, static m => m.NoNewTabHint, static (m, v) => m.NoNewTabHint = v);
        }

        if (NoIcon.HasValue)
        {
            bitMenuButton.TakeFromCascade(nameof(NoIcon), NoIcon.Value, static m => m.NoIcon, static (m, v) => m.NoIcon = v);
        }


        if (RadioIcon is not null)
        {
            bitMenuButton.TakeFromCascade(nameof(RadioIcon), RadioIcon, static m => m.RadioIcon, static (m, v) => m.RadioIcon = v);
        }

        if (RadioIconName.HasValue())
        {
            bitMenuButton.TakeFromCascade(nameof(RadioIconName), RadioIconName, static m => m.RadioIconName, static (m, v) => m.RadioIconName = v);
        }

        if (Reclickable.HasValue)
        {
            bitMenuButton.TakeFromCascade(nameof(Reclickable), Reclickable.Value, static m => m.Reclickable, static (m, v) => m.Reclickable = v);
        }

        if (Size.HasValue)
        {
            bitMenuButton.TakeFromCascade(nameof(Size), Size.Value, static m => m.Size, static (m, v) => m.Size = v);
        }

        if (Split.HasValue)
        {
            bitMenuButton.TakeFromCascade(nameof(Split), Split.Value, static m => m.Split, static (m, v) => m.Split = v);
        }

        if (Sticky.HasValue)
        {
            bitMenuButton.TakeFromCascade(nameof(Sticky), Sticky.Value, static m => m.Sticky, static (m, v) => m.Sticky = v);
        }

        if (StopPropagation.HasValue)
        {
            bitMenuButton.TakeFromCascade(nameof(StopPropagation), StopPropagation.Value, static m => m.StopPropagation, static (m, v) => m.StopPropagation = v);
        }

        if (Styles is not null)
        {
            bitMenuButton.TakeFromCascade(nameof(Styles), Styles, static m => m.Styles, static (m, v) => m.Styles = v);
        }


        if (SubmenuIcon is not null)
        {
            bitMenuButton.TakeFromCascade(nameof(SubmenuIcon), SubmenuIcon, static m => m.SubmenuIcon, static (m, v) => m.SubmenuIcon = v);
        }

        if (SubmenuIconName.HasValue())
        {
            bitMenuButton.TakeFromCascade(nameof(SubmenuIconName), SubmenuIconName, static m => m.SubmenuIconName, static (m, v) => m.SubmenuIconName = v);
        }

        if (Text.HasValue())
        {
            bitMenuButton.TakeFromCascade(nameof(Text), Text, static m => m.Text, static (m, v) => m.Text = v);
        }

        if (Title.HasValue())
        {
            bitMenuButton.TakeFromCascade(nameof(Title), Title, static m => m.Title, static (m, v) => m.Title = v);
        }

        if (Toggle.HasValue)
        {
            bitMenuButton.TakeFromCascade(nameof(Toggle), Toggle.Value, static m => m.Toggle, static (m, v) => m.Toggle = v);
        }

        if (Variant.HasValue)
        {
            bitMenuButton.TakeFromCascade(nameof(Variant), Variant.Value, static m => m.Variant, static (m, v) => m.Variant = v);
        }
    }
}
