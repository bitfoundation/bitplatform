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
    private bool _isOpen;
    private bool _hasFocus;
    private string? _searchText;
    private int _activeIndex = -1;
    private int _lastScrolledIndex = -1;

    // Reconciliation state between the full Value and its parts (Number + Country).
    // _lastValue mirrors the last full value the component produced so an external Value
    // change (e.g. from a form) can be told apart from the component's own updates.
    private string? _lastValue;
    private string? _lastNumber;
    private string? _lastCountryIso;

    private string _labelId = string.Empty;
    private string _inputId = string.Empty;
    private string _searchId = string.Empty;
    private string _calloutId = string.Empty;
    private string _overlayId = string.Empty;
    private string _dropdownId = string.Empty;
    private string _fieldGroupId = string.Empty;
    private string _scrollContainerId = string.Empty;
    
    private List<BitCountry> _allItems = [];
    private List<BitCountry> _viewItems = [];
    private ElementReference _searchInputRef;
    private ElementReference _dropdownButtonRef;
    private ICollection<BitCountry>? _lastCountries;
    private DotNetObjectReference<BitPhoneInput>? _dotnetObj;

    // Keys whose default browser behavior must be suppressed. These are applied through a
    // deterministic JS keydown listener (see BitExtrasSetPreventKeys) so the suppression
    // always matches the key of the current event instead of lagging one event behind, as
    // Blazor's stateful `@onkeydown:preventDefault` binding would.
    private static readonly string[] _searchBoxKeys = ["ArrowDown", "ArrowUp", "Home", "End", "Enter"];
    private static readonly string[] _dropdownClosedKeys = ["ArrowDown", "ArrowUp", "Home", "End"];
    private static readonly string[] _dropdownOpenKeys = ["ArrowDown", "ArrowUp", "Home", "End", "Enter", " ", "Spacebar"];



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Custom CSS classes for different parts of the BitPhoneInput.
    /// </summary>
    [Parameter] public BitPhoneInputClassStyles? Classes { get; set; }

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
    /// The default selected country to be initially used when the Country parameter is not set.
    /// </summary>
    [Parameter] public BitCountry? DefaultCountry { get; set; }

    /// <summary>
    /// Determines the allowed drop directions of the country dropdown callout.
    /// </summary>
    [Parameter] public BitDropDirection DropDirection { get; set; } = BitDropDirection.TopAndBottom;

    /// <summary>
    /// The placeholder text of the country dropdown when no country is selected.
    /// </summary>
    [Parameter] public string? DropdownPlaceholder { get; set; }

    /// <summary>
    /// Renders the phone input to fill 100% of its container width.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FullWidth { get; set; }

    /// <summary>
    /// The label of the phone input shown above the field.
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// The custom template for the label of the phone input.
    /// </summary>
    [Parameter] public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// Determines the maximum number of characters allowed in the number input.
    /// </summary>
    [Parameter] public int MaxLength { get; set; } = -1;

    /// <summary>
    /// The message to show when the search result of the country dropdown is empty.
    /// </summary>
    [Parameter] public string? NoResultsMessage { get; set; }

    /// <summary>
    /// Hides the search box of the country dropdown.
    /// </summary>
    [Parameter] public bool NoSearchBox { get; set; }

    /// <summary>
    /// The local phone number (the digits typed in the input box, without the country dialing code). (two-way bound)
    /// <para>
    /// This is the part the user edits. The full phone number, including the dialing code of the
    /// selected <see cref="Country"/>, is exposed through the two-way bound <see cref="BitInputBase{T}.Value"/>.
    /// </para>
    /// </summary>
    [Parameter, TwoWayBound]
    public string? Number { get; set; }

    /// <summary>
    /// The callback that is invoked when the selected country changes.
    /// </summary>
    [Parameter] public EventCallback<BitCountry?> OnCountryChange { get; set; }

    /// <summary>
    /// The placeholder text of the number input.
    /// </summary>
    [Parameter] public string? Placeholder { get; set; }

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
    /// Custom CSS styles for different parts of the BitPhoneInput.
    /// </summary>
    [Parameter] public BitPhoneInputClassStyles? Styles { get; set; }



    /// <summary>
    /// The full phone number including the dialing code of the selected country in the form of "+[code][number]".
    /// This is the same value exposed through the two-way bound <see cref="BitInputBase{T}.Value"/> and is kept for convenience.
    /// </summary>
    public string? FullNumber => CurrentValue;



    // Builds the full phone number from the selected country and the local number. The dialing
    // code is normalized to digits only (any hyphens are removed) so the result follows E.164.
    // Returns null when there is no local number so an empty input maps to an empty value.
    private static string? ComposeFullNumber(BitCountry? country, string? number)
    {
        if (string.IsNullOrEmpty(number)) return null;
        if (country is null) return number;
        return $"+{country.Code.Replace("-", string.Empty)}{number}";
    }

    // Splits a full phone number into its country and local-number parts. A leading '+' (or its
    // "00" international call-prefix equivalent) triggers a dialing-code lookup (longest matching
    // code wins; the current country is preferred to disambiguate shared codes like +1). Without
    // such a prefix the whole input is treated as the local number and the current country is kept.
    private (BitCountry? Country, string? Number) ParseFullNumber(string? full)
    {
        if (string.IsNullOrWhiteSpace(full)) return (Country, null);

        full = full.Trim();

        string digits;
        if (full.StartsWith('+'))
        {
            digits = full[1..];
        }
        else if (full.StartsWith("00", StringComparison.Ordinal))
        {
            digits = full[2..];
        }
        else
        {
            return (Country, full);
        }

        BitCountry? best = null;
        var bestLength = 0;
        foreach (var country in _allItems)
        {
            var code = country.Code.Replace("-", string.Empty);
            if (code.Length == 0 || digits.StartsWith(code, StringComparison.Ordinal) is false) continue;

            // Prefer a longer (more specific) code, and prefer the currently selected country
            // when its code is at least as specific so ambiguous codes (e.g. +1) stay stable.
            var isCurrent = Country is not null && country.Iso2 == Country.Iso2;
            if (code.Length > bestLength || (code.Length == bestLength && isCurrent))
            {
                best = country;
                bestLength = code.Length;
            }
        }

        return best is null ? (Country, digits) : (best, digits[bestLength..]);
    }



    [JSInvokable("CloseCallout")]
    public async Task _CloseCalloutBeforeAnotherCalloutIsOpened()
    {
        if (_isOpen is false) return;

        _isOpen = false;
        _searchText = null;
        _activeIndex = -1;
        _lastScrolledIndex = -1;

        await _js.BitExtrasSetPreventKeys(_dropdownButtonRef, _dropdownClosedKeys);

        await InvokeAsync(StateHasChanged);
    }



    protected override string RootElementClass => "bit-phi";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => FullWidth ? "bit-phi-fwd" : string.Empty);

        ClassBuilder.Register(() => _hasFocus ? "bit-phi-fcs" : string.Empty);

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
        _inputId = $"BitPhoneInput-{UniqueId}-input";
        _searchId = $"BitPhoneInput-{UniqueId}-search";
        _dropdownId = $"BitPhoneInput-{UniqueId}-dropdown";
        _fieldGroupId = $"BitPhoneInput-{UniqueId}-field-group";
        _calloutId = $"BitPhoneInput-{UniqueId}-callout";
        _overlayId = $"BitPhoneInput-{UniqueId}-overlay";
        _scrollContainerId = $"BitPhoneInput-{UniqueId}-scroll-container";

        if (CountryHasBeenSet is false && DefaultCountry is not null)
        {
            Country = DefaultCountry;
        }

        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        // Materialize the country list only when the Countries reference actually changes.
        // GetFilteredCountries() runs on every render while the callout is open, so without
        // this cache the default BitCountry[] (BitCountries.All) would allocate a new list of
        // ~240 items each cycle because the "as List<BitCountry>" cast always fails for arrays.
        if (ReferenceEquals(_lastCountries, Countries) is false)
        {
            _lastCountries = Countries;
            _allItems = Countries as List<BitCountry> ?? [.. Countries];
        }

        base.OnParametersSet();
    }

    protected override async Task OnParametersSetAsync()
    {
        await ReconcileValueAndParts();

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

            await AssignNumber(number);
            if (country is not null)
            {
                await AssignCountry(country);
            }

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

            // Wire up deterministic key suppression once the elements exist. The callout
            // (including the search box) is always present in the DOM, so the search input
            // reference is available here when the search box is enabled.
            await _js.BitExtrasSetPreventKeys(_dropdownButtonRef, _dropdownClosedKeys);
            if (NoSearchBox is false)
            {
                await _js.BitExtrasSetPreventKeys(_searchInputRef, _searchBoxKeys);
            }
        }

        // Keep the active option visible during keyboard navigation. Done after render so
        // the option element is guaranteed to exist and the callout is laid out.
        if (_isOpen && _activeIndex >= 0 && _activeIndex != _lastScrolledIndex)
        {
            _lastScrolledIndex = _activeIndex;
            await _js.BitExtrasScrollOptionIntoView(GetOptionId(_activeIndex));
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out string? result, [NotNullWhen(false)] out string? parsingErrorMessage)
    {
        result = value;
        parsingErrorMessage = null;
        return true;
    }



    private List<BitCountry> GetFilteredCountries()
    {
        if (_searchText.HasNoValue())
        {
            _viewItems = _allItems;
            return _viewItems;
        }

        var text = _searchText!.Trim();

        _viewItems = [.. _allItems.Where(c => c.Name.Contains(text, StringComparison.InvariantCultureIgnoreCase) ||
                                              c.Code.Contains(text, StringComparison.InvariantCultureIgnoreCase) ||
                                              c.Iso2.Contains(text, StringComparison.InvariantCultureIgnoreCase) ||
                                              c.Iso3.Contains(text, StringComparison.InvariantCultureIgnoreCase))];

        return _viewItems;
    }

    private string GetOptionId(int index) => $"{_calloutId}-opt-{index}";

    private static string GetFlagUrl(BitCountry country)
    {
        return $"_content/Bit.BlazorUI.Extras/flags/{country.Iso2.ToUpperInvariant()}-flat-16.webp";
    }

    private async Task HandleOnDropdownClick()
    {
        if (IsEnabled is false || ReadOnly) return;

        if (_isOpen)
        {
            await CloseCallout();
        }
        else
        {
            await OpenCallout();
        }
    }

    private async Task HandleOnKeyDown(KeyboardEventArgs e)
    {
        if (IsEnabled is false || ReadOnly) return;

        var key = e.Key;

        if (_isOpen is false)
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
                break;

            case "ArrowDown":
                if (_viewItems.Count > 0)
                {
                    _activeIndex = _activeIndex < _viewItems.Count - 1 ? _activeIndex + 1 : 0;
                }
                break;

            case "ArrowUp":
                if (_viewItems.Count > 0)
                {
                    _activeIndex = _activeIndex > 0 ? _activeIndex - 1 : _viewItems.Count - 1;
                }
                break;

            case "Home":
                if (_viewItems.Count > 0) _activeIndex = 0;
                break;

            case "End":
                if (_viewItems.Count > 0) _activeIndex = _viewItems.Count - 1;
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
                if (NoSearchBox && _activeIndex >= 0 && _activeIndex < _viewItems.Count)
                {
                    await HandleOnCountrySelect(_viewItems[_activeIndex]);
                }
                break;
        }
    }

    private async Task OpenCallout()
    {
        _isOpen = true;

        // Populate the view items before computing the active index. GetFilteredCountries()
        // is otherwise only called during render, so opening via the keyboard (ArrowDown)
        // before the list has rendered would leave _viewItems empty and _activeIndex at -1,
        // making the first Enter select nothing.
        GetFilteredCountries();

        var selectedIndex = _viewItems.FindIndex(c => c.Iso2 == Country?.Iso2);
        _activeIndex = selectedIndex >= 0 ? selectedIndex : (_viewItems.Count > 0 ? 0 : -1);
        _lastScrolledIndex = -1;

        await _js.BitExtrasSetPreventKeys(_dropdownButtonRef, _dropdownOpenKeys);

        await ToggleCallout();

        if (NoSearchBox is false)
        {
            try
            {
                await _searchInputRef.FocusAsync();
            }
            catch (JSException) { } // the element might not be ready/visible yet
        }
    }

    private async Task CloseCallout()
    {
        _isOpen = false;
        _searchText = null;
        _activeIndex = -1;
        _lastScrolledIndex = -1;
        await _js.BitExtrasSetPreventKeys(_dropdownButtonRef, _dropdownClosedKeys);
        await ToggleCallout();
    }

    private async Task ToggleCallout()
    {
        if (IsEnabled is false || IsDisposed) return;

        await _js.BitCalloutToggleCallout(
            dotnetObj: _dotnetObj!,
            componentId: _fieldGroupId,
            component: null,
            calloutId: _calloutId,
            callout: null,
            overlayId: _overlayId,
            isCalloutOpen: _isOpen,
            responsiveMode: BitResponsiveMode.None,
            dropDirection: DropDirection,
            isRtl: Dir is BitDir.Rtl,
            scrollContainerId: _scrollContainerId,
            scrollOffset: NoSearchBox ? 0 : 32,
            headerId: "",
            footerId: "",
            setCalloutWidth: false,
            fixedCalloutWidth: true,
            maxWindowWidth: 0);
    }

    private void HandleOnSearchInput(ChangeEventArgs e)
    {
        _searchText = e.Value?.ToString();

        // Re-evaluate the filtered list so the active option stays within range and
        // pressing Enter selects the first matching result instead of a stale one.
        GetFilteredCountries();
        _activeIndex = _viewItems.Count > 0 ? 0 : -1;
        _lastScrolledIndex = -1;
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
            await UpdateValueFromParts();

            await OnCountryChange.InvokeAsync(country);
        }
    }

    protected override async Task HandleOnStringValueChangeAsync(ChangeEventArgs e)
    {
        if (IsEnabled is false || ReadOnly) return;

        await AssignNumber(e.Value?.ToString());

        await UpdateValueFromParts();
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

    private void HandleOnInputFocusIn()
    {
        if (IsEnabled is false) return;

        _hasFocus = true;
        ClassBuilder.Reset();
    }

    private void HandleOnInputFocusOut()
    {
        if (IsEnabled is false) return;

        _hasFocus = false;
        ClassBuilder.Reset();
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        await base.DisposeAsync(disposing);

        _dotnetObj?.Dispose();

        try
        {
            await _js.BitExtrasDisposePreventKeys(_dropdownButtonRef);
            if (NoSearchBox is false)
            {
                await _js.BitExtrasDisposePreventKeys(_searchInputRef);
            }
            await _js.BitCalloutClearCallout(_calloutId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }
}
