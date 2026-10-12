using System.Text;
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
    private bool _fieldHasFocus;
    private bool _tabOutRegistered;
    private bool _hasOpened;
    private bool _dropdownKeysWired;
    private bool _dropdownKeysOpen;
    private bool _searchBoxKeysWired;
    private bool _internalIsOpenChange;
    private bool _pendingCalloutToggle;
    private bool _lastCanOpen = true;
    private string? _announcement;
    private bool _announcementMarker;

    // Reconciliation state between the full Value and its parts (Number + Country).
    // _lastValue mirrors the last full value the component produced so an external Value
    // change (e.g. from a form) can be told apart from the component's own updates.
    private string? _lastValue;
    private string? _lastNumber;
    private string? _lastCountryIso;
    private string? _lastErrorMessage;

    // The countries the dropdown offers, what the search term leaves of them, the one the keyboard is on and the
    // type-ahead that moves it - the part of the list BitCountryPicker walks and searches the same way. A dialing
    // code is searched for as well here, and the countries sharing one are ordered the way the same code typed into
    // the number input resolves.
    private readonly BitCountryList _list;
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

    private ElementReference _searchInputRef;
    private ElementReference _dropdownButtonRef;
    private DotNetObjectReference<BitPhoneInput>? _dotnetObj;

    public BitPhoneInput()
    {
        _list = new(GetCodeTerm, GetDialCodeRank);
    }



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Gets or sets the cascading parameters for the phone input component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings - the market of the app through
    /// <see cref="DefaultCountry"/>, its country list, its look and its localized texts - to be applied to
    /// multiple phone input components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitPhoneInputParams.ParamName)]
    public BitPhoneInputParams? CascadingParameters { get; set; }



    /// <summary>
    /// Shows the pattern the number is currently formatted with as the placeholder of the number input,
    /// so the shape a country expects is offered before anything is typed - "(###) ###-####" for a
    /// number formatted by that mask. It only fills in for a missing placeholder: an explicit
    /// <see cref="Placeholder"/> is always kept, and a country with no pattern of its own shows none.
    /// </summary>
    [Parameter] public bool AutoPlaceholder { get; set; }

    /// <summary>
    /// The color kind of the fill of the phone input, for a field that sits on a surface other than the
    /// primary one.
    /// </summary>
    /// <remarks>
    /// An explicit value wins over <c>--bit-PhoneInput-background</c>; left unset, the fill is the primary kind unless
    /// that variable says otherwise.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitColorKind? Background { get; set; }

    /// <summary>
    /// The color kind of the frame of the phone input at rest and under a pointer, winning over the main color
    /// of <see cref="Color"/>, which keeps the focus ring.
    /// </summary>
    /// <remarks>
    /// An explicit value wins over <c>--bit-PhoneInput-border-color</c> and <c>--bit-PhoneInput-hover-border-color</c>;
    /// left unset, the frame is the primary border unless an explicit <see cref="Color"/> or those variables say
    /// otherwise.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitColorKind? Border { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitPhoneInput.
    /// </summary>
    [Parameter, ResetClassBuilder] public BitPhoneInputClassStyles? Classes { get; set; }

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
    [Parameter, Outranks(nameof(ClearButtonIcon))] public RenderFragment? ClearButtonTemplate { get; set; }

    /// <summary>
    /// What a screen reader announces once the number has been emptied - by the clear button or by
    /// <see cref="ClearAsync"/> - in place of the default "Cleared". Emptying a field moves nothing and says
    /// nothing on its own, so without it the one interaction that throws the whole number away is the one a
    /// screen reader user gets no confirmation of. Set it to an empty string to keep the clearing from being
    /// announced at all.
    /// </summary>
    [Parameter] public string? ClearedAnnouncement { get; set; }

    /// <summary>
    /// The general color of the phone input: the frame takes its main color and the focus ring its focus color.
    /// </summary>
    /// <remarks>
    /// An explicit value wins over <c>--bit-PhoneInput-focus-color</c> and the <c>--bit-PhoneInput-*</c> border colors
    /// (an explicit <see cref="Border"/> still wins the frame); left unset, the field is primary unless those variables
    /// say otherwise.
    /// </remarks>
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
    /// The accessible name of the country selector, "Country" by default. The name of the selected country is
    /// appended to it ("Country: Germany"), so a selector showing only a flag still says which country it is on,
    /// and it is also the name of the country list. Set it to translate the selector for a screen reader.
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
    /// Keeps the national (trunk) prefix of a number in the composed <see cref="BitInputBase{T}.Value"/>. By
    /// default the prefix a number is written with inside its own country - the 0 of "07911 123456" in the
    /// United Kingdom, the 1 of "1 415 555 0123" in North America, the 8 of a Russian number - is dropped, since
    /// an international caller never dials it, so the value is "+447911123456" rather than "+4407911123456". The
    /// number input keeps showing it either way. The 0 that begins the numbers of Italy, San Marino, the Vatican,
    /// Côte d'Ivoire, the Republic of the Congo, Benin and Gabon belongs to the number and is always kept.
    /// </summary>
    [Parameter] public bool KeepNationalPrefix { get; set; }

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
    /// A number typed or pasted with the international prefix of another country does not move it either: it is
    /// kept whole, the way a number with a dialing code no country of the list claims is.
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
    /// The message to show when the search result of the country dropdown is empty. It is also what a screen
    /// reader announces for an empty result.
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
    /// What a screen reader announces for the number of countries a search term leaves in the list, in place of
    /// the default "1 country found" / "{count} countries found" - the way to translate that announcement. An
    /// empty result announces <see cref="NoResultsMessage"/> instead, and returning null or an empty string keeps
    /// a count from being announced at all.
    /// </summary>
    [Parameter] public Func<int, string?>? SearchResultsAnnouncement { get; set; }

    /// <summary>
    /// Shows a clear button in the number input while it holds a value.
    /// </summary>
    [Parameter] public bool ShowClearButton { get; set; }

    /// <summary>
    /// The size of the phone input.
    /// </summary>
    /// <remarks>
    /// An explicit value wins over the <c>--bit-PhoneInput-*</c> size variables (minimum height, font sizes, paddings,
    /// flag size, row height); left unset, the field is medium unless those variables say otherwise.
    /// </remarks>
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
    [Parameter, ResetStyleBuilder] public BitPhoneInputClassStyles? Styles { get; set; }

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
    public Task OpenAsync() => InvokeAsync(async () =>
    {
        // Called from outside a Blazor event handler as well as from one, so the render the opened
        // callout needs is asked for here rather than left to the caller - and on the renderer's own
        // context, since the caller can be on any thread.
        if (Disabled || ReadOnly || NoDropdown || IsOpen) return;

        await OpenCallout();

        StateHasChanged();
    });

    /// <summary>
    /// Closes the country dropdown callout.
    /// </summary>
    public Task CloseAsync() => InvokeAsync(async () =>
    {
        if (IsOpen is false) return;

        await CloseCallout();

        StateHasChanged();
    });

    /// <summary>
    /// Selects the given country exactly as picking it in the callout would, so the same events fire.
    /// </summary>
    public Task SelectCountryAsync(BitCountry country) => InvokeAsync(async () =>
    {
        await HandleOnCountrySelect(country);

        // Unlike a click in the list, this call does not arrive through an event handler of the component,
        // so nothing re-renders it on its own.
        StateHasChanged();
    });

    /// <summary>
    /// Clears the number of the phone input, leaving the selected country as it is.
    /// </summary>
    public Task ClearAsync() => InvokeAsync(async () =>
    {
        await ClearNumber();

        // Unlike the clear button, this call does not arrive through an event handler of the component, so
        // nothing re-renders it on its own.
        StateHasChanged();
    });

    /// <summary>
    /// Sets the local number of the phone input, laid out over the pattern of the selected country the
    /// same way typing it would be, and recomposes the full value from it. A number carrying an
    /// international prefix ('+' or its "00" equivalent) selects the country that owns its dialing code,
    /// exactly as pasting it into the field does.
    /// </summary>
    public Task SetNumberAsync(string? number) => InvokeAsync(async () =>
    {
        await SetNumber(number);

        StateHasChanged();
    });

    private async Task SetNumber(string? number)
    {
        if (Disabled || ReadOnly) return;

        var (country, local) = ParseFullNumber(NormalizeTyped(number), keepCountry: NoDropdown);

        if (country is not null && country.Iso2 != Country?.Iso2)
        {
            // The local part was cut off the dialing code of the parsed country, so it is only kept once that
            // country is actually adopted: a one-way controlled Country would otherwise keep its own country
            // under a number parsed for another one (see HandleOnStringValueChangeAsync).
            if (await AssignCountry(country))
            {
                await AssignNumber(FormatNumber(local));

                await OnCountryChange.InvokeAsync(country);
            }
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
    private static string? ComposeFullNumber(BitCountry? country, string? number, bool keepNationalPrefix = false)
    {
        var local = KeepDigits(number, keepLeadingPlus: true);

        if (local.Length == 0 || local == "+") return null;

        // A local part that still carries a '+' is a full international number whose dialing code no
        // country in the list claims, so it is already the whole value and must not be prefixed again.
        if (local[0] == '+') return local;

        if (country is null) return local;

        if (keepNationalPrefix is false)
        {
            local = StripNationalPrefix(country, local);
        }

        return $"+{country.DigitsCode}{local}";
    }

    // The countries whose numbers can begin with a 0 after the dialing code, where that 0 is part of the
    // number rather than the trunk prefix a call inside the country is dialed with.
    private static readonly HashSet<string> _significantLeadingZero = new(StringComparer.OrdinalIgnoreCase)
    {
        "IT", "SM", "VA", // Italy, San Marino and the Vatican keep the 0 of a landline in international form
        "CI", "CG", "BJ", "GA", // Côte d'Ivoire, the Republic of the Congo, Benin and Gabon number from 0
    };

    // Drops the national (trunk) prefix a number is written with inside its own country - the 0 of the
    // "07911 123456" written in the United Kingdom, the 1 of "1 415 555 0123" in North America - which an
    // international caller never dials, so the E.164 value of a number typed the way its owner writes it is
    // "+447911123456" rather than "+4407911123456". Where the prefix is a digit a number can also begin with,
    // it is only recognized at the length a number that carries it has.
    private static string StripNationalPrefix(BitCountry country, string digits)
    {
        if (digits.Length < 2) return digits;

        var code = country.DigitsCode;

        // The North American Numbering Plan: no area code and no exchange code begins with 0 or 1.
        if (code.StartsWith('1') && digits[0] == '1') return digits[1..];

        switch (country.Iso2.ToUpperInvariant())
        {
            // A 10-digit number, dialed with 8 at home ("8 912 345-67-89").
            case "RU" or "KZ" when digits.Length == 11 && digits[0] == '8':
                return digits[1..];

            // A 9-digit number, dialed with 80 at home ("8 029 123-45-67").
            case "BY" when digits.Length == 11 && digits.StartsWith("80", StringComparison.Ordinal):
                return digits[2..];

            // An 8-digit number, dialed with 8 at home.
            case "LT" or "TM" when digits.Length == 9 && digits[0] == '8':
                return digits[1..];

            // Hungary dials 06 at home ("06 30 123 4567").
            case "HU" when digits.Length > 2 && digits.StartsWith("06", StringComparison.Ordinal):
                return digits[2..];
        }

        if (digits[0] == '0' && _significantLeadingZero.Contains(country.Iso2) is false) return digits[1..];

        return digits;
    }

    // Splits a full phone number into its country and local-number parts. A leading '+' (or its
    // "00" international call-prefix equivalent) triggers a dialing-code lookup (longest matching
    // code wins; the current country is preferred to disambiguate shared codes like +1, then the
    // preferred countries, then BitCountry.Priority). Without such a prefix the whole input is
    // treated as the local number and the current country is kept.
    // A field whose country is fixed (NoDropdown) is never moved by what is typed into it: keepCountry limits the
    // lookup to the current country, so a number with the code of another one is kept whole instead.
    private (BitCountry? Country, string? Number) ParseFullNumber(string? full, bool keepCountry = false)
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
        IEnumerable<BitCountry> candidates = keepCountry && Country is not null ? [Country] : _list.All;

        foreach (var country in candidates)
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

        // The JS side has already hidden this callout to make room for another one, or because the focus moved on
        // from both the field and the list, so the state is brought in line without toggling it a second time.
        if (await AssignIsOpenInternal(false) is false) return;

        // The dismissal has taken the tab order around the button back along with the callout (see ToggleCallout).
        _tabOutRegistered = false;

        _list.Reset();

        await ClearSearchBoxElement();

        await SyncDropdownKeys();

        // The callout this component had open is gone to make room for another one's, so whatever the
        // focus was on inside it is gone along with it.
        SetHasFocus(false);

        await OnClose.InvokeAsync();

        await InvokeAsync(StateHasChanged);
    }

    [JSInvokable("OnTabOut")]
    public async Task _OnTabOut()
    {
        // The list may have been closed by something else - an outside click, the IsOpen parameter - while this
        // call was on its way, and closing it again would reach the JS side to toggle a callout that is not shown.
        if (IsDisposed || IsOpen is false) return;

        await CloseCallout();

        // An IsOpen bound one way keeps the list open whatever the keyboard does, and the ring stays on with it.
        if (IsOpen) return;

        // The JS side has already moved the focus on past the country button, which is usually the number input
        // of this same field, so the ring CloseCallout dropped stays on when the focus has landed back in it.
        SetHasFocus(_fieldHasFocus);

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

        ClassBuilder.Register(() => ReadOnly ? "bit-phi-rdo" : string.Empty);

        ClassBuilder.Register(() => Disabled is false && Required ? "bit-phi-req" : string.Empty);

        ClassBuilder.Register(() => _hasFocus ? "bit-phi-fcs" : string.Empty);

        // Size, Color, Background and Border publish nothing while they are unset, which is what lets the stylesheet
        // tell a default from a choice: the public --bit-PhoneInput-* variables restyle the default and never an
        // explicit value.
        ClassBuilder.Register(() => BitCssClasses.Size(Size, "bit-phi"));

        ClassBuilder.Register(() => ColorClass ?? string.Empty);

        ClassBuilder.Register(() => Background switch
        {
            BitColorKind.Primary => "bit-phi-bpr",
            BitColorKind.Secondary => "bit-phi-bse",
            BitColorKind.Tertiary => "bit-phi-btr",
            BitColorKind.Transparent => "bit-phi-btn",
            _ => string.Empty
        });

        ClassBuilder.Register(() => Border switch
        {
            BitColorKind.Primary => "bit-phi-brp",
            BitColorKind.Secondary => "bit-phi-brs",
            BitColorKind.Tertiary => "bit-phi-brt",
            BitColorKind.Transparent => "bit-phi-brn",
            _ => string.Empty
        });
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitPhoneInputParams))]
    protected override void OnInitialized()
    {
        // The cascade is read before the default country is applied, since the market of an app is the one
        // default it is most likely to hand every phone input under it.
        CascadingParameters?.UpdateParameters(this);

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

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitPhoneInputParams))]
    protected override void OnParametersSet()
    {
        // The cascade is read first, so that the country list, the pattern and the classes below are computed from
        // the parameters the field ends up with rather than from the ones it was written with.
        CascadingParameters?.UpdateParameters(this);

        // The list is read on every render while the callout is open, so it is only built again when its
        // source actually changes; without that the default country set (BitCountries.All) would be copied
        // into a new list of ~240 items each cycle.
        _list.Update(Countries, ExcludeCountries, PreferredCountries);

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

        // A list is only ever shown on a field whose country can be changed. One the page disables or makes
        // read-only while it is open - a form disabled for its submit - is closed with it, rather than left on
        // screen with nothing that could pick from it or close it; and one an IsOpen bound one way keeps open
        // is hidden while the country cannot be changed and shown again once it can (see IsCalloutShown), which
        // takes a toggle of its own either way.
        if (CanOpen != _lastCanOpen)
        {
            _lastCanOpen = CanOpen;

            if (IsOpen) _pendingCalloutToggle = true;
        }

        if (IsOpen && CanOpen is false)
        {
            await CloseCallout();
        }

        await base.OnParametersSetAsync();
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
            if (IsCalloutShown && _list.ActiveIndex < 0)
            {
                ResetActiveIndexToSelection();

                activeIndexSettled = true;
            }

            await ToggleCallout();

            // The open list is driven from whichever element carries aria-activedescendant, so the focus
            // is put on it: the search box when there is one, and the button itself when there is not -
            // a callout opened through the IsOpen parameter would otherwise leave the keys nowhere to go.
            if (IsCalloutShown)
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
        if (IsCalloutShown && _list.ActiveIndex >= 0 && _list.ActiveIndex != _list.LastScrolledIndex)
        {
            _list.LastScrolledIndex = _list.ActiveIndex;
            await _js.BitExtrasScrollOptionIntoView(GetOptionId(_list.ActiveIndex));
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
                _dropdownKeysOpen = IsCalloutShown;
                await _js.BitExtrasSetPreventKeys(_dropdownButtonRef, _dropdownKeysOpen ? BitCountryList.TriggerOpenKeys : BitCountryList.TriggerClosedKeys);
            }
            else
            {
                // Whatever opened or closed the list - the IsOpen parameter or the field being disabled under
                // it, which swap no keys of their own - the keys of the button are put in step with it once the
                // render is through.
                await SyncDropdownKeys();
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
                await _js.BitExtrasSetPreventKeys(_searchInputRef, BitCountryList.SearchBoxKeys);
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
    // flags of the list would ignore an explicit Size. Like the root's, it is empty while the Size is
    // unset, so the public --bit-PhoneInput-* variables restyle the default rows.
    private string SizeClass => BitCssClasses.Size(Size, "bit-phi");

    // The callout is rendered outside the root as well, and the keyboard cue of its active row takes the focus
    // color of the role, so the class is repeated on it the same way the size class is.
    private string? ColorClass => Color switch
    {
        BitColor.Primary => "bit-phi-pri",
        BitColor.Secondary => "bit-phi-sec",
        BitColor.Tertiary => "bit-phi-ter",
        BitColor.Info => "bit-phi-inf",
        BitColor.Success => "bit-phi-suc",
        BitColor.Warning => "bit-phi-wrn",
        BitColor.SevereWarning => "bit-phi-swr",
        BitColor.Error => "bit-phi-err",
        BitColor.PrimaryBackground => "bit-phi-pbg",
        BitColor.SecondaryBackground => "bit-phi-sbg",
        BitColor.TertiaryBackground => "bit-phi-tbg",
        BitColor.PrimaryForeground => "bit-phi-pfg",
        BitColor.SecondaryForeground => "bit-phi-sfg",
        BitColor.TertiaryForeground => "bit-phi-tfg",
        BitColor.PrimaryBorder => "bit-phi-pbr",
        BitColor.SecondaryBorder => "bit-phi-sbr",
        BitColor.TertiaryBorder => "bit-phi-tbr",
        _ => null
    };

    // Only a field whose country can be changed opens its list: the button, the keyboard and OpenAsync all refuse
    // to open the list of a disabled or read-only one, and one with no dropdown has no list to open.
    private bool CanOpen => Disabled is false && ReadOnly is false && NoDropdown is false;

    // Whether the list is actually on screen. An IsOpen the page holds at true on a field whose country cannot be
    // changed is refused the way the button refuses to open (see OnParametersSetAsync), and one bound one way,
    // which the field cannot set back to false, leaves the list hidden - with nothing it names and no overlay -
    // until the country can be changed again.
    private bool IsCalloutShown => IsOpen && CanOpen;

    private bool HasLabel => Label.HasValue() || LabelTemplate is not null;

    // aria-labelledby takes precedence over aria-label, so pointing at the visible label while a name of
    // its own was given would quietly throw that name away. The visible label keeps naming the input
    // through the for/id pair either way, which is what the label element is for.
    private string? LabelledBy => HasLabel && AriaLabel.HasNoValue() ? _labelId : null;

    private bool HasDescription => Description.HasValue() || DescriptionTemplate is not null;

    private bool HasErrorMessage => ErrorMessage.HasValue() || ErrorMessageTemplate is not null;

    // The invalid state the consumer forces, either as a flag or by handing the field a message to show.
    protected override bool HasError => Invalid || HasErrorMessage;

    // The accessible name of the country selector. It carries the selected country, because an
    // aria-label replaces the content of the button for a screen reader: without the name in it, a
    // selector showing only a flag (NoDialCode) would be announced as "country" and nothing else,
    // whatever country it is actually on - which is why a translated DropdownAriaLabel is a prefix
    // rather than the whole name.
    private string DropdownLabel => Country is not null
                                        ? $"{DropdownAriaLabel ?? "Country"}: {Country.Name}"
                                        : DropdownPlaceholder ?? DropdownAriaLabel ?? "Select country";

    // The tooltip of the selector names the country its flag stands for.
    private string DropdownTitle => Country?.Name ?? DropdownLabel;

    // The tooltip is written on the root, so it also covers the selector, and on the input itself, so it reaches
    // a keyboard as well as a pointer. A title the consumer put on the input is kept, since the explicit
    // attribute of the input would otherwise write a null over it.
    private string? InputTitle => Title ?? GetSplattedInputAttribute("title");

    private bool ShowClear => ShowClearButton && Number.HasValue();

    // The placeholder of a field that has none of its own, which is the pattern the number is laid out
    // over: it says the shape the country expects before a single digit has been typed.
    private string? InputPlaceholder => Placeholder ?? (AutoPlaceholder ? CurrentMask : null);

    private string? InputModeValue => InputMode?.ToString().ToLower();

    // What is written under the field is what describes it, so both lines are pointed at rather than
    // one of them. A value the consumer put on the input itself is kept: a field with a description of
    // its own would otherwise lose it the moment the component has anything to reference.
    // A selector the keyboard never reaches - a fixed country, or one a ReadOnly field takes out of the tab
    // order - is pointed at as well, since the number means nothing without the country it is read with.
    private string? AriaDescribedBy
    {
        get
        {
            var describesCountry = NoDropdown || ReadOnly;

            if (HasErrorMessage is false && HasDescription is false && describesCountry is false) return GetSplattedInputAttribute("aria-describedby");

            var ids = string.Join(' ', new[]
            {
                GetSplattedInputAttribute("aria-describedby"),
                describesCountry ? _dropdownId : null,
                HasErrorMessage ? _errorId : null,
                HasDescription ? _descriptionId : null
            }.Where(id => id.HasValue()));

            return ids.HasValue() ? ids : null;
        }
    }

    // The forced invalid state and the one the EditContext produces end up on the same attribute, and
    // the base class writes the latter into the splatted attributes, so the value of the consumer is
    // read back here instead of being overwritten by the explicit attribute of the input.
    private string? AriaInvalid => IsInvalid ? "true" : GetSplattedInputAttribute("aria-invalid");

    // The list is only put in the document once the callout has been opened. A phone input renders
    // every country it offers - around 240 of them, each with a flag image - so a page holding
    // several of them would otherwise pay for a list nobody has asked to see yet.
    private bool RenderList => _hasOpened && NoDropdown is false;

    // A dialing code is searched for the way it is written down: "+44", "0044" and "44" all mean the same code, so
    // the term is reduced to its digits, less the "00" international prefix, before it is matched against one.
    private static string GetCodeTerm(string text)
    {
        var codeTerm = KeepDigits(text);

        return codeTerm.Length > 2 && codeTerm.StartsWith("00", StringComparison.Ordinal) ? codeTerm[2..] : codeTerm;
    }

    private string GetOptionId(int index) => $"{_calloutId}-opt-{index}";

    // The display is written here rather than in the stylesheet because it is what the component toggles the
    // layer with, and Styles.Overlay is appended after it so a value written for the overlay still wins.
    private string GetOverlayStyles() => $"display:{(IsCalloutShown ? "block" : "none")};{Styles?.Overlay}";

    // Null for a country with no flag image at all, which is drawn as an empty box of the size of one.
    private string? GetFlagUrl(BitCountry country) => BitCountryList.GetFlagUrl(country, FlagUrlSelector);

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
        if (Disabled || ReadOnly || NoDropdown) return;

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
        if (Disabled || ReadOnly || NoDropdown) return;

        var key = e.Key;

        if (IsOpen is false)
        {
            // While closed, Enter/Space are handled by the button's native click (which calls
            // HandleOnDropdownClick -> OpenCallout), so they are intentionally not treated as
            // open triggers here to avoid a double toggle. Their defaults are not suppressed
            // (see BitCountryList.TriggerClosedKeys) so the native click can open the callout.
            if (key is "ArrowDown" or "ArrowUp" or "Home" or "End")
            {
                await OpenCallout();

                // Home and End say where in the list to land, the way they do on a native select.
                if (key is "Home" or "End")
                {
                    _list.Navigate(key, fromSearchBox: false, NoWrapNavigation);
                }
            }
            else if (NoSearchBox && BitCountryList.IsTypeAheadKey(e))
            {
                // A selector with no search box is a select-only combobox, and typing on one opens it on the
                // country the letters start - there is no box for the letters to go into instead.
                await OpenCallout();

                _list.TypeAhead(key);
            }

            return;
        }

        // A list with a search box is driven from that box, which is the combobox naming the active option. The
        // button only holds the focus of the open list when a Shift+Tab has taken it back there, and as a plain
        // button it names no option, so the keys that work the list hand the focus back to the search box along
        // with what they do, rather than moving an option nobody is told about.
        if (fromSearchBox is false && NoSearchBox is false && await HandleOnButtonOfSearchableListKeyDown(e)) return;

        switch (key)
        {
            case "Escape":
                await CloseCallout();
                await FocusDropdown();
                break;

            case "ArrowUp" when e.AltKey:
                // Alt+Up closes the list the way the combobox pattern has it: a select-only one takes the option
                // the arrows are on, while one with a search box only gives the focus back to its button.
                if (fromSearchBox is false && _list.Active is { } active)
                {
                    await HandleOnCountrySelect(active);
                }
                else
                {
                    await CloseCallout();
                    await FocusDropdown();
                }
                break;

            case "Tab":
                // While the list is in the tab order after the button (see ToggleCallout), the Tab key is the JS
                // side's to answer, and the focus leaving the field and the list closes the list wherever it goes.
                // A list without a search box keeps the focus on the button, so its Tab moves on from there into
                // the rest of the field, and the list must not stay open behind it. Its default is deliberately not
                // suppressed, so the focus move itself still happens.
                if (_tabOutRegistered) break;

                await CloseCallout();
                break;

            case "Enter":
                if (_list.Active is { } country)
                {
                    await HandleOnCountrySelect(country);
                }
                break;

            case " ":
            case "Spacebar":
                // When the search box is visible the space key must remain available for
                // typing, so it is only treated as a selection key when focus is on the
                // dropdown button (i.e. there is no search box).
                if (fromSearchBox) break;

                // A space typed in the middle of a type-ahead term is part of the name being typed
                // ("united s" for the United States), not a choice of the country the term has reached so far.
                if (_list.IsTypeAheadInProgress)
                {
                    _list.TypeAhead(" ");
                }
                else if (_list.Active is { } spaced)
                {
                    await HandleOnCountrySelect(spaced);
                }
                break;

            default:
                // The arrows, PageUp/PageDown, and Home/End outside the search box, whose caret they belong to.
                if (_list.Navigate(key, fromSearchBox, NoWrapNavigation)) break;

                // Without a search box to type into, the letters typed on the open list jump to the
                // country whose name they start, the way a native select behaves.
                if (fromSearchBox is false && BitCountryList.IsTypeAheadKey(e))
                {
                    _list.TypeAhead(key);
                }
                break;
        }
    }

    // The keys of the open list pressed on the button of a list that has a search box (see HandleOnCalloutKeyDown).
    // The navigation keys move the active option the way they do anywhere in the list - Home and End included, since
    // they were not pressed in the search box whose caret they would move - and hand the focus to the search box,
    // which announces it; a letter goes into the search box as if it had been typed there; Enter and Space press the
    // button, which closes the list as a click on it does; Alt+Up closes it the way it does from the search box.
    // Escape and Tab are left to the handling they get anywhere in the list. Reports whether the key was handled here.
    private async Task<bool> HandleOnButtonOfSearchableListKeyDown(KeyboardEventArgs e)
    {
        switch (e.Key)
        {
            case "ArrowUp" when e.AltKey:
                await CloseCallout();
                await FocusDropdown();
                return true;

            case "Enter":
            case " ":
            case "Spacebar":
                await CloseCallout();

                // The focus is still on the button, so the ring CloseCallout dropped belongs right back on.
                SetHasFocus(true);
                return true;
        }

        if (_list.Navigate(e.Key, fromSearchBox: false, NoWrapNavigation) is false)
        {
            if (BitCountryList.IsTypeAheadKey(e) is false) return false;

            await ApplySearchText(_list.SearchText + e.Key);
        }

        try
        {
            await _searchInputRef.FocusAsync();
        }
        catch (JSException) { } // the element might not be ready/visible yet

        return true;
    }

    private async Task OpenCallout()
    {
        if (CanOpen is false) return;

        _hasOpened = true;

        if (await AssignIsOpenInternal(true) is false) return;

        // Settle the active option before the list has rendered: opening via the keyboard (ArrowDown)
        // would otherwise leave it at -1, making the first Enter select nothing.
        ResetActiveIndexToSelection();

        _list.ResetTypeAhead();

        await SyncDropdownKeys();

        _pendingCalloutToggle = true;

        await OnOpen.InvokeAsync();
    }

    private async Task CloseCallout()
    {
        if (IsOpen is false) return;

        if (await AssignIsOpenInternal(false) is false) return;

        _list.Reset();

        await ClearSearchBoxElement();

        await SyncDropdownKeys();

        _pendingCalloutToggle = true;

        // The focus may well have been inside the callout that is now gone, and the focusout it fired
        // on its way there was ignored while the callout was open, so the ring is dropped here. The
        // flows that put the focus back into the field - Escape, picking a country, clicking the
        // dropdown button - turn it on again themselves.
        SetHasFocus(false);

        await OnClose.InvokeAsync();
    }

    // Dismissing the callout by hand - a click outside it, or the close button of the responsive panel - hides
    // whatever inside it had the focus, which would otherwise fall to the document body and strand a keyboard
    // user at the top of the page, so it is given back to the selector the list was opened from.
    private async Task CloseCalloutAndRestoreFocus()
    {
        if (IsOpen is false) return;

        await CloseCallout();

        await FocusDropdown();
    }

    // The keys the button prevents follow whether its list is on screen, and are swapped the moment that changes,
    // so the very next key is answered the way the list now is: Enter and Space are left to the native click of a
    // closed button, which opens the list, and are kept from an open one, where they pick a country and the click
    // would open the list again straight after.
    private async Task SyncDropdownKeys()
    {
        if (_dropdownKeysWired is false || _dropdownKeysOpen == IsCalloutShown) return;

        _dropdownKeysOpen = IsCalloutShown;

        await _js.BitExtrasSetPreventKeys(_dropdownButtonRef, _dropdownKeysOpen ? BitCountryList.TriggerOpenKeys : BitCountryList.TriggerClosedKeys);
    }

    // Puts the active option on the current selection, or on the first country when nothing is
    // selected yet, so the list always opens with something for Enter to act on.
    private void ResetActiveIndexToSelection() => _list.ResetActiveToSelection(c => c.Iso2 == Country?.Iso2);

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
        // toggle as soon as there is one; it is also where the keys of the button are put in step
        // with the list (see WirePreventKeys), and an open on a field whose country cannot be changed
        // is refused before that (see OnParametersSetAsync).
        if (_internalIsOpenChange) return;

        if (IsOpen)
        {
            _hasOpened = true;

            ResetActiveIndexToSelection();
        }
        else
        {
            _list.Reset();

            // A callout closed through the IsOpen parameter takes whatever focus it holds down with it,
            // and no focusout of the field is fired for it.
            SetHasFocus(false);
        }

        _pendingCalloutToggle = true;
    }

    // The open list is relocated to the end of the body, so the browser's own tab order would run from its search
    // box off the end of the page - skipping the number input right after the country button - and back from it
    // into whatever ends the page, leaving the list open behind its overlay either way. While it is open the
    // search box is put into the tab order right after the country button instead (see Utils.setupTabOut): Tab
    // moves on through the rest of the list - the responsive close button, rendered after the search box for that
    // reason - to the number input and closes the list through OnTabOut, and Shift+Tab from the search box goes
    // back to the button, leaving the list open for the Tab that brings the user back in.
    // A list without a search box has nothing to tab into: the focus stays on the button, which is the
    // select-only combobox the list belongs to, so the Tab key is left to move on from there as it would anyway.
    // Both ride on the toggle itself, which registers them with the open and takes them back with the close or a
    // dismissal, so they cost no round trip of their own and are never left behind a list that is gone. What is
    // left of the Tab key - the focus moving on from both the field and the list some other way, a Shift+Tab from
    // the button for one - is answered by where the focus lands rather than by the key (dismissOnFocusLeave), so
    // nothing here has to guess which of the Tabs the JS side took.
    private async Task ToggleCallout()
    {
        // Without a dropdown there is no callout element in the document to show or position.
        if (IsDisposed || NoDropdown) return;

        // What is shown is what the field can show: a list the field was disabled under is closed on the JS side
        // all the same, rather than left on screen with nothing that could close it.
        var shown = IsCalloutShown;
        var tabOut = shown && NoSearchBox is false;

        // A close takes the tab order back whatever it does, and an open that fails registers none.
        _tabOutRegistered = false;

        // The callout is positioned against the field group, but the root is the element whose Style, classes
        // and theme the callout and the overlay have to go on inheriting once they are relocated to the body,
        // so it is named apart as the rootId.
        await _js.BitCalloutToggleCallout(
            dotnetObj: _dotnetObj!,
            componentId: _fieldGroupId,
            component: null,
            calloutId: _calloutId,
            callout: null,
            overlayId: _overlayId,
            isCalloutOpen: shown,
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
            maxHeight: MaxHeight is > 0 ? MaxHeight.Value : 0,
            dismissOnFocusLeave: true,
            rootId: _Id,
            tabOutTriggerId: tabOut ? _dropdownId : "");

        // Registered in the same call that showed the list, after the same lookups the showing depends on.
        _tabOutRegistered = tabOut;
    }

    private Task HandleOnSearchInput(ChangeEventArgs e) => ApplySearchText(e.Value?.ToString());

    private async Task ApplySearchText(string? text)
    {
        // The active option moves to the first match, so pressing Enter selects it instead of a stale one.
        var count = _list.Search(text);

        // The list the term filtered down to is inside a callout, so its length is only ever seen:
        // it is announced here for the users who cannot see it.
        Announce(BitCountryList.GetSearchAnnouncement(count, NoResultsMessage, SearchResultsAnnouncement));

        await OnSearch.InvokeAsync(_list.SearchText);
    }

    private async Task HandleOnCountrySelect(BitCountry country)
    {
        if (Disabled || ReadOnly) return;

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

    private async Task ClearNumber()
    {
        if (Disabled || ReadOnly) return;

        // An empty field has nothing to clear, and reporting a clear that changed nothing would have a
        // consumer react to a number that was never there.
        if (Number.HasNoValue()) return;

        // AssignNumber returns false for a one-way controlled Number (set without NumberChanged).
        // In that case the field cannot drop what it shows, so reporting a clear that never happened
        // would have a consumer react to a number that is still there (see HandleOnCountrySelect).
        if (await AssignNumber(null) is false) return;

        await UpdateValueFromParts();

        // Emptying the field moves nothing and says nothing on its own, so the one interaction that throws
        // the whole number away is announced explicitly. An empty announcement is how a consumer asks for
        // silence.
        var announcement = ClearedAnnouncement ?? "Cleared";
        if (announcement.HasValue())
        {
            Announce(announcement);
        }

        await OnClear.InvokeAsync();
    }

    private async Task HandleOnClearButtonClick()
    {
        await ClearNumber();

        try
        {
            await InputElement.FocusAsync();
        }
        catch (JSException) { } // the element might not be ready/visible yet
    }

    protected override async Task HandleOnStringValueChangeAsync(ChangeEventArgs e)
    {
        if (Disabled || ReadOnly) return;

        // Parse what the user typed so a full number entered with an international prefix
        // ('+' or its "00" equivalent) selects the matching country and keeps only the local
        // digits in the number input. Without such a prefix ParseFullNumber returns the current
        // country unchanged and the whole input as the local number.
        // The text is normalized first, because a value that only ever travels on the change event
        // (a paste followed by a blur, with Immediate off) has not been through the input handler.
        var (country, number) = ParseFullNumber(NormalizeTyped(e.Value?.ToString()), keepCountry: NoDropdown);

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
        if (Disabled || ReadOnly) return;

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
        var composed = ComposeFullNumber(Country, Number, KeepNationalPrefix);

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
        if (Disabled) return;

        _fieldHasFocus = true;

        SetHasFocus(true);

        await OnFocusIn.InvokeAsync(e);
    }

    private async Task HandleOnFocusOut(FocusEventArgs e)
    {
        if (Disabled) return;

        // Where the focus actually is, apart from the ring that stays on for the open callout (see _OnTabOut).
        _fieldHasFocus = false;

        // The focus ring belongs to the field as a whole, and the search box of an open callout is
        // part of that field as far as the user is concerned, so the ring stays on while it is open.
        // Closing the callout is what turns it off again (see CloseCallout).
        if (IsCalloutShown is false)
        {
            SetHasFocus(false);
        }

        await OnFocusOut.InvokeAsync(e);
    }

    private async Task HandleOnInputFocus(FocusEventArgs e)
    {
        if (Disabled) return;

        await OnFocus.InvokeAsync(e);
    }

    private async Task HandleOnInputBlur(FocusEventArgs e)
    {
        if (Disabled) return;

        await OnBlur.InvokeAsync(e);
    }

    private async Task HandleOnInputClick(MouseEventArgs e)
    {
        if (Disabled || ReadOnly) return;

        await OnClick.InvokeAsync(e);
    }

    private async Task HandleOnInputKeyDown(KeyboardEventArgs e)
    {
        if (Disabled || ReadOnly) return;

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

        try
        {
            // First, so that nothing failing after it can leave the listeners of an open list behind: clearing the
            // callout takes back the tab order and the focus watch its toggle registered along with it.
            _tabOutRegistered = false;
            await _js.BitCalloutClearCallout(_calloutId);

            if (_dropdownKeysWired)
            {
                await _js.BitExtrasDisposePreventKeys(_dropdownButtonRef);
            }

            if (_searchBoxKeysWired)
            {
                await _js.BitExtrasDisposePreventKeys(_searchInputRef);
            }
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
        finally
        {
            // Only once the JS side holds no listener that could still call back on it.
            _dotnetObj?.Dispose();
        }
    }
}
