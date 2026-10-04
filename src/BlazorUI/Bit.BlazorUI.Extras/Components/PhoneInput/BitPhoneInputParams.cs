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

        // The parameters of the input base classes are not tracked by the generated HasNotBeenSet of the
        // component, which only knows the ones the component declares itself, so they are asked about through
        // the tier that does track them.
        if (AutoComplete.HasValue() && bitPhoneInput.HasNotBeenSetOnTextInput(nameof(AutoComplete)))
        {
            bitPhoneInput.AutoComplete = AutoComplete;
        }

        if (AutoPlaceholder.HasValue && bitPhoneInput.HasNotBeenSet(nameof(AutoPlaceholder)))
        {
            bitPhoneInput.AutoPlaceholder = AutoPlaceholder.Value;
        }

        if (Background.HasValue && bitPhoneInput.HasNotBeenSet(nameof(Background)))
        {
            bitPhoneInput.Background = Background.Value;

            bitPhoneInput.ClassBuilder.Reset();
        }

        if (Border.HasValue && bitPhoneInput.HasNotBeenSet(nameof(Border)))
        {
            bitPhoneInput.Border = Border.Value;

            bitPhoneInput.ClassBuilder.Reset();
        }

        if (Classes is not null && bitPhoneInput.HasNotBeenSet(nameof(Classes)))
        {
            bitPhoneInput.Classes = Classes;

            bitPhoneInput.ClassBuilder.Reset();
        }

        if (ClearButtonAriaLabel.HasValue() && bitPhoneInput.HasNotBeenSet(nameof(ClearButtonAriaLabel)))
        {
            bitPhoneInput.ClearButtonAriaLabel = ClearButtonAriaLabel;
        }

        if (ClearButtonIcon is not null && bitPhoneInput.HasNotBeenSet(nameof(ClearButtonIcon)))
        {
            bitPhoneInput.ClearButtonIcon = ClearButtonIcon;
        }

        if (ClearButtonIconName.HasValue() && bitPhoneInput.HasNotBeenSet(nameof(ClearButtonIconName)))
        {
            bitPhoneInput.ClearButtonIconName = ClearButtonIconName;
        }

        if (ClearButtonTemplate is not null && bitPhoneInput.HasNotBeenSet(nameof(ClearButtonTemplate)))
        {
            bitPhoneInput.ClearButtonTemplate = ClearButtonTemplate;
        }

        // Null rather than empty is what leaves the field alone: an empty announcement is how silence is asked for.
        if (ClearedAnnouncement is not null && bitPhoneInput.HasNotBeenSet(nameof(ClearedAnnouncement)))
        {
            bitPhoneInput.ClearedAnnouncement = ClearedAnnouncement;
        }

        if (Color.HasValue && bitPhoneInput.HasNotBeenSet(nameof(Color)))
        {
            bitPhoneInput.Color = Color.Value;

            bitPhoneInput.ClassBuilder.Reset();
        }

        if (Countries is not null && bitPhoneInput.HasNotBeenSet(nameof(Countries)))
        {
            bitPhoneInput.Countries = Countries;
        }

        if (DebounceTime.HasValue && bitPhoneInput.HasNotBeenSetOnTextInput(nameof(DebounceTime)))
        {
            bitPhoneInput.DebounceTime = DebounceTime.Value;
        }

        if (DefaultCountry is not null && bitPhoneInput.HasNotBeenSet(nameof(DefaultCountry)))
        {
            bitPhoneInput.DefaultCountry = DefaultCountry;
        }

        if (Description.HasValue() && bitPhoneInput.HasNotBeenSet(nameof(Description)))
        {
            bitPhoneInput.Description = Description;
        }

        if (DescriptionTemplate is not null && bitPhoneInput.HasNotBeenSet(nameof(DescriptionTemplate)))
        {
            bitPhoneInput.DescriptionTemplate = DescriptionTemplate;
        }

        if (DropDirection.HasValue && bitPhoneInput.HasNotBeenSet(nameof(DropDirection)))
        {
            bitPhoneInput.DropDirection = DropDirection.Value;
        }

        if (DropdownAriaLabel.HasValue() && bitPhoneInput.HasNotBeenSet(nameof(DropdownAriaLabel)))
        {
            bitPhoneInput.DropdownAriaLabel = DropdownAriaLabel;
        }

        if (DropdownPlaceholder.HasValue() && bitPhoneInput.HasNotBeenSet(nameof(DropdownPlaceholder)))
        {
            bitPhoneInput.DropdownPlaceholder = DropdownPlaceholder;
        }

        if (DropdownTemplate is not null && bitPhoneInput.HasNotBeenSet(nameof(DropdownTemplate)))
        {
            bitPhoneInput.DropdownTemplate = DropdownTemplate;
        }

        if (EnterKeyHint.HasValue() && bitPhoneInput.HasNotBeenSet(nameof(EnterKeyHint)))
        {
            bitPhoneInput.EnterKeyHint = EnterKeyHint;
        }

        if (ExcludeCountries is not null && bitPhoneInput.HasNotBeenSet(nameof(ExcludeCountries)))
        {
            bitPhoneInput.ExcludeCountries = ExcludeCountries;
        }

        if (FlagUrlSelector is not null && bitPhoneInput.HasNotBeenSet(nameof(FlagUrlSelector)))
        {
            bitPhoneInput.FlagUrlSelector = FlagUrlSelector;
        }

        if (FullWidth.HasValue && bitPhoneInput.HasNotBeenSet(nameof(FullWidth)))
        {
            bitPhoneInput.FullWidth = FullWidth.Value;

            bitPhoneInput.ClassBuilder.Reset();
        }

        if (Immediate.HasValue && bitPhoneInput.HasNotBeenSetOnTextInput(nameof(Immediate)))
        {
            bitPhoneInput.Immediate = Immediate.Value;
        }

        if (InputMode.HasValue && bitPhoneInput.HasNotBeenSet(nameof(InputMode)))
        {
            bitPhoneInput.InputMode = InputMode.Value;
        }

        if (ItemTemplate is not null && bitPhoneInput.HasNotBeenSet(nameof(ItemTemplate)))
        {
            bitPhoneInput.ItemTemplate = ItemTemplate;
        }

        if (Label.HasValue() && bitPhoneInput.HasNotBeenSet(nameof(Label)))
        {
            bitPhoneInput.Label = Label;
        }

        if (LabelTemplate is not null && bitPhoneInput.HasNotBeenSet(nameof(LabelTemplate)))
        {
            bitPhoneInput.LabelTemplate = LabelTemplate;
        }

        if (Mask.HasValue() && bitPhoneInput.HasNotBeenSet(nameof(Mask)))
        {
            bitPhoneInput.Mask = Mask;
        }

        if (MaskSelector is not null && bitPhoneInput.HasNotBeenSet(nameof(MaskSelector)))
        {
            bitPhoneInput.MaskSelector = MaskSelector;
        }

        if (MaxHeight.HasValue && bitPhoneInput.HasNotBeenSet(nameof(MaxHeight)))
        {
            bitPhoneInput.MaxHeight = MaxHeight.Value;
        }

        if (MaxLength.HasValue && bitPhoneInput.HasNotBeenSet(nameof(MaxLength)))
        {
            bitPhoneInput.MaxLength = MaxLength.Value;
        }

        if (NoBorder.HasValue && bitPhoneInput.HasNotBeenSet(nameof(NoBorder)))
        {
            bitPhoneInput.NoBorder = NoBorder.Value;

            bitPhoneInput.ClassBuilder.Reset();
        }

        if (NoDialCode.HasValue && bitPhoneInput.HasNotBeenSet(nameof(NoDialCode)))
        {
            bitPhoneInput.NoDialCode = NoDialCode.Value;
        }

        if (NoDropdown.HasValue && bitPhoneInput.HasNotBeenSet(nameof(NoDropdown)))
        {
            bitPhoneInput.NoDropdown = NoDropdown.Value;

            bitPhoneInput.ClassBuilder.Reset();
        }

        if (NoFlags.HasValue && bitPhoneInput.HasNotBeenSet(nameof(NoFlags)))
        {
            bitPhoneInput.NoFlags = NoFlags.Value;
        }

        if (NoFocusOnSelect.HasValue && bitPhoneInput.HasNotBeenSet(nameof(NoFocusOnSelect)))
        {
            bitPhoneInput.NoFocusOnSelect = NoFocusOnSelect.Value;
        }

        if (NoResultsMessage.HasValue() && bitPhoneInput.HasNotBeenSet(nameof(NoResultsMessage)))
        {
            bitPhoneInput.NoResultsMessage = NoResultsMessage;
        }

        if (NoResultsTemplate is not null && bitPhoneInput.HasNotBeenSet(nameof(NoResultsTemplate)))
        {
            bitPhoneInput.NoResultsTemplate = NoResultsTemplate;
        }

        if (NoSearchBox.HasValue && bitPhoneInput.HasNotBeenSet(nameof(NoSearchBox)))
        {
            bitPhoneInput.NoSearchBox = NoSearchBox.Value;
        }

        if (NoWrapNavigation.HasValue && bitPhoneInput.HasNotBeenSet(nameof(NoWrapNavigation)))
        {
            bitPhoneInput.NoWrapNavigation = NoWrapNavigation.Value;
        }

        if (Placeholder.HasValue() && bitPhoneInput.HasNotBeenSet(nameof(Placeholder)))
        {
            bitPhoneInput.Placeholder = Placeholder;
        }

        if (PreferredCountries is not null && bitPhoneInput.HasNotBeenSet(nameof(PreferredCountries)))
        {
            bitPhoneInput.PreferredCountries = PreferredCountries;
        }

        if (Responsive.HasValue && bitPhoneInput.HasNotBeenSet(nameof(Responsive)))
        {
            bitPhoneInput.Responsive = Responsive.Value;

            bitPhoneInput.ClassBuilder.Reset();
        }

        if (ResponsiveCloseButtonAriaLabel.HasValue() && bitPhoneInput.HasNotBeenSet(nameof(ResponsiveCloseButtonAriaLabel)))
        {
            bitPhoneInput.ResponsiveCloseButtonAriaLabel = ResponsiveCloseButtonAriaLabel;
        }

        if (ResponsiveCloseIcon is not null && bitPhoneInput.HasNotBeenSet(nameof(ResponsiveCloseIcon)))
        {
            bitPhoneInput.ResponsiveCloseIcon = ResponsiveCloseIcon;
        }

        if (ResponsiveCloseIconName.HasValue() && bitPhoneInput.HasNotBeenSet(nameof(ResponsiveCloseIconName)))
        {
            bitPhoneInput.ResponsiveCloseIconName = ResponsiveCloseIconName;
        }

        if (SearchBoxAriaLabel.HasValue() && bitPhoneInput.HasNotBeenSet(nameof(SearchBoxAriaLabel)))
        {
            bitPhoneInput.SearchBoxAriaLabel = SearchBoxAriaLabel;
        }

        if (SearchBoxPlaceholder.HasValue() && bitPhoneInput.HasNotBeenSet(nameof(SearchBoxPlaceholder)))
        {
            bitPhoneInput.SearchBoxPlaceholder = SearchBoxPlaceholder;
        }

        if (SearchResultsAnnouncement is not null && bitPhoneInput.HasNotBeenSet(nameof(SearchResultsAnnouncement)))
        {
            bitPhoneInput.SearchResultsAnnouncement = SearchResultsAnnouncement;
        }

        if (ShowClearButton.HasValue && bitPhoneInput.HasNotBeenSet(nameof(ShowClearButton)))
        {
            bitPhoneInput.ShowClearButton = ShowClearButton.Value;
        }

        if (Size.HasValue && bitPhoneInput.HasNotBeenSet(nameof(Size)))
        {
            bitPhoneInput.Size = Size.Value;

            bitPhoneInput.ClassBuilder.Reset();
        }

        if (Strict.HasValue && bitPhoneInput.HasNotBeenSet(nameof(Strict)))
        {
            bitPhoneInput.Strict = Strict.Value;
        }

        if (Styles is not null && bitPhoneInput.HasNotBeenSet(nameof(Styles)))
        {
            bitPhoneInput.Styles = Styles;

            bitPhoneInput.StyleBuilder.Reset();
        }

        if (ThrottleTime.HasValue && bitPhoneInput.HasNotBeenSetOnTextInput(nameof(ThrottleTime)))
        {
            bitPhoneInput.ThrottleTime = ThrottleTime.Value;
        }

        if (Title.HasValue() && bitPhoneInput.HasNotBeenSet(nameof(Title)))
        {
            bitPhoneInput.Title = Title;
        }

        if (Underlined.HasValue && bitPhoneInput.HasNotBeenSet(nameof(Underlined)))
        {
            bitPhoneInput.Underlined = Underlined.Value;

            bitPhoneInput.ClassBuilder.Reset();
        }
    }
}
