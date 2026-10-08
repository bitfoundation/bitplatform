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
    public BitSelectionMode? SelectionMode { get; set; }

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

        if (AutoFocus.HasValue)
        {
            bitButtonGroup.TakeFromCascade(nameof(AutoFocus), AutoFocus.Value, static b => b.AutoFocus, static (b, v) => b.AutoFocus = v);
        }

        if (Classes is not null)
        {
            bitButtonGroup.TakeFromCascade(nameof(Classes), Classes, static b => b.Classes, static (b, v) => b.Classes = v);
        }

        if (Color.HasValue)
        {
            bitButtonGroup.TakeFromCascade(nameof(Color), Color.Value, static b => b.Color, static (b, v) => b.Color = v);
        }

        if (DefaultToggleKey.HasValue())
        {
            bitButtonGroup.TakeFromCascade(nameof(DefaultToggleKey), DefaultToggleKey, static b => b.DefaultToggleKey, static (b, v) => b.DefaultToggleKey = v);
        }

        if (DefaultToggleKeys is not null)
        {
            bitButtonGroup.TakeFromCascade(nameof(DefaultToggleKeys), DefaultToggleKeys, static b => b.DefaultToggleKeys, static (b, v) => b.DefaultToggleKeys = v);
        }

        if (Detached.HasValue)
        {
            bitButtonGroup.TakeFromCascade(nameof(Detached), Detached.Value, static b => b.Detached, static (b, v) => b.Detached = v);
        }

        if (DisabledInteractive.HasValue)
        {
            bitButtonGroup.TakeFromCascade(nameof(DisabledInteractive), DisabledInteractive.Value, static b => b.DisabledInteractive, static (b, v) => b.DisabledInteractive = v);
        }

        if (FixedToggle.HasValue)
        {
            bitButtonGroup.TakeFromCascade(nameof(FixedToggle), FixedToggle.Value, static b => b.FixedToggle, static (b, v) => b.FixedToggle = v);
        }

        if (FullWidth.HasValue)
        {
            bitButtonGroup.TakeFromCascade(nameof(FullWidth), FullWidth.Value, static b => b.FullWidth, static (b, v) => b.FullWidth = v);
        }

        if (Gap.HasValue())
        {
            bitButtonGroup.TakeFromCascade(nameof(Gap), Gap, static b => b.Gap, static (b, v) => b.Gap = v);
        }

        if (IconOnly.HasValue)
        {
            bitButtonGroup.TakeFromCascade(nameof(IconOnly), IconOnly.Value, static b => b.IconOnly, static (b, v) => b.IconOnly = v);
        }

        if (Justified.HasValue)
        {
            bitButtonGroup.TakeFromCascade(nameof(Justified), Justified.Value, static b => b.Justified, static (b, v) => b.Justified = v);
        }

        if (MaxToggles.HasValue)
        {
            bitButtonGroup.TakeFromCascade(nameof(MaxToggles), MaxToggles.Value, static b => b.MaxToggles, static (b, v) => b.MaxToggles = v);
        }

        if (Navigable.HasValue)
        {
            bitButtonGroup.TakeFromCascade(nameof(Navigable), Navigable.Value, static b => b.Navigable, static (b, v) => b.Navigable = v);
        }

        if (Overflow.HasValue)
        {
            bitButtonGroup.TakeFromCascade(nameof(Overflow), Overflow.Value, static b => b.Overflow, static (b, v) => b.Overflow = v);
        }

        if (Rounded.HasValue)
        {
            bitButtonGroup.TakeFromCascade(nameof(Rounded), Rounded.Value, static b => b.Rounded, static (b, v) => b.Rounded = v);
        }

        if (SelectOnFocus.HasValue)
        {
            bitButtonGroup.TakeFromCascade(nameof(SelectOnFocus), SelectOnFocus.Value, static b => b.SelectOnFocus, static (b, v) => b.SelectOnFocus = v);
        }

        if (SelectionMode.HasValue)
        {
            bitButtonGroup.TakeFromCascade(nameof(SelectionMode), SelectionMode.Value, static b => b.SelectionMode, static (b, v) => b.SelectionMode = v);
        }

        if (ShowSelectionIndicator.HasValue)
        {
            bitButtonGroup.TakeFromCascade(nameof(ShowSelectionIndicator), ShowSelectionIndicator.Value, static b => b.ShowSelectionIndicator, static (b, v) => b.ShowSelectionIndicator = v);
        }

        if (Size.HasValue)
        {
            bitButtonGroup.TakeFromCascade(nameof(Size), Size.Value, static b => b.Size, static (b, v) => b.Size = v);
        }

        if (Styles is not null)
        {
            bitButtonGroup.TakeFromCascade(nameof(Styles), Styles, static b => b.Styles, static (b, v) => b.Styles = v);
        }

        if (Toggle.HasValue)
        {
            bitButtonGroup.TakeFromCascade(nameof(Toggle), Toggle.Value, static b => b.Toggle, static (b, v) => b.Toggle = v);
        }

        if (Variant.HasValue)
        {
            bitButtonGroup.TakeFromCascade(nameof(Variant), Variant.Value, static b => b.Variant, static (b, v) => b.Variant = v);
        }

        if (Vertical.HasValue)
        {
            bitButtonGroup.TakeFromCascade(nameof(Vertical), Vertical.Value, static b => b.Vertical, static (b, v) => b.Vertical = v);
        }
    }
}
