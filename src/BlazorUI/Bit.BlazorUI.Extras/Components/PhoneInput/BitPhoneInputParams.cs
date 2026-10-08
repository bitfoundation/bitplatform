namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitPhoneInput"/> component.
/// </summary>
/// <remarks>
/// What it carries is a default and not an override: a phone input that writes a parameter for itself keeps its
/// own value, and only what it left unset is filled in from the cascade.
/// <br />
/// Three groups of parameters are deliberately left out of it, because a value shared between fields would be
/// wrong rather than merely unused: what identifies a field and carries its value (<c>Value</c>,
/// <c>DefaultValue</c>, <c>Number</c>, <c>Country</c>, <c>IsOpen</c>, <c>Name</c>, <c>CountryName</c>,
/// <c>DisplayName</c>), its event callbacks, and what says something about the value currently in one field alone
/// (<c>Invalid</c>, <c>ErrorMessage</c>, <c>ErrorMessageTemplate</c>, <c>AutoFocus</c>).
/// <br />
/// <c>InputHtmlAttributes</c> and <c>NoValidate</c> are left out for the reasons
/// <see cref="BitInputBaseParams{TValue}"/> gives: the first is a dictionary the field writes into, and the second
/// is read while the parameters are still being set, before any cascade has been applied.
/// </remarks>
public class BitPhoneInputParams : BitInputBaseParams<string?>, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitPhoneInput"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitPhoneInput value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitPhoneInput)}";



    public string Name => ParamName;



    /// <summary>
    /// Specifies the value of the autocomplete attribute of the number input.
    /// </summary>
    public string? AutoComplete { get; set; }

    /// <summary>
    /// Shows the pattern the number is formatted with as the placeholder of the number input.
    /// </summary>
    public bool? AutoPlaceholder { get; set; }

    /// <summary>
    /// The color kind of the fill of the phone input.
    /// </summary>
    public BitColorKind? Background { get; set; }

    /// <summary>
    /// The color kind of the frame of the phone input, winning over the main color of <see cref="Color"/>.
    /// </summary>
    public BitColorKind? Border { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitPhoneInput.
    /// </summary>
    public BitPhoneInputClassStyles? Classes { get; set; }

    /// <summary>
    /// The aria-label of the clear button of the number input.
    /// </summary>
    public string? ClearButtonAriaLabel { get; set; }

    /// <summary>
    /// The icon of the clear button of the number input, from an external icon library.
    /// </summary>
    public BitIconInfo? ClearButtonIcon { get; set; }

    /// <summary>
    /// The icon name of the clear button of the number input from the Fluent UI icon set.
    /// </summary>
    public string? ClearButtonIconName { get; set; }

    /// <summary>
    /// The custom template for the clear button of the number input.
    /// </summary>
    public RenderFragment? ClearButtonTemplate { get; set; }

    /// <summary>
    /// What a screen reader announces once the number has been emptied, in place of the default "Cleared".
    /// An empty string keeps the clearing from being announced at all.
    /// </summary>
    public string? ClearedAnnouncement { get; set; }

    /// <summary>
    /// The general color of the phone input.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// The list of the countries to show in the country dropdown.
    /// </summary>
    public ICollection<BitCountry>? Countries { get; set; }

    /// <summary>
    /// The delay in milliseconds a value waits for the typing to pause before it is reported, when Immediate is set.
    /// </summary>
    public int? DebounceTime { get; set; }

    /// <summary>
    /// The country a phone input starts on when its Country parameter is not set - the market of the app.
    /// </summary>
    public BitCountry? DefaultCountry { get; set; }

    /// <summary>
    /// The description shown under the phone input.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The custom template for the description of the phone input.
    /// </summary>
    public RenderFragment? DescriptionTemplate { get; set; }

    /// <summary>
    /// Determines the allowed drop directions of the country dropdown callout.
    /// </summary>
    public BitDropDirection? DropDirection { get; set; }

    /// <summary>
    /// The accessible name of the country selector, which the name of the selected country is appended to.
    /// </summary>
    public string? DropdownAriaLabel { get; set; }

    /// <summary>
    /// The placeholder text of the country dropdown when no country is selected.
    /// </summary>
    public string? DropdownPlaceholder { get; set; }

    /// <summary>
    /// The custom template for the content of the country dropdown button.
    /// </summary>
    public RenderFragment<BitCountry?>? DropdownTemplate { get; set; }

    /// <summary>
    /// Sets the enterkeyhint html attribute of the number input.
    /// </summary>
    public string? EnterKeyHint { get; set; }

    /// <summary>
    /// The countries to leave out of the country dropdown and of the dialing-code lookup.
    /// </summary>
    public ICollection<BitCountry>? ExcludeCountries { get; set; }

    /// <summary>
    /// The url of the flag image of a country, replacing the flags that ship with the library.
    /// </summary>
    public Func<BitCountry, string?>? FlagUrlSelector { get; set; }

    /// <summary>
    /// Renders the phone input to fill 100% of its container width.
    /// </summary>
    public bool? FullWidth { get; set; }

    /// <summary>
    /// Changes the value of the phone input on every keystroke (the oninput event) instead of on the change event.
    /// </summary>
    public bool? Immediate { get; set; }

    /// <summary>
    /// Sets the inputmode html attribute of the number input.
    /// </summary>
    public BitInputMode? InputMode { get; set; }

    /// <summary>
    /// The custom template for each country of the dropdown list.
    /// </summary>
    public RenderFragment<BitCountry>? ItemTemplate { get; set; }

    /// <summary>
    /// Keeps the national (trunk) prefix of a number in the composed value instead of dropping it.
    /// </summary>
    public bool? KeepNationalPrefix { get; set; }

    /// <summary>
    /// The label of the phone input shown above the field.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// The custom template for the label of the phone input.
    /// </summary>
    public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// The pattern the local number is formatted with as the user types, where every '#' is a digit slot.
    /// </summary>
    public string? Mask { get; set; }

    /// <summary>
    /// The pattern to format the local number with for a given country, taking precedence over <see cref="Mask"/>.
    /// </summary>
    public Func<BitCountry?, string?>? MaskSelector { get; set; }

    /// <summary>
    /// The maximum height of the country dropdown callout in pixels.
    /// </summary>
    public int? MaxHeight { get; set; }

    /// <summary>
    /// Determines the maximum number of characters allowed in the number input.
    /// </summary>
    public int? MaxLength { get; set; }

    /// <summary>
    /// Removes the border of the phone input.
    /// </summary>
    public bool? NoBorder { get; set; }

    /// <summary>
    /// Hides the dialing code of the selected country in the country dropdown button.
    /// </summary>
    public bool? NoDialCode { get; set; }

    /// <summary>
    /// Removes the country dropdown, so the country can only be set through the parameters.
    /// </summary>
    public bool? NoDropdown { get; set; }

    /// <summary>
    /// Hides the flag images of the countries in the dropdown button and in the dropdown list.
    /// </summary>
    public bool? NoFlags { get; set; }

    /// <summary>
    /// Stops the focus from moving to the number input once a country has been picked.
    /// </summary>
    public bool? NoFocusOnSelect { get; set; }

    /// <summary>
    /// The message to show, and to announce, when the search result of the country dropdown is empty.
    /// </summary>
    public string? NoResultsMessage { get; set; }

    /// <summary>
    /// The custom template to show when the search result of the country dropdown is empty.
    /// </summary>
    public RenderFragment? NoResultsTemplate { get; set; }

    /// <summary>
    /// Hides the search box of the country dropdown.
    /// </summary>
    public bool? NoSearchBox { get; set; }

    /// <summary>
    /// Stops the keyboard navigation of the country list from wrapping around at its two ends.
    /// </summary>
    public bool? NoWrapNavigation { get; set; }

    /// <summary>
    /// The placeholder text of the number input.
    /// </summary>
    public string? Placeholder { get; set; }

    /// <summary>
    /// The countries to pin to the top of the country dropdown list.
    /// </summary>
    public ICollection<BitCountry>? PreferredCountries { get; set; }

    /// <summary>
    /// Shows the country dropdown as a full height panel on small screens instead of an inline callout.
    /// </summary>
    public bool? Responsive { get; set; }

    /// <summary>
    /// The aria-label of the close button of the responsive panel of the country dropdown.
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
    /// The aria-label of the search box of the country dropdown.
    /// </summary>
    public string? SearchBoxAriaLabel { get; set; }

    /// <summary>
    /// The placeholder text of the search box of the country dropdown.
    /// </summary>
    public string? SearchBoxPlaceholder { get; set; }

    /// <summary>
    /// What a screen reader announces for the number of countries a search term leaves in the list.
    /// </summary>
    public Func<int, string?>? SearchResultsAnnouncement { get; set; }

    /// <summary>
    /// Shows a clear button in the number input while it holds a value.
    /// </summary>
    public bool? ShowClearButton { get; set; }

    /// <summary>
    /// The size of the phone input.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// Discards every character typed or pasted into the number input that cannot be part of a phone number.
    /// </summary>
    public bool? Strict { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitPhoneInput.
    /// </summary>
    public BitPhoneInputClassStyles? Styles { get; set; }

    /// <summary>
    /// The minimum time in milliseconds between two values reported while typing, when Immediate is set.
    /// </summary>
    public int? ThrottleTime { get; set; }

    /// <summary>
    /// The tooltip (title attribute) of the phone input.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Renders the phone input with only a bottom border instead of a full one.
    /// </summary>
    public bool? Underlined { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitPhoneInput"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitPhoneInput"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitPhoneInput"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitPhoneInput"/>.
    /// </remarks>
    /// <param name="bitPhoneInput">
    /// The <see cref="BitPhoneInput"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitPhoneInput bitPhoneInput)
    {
        if (bitPhoneInput is null) return;

        UpdateInputBaseParameters(bitPhoneInput);

        if (AutoComplete.HasValue())
        {
            bitPhoneInput.TakeFromCascade(nameof(AutoComplete), AutoComplete, static p => p.AutoComplete, static (p, v) => p.AutoComplete = v);
        }

        if (AutoPlaceholder.HasValue)
        {
            bitPhoneInput.TakeFromCascade(nameof(AutoPlaceholder), AutoPlaceholder.Value, static p => p.AutoPlaceholder, static (p, v) => p.AutoPlaceholder = v);
        }

        if (Background.HasValue)
        {
            bitPhoneInput.TakeFromCascade(nameof(Background), Background.Value, static p => p.Background, static (p, v) => p.Background = v);
        }

        if (Border.HasValue)
        {
            bitPhoneInput.TakeFromCascade(nameof(Border), Border.Value, static p => p.Border, static (p, v) => p.Border = v);
        }

        if (Classes is not null)
        {
            bitPhoneInput.TakeFromCascade(nameof(Classes), Classes, static p => p.Classes, static (p, v) => p.Classes = v);
        }

        if (ClearButtonAriaLabel.HasValue())
        {
            bitPhoneInput.TakeFromCascade(nameof(ClearButtonAriaLabel), ClearButtonAriaLabel, static p => p.ClearButtonAriaLabel, static (p, v) => p.ClearButtonAriaLabel = v);
        }

        if (ClearButtonIcon is not null)
        {
            bitPhoneInput.TakeFromCascade(nameof(ClearButtonIcon), ClearButtonIcon, static p => p.ClearButtonIcon, static (p, v) => p.ClearButtonIcon = v);
        }

        if (ClearButtonIconName.HasValue())
        {
            bitPhoneInput.TakeFromCascade(nameof(ClearButtonIconName), ClearButtonIconName, static p => p.ClearButtonIconName, static (p, v) => p.ClearButtonIconName = v);
        }

        if (ClearButtonTemplate is not null)
        {
            bitPhoneInput.TakeFromCascade(nameof(ClearButtonTemplate), ClearButtonTemplate, static p => p.ClearButtonTemplate, static (p, v) => p.ClearButtonTemplate = v);
        }

        // Null rather than empty is what leaves the field alone: an empty announcement is how silence is asked for.
        if (ClearedAnnouncement is not null)
        {
            bitPhoneInput.TakeFromCascade(nameof(ClearedAnnouncement), ClearedAnnouncement, static p => p.ClearedAnnouncement, static (p, v) => p.ClearedAnnouncement = v);
        }

        if (Color.HasValue)
        {
            bitPhoneInput.TakeFromCascade(nameof(Color), Color.Value, static p => p.Color, static (p, v) => p.Color = v);
        }

        if (Countries is not null)
        {
            bitPhoneInput.TakeFromCascade(nameof(Countries), Countries, static p => p.Countries, static (p, v) => p.Countries = v);
        }

        if (DebounceTime.HasValue)
        {
            bitPhoneInput.TakeFromCascade(nameof(DebounceTime), DebounceTime.Value, static p => p.DebounceTime, static (p, v) => p.DebounceTime = v);
        }

        if (DefaultCountry is not null)
        {
            bitPhoneInput.TakeFromCascade(nameof(DefaultCountry), DefaultCountry, static p => p.DefaultCountry, static (p, v) => p.DefaultCountry = v);
        }

        if (Description.HasValue())
        {
            bitPhoneInput.TakeFromCascade(nameof(Description), Description, static p => p.Description, static (p, v) => p.Description = v);
        }

        if (DescriptionTemplate is not null)
        {
            bitPhoneInput.TakeFromCascade(nameof(DescriptionTemplate), DescriptionTemplate, static p => p.DescriptionTemplate, static (p, v) => p.DescriptionTemplate = v);
        }

        if (DropDirection.HasValue)
        {
            bitPhoneInput.TakeFromCascade(nameof(DropDirection), DropDirection.Value, static p => p.DropDirection, static (p, v) => p.DropDirection = v);
        }

        if (DropdownAriaLabel.HasValue())
        {
            bitPhoneInput.TakeFromCascade(nameof(DropdownAriaLabel), DropdownAriaLabel, static p => p.DropdownAriaLabel, static (p, v) => p.DropdownAriaLabel = v);
        }

        if (DropdownPlaceholder.HasValue())
        {
            bitPhoneInput.TakeFromCascade(nameof(DropdownPlaceholder), DropdownPlaceholder, static p => p.DropdownPlaceholder, static (p, v) => p.DropdownPlaceholder = v);
        }

        if (DropdownTemplate is not null)
        {
            bitPhoneInput.TakeFromCascade(nameof(DropdownTemplate), DropdownTemplate, static p => p.DropdownTemplate, static (p, v) => p.DropdownTemplate = v);
        }

        if (EnterKeyHint.HasValue())
        {
            bitPhoneInput.TakeFromCascade(nameof(EnterKeyHint), EnterKeyHint, static p => p.EnterKeyHint, static (p, v) => p.EnterKeyHint = v);
        }

        if (ExcludeCountries is not null)
        {
            bitPhoneInput.TakeFromCascade(nameof(ExcludeCountries), ExcludeCountries, static p => p.ExcludeCountries, static (p, v) => p.ExcludeCountries = v);
        }

        if (FlagUrlSelector is not null)
        {
            bitPhoneInput.TakeFromCascade(nameof(FlagUrlSelector), FlagUrlSelector, static p => p.FlagUrlSelector, static (p, v) => p.FlagUrlSelector = v);
        }

        if (FullWidth.HasValue)
        {
            bitPhoneInput.TakeFromCascade(nameof(FullWidth), FullWidth.Value, static p => p.FullWidth, static (p, v) => p.FullWidth = v);
        }

        if (Immediate.HasValue)
        {
            bitPhoneInput.TakeFromCascade(nameof(Immediate), Immediate.Value, static p => p.Immediate, static (p, v) => p.Immediate = v);
        }

        if (InputMode.HasValue)
        {
            bitPhoneInput.TakeFromCascade(nameof(InputMode), InputMode.Value, static p => p.InputMode, static (p, v) => p.InputMode = v);
        }

        if (ItemTemplate is not null)
        {
            bitPhoneInput.TakeFromCascade(nameof(ItemTemplate), ItemTemplate, static p => p.ItemTemplate, static (p, v) => p.ItemTemplate = v);
        }

        if (KeepNationalPrefix.HasValue)
        {
            bitPhoneInput.TakeFromCascade(nameof(KeepNationalPrefix), KeepNationalPrefix.Value, static p => p.KeepNationalPrefix, static (p, v) => p.KeepNationalPrefix = v);
        }

        if (Label.HasValue())
        {
            bitPhoneInput.TakeFromCascade(nameof(Label), Label, static p => p.Label, static (p, v) => p.Label = v);
        }

        if (LabelTemplate is not null)
        {
            bitPhoneInput.TakeFromCascade(nameof(LabelTemplate), LabelTemplate, static p => p.LabelTemplate, static (p, v) => p.LabelTemplate = v);
        }

        if (Mask.HasValue())
        {
            bitPhoneInput.TakeFromCascade(nameof(Mask), Mask, static p => p.Mask, static (p, v) => p.Mask = v);
        }

        if (MaskSelector is not null)
        {
            bitPhoneInput.TakeFromCascade(nameof(MaskSelector), MaskSelector, static p => p.MaskSelector, static (p, v) => p.MaskSelector = v);
        }

        if (MaxHeight.HasValue)
        {
            bitPhoneInput.TakeFromCascade(nameof(MaxHeight), MaxHeight.Value, static p => p.MaxHeight, static (p, v) => p.MaxHeight = v);
        }

        if (MaxLength.HasValue)
        {
            bitPhoneInput.TakeFromCascade(nameof(MaxLength), MaxLength.Value, static p => p.MaxLength, static (p, v) => p.MaxLength = v);
        }

        if (NoBorder.HasValue)
        {
            bitPhoneInput.TakeFromCascade(nameof(NoBorder), NoBorder.Value, static p => p.NoBorder, static (p, v) => p.NoBorder = v);
        }

        if (NoDialCode.HasValue)
        {
            bitPhoneInput.TakeFromCascade(nameof(NoDialCode), NoDialCode.Value, static p => p.NoDialCode, static (p, v) => p.NoDialCode = v);
        }

        if (NoDropdown.HasValue)
        {
            bitPhoneInput.TakeFromCascade(nameof(NoDropdown), NoDropdown.Value, static p => p.NoDropdown, static (p, v) => p.NoDropdown = v);
        }

        if (NoFlags.HasValue)
        {
            bitPhoneInput.TakeFromCascade(nameof(NoFlags), NoFlags.Value, static p => p.NoFlags, static (p, v) => p.NoFlags = v);
        }

        if (NoFocusOnSelect.HasValue)
        {
            bitPhoneInput.TakeFromCascade(nameof(NoFocusOnSelect), NoFocusOnSelect.Value, static p => p.NoFocusOnSelect, static (p, v) => p.NoFocusOnSelect = v);
        }

        if (NoResultsMessage.HasValue())
        {
            bitPhoneInput.TakeFromCascade(nameof(NoResultsMessage), NoResultsMessage, static p => p.NoResultsMessage, static (p, v) => p.NoResultsMessage = v);
        }

        if (NoResultsTemplate is not null)
        {
            bitPhoneInput.TakeFromCascade(nameof(NoResultsTemplate), NoResultsTemplate, static p => p.NoResultsTemplate, static (p, v) => p.NoResultsTemplate = v);
        }

        if (NoSearchBox.HasValue)
        {
            bitPhoneInput.TakeFromCascade(nameof(NoSearchBox), NoSearchBox.Value, static p => p.NoSearchBox, static (p, v) => p.NoSearchBox = v);
        }

        if (NoWrapNavigation.HasValue)
        {
            bitPhoneInput.TakeFromCascade(nameof(NoWrapNavigation), NoWrapNavigation.Value, static p => p.NoWrapNavigation, static (p, v) => p.NoWrapNavigation = v);
        }

        if (Placeholder.HasValue())
        {
            bitPhoneInput.TakeFromCascade(nameof(Placeholder), Placeholder, static p => p.Placeholder, static (p, v) => p.Placeholder = v);
        }

        if (PreferredCountries is not null)
        {
            bitPhoneInput.TakeFromCascade(nameof(PreferredCountries), PreferredCountries, static p => p.PreferredCountries, static (p, v) => p.PreferredCountries = v);
        }

        if (Responsive.HasValue)
        {
            bitPhoneInput.TakeFromCascade(nameof(Responsive), Responsive.Value, static p => p.Responsive, static (p, v) => p.Responsive = v);
        }

        if (ResponsiveCloseButtonAriaLabel.HasValue())
        {
            bitPhoneInput.TakeFromCascade(nameof(ResponsiveCloseButtonAriaLabel), ResponsiveCloseButtonAriaLabel, static p => p.ResponsiveCloseButtonAriaLabel, static (p, v) => p.ResponsiveCloseButtonAriaLabel = v);
        }

        if (ResponsiveCloseIcon is not null)
        {
            bitPhoneInput.TakeFromCascade(nameof(ResponsiveCloseIcon), ResponsiveCloseIcon, static p => p.ResponsiveCloseIcon, static (p, v) => p.ResponsiveCloseIcon = v);
        }

        if (ResponsiveCloseIconName.HasValue())
        {
            bitPhoneInput.TakeFromCascade(nameof(ResponsiveCloseIconName), ResponsiveCloseIconName, static p => p.ResponsiveCloseIconName, static (p, v) => p.ResponsiveCloseIconName = v);
        }

        if (SearchBoxAriaLabel.HasValue())
        {
            bitPhoneInput.TakeFromCascade(nameof(SearchBoxAriaLabel), SearchBoxAriaLabel, static p => p.SearchBoxAriaLabel, static (p, v) => p.SearchBoxAriaLabel = v);
        }

        if (SearchBoxPlaceholder.HasValue())
        {
            bitPhoneInput.TakeFromCascade(nameof(SearchBoxPlaceholder), SearchBoxPlaceholder, static p => p.SearchBoxPlaceholder, static (p, v) => p.SearchBoxPlaceholder = v);
        }

        if (SearchResultsAnnouncement is not null)
        {
            bitPhoneInput.TakeFromCascade(nameof(SearchResultsAnnouncement), SearchResultsAnnouncement, static p => p.SearchResultsAnnouncement, static (p, v) => p.SearchResultsAnnouncement = v);
        }

        if (ShowClearButton.HasValue)
        {
            bitPhoneInput.TakeFromCascade(nameof(ShowClearButton), ShowClearButton.Value, static p => p.ShowClearButton, static (p, v) => p.ShowClearButton = v);
        }

        if (Size.HasValue)
        {
            bitPhoneInput.TakeFromCascade(nameof(Size), Size.Value, static p => p.Size, static (p, v) => p.Size = v);
        }

        if (Strict.HasValue)
        {
            bitPhoneInput.TakeFromCascade(nameof(Strict), Strict.Value, static p => p.Strict, static (p, v) => p.Strict = v);
        }

        if (Styles is not null)
        {
            bitPhoneInput.TakeFromCascade(nameof(Styles), Styles, static p => p.Styles, static (p, v) => p.Styles = v);
        }

        if (ThrottleTime.HasValue)
        {
            bitPhoneInput.TakeFromCascade(nameof(ThrottleTime), ThrottleTime.Value, static p => p.ThrottleTime, static (p, v) => p.ThrottleTime = v);
        }

        if (Title.HasValue())
        {
            bitPhoneInput.TakeFromCascade(nameof(Title), Title, static p => p.Title, static (p, v) => p.Title = v);
        }

        if (Underlined.HasValue)
        {
            bitPhoneInput.TakeFromCascade(nameof(Underlined), Underlined.Value, static p => p.Underlined, static (p, v) => p.Underlined = v);
        }
    }
}
