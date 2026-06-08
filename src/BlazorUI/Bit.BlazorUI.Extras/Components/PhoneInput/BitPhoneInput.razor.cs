using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// BitPhoneInput is an input component for entering phone numbers with a searchable
/// country selector that shows the flag and the dialing code of each country.
/// </summary>
public partial class BitPhoneInput : BitInputBase<string?>
{
    private bool _isOpen;
    private bool _hasFocus;
    private int _activeIndex = -1;
    private string? _searchText;
    private List<BitCountry> _viewItems = [];
    private string _labelId = string.Empty;
    private string _inputId = string.Empty;
    private string _searchId = string.Empty;
    private string _calloutId = string.Empty;
    private string _overlayId = string.Empty;
    private string _dropdownId = string.Empty;
    private string _fieldGroupId = string.Empty;
    private string _scrollContainerId = string.Empty;
    private DotNetObjectReference<BitPhoneInput>? _dotnetObj;
    private ElementReference _searchInputRef;



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
    /// Updates the number input value as the user types (based on the 'oninput' HTML event).
    /// </summary>
    [Parameter] public bool Immediate { get; set; }

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
    /// The callback that is invoked when the selected country changes.
    /// </summary>
    [Parameter] public EventCallback<BitCountry?> OnCountryChange { get; set; }

    /// <summary>
    /// The placeholder text of the number input.
    /// </summary>
    [Parameter] public string? Placeholder { get; set; }

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
    /// </summary>
    public string? FullNumber => Country is null
                                    ? CurrentValue
                                    : $"+{Country.Code}{CurrentValue}";



    [JSInvokable("CloseCallout")]
    public async Task _CloseCalloutBeforeAnotherCalloutIsOpened()
    {
        if (_isOpen is false) return;

        _isOpen = false;
        _searchText = null;
        _activeIndex = -1;

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

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _dotnetObj = DotNetObjectReference.Create(this);
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
            _viewItems = Countries as List<BitCountry> ?? [.. Countries];
            return _viewItems;
        }

        var text = _searchText!.Trim();

        _viewItems = [.. Countries.Where(c => c.Name.Contains(text, StringComparison.InvariantCultureIgnoreCase) ||
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

    private async Task HandleOnDropdownKeyDown(KeyboardEventArgs e)
    {
        if (IsEnabled is false || ReadOnly) return;

        var key = e.Key;

        if (_isOpen is false)
        {
            // Enter/Space are handled by the button's native click (which calls
            // HandleOnDropdownClick -> OpenCallout), so they are intentionally
            // excluded here to avoid a double toggle.
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
            case " ":
            case "Spacebar":
                if (_activeIndex >= 0 && _activeIndex < _viewItems.Count)
                {
                    await HandleOnCountrySelect(_viewItems[_activeIndex]);
                }
                break;
        }
    }

    private async Task OpenCallout()
    {
        _isOpen = true;

        var selectedIndex = _viewItems.FindIndex(c => c.Iso2 == Country?.Iso2);
        _activeIndex = selectedIndex >= 0 ? selectedIndex : (_viewItems.Count > 0 ? 0 : -1);

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
    }

    private async Task HandleOnCountrySelect(BitCountry country)
    {
        if (IsEnabled is false || ReadOnly) return;

        await CloseCallout();

        if (await AssignCountry(country) is false) return;

        await OnCountryChange.InvokeAsync(country);
    }

    private async Task HandleOnNumberChange(ChangeEventArgs e)
    {
        if (IsEnabled is false || ReadOnly) return;

        await SetCurrentValueAsStringAsync(e.Value?.ToString());
    }

    private async Task HandleOnNumberInput(ChangeEventArgs e)
    {
        if (IsEnabled is false || ReadOnly) return;

        if (Immediate is false) return;

        await SetCurrentValueAsStringAsync(e.Value?.ToString());
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
            await _js.BitCalloutClearCallout(_calloutId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }
}
