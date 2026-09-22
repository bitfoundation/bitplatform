namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitButtonGroup{TItem}"/> component.
/// </summary>
/// <remarks>
/// It carries the parameters that say nothing about the item type, so one object fits every group under it whatever
/// each of them holds. What is left on the group itself is what no subtree can share: the parameters that depend on
/// the item type (Items, ItemTemplate, NameSelectors, and the item callbacks) and the selection state the group is
/// bound to (ToggleKey and ToggleKeys), which belongs to one group rather than being a default for many.
/// The initial selection is still cascadable through <see cref="DefaultToggleKey"/> and <see cref="DefaultToggleKeys"/>.
/// </remarks>
public class BitButtonGroupParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitButtonGroup{TItem}"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitButtonGroup value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// <br />
    /// The name is the bare name of the component, so a single cascade reaches every group whatever it is generic over.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitButtonGroup<BitButtonGroupOption>)}";



    public string Name => ParamName;



    /// <summary>
    /// Gives the keyboard focus to the ButtonGroup when the page first renders.
    /// </summary>
    public bool? AutoFocus { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the ButtonGroup.
    /// </summary>
    public BitButtonGroupClassStyles? Classes { get; set; }

    /// <summary>
    /// Defines the general colors available in the bit BlazorUI.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// The default key that will be initially used to set toggled item in toggle mode if the ToggleKey parameter is not set.
    /// </summary>
    public string? DefaultToggleKey { get; set; }

    /// <summary>
    /// The default keys that will be initially used to set the toggled items in the Multiple selection mode
    /// if the ToggleKeys parameter is not set.
    /// </summary>
    public IEnumerable<string>? DefaultToggleKeys { get; set; }

    /// <summary>
    /// Detaches the buttons from each other, so each button is rendered as a separate rounded button.
    /// </summary>
    public bool? Detached { get; set; }

    /// <summary>
    /// Keeps the disabled buttons focusable by rendering them with the aria-disabled attribute instead of
    /// the disabled attribute, so that assistive technologies can still discover them.
    /// </summary>
    public bool? DisabledInteractive { get; set; }

    /// <summary>
    /// Enables the fixed-toggle mode that ensures one item to be always toggled.
    /// In the Multiple selection mode it prevents un-toggling the last toggled item.
    /// </summary>
    public bool? FixedToggle { get; set; }

    /// <summary>
    /// Expand the ButtonGroup width to 100% of the available width.
    /// </summary>
    public bool? FullWidth { get; set; }

    /// <summary>
    /// The gap between the buttons of the ButtonGroup in the detached mode, as any CSS length.
    /// </summary>
    public string? Gap { get; set; }

    /// <summary>
    /// Determines that only the icon should be rendered.
    /// </summary>
    public bool? IconOnly { get; set; }

    /// <summary>
    /// Gives every button an equal width so that the buttons evenly fill the width of the ButtonGroup.
    /// </summary>
    public bool? Justified { get; set; }

    /// <summary>
    /// The maximum number of items that can be toggled at the same time in the Multiple selection mode.
    /// </summary>
    public int? MaxToggles { get; set; }

    /// <summary>
    /// Enables the roving tabindex behavior, which turns the whole ButtonGroup into a single tab stop
    /// that is navigable using the arrow, Home, and End keys.
    /// </summary>
    public bool? Navigable { get; set; }

    /// <summary>
    /// Determines how the ButtonGroup behaves when its buttons do not fit in the available space.
    /// </summary>
    public BitButtonGroupOverflow? Overflow { get; set; }

    /// <summary>
    /// Renders the ButtonGroup with fully rounded (pill shaped) corners.
    /// </summary>
    public bool? Rounded { get; set; }

    /// <summary>
    /// Toggles the focused item while navigating the ButtonGroup using the keyboard, so that the selection
    /// follows the focus. When not set, it follows the selection mode.
    /// </summary>
    public bool? SelectOnFocus { get; set; }

    /// <summary>
    /// Determines how many items can be toggled at the same time.
    /// When not set, it falls back to Single if the Toggle parameter is enabled, otherwise None.
    /// </summary>
    public BitButtonGroupSelectionMode? SelectionMode { get; set; }

    /// <summary>
    /// Renders a check mark at the start of the toggled buttons.
    /// </summary>
    public bool? ShowSelectionIndicator { get; set; }

    /// <summary>
    /// The size of ButtonGroup, Possible values: Small | Medium | Large
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the ButtonGroup.
    /// </summary>
    public BitButtonGroupClassStyles? Styles { get; set; }

    /// <summary>
    /// Display ButtonGroup with toggle mode enabled for each button.
    /// It is a shorthand of setting the SelectionMode parameter to Single.
    /// </summary>
    public bool? Toggle { get; set; }

    /// <summary>
    /// The visual variant of the button group.
    /// </summary>
    public BitVariant? Variant { get; set; }

    /// <summary>
    /// Defines whether to render ButtonGroup children vertically.
    /// </summary>
    public bool? Vertical { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitButtonGroup{TItem}"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitButtonGroup{TItem}"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitButtonGroup"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitButtonGroup"/>.
    /// </remarks>
    /// <param name="bitButtonGroup">
    /// The <see cref="BitButtonGroup{TItem}"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters<TItem>(BitButtonGroup<TItem> bitButtonGroup) where TItem : class
    {
        if (bitButtonGroup is null) return;

        UpdateBaseParameters(bitButtonGroup);

        if (AutoFocus.HasValue && bitButtonGroup.HasNotBeenSet(nameof(AutoFocus)))
        {
            bitButtonGroup.AutoFocus = AutoFocus.Value;
        }

        if (Classes is not null && bitButtonGroup.HasNotBeenSet(nameof(Classes)))
        {
            bitButtonGroup.Classes = Classes;

            bitButtonGroup.ClassBuilder.Reset();
        }

        if (Color.HasValue && bitButtonGroup.HasNotBeenSet(nameof(Color)))
        {
            bitButtonGroup.Color = Color.Value;

            bitButtonGroup.ClassBuilder.Reset();
        }

        if (DefaultToggleKey.HasValue() && bitButtonGroup.HasNotBeenSet(nameof(DefaultToggleKey)))
        {
            bitButtonGroup.DefaultToggleKey = DefaultToggleKey;
        }

        if (DefaultToggleKeys is not null && bitButtonGroup.HasNotBeenSet(nameof(DefaultToggleKeys)))
        {
            bitButtonGroup.DefaultToggleKeys = DefaultToggleKeys;
        }

        if (Detached.HasValue && bitButtonGroup.HasNotBeenSet(nameof(Detached)))
        {
            bitButtonGroup.Detached = Detached.Value;

            bitButtonGroup.ClassBuilder.Reset();
        }

        if (DisabledInteractive.HasValue && bitButtonGroup.HasNotBeenSet(nameof(DisabledInteractive)))
        {
            bitButtonGroup.DisabledInteractive = DisabledInteractive.Value;
        }

        if (FixedToggle.HasValue && bitButtonGroup.HasNotBeenSet(nameof(FixedToggle)))
        {
            bitButtonGroup.FixedToggle = FixedToggle.Value;
        }

        if (FullWidth.HasValue && bitButtonGroup.HasNotBeenSet(nameof(FullWidth)))
        {
            bitButtonGroup.FullWidth = FullWidth.Value;

            bitButtonGroup.ClassBuilder.Reset();
        }

        if (Gap.HasValue() && bitButtonGroup.HasNotBeenSet(nameof(Gap)))
        {
            bitButtonGroup.Gap = Gap;

            bitButtonGroup.StyleBuilder.Reset();
        }

        if (IconOnly.HasValue && bitButtonGroup.HasNotBeenSet(nameof(IconOnly)))
        {
            bitButtonGroup.IconOnly = IconOnly.Value;

            bitButtonGroup.ClassBuilder.Reset();
        }

        if (Justified.HasValue && bitButtonGroup.HasNotBeenSet(nameof(Justified)))
        {
            bitButtonGroup.Justified = Justified.Value;

            bitButtonGroup.ClassBuilder.Reset();
        }

        if (MaxToggles.HasValue && bitButtonGroup.HasNotBeenSet(nameof(MaxToggles)))
        {
            bitButtonGroup.MaxToggles = MaxToggles.Value;
        }

        if (Navigable.HasValue && bitButtonGroup.HasNotBeenSet(nameof(Navigable)))
        {
            bitButtonGroup.Navigable = Navigable.Value;

            bitButtonGroup.ClassBuilder.Reset();
        }

        if (Overflow.HasValue && bitButtonGroup.HasNotBeenSet(nameof(Overflow)))
        {
            bitButtonGroup.Overflow = Overflow.Value;

            bitButtonGroup.ClassBuilder.Reset();
        }

        if (Rounded.HasValue && bitButtonGroup.HasNotBeenSet(nameof(Rounded)))
        {
            bitButtonGroup.Rounded = Rounded.Value;

            bitButtonGroup.ClassBuilder.Reset();
        }

        if (SelectOnFocus.HasValue && bitButtonGroup.HasNotBeenSet(nameof(SelectOnFocus)))
        {
            bitButtonGroup.SelectOnFocus = SelectOnFocus.Value;
        }

        if (SelectionMode.HasValue && bitButtonGroup.HasNotBeenSet(nameof(SelectionMode)))
        {
            bitButtonGroup.SelectionMode = SelectionMode.Value;
        }

        if (ShowSelectionIndicator.HasValue && bitButtonGroup.HasNotBeenSet(nameof(ShowSelectionIndicator)))
        {
            bitButtonGroup.ShowSelectionIndicator = ShowSelectionIndicator.Value;
        }

        if (Size.HasValue && bitButtonGroup.HasNotBeenSet(nameof(Size)))
        {
            bitButtonGroup.Size = Size.Value;

            bitButtonGroup.ClassBuilder.Reset();
        }

        if (Styles is not null && bitButtonGroup.HasNotBeenSet(nameof(Styles)))
        {
            bitButtonGroup.Styles = Styles;

            bitButtonGroup.StyleBuilder.Reset();
        }

        if (Toggle.HasValue && bitButtonGroup.HasNotBeenSet(nameof(Toggle)))
        {
            bitButtonGroup.Toggle = Toggle.Value;
        }

        if (Variant.HasValue && bitButtonGroup.HasNotBeenSet(nameof(Variant)))
        {
            bitButtonGroup.Variant = Variant.Value;

            bitButtonGroup.ClassBuilder.Reset();
        }

        if (Vertical.HasValue && bitButtonGroup.HasNotBeenSet(nameof(Vertical)))
        {
            bitButtonGroup.Vertical = Vertical.Value;

            bitButtonGroup.ClassBuilder.Reset();
        }
    }
}
