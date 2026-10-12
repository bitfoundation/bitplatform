namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitCountryPicker"/> component.
/// </summary>
/// <remarks>
/// What it carries is a default and not an override: a country picker that writes a parameter for itself keeps its
/// own value, and only what it left unset is filled in from the cascade.
/// <br />
/// Three groups of parameters are deliberately left out of it, because a value shared between fields would be
/// wrong rather than merely unused: what identifies a field and carries its value (<c>Value</c>,
/// <c>DefaultValue</c>, <c>IsOpen</c>, <c>Name</c>, <c>DisplayName</c>), its event callbacks, and what says something
/// about the value currently in one field alone (<c>Invalid</c>, <c>ErrorMessage</c>, <c>ErrorMessageTemplate</c>).
/// <br />
/// <c>InputHtmlAttributes</c> and <c>NoValidate</c> are left out for the reasons
/// <see cref="BitInputBaseParams{TValue}"/> gives: the first is a dictionary the field writes into, and the second
/// is read while the parameters are still being set, before any cascade has been applied.
/// </remarks>
public class BitCountryPickerParams : BitInputBaseParams<BitCountry?>, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitCountryPicker"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitCountryPicker value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitCountryPicker)}";



    public string Name => ParamName;



    /// <summary>
    /// The color kind of the fill of the country picker.
    /// </summary>
    public BitColorKind? Background { get; set; }

    /// <summary>
    /// The color kind of the frame of the country picker, winning over the main color of <see cref="Color"/>.
    /// </summary>
    public BitColorKind? Border { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitCountryPicker.
    /// </summary>
    public BitCountryPickerClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the country picker.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// The list of the countries the picker offers.
    /// </summary>
    public ICollection<BitCountry>? Countries { get; set; }

    /// <summary>
    /// The country an unbound picker starts on when no DefaultValue is given - the market of the app.
    /// </summary>
    public BitCountry? DefaultCountry { get; set; }

    /// <summary>
    /// The description shown under the country picker.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The custom template for the description of the country picker.
    /// </summary>
    public RenderFragment? DescriptionTemplate { get; set; }

    /// <summary>
    /// Determines the allowed drop directions of the country list callout.
    /// </summary>
    public BitDropDirection? DropDirection { get; set; }

    /// <summary>
    /// The countries to leave out of the list.
    /// </summary>
    public ICollection<BitCountry>? ExcludeCountries { get; set; }

    /// <summary>
    /// The url of the flag image of a country, replacing the flags that ship with the library.
    /// </summary>
    public Func<BitCountry, string?>? FlagUrlSelector { get; set; }

    /// <summary>
    /// The custom template for each country of the list.
    /// </summary>
    public RenderFragment<BitCountry>? ItemTemplate { get; set; }

    /// <summary>
    /// The label of the country picker shown above the field.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// The custom template for the label of the country picker.
    /// </summary>
    public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// The maximum height of the country list callout in pixels.
    /// </summary>
    public int? MaxHeight { get; set; }

    /// <summary>
    /// Removes the border of the country picker.
    /// </summary>
    public bool? NoBorder { get; set; }

    /// <summary>
    /// Hides the flag images of the countries in the field and in the list.
    /// </summary>
    public bool? NoFlags { get; set; }

    /// <summary>
    /// The message to show, and to announce, when the search result of the country list is empty.
    /// </summary>
    public string? NoResultsMessage { get; set; }

    /// <summary>
    /// The custom template to show when the search result of the country list is empty.
    /// </summary>
    public RenderFragment? NoResultsTemplate { get; set; }

    /// <summary>
    /// Hides the search box of the country list.
    /// </summary>
    public bool? NoSearchBox { get; set; }

    /// <summary>
    /// Stops the keyboard navigation of the country list from wrapping around at its two ends.
    /// </summary>
    public bool? NoWrapNavigation { get; set; }

    /// <summary>
    /// The text shown in the field while no country is selected.
    /// </summary>
    public string? Placeholder { get; set; }

    /// <summary>
    /// The countries to pin to the top of the list.
    /// </summary>
    public ICollection<BitCountry>? PreferredCountries { get; set; }

    /// <summary>
    /// Shows the country list as a full height panel on small screens instead of an inline callout.
    /// </summary>
    public bool? Responsive { get; set; }

    /// <summary>
    /// The aria-label of the close button of the responsive panel of the country list.
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
    /// The aria-label of the search box of the country list.
    /// </summary>
    public string? SearchBoxAriaLabel { get; set; }

    /// <summary>
    /// The placeholder text of the search box of the country list.
    /// </summary>
    public string? SearchBoxPlaceholder { get; set; }

    /// <summary>
    /// What a screen reader announces for the number of countries a search term leaves in the list.
    /// </summary>
    public Func<int, string?>? SearchResultsAnnouncement { get; set; }

    /// <summary>
    /// The size of the country picker.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitCountryPicker.
    /// </summary>
    public BitCountryPickerClassStyles? Styles { get; set; }

    /// <summary>
    /// The tooltip (title attribute) of the country picker.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Renders the country picker with only a bottom border instead of a full one.
    /// </summary>
    public bool? Underlined { get; set; }

    /// <summary>
    /// The custom template for the content of the field, receiving the selected country.
    /// </summary>
    public RenderFragment<BitCountry?>? ValueTemplate { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitCountryPicker"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitCountryPicker"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitCountryPicker"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitCountryPicker"/>.
    /// </remarks>
    /// <param name="bitCountryPicker">
    /// The <see cref="BitCountryPicker"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitCountryPicker bitCountryPicker)
    {
        if (bitCountryPicker is null) return;

        UpdateInputBaseParameters(bitCountryPicker);

        if (Background.HasValue)
        {
            bitCountryPicker.TakeFromCascade(nameof(Background), Background.Value, static p => p.Background, static (p, v) => p.Background = v);
        }

        if (Border.HasValue)
        {
            bitCountryPicker.TakeFromCascade(nameof(Border), Border.Value, static p => p.Border, static (p, v) => p.Border = v);
        }

        if (Classes is not null)
        {
            bitCountryPicker.TakeFromCascade(nameof(Classes), Classes, static p => p.Classes, static (p, v) => p.Classes = v);
        }

        if (Color.HasValue)
        {
            bitCountryPicker.TakeFromCascade(nameof(Color), Color.Value, static p => p.Color, static (p, v) => p.Color = v);
        }

        if (Countries is not null)
        {
            bitCountryPicker.TakeFromCascade(nameof(Countries), Countries, static p => p.Countries, static (p, v) => p.Countries = v);
        }

        if (DefaultCountry is not null)
        {
            bitCountryPicker.TakeFromCascade(nameof(DefaultCountry), DefaultCountry, static p => p.DefaultCountry, static (p, v) => p.DefaultCountry = v);
        }

        if (Description.HasValue())
        {
            bitCountryPicker.TakeFromCascade(nameof(Description), Description, static p => p.Description, static (p, v) => p.Description = v);
        }

        if (DescriptionTemplate is not null)
        {
            bitCountryPicker.TakeFromCascade(nameof(DescriptionTemplate), DescriptionTemplate, static p => p.DescriptionTemplate, static (p, v) => p.DescriptionTemplate = v);
        }

        if (DropDirection.HasValue)
        {
            bitCountryPicker.TakeFromCascade(nameof(DropDirection), DropDirection.Value, static p => p.DropDirection, static (p, v) => p.DropDirection = v);
        }

        if (ExcludeCountries is not null)
        {
            bitCountryPicker.TakeFromCascade(nameof(ExcludeCountries), ExcludeCountries, static p => p.ExcludeCountries, static (p, v) => p.ExcludeCountries = v);
        }

        if (FlagUrlSelector is not null)
        {
            bitCountryPicker.TakeFromCascade(nameof(FlagUrlSelector), FlagUrlSelector, static p => p.FlagUrlSelector, static (p, v) => p.FlagUrlSelector = v);
        }

        if (ItemTemplate is not null)
        {
            bitCountryPicker.TakeFromCascade(nameof(ItemTemplate), ItemTemplate, static p => p.ItemTemplate, static (p, v) => p.ItemTemplate = v);
        }

        if (Label.HasValue())
        {
            bitCountryPicker.TakeFromCascade(nameof(Label), Label, static p => p.Label, static (p, v) => p.Label = v);
        }

        if (LabelTemplate is not null)
        {
            bitCountryPicker.TakeFromCascade(nameof(LabelTemplate), LabelTemplate, static p => p.LabelTemplate, static (p, v) => p.LabelTemplate = v);
        }

        if (MaxHeight.HasValue)
        {
            bitCountryPicker.TakeFromCascade(nameof(MaxHeight), MaxHeight.Value, static p => p.MaxHeight, static (p, v) => p.MaxHeight = v);
        }

        if (NoBorder.HasValue)
        {
            bitCountryPicker.TakeFromCascade(nameof(NoBorder), NoBorder.Value, static p => p.NoBorder, static (p, v) => p.NoBorder = v);
        }

        if (NoFlags.HasValue)
        {
            bitCountryPicker.TakeFromCascade(nameof(NoFlags), NoFlags.Value, static p => p.NoFlags, static (p, v) => p.NoFlags = v);
        }

        if (NoResultsMessage.HasValue())
        {
            bitCountryPicker.TakeFromCascade(nameof(NoResultsMessage), NoResultsMessage, static p => p.NoResultsMessage, static (p, v) => p.NoResultsMessage = v);
        }

        if (NoResultsTemplate is not null)
        {
            bitCountryPicker.TakeFromCascade(nameof(NoResultsTemplate), NoResultsTemplate, static p => p.NoResultsTemplate, static (p, v) => p.NoResultsTemplate = v);
        }

        if (NoSearchBox.HasValue)
        {
            bitCountryPicker.TakeFromCascade(nameof(NoSearchBox), NoSearchBox.Value, static p => p.NoSearchBox, static (p, v) => p.NoSearchBox = v);
        }

        if (NoWrapNavigation.HasValue)
        {
            bitCountryPicker.TakeFromCascade(nameof(NoWrapNavigation), NoWrapNavigation.Value, static p => p.NoWrapNavigation, static (p, v) => p.NoWrapNavigation = v);
        }

        if (Placeholder.HasValue())
        {
            bitCountryPicker.TakeFromCascade(nameof(Placeholder), Placeholder, static p => p.Placeholder, static (p, v) => p.Placeholder = v);
        }

        if (PreferredCountries is not null)
        {
            bitCountryPicker.TakeFromCascade(nameof(PreferredCountries), PreferredCountries, static p => p.PreferredCountries, static (p, v) => p.PreferredCountries = v);
        }

        if (Responsive.HasValue)
        {
            bitCountryPicker.TakeFromCascade(nameof(Responsive), Responsive.Value, static p => p.Responsive, static (p, v) => p.Responsive = v);
        }

        if (ResponsiveCloseButtonAriaLabel.HasValue())
        {
            bitCountryPicker.TakeFromCascade(nameof(ResponsiveCloseButtonAriaLabel), ResponsiveCloseButtonAriaLabel, static p => p.ResponsiveCloseButtonAriaLabel, static (p, v) => p.ResponsiveCloseButtonAriaLabel = v);
        }

        if (ResponsiveCloseIcon is not null)
        {
            bitCountryPicker.TakeFromCascade(nameof(ResponsiveCloseIcon), ResponsiveCloseIcon, static p => p.ResponsiveCloseIcon, static (p, v) => p.ResponsiveCloseIcon = v);
        }

        if (ResponsiveCloseIconName.HasValue())
        {
            bitCountryPicker.TakeFromCascade(nameof(ResponsiveCloseIconName), ResponsiveCloseIconName, static p => p.ResponsiveCloseIconName, static (p, v) => p.ResponsiveCloseIconName = v);
        }

        if (SearchBoxAriaLabel.HasValue())
        {
            bitCountryPicker.TakeFromCascade(nameof(SearchBoxAriaLabel), SearchBoxAriaLabel, static p => p.SearchBoxAriaLabel, static (p, v) => p.SearchBoxAriaLabel = v);
        }

        if (SearchBoxPlaceholder.HasValue())
        {
            bitCountryPicker.TakeFromCascade(nameof(SearchBoxPlaceholder), SearchBoxPlaceholder, static p => p.SearchBoxPlaceholder, static (p, v) => p.SearchBoxPlaceholder = v);
        }

        if (SearchResultsAnnouncement is not null)
        {
            bitCountryPicker.TakeFromCascade(nameof(SearchResultsAnnouncement), SearchResultsAnnouncement, static p => p.SearchResultsAnnouncement, static (p, v) => p.SearchResultsAnnouncement = v);
        }

        if (Size.HasValue)
        {
            bitCountryPicker.TakeFromCascade(nameof(Size), Size.Value, static p => p.Size, static (p, v) => p.Size = v);
        }

        if (Styles is not null)
        {
            bitCountryPicker.TakeFromCascade(nameof(Styles), Styles, static p => p.Styles, static (p, v) => p.Styles = v);
        }

        if (Title.HasValue())
        {
            bitCountryPicker.TakeFromCascade(nameof(Title), Title, static p => p.Title, static (p, v) => p.Title = v);
        }

        if (Underlined.HasValue)
        {
            bitCountryPicker.TakeFromCascade(nameof(Underlined), Underlined.Value, static p => p.Underlined, static (p, v) => p.Underlined = v);
        }

        if (ValueTemplate is not null)
        {
            bitCountryPicker.TakeFromCascade(nameof(ValueTemplate), ValueTemplate, static p => p.ValueTemplate, static (p, v) => p.ValueTemplate = v);
        }
    }
}
