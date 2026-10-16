using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// BitCountryPicker is an input component for picking a country out of a searchable list that shows the flag and
/// the name of each country - for the billing country, the nationality or the country of residence a form asks for.
/// <para>
/// The two-way bound <see cref="BitInputBase{T}.Value"/> is the selected <see cref="BitCountry"/>, one of the
/// <see cref="Countries"/> the picker offers (<see cref="BitCountries.All"/> by default).
/// </para>
/// </summary>
public partial class BitCountryPicker : BitInputBase<BitCountry?>
{
    private bool _hasFocus;
    private bool _fieldHasFocus;
    private bool _tabOutRegistered;
    private bool _hasOpened;
    private bool _fieldKeysWired;
    private bool _fieldKeysOpen;
    private bool _searchBoxKeysWired;
    private bool _internalIsOpenChange;
    private bool _pendingCalloutToggle;

    // Whether the callout is shown on the page, which is not yet the case in a render that runs while the handler
    // opening it is still awaiting: that render has IsOpen already, but the callout is only shown once the toggle is
    // asked for, so a scroll of its active option made there would be a scroll of a hidden list - lost, and marked
    // done all the same, which left the selected option out of sight on the first opening.
    private bool _isCalloutDisplayed;

    private bool _lastCanOpen = true;
    private string? _announcement;
    private bool _announcementMarker;
    private string? _lastErrorMessage;

    // The countries the picker offers, what the search term leaves of them, the one the keyboard is on and the
    // type-ahead that moves it - the part of the list BitPhoneInput walks and searches the same way.
    private readonly BitCountryList _list = new();

    private string _labelId = string.Empty;
    private string _errorId = string.Empty;
    private string _fieldId = string.Empty;
    private string _descriptionId = string.Empty;
    private string _searchId = string.Empty;
    private string _listId = string.Empty;
    private string _calloutId = string.Empty;
    private string _overlayId = string.Empty;
    private string _scrollContainerId = string.Empty;

    private ElementReference _searchInputRef;
    private DotNetObjectReference<BitCountryPicker>? _dotnetObj;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Gets or sets the cascading parameters for the country picker component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings - the market of the app through
    /// <see cref="DefaultCountry"/>, its country list, its look and its localized texts - to be applied to
    /// multiple country picker components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitCountryPickerParams.ParamName)]
    public BitCountryPickerParams? CascadingParameters { get; set; }



    /// <summary>
    /// The color kind of the fill of the country picker, for a field that sits on a surface other than the
    /// primary one.
    /// </summary>
    /// <remarks>
    /// An explicit value wins over <c>--bit-CountryPicker-background</c>; left unset, the fill is the primary kind
    /// unless that variable says otherwise.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitColorKind? Background { get; set; }

    /// <summary>
    /// The color kind of the frame of the country picker at rest and under a pointer, winning over the main color
    /// of <see cref="Color"/>, which keeps the focus ring.
    /// </summary>
    /// <remarks>
    /// An explicit value wins over <c>--bit-CountryPicker-border-color</c> and
    /// <c>--bit-CountryPicker-hover-border-color</c>; left unset, the frame is the primary border unless an explicit
    /// <see cref="Color"/> or those variables say otherwise.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitColorKind? Border { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitCountryPicker.
    /// </summary>
    [Parameter, ResetClassBuilder] public BitCountryPickerClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the country picker: the frame takes its main color and the focus ring its focus color.
    /// </summary>
    /// <remarks>
    /// An explicit value wins over <c>--bit-CountryPicker-focus-color</c> and the <c>--bit-CountryPicker-*</c> border
    /// colors (an explicit <see cref="Border"/> still wins the frame); left unset, the field is primary unless those
    /// variables say otherwise.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// The list of the countries the picker offers. Defaults to <see cref="BitCountries.All"/>.
    /// </summary>
    [Parameter] public ICollection<BitCountry> Countries { get; set; } = BitCountries.All;

    /// <summary>
    /// The country an unbound picker starts on when no DefaultValue is given - the market of an app, which a
    /// <see cref="BitCountryPickerParams"/> can hand every picker under it. A bound Value is left as it is, so a
    /// field the user never answered stays empty for its validation to see. It is matched to the list by its ISO
    /// code, so the picker starts on the country the list offers (with the name it shows), and on none when the list
    /// does not offer it.
    /// </summary>
    [Parameter] public BitCountry? DefaultCountry { get; set; }

    /// <summary>
    /// The description shown under the country picker, for the hint a label is too short to carry ("Where the
    /// card is registered"). It is tied to the field through aria-describedby, so a screen reader reads it with
    /// the field rather than as a stray line of text.
    /// </summary>
    [Parameter] public string? Description { get; set; }

    /// <summary>
    /// The custom template for the description of the country picker, taking the place of
    /// <see cref="Description"/> and tied to the field the same way.
    /// </summary>
    [Parameter] public RenderFragment? DescriptionTemplate { get; set; }

    /// <summary>
    /// Determines the allowed drop directions of the country list callout.
    /// </summary>
    [Parameter] public BitDropDirection DropDirection { get; set; } = BitDropDirection.TopAndBottom;

    /// <summary>
    /// The error message shown under the country picker, which also puts the field in its invalid state - for a
    /// rejection that comes from a server or from a rule no data annotation can express. Inside an EditContext the
    /// validation of the model is what decides the state instead, and its message is rendered by a
    /// ValidationMessage of its own.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The custom template for the error message of the country picker, taking the place of
    /// <see cref="ErrorMessage"/> and putting the field in the same invalid state.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public RenderFragment? ErrorMessageTemplate { get; set; }

    /// <summary>
    /// The countries to leave out of the list, which is how a list built from <see cref="Countries"/> is trimmed
    /// without being written out in full.
    /// </summary>
    [Parameter] public ICollection<BitCountry>? ExcludeCountries { get; set; }

    /// <summary>
    /// The url of the flag image of a country, replacing the flags that ship with the library - the place to point
    /// at a sprite, a CDN or an icon set of your own. Returning null or an empty string leaves that country with the
    /// built-in flag.
    /// </summary>
    [Parameter] public Func<BitCountry, string?>? FlagUrlSelector { get; set; }

    /// <summary>
    /// Renders the country picker in an invalid state without going through the validation of an EditContext.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Invalid { get; set; }

    /// <summary>
    /// Determines the opening state of the country list callout. (two-way bound)
    /// </summary>
    [Parameter, TwoWayBound]
    [CallOnSet(nameof(OnSetIsOpen))]
    public bool IsOpen { get; set; }

    /// <summary>
    /// The custom template for each country of the list.
    /// </summary>
    [Parameter] public RenderFragment<BitCountry>? ItemTemplate { get; set; }

    /// <summary>
    /// The label of the country picker shown above the field, which is also the accessible name of the field and
    /// of the country list.
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// The custom template for the label of the country picker.
    /// </summary>
    [Parameter] public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// The maximum height of the country list callout in pixels.
    /// </summary>
    [Parameter] public int? MaxHeight { get; set; }

    /// <summary>
    /// Removes the border of the country picker.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool NoBorder { get; set; }

    /// <summary>
    /// Hides the flag images of the countries in the field and in the list.
    /// </summary>
    [Parameter] public bool NoFlags { get; set; }

    /// <summary>
    /// The message to show when the search result of the country list is empty. It is also what a screen reader
    /// announces for an empty result.
    /// </summary>
    [Parameter] public string? NoResultsMessage { get; set; }

    /// <summary>
    /// The custom template to show when the search result of the country list is empty.
    /// </summary>
    [Parameter] public RenderFragment? NoResultsTemplate { get; set; }

    /// <summary>
    /// Hides the search box of the country list. The field then answers to type-ahead instead, open or closed: the
    /// letters typed on it move to the country whose name they start.
    /// </summary>
    [Parameter] public bool NoSearchBox { get; set; }

    /// <summary>
    /// Stops the keyboard navigation of the country list from wrapping around at its two ends.
    /// </summary>
    [Parameter] public bool NoWrapNavigation { get; set; }

    /// <summary>
    /// The callback that is invoked when the field loses focus.
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnBlur { get; set; }

    /// <summary>
    /// The callback that is invoked when the country list callout closes.
    /// </summary>
    [Parameter] public EventCallback OnClose { get; set; }

    /// <summary>
    /// The callback that is invoked when the field receives focus.
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnFocus { get; set; }

    /// <summary>
    /// The callback that is invoked when the country list callout opens.
    /// </summary>
    [Parameter] public EventCallback OnOpen { get; set; }

    /// <summary>
    /// The callback that is invoked when the search text of the country list changes.
    /// </summary>
    [Parameter] public EventCallback<string?> OnSearch { get; set; }

    /// <summary>
    /// The text shown in the field while no country is selected.
    /// </summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>
    /// The countries to pin to the top of the list, set apart from the rest of it by a rule.
    /// </summary>
    [Parameter] public ICollection<BitCountry>? PreferredCountries { get; set; }

    /// <summary>
    /// Shows the country list as a full height panel on small screens instead of an inline callout.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Responsive { get; set; }

    /// <summary>
    /// The aria-label of the close button of the responsive panel of the country list.
    /// </summary>
    [Parameter] public string? ResponsiveCloseButtonAriaLabel { get; set; }

    /// <summary>
    /// The icon of the close button of the responsive panel of the country list.
    /// Takes precedence over <see cref="ResponsiveCloseIconName"/> when both are set.
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// </summary>
    [Parameter] public BitIconInfo? ResponsiveCloseIcon { get; set; }

    /// <summary>
    /// The icon name of the close button of the responsive panel of the country list from the Fluent UI icon set.
    /// For external icon libraries, use <see cref="ResponsiveCloseIcon"/> instead.
    /// </summary>
    [Parameter] public string? ResponsiveCloseIconName { get; set; }

    /// <summary>
    /// The aria-label of the search box of the country list. Falls back to the search box placeholder, then to a
    /// default English value, when not provided.
    /// </summary>
    [Parameter] public string? SearchBoxAriaLabel { get; set; }

    /// <summary>
    /// The placeholder text of the search box of the country list.
    /// </summary>
    [Parameter] public string? SearchBoxPlaceholder { get; set; }

    /// <summary>
    /// What a screen reader announces for the number of countries a search term leaves in the list, in place of
    /// the default "1 country found" / "{count} countries found" - the way to translate that announcement. An empty
    /// result announces <see cref="NoResultsMessage"/> instead, and returning null or an empty string keeps a count
    /// from being announced at all.
    /// </summary>
    [Parameter] public Func<int, string?>? SearchResultsAnnouncement { get; set; }

    /// <summary>
    /// The size of the country picker.
    /// </summary>
    /// <remarks>
    /// An explicit value wins over the <c>--bit-CountryPicker-*</c> size variables (minimum height, font sizes,
    /// padding, flag size, row height); left unset, the field is medium unless those variables say otherwise.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitCountryPicker.
    /// </summary>
    [Parameter, ResetStyleBuilder] public BitCountryPickerClassStyles? Styles { get; set; }

    /// <summary>
    /// The tooltip (title attribute) of the country picker.
    /// </summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// Renders the country picker with only a bottom border instead of a full one.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Underlined { get; set; }

    /// <summary>
    /// The custom template for the content of the field, receiving the selected country (null while none is).
    /// </summary>
    [Parameter] public RenderFragment<BitCountry?>? ValueTemplate { get; set; }



    /// <summary>
    /// Opens the country list callout.
    /// </summary>
    public Task OpenAsync() => InvokeAsync(async () =>
    {
        // Called from outside a Blazor event handler as well as from one, so the render the opened callout needs
        // is asked for here rather than left to the caller - and on the renderer's own context, since the caller
        // can be on any thread.
        if (Disabled || ReadOnly || IsOpen) return;

        await OpenCallout();

        StateHasChanged();
    });

    /// <summary>
    /// Closes the country list callout.
    /// </summary>
    public Task CloseAsync() => InvokeAsync(async () =>
    {
        if (IsOpen is false) return;

        await CloseCallout();

        StateHasChanged();
    });

    /// <summary>
    /// Selects the given country exactly as picking it in the list would, so the same events fire. The country is
    /// matched to the list by its ISO code, so the value is the one the list offers (with the name it shows), and a
    /// country the list does not offer - one left out of <see cref="Countries"/> or named in
    /// <see cref="ExcludeCountries"/> - is not selected.
    /// </summary>
    public Task SelectCountryAsync(BitCountry country) => InvokeAsync(async () =>
    {
        var offered = _list.Resolve(country);

        if (offered is null) return;

        await SelectCountry(offered, focusField: false);

        // Unlike a click in the list, this call does not arrive through an event handler of the component, so
        // nothing re-renders it on its own.
        StateHasChanged();
    });



    [JSInvokable("CloseCallout")]
    public async Task _CloseCalloutBeforeAnotherCalloutIsOpened()
    {
        if (IsOpen is false) return;

        _isCalloutDisplayed = false;

        // The JS side has already hidden this callout to make room for another one, or because the focus moved on
        // from both the field and the list, so the state is brought in line without toggling it a second time.
        if (await AssignIsOpenInternal(false) is false) return;

        // The dismissal has taken the tab order around the field back along with the callout (see ToggleCallout).
        _tabOutRegistered = false;

        _list.Reset();

        await ClearSearchBoxElement();

        await SyncFieldKeys();

        // The callout this component had open is gone to make room for another one's, so whatever the focus was
        // on inside it is gone along with it.
        SetHasFocus(_fieldHasFocus);

        await OnClose.InvokeAsync();

        await InvokeAsync(StateHasChanged);
    }

    [JSInvokable("OnTabOut")]
    public async Task _OnTabOut()
    {
        // The list may have been closed by something else - an outside click, the IsOpen parameter - while this
        // call was on its way, and closing it again would reach the JS side to toggle a callout that is not shown.
        if (IsDisposing || IsOpen is false) return;

        await CloseCallout();

        // An IsOpen bound one way keeps the list open whatever the keyboard does, and the ring stays on with it.
        if (IsOpen) return;

        // The JS side has already moved the focus on past the field, so the ring CloseCallout dropped stays off
        // unless the focus has landed back in it.
        SetHasFocus(_fieldHasFocus);

        await InvokeAsync(StateHasChanged);
    }



    protected override string RootElementClass => "bit-cnp";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => NoBorder ? "bit-cnp-nbd" : string.Empty);

        ClassBuilder.Register(() => Underlined ? "bit-cnp-und" : string.Empty);

        ClassBuilder.Register(() => ReadOnly ? "bit-cnp-rdo" : string.Empty);

        ClassBuilder.Register(() => Disabled is false && Required ? "bit-cnp-req" : string.Empty);

        ClassBuilder.Register(() => _hasFocus ? "bit-cnp-fcs" : string.Empty);

        // Size, Color, Background and Border publish nothing while they are unset, which is what lets the stylesheet
        // tell a default from a choice: the public --bit-CountryPicker-* variables restyle the default and never an
        // explicit value.
        ClassBuilder.Register(() => BitCssClasses.Size(Size, "bit-cnp"));

        ClassBuilder.Register(() => ColorClass ?? string.Empty);

        ClassBuilder.Register(() => Background switch
        {
            BitColorKind.Primary => "bit-cnp-bpr",
            BitColorKind.Secondary => "bit-cnp-bse",
            BitColorKind.Tertiary => "bit-cnp-btr",
            BitColorKind.Transparent => "bit-cnp-btn",
            _ => string.Empty
        });

        ClassBuilder.Register(() => Border switch
        {
            BitColorKind.Primary => "bit-cnp-brp",
            BitColorKind.Secondary => "bit-cnp-brs",
            BitColorKind.Tertiary => "bit-cnp-brt",
            BitColorKind.Transparent => "bit-cnp-brn",
            _ => string.Empty
        });
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitCountryPickerParams))]
    protected override void OnInitialized()
    {
        // The cascade is read before the default country is applied, since the market of an app is the one default
        // it is most likely to hand every picker under it.
        CascadingParameters?.UpdateParameters(this);

        _labelId = $"BitCountryPicker-{UniqueId}-label";
        _errorId = $"BitCountryPicker-{UniqueId}-error";
        _fieldId = $"BitCountryPicker-{UniqueId}-field";
        _descriptionId = $"BitCountryPicker-{UniqueId}-description";
        _searchId = $"BitCountryPicker-{UniqueId}-search";
        _listId = $"BitCountryPicker-{UniqueId}-list";
        _calloutId = $"BitCountryPicker-{UniqueId}-callout";
        _overlayId = $"BitCountryPicker-{UniqueId}-overlay";
        _scrollContainerId = $"BitCountryPicker-{UniqueId}-scroll-container";

        // The list is built before the defaults are applied, since a default is the country of the list that carries
        // its ISO code - the one with the name the list shows (a translated one, say) - and none at all when the
        // list does not offer it.
        _list.Update(Countries, ExcludeCountries, PreferredCountries);

        if (ValueHasBeenSet is false && DefaultValueHasBeenSet is false && DefaultCountry is not null)
        {
            Value = _list.Resolve(DefaultCountry);
        }

        SetDefaultValue();

        if (ValueHasBeenSet is false && DefaultValueHasBeenSet)
        {
            Value = _list.Resolve(Value);
        }

        base.OnInitialized();
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitCountryPickerParams))]
    protected override void OnParametersSet()
    {
        // The cascade is read first, so that the country list and the classes below are computed from the
        // parameters the field ends up with rather than from the ones it was written with.
        CascadingParameters?.UpdateParameters(this);

        // The list is read on every render while the callout is open, so it is only built again when its source
        // actually changes; without that the default country set (BitCountries.All) would be copied into a new list
        // of ~240 items each cycle.
        _list.Update(Countries, ExcludeCountries, PreferredCountries);

        // A message that says the value was rejected has to be heard as well as seen, and it is written into a part
        // of the page a screen reader has already read past by the time it appears.
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
        // A list is only ever shown on a field that can be used. One the page disables or makes read-only while it
        // is open - a form disabled for its submit - is closed with it, rather than left on screen with nothing that
        // could pick from it or close it; and one an IsOpen bound one way keeps open is hidden while the field cannot
        // be used and shown again once it can (see IsCalloutShown), which takes a toggle of its own either way.
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

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _dotnetObj = DotNetObjectReference.Create(this);
        }

        // Wire up deterministic key suppression once the elements exist. Done on every render rather than only on
        // the first one, because the search box comes and goes with NoSearchBox, and a search box that appeared
        // later would otherwise let its navigation keys move the caret as well as the selection.
        await WirePreventKeys();

        // The callout is shown and positioned only once the render that filled its list has reached the DOM, so the
        // country list can stay out of the document until the first opening while the callout is still measured
        // against its real content.
        if (_pendingCalloutToggle)
        {
            _pendingCalloutToggle = false;

            // A callout opened through the IsOpen parameter before the first render had no country list to point
            // at yet, so the active option is settled here instead.
            var activeIndexSettled = false;
            if (IsCalloutShown && _list.ActiveIndex < 0)
            {
                ResetActiveIndexToSelection();

                activeIndexSettled = true;
            }

            await ToggleCallout();

            // The open list is driven from whichever element carries aria-activedescendant, so the focus is put on
            // it: the search box when there is one, and the field itself when there is not - a callout opened
            // through the IsOpen parameter would otherwise leave the keys nowhere to go.
            if (IsCalloutShown)
            {
                try
                {
                    if (NoSearchBox)
                    {
                        await InputElement.FocusAsync();
                    }
                    else
                    {
                        await _searchInputRef.FocusAsync();
                    }
                }
                catch (JSException) { } // the element might not be ready/visible yet
            }

            // The active option was decided after the render that got here, so what names it - the active class on
            // the option and the aria-activedescendant pointing at it - is asked for again instead of waiting for
            // the next render to come along.
            if (activeIndexSettled)
            {
                StateHasChanged();
            }
        }

        // Keep the active option visible during keyboard navigation. Done after render so the option element is
        // guaranteed to exist and the callout is laid out.
        if (_isCalloutDisplayed && IsCalloutShown && _list.ActiveIndex >= 0 && _list.ActiveIndex != _list.LastScrolledIndex)
        {
            _list.LastScrolledIndex = _list.ActiveIndex;
            await _js.BitExtrasScrollOptionIntoView(GetOptionId(_list.ActiveIndex));
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    // Keeps the keydown listeners of the two elements that navigate the country list in step with the elements
    // themselves, and remembers which ones were wired so disposal can find them again whatever the parameters say
    // by then.
    private async Task WirePreventKeys()
    {
        if (_fieldKeysWired is false)
        {
            _fieldKeysWired = true;
            _fieldKeysOpen = IsCalloutShown;
            await _js.BitExtrasSetPreventKeys(InputElement, _fieldKeysOpen ? BitCountryList.TriggerOpenKeys : BitCountryList.TriggerClosedKeys);
        }
        else
        {
            // Whatever opened or closed the list - the IsOpen parameter or the field being disabled under it, which
            // swap no keys of their own - the keys of the field are put in step with it once the render is through.
            await SyncFieldKeys();
        }

        if (NoSearchBox is false)
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

    // The text a form posts or binds is the ISO 3166-1 alpha-2 code, the one way of writing a country down that
    // every other is resolved from.
    protected override string? FormatValueAsString(BitCountry? value) => value?.Iso2;

    // A country written down any of the ways BitCountries resolves - an ISO code, a name, a dialing code - is read
    // back as the one the picker offers, so a value parsed from a text is never a country the list does not hold.
    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out BitCountry? result, [NotNullWhen(false)] out string? parsingErrorMessage)
    {
        var offered = _list.Resolve(BitCountries.Find(value));

        if (offered is not null)
        {
            result = offered;
            parsingErrorMessage = null;
            return true;
        }

        result = default;
        parsingErrorMessage = $"The {DisplayName ?? FieldIdentifier.FieldName} field is not a valid country.";
        return false;
    }



    // The size of the control is driven by CSS variables, and the callout is rendered outside the root element, so
    // the class carrying them is repeated on it: without that the rows and the flags of the list would ignore an
    // explicit Size. Like the root's, it is empty while the Size is unset, so the public --bit-CountryPicker-*
    // variables restyle the default rows.
    private string SizeClass => BitCssClasses.Size(Size, "bit-cnp");

    // The callout is rendered outside the root as well, and the keyboard cue of its active row takes the focus color
    // of the role, so the class is repeated on it the same way the size class is.
    private string? ColorClass => Color switch
    {
        BitColor.Primary => "bit-cnp-pri",
        BitColor.Secondary => "bit-cnp-sec",
        BitColor.Tertiary => "bit-cnp-ter",
        BitColor.Info => "bit-cnp-inf",
        BitColor.Success => "bit-cnp-suc",
        BitColor.Warning => "bit-cnp-wrn",
        BitColor.SevereWarning => "bit-cnp-swr",
        BitColor.Error => "bit-cnp-err",
        BitColor.PrimaryBackground => "bit-cnp-pbg",
        BitColor.SecondaryBackground => "bit-cnp-sbg",
        BitColor.TertiaryBackground => "bit-cnp-tbg",
        BitColor.PrimaryForeground => "bit-cnp-pfg",
        BitColor.SecondaryForeground => "bit-cnp-sfg",
        BitColor.TertiaryForeground => "bit-cnp-tfg",
        BitColor.PrimaryBorder => "bit-cnp-pbr",
        BitColor.SecondaryBorder => "bit-cnp-sbr",
        BitColor.TertiaryBorder => "bit-cnp-tbr",
        _ => null
    };

    // Only a field that can be used opens its list: the field, the keyboard and OpenAsync all refuse to open the list
    // of a disabled or read-only one.
    private bool CanOpen => Disabled is false && ReadOnly is false;

    // Whether the list is actually on screen. An IsOpen the page holds at true on a field that cannot be used is
    // refused the way the field refuses to open (see OnParametersSetAsync), and one bound one way, which the picker
    // cannot set back to false, leaves the list hidden - with nothing it names and no overlay - until the field can
    // be used again.
    private bool IsCalloutShown => IsOpen && CanOpen;

    // A country is the selected one when it carries the selected ISO code: a page that offers countries of its own
    // (translated names, say) can bind a Value that is a different instance of the same country.
    private bool IsSelected(BitCountry country) => CurrentValue is not null && country.Iso2.Equals(CurrentValue.Iso2, StringComparison.OrdinalIgnoreCase);

    private bool HasLabel => Label.HasValue() || LabelTemplate is not null;

    // The field is named by the visible label; aria-labelledby takes precedence over aria-label, so it is only
    // pointed at while no name of its own was given. A field with neither is still named, since a combobox has no
    // name to take from its content.
    private string? FieldLabelledBy => HasLabel && AriaLabel.HasNoValue() ? _labelId : null;

    private string? FieldAriaLabel => FieldLabelledBy is null ? (AriaLabel ?? "Country") : null;

    // The list is named the same way the field it belongs to is.
    private string? ListLabelledBy => FieldLabelledBy;

    private string? ListAriaLabel => FieldLabelledBy is null ? (AriaLabel ?? "Countries") : null;

    // The tooltip is written on the root, so it also covers the label, and on the field itself, so it reaches a
    // keyboard as well as a pointer. A title the consumer put on the field through InputHtmlAttributes is kept,
    // since the explicit attribute would otherwise write a null over it. Without either, the field is titled with
    // the name of its country, which a narrow field cuts short.
    private string? FieldTitle => Title ?? GetSplattedInputAttribute("title") ?? CurrentValue?.Name;

    private bool HasDescription => Description.HasValue() || DescriptionTemplate is not null;

    private bool HasErrorMessage => ErrorMessage.HasValue() || ErrorMessageTemplate is not null;

    // The invalid state the consumer forces, either as a flag or by handing the field a message to show.
    protected override bool HasError => Invalid || HasErrorMessage;

    // What is written under the field is what describes it, so both lines are pointed at rather than one of them. A
    // value the consumer put on the field itself is kept: a field with a description of its own would otherwise lose
    // it the moment the component has anything to reference.
    private string? AriaDescribedBy
    {
        get
        {
            if (HasErrorMessage is false && HasDescription is false) return GetSplattedInputAttribute("aria-describedby");

            var ids = string.Join(' ', new[]
            {
                GetSplattedInputAttribute("aria-describedby"),
                HasErrorMessage ? _errorId : null,
                HasDescription ? _descriptionId : null
            }.Where(id => id.HasValue()));

            return ids.HasValue() ? ids : null;
        }
    }

    // The forced invalid state and the one the EditContext produces end up on the same attribute, and the base class
    // writes the latter into the splatted attributes, so the value of the consumer is read back here instead of
    // being overwritten by the explicit attribute of the field.
    private string? AriaInvalid => IsInvalid ? "true" : GetSplattedInputAttribute("aria-invalid");

    private string GetOptionId(int index) => $"{_calloutId}-opt-{index}";

    // The display is written here rather than in the stylesheet because it is what the component toggles the layer
    // with, and Styles.Overlay is appended after it so a value written for the overlay still wins.
    private string GetOverlayStyles() => $"display:{(IsCalloutShown ? "block" : "none")};{Styles?.Overlay}";

    // Null for a country with no flag image at all, which is drawn as an empty box of the size of one.
    private string? GetFlagUrl(BitCountry country) => BitCountryList.GetFlagUrl(country, FlagUrlSelector);

    // A single live region carries everything the picker has to say, and a screen reader only announces what changes
    // inside one: repeating the same sentence (two searches with the same number of results) would otherwise be
    // silent, so an invisible marker alternates with it.
    private void Announce(string? text)
    {
        _announcementMarker = !_announcementMarker;
        _announcement = text.HasValue() && _announcementMarker ? text + '​' : text;
    }

    private async Task HandleOnLabelClick()
    {
        if (Disabled) return;

        try
        {
            await InputElement.FocusAsync();
        }
        catch (JSException) { } // the element might not be ready/visible yet
    }

    private async Task HandleOnFieldClick()
    {
        if (Disabled || ReadOnly) return;

        if (IsOpen)
        {
            await CloseCallout();

            // Clicking the field is what closed the callout, so the focus is on the field and the ring CloseCallout
            // dropped belongs right back on.
            SetHasFocus(true);
        }
        else
        {
            await OpenCallout();
        }
    }

    private Task HandleOnFieldKeyDown(KeyboardEventArgs e) => HandleOnCalloutKeyDown(e, false);

    private Task HandleOnSearchKeyDown(KeyboardEventArgs e) => HandleOnCalloutKeyDown(e, true);

    private async Task HandleOnCalloutKeyDown(KeyboardEventArgs e, bool fromSearchBox)
    {
        if (Disabled || ReadOnly) return;

        var key = e.Key;

        if (IsOpen is false)
        {
            // While closed, Enter/Space are handled by the field's native click (which calls HandleOnFieldClick ->
            // OpenCallout), so they are intentionally not treated as open triggers here to avoid a double toggle.
            // Their defaults are not suppressed (see BitCountryList.TriggerClosedKeys) so the native click can open
            // the callout.
            if (key is "ArrowDown" or "ArrowUp" or "Home" or "End")
            {
                // Home and End say where in the list to land, the way they do on a native select.
                await OpenCallout(key is "Home" or "End" ? key : null);
            }
            else if (BitCountryList.IsTypeAheadKey(e))
            {
                // Typing on the closed field opens it on what was typed, the way it does on a native select: on the
                // country the letters start when there is no search box, and as the first letter of the search term
                // when there is one.
                await OpenCallout(key);
            }

            return;
        }

        // A list with a search box is driven from that box, which is the combobox naming the active option. The field
        // only holds the focus of the open list when a Shift+Tab has taken it back there, and the keys that work the
        // list hand the focus back to the search box along with what they do, rather than moving an option the
        // field does not name.
        if (fromSearchBox is false && NoSearchBox is false && await HandleOnFieldOfSearchableListKeyDown(e)) return;

        switch (key)
        {
            case "Escape":
                await CloseCallout();
                await FocusField();
                break;

            case "ArrowUp" when e.AltKey:
                // Alt+Up closes the list the way the combobox pattern has it: a select-only one takes the option the
                // arrows are on, while one with a search box only gives the focus back to its field.
                if (fromSearchBox is false && _list.Active is { } active)
                {
                    await HandleOnCountrySelect(active);
                }
                else
                {
                    await CloseCallout();
                    await FocusField();
                }
                break;

            case "Tab":
                // While the list is in the tab order after the field (see ToggleCallout), the Tab key is the JS side's
                // to answer, and the focus leaving the field and the list closes the list wherever it goes. A list
                // without a search box keeps the focus on the field, so its Tab moves on from there, and the list
                // must not stay open behind it. Its default is deliberately not suppressed, so the focus move itself
                // still happens.
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
                // When the search box is visible the space key must remain available for typing, so it is only
                // treated as a selection key when focus is on the field (i.e. there is no search box).
                if (fromSearchBox) break;

                // A space typed in the middle of a type-ahead term is part of the name being typed ("united s" for
                // the United States), not a choice of the country the term has reached so far.
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

                // Without a search box to type into, the letters typed on the open list jump to the country whose
                // name they start, the way a native select behaves.
                if (fromSearchBox is false && BitCountryList.IsTypeAheadKey(e))
                {
                    _list.TypeAhead(key);
                }
                break;
        }
    }

    // The keys of the open list pressed on the field of a list that has a search box (see HandleOnCalloutKeyDown).
    // The navigation keys move the active option the way they do anywhere in the list - Home and End included, since
    // they were not pressed in the search box whose caret they would move - and hand the focus to the search box,
    // which announces it; a letter goes into the search box as if it had been typed there; Enter and Space press the
    // field, which closes the list as a click on it does; Alt+Up closes it the way it does from the search box.
    // Escape and Tab are left to the handling they get anywhere in the list. Reports whether the key was handled here.
    private async Task<bool> HandleOnFieldOfSearchableListKeyDown(KeyboardEventArgs e)
    {
        switch (e.Key)
        {
            case "ArrowUp" when e.AltKey:
                await CloseCallout();
                await FocusField();
                return true;

            case "Enter":
            case " ":
            case "Spacebar":
                await CloseCallout();

                // The focus is still on the field, so the ring CloseCallout dropped belongs right back on.
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

    private async Task OpenCallout(string? openingKey = null)
    {
        if (CanOpen is false) return;

        _hasOpened = true;

        if (await AssignIsOpenInternal(true) is false) return;

        // Settle the active option before the list has rendered: opening via the keyboard (ArrowDown) would
        // otherwise leave it at -1, making the first Enter select nothing.
        ResetActiveIndexToSelection();

        _list.ResetTypeAhead();

        // The key the list was opened with is applied before anything is awaited. The keys pressed while the open's round
        // trips run are answered by the list that is already open, so the first key, applied after them, would land out of
        // order: "ger" typed at once would end on Eritrea rather than Germany.
        var searched = ApplyOpeningKey(openingKey);

        await SyncFieldKeys();

        _pendingCalloutToggle = true;

        await OnOpen.InvokeAsync();

        if (searched)
        {
            await OnSearch.InvokeAsync(_list.SearchText);
        }
    }

    // Lands the list a key opened where that key says: Home and End on the first and the last country, a letter on the
    // country it starts, or as the first letter of the search term when there is a search box. Reports whether it started
    // a search, whose OnSearch is raised once the list is open.
    private bool ApplyOpeningKey(string? key)
    {
        if (key is null) return false;

        if (key is "Home" or "End")
        {
            _list.Navigate(key, fromSearchBox: false, NoWrapNavigation);
            return false;
        }

        if (NoSearchBox)
        {
            _list.TypeAhead(key);
            return false;
        }

        SearchFor(key);
        return true;
    }

    private async Task CloseCallout()
    {
        if (IsOpen is false) return;

        if (await AssignIsOpenInternal(false) is false) return;

        _list.Reset();

        await ClearSearchBoxElement();

        await SyncFieldKeys();

        _pendingCalloutToggle = true;

        // The focus may well have been inside the callout that is now gone, and the focusout it fired on its way
        // there was ignored while the callout was open, so the ring is dropped here. The flows that put the focus
        // back into the field - Escape, picking a country, clicking the field - turn it on again themselves.
        SetHasFocus(false);

        await OnClose.InvokeAsync();
    }

    // Dismissing the callout by hand - a click outside it, or the close button of the responsive panel - hides
    // whatever inside it had the focus, which would otherwise fall to the document body and strand a keyboard user
    // at the top of the page, so it is given back to the field the list was opened from.
    private async Task CloseCalloutAndRestoreFocus()
    {
        if (IsOpen is false) return;

        await CloseCallout();

        await FocusField();
    }

    // The keys the field prevents follow whether its list is on screen, and are swapped the moment that changes, so
    // the very next key is answered the way the list now is: Enter and Space are left to the native click of a
    // closed field, which opens it, and are kept from an open one, where they pick a country and the click would
    // open the list again straight after.
    private async Task SyncFieldKeys()
    {
        if (_fieldKeysWired is false || _fieldKeysOpen == IsCalloutShown) return;

        _fieldKeysOpen = IsCalloutShown;

        await _js.BitExtrasSetPreventKeys(InputElement, _fieldKeysOpen ? BitCountryList.TriggerOpenKeys : BitCountryList.TriggerClosedKeys);
    }

    // Puts the active option on the current selection, or on the first country when nothing is selected yet, so
    // the list always opens with something for Enter to act on.
    private void ResetActiveIndexToSelection() => _list.ResetActiveToSelection(IsSelected);

    // Clears the text left in the search box element itself. The value attribute alone is not enough: an input
    // keeps the value its user typed as a DOM property, which a re-render of the same element does not necessarily
    // put back, and the next opening would show a stale term above a list that is no longer filtered by it.
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

    // See OnSetIsOpen: the flows that follow AssignIsOpen with their own callout toggle mark the change as internal,
    // so the hook does not arrange a second one.
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
        // The internal open and close flows arrange the toggle themselves. The hook only reacts to a change pushed
        // from the outside through the IsOpen parameter, which otherwise has no path to the JS side that actually
        // shows and hides the callout. Before the first render there is no element to toggle either, which
        // OnAfterRenderAsync takes care of by running the pending toggle as soon as there is one; it is also where
        // the keys of the field are put in step with the list (see WirePreventKeys), and an open on a field that
        // cannot be used is refused before that (see OnParametersSetAsync).
        if (_internalIsOpenChange) return;

        if (IsOpen)
        {
            _hasOpened = true;

            ResetActiveIndexToSelection();
        }
        else
        {
            _list.Reset();

            // A callout closed through the IsOpen parameter takes whatever focus it holds down with it, and no
            // focusout of the field is fired for it.
            SetHasFocus(false);
        }

        _pendingCalloutToggle = true;
    }

    // The open list is relocated to the end of the body, so the browser's own tab order would run from its search
    // box off the end of the page and back from it into whatever ends the page, leaving the list open behind its
    // overlay either way. While it is open the search box is put into the tab order right after the field instead
    // (see Utils.setupTabOut): Tab moves on through the rest of the list - the responsive close button, rendered
    // after the search box for that reason - to whatever follows the field and closes the list through OnTabOut, and
    // Shift+Tab from the search box goes back to the field, leaving the list open for the Tab that brings the user
    // back in. A list without a search box has nothing to tab into: the focus stays on the field, which is the
    // select-only combobox the list belongs to, so the Tab key is left to move on from there as it would anyway.
    // Both ride on the toggle itself, which registers them with the open and takes them back with the close or a
    // dismissal, so they cost no round trip of their own and are never left behind a list that is gone.
    private async Task ToggleCallout()
    {
        if (IsDisposed) return;

        // What is shown is what the field can show: a list the field was disabled under is closed on the JS side all
        // the same, rather than left on screen with nothing that could close it.
        var shown = IsCalloutShown;
        var tabOut = shown && NoSearchBox is false;

        // A close takes the tab order back whatever it does, and an open that fails registers none.
        _tabOutRegistered = false;

        // The callout is positioned against the field, but the root is the element whose Style, classes and theme
        // the callout and the overlay have to go on inheriting once they are relocated to the body, so it is named
        // apart as the rootId.
        await _js.BitCalloutToggleCallout(
            dotnetObj: _dotnetObj!,
            componentId: _fieldId,
            component: null,
            calloutId: _calloutId,
            callout: null,
            overlayId: _overlayId,
            isCalloutOpen: shown,
            responsiveMode: Responsive ? BitResponsiveMode.Panel : BitResponsiveMode.None,
            dropDirection: DropDirection,
            isRtl: ExplicitRtl,
            scrollContainerId: _scrollContainerId,
            scrollOffset: NoSearchBox ? 0 : 32,
            headerId: "",
            footerId: "",
            setCalloutWidth: true,
            fixedCalloutWidth: false,
            maxWindowWidth: 0,
            maxHeight: MaxHeight is > 0 ? MaxHeight.Value : 0,
            dismissOnFocusLeave: true,
            rootId: _Id,
            tabOutTriggerId: tabOut ? _fieldId : "");

        // Registered in the same call that showed the list, after the same lookups the showing depends on.
        _tabOutRegistered = tabOut;

        _isCalloutDisplayed = shown;
    }

    private Task HandleOnSearchInput(ChangeEventArgs e) => ApplySearchText(e.Value?.ToString());

    private async Task ApplySearchText(string? text)
    {
        SearchFor(text);

        await OnSearch.InvokeAsync(_list.SearchText);
    }

    private void SearchFor(string? text)
    {
        // The active option moves to the first match, so pressing Enter selects it instead of a stale one.
        var count = _list.Search(text);

        // The list the term filtered down to is inside a callout, so its length is only ever seen: it is announced
        // here for the users who cannot see it.
        Announce(BitCountryList.GetSearchAnnouncement(count, NoResultsMessage, SearchResultsAnnouncement));
    }

    private Task HandleOnCountrySelect(BitCountry country) => SelectCountry(country, focusField: true);

    private async Task SelectCountry(BitCountry country, bool focusField)
    {
        if (Disabled || ReadOnly) return;

        // The focus stays on the field of a list without a search box, so the field naming the new country is not
        // announced again, and the choice is said out loud instead.
        var focusStays = NoSearchBox && IsOpen;

        await CloseCallout();

        if (IsSelected(country) is false)
        {
            var previous = CurrentValue;

            await SetCurrentValueAsync(country);

            // A Value bound one way (with no ValueChanged and no OnChange) cannot be changed by the picker, so the
            // field keeps showing the country it was given and nothing is announced for a choice that never landed.
            if (ReferenceEquals(previous, CurrentValue) is false && focusStays)
            {
                Announce(country.Name);
            }
        }

        if (focusField)
        {
            await FocusField();
        }
    }

    private async Task FocusField()
    {
        try
        {
            await InputElement.FocusAsync();

            // The field is what the focus is back on, so the ring the closing callout dropped is put back on with the
            // focus itself instead of a render later, once the focus event finds its way back.
            SetHasFocus(true);
        }
        catch (JSException) { } // the element might not be ready/visible yet
    }

    // The focus ring is on while the focus is on the field, and stays on while the callout has taken it away from
    // there. Only the field reports its focus, so what happens inside the callout is told to the ring by the flows
    // that make it happen rather than by an event.
    private void SetHasFocus(bool value)
    {
        if (_hasFocus == value) return;

        _hasFocus = value;

        ClassBuilder.Reset();
    }

    // The field is a button with nothing focusable inside it, so its focus and blur are the only focus events it
    // has: one handler per direction keeps the ring in step and reports the event, in a single dispatch.
    private async Task HandleOnFieldFocus(FocusEventArgs e)
    {
        if (Disabled) return;

        _fieldHasFocus = true;

        SetHasFocus(true);

        await OnFocus.InvokeAsync(e);
    }

    private async Task HandleOnFieldBlur(FocusEventArgs e)
    {
        if (Disabled) return;

        // Where the focus actually is, apart from the ring that stays on for the open callout (see _OnTabOut).
        _fieldHasFocus = false;

        // The focus ring belongs to the field, and the search box of an open callout is part of that field as far
        // as the user is concerned, so the ring stays on while it is open. Closing the callout is what turns it off
        // again (see CloseCallout).
        if (IsCalloutShown is false)
        {
            SetHasFocus(false);
        }

        await OnBlur.InvokeAsync(e);
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

            if (_fieldKeysWired)
            {
                await _js.BitExtrasDisposePreventKeys(InputElement);
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
