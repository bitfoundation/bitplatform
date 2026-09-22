namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitSearchBox"/> component.
/// </summary>
/// <remarks>
/// Everything it carries is shared configuration: the look of the field, how the suggest list behaves and
/// the strings that name its parts for a screen reader. What belongs to one search box alone - the value,
/// the label, the suggest source, the templates and the callbacks - stays in the markup of that search box.
/// </remarks>
public class BitSearchBoxParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitSearchBox"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitSearchBox value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitSearchBox)}";



    public string Name => ParamName;



    /// <summary>
    /// Builds the text that the screen reader announces through the live region of the search box whenever
    /// the suggest items change, in place of the built-in English announcements. It is the hook for
    /// localizing them, which is a decision a whole app makes once rather than every field of it.
    /// </summary>
    public Func<BitSearchBoxAnnouncementArgs, string?>? AnnouncementProvider { get; set; }

    /// <summary>
    /// Detailed description of the search box for the benefit of screen readers (rendered into <c>aria-describedby</c>).
    /// </summary>
    public string? AriaDescription { get; set; }

    /// <summary>
    /// Sets the autocapitalize html attribute of the input element, which tells a virtual keyboard whether
    /// and how to capitalize what is typed.
    /// </summary>
    public string? AutoCapitalize { get; set; }

    /// <summary>
    /// Sets the autocomplete html attribute of the input element.
    /// </summary>
    public string? AutoComplete { get; set; }

    /// <summary>
    /// Sets the autocorrect html attribute of the input element.
    /// </summary>
    public bool? AutoCorrect { get; set; }

    /// <summary>
    /// If true, the input automatically receives focus when the page renders (rendered as the <c>autofocus</c> attribute).
    /// </summary>
    public bool? AutoFocus { get; set; }

    /// <summary>
    /// Completes what is being typed with the first suggest item that starts with it, selecting the part
    /// the user has not typed.
    /// </summary>
    public bool? AutoFillSuggestItem { get; set; }

    /// <summary>
    /// Automatically highlights the first suggest item as soon as the suggest list opens.
    /// </summary>
    public bool? AutoSelectSuggestItem { get; set; }

    /// <summary>
    /// The background color kind of the search box.
    /// </summary>
    public BitColorKind? Background { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the search box.
    /// </summary>
    public BitSearchBoxClassStyles? Classes { get; set; }

    /// <summary>
    /// The accessible label (aria-label) of the clear button.
    /// </summary>
    public string? ClearButtonAriaLabel { get; set; }

    /// <summary>
    /// The icon of the clear button, from an external icon library.
    /// </summary>
    public BitIconInfo? ClearButtonIcon { get; set; }

    /// <summary>
    /// The name of the icon of the clear button, from the built-in Fluent UI icons.
    /// </summary>
    public string? ClearButtonIconName { get; set; }

    /// <summary>
    /// The general color of the search box, used for colored parts like icons.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// The number of milliseconds to wait before the value is committed while the user keeps typing.
    /// </summary>
    public int? DebounceTime { get; set; }

    /// <summary>
    /// Whether or not to animate the search box icon on focus.
    /// </summary>
    public bool? DisableAnimation { get; set; }

    /// <summary>
    /// Sets the enterkeyhint html attribute of the input element, which tells virtual keyboards which
    /// action label to render on their enter key.
    /// </summary>
    public BitEnterKeyHint? EnterKeyHint { get; set; }

    /// <summary>
    /// Forces the suggest callout width to be always fixed at the component's width.
    /// </summary>
    public bool? FixedCalloutWidth { get; set; }

    /// <summary>
    /// Whether or not to make the icon be always visible (it hides by default when the search box is focused).
    /// </summary>
    public bool? FixedIcon { get; set; }

    /// <summary>
    /// The keyboard shortcut that moves the focus into the search box from anywhere on the page, written in
    /// the syntax of the <c>aria-keyshortcuts</c> attribute (for example <c>Control+K Meta+K</c>, which
    /// covers a Windows and a macOS keyboard at once).
    /// </summary>
    public string? FocusShortcut { get; set; }

    /// <summary>
    /// Expands the search box to fill the available width of its container.
    /// </summary>
    public bool? FullWidth { get; set; }

    /// <summary>
    /// Whether to hide the clear button when the search box has value.
    /// </summary>
    public bool? HideClearButton { get; set; }

    /// <summary>
    /// Whether or not the icon is visible.
    /// </summary>
    public bool? HideIcon { get; set; }

    /// <summary>
    /// Highlights the part of each suggest item that matches the current search term.
    /// </summary>
    public bool? HighlightSuggestItems { get; set; }

    /// <summary>
    /// The icon of the search box, from an external icon library.
    /// </summary>
    public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// The name of the icon of the search box, from the built-in Fluent UI icons.
    /// </summary>
    public string? IconName { get; set; }

    /// <summary>
    /// Commits the value on every keystroke (the input event) instead of on the change event.
    /// </summary>
    public bool? Immediate { get; set; }

    /// <summary>
    /// Sets the inputmode html attribute of the input element.
    /// </summary>
    public BitInputMode? InputMode { get; set; }

    /// <summary>
    /// What the live region of the component announces while the search box is loading.
    /// </summary>
    public string? LoadingAriaLabel { get; set; }

    /// <summary>
    /// The text rendered next to the loading indicator of the suggest callout while the suggest items
    /// provider is resolving the suggest items.
    /// </summary>
    public string? LoadingText { get; set; }

    /// <summary>
    /// Sets the maxlength html attribute of the input element. A negative value means no limit.
    /// </summary>
    public int? MaxLength { get; set; }

    /// <summary>
    /// The maximum number of items or suggestions that will be displayed. A value of zero or less means no limit.
    /// </summary>
    public int? MaxSuggestCount { get; set; }

    /// <summary>
    /// The minimum character requirement for doing a search in suggest items.
    /// </summary>
    public int? MinSuggestTriggerChars { get; set; }

    /// <summary>
    /// The composite format of the hint the callout shows while the typed term is still shorter than
    /// <see cref="MinSuggestTriggerChars"/>, which receives the number of characters that are still missing.
    /// </summary>
    public string? MinSuggestTriggerCharsText { get; set; }

    /// <summary>
    /// Removes the overlay of suggest items callout.
    /// </summary>
    public bool? Modeless { get; set; }

    /// <summary>
    /// Removes the default border of the search box.
    /// </summary>
    public bool? NoBorder { get; set; }

    /// <summary>
    /// Prevents clearing the value of the search box when the user presses the escape key.
    /// </summary>
    public bool? NoClearOnEscape { get; set; }

    /// <summary>
    /// The text rendered in the callout when the search finds no suggest item.
    /// </summary>
    public string? NoResultsText { get; set; }

    /// <summary>
    /// Opts the search box out of the validation of the cascading EditContext.
    /// </summary>
    public bool? NoValidate { get; set; }

    /// <summary>
    /// Stops the up and down arrows from cycling between the two ends of the suggest list.
    /// </summary>
    public bool? NoWrapNavigation { get; set; }

    /// <summary>
    /// Placeholder for the search box.
    /// </summary>
    public string? Placeholder { get; set; }

    /// <summary>
    /// Keeps the value visible and selectable but blocks editing, clearing and picking suggestions.
    /// </summary>
    public bool? ReadOnly { get; set; }

    /// <summary>
    /// Renders the required html attribute on the input element and the required marker next to the label.
    /// </summary>
    public bool? Required { get; set; }

    /// <summary>
    /// The accessible label (aria-label) of the search button.
    /// </summary>
    public string? SearchButtonAriaLabel { get; set; }

    /// <summary>
    /// The icon of the search button, from an external icon library.
    /// </summary>
    public BitIconInfo? SearchButtonIcon { get; set; }

    /// <summary>
    /// The name of the icon of the search button, from the built-in Fluent UI icons.
    /// </summary>
    public string? SearchButtonIconName { get; set; }

    /// <summary>
    /// The label rendered on the search button next to its icon.
    /// </summary>
    public string? SearchButtonText { get; set; }

    /// <summary>
    /// Selects the text already in the search box whenever the input takes the focus.
    /// </summary>
    public bool? SelectTextOnFocus { get; set; }

    /// <summary>
    /// Whether to show the search button.
    /// </summary>
    public bool? ShowSearchButton { get; set; }

    /// <summary>
    /// Opens the suggest items callout as soon as the input gets focused, without waiting for the user to type.
    /// </summary>
    public bool? ShowSuggestItemsOnFocus { get; set; }

    /// <summary>
    /// The size of the search box.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// Sets the spellcheck html attribute of the input element.
    /// </summary>
    public bool? SpellCheck { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the search box.
    /// </summary>
    public BitSearchBoxClassStyles? Styles { get; set; }

    /// <summary>
    /// The text rendered in the callout when the suggest items provider throws.
    /// </summary>
    public string? SuggestFailedText { get; set; }

    /// <summary>
    /// Matches the search term against the suggest items with the diacritics of both removed.
    /// </summary>
    public bool? SuggestIgnoreDiacritics { get; set; }

    /// <summary>
    /// Custom search function to be used in place of the default search algorithm, so that how a term is
    /// matched against the suggest items is decided once for every search box under the cascade.
    /// </summary>
    public Func<string?, string?, bool>? SuggestFilterFunction { get; set; }

    /// <summary>
    /// The accessible label (aria-label) of the suggest items list.
    /// </summary>
    public string? SuggestItemsAriaLabel { get; set; }

    /// <summary>
    /// The maximum number of milliseconds the value is committed at, while the user keeps typing.
    /// </summary>
    public int? ThrottleTime { get; set; }

    /// <summary>
    /// Trims the leading and trailing white-spaces of the value of the search box.
    /// </summary>
    public bool? Trim { get; set; }

    /// <summary>
    /// Whether or not the search box is underlined.
    /// </summary>
    public bool? Underlined { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitSearchBox"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitSearchBox"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitSearchBox"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitSearchBox"/>.
    /// </remarks>
    /// <param name="bitSearchBox">
    /// The <see cref="BitSearchBox"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitSearchBox bitSearchBox)
    {
        if (bitSearchBox is null) return;

        UpdateBaseParameters(bitSearchBox);

        // The parameters the search box declares itself are asked through the HasNotBeenSet the source
        // generator writes for it; the ones it inherits from the input base classes have a record of
        // their own, which is what HasNotBeenSetOnInputBase reads (see BitInputBase).

        if (AnnouncementProvider is not null && bitSearchBox.HasNotBeenSet(nameof(AnnouncementProvider)))
        {
            bitSearchBox.AnnouncementProvider = AnnouncementProvider;
        }

        if (AriaDescription.HasValue() && bitSearchBox.HasNotBeenSet(nameof(AriaDescription)))
        {
            bitSearchBox.AriaDescription = AriaDescription;
        }

        if (AutoCapitalize.HasValue() && bitSearchBox.HasNotBeenSet(nameof(AutoCapitalize)))
        {
            bitSearchBox.AutoCapitalize = AutoCapitalize;
        }

        if (AutoComplete.HasValue() && bitSearchBox.HasNotBeenSetOnInputBase(nameof(AutoComplete)))
        {
            bitSearchBox.AutoComplete = AutoComplete;
        }

        if (AutoCorrect.HasValue && bitSearchBox.HasNotBeenSet(nameof(AutoCorrect)))
        {
            bitSearchBox.AutoCorrect = AutoCorrect.Value;
        }

        if (AutoFocus.HasValue && bitSearchBox.HasNotBeenSetOnInputBase(nameof(AutoFocus)))
        {
            bitSearchBox.AutoFocus = AutoFocus.Value;
        }

        if (AutoFillSuggestItem.HasValue && bitSearchBox.HasNotBeenSet(nameof(AutoFillSuggestItem)))
        {
            bitSearchBox.AutoFillSuggestItem = AutoFillSuggestItem.Value;
        }

        if (AutoSelectSuggestItem.HasValue && bitSearchBox.HasNotBeenSet(nameof(AutoSelectSuggestItem)))
        {
            bitSearchBox.AutoSelectSuggestItem = AutoSelectSuggestItem.Value;
        }

        if (Background.HasValue && bitSearchBox.HasNotBeenSet(nameof(Background)))
        {
            bitSearchBox.Background = Background.Value;

            bitSearchBox.ClassBuilder.Reset();
        }

        if (Classes is not null && bitSearchBox.HasNotBeenSet(nameof(Classes)))
        {
            bitSearchBox.Classes = Classes;

            bitSearchBox.ClassBuilder.Reset();
        }

        if (ClearButtonAriaLabel.HasValue() && bitSearchBox.HasNotBeenSet(nameof(ClearButtonAriaLabel)))
        {
            bitSearchBox.ClearButtonAriaLabel = ClearButtonAriaLabel!;
        }

        if (ClearButtonIcon is not null && bitSearchBox.HasNotBeenSet(nameof(ClearButtonIcon)))
        {
            bitSearchBox.ClearButtonIcon = ClearButtonIcon;
        }

        if (ClearButtonIconName.HasValue() && bitSearchBox.HasNotBeenSet(nameof(ClearButtonIconName)))
        {
            bitSearchBox.ClearButtonIconName = ClearButtonIconName;
        }

        if (Color.HasValue && bitSearchBox.HasNotBeenSet(nameof(Color)))
        {
            bitSearchBox.Color = Color.Value;

            bitSearchBox.ClassBuilder.Reset();
        }

        if (DebounceTime.HasValue && bitSearchBox.HasNotBeenSetOnInputBase(nameof(DebounceTime)))
        {
            bitSearchBox.DebounceTime = DebounceTime.Value;
        }

        if (DisableAnimation.HasValue && bitSearchBox.HasNotBeenSet(nameof(DisableAnimation)))
        {
            bitSearchBox.DisableAnimation = DisableAnimation.Value;

            bitSearchBox.ClassBuilder.Reset();
        }

        if (EnterKeyHint.HasValue && bitSearchBox.HasNotBeenSet(nameof(EnterKeyHint)))
        {
            bitSearchBox.EnterKeyHint = EnterKeyHint.Value;
        }

        if (FixedCalloutWidth.HasValue && bitSearchBox.HasNotBeenSet(nameof(FixedCalloutWidth)))
        {
            bitSearchBox.FixedCalloutWidth = FixedCalloutWidth.Value;
        }

        if (FixedIcon.HasValue && bitSearchBox.HasNotBeenSet(nameof(FixedIcon)))
        {
            bitSearchBox.FixedIcon = FixedIcon.Value;

            bitSearchBox.ClassBuilder.Reset();
        }

        if (FocusShortcut is not null && bitSearchBox.HasNotBeenSet(nameof(FocusShortcut)))
        {
            bitSearchBox.FocusShortcut = FocusShortcut;
        }

        if (FullWidth.HasValue && bitSearchBox.HasNotBeenSet(nameof(FullWidth)))
        {
            bitSearchBox.FullWidth = FullWidth.Value;

            bitSearchBox.ClassBuilder.Reset();
        }

        if (HideClearButton.HasValue && bitSearchBox.HasNotBeenSet(nameof(HideClearButton)))
        {
            bitSearchBox.HideClearButton = HideClearButton.Value;
        }

        if (HideIcon.HasValue && bitSearchBox.HasNotBeenSet(nameof(HideIcon)))
        {
            bitSearchBox.HideIcon = HideIcon.Value;

            bitSearchBox.ClassBuilder.Reset();
        }

        if (HighlightSuggestItems.HasValue && bitSearchBox.HasNotBeenSet(nameof(HighlightSuggestItems)))
        {
            bitSearchBox.HighlightSuggestItems = HighlightSuggestItems.Value;
        }

        if (Icon is not null && bitSearchBox.HasNotBeenSet(nameof(Icon)))
        {
            bitSearchBox.Icon = Icon;
        }

        if (IconName.HasValue() && bitSearchBox.HasNotBeenSet(nameof(IconName)))
        {
            bitSearchBox.IconName = IconName;
        }

        if (Immediate.HasValue && bitSearchBox.HasNotBeenSetOnInputBase(nameof(Immediate)))
        {
            bitSearchBox.Immediate = Immediate.Value;
        }

        if (InputMode.HasValue && bitSearchBox.HasNotBeenSet(nameof(InputMode)))
        {
            bitSearchBox.InputMode = InputMode.Value;
        }

        if (LoadingAriaLabel.HasValue() && bitSearchBox.HasNotBeenSet(nameof(LoadingAriaLabel)))
        {
            bitSearchBox.LoadingAriaLabel = LoadingAriaLabel!;
        }

        if (LoadingText.HasValue() && bitSearchBox.HasNotBeenSet(nameof(LoadingText)))
        {
            bitSearchBox.LoadingText = LoadingText;
        }

        if (MaxLength.HasValue && bitSearchBox.HasNotBeenSet(nameof(MaxLength)))
        {
            bitSearchBox.MaxLength = MaxLength.Value;
        }

        if (MaxSuggestCount.HasValue && bitSearchBox.HasNotBeenSet(nameof(MaxSuggestCount)))
        {
            bitSearchBox.MaxSuggestCount = MaxSuggestCount.Value;
        }

        if (MinSuggestTriggerChars.HasValue && bitSearchBox.HasNotBeenSet(nameof(MinSuggestTriggerChars)))
        {
            bitSearchBox.MinSuggestTriggerChars = MinSuggestTriggerChars.Value;
        }

        if (MinSuggestTriggerCharsText.HasValue() && bitSearchBox.HasNotBeenSet(nameof(MinSuggestTriggerCharsText)))
        {
            bitSearchBox.MinSuggestTriggerCharsText = MinSuggestTriggerCharsText;
        }

        if (Modeless.HasValue && bitSearchBox.HasNotBeenSet(nameof(Modeless)))
        {
            bitSearchBox.Modeless = Modeless.Value;
        }

        if (NoBorder.HasValue && bitSearchBox.HasNotBeenSet(nameof(NoBorder)))
        {
            bitSearchBox.NoBorder = NoBorder.Value;

            bitSearchBox.ClassBuilder.Reset();
        }

        if (NoClearOnEscape.HasValue && bitSearchBox.HasNotBeenSet(nameof(NoClearOnEscape)))
        {
            bitSearchBox.NoClearOnEscape = NoClearOnEscape.Value;
        }

        if (NoResultsText.HasValue() && bitSearchBox.HasNotBeenSet(nameof(NoResultsText)))
        {
            bitSearchBox.NoResultsText = NoResultsText;
        }

        if (NoValidate.HasValue && bitSearchBox.HasNotBeenSetOnInputBase(nameof(NoValidate)))
        {
            bitSearchBox.NoValidate = NoValidate.Value;
        }

        if (NoWrapNavigation.HasValue && bitSearchBox.HasNotBeenSet(nameof(NoWrapNavigation)))
        {
            bitSearchBox.NoWrapNavigation = NoWrapNavigation.Value;
        }

        if (Placeholder.HasValue() && bitSearchBox.HasNotBeenSet(nameof(Placeholder)))
        {
            bitSearchBox.Placeholder = Placeholder;
        }

        if (ReadOnly.HasValue && bitSearchBox.HasNotBeenSetOnInputBase(nameof(ReadOnly)))
        {
            bitSearchBox.ReadOnly = ReadOnly.Value;
        }

        if (Required.HasValue && bitSearchBox.HasNotBeenSetOnInputBase(nameof(Required)))
        {
            bitSearchBox.Required = Required.Value;

            bitSearchBox.ClassBuilder.Reset();
        }

        if (SearchButtonAriaLabel.HasValue() && bitSearchBox.HasNotBeenSet(nameof(SearchButtonAriaLabel)))
        {
            bitSearchBox.SearchButtonAriaLabel = SearchButtonAriaLabel!;
        }

        if (SearchButtonIcon is not null && bitSearchBox.HasNotBeenSet(nameof(SearchButtonIcon)))
        {
            bitSearchBox.SearchButtonIcon = SearchButtonIcon;
        }

        if (SearchButtonIconName.HasValue() && bitSearchBox.HasNotBeenSet(nameof(SearchButtonIconName)))
        {
            bitSearchBox.SearchButtonIconName = SearchButtonIconName;
        }

        if (SearchButtonText.HasValue() && bitSearchBox.HasNotBeenSet(nameof(SearchButtonText)))
        {
            bitSearchBox.SearchButtonText = SearchButtonText;

            bitSearchBox.ClassBuilder.Reset();
        }

        if (SelectTextOnFocus.HasValue && bitSearchBox.HasNotBeenSet(nameof(SelectTextOnFocus)))
        {
            bitSearchBox.SelectTextOnFocus = SelectTextOnFocus.Value;
        }

        if (ShowSearchButton.HasValue && bitSearchBox.HasNotBeenSet(nameof(ShowSearchButton)))
        {
            bitSearchBox.ShowSearchButton = ShowSearchButton.Value;

            bitSearchBox.ClassBuilder.Reset();
        }

        if (ShowSuggestItemsOnFocus.HasValue && bitSearchBox.HasNotBeenSet(nameof(ShowSuggestItemsOnFocus)))
        {
            bitSearchBox.ShowSuggestItemsOnFocus = ShowSuggestItemsOnFocus.Value;
        }

        if (Size.HasValue && bitSearchBox.HasNotBeenSet(nameof(Size)))
        {
            bitSearchBox.Size = Size.Value;

            bitSearchBox.ClassBuilder.Reset();
        }

        if (SpellCheck.HasValue && bitSearchBox.HasNotBeenSet(nameof(SpellCheck)))
        {
            bitSearchBox.SpellCheck = SpellCheck.Value;
        }

        if (Styles is not null && bitSearchBox.HasNotBeenSet(nameof(Styles)))
        {
            bitSearchBox.Styles = Styles;

            bitSearchBox.StyleBuilder.Reset();
        }

        if (SuggestFailedText is not null && bitSearchBox.HasNotBeenSet(nameof(SuggestFailedText)))
        {
            bitSearchBox.SuggestFailedText = SuggestFailedText;
        }

        if (SuggestIgnoreDiacritics.HasValue && bitSearchBox.HasNotBeenSet(nameof(SuggestIgnoreDiacritics)))
        {
            bitSearchBox.SuggestIgnoreDiacritics = SuggestIgnoreDiacritics.Value;
        }

        if (SuggestFilterFunction is not null && bitSearchBox.HasNotBeenSet(nameof(SuggestFilterFunction)))
        {
            bitSearchBox.SuggestFilterFunction = SuggestFilterFunction;
        }

        if (SuggestItemsAriaLabel.HasValue() && bitSearchBox.HasNotBeenSet(nameof(SuggestItemsAriaLabel)))
        {
            bitSearchBox.SuggestItemsAriaLabel = SuggestItemsAriaLabel!;
        }

        if (ThrottleTime.HasValue && bitSearchBox.HasNotBeenSetOnInputBase(nameof(ThrottleTime)))
        {
            bitSearchBox.ThrottleTime = ThrottleTime.Value;
        }

        if (Trim.HasValue && bitSearchBox.HasNotBeenSet(nameof(Trim)))
        {
            bitSearchBox.Trim = Trim.Value;
        }

        if (Underlined.HasValue && bitSearchBox.HasNotBeenSet(nameof(Underlined)))
        {
            bitSearchBox.Underlined = Underlined.Value;

            bitSearchBox.ClassBuilder.Reset();
        }
    }
}
