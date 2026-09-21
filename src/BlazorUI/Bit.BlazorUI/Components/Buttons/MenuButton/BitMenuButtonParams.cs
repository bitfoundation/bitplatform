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
    /// If true, removes the icon from the header button.
    /// </summary>
    public bool? NoIcon { get; set; }

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

        if (AriaDescription.HasValue() && bitMenuButton.HasNotBeenSet(nameof(AriaDescription)))
        {
            bitMenuButton.AriaDescription = AriaDescription;
        }

        if (AriaHidden.HasValue && bitMenuButton.HasNotBeenSet(nameof(AriaHidden)))
        {
            bitMenuButton.AriaHidden = AriaHidden.Value;
        }

        if (AutoFocus.HasValue && bitMenuButton.HasNotBeenSet(nameof(AutoFocus)))
        {
            bitMenuButton.AutoFocus = AutoFocus.Value;
        }

        if (AutoLoading.HasValue && bitMenuButton.HasNotBeenSet(nameof(AutoLoading)))
        {
            bitMenuButton.AutoLoading = AutoLoading.Value;
        }

        if (Background.HasValue && bitMenuButton.HasNotBeenSet(nameof(Background)))
        {
            bitMenuButton.Background = Background.Value;

            bitMenuButton.ClassBuilder.Reset();
        }

        if (ButtonType.HasValue && bitMenuButton.HasNotBeenSet(nameof(ButtonType)))
        {
            bitMenuButton.ButtonType = ButtonType.Value;
        }

        if (CheckIcon is not null && bitMenuButton.HasNotBeenSet(nameof(CheckIcon)))
        {
            bitMenuButton.CheckIcon = CheckIcon;
        }

        if (CheckIconName.HasValue() && bitMenuButton.HasNotBeenSet(nameof(CheckIconName)))
        {
            bitMenuButton.CheckIconName = CheckIconName;
        }

        if (ChevronDownAriaLabel.HasValue() && bitMenuButton.HasNotBeenSet(nameof(ChevronDownAriaLabel)))
        {
            bitMenuButton.ChevronDownAriaLabel = ChevronDownAriaLabel;
        }

        if (ChevronDownIcon is not null && bitMenuButton.HasNotBeenSet(nameof(ChevronDownIcon)))
        {
            bitMenuButton.ChevronDownIcon = ChevronDownIcon;
        }

        if (ChevronDownIconName.HasValue() && bitMenuButton.HasNotBeenSet(nameof(ChevronDownIconName)))
        {
            bitMenuButton.ChevronDownIconName = ChevronDownIconName;
        }

        if (ChevronDownTitle.HasValue() && bitMenuButton.HasNotBeenSet(nameof(ChevronDownTitle)))
        {
            bitMenuButton.ChevronDownTitle = ChevronDownTitle;
        }

        if (Classes is not null && bitMenuButton.HasNotBeenSet(nameof(Classes)))
        {
            bitMenuButton.Classes = Classes;

            bitMenuButton.ClassBuilder.Reset();
        }

        if (CloseOnItemClick.HasValue && bitMenuButton.HasNotBeenSet(nameof(CloseOnItemClick)))
        {
            bitMenuButton.CloseOnItemClick = CloseOnItemClick.Value;
        }

        if (Color.HasValue && bitMenuButton.HasNotBeenSet(nameof(Color)))
        {
            bitMenuButton.Color = Color.Value;

            bitMenuButton.ClassBuilder.Reset();
        }

        if (DefaultIsToggled.HasValue && bitMenuButton.HasNotBeenSet(nameof(DefaultIsToggled)))
        {
            bitMenuButton.DefaultIsToggled = DefaultIsToggled.Value;
        }

        if (DisabledInteractive.HasValue && bitMenuButton.HasNotBeenSet(nameof(DisabledInteractive)))
        {
            bitMenuButton.DisabledInteractive = DisabledInteractive.Value;
        }

        if (DropDirection.HasValue && bitMenuButton.HasNotBeenSet(nameof(DropDirection)))
        {
            bitMenuButton.DropDirection = DropDirection.Value;
        }

        if (FormId.HasValue() && bitMenuButton.HasNotBeenSet(nameof(FormId)))
        {
            bitMenuButton.FormId = FormId;
        }

        if (FullWidth.HasValue && bitMenuButton.HasNotBeenSet(nameof(FullWidth)))
        {
            bitMenuButton.FullWidth = FullWidth.Value;

            bitMenuButton.ClassBuilder.Reset();
        }

        if (Icon is not null && bitMenuButton.HasNotBeenSet(nameof(Icon)))
        {
            bitMenuButton.Icon = Icon;
        }

        if (IconName.HasValue() && bitMenuButton.HasNotBeenSet(nameof(IconName)))
        {
            bitMenuButton.IconName = IconName;
        }

        if (IconOnly.HasValue && bitMenuButton.HasNotBeenSet(nameof(IconOnly)))
        {
            bitMenuButton.IconOnly = IconOnly.Value;

            bitMenuButton.ClassBuilder.Reset();
        }

        if (IsLoading.HasValue && bitMenuButton.HasNotBeenSet(nameof(IsLoading)))
        {
            bitMenuButton.IsLoading = IsLoading.Value;

            bitMenuButton.ClassBuilder.Reset();
        }

        if (LoadingDelay.HasValue && bitMenuButton.HasNotBeenSet(nameof(LoadingDelay)))
        {
            bitMenuButton.LoadingDelay = LoadingDelay.Value;
        }

        if (LoadingLabel.HasValue() && bitMenuButton.HasNotBeenSet(nameof(LoadingLabel)))
        {
            bitMenuButton.LoadingLabel = LoadingLabel;
        }

        if (MaxHeight.HasValue() && bitMenuButton.HasNotBeenSet(nameof(MaxHeight)))
        {
            bitMenuButton.MaxHeight = MaxHeight;
        }

        if (NoIcon.HasValue && bitMenuButton.HasNotBeenSet(nameof(NoIcon)))
        {
            bitMenuButton.NoIcon = NoIcon.Value;
        }

        if (RadioIcon is not null && bitMenuButton.HasNotBeenSet(nameof(RadioIcon)))
        {
            bitMenuButton.RadioIcon = RadioIcon;
        }

        if (RadioIconName.HasValue() && bitMenuButton.HasNotBeenSet(nameof(RadioIconName)))
        {
            bitMenuButton.RadioIconName = RadioIconName;
        }

        if (Reclickable.HasValue && bitMenuButton.HasNotBeenSet(nameof(Reclickable)))
        {
            bitMenuButton.Reclickable = Reclickable.Value;

            bitMenuButton.ClassBuilder.Reset();
        }

        if (Size.HasValue && bitMenuButton.HasNotBeenSet(nameof(Size)))
        {
            bitMenuButton.Size = Size.Value;

            bitMenuButton.ClassBuilder.Reset();
        }

        if (Split.HasValue && bitMenuButton.HasNotBeenSet(nameof(Split)))
        {
            bitMenuButton.Split = Split.Value;

            bitMenuButton.ClassBuilder.Reset();
        }

        if (Sticky.HasValue && bitMenuButton.HasNotBeenSet(nameof(Sticky)))
        {
            bitMenuButton.Sticky = Sticky.Value;

            bitMenuButton.ClassBuilder.Reset();
            bitMenuButton.StyleBuilder.Reset();
        }

        if (StopPropagation.HasValue && bitMenuButton.HasNotBeenSet(nameof(StopPropagation)))
        {
            bitMenuButton.StopPropagation = StopPropagation.Value;
        }

        if (Styles is not null && bitMenuButton.HasNotBeenSet(nameof(Styles)))
        {
            bitMenuButton.Styles = Styles;

            bitMenuButton.StyleBuilder.Reset();
        }

        if (SubmenuIcon is not null && bitMenuButton.HasNotBeenSet(nameof(SubmenuIcon)))
        {
            bitMenuButton.SubmenuIcon = SubmenuIcon;
        }

        if (SubmenuIconName.HasValue() && bitMenuButton.HasNotBeenSet(nameof(SubmenuIconName)))
        {
            bitMenuButton.SubmenuIconName = SubmenuIconName;
        }

        if (Text.HasValue() && bitMenuButton.HasNotBeenSet(nameof(Text)))
        {
            bitMenuButton.Text = Text;
        }

        if (Title.HasValue() && bitMenuButton.HasNotBeenSet(nameof(Title)))
        {
            bitMenuButton.Title = Title;
        }

        if (Toggle.HasValue && bitMenuButton.HasNotBeenSet(nameof(Toggle)))
        {
            bitMenuButton.Toggle = Toggle.Value;

            bitMenuButton.ClassBuilder.Reset();
            bitMenuButton.StyleBuilder.Reset();
        }

        if (Variant.HasValue && bitMenuButton.HasNotBeenSet(nameof(Variant)))
        {
            bitMenuButton.Variant = Variant.Value;

            bitMenuButton.ClassBuilder.Reset();
        }
    }
}
