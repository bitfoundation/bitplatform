using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// BitTimeZonePicker is an input component for picking a time zone out of a searchable list that shows the offset from
/// UTC, the place and the country of each time zone - for the time zone of a user profile, of a meeting or of a report,
/// and for the <c>TimeZone</c> of BitCalendar, BitDatePicker, BitDateRangePicker and BitTimePicker.
/// <para>
/// The two-way bound <see cref="BitInputBase{TValue}.Value"/> is either a <see cref="System.TimeZoneInfo"/> or the IANA id
/// of the time zone as a <see cref="string"/> (a <see cref="BitTimeZone"/> is taken as well), whichever the page binds it
/// to. The list is the same on every runtime - Blazor Server on Windows or Linux, WebAssembly, Hybrid - because it is
/// <see cref="BitTimeZones.All"/> rather than what the runtime happens to list, and a time zone is always written down
/// under its canonical IANA id, while a value handed in under a Windows id or an older IANA id is still recognized.
/// </para>
/// </summary>
/// <typeparam name="TValue">
/// The type of the value: <see cref="System.TimeZoneInfo"/>, <see cref="string"/> (the IANA id) or <see cref="BitTimeZone"/>.
/// </typeparam>
public partial class BitTimeZonePicker<TValue> : BitInputBase<TValue>
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

    // The time zone of the list the current value stands for, worked out once per value rather than once per row of
    // every render: an open list asks every one of its rows whether it is the selected one.
    private bool _selectedResolved;
    private object? _selectedOf;
    private List<BitTimeZone>? _selectedIn;
    private BitTimeZone? _selected;

    // The time zones the picker offers in the order of their offsets, what the search term leaves of them, the one the
    // keyboard is on and the type-ahead that moves it.
    private readonly BitTimeZoneList _list = new();

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
    private DotNetObjectReference<BitTimeZonePicker<TValue>>? _dotnetObj;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Gets or sets the cascading parameters for the time zone picker component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings - the time zones an app offers, whether the
    /// visitor's own one is detected, its look and its localized texts - to be applied to multiple time zone picker
    /// components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitTimeZonePickerParams.ParamName)]
    public BitTimeZonePickerParams? CascadingParameters { get; set; }



    /// <summary>
    /// The color kind of the fill of the time zone picker, for a field that sits on a surface other than the primary one.
    /// </summary>
    /// <remarks>
    /// An explicit value wins over <c>--bit-TimeZonePicker-background</c>; left unset, the fill is the primary kind
    /// unless that variable says otherwise.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitColorKind? Background { get; set; }

    /// <summary>
    /// The color kind of the frame of the time zone picker at rest and under a pointer, winning over the main color of
    /// <see cref="Color"/>, which keeps the focus ring.
    /// </summary>
    /// <remarks>
    /// An explicit value wins over <c>--bit-TimeZonePicker-border-color</c> and
    /// <c>--bit-TimeZonePicker-hover-border-color</c>; left unset, the frame is the primary border unless an explicit
    /// <see cref="Color"/> or those variables say otherwise.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitColorKind? Border { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitTimeZonePicker.
    /// </summary>
    [Parameter, ResetClassBuilder] public BitTimeZonePickerClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the time zone picker: the frame takes its main color and the focus ring its focus color.
    /// </summary>
    /// <remarks>
    /// An explicit value wins over <c>--bit-TimeZonePicker-focus-color</c> and the <c>--bit-TimeZonePicker-*</c> border
    /// colors (an explicit <see cref="Border"/> still wins the frame); left unset, the field is primary unless those
    /// variables say otherwise.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// The description shown under the time zone picker, for the hint a label is too short to carry ("Dates are shown
    /// in this time zone"). It is tied to the field through aria-describedby, so a screen reader reads it with the
    /// field rather than as a stray line of text.
    /// </summary>
    [Parameter] public string? Description { get; set; }

    /// <summary>
    /// The custom template for the description of the time zone picker, taking the place of <see cref="Description"/>
    /// and tied to the field the same way.
    /// </summary>
    [Parameter] public RenderFragment? DescriptionTemplate { get; set; }

    /// <summary>
    /// Preselects the time zone of the user's device when the picker has no value: the one the browser runs in, which
    /// on Blazor Server is the visitor's own rather than the server's <see cref="TimeZoneInfo.Local"/>. It is read once
    /// the picker has rendered in the browser, and written into a bound value the way a pick in the list would be, so a
    /// form starts on it; a value the page has already set is left as it is, and so is a disabled or read-only field.
    /// </summary>
    [Parameter] public bool DetectTimeZone { get; set; }

    /// <summary>
    /// Determines the allowed drop directions of the time zone list callout.
    /// </summary>
    [Parameter] public BitDropDirection DropDirection { get; set; } = BitDropDirection.TopAndBottom;

    /// <summary>
    /// The error message shown under the time zone picker, which also puts the field in its invalid state - for a
    /// rejection that comes from a server or from a rule no data annotation can express. Inside an EditContext the
    /// validation of the model is what decides the state instead, and its message is rendered by a ValidationMessage of
    /// its own.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The custom template for the error message of the time zone picker, taking the place of <see cref="ErrorMessage"/>
    /// and putting the field in the same invalid state.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public RenderFragment? ErrorMessageTemplate { get; set; }

    /// <summary>
    /// Renders the time zone picker in an invalid state without going through the validation of an EditContext.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Invalid { get; set; }

    /// <summary>
    /// Determines the opening state of the time zone list callout. (two-way bound)
    /// </summary>
    [Parameter, TwoWayBound]
    [CallOnSet(nameof(OnSetIsOpen))]
    public bool IsOpen { get; set; }

    /// <summary>
    /// The custom template for each time zone of the list. The offset the list shows for it is
    /// <see cref="BitTimeZone.GetUtcOffset(DateTimeOffset)"/> at the <see cref="ReferenceDate"/> (now when it is not
    /// given), written the way the list writes it by <see cref="BitTimeZones.FormatUtcOffset(TimeSpan)"/>.
    /// </summary>
    [Parameter] public RenderFragment<BitTimeZone>? ItemTemplate { get; set; }

    /// <summary>
    /// The label of the time zone picker shown above the field, which is also the accessible name of the field and of
    /// the time zone list.
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// The custom template for the label of the time zone picker.
    /// </summary>
    [Parameter] public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// The maximum height of the time zone list callout in pixels.
    /// </summary>
    [Parameter] public int? MaxHeight { get; set; }

    /// <summary>
    /// Removes the border of the time zone picker.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool NoBorder { get; set; }

    /// <summary>
    /// Hides the names of the countries the time zones are in, which the list otherwise shows beside each place.
    /// </summary>
    [Parameter] public bool NoCountry { get; set; }

    /// <summary>
    /// Hides the offsets from UTC of the time zones in the field and in the list.
    /// </summary>
    [Parameter] public bool NoOffset { get; set; }

    /// <summary>
    /// The message to show when the search result of the time zone list is empty. It is also what a screen reader
    /// announces for an empty result.
    /// </summary>
    [Parameter] public string? NoResultsMessage { get; set; }

    /// <summary>
    /// The custom template to show when the search result of the time zone list is empty.
    /// </summary>
    [Parameter] public RenderFragment? NoResultsTemplate { get; set; }

    /// <summary>
    /// Hides the search box of the time zone list. The field then answers to type-ahead instead, open or closed: the
    /// letters typed on it move to the time zone whose name they start.
    /// </summary>
    [Parameter] public bool NoSearchBox { get; set; }

    /// <summary>
    /// Stops the keyboard navigation of the time zone list from wrapping around at its two ends.
    /// </summary>
    [Parameter] public bool NoWrapNavigation { get; set; }

    /// <summary>
    /// The callback that is invoked when the field loses focus.
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnBlur { get; set; }

    /// <summary>
    /// The callback that is invoked when the time zone list callout closes.
    /// </summary>
    [Parameter] public EventCallback OnClose { get; set; }

    /// <summary>
    /// The callback that is invoked when the field receives focus.
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnFocus { get; set; }

    /// <summary>
    /// The callback that is invoked when the time zone list callout opens.
    /// </summary>
    [Parameter] public EventCallback OnOpen { get; set; }

    /// <summary>
    /// The callback that is invoked when the search text of the time zone list changes.
    /// </summary>
    [Parameter] public EventCallback<string?> OnSearch { get; set; }

    /// <summary>
    /// The text shown in the field while no time zone is selected.
    /// </summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>
    /// The time zones to pin to the top of the list, set apart from the rest of it by a rule - the ones an app's users
    /// pick most. Each is written as any id <see cref="BitTimeZones.Find(string)"/> resolves: an IANA id, an older IANA
    /// id or a Windows id.
    /// </summary>
    [Parameter] public ICollection<string>? PreferredTimeZones { get; set; }

    /// <summary>
    /// The moment the offsets of the time zones are computed for - and the list ordered by - which is now unless it is
    /// given: the offset of a time zone that observes daylight saving time changes over the year, and a picker for the
    /// time zone of an event in the future shows the offsets that will hold then.
    /// </summary>
    [Parameter] public DateTimeOffset? ReferenceDate { get; set; }

    /// <summary>
    /// Shows the time zone list as a full height panel on small screens instead of an inline callout.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Responsive { get; set; }

    /// <summary>
    /// The aria-label of the close button of the responsive panel of the time zone list.
    /// </summary>
    [Parameter] public string? ResponsiveCloseButtonAriaLabel { get; set; }

    /// <summary>
    /// The icon of the close button of the responsive panel of the time zone list.
    /// Takes precedence over <see cref="ResponsiveCloseIconName"/> when both are set.
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// </summary>
    [Parameter] public BitIconInfo? ResponsiveCloseIcon { get; set; }

    /// <summary>
    /// The icon name of the close button of the responsive panel of the time zone list from the Fluent UI icon set.
    /// For external icon libraries, use <see cref="ResponsiveCloseIcon"/> instead.
    /// </summary>
    [Parameter] public string? ResponsiveCloseIconName { get; set; }

    /// <summary>
    /// The aria-label of the search box of the time zone list. Falls back to the search box placeholder, then to a
    /// default English value, when not provided.
    /// </summary>
    [Parameter] public string? SearchBoxAriaLabel { get; set; }

    /// <summary>
    /// The placeholder text of the search box of the time zone list.
    /// </summary>
    [Parameter] public string? SearchBoxPlaceholder { get; set; }

    /// <summary>
    /// What a screen reader announces for the number of time zones a search term leaves in the list, in place of the
    /// default "1 time zone found" / "{count} time zones found" - the way to translate that announcement. An empty result
    /// announces <see cref="NoResultsMessage"/> instead, and returning null or an empty string keeps a count from being
    /// announced at all.
    /// </summary>
    [Parameter] public Func<int, string?>? SearchResultsAnnouncement { get; set; }

    /// <summary>
    /// The size of the time zone picker.
    /// </summary>
    /// <remarks>
    /// An explicit value wins over the <c>--bit-TimeZonePicker-*</c> size variables (minimum height, font sizes,
    /// padding, row height); left unset, the field is medium unless those variables say otherwise.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitTimeZonePicker.
    /// </summary>
    [Parameter, ResetStyleBuilder] public BitTimeZonePickerClassStyles? Styles { get; set; }

    /// <summary>
    /// The list of the time zones the picker offers. Defaults to <see cref="BitTimeZones.All"/>; a list of translated
    /// <see cref="BitTimeZone"/> instances is how the names are localized, and a filtered one how the choice is narrowed
    /// down. Whatever its order, the picker shows them by their offsets from UTC.
    /// </summary>
    [Parameter] public ICollection<BitTimeZone> TimeZones { get; set; } = BitTimeZones.All;

    /// <summary>
    /// The tooltip (title attribute) of the time zone picker.
    /// </summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// Renders the time zone picker with only a bottom border instead of a full one.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Underlined { get; set; }

    /// <summary>
    /// The custom template for the content of the field, receiving the selected time zone (null while none is).
    /// </summary>
    [Parameter] public RenderFragment<BitTimeZone?>? ValueTemplate { get; set; }



    /// <summary>
    /// The time zone of the list the current value stands for, or null while there is none - or the value is one the
    /// list does not offer.
    /// </summary>
    public BitTimeZone? SelectedTimeZone => GetSelectedZone();

    /// <summary>
    /// Opens the time zone list callout.
    /// </summary>
    public Task OpenAsync() => InvokeAsync(async () =>
    {
        // Called from outside a Blazor event handler as well as from one, so the render the opened callout needs is
        // asked for here rather than left to the caller - and on the renderer's own context, since the caller can be on
        // any thread.
        if (Disabled || ReadOnly || IsOpen) return;

        await OpenCallout();

        StateHasChanged();
    });

    /// <summary>
    /// Closes the time zone list callout.
    /// </summary>
    public Task CloseAsync() => InvokeAsync(async () =>
    {
        if (IsOpen is false) return;

        await CloseCallout();

        StateHasChanged();
    });

    /// <summary>
    /// Selects the time zone an id stands for - an IANA id, an older IANA id or a Windows id - exactly as picking it in
    /// the list would, so the same events fire. A time zone the list does not offer is not selected.
    /// </summary>
    public Task SelectTimeZoneAsync(string timeZoneId) => InvokeAsync(async () =>
    {
        var offered = _list.Resolve(timeZoneId);

        if (offered is null) return;

        await SelectTimeZone(offered, focusField: false);

        // Unlike a click in the list, this call does not arrive through an event handler of the component, so nothing
        // re-renders it on its own.
        StateHasChanged();
    });

    /// <summary>
    /// Selects the time zone a <see cref="System.TimeZoneInfo"/> stands for exactly as picking it in the list would, so
    /// the same events fire. A time zone the list does not offer is not selected.
    /// </summary>
    public Task SelectTimeZoneAsync(TimeZoneInfo timeZone) => SelectTimeZoneAsync(timeZone.Id);



    [JSInvokable("CloseCallout")]
    public async Task _CloseCalloutBeforeAnotherCalloutIsOpened()
    {
        if (IsOpen is false) return;

        _isCalloutDisplayed = false;

        // The JS side has already hidden this callout to make room for another one, or because the focus moved on from
        // both the field and the list, so the state is brought in line without toggling it a second time.
        if (await AssignIsOpenInternal(false) is false) return;

        // The dismissal has taken the tab order around the field back along with the callout (see ToggleCallout).
        _tabOutRegistered = false;

        _list.Reset();

        await ClearSearchBoxElement();

        await SyncFieldKeys();

        // The callout this component had open is gone to make room for another one's, so whatever the focus was on
        // inside it is gone along with it.
        SetHasFocus(_fieldHasFocus);

        await OnClose.InvokeAsync();

        await InvokeAsync(StateHasChanged);
    }

    [JSInvokable("OnTabOut")]
    public async Task _OnTabOut()
    {
        // The list may have been closed by something else - an outside click, the IsOpen parameter - while this call
        // was on its way, and closing it again would reach the JS side to toggle a callout that is not shown.
        if (IsDisposed || IsOpen is false) return;

        await CloseCallout();

        // An IsOpen bound one way keeps the list open whatever the keyboard does, and the ring stays on with it.
        if (IsOpen) return;

        // The JS side has already moved the focus on past the field, so the ring CloseCallout dropped stays off unless
        // the focus has landed back in it.
        SetHasFocus(_fieldHasFocus);

        await InvokeAsync(StateHasChanged);
    }



    protected override string RootElementClass => "bit-tzp";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => NoBorder ? "bit-tzp-nbd" : string.Empty);

        ClassBuilder.Register(() => Underlined ? "bit-tzp-und" : string.Empty);

        ClassBuilder.Register(() => ReadOnly ? "bit-tzp-rdo" : string.Empty);

        ClassBuilder.Register(() => Disabled is false && Required ? "bit-tzp-req" : string.Empty);

        ClassBuilder.Register(() => _hasFocus ? "bit-tzp-fcs" : string.Empty);

        // Size, Color, Background and Border publish nothing while they are unset, which is what lets the stylesheet tell
        // a default from a choice: the public --bit-TimeZonePicker-* variables restyle the default and never an explicit
        // value.
        ClassBuilder.Register(() => BitCssClasses.Size(Size, "bit-tzp"));

        ClassBuilder.Register(() => ColorClass ?? string.Empty);

        ClassBuilder.Register(() => Background switch
        {
            BitColorKind.Primary => "bit-tzp-bpr",
            BitColorKind.Secondary => "bit-tzp-bse",
            BitColorKind.Tertiary => "bit-tzp-btr",
            BitColorKind.Transparent => "bit-tzp-btn",
            _ => string.Empty
        });

        ClassBuilder.Register(() => Border switch
        {
            BitColorKind.Primary => "bit-tzp-brp",
            BitColorKind.Secondary => "bit-tzp-brs",
            BitColorKind.Tertiary => "bit-tzp-brt",
            BitColorKind.Transparent => "bit-tzp-brn",
            _ => string.Empty
        });
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitTimeZonePickerParams))]
    protected override void OnInitialized()
    {
        // A value is read and written as one of the types the time zone of the list converts to, and a picker of any
        // other type could never hold one, so it is refused before anything is rendered rather than left blank.
        if (typeof(TValue) != typeof(string) && typeof(TValue) != typeof(TimeZoneInfo) && typeof(TValue) != typeof(BitTimeZone))
        {
            throw new InvalidOperationException($"{nameof(BitTimeZonePicker<TValue>)} only supports {nameof(String)}, {nameof(System.TimeZoneInfo)} and {nameof(BitTimeZone)} as its TValue, not {typeof(TValue).Name}.");
        }

        CascadingParameters?.UpdateParameters(this);

        _labelId = $"BitTimeZonePicker-{UniqueId}-label";
        _errorId = $"BitTimeZonePicker-{UniqueId}-error";
        _fieldId = $"BitTimeZonePicker-{UniqueId}-field";
        _descriptionId = $"BitTimeZonePicker-{UniqueId}-description";
        _searchId = $"BitTimeZonePicker-{UniqueId}-search";
        _listId = $"BitTimeZonePicker-{UniqueId}-list";
        _calloutId = $"BitTimeZonePicker-{UniqueId}-callout";
        _overlayId = $"BitTimeZonePicker-{UniqueId}-overlay";
        _scrollContainerId = $"BitTimeZonePicker-{UniqueId}-scroll-container";

        _list.Update(TimeZones, PreferredTimeZones, ReferenceDate);

        SetDefaultValue();

        base.OnInitialized();
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitTimeZonePickerParams))]
    protected override void OnParametersSet()
    {
        // The cascade is read first, so that the time zone list and the classes below are computed from the parameters
        // the field ends up with rather than from the ones it was written with.
        CascadingParameters?.UpdateParameters(this);

        // The list is read on every render while the callout is open, so it is only built again when its source
        // actually changes, or when the hour its offsets were computed in has passed.
        _list.Update(TimeZones, PreferredTimeZones, ReferenceDate);

        // A message that says the value was rejected has to be heard as well as seen, and it is written into a part of
        // the page a screen reader has already read past by the time it appears.
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
        // A list is only ever shown on a field that can be used. One the page disables or makes read-only while it is
        // open is closed with it, and one an IsOpen bound one way keeps open is hidden while the field cannot be used
        // and shown again once it can (see IsCalloutShown), which takes a toggle of its own either way.
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

            await DetectBrowserTimeZone();
        }

        // Wire up deterministic key suppression once the elements exist. Done on every render rather than only on the
        // first one, because the search box comes and goes with NoSearchBox, and a search box that appeared later would
        // otherwise let its navigation keys move the caret as well as the selection.
        await WirePreventKeys();

        // The callout is shown and positioned only once the render that filled its list has reached the DOM, so the time
        // zone list can stay out of the document until the first opening while the callout is still measured against
        // its real content.
        if (_pendingCalloutToggle)
        {
            _pendingCalloutToggle = false;

            // A callout opened through the IsOpen parameter before the first render had no list to point at yet, so the
            // active option is settled here instead.
            var activeIndexSettled = false;
            if (IsCalloutShown && _list.ActiveIndex < 0)
            {
                ResetActiveIndexToSelection();

                activeIndexSettled = true;
            }

            await ToggleCallout();

            // The open list is driven from whichever element carries aria-activedescendant, so the focus is put on it:
            // the search box when there is one, and the field itself when there is not.
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

            // The active option was decided after the render that got here, so what names it is asked for again instead
            // of waiting for the next render to come along.
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

    // The text a form posts or binds is the canonical IANA id, the one way of writing a time zone down that every
    // runtime reads back.
    protected override string? FormatValueAsString(TValue? value) => ToZone(value)?.Id ?? GetRawId(value);

    // A time zone written down any of the ways BitTimeZones resolves - an IANA id, an older one, a Windows id - is read
    // back as the one the picker offers, so a value parsed from a text is never a time zone the list does not hold.
    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out TValue result, [NotNullWhen(false)] out string? parsingErrorMessage)
    {
        if (_list.Resolve(value) is { } offered && FromZone(offered) is { } parsed)
        {
            result = parsed;
            parsingErrorMessage = null;
            return true;
        }

        result = default;
        parsingErrorMessage = $"The {DisplayName ?? FieldIdentifier.FieldName} field is not a valid time zone.";
        return false;
    }



    // The size of the control is driven by CSS variables, and the callout is rendered outside the root element, so the
    // class carrying them is repeated on it: without that the rows of the list would ignore an explicit Size. Like the
    // root's, it is empty while the Size is unset, so the public --bit-TimeZonePicker-* variables restyle the default
    // rows.
    private string SizeClass => BitCssClasses.Size(Size, "bit-tzp");

    // The callout is rendered outside the root as well, and the keyboard cue of its active row takes the focus color of
    // the role, so the class is repeated on it the same way the size class is.
    private string? ColorClass => Color switch
    {
        BitColor.Primary => "bit-tzp-pri",
        BitColor.Secondary => "bit-tzp-sec",
        BitColor.Tertiary => "bit-tzp-ter",
        BitColor.Info => "bit-tzp-inf",
        BitColor.Success => "bit-tzp-suc",
        BitColor.Warning => "bit-tzp-wrn",
        BitColor.SevereWarning => "bit-tzp-swr",
        BitColor.Error => "bit-tzp-err",
        BitColor.PrimaryBackground => "bit-tzp-pbg",
        BitColor.SecondaryBackground => "bit-tzp-sbg",
        BitColor.TertiaryBackground => "bit-tzp-tbg",
        BitColor.PrimaryForeground => "bit-tzp-pfg",
        BitColor.SecondaryForeground => "bit-tzp-sfg",
        BitColor.TertiaryForeground => "bit-tzp-tfg",
        BitColor.PrimaryBorder => "bit-tzp-pbr",
        BitColor.SecondaryBorder => "bit-tzp-sbr",
        BitColor.TertiaryBorder => "bit-tzp-tbr",
        _ => null
    };

    // Only a field that can be used opens its list: the field, the keyboard and OpenAsync all refuse to open the list of
    // a disabled or read-only one.
    private bool CanOpen => Disabled is false && ReadOnly is false;

    // Whether the list is actually on screen. An IsOpen the page holds at true on a field that cannot be used is refused
    // the way the field refuses to open (see OnParametersSetAsync), and one bound one way leaves the list hidden until
    // the field can be used again.
    private bool IsCalloutShown => IsOpen && CanOpen;

    private bool IsSelected(BitTimeZone zone) => ReferenceEquals(zone, GetSelectedZone());

    private bool HasLabel => Label.HasValue() || LabelTemplate is not null;

    // The field is named by the visible label; aria-labelledby takes precedence over aria-label, so it is only pointed at
    // while no name of its own was given. A field with neither is still named, since a combobox has no name to take
    // from its content.
    private string? FieldLabelledBy => HasLabel && AriaLabel.HasNoValue() ? _labelId : null;

    private string? FieldAriaLabel => FieldLabelledBy is null ? (AriaLabel ?? "Time zone") : null;

    // The list is named the same way the field it belongs to is.
    private string? ListLabelledBy => FieldLabelledBy;

    private string? ListAriaLabel => FieldLabelledBy is null ? (AriaLabel ?? "Time zones") : null;

    // The tooltip is written on the root, so it also covers the label, and on the field itself, so it reaches a keyboard
    // as well as a pointer. A title the consumer put on the field through InputHtmlAttributes is kept. Without either,
    // the field is titled with the whole of what it shows, which a narrow field cuts short.
    private string? FieldTitle => Title ?? GetSplattedInputAttribute("title") ?? (GetSelectedZone() is { } zone ? GetZoneText(zone) : GetRawText(CurrentValue));

    private bool HasDescription => Description.HasValue() || DescriptionTemplate is not null;

    private bool HasErrorMessage => ErrorMessage.HasValue() || ErrorMessageTemplate is not null;

    // The invalid state the consumer forces, either as a flag or by handing the field a message to show.
    protected override bool HasError => Invalid || HasErrorMessage;

    // What is written under the field is what describes it, so both lines are pointed at rather than one of them. A
    // value the consumer put on the field itself is kept.
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
    // writes the latter into the splatted attributes, so the value of the consumer is read back here instead of being
    // overwritten by the explicit attribute of the field.
    private string? AriaInvalid => IsInvalid ? "true" : GetSplattedInputAttribute("aria-invalid");

    private string GetOptionId(int index) => $"{_calloutId}-opt-{index}";

    // The display is written here rather than in the stylesheet because it is what the component toggles the layer with,
    // and Styles.Overlay is appended after it so a value written for the overlay still wins.
    private string GetOverlayStyles() => $"display:{(IsCalloutShown ? "block" : "none")};{Styles?.Overlay}";

    // The offset the list shows for a time zone, written the way the list writes every one of them.
    private string GetOffsetText(BitTimeZone zone) => BitTimeZones.FormatUtcOffset(_list.GetOffset(zone));

    // Everything a row shows, as one line: the title of the field, and the choice announced on it.
    private string GetZoneText(BitTimeZone zone)
    {
        var place = NoCountry || zone.CountryName.HasNoValue() ? zone.Name : $"{zone.Name}, {zone.CountryName}";

        return NoOffset ? place : $"({GetOffsetText(zone)}) {place}";
    }

    // The time zone of the list a value stands for, worked out again only when the value or the list has changed.
    private BitTimeZone? GetSelectedZone()
    {
        var value = (object?)CurrentValue;

        if (_selectedResolved is false || Equals(_selectedOf, value) is false || ReferenceEquals(_selectedIn, _list.All) is false)
        {
            _selected = ToZone(CurrentValue);
            _selectedOf = value;
            _selectedIn = _list.All;
            _selectedResolved = true;
        }

        return _selected;
    }

    private BitTimeZone? ToZone(TValue? value) => _list.Resolve(GetRawId(value));

    private static string? GetRawId(TValue? value) => value switch
    {
        BitTimeZone zone => zone.Id,
        TimeZoneInfo timeZone => timeZone.Id,
        string id => id.HasValue() ? id : null,
        _ => null
    };

    // What the field shows for a value the list does not offer - a custom TimeZoneInfo, or an id no table knows - which
    // is still the value, so it is shown rather than taken for an empty field.
    private static string? GetRawText(TValue? value) => value switch
    {
        BitTimeZone zone => zone.Name,
        TimeZoneInfo timeZone => timeZone.DisplayName,
        string id => id.HasValue() ? id : null,
        _ => null
    };

    private static TValue? FromZone(BitTimeZone zone)
    {
        object? value = typeof(TValue) == typeof(string) ? zone.Id
                      : typeof(TValue) == typeof(TimeZoneInfo) ? zone.TimeZoneInfo
                      : zone;

        return (TValue?)value;
    }

    // A single live region carries everything the picker has to say, and a screen reader only announces what changes
    // inside one: repeating the same sentence would otherwise be silent, so an invisible marker alternates with it.
    private void Announce(string? text)
    {
        _announcementMarker = !_announcementMarker;
        _announcement = text.HasValue() && _announcementMarker ? text + '​' : text;
    }

    // The visitor's own time zone is only known to the browser, which is why it is read after the first render rather
    // than from TimeZoneInfo.Local: on Blazor Server that is the time zone of the server.
    private async Task DetectBrowserTimeZone()
    {
        if (DetectTimeZone is false || CanOpen is false || GetRawId(CurrentValue) is not null) return;

        string? browserTimeZone;

        try
        {
            browserTimeZone = await _js.BitExtrasGetTimeZone();
        }
        catch (JSException) { return; } // a browser that cannot tell leaves the field empty
        catch (JSDisconnectedException) { return; } // we can ignore this exception here

        // The browser answers with an IANA id, possibly one IANA has since renamed (Asia/Calcutta), which the list
        // resolves to the zone it offers; one it does not offer leaves the field empty.
        if (_list.Resolve(browserTimeZone) is not { } zone || FromZone(zone) is not { } value) return;

        // The page may have set a value while the browser was being asked.
        if (IsDisposed || GetRawId(CurrentValue) is not null) return;

        await SetCurrentValueAsync(value);

        StateHasChanged();
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
            // While closed, Enter/Space are handled by the field's native click, so they are intentionally not treated as
            // open triggers here to avoid a double toggle.
            if (key is "ArrowDown" or "ArrowUp" or "Home" or "End")
            {
                await OpenCallout();

                // Home and End say where in the list to land, the way they do on a native select.
                if (key is "Home" or "End")
                {
                    _list.Navigate(key, fromSearchBox: false, NoWrapNavigation);
                }
            }
            else if (NoSearchBox ? BitCountryList.IsTypeAheadKey(e) : BitTimeZoneList.IsSearchKey(e))
            {
                // Typing on the closed field opens it on what was typed, the way it does on a native select: on the time
                // zone the letters start when there is no search box, and as the first character of the search term -
                // punctuation included ("+03:30") - when there is one.
                await OpenCallout();

                if (NoSearchBox)
                {
                    _list.TypeAhead(key);
                }
                else
                {
                    await ApplySearchText(key);
                }
            }

            return;
        }

        // A list with a search box is driven from that box, which is the combobox naming the active option. The field
        // only holds the focus of the open list when a Shift+Tab has taken it back there, and the keys that work the
        // list hand the focus back to the search box along with what they do.
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
                    await HandleOnTimeZoneSelect(active);
                }
                else
                {
                    await CloseCallout();
                    await FocusField();
                }
                break;

            case "Tab":
                // While the list is in the tab order after the field (see ToggleCallout), the Tab key is the JS side's to
                // answer. A list without a search box keeps the focus on the field, so its Tab moves on from there, and
                // the list must not stay open behind it.
                if (_tabOutRegistered) break;

                await CloseCallout();
                break;

            case "Enter":
                if (_list.Active is { } zone)
                {
                    await HandleOnTimeZoneSelect(zone);
                }
                break;

            case " ":
            case "Spacebar":
                // When the search box is visible the space key must remain available for typing, so it is only treated as
                // a selection key when focus is on the field (i.e. there is no search box).
                if (fromSearchBox) break;

                // A space typed in the middle of a type-ahead term is part of the name being typed ("new y" for New
                // York), not a choice of the time zone the term has reached so far.
                if (_list.IsTypeAheadInProgress)
                {
                    _list.TypeAhead(" ");
                }
                else if (_list.Active is { } spaced)
                {
                    await HandleOnTimeZoneSelect(spaced);
                }
                break;

            default:
                // The arrows, PageUp/PageDown, and Home/End outside the search box, whose caret they belong to.
                if (_list.Navigate(key, fromSearchBox, NoWrapNavigation)) break;

                // Without a search box to type into, the letters typed on the open list jump to the time zone whose name
                // they start, the way a native select behaves.
                if (fromSearchBox is false && BitCountryList.IsTypeAheadKey(e))
                {
                    _list.TypeAhead(key);
                }
                break;
        }
    }

    // The keys of the open list pressed on the field of a list that has a search box (see HandleOnCalloutKeyDown): the
    // navigation keys move the active option and hand the focus to the search box, which announces it; a letter goes
    // into the search box as if it had been typed there; Enter and Space press the field, which closes the list as a
    // click on it does; Alt+Up closes it. Reports whether the key was handled here.
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
            if (BitTimeZoneList.IsSearchKey(e) is false) return false;

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

        // The offsets are those of the moment the list is opened in, so one left open across a clock change is not the
        // only time they are brought up to date.
        _list.Update(TimeZones, PreferredTimeZones, ReferenceDate);

        // Settle the active option before the list has rendered: opening via the keyboard (ArrowDown) would otherwise
        // leave it at -1, making the first Enter select nothing.
        ResetActiveIndexToSelection();

        _list.ResetTypeAhead();

        await SyncFieldKeys();

        _pendingCalloutToggle = true;

        await OnOpen.InvokeAsync();
    }

    private async Task CloseCallout()
    {
        if (IsOpen is false) return;

        if (await AssignIsOpenInternal(false) is false) return;

        _list.Reset();

        await ClearSearchBoxElement();

        await SyncFieldKeys();

        _pendingCalloutToggle = true;

        // The focus may well have been inside the callout that is now gone, so the ring is dropped here. The flows that
        // put the focus back into the field turn it on again themselves.
        SetHasFocus(false);

        await OnClose.InvokeAsync();
    }

    // Dismissing the callout by hand - a click outside it, or the close button of the responsive panel - hides whatever
    // inside it had the focus, which is given back to the field the list was opened from.
    private async Task CloseCalloutAndRestoreFocus()
    {
        if (IsOpen is false) return;

        await CloseCallout();

        await FocusField();
    }

    // Keeps the keydown listeners of the two elements that navigate the list in step with the elements themselves, and
    // remembers which ones were wired so disposal can find them again whatever the parameters say by then.
    private async Task WirePreventKeys()
    {
        if (_fieldKeysWired is false)
        {
            _fieldKeysWired = true;
            _fieldKeysOpen = IsCalloutShown;
            await _js.BitExtrasSetPreventKeys(InputElement, _fieldKeysOpen ? BitTimeZoneList.TriggerOpenKeys : BitTimeZoneList.TriggerClosedKeys);
        }
        else
        {
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

    // The keys the field prevents follow whether its list is on screen, and are swapped the moment that changes: Enter
    // and Space are left to the native click of a closed field, which opens it, and are kept from an open one, where
    // they pick a time zone and the click would open the list again straight after.
    private async Task SyncFieldKeys()
    {
        if (_fieldKeysWired is false || _fieldKeysOpen == IsCalloutShown) return;

        _fieldKeysOpen = IsCalloutShown;

        await _js.BitExtrasSetPreventKeys(InputElement, _fieldKeysOpen ? BitTimeZoneList.TriggerOpenKeys : BitTimeZoneList.TriggerClosedKeys);
    }

    private void ResetActiveIndexToSelection() => _list.ResetActiveToSelection(IsSelected);

    // Clears the text left in the search box element itself: an input keeps the value its user typed as a DOM property,
    // which a re-render of the same element does not necessarily put back.
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

    // See OnSetIsOpen: the flows that follow AssignIsOpen with their own callout toggle mark the change as internal, so
    // the hook does not arrange a second one.
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
        // The internal open and close flows arrange the toggle themselves. The hook only reacts to a change pushed from
        // the outside through the IsOpen parameter, which otherwise has no path to the JS side that actually shows and
        // hides the callout.
        if (_internalIsOpenChange) return;

        if (IsOpen)
        {
            _hasOpened = true;

            ResetActiveIndexToSelection();
        }
        else
        {
            _list.Reset();

            // A callout closed through the IsOpen parameter takes whatever focus it holds down with it, and no focusout
            // of the field is fired for it.
            SetHasFocus(false);
        }

        _pendingCalloutToggle = true;
    }

    // The open list is relocated to the end of the body, so while it is open the search box is put into the tab order
    // right after the field (see Utils.setupTabOut): Tab moves on through the rest of the list to whatever follows the
    // field and closes the list through OnTabOut, and Shift+Tab from the search box goes back to the field. A list
    // without a search box has nothing to tab into: the focus stays on the field.
    private async Task ToggleCallout()
    {
        if (IsDisposed) return;

        var shown = IsCalloutShown;
        var tabOut = shown && NoSearchBox is false;

        _tabOutRegistered = false;

        // The callout is positioned against the field, but the root is the element whose Style, classes and theme the
        // callout and the overlay have to go on inheriting once they are relocated to the body, so it is named apart as
        // the rootId.
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

        _tabOutRegistered = tabOut;

        _isCalloutDisplayed = shown;
    }

    private Task HandleOnSearchInput(ChangeEventArgs e) => ApplySearchText(e.Value?.ToString());

    private async Task ApplySearchText(string? text)
    {
        // The active option moves to the first match, so pressing Enter selects it instead of a stale one.
        var count = _list.Search(text);

        // The list the term filtered down to is inside a callout, so its length is only ever seen: it is announced here
        // for the users who cannot see it.
        Announce(BitTimeZoneList.GetSearchAnnouncement(count, NoResultsMessage, SearchResultsAnnouncement));

        await OnSearch.InvokeAsync(_list.SearchText);
    }

    private Task HandleOnTimeZoneSelect(BitTimeZone zone) => SelectTimeZone(zone, focusField: true);

    private async Task SelectTimeZone(BitTimeZone zone, bool focusField)
    {
        if (Disabled || ReadOnly) return;

        // The focus stays on the field of a list without a search box, so the field naming the new time zone is not
        // announced again, and the choice is said out loud instead.
        var focusStays = NoSearchBox && IsOpen;

        await CloseCallout();

        if (IsSelected(zone) is false && FromZone(zone) is { } value)
        {
            var previous = CurrentValue;

            await SetCurrentValueAsync(value);

            // A Value bound one way (with no ValueChanged and no OnChange) cannot be changed by the picker, so the field
            // keeps showing the time zone it was given and nothing is announced for a choice that never landed.
            if (ReferenceEquals(previous, CurrentValue) is false && focusStays)
            {
                Announce(GetZoneText(zone));
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
            // focus itself instead of a render later.
            SetHasFocus(true);
        }
        catch (JSException) { } // the element might not be ready/visible yet
    }

    // The focus ring is on while the focus is on the field, and stays on while the callout has taken it away from there.
    private void SetHasFocus(bool value)
    {
        if (_hasFocus == value) return;

        _hasFocus = value;

        ClassBuilder.Reset();
    }

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

        _fieldHasFocus = false;

        // The search box of an open callout is part of the field as far as the user is concerned, so the ring stays on
        // while it is open. Closing the callout is what turns it off again (see CloseCallout).
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
            // First, so that nothing failing after it can leave the listeners of an open list behind.
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
