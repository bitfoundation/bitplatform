namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitTimeZonePicker{TValue}"/> component.
/// </summary>
/// <remarks>
/// What it carries is a default and not an override: a time zone picker that writes a parameter for itself keeps its
/// own value, and only what it left unset is filled in from the cascade.
/// <br />
/// The time zone picker is generic over its value type, but nothing worth sharing between the pickers of a page is, so
/// this object is not: it serves every picker under it from a single non-generic <see cref="BitParams"/> list, whether
/// it is bound to a TimeZoneInfo or to an id. Three groups of parameters are deliberately left out of it, because a
/// value shared between fields would be wrong rather than merely unused: what identifies a field and carries its value
/// (<c>Value</c>, <c>DefaultValue</c>, <c>IsOpen</c>, <c>Name</c>, <c>DisplayName</c>), its event callbacks, and what
/// says something about the value currently in one field alone (<c>Invalid</c>, <c>ErrorMessage</c>,
/// <c>ErrorMessageTemplate</c>).
/// <br />
/// <c>InputHtmlAttributes</c> and <c>NoValidate</c> are left out for the reasons
/// <see cref="BitInputBaseParams{TValue}"/> gives: the first is a dictionary the field writes into, and the second is
/// read while the parameters are still being set, before any cascade has been applied.
/// </remarks>
public class BitTimeZonePickerParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitTimeZonePicker{TValue}"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitTimeZonePicker value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitTimeZonePicker<object>)}";



    public string Name => ParamName;



    /// <summary>
    /// The color kind of the fill of the time zone picker.
    /// </summary>
    public BitColorKind? Background { get; set; }

    /// <summary>
    /// The color kind of the frame of the time zone picker, winning over the main color of <see cref="Color"/>.
    /// </summary>
    public BitColorKind? Border { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitTimeZonePicker.
    /// </summary>
    public BitTimeZonePickerClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the time zone picker.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// The description shown under the time zone picker.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The custom template for the description of the time zone picker.
    /// </summary>
    public RenderFragment? DescriptionTemplate { get; set; }

    /// <summary>
    /// Preselects the time zone of the user's device, read from the browser, when the picker has no value.
    /// </summary>
    public bool? DetectTimeZone { get; set; }

    /// <summary>
    /// Determines the allowed drop directions of the time zone list callout.
    /// </summary>
    public BitDropDirection? DropDirection { get; set; }

    /// <summary>
    /// The custom template for each time zone of the list.
    /// </summary>
    public RenderFragment<BitTimeZone>? ItemTemplate { get; set; }

    /// <summary>
    /// The label of the time zone picker shown above the field.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// The custom template for the label of the time zone picker.
    /// </summary>
    public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// The maximum height of the time zone list callout in pixels.
    /// </summary>
    public int? MaxHeight { get; set; }

    /// <summary>
    /// Removes the border of the time zone picker.
    /// </summary>
    public bool? NoBorder { get; set; }

    /// <summary>
    /// Hides the names of the countries the time zones are in.
    /// </summary>
    public bool? NoCountry { get; set; }

    /// <summary>
    /// Hides the offsets from UTC of the time zones in the field and in the list.
    /// </summary>
    public bool? NoOffset { get; set; }

    /// <summary>
    /// The message to show, and to announce, when the search result of the time zone list is empty.
    /// </summary>
    public string? NoResultsMessage { get; set; }

    /// <summary>
    /// The custom template to show when the search result of the time zone list is empty.
    /// </summary>
    public RenderFragment? NoResultsTemplate { get; set; }

    /// <summary>
    /// Hides the search box of the time zone list.
    /// </summary>
    public bool? NoSearchBox { get; set; }

    /// <summary>
    /// Stops the keyboard navigation of the time zone list from wrapping around at its two ends.
    /// </summary>
    public bool? NoWrapNavigation { get; set; }

    /// <summary>
    /// The text shown in the field while no time zone is selected.
    /// </summary>
    public string? Placeholder { get; set; }

    /// <summary>
    /// The time zones to pin to the top of the list, by any id <see cref="BitTimeZones.Find(string)"/> resolves.
    /// </summary>
    public ICollection<string>? PreferredTimeZones { get; set; }

    /// <summary>
    /// Makes the time zone picker read-only.
    /// </summary>
    public bool? ReadOnly { get; set; }

    /// <summary>
    /// The moment the offsets of the time zones are computed for, which is now unless it is given.
    /// </summary>
    public DateTimeOffset? ReferenceDate { get; set; }

    /// <summary>
    /// Makes the time zone picker required.
    /// </summary>
    public bool? Required { get; set; }

    /// <summary>
    /// Shows the time zone list as a full height panel on small screens instead of an inline callout.
    /// </summary>
    public bool? Responsive { get; set; }

    /// <summary>
    /// The aria-label of the close button of the responsive panel of the time zone list.
    /// </summary>
    public string? ResponsiveCloseButtonAriaLabel { get; set; }

    /// <summary>
    /// The icon of the close button of the responsive panel, from an external icon library.
    /// </summary>
    public BitIconInfo? ResponsiveCloseIcon { get; set; }

    /// <summary>
    /// The icon name of the close button of the responsive panel from the Fluent UI icon set.
    /// </summary>
    public string? ResponsiveCloseIconName { get; set; }

    /// <summary>
    /// The aria-label of the search box of the time zone list.
    /// </summary>
    public string? SearchBoxAriaLabel { get; set; }

    /// <summary>
    /// The placeholder text of the search box of the time zone list.
    /// </summary>
    public string? SearchBoxPlaceholder { get; set; }

    /// <summary>
    /// What a screen reader announces for the number of time zones a search term leaves in the list.
    /// </summary>
    public Func<int, string?>? SearchResultsAnnouncement { get; set; }

    /// <summary>
    /// The size of the time zone picker.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitTimeZonePicker.
    /// </summary>
    public BitTimeZonePickerClassStyles? Styles { get; set; }

    /// <summary>
    /// The list of the time zones the picker offers.
    /// </summary>
    public ICollection<BitTimeZone>? TimeZones { get; set; }

    /// <summary>
    /// The tooltip (title attribute) of the time zone picker.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Renders the time zone picker with only a bottom border instead of a full one.
    /// </summary>
    public bool? Underlined { get; set; }

    /// <summary>
    /// The custom template for the content of the field, receiving the selected time zone.
    /// </summary>
    public RenderFragment<BitTimeZone?>? ValueTemplate { get; set; }


    /// <summary>
    /// Updates the properties of the specified <see cref="BitTimeZonePicker{TValue}"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitTimeZonePicker{TValue}"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitTimeZonePicker"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitTimeZonePicker"/>.
    /// </remarks>
    /// <param name="bitTimeZonePicker">
    /// The <see cref="BitTimeZonePicker{TValue}"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters<TValue>(BitTimeZonePicker<TValue> bitTimeZonePicker)
    {
        if (bitTimeZonePicker is null) return;

        UpdateBaseParameters(bitTimeZonePicker);

        if (Background.HasValue)
        {
            bitTimeZonePicker.TakeFromCascade(nameof(Background), Background.Value, static p => p.Background, static (p, v) => p.Background = v);
        }

        if (Border.HasValue)
        {
            bitTimeZonePicker.TakeFromCascade(nameof(Border), Border.Value, static p => p.Border, static (p, v) => p.Border = v);
        }

        if (Classes is not null)
        {
            bitTimeZonePicker.TakeFromCascade(nameof(Classes), Classes, static p => p.Classes, static (p, v) => p.Classes = v);
        }

        if (Color.HasValue)
        {
            bitTimeZonePicker.TakeFromCascade(nameof(Color), Color.Value, static p => p.Color, static (p, v) => p.Color = v);
        }

        if (Description.HasValue())
        {
            bitTimeZonePicker.TakeFromCascade(nameof(Description), Description, static p => p.Description, static (p, v) => p.Description = v);
        }

        if (DescriptionTemplate is not null)
        {
            bitTimeZonePicker.TakeFromCascade(nameof(DescriptionTemplate), DescriptionTemplate, static p => p.DescriptionTemplate, static (p, v) => p.DescriptionTemplate = v);
        }

        if (DetectTimeZone.HasValue)
        {
            bitTimeZonePicker.TakeFromCascade(nameof(DetectTimeZone), DetectTimeZone.Value, static p => p.DetectTimeZone, static (p, v) => p.DetectTimeZone = v);
        }

        if (DropDirection.HasValue)
        {
            bitTimeZonePicker.TakeFromCascade(nameof(DropDirection), DropDirection.Value, static p => p.DropDirection, static (p, v) => p.DropDirection = v);
        }

        if (ItemTemplate is not null)
        {
            bitTimeZonePicker.TakeFromCascade(nameof(ItemTemplate), ItemTemplate, static p => p.ItemTemplate, static (p, v) => p.ItemTemplate = v);
        }

        if (Label.HasValue())
        {
            bitTimeZonePicker.TakeFromCascade(nameof(Label), Label, static p => p.Label, static (p, v) => p.Label = v);
        }

        if (LabelTemplate is not null)
        {
            bitTimeZonePicker.TakeFromCascade(nameof(LabelTemplate), LabelTemplate, static p => p.LabelTemplate, static (p, v) => p.LabelTemplate = v);
        }

        if (MaxHeight.HasValue)
        {
            bitTimeZonePicker.TakeFromCascade(nameof(MaxHeight), MaxHeight.Value, static p => p.MaxHeight, static (p, v) => p.MaxHeight = v);
        }

        if (NoBorder.HasValue)
        {
            bitTimeZonePicker.TakeFromCascade(nameof(NoBorder), NoBorder.Value, static p => p.NoBorder, static (p, v) => p.NoBorder = v);
        }

        if (NoCountry.HasValue)
        {
            bitTimeZonePicker.TakeFromCascade(nameof(NoCountry), NoCountry.Value, static p => p.NoCountry, static (p, v) => p.NoCountry = v);
        }

        if (NoOffset.HasValue)
        {
            bitTimeZonePicker.TakeFromCascade(nameof(NoOffset), NoOffset.Value, static p => p.NoOffset, static (p, v) => p.NoOffset = v);
        }

        if (NoResultsMessage.HasValue())
        {
            bitTimeZonePicker.TakeFromCascade(nameof(NoResultsMessage), NoResultsMessage, static p => p.NoResultsMessage, static (p, v) => p.NoResultsMessage = v);
        }

        if (NoResultsTemplate is not null)
        {
            bitTimeZonePicker.TakeFromCascade(nameof(NoResultsTemplate), NoResultsTemplate, static p => p.NoResultsTemplate, static (p, v) => p.NoResultsTemplate = v);
        }

        if (NoSearchBox.HasValue)
        {
            bitTimeZonePicker.TakeFromCascade(nameof(NoSearchBox), NoSearchBox.Value, static p => p.NoSearchBox, static (p, v) => p.NoSearchBox = v);
        }

        if (NoWrapNavigation.HasValue)
        {
            bitTimeZonePicker.TakeFromCascade(nameof(NoWrapNavigation), NoWrapNavigation.Value, static p => p.NoWrapNavigation, static (p, v) => p.NoWrapNavigation = v);
        }

        if (Placeholder.HasValue())
        {
            bitTimeZonePicker.TakeFromCascade(nameof(Placeholder), Placeholder, static p => p.Placeholder, static (p, v) => p.Placeholder = v);
        }

        if (PreferredTimeZones is not null)
        {
            bitTimeZonePicker.TakeFromCascade(nameof(PreferredTimeZones), PreferredTimeZones, static p => p.PreferredTimeZones, static (p, v) => p.PreferredTimeZones = v);
        }

        if (ReadOnly.HasValue)
        {
            bitTimeZonePicker.TakeFromCascade(nameof(ReadOnly), ReadOnly.Value, static p => p.ReadOnly, static (p, v) => p.ReadOnly = v);
        }

        if (ReferenceDate.HasValue)
        {
            bitTimeZonePicker.TakeFromCascade(nameof(ReferenceDate), ReferenceDate.Value, static p => p.ReferenceDate, static (p, v) => p.ReferenceDate = v);
        }

        if (Required.HasValue)
        {
            bitTimeZonePicker.TakeFromCascade(nameof(Required), Required.Value, static p => p.Required, static (p, v) => p.Required = v);
        }

        if (Responsive.HasValue)
        {
            bitTimeZonePicker.TakeFromCascade(nameof(Responsive), Responsive.Value, static p => p.Responsive, static (p, v) => p.Responsive = v);
        }

        if (ResponsiveCloseButtonAriaLabel.HasValue())
        {
            bitTimeZonePicker.TakeFromCascade(nameof(ResponsiveCloseButtonAriaLabel), ResponsiveCloseButtonAriaLabel, static p => p.ResponsiveCloseButtonAriaLabel, static (p, v) => p.ResponsiveCloseButtonAriaLabel = v);
        }

        if (ResponsiveCloseIcon is not null)
        {
            bitTimeZonePicker.TakeFromCascade(nameof(ResponsiveCloseIcon), ResponsiveCloseIcon, static p => p.ResponsiveCloseIcon, static (p, v) => p.ResponsiveCloseIcon = v);
        }

        if (ResponsiveCloseIconName.HasValue())
        {
            bitTimeZonePicker.TakeFromCascade(nameof(ResponsiveCloseIconName), ResponsiveCloseIconName, static p => p.ResponsiveCloseIconName, static (p, v) => p.ResponsiveCloseIconName = v);
        }

        if (SearchBoxAriaLabel.HasValue())
        {
            bitTimeZonePicker.TakeFromCascade(nameof(SearchBoxAriaLabel), SearchBoxAriaLabel, static p => p.SearchBoxAriaLabel, static (p, v) => p.SearchBoxAriaLabel = v);
        }

        if (SearchBoxPlaceholder.HasValue())
        {
            bitTimeZonePicker.TakeFromCascade(nameof(SearchBoxPlaceholder), SearchBoxPlaceholder, static p => p.SearchBoxPlaceholder, static (p, v) => p.SearchBoxPlaceholder = v);
        }

        if (SearchResultsAnnouncement is not null)
        {
            bitTimeZonePicker.TakeFromCascade(nameof(SearchResultsAnnouncement), SearchResultsAnnouncement, static p => p.SearchResultsAnnouncement, static (p, v) => p.SearchResultsAnnouncement = v);
        }

        if (Size.HasValue)
        {
            bitTimeZonePicker.TakeFromCascade(nameof(Size), Size.Value, static p => p.Size, static (p, v) => p.Size = v);
        }

        if (Styles is not null)
        {
            bitTimeZonePicker.TakeFromCascade(nameof(Styles), Styles, static p => p.Styles, static (p, v) => p.Styles = v);
        }

        if (TimeZones is not null)
        {
            bitTimeZonePicker.TakeFromCascade(nameof(TimeZones), TimeZones, static p => p.TimeZones, static (p, v) => p.TimeZones = v);
        }

        if (Title.HasValue())
        {
            bitTimeZonePicker.TakeFromCascade(nameof(Title), Title, static p => p.Title, static (p, v) => p.Title = v);
        }

        if (Underlined.HasValue)
        {
            bitTimeZonePicker.TakeFromCascade(nameof(Underlined), Underlined.Value, static p => p.Underlined, static (p, v) => p.Underlined = v);
        }

        if (ValueTemplate is not null)
        {
            bitTimeZonePicker.TakeFromCascade(nameof(ValueTemplate), ValueTemplate, static p => p.ValueTemplate, static (p, v) => p.ValueTemplate = v);
        }
    }
}
