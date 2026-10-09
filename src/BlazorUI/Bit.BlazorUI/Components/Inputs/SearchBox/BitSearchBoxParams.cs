namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitSearchBox"/> component.
/// </summary>
/// <remarks>
/// Everything it carries is shared configuration: the look of the field, how the suggest list behaves and
/// the strings that name its parts for a screen reader. What belongs to one search box alone - the value,
/// the label, the suggest source, the templates and the callbacks - stays in the markup of that search box.
/// </remarks>
public class BitSearchBoxParams : BitInputBaseParams<string?>, IBitComponentParams
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
    /// Stops the up and down arrows from cycling between the two ends of the suggest list.
    /// </summary>
    public bool? NoWrapNavigation { get; set; }

    /// <summary>
    /// Placeholder for the search box.
    /// </summary>
    public string? Placeholder { get; set; }

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

        UpdateInputBaseParameters(bitSearchBox);

        if (AnnouncementProvider is not null)
        {
            bitSearchBox.TakeFromCascade(nameof(AnnouncementProvider), AnnouncementProvider, static s => s.AnnouncementProvider, static (s, v) => s.AnnouncementProvider = v);
        }

        if (AriaDescription.HasValue())
        {
            bitSearchBox.TakeFromCascade(nameof(AriaDescription), AriaDescription, static s => s.AriaDescription, static (s, v) => s.AriaDescription = v);
        }

        if (AutoCapitalize.HasValue())
        {
            bitSearchBox.TakeFromCascade(nameof(AutoCapitalize), AutoCapitalize, static s => s.AutoCapitalize, static (s, v) => s.AutoCapitalize = v);
        }

        if (AutoComplete.HasValue())
        {
            bitSearchBox.TakeFromCascade(nameof(AutoComplete), AutoComplete, static s => s.AutoComplete, static (s, v) => s.AutoComplete = v);
        }

        if (AutoCorrect.HasValue)
        {
            bitSearchBox.TakeFromCascade(nameof(AutoCorrect), AutoCorrect.Value, static s => s.AutoCorrect, static (s, v) => s.AutoCorrect = v);
        }

        if (AutoFocus.HasValue)
        {
            bitSearchBox.TakeFromCascade(nameof(AutoFocus), AutoFocus.Value, static s => s.AutoFocus, static (s, v) => s.AutoFocus = v);
        }

        if (AutoFillSuggestItem.HasValue)
        {
            bitSearchBox.TakeFromCascade(nameof(AutoFillSuggestItem), AutoFillSuggestItem.Value, static s => s.AutoFillSuggestItem, static (s, v) => s.AutoFillSuggestItem = v);
        }

        if (AutoSelectSuggestItem.HasValue)
        {
            bitSearchBox.TakeFromCascade(nameof(AutoSelectSuggestItem), AutoSelectSuggestItem.Value, static s => s.AutoSelectSuggestItem, static (s, v) => s.AutoSelectSuggestItem = v);
        }

        if (Background.HasValue)
        {
            bitSearchBox.TakeFromCascade(nameof(Background), Background.Value, static s => s.Background, static (s, v) => s.Background = v);
        }

        if (Classes is not null)
        {
            bitSearchBox.TakeFromCascade(nameof(Classes), Classes, static s => s.Classes, static (s, v) => s.Classes = v);
        }

        if (ClearButtonAriaLabel.HasValue())
        {
            bitSearchBox.TakeFromCascade(nameof(ClearButtonAriaLabel), ClearButtonAriaLabel!, static s => s.ClearButtonAriaLabel, static (s, v) => s.ClearButtonAriaLabel = v);
        }

        if (ClearButtonIcon is not null)
        {
            bitSearchBox.TakeFromCascade(nameof(ClearButtonIcon), ClearButtonIcon, static s => s.ClearButtonIcon, static (s, v) => s.ClearButtonIcon = v);
        }

        if (ClearButtonIconName.HasValue())
        {
            bitSearchBox.TakeFromCascade(nameof(ClearButtonIconName), ClearButtonIconName, static s => s.ClearButtonIconName, static (s, v) => s.ClearButtonIconName = v);
        }

        if (Color.HasValue)
        {
            bitSearchBox.TakeFromCascade(nameof(Color), Color.Value, static s => s.Color, static (s, v) => s.Color = v);
        }

        if (DebounceTime.HasValue)
        {
            bitSearchBox.TakeFromCascade(nameof(DebounceTime), DebounceTime.Value, static s => s.DebounceTime, static (s, v) => s.DebounceTime = v);
        }

        if (DisableAnimation.HasValue)
        {
            bitSearchBox.TakeFromCascade(nameof(DisableAnimation), DisableAnimation.Value, static s => s.DisableAnimation, static (s, v) => s.DisableAnimation = v);
        }

        if (EnterKeyHint.HasValue)
        {
            bitSearchBox.TakeFromCascade(nameof(EnterKeyHint), EnterKeyHint.Value, static s => s.EnterKeyHint, static (s, v) => s.EnterKeyHint = v);
        }

        if (FixedCalloutWidth.HasValue)
        {
            bitSearchBox.TakeFromCascade(nameof(FixedCalloutWidth), FixedCalloutWidth.Value, static s => s.FixedCalloutWidth, static (s, v) => s.FixedCalloutWidth = v);
        }

        if (FixedIcon.HasValue)
        {
            bitSearchBox.TakeFromCascade(nameof(FixedIcon), FixedIcon.Value, static s => s.FixedIcon, static (s, v) => s.FixedIcon = v);
        }

        if (FocusShortcut is not null)
        {
            bitSearchBox.TakeFromCascade(nameof(FocusShortcut), FocusShortcut, static s => s.FocusShortcut, static (s, v) => s.FocusShortcut = v);
        }

        if (FullWidth.HasValue)
        {
            bitSearchBox.TakeFromCascade(nameof(FullWidth), FullWidth.Value, static s => s.FullWidth, static (s, v) => s.FullWidth = v);
        }

        if (HideClearButton.HasValue)
        {
            bitSearchBox.TakeFromCascade(nameof(HideClearButton), HideClearButton.Value, static s => s.HideClearButton, static (s, v) => s.HideClearButton = v);
        }

        if (HideIcon.HasValue)
        {
            bitSearchBox.TakeFromCascade(nameof(HideIcon), HideIcon.Value, static s => s.HideIcon, static (s, v) => s.HideIcon = v);
        }

        if (HighlightSuggestItems.HasValue)
        {
            bitSearchBox.TakeFromCascade(nameof(HighlightSuggestItems), HighlightSuggestItems.Value, static s => s.HighlightSuggestItems, static (s, v) => s.HighlightSuggestItems = v);
        }

        if (Icon is not null)
        {
            bitSearchBox.TakeFromCascade(nameof(Icon), Icon, static s => s.Icon, static (s, v) => s.Icon = v);
        }

        if (IconName.HasValue())
        {
            bitSearchBox.TakeFromCascade(nameof(IconName), IconName, static s => s.IconName, static (s, v) => s.IconName = v);
        }

        if (Immediate.HasValue)
        {
            bitSearchBox.TakeFromCascade(nameof(Immediate), Immediate.Value, static s => s.Immediate, static (s, v) => s.Immediate = v);
        }

        if (InputMode.HasValue)
        {
            bitSearchBox.TakeFromCascade(nameof(InputMode), InputMode.Value, static s => s.InputMode, static (s, v) => s.InputMode = v);
        }

        if (LoadingAriaLabel.HasValue())
        {
            bitSearchBox.TakeFromCascade(nameof(LoadingAriaLabel), LoadingAriaLabel!, static s => s.LoadingAriaLabel, static (s, v) => s.LoadingAriaLabel = v);
        }

        if (LoadingText.HasValue())
        {
            bitSearchBox.TakeFromCascade(nameof(LoadingText), LoadingText, static s => s.LoadingText, static (s, v) => s.LoadingText = v);
        }

        if (MaxLength.HasValue)
        {
            bitSearchBox.TakeFromCascade(nameof(MaxLength), MaxLength.Value, static s => s.MaxLength, static (s, v) => s.MaxLength = v);
        }

        if (MaxSuggestCount.HasValue)
        {
            bitSearchBox.TakeFromCascade(nameof(MaxSuggestCount), MaxSuggestCount.Value, static s => s.MaxSuggestCount, static (s, v) => s.MaxSuggestCount = v);
        }

        if (MinSuggestTriggerChars.HasValue)
        {
            bitSearchBox.TakeFromCascade(nameof(MinSuggestTriggerChars), MinSuggestTriggerChars.Value, static s => s.MinSuggestTriggerChars, static (s, v) => s.MinSuggestTriggerChars = v);
        }

        if (MinSuggestTriggerCharsText.HasValue())
        {
            bitSearchBox.TakeFromCascade(nameof(MinSuggestTriggerCharsText), MinSuggestTriggerCharsText, static s => s.MinSuggestTriggerCharsText, static (s, v) => s.MinSuggestTriggerCharsText = v);
        }

        if (Modeless.HasValue)
        {
            bitSearchBox.TakeFromCascade(nameof(Modeless), Modeless.Value, static s => s.Modeless, static (s, v) => s.Modeless = v);
        }

        if (NoBorder.HasValue)
        {
            bitSearchBox.TakeFromCascade(nameof(NoBorder), NoBorder.Value, static s => s.NoBorder, static (s, v) => s.NoBorder = v);
        }

        if (NoClearOnEscape.HasValue)
        {
            bitSearchBox.TakeFromCascade(nameof(NoClearOnEscape), NoClearOnEscape.Value, static s => s.NoClearOnEscape, static (s, v) => s.NoClearOnEscape = v);
        }

        if (NoResultsText.HasValue())
        {
            bitSearchBox.TakeFromCascade(nameof(NoResultsText), NoResultsText, static s => s.NoResultsText, static (s, v) => s.NoResultsText = v);
        }

        if (NoWrapNavigation.HasValue)
        {
            bitSearchBox.TakeFromCascade(nameof(NoWrapNavigation), NoWrapNavigation.Value, static s => s.NoWrapNavigation, static (s, v) => s.NoWrapNavigation = v);
        }

        if (Placeholder.HasValue())
        {
            bitSearchBox.TakeFromCascade(nameof(Placeholder), Placeholder, static s => s.Placeholder, static (s, v) => s.Placeholder = v);
        }

        if (SearchButtonAriaLabel.HasValue())
        {
            bitSearchBox.TakeFromCascade(nameof(SearchButtonAriaLabel), SearchButtonAriaLabel!, static s => s.SearchButtonAriaLabel, static (s, v) => s.SearchButtonAriaLabel = v);
        }

        if (SearchButtonIcon is not null)
        {
            bitSearchBox.TakeFromCascade(nameof(SearchButtonIcon), SearchButtonIcon, static s => s.SearchButtonIcon, static (s, v) => s.SearchButtonIcon = v);
        }

        if (SearchButtonIconName.HasValue())
        {
            bitSearchBox.TakeFromCascade(nameof(SearchButtonIconName), SearchButtonIconName, static s => s.SearchButtonIconName, static (s, v) => s.SearchButtonIconName = v);
        }

        if (SearchButtonText.HasValue())
        {
            bitSearchBox.TakeFromCascade(nameof(SearchButtonText), SearchButtonText, static s => s.SearchButtonText, static (s, v) => s.SearchButtonText = v);
        }

        if (SelectTextOnFocus.HasValue)
        {
            bitSearchBox.TakeFromCascade(nameof(SelectTextOnFocus), SelectTextOnFocus.Value, static s => s.SelectTextOnFocus, static (s, v) => s.SelectTextOnFocus = v);
        }

        if (ShowSearchButton.HasValue)
        {
            bitSearchBox.TakeFromCascade(nameof(ShowSearchButton), ShowSearchButton.Value, static s => s.ShowSearchButton, static (s, v) => s.ShowSearchButton = v);
        }

        if (ShowSuggestItemsOnFocus.HasValue)
        {
            bitSearchBox.TakeFromCascade(nameof(ShowSuggestItemsOnFocus), ShowSuggestItemsOnFocus.Value, static s => s.ShowSuggestItemsOnFocus, static (s, v) => s.ShowSuggestItemsOnFocus = v);
        }

        if (Size.HasValue)
        {
            bitSearchBox.TakeFromCascade(nameof(Size), Size.Value, static s => s.Size, static (s, v) => s.Size = v);
        }

        if (SpellCheck.HasValue)
        {
            bitSearchBox.TakeFromCascade(nameof(SpellCheck), SpellCheck.Value, static s => s.SpellCheck, static (s, v) => s.SpellCheck = v);
        }

        if (Styles is not null)
        {
            bitSearchBox.TakeFromCascade(nameof(Styles), Styles, static s => s.Styles, static (s, v) => s.Styles = v);
        }

        if (SuggestFailedText is not null)
        {
            bitSearchBox.TakeFromCascade(nameof(SuggestFailedText), SuggestFailedText, static s => s.SuggestFailedText, static (s, v) => s.SuggestFailedText = v);
        }

        if (SuggestIgnoreDiacritics.HasValue)
        {
            bitSearchBox.TakeFromCascade(nameof(SuggestIgnoreDiacritics), SuggestIgnoreDiacritics.Value, static s => s.SuggestIgnoreDiacritics, static (s, v) => s.SuggestIgnoreDiacritics = v);
        }

        if (SuggestFilterFunction is not null)
        {
            bitSearchBox.TakeFromCascade(nameof(SuggestFilterFunction), SuggestFilterFunction, static s => s.SuggestFilterFunction, static (s, v) => s.SuggestFilterFunction = v);
        }

        if (SuggestItemsAriaLabel.HasValue())
        {
            bitSearchBox.TakeFromCascade(nameof(SuggestItemsAriaLabel), SuggestItemsAriaLabel!, static s => s.SuggestItemsAriaLabel, static (s, v) => s.SuggestItemsAriaLabel = v);
        }

        if (ThrottleTime.HasValue)
        {
            bitSearchBox.TakeFromCascade(nameof(ThrottleTime), ThrottleTime.Value, static s => s.ThrottleTime, static (s, v) => s.ThrottleTime = v);
        }

        if (Trim.HasValue)
        {
            bitSearchBox.TakeFromCascade(nameof(Trim), Trim.Value, static s => s.Trim, static (s, v) => s.Trim = v);
        }

        if (Underlined.HasValue)
        {
            bitSearchBox.TakeFromCascade(nameof(Underlined), Underlined.Value, static s => s.Underlined, static (s, v) => s.Underlined = v);
        }
    }
}
