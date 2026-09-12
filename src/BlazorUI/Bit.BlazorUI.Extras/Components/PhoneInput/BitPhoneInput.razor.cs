using System.Text;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// BitPhoneInput is an input component for entering phone numbers with a searchable
/// country selector that shows the flag and the dialing code of each country.
/// <para>
/// The two-way bound <see cref="BitInputBase{T}.Value"/> holds the full phone number in
/// the E.164 form ("+[code][number]"), which makes it convenient to bind a single field in
/// a form. The local part (the digits typed in the input box) is exposed separately through
/// the two-way bound <see cref="Number"/> parameter, and the dialing code comes from the
/// selected <see cref="Country"/>.
/// </para>
/// </summary>
public partial class BitPhoneInput : BitTextInputBase<string?>
{
    private bool _hasFocus;
    private bool _hasOpened;
    private bool _dropdownKeysWired;
    private bool _searchBoxKeysWired;
    private bool _internalIsOpenChange;
    private bool _pendingCalloutToggle;
    private string? _searchText;
    private string? _announcement;
    private bool _announcementMarker;
    private int _activeIndex = -1;
    private int _lastScrolledIndex = -1;

    // Reconciliation state between the full Value and its parts (Number + Country).
    // _lastValue mirrors the last full value the component produced so an external Value
    // change (e.g. from a form) can be told apart from the component's own updates.
    private string? _lastValue;
    private string? _lastNumber;
    private string? _lastCountryIso;
    private string? _lastErrorMessage;

    // Type-ahead state of the country list, used when there is no search box to type into.
    private string _typeAhead = string.Empty;
    private DateTimeOffset _typeAheadAt;
    private static readonly TimeSpan _typeAheadTimeout = TimeSpan.FromMilliseconds(1000);

    private string _labelId = string.Empty;
    private string _errorId = string.Empty;
    private string _inputId = string.Empty;
    private string _descriptionId = string.Empty;
    private string _searchId = string.Empty;
    private string _listId = string.Empty;
    private string _calloutId = string.Empty;
    private string _overlayId = string.Empty;
    private string _dropdownId = string.Empty;
    private string _fieldGroupId = string.Empty;
    private string _scrollContainerId = string.Empty;

    private List<BitCountry> _allItems = [];
    private List<BitCountry> _viewItems = [];

    // The name of each country of _allItems with its diacritics folded away, and the same name cut
    // into words. Both are read once per country per keystroke, so they are computed with the list
    // instead of being rebuilt 240 times for every letter typed into the search box.
    private string[] _foldedNames = [];
    private string[][] _foldedWords = [];
    private ElementReference _searchInputRef;
    private ElementReference _dropdownButtonRef;
    private ICollection<BitCountry>? _lastCountries;
    private ICollection<BitCountry>? _lastExcludeCountries;
    private ICollection<BitCountry>? _lastPreferredCountries;
    private DotNetObjectReference<BitPhoneInput>? _dotnetObj;

    // The search term the cached _viewItems were produced for. GetFilteredCountries() runs on every
    // render while the callout is open, so the filtered list is only rebuilt when the term (or the
    // source list behind it) actually changes.
    private bool _viewItemsValid;
    private string? _viewItemsKey;

    // Keys whose default browser behavior must be suppressed. These are applied through a
    // deterministic JS keydown listener (see BitExtrasSetPreventKeys) so the suppression
    // always matches the key of the current event instead of lagging one event behind, as
    // Blazor's stateful `@onkeydown:preventDefault` binding would.
    // Home and End are deliberately absent from the search box set: inside a text field they move
    // the caret, which is what a user typing a search term expects them to keep doing.
    private static readonly string[] _searchBoxKeys = ["ArrowDown", "ArrowUp", "PageDown", "PageUp", "Enter"];
    private static readonly string[] _dropdownClosedKeys = ["ArrowDown", "ArrowUp", "Home", "End"];
    private static readonly string[] _dropdownOpenKeys = ["ArrowDown", "ArrowUp", "PageDown", "PageUp", "Home", "End", "Enter", " ", "Spacebar"];

    private const int PageNavigationSize = 10;

    private const int DialCodeExactRank = 3;
    private const int DialCodeStartsRank = 4;
    private const int DialCodeContainsRank = 7;

    // The flag file of a country, keyed by its ISO code. Every open callout renders one image per
    // country it offers, and the url of each of them is otherwise built from scratch on every render.
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _flagUrls = new();



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Shows the pattern the number is currently formatted with as the placeholder of the number input,
    /// so the shape a country expects is offered before anything is typed - "(###) ###-####" for a
    /// number formatted by that mask. It only fills in for a missing placeholder: an explicit
    /// <see cref="Placeholder"/> is always kept, and a country with no pattern of its own shows none.
    /// </summary>
    [Parameter] public bool AutoPlaceholder { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitPhoneInput.
    /// </summary>
    [Parameter] public BitPhoneInputClassStyles? Classes { get; set; }

    /// <summary>
    /// The aria-label of the clear button of the number input.
    /// </summary>
    [Parameter] public string? ClearButtonAriaLabel { get; set; }

    /// <summary>
    /// The icon of the clear button of the number input.
    /// Takes precedence over <see cref="ClearButtonIconName"/> when both are set.
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// </summary>
    [Parameter] public BitIconInfo? ClearButtonIcon { get; set; }

    /// <summary>
    /// The icon name of the clear button of the number input from the Fluent UI icon set.
    /// For external icon libraries, use <see cref="ClearButtonIcon"/> instead.
    /// </summary>
    [Parameter] public string? ClearButtonIconName { get; set; }

    /// <summary>
    /// The custom template for the clear button of the number input.
    /// </summary>
    [Parameter] public RenderFragment? ClearButtonTemplate { get; set; }

    /// <summary>
    /// The general color of the phone input.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// The list of the countries to show in the country dropdown. Defaults to <see cref="BitCountries.All"/>.
    /// </summary>
    [Parameter] public ICollection<BitCountry> Countries { get; set; } = BitCountries.All;

    /// <summary>
    /// The currently selected country of the phone input. (two-way bound)
    /// </summary>
    [Parameter, TwoWayBound]
    public BitCountry? Country { get; set; }

    /// <summary>
    /// The name of the hidden field carrying the ISO 3166-1 alpha-2 code of the selected country, for a
    /// plain HTML form that stores the country beside the number. The number itself is posted through
    /// the field named by <see cref="BitInputBase{T}.Name"/>.
    /// </summary>
    [Parameter] public string? CountryName { get; set; }

    /// <summary>
    /// The default selected country to be initially used when the Country parameter is not set.
    /// </summary>
    [Parameter] public BitCountry? DefaultCountry { get; set; }

    /// <summary>
    /// The description shown under the phone input, for the hint a label is too short to carry ("We
    /// will text you a code"). It is tied to the number input through aria-describedby, so a screen
    /// reader reads it with the field rather than as a stray line of text.
    /// </summary>
    [Parameter] public string? Description { get; set; }

    /// <summary>
    /// The custom template for the description of the phone input, taking the place of
    /// <see cref="Description"/> and tied to the number input the same way.
    /// </summary>
    [Parameter] public RenderFragment? DescriptionTemplate { get; set; }

    /// <summary>
    /// Determines the allowed drop directions of the country dropdown callout.
    /// </summary>
    [Parameter] public BitDropDirection DropDirection { get; set; } = BitDropDirection.TopAndBottom;

    /// <summary>
    /// The aria-label of the country dropdown button.
    /// </summary>
    [Parameter] public string? DropdownAriaLabel { get; set; }

    /// <summary>
    /// The placeholder text of the country dropdown when no country is selected.
    /// </summary>
    [Parameter] public string? DropdownPlaceholder { get; set; }

    /// <summary>
    /// The custom template for the content of the country dropdown button, receiving the selected country.
    /// </summary>
    [Parameter] public RenderFragment<BitCountry?>? DropdownTemplate { get; set; }

    /// <summary>
    /// Sets the enterkeyhint html attribute of the number input, which decides the label of the return
    /// key of an on-screen keyboard ("done", "next", "send", ...).
    /// </summary>
    [Parameter] public string? EnterKeyHint { get; set; }

    /// <summary>
    /// The error message shown under the phone input, which also puts the field in its invalid state -
    /// for a rejection that comes from a server or from a rule no data annotation can express. Inside an
    /// EditContext the validation of the model is what decides the state instead, and its message is
    /// rendered by a ValidationMessage of its own.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The custom template for the error message of the phone input, taking the place of
    /// <see cref="ErrorMessage"/> and putting the field in the same invalid state.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public RenderFragment? ErrorMessageTemplate { get; set; }

    /// <summary>
    /// The countries to leave out of the country dropdown, which is how a list built from
    /// <see cref="Countries"/> is trimmed without being written out in full. They are also left out of
    /// the dialing-code lookup, so a number typed with the code of an excluded country is kept whole
    /// instead of selecting it.
    /// </summary>
    [Parameter] public ICollection<BitCountry>? ExcludeCountries { get; set; }

    /// <summary>
    /// The url of the flag image of a country, replacing the flags that ship with the library - the
    /// place to point at a sprite, a CDN or an icon set of your own. Returning null or an empty string
    /// leaves that country with the built-in flag.
    /// </summary>
    [Parameter] public Func<BitCountry, string?>? FlagUrlSelector { get; set; }

    /// <summary>
    /// Renders the phone input to fill 100% of its container width.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FullWidth { get; set; }

    /// <summary>
    /// Sets the inputmode html attribute of the number input, which decides the on-screen keyboard it
    /// asks for. A tel input already asks for a telephone keypad, so this is only for the cases that
    /// need another one.
    /// </summary>
    [Parameter] public BitInputMode? InputMode { get; set; }

    /// <summary>
    /// Renders the phone input in an invalid state without going through the validation of an EditContext.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Invalid { get; set; }

    /// <summary>
    /// Determines the opening state of the country dropdown callout. (two-way bound)
    /// </summary>
    [Parameter, TwoWayBound]
    [CallOnSet(nameof(OnSetIsOpen))]
    public bool IsOpen { get; set; }

    /// <summary>
    /// The custom template for each country of the dropdown list.
    /// </summary>
    [Parameter] public RenderFragment<BitCountry>? ItemTemplate { get; set; }

    /// <summary>
    /// The label of the phone input shown above the field.
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// The custom template for the label of the phone input.
    /// </summary>
    [Parameter] public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// The maximum height of the country dropdown callout in pixels.
    /// </summary>
    [Parameter] public int? MaxHeight { get; set; }

    /// <summary>
    /// The pattern the local number is formatted with as the user types, where every '#' is a digit
    /// slot and every other character is a literal that is inserted for them - "(###) ###-####"
    /// turns 4155550123 into "(415) 555-0123". Digits beyond the last slot are appended unformatted,
    /// so a number longer than the pattern is never cut short. The formatting is display only: the
    /// separators never reach <see cref="BitInputBase{T}.Value"/>, which stays in the E.164 form.
    /// <para>
    /// Use <see cref="MaskSelector"/> instead to format each country by a pattern of its own.
    /// </para>
    /// </summary>
    [Parameter] public string? Mask { get; set; }

    /// <summary>
    /// The pattern to format the local number with for a given country, taking precedence over
    /// <see cref="Mask"/> whenever it returns one. It is what a per-country format needs, since the
    /// national conventions of one country say nothing about those of the next.
    /// </summary>
    [Parameter] public Func<BitCountry?, string?>? MaskSelector { get; set; }

    /// <summary>
    /// Determines the maximum number of characters allowed in the number input.
    /// </summary>
    [Parameter] public int MaxLength { get; set; } = -1;

    /// <summary>
    /// Removes the border of the phone input.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool NoBorder { get; set; }

    /// <summary>
    /// Hides the dialing code of the selected country in the country dropdown button.
    /// </summary>
    [Parameter] public bool NoDialCode { get; set; }

    /// <summary>
    /// Removes the country dropdown, so the country of the phone input can only be set through its parameters.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool NoDropdown { get; set; }

    /// <summary>
    /// Hides the flag images of the countries in the dropdown button and in the dropdown list.
    /// </summary>
    [Parameter] public bool NoFlags { get; set; }

    /// <summary>
    /// Stops the focus from moving to the number input once a country has been picked, for a form that
    /// would rather decide itself where the focus goes next.
    /// </summary>
    [Parameter] public bool NoFocusOnSelect { get; set; }

    /// <summary>
    /// The message to show when the search result of the country dropdown is empty.
    /// </summary>
    [Parameter] public string? NoResultsMessage { get; set; }

    /// <summary>
    /// The custom template to show when the search result of the country dropdown is empty.
    /// </summary>
    [Parameter] public RenderFragment? NoResultsTemplate { get; set; }

    /// <summary>
    /// Hides the search box of the country dropdown.
    /// </summary>
    [Parameter] public bool NoSearchBox { get; set; }

    /// <summary>
    /// Stops the keyboard navigation of the country list from wrapping around at its two ends.
    /// </summary>
    [Parameter] public bool NoWrapNavigation { get; set; }

    /// <summary>
    /// The local phone number (the digits typed in the input box, without the country dialing code). (two-way bound)
    /// <para>
    /// This is the part the user edits, exactly as the input shows it - separators and all, including
    /// the ones a <see cref="Mask"/> inserts. The full phone number, reduced to its digits and carrying
    /// the dialing code of the selected <see cref="Country"/>, is exposed through the two-way bound
    /// <see cref="BitInputBase{T}.Value"/>.
    /// </para>
    /// </summary>
    [Parameter, TwoWayBound]
    public string? Number { get; set; }

    /// <summary>
    /// The callback that is invoked when the number input loses focus.
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnBlur { get; set; }

    /// <summary>
    /// The callback that is invoked when the clear button of the number input is clicked.
    /// </summary>
    [Parameter] public EventCallback OnClear { get; set; }

    /// <summary>
    /// The callback that is invoked when the number input is clicked.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// The callback that is invoked when the country dropdown callout closes.
    /// </summary>
    [Parameter] public EventCallback OnClose { get; set; }

    /// <summary>
    /// The callback that is invoked when the selected country changes.
    /// </summary>
    [Parameter] public EventCallback<BitCountry?> OnCountryChange { get; set; }

    /// <summary>
    /// The callback that is invoked when the Enter key is pressed in the number input.
    /// </summary>
    [Parameter] public EventCallback<KeyboardEventArgs> OnEnter { get; set; }

    /// <summary>
    /// The callback that is invoked when the Escape key is pressed in the number input.
    /// </summary>
    [Parameter] public EventCallback<KeyboardEventArgs> OnEscape { get; set; }

    /// <summary>
    /// The callback that is invoked when the number input receives focus.
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnFocus { get; set; }

    /// <summary>
    /// The callback that is invoked when focus moves into the phone input.
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnFocusIn { get; set; }

    /// <summary>
    /// The callback that is invoked when focus moves out of the phone input.
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnFocusOut { get; set; }

    /// <summary>
    /// The callback that is invoked on every key press in the number input.
    /// </summary>
    [Parameter] public EventCallback<KeyboardEventArgs> OnKeyDown { get; set; }

    /// <summary>
    /// The callback that is invoked when the country dropdown callout opens.
    /// </summary>
    [Parameter] public EventCallback OnOpen { get; set; }

    /// <summary>
    /// The callback that is invoked when the search text of the country dropdown changes.
    /// </summary>
    [Parameter] public EventCallback<string?> OnSearch { get; set; }

    /// <summary>
    /// The placeholder text of the number input.
    /// </summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>
    /// The countries to pin to the top of the country dropdown list. They also break the tie when a
    /// typed dialing code is shared by several countries (+1 for instance), which is otherwise
    /// decided by <see cref="BitCountry.Priority"/>.
    /// </summary>
    [Parameter] public ICollection<BitCountry>? PreferredCountries { get; set; }

    /// <summary>
    /// Shows the country dropdown as a full height panel on small screens instead of an inline callout.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Responsive { get; set; }

    /// <summary>
    /// The aria-label of the close button of the responsive panel of the country dropdown.
    /// </summary>
    [Parameter] public string? ResponsiveCloseButtonAriaLabel { get; set; }

    /// <summary>
    /// The icon of the close button of the responsive panel of the country dropdown.
    /// Takes precedence over <see cref="ResponsiveCloseIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? ResponsiveCloseIcon { get; set; }

    /// <summary>
    /// The icon name of the close button of the responsive panel of the country dropdown from the Fluent UI icon set.
    /// </summary>
    [Parameter] public string? ResponsiveCloseIconName { get; set; }

    /// <summary>
    /// The aria-label of the search box of the country dropdown. Falls back to the search box
    /// placeholder, then to a default English value, when not provided.
    /// </summary>
    [Parameter] public string? SearchBoxAriaLabel { get; set; }

    /// <summary>
    /// The placeholder text of the search box of the country dropdown.
    /// </summary>
    [Parameter] public string? SearchBoxPlaceholder { get; set; }

    /// <summary>
    /// Shows a clear button in the number input while it holds a value.
    /// </summary>
    [Parameter] public bool ShowClearButton { get; set; }

    /// <summary>
    /// The size of the phone input.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// Discards every character typed or pasted into the number input that cannot be part of a phone
    /// number, so only digits (and an optional leading plus sign) survive.
    /// </summary>
    [Parameter] public bool Strict { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitPhoneInput.
    /// </summary>
    [Parameter] public BitPhoneInputClassStyles? Styles { get; set; }

    /// <summary>
    /// The tooltip (title attribute) of the phone input.
    /// </summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// Renders the phone input with only a bottom border instead of a full one.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Underlined { get; set; }



    /// <summary>
    /// The full phone number including the dialing code of the selected country in the form of "+[code][number]".
    /// This is the same value exposed through the two-way bound <see cref="BitInputBase{T}.Value"/> and is kept for convenience.
    /// </summary>
    public string? FullNumber => CurrentValue;

    /// <summary>
    /// Opens the country dropdown callout.
    /// </summary>
    public async Task OpenAsync()
    {
        // Called from outside a Blazor event handler as well as from one, so the render the opened
        // callout needs is asked for here rather than left to the caller.
        if (IsEnabled is false || ReadOnly || NoDropdown || IsOpen) return;

        await OpenCallout();

        StateHasChanged();
    }

    /// <summary>
    /// Closes the country dropdown callout.
    /// </summary>
    public async Task CloseAsync()
    {
        if (IsOpen is false) return;

        await CloseCallout();

        StateHasChanged();
    }

    /// <summary>
    /// Selects the given country exactly as picking it in the callout would, so the same events fire.
    /// </summary>
    public Task SelectCountryAsync(BitCountry country) => HandleOnCountrySelect(country);

    /// <summary>
    /// Clears the number of the phone input, leaving the selected country as it is.
    /// </summary>
    public async Task ClearAsync()
    {
        if (IsEnabled is false || ReadOnly) return;

        // An empty field has nothing to clear, and reporting a clear that changed nothing would have a
        // consumer react to a number that was never there.
        if (Number.HasNoValue()) return;

        // AssignNumber returns false for a one-way controlled Number (set without NumberChanged).
        // In that case the field cannot drop what it shows, so reporting a clear that never happened
        // would have a consumer react to a number that is still there (see HandleOnCountrySelect).
        if (await AssignNumber(null) is false) return;

        await UpdateValueFromParts();
        await OnClear.InvokeAsync();
    }

    /// <summary>
    /// Sets the local number of the phone input, laid out over the pattern of the selected country the
    /// same way typing it would be, and recomposes the full value from it. A number carrying an
    /// international prefix ('+' or its "00" equivalent) selects the country that owns its dialing code,
    /// exactly as pasting it into the field does.
    /// </summary>
    public async Task SetNumberAsync(string? number)
    {
        if (IsEnabled is false || ReadOnly) return;

        var (country, local) = ParseFullNumber(NormalizeTyped(number));

        if (country is not null && country.Iso2 != Country?.Iso2 && await AssignCountry(country))
        {
            await AssignNumber(FormatNumber(local));

            await OnCountryChange.InvokeAsync(country);
        }
        else
        {
            await AssignNumber(FormatNumber(local));
        }

        await UpdateValueFromParts();
    }



    // Keeps only what can appear in a phone number: the digits, plus the leading '+' of an
    // international number when it is asked for. Everything a user types or pastes as a separator
    // (spaces, hyphens, parentheses, dots) is dropped so the composed Value stays E.164.
    private static string KeepDigits(string? text, bool keepLeadingPlus = false)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        var builder = new StringBuilder(text.Length);

        foreach (var c in text)
        {
            if (char.IsAsciiDigit(c))
            {
                builder.Append(c);
            }
            else if (keepLeadingPlus && c == '+' && builder.Length == 0)
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }

    // The pattern the local number is currently formatted with, or null when it is left alone.
    private string? CurrentMask => MaskSelector?.Invoke(Country) ?? Mask;

    // Lays the digits of the local number out over the pattern: every '#' takes the next digit and
    // every other character is copied as it is, but only while there are still digits to place, so a
    // half typed number never grows a trailing separator. Digits left over once the pattern runs out
    // are appended as they are, so a number longer than the pattern is formatted as far as the
    // pattern goes and kept whole beyond it.
    private static string ApplyMask(string digits, string mask)
    {
        if (digits.Length == 0) return digits;

        var builder = new StringBuilder(mask.Length + digits.Length);
        var next = 0;

        foreach (var c in mask)
        {
            if (next >= digits.Length) break;

            if (c == '#')
            {
                builder.Append(digits[next++]);
            }
            else
            {
                builder.Append(c);
            }
        }

        if (next < digits.Length)
        {
            builder.Append(digits, next, digits.Length - next);
        }

        return builder.ToString();
    }

    // Reads the digits of the local number back out of a text the pattern has already been laid over.
    // Walking the text and the pattern side by side is what tells a character the pattern inserted
    // apart from one the user typed, which the digits alone cannot say for a pattern holding a literal
    // digit ("0## ### ####"): laying such a pattern over its own result would otherwise keep the '0'
    // the last formatting inserted and grow the number by a digit per keystroke - until the field
    // reads "00...", the international call prefix of most of the world, which moves the country with it.
    private static string StripMask(string text, string mask)
    {
        var builder = new StringBuilder(text.Length);
        var maskIndex = 0;
        var textIndex = 0;

        while (textIndex < text.Length)
        {
            var c = text[textIndex];

            // Past the end of the pattern there is nothing left but the digits that did not fit in it.
            if (maskIndex >= mask.Length)
            {
                if (char.IsAsciiDigit(c))
                {
                    builder.Append(c);
                }

                textIndex++;
            }
            else if (mask[maskIndex] == '#')
            {
                // A separator where the pattern wants a digit is one the text carries of its own, so it
                // is dropped without spending the placeholder it was found on.
                if (char.IsAsciiDigit(c))
                {
                    builder.Append(c);
                    maskIndex++;
                }

                textIndex++;
            }
            else if (c == mask[maskIndex])
            {
                // A literal of the pattern, in the very place the pattern puts it: it belongs to the
                // layout rather than to the number.
                textIndex++;
                maskIndex++;
            }
            else if (char.IsAsciiDigit(c))
            {
                // The text has not been laid out this far yet, so the literal it is missing is stepped
                // over and the same character is measured against what the pattern has after it.
                maskIndex++;
            }
            else
            {
                textIndex++;
            }
        }

        return builder.ToString();
    }

    // The local number as the input shows it: formatted by the current pattern when there is one,
    // and left exactly as it is when there is not. Laying a pattern over a number it has already been
    // laid over is what typing into the field does on every keystroke, so the pattern is peeled off
    // again before it is applied.
    private string? FormatNumber(string? number)
    {
        var mask = CurrentMask;

        if (mask.HasNoValue() || string.IsNullOrEmpty(number)) return number;

        // A number that still carries an international prefix belongs to no country of the list, so no
        // national pattern can describe it - unless the pattern writes that prefix itself, where the
        // '+' is a literal of the pattern rather than one the number came with.
        if (mask!.Contains('+') is false && KeepDigits(number, keepLeadingPlus: true).StartsWith('+')) return number;

        var digits = StripMask(number!, mask!);

        if (digits.Length == 0) return number;

        return ApplyMask(digits, mask!);
    }

    // Builds the full phone number from the selected country and the local number. Both the dialing
    // code and the local part are reduced to digits (any separator typed or pasted into the input is
    // dropped) so the result follows E.164. Returns null when there is no local number so an empty
    // input maps to an empty value.
    private static string? ComposeFullNumber(BitCountry? country, string? number)
    {
        var local = KeepDigits(number, keepLeadingPlus: true);

        if (local.Length == 0 || local == "+") return null;

        // A local part that still carries a '+' is a full international number whose dialing code no
        // country in the list claims, so it is already the whole value and must not be prefixed again.
        if (local[0] == '+') return local;

        if (country is null) return local;

        return $"+{country.DigitsCode}{local}";
    }

    // Splits a full phone number into its country and local-number parts. A leading '+' (or its
    // "00" international call-prefix equivalent) triggers a dialing-code lookup (longest matching
    // code wins; the current country is preferred to disambiguate shared codes like +1, then the
    // preferred countries, then BitCountry.Priority). Without such a prefix the whole input is
    // treated as the local number and the current country is kept.
    private (BitCountry? Country, string? Number) ParseFullNumber(string? full)
    {
        if (string.IsNullOrWhiteSpace(full)) return (Country, null);

        var text = full.Trim();

        var digits = KeepDigits(text);

        if (text.StartsWith('+') is false)
        {
            // "00" is the international call prefix of most of the world, so a number that starts
            // with it is read as an international one, minus that prefix.
            // Anything else is a national number, which is handed back exactly as it was written:
            // the separators a user types (or a mask inserts) belong to the field, and only the
            // composed value is reduced to its digits.
            if (digits.StartsWith("00", StringComparison.Ordinal) is false) return (Country, text);

            digits = digits[2..];
        }

        BitCountry? best = null;
        var bestLength = 0;
        var bestRank = int.MinValue;
        foreach (var country in _allItems)
        {
            // A country can answer to several codes (+1-809, +1-829 and +1-849 are all the Dominican
            // Republic), so every one of them is measured against the number rather than the main one.
            foreach (var code in country.DigitsCodes)
            {
                if (code.Length == 0 || digits.StartsWith(code, StringComparison.Ordinal) is false) continue;

                var rank = GetDialCodeRank(country);

                // A longer (more specific) code always wins; among equally specific ones the ranking
                // decides, so an ambiguous code (+1, +7) resolves to the country that owns it rather
                // than to whichever one happens to come first in the list.
                if (code.Length > bestLength || (code.Length == bestLength && rank > bestRank))
                {
                    best = country;
                    bestLength = code.Length;
                    bestRank = rank;
                }
            }
        }

        // No country in the list claims this dialing code, so the number is kept whole (with its
        // international prefix) instead of being silently re-prefixed with the current country's code.
        return best is null ? (Country, digits.Length > 0 ? $"+{digits}" : null) : (best, digits[bestLength..]);
    }

    // Orders the countries that share a dialing code: the current selection first, so an ambiguous
    // code never moves the selection on its own, then the preferred countries in the order given,
    // then the priority carried by the country itself.
    private int GetDialCodeRank(BitCountry country)
    {
        if (Country is not null && country.Iso2 == Country.Iso2) return int.MaxValue;

        if (PreferredCountries is not null)
        {
            var index = 0;
            foreach (var preferred in PreferredCountries)
            {
                if (preferred.Iso2 == country.Iso2) return int.MaxValue - 1 - index;
                index++;
            }
        }

        return country.Priority;
    }



    [JSInvokable("CloseCallout")]
    public async Task _CloseCalloutBeforeAnotherCalloutIsOpened()
    {
        if (IsOpen is false) return;

        // The JS side has already hidden this callout to make room for another one, so the state is
        // brought in line without toggling it a second time.
        if (await AssignIsOpenInternal(false) is false) return;

        ResetCalloutState();

        await ClearSearchBoxElement();

        if (_dropdownKeysWired)
        {
            await _js.BitExtrasSetPreventKeys(_dropdownButtonRef, _dropdownClosedKeys);
        }

        // The callout this component had open is gone to make room for another one's, so whatever the
        // focus was on inside it is gone along with it.
        SetHasFocus(false);

        await OnClose.InvokeAsync();

        await InvokeAsync(StateHasChanged);
    }



    protected override string RootElementClass => "bit-phi";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => FullWidth ? "bit-phi-fwd" : string.Empty);

        ClassBuilder.Register(() => NoBorder ? "bit-phi-nbd" : string.Empty);

        ClassBuilder.Register(() => Underlined ? "bit-phi-und" : string.Empty);

        ClassBuilder.Register(() => NoDropdown ? "bit-phi-nod" : string.Empty);

        ClassBuilder.Register(() => HasError ? "bit-inv" : string.Empty);

        ClassBuilder.Register(() => IsEnabled && Required ? "bit-phi-req" : string.Empty);

        ClassBuilder.Register(() => _hasFocus ? "bit-phi-fcs" : string.Empty);

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-phi-sm",
            BitSize.Medium => "bit-phi-md",
            BitSize.Large => "bit-phi-lg",
            _ => string.Empty
        });

        ClassBuilder.Register(() => Color switch
        {
            BitColor.Primary => "bit-phi-pri",
            BitColor.Secondary => "bit-phi-sec",
            BitColor.Tertiary => "bit-phi-ter",
            BitColor.Info => "bit-phi-inf",
            BitColor.Success => "bit-phi-suc",
            BitColor.Warning => "bit-phi-wrn",
            BitColor.SevereWarning => "bit-phi-swr",
            BitColor.Error => "bit-phi-err",
            _ => string.Empty
        });
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override void OnInitialized()
    {
        _labelId = $"BitPhoneInput-{UniqueId}-label";
        _errorId = $"BitPhoneInput-{UniqueId}-error";
        _inputId = $"BitPhoneInput-{UniqueId}-input";
        _descriptionId = $"BitPhoneInput-{UniqueId}-description";
        _searchId = $"BitPhoneInput-{UniqueId}-search";
        _listId = $"BitPhoneInput-{UniqueId}-list";
        _dropdownId = $"BitPhoneInput-{UniqueId}-dropdown";
        _fieldGroupId = $"BitPhoneInput-{UniqueId}-field-group";
        _calloutId = $"BitPhoneInput-{UniqueId}-callout";
        _overlayId = $"BitPhoneInput-{UniqueId}-overlay";
        _scrollContainerId = $"BitPhoneInput-{UniqueId}-scroll-container";

        if (CountryHasBeenSet is false && DefaultCountry is not null)
        {
            Country = DefaultCountry;
        }

        SetDefaultValue();

        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        // Materialize the country list only when the source of it actually changes. The list is read
        // on every render while the callout is open, so without this cache the default country set
        // (BitCountries.All) would be copied into a new list of ~240 items each cycle.
        if (ReferenceEquals(_lastCountries, Countries) is false ||
            ReferenceEquals(_lastExcludeCountries, ExcludeCountries) is false ||
            ReferenceEquals(_lastPreferredCountries, PreferredCountries) is false)
        {
            _lastCountries = Countries;
            _lastExcludeCountries = ExcludeCountries;
            _lastPreferredCountries = PreferredCountries;
            _allItems = BuildItems();
            _foldedNames = [.. _allItems.Select(c => Fold(c.Name))];
            _foldedWords = [.. _foldedNames.Select(n => n.Split([' ', '-'], StringSplitOptions.RemoveEmptyEntries))];
            _viewItemsValid = false;
        }

        // A message that says the value was rejected has to be heard as well as seen, and it is written
        // into a part of the page a screen reader has already read past by the time it appears.
        if (ErrorMessage != _lastErrorMessage)
        {
            _lastErrorMessage = ErrorMessage;

            if (ErrorMessage.HasValue())
            {
                Announce(ErrorMessage);
            }
        }

        base.OnParametersSet();
    }

    protected override async Task OnParametersSetAsync()
    {
        await ReconcileValueAndParts();

        await base.OnParametersSetAsync();
    }

    // The countries in the order the dropdown shows them: the preferred ones first, in the order they
    // were given, then the rest in the order of the Countries list.
    private List<BitCountry> BuildItems()
    {
        var countries = Countries;

        if (ExcludeCountries is not null && ExcludeCountries.Count > 0)
        {
            var excluded = new HashSet<string>(ExcludeCountries.Select(c => c.Iso2), StringComparer.OrdinalIgnoreCase);
            countries = [.. countries.Where(c => excluded.Contains(c.Iso2) is false)];
        }

        if (PreferredCountries is null || PreferredCountries.Count == 0)
        {
            // A copy even when the source already is a List: _foldedNames and _foldedWords are read
            // by the index of _allItems, and a list the caller keeps a reference to could otherwise
            // grow underneath them without the parameter reference ever changing.
            return [.. countries];
        }

        var preferred = new List<BitCountry>();
        var preferredIso = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var country in PreferredCountries)
        {
            // Only the countries the dropdown actually offers can be pinned to the top of it.
            var match = countries.FirstOrDefault(c => c.Iso2.Equals(country.Iso2, StringComparison.OrdinalIgnoreCase));
            if (match is null || preferredIso.Add(match.Iso2) is false) continue;

            preferred.Add(match);
        }

        return [.. preferred, .. countries.Where(c => preferredIso.Contains(c.Iso2) is false)];
    }

    // Keeps the full Value and the (Number, Country) parts in sync when parameters change.
    // An external Value change (e.g. a form or another field setting the bound value) is parsed
    // back into the parts; otherwise the parts are the source of truth and the Value is recomposed
    // from them. The parts and the value are always propagated through their two-way callbacks
    // (AssignNumber/AssignCountry/SetCurrentValueAsString) so every bound parameter stays in sync.
    private async Task ReconcileValueAndParts()
    {
        if (CurrentValue != _lastValue)
        {
            // The consumer changed the full Value, so derive the parts from it and notify the
            // Number/Country bindings about the parsed values.
            var (country, number) = ParseFullNumber(CurrentValue);

            // The country is adopted before the number is laid out, because the pattern the number is
            // laid out over can be the one the newly adopted country brings with it: formatting first
            // would show the new number under the pattern of the country it is replacing
            // (see HandleOnStringValueChangeAsync).
            if (country is not null)
            {
                await AssignCountry(country);
            }

            await AssignNumber(FormatNumber(number));

            _lastValue = CurrentValue;
            _lastNumber = Number;
            _lastCountryIso = Country?.Iso2;
        }
        else if (Number != _lastNumber || Country?.Iso2 != _lastCountryIso)
        {
            // The parts changed (e.g. external Number/Country or the applied DefaultCountry),
            // so recompose the full Value and notify the Value binding.
            await UpdateValueFromParts();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _dotnetObj = DotNetObjectReference.Create(this);
        }

        // Wire up deterministic key suppression once the elements exist. Done on every render
        // rather than only on the first one, because the two elements it listens on come and go
        // with NoDropdown and NoSearchBox, and a search box that appeared later would otherwise
        // let its navigation keys move the caret as well as the selection.
        await WirePreventKeys();

        // The callout is shown and positioned only once the render that filled its list has reached
        // the DOM, so the country list can stay out of the document until the first opening while the
        // callout is still measured against its real content.
        if (_pendingCalloutToggle)
        {
            _pendingCalloutToggle = false;

            // A callout opened through the IsOpen parameter before the first render had no country
            // list to point at yet, so the active option is settled here instead.
            var activeIndexSettled = false;
            if (IsOpen && _activeIndex < 0)
            {
                ResetActiveIndexToSelection();

                activeIndexSettled = true;
            }

            await ToggleCallout();

            // The open list is driven from whichever element carries aria-activedescendant, so the focus
            // is put on it: the search box when there is one, and the button itself when there is not -
            // a callout opened through the IsOpen parameter would otherwise leave the keys nowhere to go.
            if (IsOpen && NoDropdown is false)
            {
                try
                {
                    if (NoSearchBox)
                    {
                        await _dropdownButtonRef.FocusAsync();
                    }
                    else
                    {
                        await _searchInputRef.FocusAsync();
                    }
                }
                catch (JSException) { } // the element might not be ready/visible yet
            }

            // The active option was decided after the render that got here, so what names it - the
            // active class on the option and the aria-activedescendant pointing at it - is asked for
            // again instead of waiting for the next render to come along.
            if (activeIndexSettled)
            {
                StateHasChanged();
            }
        }

        // Keep the active option visible during keyboard navigation. Done after render so
        // the option element is guaranteed to exist and the callout is laid out.
        if (IsOpen && _activeIndex >= 0 && _activeIndex != _lastScrolledIndex)
        {
            _lastScrolledIndex = _activeIndex;
            await _js.BitExtrasScrollOptionIntoView(GetOptionId(_activeIndex));
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    // Keeps the keydown listeners of the two elements that navigate the country list in step with
    // the elements themselves, and remembers which ones were wired so disposal can find them again
    // whatever the parameters say by then.
    private async Task WirePreventKeys()
    {
        var wantsDropdown = NoDropdown is false;
        var wantsSearchBox = wantsDropdown && NoSearchBox is false;

        if (wantsDropdown)
        {
            if (_dropdownKeysWired is false)
            {
                _dropdownKeysWired = true;
                await _js.BitExtrasSetPreventKeys(_dropdownButtonRef, IsOpen ? _dropdownOpenKeys : _dropdownClosedKeys);
            }
        }
        else if (_dropdownKeysWired)
        {
            _dropdownKeysWired = false;
            await _js.BitExtrasDisposePreventKeys(_dropdownButtonRef);
        }

        if (wantsSearchBox)
        {
            if (_searchBoxKeysWired is false)
            {
                _searchBoxKeysWired = true;
                await _js.BitExtrasSetPreventKeys(_searchInputRef, _searchBoxKeys);
            }
        }
        else if (_searchBoxKeysWired)
        {
            _searchBoxKeysWired = false;
            await _js.BitExtrasDisposePreventKeys(_searchInputRef);
        }
    }

    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out string? result, [NotNullWhen(false)] out string? parsingErrorMessage)
    {
        result = value;
        parsingErrorMessage = null;
        return true;
    }



    // The size of the control is driven by CSS variables, and the callout is rendered outside the
    // root element, so the class carrying them is repeated on it: without that the rows and the
    // flags of the list would be left with an undefined height.
    private string SizeClass => Size switch
    {
        BitSize.Small => "bit-phi-sm",
        BitSize.Large => "bit-phi-lg",
        _ => "bit-phi-md"
    };

    private bool HasLabel => Label.HasValue() || LabelTemplate is not null;

    // aria-labelledby takes precedence over aria-label, so pointing at the visible label while a name of
    // its own was given would quietly throw that name away. The visible label keeps naming the input
    // through the for/id pair either way, which is what the label element is for.
    private string? LabelledBy => HasLabel && AriaLabel.HasNoValue() ? _labelId : null;

    private bool HasDescription => Description.HasValue() || DescriptionTemplate is not null;

    private bool HasErrorMessage => ErrorMessage.HasValue() || ErrorMessageTemplate is not null;

    // The invalid state the consumer forces, either as a flag or by handing the field a message to show.
    private bool HasError => Invalid || HasErrorMessage;

    // The accessible name of the country selector. It carries the selected country, because an
    // aria-label replaces the content of the button for a screen reader: without the name in it, a
    // selector showing only a flag (NoDialCode) would be announced as "select country" and nothing
    // else, whatever country it is actually on.
    private string DropdownLabel => DropdownAriaLabel
                                 ?? (Country is not null
                                        ? $"Country: {Country.Name}"
                                        : DropdownPlaceholder ?? "Select country");

    private bool ShowClear => ShowClearButton && Number.HasValue();

    // The placeholder of a field that has none of its own, which is the pattern the number is laid out
    // over: it says the shape the country expects before a single digit has been typed.
    private string? InputPlaceholder => Placeholder ?? (AutoPlaceholder ? CurrentMask : null);

    private string? InputModeValue => InputMode?.ToString().ToLower();

    // What is written under the field is what describes it, so both lines are pointed at rather than
    // one of them. A value the consumer put on the input itself is kept: a field with a description of
    // its own would otherwise lose it the moment the component has anything to reference.
    private string? AriaDescribedBy
    {
        get
        {
            if (HasErrorMessage is false && HasDescription is false) return GetInputAttribute("aria-describedby");

            var ids = string.Join(' ', new[]
            {
                GetInputAttribute("aria-describedby"),
                HasErrorMessage ? _errorId : null,
                HasDescription ? _descriptionId : null
            }.Where(id => id.HasValue()));

            return ids.HasValue() ? ids : null;
        }
    }

    private string? GetInputAttribute(string name)
    {
        return InputHtmlAttributes is not null && InputHtmlAttributes.TryGetValue(name, out var value)
                ? value?.ToString()
                : null;
    }

    // The forced invalid state and the one the EditContext produces end up on the same attribute, and
    // the base class writes the latter into the splatted attributes, so the value of the consumer is
    // read back here instead of being overwritten by the explicit attribute of the input.
    private string? AriaInvalid => HasError || ValueInvalid is true ? "true" : GetInputAttribute("aria-invalid");

    // The list is only put in the document once the callout has been opened. A phone input renders
    // every country it offers - around 240 of them, each with a flag image - so a page holding
    // several of them would otherwise pay for a list nobody has asked to see yet.
    private bool RenderList => _hasOpened && NoDropdown is false;

    private List<BitCountry> GetFilteredCountries()
    {
        if (_viewItemsValid && _viewItemsKey == _searchText) return _viewItems;

        _viewItemsValid = true;
        _viewItemsKey = _searchText;

        if (_searchText.HasNoValue())
        {
            _viewItems = _allItems;
            return _viewItems;
        }

        var text = Fold(_searchText!.Trim());

        // A dialing code is searched for the way it is written down: "+44", "0044" and "44" all mean
        // the same code, so the term is reduced to its digits before it is matched against one.
        var codeTerm = KeepDigits(text);

        // A term that is nothing but punctuation asks for nothing: the '+' of a dialing code about to
        // be typed is a term of that kind, and answering it with "no results" would say the country
        // being typed does not exist.
        if (codeTerm.Length == 0 && text.Any(char.IsLetterOrDigit) is false)
        {
            _viewItems = _allItems;
            return _viewItems;
        }

        if (codeTerm.Length > 2 && codeTerm.StartsWith("00", StringComparison.Ordinal))
        {
            codeTerm = codeTerm[2..];
        }

        var matches = new List<(BitCountry Country, int Rank, int Index)>();

        for (var i = 0; i < _allItems.Count; i++)
        {
            var country = _allItems[i];
            var rank = GetSearchRank(country, _foldedNames[i], _foldedWords[i], text, codeTerm);
            if (rank < 0) continue;

            matches.Add((country, rank, i));
        }

        // The closest match first (an exact ISO code, then a name that starts with the term, then a
        // dialing code that does), with the list order kept inside each of those groups. A group of
        // countries matched by their dialing code is ordered the way the same code typed into the
        // number input resolves, so searching "+1" offers the United States before Canada rather
        // than whichever of them the alphabet happens to put first.
        matches.Sort((x, y) =>
        {
            if (x.Rank != y.Rank) return x.Rank.CompareTo(y.Rank);

            if (IsDialCodeRank(x.Rank))
            {
                var byOwner = GetDialCodeRank(y.Country).CompareTo(GetDialCodeRank(x.Country));
                if (byOwner != 0) return byOwner;
            }

            return x.Index.CompareTo(y.Index);
        });

        _viewItems = [.. matches.Select(m => m.Country)];

        return _viewItems;
    }

    // The ranks of a match found through the dialing code rather than through a name or an ISO code.
    private static bool IsDialCodeRank(int rank) => rank is DialCodeExactRank or DialCodeStartsRank or DialCodeContainsRank;

    // The rank of a country against a search term, lower being closer. A negative rank means the
    // country does not match the term at all.
    private static int GetSearchRank(BitCountry country, string name, string[] words, string text, string codeTerm)
    {
        var codes = country.DigitsCodes;

        if (country.Iso2.Equals(text, StringComparison.OrdinalIgnoreCase)) return 0;
        if (country.Iso3.Equals(text, StringComparison.OrdinalIgnoreCase)) return 1;
        if (name.StartsWith(text, StringComparison.InvariantCultureIgnoreCase)) return 2;
        if (AnyCode(codes, codeTerm, static (code, term) => code.Equals(term, StringComparison.Ordinal))) return DialCodeExactRank;
        if (AnyCode(codes, codeTerm, static (code, term) => code.StartsWith(term, StringComparison.Ordinal))) return DialCodeStartsRank;

        // A term of several words matches a name whose words start with them, so "united sta" and
        // "so afr" both find their country without the term having to be a prefix of the whole name.
        if (MatchesWordStarts(words, text)) return 5;

        if (name.Contains(text, StringComparison.InvariantCultureIgnoreCase)) return 6;
        if (AnyCode(codes, codeTerm, static (code, term) => code.Contains(term, StringComparison.Ordinal))) return DialCodeContainsRank;
        if (country.Iso2.Contains(text, StringComparison.OrdinalIgnoreCase)) return 8;
        if (country.Iso3.Contains(text, StringComparison.OrdinalIgnoreCase)) return 9;

        return -1;
    }

    // A country is found by any of the codes it answers to, so a term is measured against all of them.
    private static bool AnyCode(string[] codes, string term, Func<string, string, bool> matches)
    {
        if (term.Length == 0) return false;

        foreach (var code in codes)
        {
            if (matches(code, term)) return true;
        }

        return false;
    }

    private static bool MatchesWordStarts(string[] words, string text)
    {
        if (text.Contains(' ') is false) return false;

        var terms = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (terms.Length == 0 || terms.Length > words.Length) return false;

        for (var i = 0; i < terms.Length; i++)
        {
            if (words[i].StartsWith(terms[i], StringComparison.InvariantCultureIgnoreCase) is false) return false;
        }

        return true;
    }

    // A copy of the text with the diacritic of each character removed, so a country is found by the
    // plain letters of its name ("Aland", "Reunion", "Curacao") as much as by the accented ones.
    private static string Fold(string text)
    {
        // Most country names are plain ASCII, which has no diacritic to fold: they are returned as
        // they are instead of being copied character by character into a builder.
        if (System.Text.Ascii.IsValid(text)) return text;

        var builder = new StringBuilder(text.Length);

        foreach (var c in text)
        {
            // ASCII has no diacritics to fold, and a lone surrogate half (one of the two chars an
            // emoji is made of) is not a valid string of its own, so normalizing it would throw.
            if (char.IsAscii(c) || char.IsSurrogate(c))
            {
                builder.Append(c);
                continue;
            }

            var baseChar = c;

            foreach (var decomposed in c.ToString().Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(decomposed) == UnicodeCategory.NonSpacingMark) continue;

                baseChar = decomposed;
                break;
            }

            builder.Append(baseChar);
        }

        return builder.ToString();
    }

    private string GetOptionId(int index) => $"{_calloutId}-opt-{index}";

    private string GetFlagUrl(BitCountry country)
    {
        var url = FlagUrlSelector?.Invoke(country);

        return url.HasValue() ? url! : _flagUrls.GetOrAdd(country.Iso2, static iso2 => $"_content/Bit.BlazorUI.Extras/flags/{iso2.ToUpperInvariant()}-flat-16.webp");
    }

    // A single live region carries everything the callout has to say, and a screen reader only
    // announces what changes inside one: repeating the same sentence (two searches with the same
    // number of results) would otherwise be silent, so an invisible marker alternates with it.
    private void Announce(string? text)
    {
        _announcementMarker = !_announcementMarker;
        _announcement = text.HasValue() && _announcementMarker ? text + '​' : text;
    }

    private async Task HandleOnDropdownClick()
    {
        if (IsEnabled is false || ReadOnly || NoDropdown) return;

        if (IsOpen)
        {
            await CloseCallout();

            // Clicking the button is what closed the callout, so the focus is on the button and the
            // ring CloseCallout dropped belongs right back on.
            SetHasFocus(true);
        }
        else
        {
            await OpenCallout();
        }
    }

    private Task HandleOnDropdownKeyDown(KeyboardEventArgs e) => HandleOnCalloutKeyDown(e, false);

    private Task HandleOnSearchKeyDown(KeyboardEventArgs e) => HandleOnCalloutKeyDown(e, true);

    private async Task HandleOnCalloutKeyDown(KeyboardEventArgs e, bool fromSearchBox)
    {
        if (IsEnabled is false || ReadOnly || NoDropdown) return;

        var key = e.Key;

        if (IsOpen is false)
        {
            // While closed, Enter/Space are handled by the button's native click (which calls
            // HandleOnDropdownClick -> OpenCallout), so they are intentionally not treated as
            // open triggers here to avoid a double toggle. Their defaults are not suppressed
            // (see _dropdownClosedKeys) so the native click can open the callout.
            if (key is "ArrowDown" or "ArrowUp" or "Home" or "End")
            {
                await OpenCallout();
            }

            return;
        }

        switch (key)
        {
            case "Escape":
                await CloseCallout();
                await FocusDropdown();
                break;

            case "Tab":
                // Tab moves on to the next field, so the list must not stay open behind it. Its
                // default is deliberately not suppressed, so the focus move itself still happens.
                await CloseCallout();
                break;

            case "ArrowDown":
                MoveActiveIndex(1);
                break;

            case "ArrowUp":
                MoveActiveIndex(-1);
                break;

            case "PageDown":
                MoveActiveIndex(PageNavigationSize, clamp: true);
                break;

            case "PageUp":
                MoveActiveIndex(-PageNavigationSize, clamp: true);
                break;

            case "Home":
                // Inside the search box Home belongs to the caret, not to the list.
                if (fromSearchBox is false && _viewItems.Count > 0) _activeIndex = 0;
                break;

            case "End":
                if (fromSearchBox is false && _viewItems.Count > 0) _activeIndex = _viewItems.Count - 1;
                break;

            case "Enter":
                if (_activeIndex >= 0 && _activeIndex < _viewItems.Count)
                {
                    await HandleOnCountrySelect(_viewItems[_activeIndex]);
                }
                break;

            case " ":
            case "Spacebar":
                // When the search box is visible the space key must remain available for
                // typing, so it is only treated as a selection key when focus is on the
                // dropdown button (i.e. there is no search box).
                if (fromSearchBox is false && _activeIndex >= 0 && _activeIndex < _viewItems.Count)
                {
                    await HandleOnCountrySelect(_viewItems[_activeIndex]);
                }
                break;

            default:
                // Without a search box to type into, the letters typed on the open list jump to the
                // country whose name they start, the way a native select behaves.
                if (fromSearchBox is false && key.Length == 1 && char.IsLetterOrDigit(key[0]))
                {
                    HandleTypeAhead(key);
                }
                break;
        }
    }

    private void MoveActiveIndex(int offset, bool clamp = false)
    {
        if (_viewItems.Count == 0) return;

        var next = _activeIndex < 0 ? (offset > 0 ? 0 : _viewItems.Count - 1) : _activeIndex + offset;

        if (clamp || NoWrapNavigation)
        {
            next = Math.Clamp(next, 0, _viewItems.Count - 1);
        }
        else if (next < 0)
        {
            next = _viewItems.Count - 1;
        }
        else if (next > _viewItems.Count - 1)
        {
            next = 0;
        }

        _activeIndex = next;
    }

    private void HandleTypeAhead(string key)
    {
        if (_viewItems.Count == 0) return;

        var now = DateTimeOffset.UtcNow;

        // A pause longer than the timeout starts a new term, and repeating a single letter cycles
        // through the countries starting with it instead of searching for a doubled letter.
        _typeAhead = (now - _typeAheadAt) > _typeAheadTimeout ? key : _typeAhead + key;
        _typeAheadAt = now;

        var term = Fold(_typeAhead);
        var start = _typeAhead.Length == 1 ? _activeIndex + 1 : Math.Max(_activeIndex, 0);

        // The type-ahead only runs on the unfiltered list (there is no search box to filter it with),
        // so the names folded once with the list are the same ones this walks over.
        var folded = ReferenceEquals(_viewItems, _allItems) ? _foldedNames : null;

        for (var i = 0; i < _viewItems.Count; i++)
        {
            var index = (start + i) % _viewItems.Count;
            if (index < 0) index += _viewItems.Count;

            var name = folded is not null ? folded[index] : Fold(_viewItems[index].Name);

            if (name.StartsWith(term, StringComparison.InvariantCultureIgnoreCase) is false) continue;

            _activeIndex = index;
            return;
        }
    }

    private async Task OpenCallout()
    {
        if (IsEnabled is false || NoDropdown) return;

        _hasOpened = true;

        if (await AssignIsOpenInternal(true) is false) return;

        // Populate the view items before computing the active index. GetFilteredCountries()
        // is otherwise only called during render, so opening via the keyboard (ArrowDown)
        // before the list has rendered would leave _viewItems empty and _activeIndex at -1,
        // making the first Enter select nothing.
        ResetActiveIndexToSelection();

        _typeAhead = string.Empty;

        if (_dropdownKeysWired)
        {
            await _js.BitExtrasSetPreventKeys(_dropdownButtonRef, _dropdownOpenKeys);
        }

        _pendingCalloutToggle = true;

        await OnOpen.InvokeAsync();
    }

    private async Task CloseCallout()
    {
        if (IsOpen is false) return;

        if (await AssignIsOpenInternal(false) is false) return;

        ResetCalloutState();

        await ClearSearchBoxElement();

        if (_dropdownKeysWired)
        {
            await _js.BitExtrasSetPreventKeys(_dropdownButtonRef, _dropdownClosedKeys);
        }

        _pendingCalloutToggle = true;

        // The focus may well have been inside the callout that is now gone, and the focusout it fired
        // on its way there was ignored while the callout was open, so the ring is dropped here. The
        // flows that put the focus back into the field - Escape, picking a country, clicking the
        // dropdown button - turn it on again themselves.
        SetHasFocus(false);

        await OnClose.InvokeAsync();
    }

    // Puts the active option on the current selection, or on the first country when nothing is
    // selected yet, so the list always opens with something for Enter to act on.
    private void ResetActiveIndexToSelection()
    {
        GetFilteredCountries();

        var selectedIndex = _viewItems.FindIndex(c => c.Iso2 == Country?.Iso2);
        _activeIndex = selectedIndex >= 0 ? selectedIndex : (_viewItems.Count > 0 ? 0 : -1);
        _lastScrolledIndex = -1;
    }

    // Clears the text left in the search box element itself. The value attribute alone is not
    // enough: an input keeps the value its user typed as a DOM property, which a re-render of the
    // same element does not necessarily put back, and the next opening would show a stale term
    // above a list that is no longer filtered by it.
    private async Task ClearSearchBoxElement()
    {
        if (_searchBoxKeysWired is false) return;

        try
        {
            await _js.BitUtilsSetProperty(_searchInputRef, "value", string.Empty);
        }
        catch (JSException) { } // the element might not be ready/visible yet
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private void ResetCalloutState()
    {
        _searchText = null;
        _activeIndex = -1;
        _lastScrolledIndex = -1;
        _typeAhead = string.Empty;
        _viewItemsValid = false;
    }

    // See OnSetIsOpen: the flows that follow AssignIsOpen with their own callout toggle mark the
    // change as internal, so the hook does not arrange a second one.
    private async Task<bool> AssignIsOpenInternal(bool value)
    {
        _internalIsOpenChange = true;
        try
        {
            return await AssignIsOpen(value);
        }
        finally
        {
            _internalIsOpenChange = false;
        }
    }

    private void OnSetIsOpen()
    {
        // The internal open and close flows arrange the toggle themselves. The hook only reacts to a
        // change pushed from the outside through the IsOpen parameter, which otherwise has no path
        // to the JS side that actually shows and hides the callout. Before the first render there is
        // no element to toggle either, which OnAfterRenderAsync takes care of by running the pending
        // toggle as soon as there is one.
        if (_internalIsOpenChange) return;

        if (IsOpen)
        {
            _hasOpened = true;

            ResetActiveIndexToSelection();
        }
        else
        {
            ResetCalloutState();

            // A callout closed through the IsOpen parameter takes whatever focus it holds down with it,
            // and no focusout of the field is fired for it.
            SetHasFocus(false);
        }

        _pendingCalloutToggle = true;
    }

    private async Task ToggleCallout()
    {
        // Without a dropdown there is no callout element in the document to show or position.
        if (IsEnabled is false || IsDisposed || NoDropdown) return;

        await _js.BitCalloutToggleCallout(
            dotnetObj: _dotnetObj!,
            componentId: _fieldGroupId,
            component: null,
            calloutId: _calloutId,
            callout: null,
            overlayId: _overlayId,
            isCalloutOpen: IsOpen,
            responsiveMode: Responsive ? BitResponsiveMode.Panel : BitResponsiveMode.None,
            dropDirection: DropDirection,
            isRtl: Dir is BitDir.Rtl,
            scrollContainerId: _scrollContainerId,
            scrollOffset: NoSearchBox ? 0 : 32,
            headerId: "",
            footerId: "",
            setCalloutWidth: false,
            fixedCalloutWidth: true,
            maxWindowWidth: 0,
            maxHeight: MaxHeight is > 0 ? MaxHeight.Value : 0);
    }

    private async Task HandleOnSearchInput(ChangeEventArgs e)
    {
        _searchText = e.Value?.ToString();
        _viewItemsValid = false;

        // Re-evaluate the filtered list so the active option stays within range and
        // pressing Enter selects the first matching result instead of a stale one.
        GetFilteredCountries();
        _activeIndex = _viewItems.Count > 0 ? 0 : -1;
        _lastScrolledIndex = -1;

        // The list the term filtered down to is inside a callout, so its length is only ever seen:
        // it is announced here for the users who cannot see it.
        Announce(_viewItems.Count switch
        {
            0 => NoResultsMessage ?? "No results found",
            1 => "1 country found",
            var count => $"{count} countries found"
        });

        await OnSearch.InvokeAsync(_searchText);
    }

    private async Task HandleOnCountrySelect(BitCountry country)
    {
        if (IsEnabled is false || ReadOnly) return;

        await CloseCallout();

        // AssignCountry returns false for a one-way controlled Country (set without
        // CountryChanged). In that case the component cannot adopt the new selection, so
        // raising OnCountryChange would report a country that the UI and Value never
        // actually switch to, desynchronizing consumer state. Only notify on a real change.
        if (await AssignCountry(country))
        {
            // The new country can carry a pattern of its own, so the number already typed is laid
            // out again over it instead of keeping the separators of the country left behind.
            // A number that was kept whole because no country claimed its dialing code stops being an
            // international one the moment a country is picked for it by hand: its prefix is dropped so
            // the selection is what carries the code, instead of a value naming one country while the
            // field shows another.
            var digits = KeepDigits(Number, keepLeadingPlus: true);

            await AssignNumber(FormatNumber(digits.StartsWith('+') ? digits[1..] : Number));

            await UpdateValueFromParts();

            await OnCountryChange.InvokeAsync(country);

            // The callout that showed the new selection is gone by now and the button naming it is
            // not what the focus lands on, so the choice that was just made is said out loud.
            Announce($"{country.Name}, +{country.Code}");
        }

        if (NoFocusOnSelect) return;

        // The number is what the user came to type, so the focus lands there instead of being
        // dropped on the document body along with the callout the click happened in.
        try
        {
            await InputElement.FocusAsync();

            // The input is part of the field, so the ring the closing callout dropped is put back on
            // with the focus itself instead of a render later, once the focusin finds its way back.
            SetHasFocus(true);
        }
        catch (JSException) { } // the element might not be ready/visible yet
    }

    private async Task HandleOnClearButtonClick()
    {
        await ClearAsync();

        try
        {
            await InputElement.FocusAsync();
        }
        catch (JSException) { } // the element might not be ready/visible yet
    }

    protected override async Task HandleOnStringValueChangeAsync(ChangeEventArgs e)
    {
        if (IsEnabled is false || ReadOnly) return;

        // Parse what the user typed so a full number entered with an international prefix
        // ('+' or its "00" equivalent) selects the matching country and keeps only the local
        // digits in the number input. Without such a prefix ParseFullNumber returns the current
        // country unchanged and the whole input as the local number.
        // The text is normalized first, because a value that only ever travels on the change event
        // (a paste followed by a blur, with Immediate off) has not been through the input handler.
        var (country, number) = ParseFullNumber(NormalizeTyped(e.Value?.ToString()));

        // Only switch the country when parsing actually resolved a different one. AssignCountry
        // returns false for a one-way controlled Country (set without CountryChanged); in that
        // case the selection can't change, so OnCountryChange must not fire (see HandleOnCountrySelect).
        if (country is not null && country.Iso2 != Country?.Iso2)
        {
            // 'number' is the local part stripped of the parsed country's dialing code, so it is
            // only valid once that country is actually adopted. Defer the number assignment until
            // the country switch succeeds; otherwise a one-way controlled Country would keep the
            // old country while Number held a local part parsed for a different one, leaving the
            // country and the local part out of sync (and recomposing into a wrong full value).
            if (await AssignCountry(country))
            {
                // The pattern can be one the newly adopted country brings with it, so the number is
                // formatted after the switch rather than before it.
                await AssignNumber(FormatNumber(number));

                await OnCountryChange.InvokeAsync(country);
            }
        }
        else
        {
            await AssignNumber(FormatNumber(number));
        }

        await UpdateValueFromParts();
    }

    protected override async Task HandleOnStringValueInputAsync(ChangeEventArgs e)
    {
        if (IsEnabled is false || ReadOnly) return;

        var typed = e.Value?.ToString();

        var text = NormalizeTyped(typed);

        if (text != typed)
        {
            e.Value = text;

            // Only written back when the text actually changed, and through the helper that keeps the
            // caret on the digit it was on: assigning the value of an input parks the caret at the end
            // of it, which would make the middle of a formatted number impossible to edit.
            try
            {
                await _js.BitExtrasSetInputValue(InputElement, text);
            }
            catch (JSException) { } // the element might not be ready/visible yet
        }

        await base.HandleOnStringValueInputAsync(e);
    }

    // What the field shows for the text that was typed into it: Strict throws away what cannot be part
    // of a number, the current pattern lays out what is left, and MaxLength caps the result - the
    // attribute of the input caps what the user types, not what a pattern inserts around it.
    private string? NormalizeTyped(string? typed)
    {
        var text = Strict ? KeepDigits(typed, keepLeadingPlus: true) : typed;

        text = FormatNumber(text);

        return MaxLength >= 0 && text is not null && text.Length > MaxLength ? text[..MaxLength] : text;
    }

    // Recomposes the full Value from the current Number and Country and pushes it out through
    // the normal value pipeline so ValueChanged/OnChange and validation fire as expected.
    private async Task UpdateValueFromParts()
    {
        var composed = ComposeFullNumber(Country, Number);

        _lastValue = composed;
        _lastNumber = Number;
        _lastCountryIso = Country?.Iso2;

        await SetCurrentValueAsStringAsync(composed);
    }

    private async Task FocusDropdown()
    {
        if (NoDropdown) return;

        try
        {
            await _dropdownButtonRef.FocusAsync();

            // The button is part of the field, so the ring the closing callout dropped is put back on
            // with the focus itself instead of a render later, once the focusin finds its way back.
            SetHasFocus(true);
        }
        catch (JSException) { } // the element might not be ready/visible yet
    }

    // The focus ring is on while the focus is in the field, and stays on while the callout has taken
    // it away from there. Only the three elements of the field report their focus, so what happens
    // inside the callout is told to the ring by the flows that make it happen rather than by an event.
    private void SetHasFocus(bool value)
    {
        if (_hasFocus == value) return;

        _hasFocus = value;

        ClassBuilder.Reset();
    }

    private async Task HandleOnFocusIn(FocusEventArgs e)
    {
        if (IsEnabled is false) return;

        SetHasFocus(true);

        await OnFocusIn.InvokeAsync(e);
    }

    private async Task HandleOnFocusOut(FocusEventArgs e)
    {
        if (IsEnabled is false) return;

        // The focus ring belongs to the field as a whole, and the search box of an open callout is
        // part of that field as far as the user is concerned, so the ring stays on while it is open.
        // Closing the callout is what turns it off again (see CloseCallout).
        if (IsOpen is false)
        {
            SetHasFocus(false);
        }

        await OnFocusOut.InvokeAsync(e);
    }

    private async Task HandleOnInputFocus(FocusEventArgs e)
    {
        if (IsEnabled is false) return;

        await OnFocus.InvokeAsync(e);
    }

    private async Task HandleOnInputBlur(FocusEventArgs e)
    {
        if (IsEnabled is false) return;

        await OnBlur.InvokeAsync(e);
    }

    private async Task HandleOnInputClick(MouseEventArgs e)
    {
        if (IsEnabled is false || ReadOnly) return;

        await OnClick.InvokeAsync(e);
    }

    private async Task HandleOnInputKeyDown(KeyboardEventArgs e)
    {
        if (IsEnabled is false || ReadOnly) return;

        await OnKeyDown.InvokeAsync(e);

        if (e.Key == "Enter")
        {
            await OnEnter.InvokeAsync(e);
        }
        else if (e.Key == "Escape")
        {
            await OnEscape.InvokeAsync(e);
        }
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        await base.DisposeAsync(disposing);

        _dotnetObj?.Dispose();

        try
        {
            if (_dropdownKeysWired)
            {
                await _js.BitExtrasDisposePreventKeys(_dropdownButtonRef);
            }

            if (_searchBoxKeysWired)
            {
                await _js.BitExtrasDisposePreventKeys(_searchInputRef);
            }

            await _js.BitCalloutClearCallout(_calloutId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }
}
