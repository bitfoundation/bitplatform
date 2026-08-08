using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// A search box (SearchBox) provides an input field for searching content within a site or app to find specific items.
/// </summary>
public partial class BitSearchBox : BitTextInputBase<string?>
{
    // The number of suggest items the page up & page down keys jump over at once.
    private const int SuggestPageSize = 5;

    private bool _isOpen;
    private bool _isLoading;
    private string? _inputMode;
    private bool _inputHasFocus;
    private bool _inputHasValue;
    private bool _suppressSearch;
    private bool _searchTriggered;
    private bool _hasSuggestSource;
    private string? _announcement;
    private bool _announcementMarker;
    private int _selectedIndex = -1;
    private string? _enterKeyHint = "search";
    private string _inputId = string.Empty;
    private string _labelId = string.Empty;
    private string _calloutId = string.Empty;
    private string _overlayId = string.Empty;
    private List<string> _viewSuggestedItems = [];
    private string _scrollContainerId = string.Empty;
    private CancellationTokenSource? _cancellationTokenSource;
    private DotNetObjectReference<BitSearchBox> _dotnetObj = default!;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Builds the text that the screen reader announces through the live region of the search box
    /// whenever the suggest items change, in place of the built-in English announcements.
    /// Returning null or an empty string announces nothing.
    /// </summary>
    [Parameter] public Func<BitSearchBoxAnnouncementArgs, string?>? AnnouncementProvider { get; set; }

    /// <summary>
    /// Automatically highlights the first suggest item as soon as the suggest list opens,
    /// so pressing enter selects it without pressing the arrow keys first.
    /// </summary>
    [Parameter] public bool AutoSelectSuggestItem { get; set; }

    /// <summary>
    /// The background color kind of the search box.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColorKind? Background { get; set; }

    /// <summary>
    /// The custom template rendered at the bottom of the suggest items callout.
    /// </summary>
    [Parameter] public RenderFragment? CalloutFooterTemplate { get; set; }

    /// <summary>
    /// The custom template rendered at the top of the suggest items callout.
    /// </summary>
    [Parameter] public RenderFragment? CalloutHeaderTemplate { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the search box.
    /// </summary>
    [Parameter] public BitSearchBoxClassStyles? Classes { get; set; }

    /// <summary>
    /// The accessible label (aria-label) of the clear button.
    /// </summary>
    [Parameter] public string ClearButtonAriaLabel { get; set; } = "Clear";

    /// <summary>
    /// Gets or sets the icon to display on the clear button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="ClearButtonIconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="ClearButtonIconName"/> instead.
    /// </remarks>
    /// <example>
    /// Bootstrap: ClearButtonIcon="BitIconInfo.Bi("x-circle-fill")"
    /// FontAwesome: ClearButtonIcon="BitIconInfo.Fa("solid xmark")"
    /// Custom CSS: ClearButtonIcon="BitIconInfo.Css("my-icon-class")"
    /// </example>
    [Parameter] public BitIconInfo? ClearButtonIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display on the clear button from the built-in Fluent UI icons.
    /// </summary>
    /// <remarks>
    /// For external icon libraries, use <see cref="ClearButtonIcon"/> instead.
    /// </remarks>
    [Parameter] public string? ClearButtonIconName { get; set; }

    /// <summary>
    /// The custom template for clear button icon.
    /// </summary>
    [Parameter] public RenderFragment? ClearButtonTemplate { get; set; }

    /// <summary>
    /// The general color of the search box, used for colored parts like icons.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// Whether or not to animate the search box icon on focus.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool DisableAnimation { get; set; }

    /// <summary>
    /// Sets the enterkeyhint html attribute of the input element, which tells virtual keyboards
    /// which action label to render on their enter key. It defaults to
    /// <see cref="BitEnterKeyHint.Search"/> because pressing enter always runs a search here.
    /// </summary>
    [Parameter] public BitEnterKeyHint? EnterKeyHint { get; set; }

    /// <summary>
    /// Forces the suggest callout width to be always fixed at the component's width.
    /// </summary>
    [Parameter] public bool FixedCalloutWidth { get; set; }

    /// <summary>
    /// Whether or not to make the icon be always visible (it hides by default when the search box is focused).
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FixedIcon { get; set; }

    /// <summary>
    /// Expands the search box to fill the available width of its container.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FullWidth { get; set; }

    /// <summary>
    /// Highlights the part of each suggest item that matches the current search term.
    /// </summary>
    [Parameter] public bool HighlightSuggestItems { get; set; }

    /// <summary>
    /// Whether or not the icon is visible.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool HideIcon { get; set; }

    /// <summary>
    /// Whether to hide the clear button when the search box has value.
    /// </summary>
    [Parameter] public bool HideClearButton { get; set; }

    /// <summary>
    /// Gets or sets the icon to display using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="IconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="IconName"/> instead.
    /// </remarks>
    /// <example>
    /// Bootstrap: Icon="BitIconInfo.Bi("search")"
    /// FontAwesome: Icon="BitIconInfo.Fa("solid house")"
    /// Custom CSS: Icon="BitIconInfo.Css("my-icon-class")"
    /// </example>
    [Parameter] public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display from the built-in Fluent UI icons.
    /// </summary>
    /// <remarks>
    /// The icon name should be from the Fluent UI icon set (e.g., <c>BitIconName.Search</c>).
    /// <br />
    /// Browse available names in <c>BitIconName</c> of the <c>Bit.BlazorUI.Icons</c> nuget package or the gallery:
    /// <see href="https://blazorui.bitplatform.dev/iconography"/>.
    /// <br />
    /// For external icon libraries, use <see cref="Icon"/> instead.
    /// </remarks>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// Sets the inputmode html attribute of the input element.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(SetInputMode))]
    public BitInputMode? InputMode { get; set; }

    /// <summary>
    /// The text of the label of the search box.
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// The custom template for the label of the search box.
    /// </summary>
    [Parameter] public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// The custom template rendered in place of the default spinner while the
    /// <see cref="SuggestItemsProvider"/> is resolving the suggest items.
    /// </summary>
    [Parameter] public RenderFragment? LoadingTemplate { get; set; }

    /// <summary>
    /// The text rendered next to the loading indicator while the
    /// <see cref="SuggestItemsProvider"/> is resolving the suggest items.
    /// </summary>
    [Parameter] public string? LoadingText { get; set; }

    /// <summary>
    /// Sets the maxlength html attribute of the input element. A negative value means no limit.
    /// </summary>
    [Parameter] public int MaxLength { get; set; } = -1;

    /// <summary>
    /// The maximum number of items or suggestions that will be displayed. A value of zero or less means no limit.
    /// </summary>
    [Parameter] public int MaxSuggestCount { get; set; } = 5;

    /// <summary>
    /// The minimum character requirement for doing a search in suggest items.
    /// Setting it to zero also enables searching with an empty search term
    /// which is useful for showing default or recent items.
    /// </summary>
    [Parameter] public int MinSuggestTriggerChars { get; set; } = 3;

    /// <summary>
    /// Removes the overlay of suggest items callout.
    /// </summary>
    [Parameter] public bool Modeless { get; set; }

    /// <summary>
    /// Prevents clearing the value of the search box when the user presses the escape key.
    /// </summary>
    [Parameter] public bool NoClearOnEscape { get; set; }

    /// <summary>
    /// Removes the default border of the search box.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool NoBorder { get; set; }

    /// <summary>
    /// The custom template rendered in the callout when the search finds no suggest item.
    /// </summary>
    [Parameter] public RenderFragment? NoResultsTemplate { get; set; }

    /// <summary>
    /// The text rendered in the callout when the search finds no suggest item.
    /// </summary>
    [Parameter] public string? NoResultsText { get; set; }

    /// <summary>
    /// Callback executed when the user clears the search box by either clicking 'X' or hitting escape.
    /// </summary>
    [Parameter] public EventCallback OnClear { get; set; }

    /// <summary>
    /// Callback executed when the user clicks on the input of the search box.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Callback executed when the user presses escape in the search box.
    /// </summary>
    [Parameter] public EventCallback OnEscape { get; set; }

    /// <summary>
    /// Callback executed when the input of the search box gets focused.
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnFocus { get; set; }

    /// <summary>
    /// Callback executed when the input of the search box gets focused in.
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnFocusIn { get; set; }

    /// <summary>
    /// Callback executed when the input of the search box loses focus.
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnFocusOut { get; set; }

    /// <summary>
    /// Callback executed on each key down of the input of the search box.
    /// </summary>
    [Parameter] public EventCallback<KeyboardEventArgs> OnKeyDown { get; set; }

    /// <summary>
    /// Callback executed on each key up of the input of the search box.
    /// </summary>
    [Parameter] public EventCallback<KeyboardEventArgs> OnKeyUp { get; set; }

    /// <summary>
    /// Callback executed when the user presses enter in the search box, clicks the search button,
    /// or picks one of the suggest items.
    /// </summary>
    [Parameter] public EventCallback<string?> OnSearch { get; set; }

    /// <summary>
    /// Callback executed when the user selects one of the suggest items either by clicking on it
    /// or by pressing enter while it is highlighted.
    /// </summary>
    [Parameter] public EventCallback<string> OnSuggestItemSelect { get; set; }

    /// <summary>
    /// Callback executed with true when the suggest items callout opens and with false when it closes.
    /// </summary>
    [Parameter] public EventCallback<bool> OnSuggestItemsToggle { get; set; }

    /// <summary>
    /// Placeholder for the search box.
    /// </summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>
    /// Prefix text displayed before the search box input. This is not included in the value.
    /// </summary>
    [Parameter] public string? Prefix { get; set; }

    /// <summary>
    /// The custom template for the prefix of the search box.
    /// </summary>
    [Parameter] public RenderFragment? PrefixTemplate { get; set; }

    /// <summary>
    /// The accessible label (aria-label) of the search button.
    /// </summary>
    [Parameter] public string SearchButtonAriaLabel { get; set; } = "Search";

    /// <summary>
    /// Gets or sets the icon to display on the search button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="SearchButtonIconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="SearchButtonIconName"/> instead.
    /// </remarks>
    /// <example>
    /// Bootstrap: SearchButtonIcon="BitIconInfo.Bi("arrow-left")"
    /// FontAwesome: SearchButtonIcon="BitIconInfo.Fa("solid arrow-left")"
    /// Custom CSS: SearchButtonIcon="BitIconInfo.Css("my-icon-class")"
    /// </example>
    [Parameter] public BitIconInfo? SearchButtonIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display on the search button from the built-in Fluent UI icons.
    /// </summary>
    /// <remarks>
    /// For external icon libraries, use <see cref="SearchButtonIcon"/> instead.
    /// </remarks>
    [Parameter] public string? SearchButtonIconName { get; set; }

    /// <summary>
    /// The custom template for search button icon.
    /// </summary>
    [Parameter] public RenderFragment? SearchButtonTemplate { get; set; }

    /// <summary>
    /// Whether to show the search button.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool ShowSearchButton { get; set; }

    /// <summary>
    /// Opens the suggest items callout as soon as the input gets focused, without waiting for the user to type.
    /// Combine it with a zero <see cref="MinSuggestTriggerChars"/> to implement default or recent search items.
    /// </summary>
    [Parameter] public bool ShowSuggestItemsOnFocus { get; set; }

    /// <summary>
    /// The size of the search box.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// Sets the spellcheck html attribute of the input element. Leaving it null keeps the
    /// default behavior of the browser, setting it to false removes the red squiggles
    /// from search terms that are not real words.
    /// </summary>
    [Parameter] public bool? SpellCheck { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the search box.
    /// </summary>
    [Parameter] public BitSearchBoxClassStyles? Styles { get; set; }

    /// <summary>
    /// Suffix text displayed after the search box input. This is not included in the value.
    /// </summary>
    [Parameter] public string? Suffix { get; set; }

    /// <summary>
    /// The custom template for the suffix of the search box.
    /// </summary>
    [Parameter] public RenderFragment? SuffixTemplate { get; set; }

    /// <summary>
    /// Custom search function to be used in place of the default search algorithm.
    /// The first argument is the current search term and the second one is the suggest item to examine.
    /// </summary>
    [Parameter] public Func<string?, string?, bool>? SuggestFilterFunction { get; set; }

    /// <summary>
    /// The list of suggest items to display in the callout.
    /// </summary>
    [Parameter] public IEnumerable<string>? SuggestItems { get; set; }

    /// <summary>
    /// The accessible label (aria-label) of the suggest items list.
    /// </summary>
    [Parameter] public string SuggestItemsAriaLabel { get; set; } = "Suggestions";

    /// <summary>
    /// The item provider function providing suggest items.
    /// </summary>
    [Parameter] public BitSearchBoxSuggestItemsProvider? SuggestItemsProvider { get; set; }

    /// <summary>
    /// The custom template for rendering the suggest items of the search box.
    /// </summary>
    [Parameter] public RenderFragment<string>? SuggestItemTemplate { get; set; }

    /// <summary>
    /// Trims the leading and trailing white-spaces of the value of the search box.
    /// </summary>
    [Parameter] public bool Trim { get; set; }

    /// <summary>
    /// Whether or not the search box is underlined.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Underlined { get; set; }



    /// <summary>
    /// Whether the callout of the suggest items is currently open.
    /// </summary>
    public bool IsSuggestItemsOpen => _isOpen;

    /// <summary>
    /// Clears the value of the search box and invokes the <see cref="OnClear"/> callback.
    /// </summary>
    public async Task Clear()
    {
        if (IsEnabled is false) return;

        await ClearValue();
    }

    /// <summary>
    /// Runs the suggest items search of the current value and opens the callout of the suggest items.
    /// </summary>
    public async Task ShowSuggestItems()
    {
        if (IsEnabled is false) return;

        await SearchItems(openCallout: true, force: true);
    }

    /// <summary>
    /// Closes the callout of the suggest items.
    /// </summary>
    public async Task HideSuggestItems()
    {
        if (IsEnabled is false) return;

        await CloseCallout();
    }



    [JSInvokable("CloseCallout")]
    public async Task _CloseCalloutBeforeAnotherCalloutIsOpened()
    {
        if (IsEnabled is false) return;
        if (_isOpen is false) return;

        _isOpen = false;
        _selectedIndex = -1;

        await InvokeAsync(StateHasChanged);

        await OnSuggestItemsToggle.InvokeAsync(false);
    }



    protected override string RootElementClass => "bit-srb";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => FixedIcon ? "bit-srb-fic" : string.Empty);

        ClassBuilder.Register(() => HasValue ? $"bit-srb-hvl" : string.Empty);

        ClassBuilder.Register(() => DisableAnimation ? "bit-srb-nan" : string.Empty);

        ClassBuilder.Register(() => Underlined ? "bit-srb-und" : string.Empty);

        ClassBuilder.Register(() => _inputHasFocus ? $"bit-srb-foc {Classes?.Focused}" : string.Empty);

        ClassBuilder.Register(() => ShowSearchButton ? "bit-srb-ssb" : string.Empty);

        ClassBuilder.Register(() => HideIcon ? "bit-srb-hic" : string.Empty);

        ClassBuilder.Register(() => NoBorder ? "bit-srb-nbr" : string.Empty);

        ClassBuilder.Register(() => FullWidth ? "bit-srb-flw" : string.Empty);

        ClassBuilder.Register(() => Required ? "bit-srb-req" : string.Empty);

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-srb-sm",
            BitSize.Medium => "bit-srb-md",
            BitSize.Large => "bit-srb-lg",
            _ => string.Empty
        });

        ClassBuilder.Register(() => Background switch
        {
            BitColorKind.Primary => "bit-srb-bpr",
            BitColorKind.Secondary => "bit-srb-bse",
            BitColorKind.Tertiary => "bit-srb-btr",
            BitColorKind.Transparent => "bit-srb-btn",
            _ => "bit-srb-bpr"
        });

        ClassBuilder.Register(() => Color switch
        {
            BitColor.Primary => "bit-srb-pri",
            BitColor.Secondary => "bit-srb-sec",
            BitColor.Tertiary => "bit-srb-ter",
            BitColor.Info => "bit-srb-inf",
            BitColor.Success => "bit-srb-suc",
            BitColor.Warning => "bit-srb-wrn",
            BitColor.SevereWarning => "bit-srb-swr",
            BitColor.Error => "bit-srb-err",
            BitColor.PrimaryBackground => "bit-srb-pbg",
            BitColor.SecondaryBackground => "bit-srb-sbg",
            BitColor.TertiaryBackground => "bit-srb-tbg",
            BitColor.PrimaryForeground => "bit-srb-pfg",
            BitColor.SecondaryForeground => "bit-srb-sfg",
            BitColor.TertiaryForeground => "bit-srb-tfg",
            BitColor.PrimaryBorder => "bit-srb-pbr",
            BitColor.SecondaryBorder => "bit-srb-sbr",
            BitColor.TertiaryBorder => "bit-srb-tbr",
            _ => "bit-srb-pri"
        });
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => _inputHasFocus ? Styles?.Focused : string.Empty);
    }

    protected override async Task OnInitializedAsync()
    {
        _calloutId = $"BitSearchBox-{UniqueId}-callout";
        _overlayId = $"BitSearchBox-{UniqueId}-overlay";
        _scrollContainerId = $"BitSearchBox-{UniqueId}-scroll-container";
        _inputId = $"BitSearchBox-{UniqueId}-input";
        _labelId = $"BitSearchBox-{UniqueId}-label";

        _dotnetObj = DotNetObjectReference.Create(this);

        SetDefaultValue();

        OnValueChanged += HandleOnValueChanged;

        await base.OnInitializedAsync();
    }

    protected override void OnParametersSet()
    {
        _hasSuggestSource = SuggestItems is not null || SuggestItemsProvider is not null;

        _enterKeyHint = (EnterKeyHint ?? BitEnterKeyHint.Search) switch
        {
            BitEnterKeyHint.Enter => "enter",
            BitEnterKeyHint.Done => "done",
            BitEnterKeyHint.Go => "go",
            BitEnterKeyHint.Next => "next",
            BitEnterKeyHint.Previous => "previous",
            BitEnterKeyHint.Send => "send",
            _ => "search"
        };

        base.OnParametersSet();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender is false) return;

        try
        {
            // The keys that drive the suggest list must not also move the caret, scroll the page or
            // submit the surrounding form. Blazor's @onkeydown:preventDefault is evaluated at render
            // time, so a flag set inside the handler would always be one keystroke behind.
            await _js.BitSearchBoxSetupInput(InputElement);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out string? result, [NotNullWhen(false)] out string? parsingErrorMessage)
    {
        result = Trim ? value?.Trim() : value;
        parsingErrorMessage = null;
        return true;
    }



    private bool HasLabel => LabelTemplate is not null || Label.HasValue();

    /// <summary>
    /// Whether the field is holding any text at all, which is not the same as having a value:
    /// without <see cref="BitTextInputBase{TValue}.Immediate"/> the text the user is typing is not
    /// committed to the model yet, but the clear button and the collapsing icon must still react to it.
    /// </summary>
    private bool HasValue => _inputHasValue || CurrentValue.HasValue();

    private string GetItemId(int index) => $"BitSearchBox-{UniqueId}-item-{index}";

    private void SetInputHasValue(string? value)
    {
        var hasValue = value.HasValue();

        if (_inputHasValue == hasValue) return;

        _inputHasValue = hasValue;

        ClassBuilder.Reset();
    }

    protected override async Task HandleOnStringValueInputAsync(ChangeEventArgs e)
    {
        if (IsEnabled is false || ReadOnly) return;

        SetInputHasValue(e.Value?.ToString());

        await base.HandleOnStringValueInputAsync(e);
    }

    private void HandleOnValueChanged(object? sender, EventArgs args)
    {
        ClassBuilder.Reset();

        if (_suppressSearch) return;

        _selectedIndex = -1;

        // Fire and forget on purpose: the value setter is synchronous, so the search cannot be
        // awaited here and its failures have to be observed by the wrapper instead.
        _ = SearchItemsAndObserveFailures();
    }

    /// <summary>
    /// Runs a search that nobody awaits, so that a circuit torn down while it is still running
    /// cannot surface as an unobserved task exception.
    /// </summary>
    private async Task SearchItemsAndObserveFailures()
    {
        try
        {
            await SearchItems();
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
        catch (ObjectDisposedException) { } // we can ignore this exception here
    }

    private async Task HandleOnClick(MouseEventArgs e)
    {
        if (IsEnabled is false) return;

        await OnClick.InvokeAsync(e);
    }

    private async Task HandleOnIconClick()
    {
        if (IsEnabled is false) return;

        await InputElement.FocusAsync();
    }

    private async Task HandleOnFocus(FocusEventArgs e)
    {
        if (IsEnabled is false) return;

        await OnFocus.InvokeAsync(e);
    }

    private async Task HandleOnKeyUp(KeyboardEventArgs e)
    {
        if (IsEnabled is false) return;

        await OnKeyUp.InvokeAsync(e);
    }

    private async Task HandleInputFocusIn(FocusEventArgs e)
    {
        if (IsEnabled is false) return;

        _inputHasFocus = true;
        ClassBuilder.Reset();
        StyleBuilder.Reset();

        await OnFocusIn.InvokeAsync(e);

        if (_hasSuggestSource is false) return;

        if (ShowSuggestItemsOnFocus)
        {
            await SearchItems();
        }
        else
        {
            await Task.Delay(100);
            await OpenOrCloseCallout();
        }
    }

    private async Task HandleInputFocusOut(FocusEventArgs e)
    {
        _inputHasFocus = false;
        ClassBuilder.Reset();
        StyleBuilder.Reset();

        await OnFocusOut.InvokeAsync(e);

        if (Modeless)
        {
            await Task.Delay(100);
            await CloseCallout();
        }
    }

    private async Task HandleOnSearchButtonClick()
    {
        if (IsEnabled is false) return;

        await CloseCallout();

        await OnSearch.InvokeAsync(CurrentValueAsString);
    }

    private async Task HandleOnClearButtonClick()
    {
        if (IsEnabled is false || ReadOnly) return;

        await ClearValue();

        await InputElement.FocusAsync();
    }

    private async Task HandleOnKeyDown(KeyboardEventArgs e)
    {
        if (IsEnabled is false) return;

        await OnKeyDown.InvokeAsync(e);

        switch (e.Key)
        {
            case "Enter":
                await HandleEnter();
                break;

            case "Escape":
                await HandleEscape();
                break;

            case "ArrowDown":
                if (e.AltKey)
                {
                    if (_isOpen is false) await SearchItems();
                }
                else
                {
                    await ChangeSelectedItem(false);
                }
                break;

            case "ArrowUp":
                if (e.AltKey)
                {
                    await CloseCallout();
                }
                else
                {
                    await ChangeSelectedItem(true);
                }
                break;

            case "Home":
                await MoveSelectionToEdge(first: true);
                break;

            case "End":
                await MoveSelectionToEdge(first: false);
                break;

            case "PageUp":
                await MoveSelectionByPage(up: true);
                break;

            case "PageDown":
                await MoveSelectionByPage(up: false);
                break;

            case "Tab":
                if (_isOpen)
                {
                    _selectedIndex = -1;
                    await CloseCallout();
                }
                break;
        }
    }

    private async Task HandleEnter()
    {
        if (ReadOnly is false && _selectedIndex > -1 && _viewSuggestedItems.Count > _selectedIndex)
        {
            await SelectSuggestItem(_viewSuggestedItems[_selectedIndex]);
            return;
        }

        if (ReadOnly is false)
        {
            try
            {
                var inputValue = await _js.BitUtilsGetProperty(InputElement, "value");

                await SetCurrentValueAsStringAsync(inputValue);
            }
            catch (JSDisconnectedException) { } // we can ignore this exception here
        }

        await CloseCallout();

        await OnSearch.InvokeAsync(CurrentValueAsString);
    }

    private async Task HandleEscape()
    {
        await OnEscape.InvokeAsync();

        if (_isOpen)
        {
            _selectedIndex = -1;

            await CloseCallout();

            return;
        }

        if (ReadOnly || NoClearOnEscape) return;

        if (CurrentValue.HasNoValue())
        {
            // The model is already empty, but without Immediate the input element can still hold
            // text that was never committed. Escape must wipe that too, and since nothing was
            // actually cleared from the model there is no OnClear to raise.
            await SetInputElementValue(null);

            return;
        }

        await ClearValue();
    }

    private async Task HandleOnSuggestedItemClick(string item)
    {
        await SelectSuggestItem(item);
    }

    private async Task SelectSuggestItem(string item)
    {
        if (IsEnabled is false || ReadOnly) return;

        await CloseCallout();

        _selectedIndex = -1;

        await SetValueSuppressingSearch(item);

        // The accepted suggestion replaces whatever was typed, so the caret belongs at its end,
        // ready for the user to keep refining the term (see the WAI-ARIA combobox pattern).
        await MoveCursorToEnd();

        await OnSuggestItemSelect.InvokeAsync(item);

        await OnSearch.InvokeAsync(CurrentValueAsString);

        await SearchItems(openCallout: false);
    }

    private async Task ClearValue()
    {
        _selectedIndex = -1;

        await SetValueSuppressingSearch(null);

        await SearchItems(openCallout: false);

        await CloseCallout();

        await OnClear.InvokeAsync();
    }

    private async Task SetValueSuppressingSearch(string? value)
    {
        _suppressSearch = true;
        try
        {
            await SetCurrentValueAsStringAsync(value);
        }
        finally
        {
            _suppressSearch = false;
        }

        await SetInputElementValue(value);
    }

    /// <summary>
    /// Writes the value into the input element itself. Blazor only patches the value attribute when it
    /// differs from what the previous render produced, so uncommitted text (without <see cref="BitTextInputBase{TValue}.Immediate"/>)
    /// or a model that cannot change (a Value bound without a ValueChanged or an OnChange) would otherwise stay on screen.
    /// </summary>
    private async Task SetInputElementValue(string? value)
    {
        SetInputHasValue(value);

        if (IsDisposed) return;

        try
        {
            await _js.BitUtilsSetProperty(InputElement, "value", value);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private async Task MoveCursorToEnd()
    {
        if (IsDisposed) return;

        try
        {
            await _js.BitSearchBoxMoveCursorToEnd(InputElement);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private void SetInputMode()
    {
        _inputMode = InputMode?.ToString().ToLower();
    }

    private bool CanSearch(string? term)
    {
        var length = term?.Length ?? 0;

        if (MinSuggestTriggerChars <= 0) return true;

        return length >= MinSuggestTriggerChars;
    }

    private bool FilterSuggestItem(string? term, string? item)
    {
        if (SuggestFilterFunction is not null) return SuggestFilterFunction(term, item);

        if (item is null) return false;

        if (term.HasNoValue()) return true;

        return item.Contains(term!, StringComparison.OrdinalIgnoreCase);
    }

    private async Task SearchItems(bool openCallout = true, bool force = false)
    {
        if (IsDisposed) return;

        var term = CurrentValueAsString;

        // Every search invalidates the one before it, whichever branch it ends up taking: a term that
        // became too short, or a switch to an in-memory list, must abandon an in-flight provider call
        // just as a newer provider call does, otherwise its late result would repopulate the callout.
        // The superseded source is only cancelled here, never disposed: the search that owns it may
        // still be sitting inside the provider with its token, and pulling the source out from under
        // that call would turn any use of the token into an ObjectDisposedException. Every search
        // disposes its own source in its own finally block instead.
        _cancellationTokenSource?.Cancel();

        if (CanSearch(term) is false)
        {
            _isLoading = false;
            _searchTriggered = false;
            _viewSuggestedItems = [];
        }
        else if (SuggestItemsProvider is not null)
        {
            _searchTriggered = true;

            var cts = _cancellationTokenSource = new();

            _isLoading = true;

            Announce(openCallout, force);

            if (openCallout)
            {
                await OpenOrCloseCallout(force);
            }
            else
            {
                StateHasChanged();
            }

            try
            {
                var items = await SuggestItemsProvider(new(term, MaxSuggestCount, cts.Token));

                if (cts.IsCancellationRequested || IsDisposed) return;

                _viewSuggestedItems = [.. Limit(items)];
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch
            {
                if (cts.IsCancellationRequested) return;

                _viewSuggestedItems = [];
            }
            finally
            {
                if (cts.IsCancellationRequested is false)
                {
                    _isLoading = false;
                }

                // Only clear the field when this search is still the current one, so that a newer
                // search that already replaced it keeps its own source alive.
                if (ReferenceEquals(_cancellationTokenSource, cts))
                {
                    _cancellationTokenSource = null;
                }

                cts.Dispose();
            }
        }
        else if (SuggestItems is not null)
        {
            _searchTriggered = true;
            _isLoading = false;
            _viewSuggestedItems = [.. Limit(SuggestItems.Where(i => FilterSuggestItem(term, i)))];
        }
        else
        {
            _isLoading = false;
            _searchTriggered = false;
            _viewSuggestedItems = [];
        }

        if (IsDisposed) return;

        _selectedIndex = AutoSelectSuggestItem && _viewSuggestedItems.Count > 0 ? 0 : -1;

        Announce(openCallout, force);

        if (openCallout)
        {
            await OpenOrCloseCallout(force);
        }
        else
        {
            StateHasChanged();
        }
    }

    /// <summary>
    /// Feeds the live region of the search box so that the outcome of a search (how many suggestions
    /// were found, that there were none, that a term is still too short, or that the provider is still
    /// running) reaches the screen reader, which would otherwise get nothing but a silently changing list.
    /// </summary>
    private void Announce(bool openCallout, bool force)
    {
        // Announcing a list the user cannot see yet is pure noise, so this stays quiet for the
        // searches that do not open the callout and for the ones triggered while the field is not focused.
        if (openCallout is false || (force is false && _inputHasFocus is false)) return;

        var text = GetAnnouncementText();

        // Screen readers skip a live region update that repeats the previous text verbatim,
        // so an invisible zero width space is alternated to make every update unique.
        _announcementMarker = !_announcementMarker;

        _announcement = text.HasValue() && _announcementMarker ? text + '\u200B' : text;
    }

    private string? GetAnnouncementText()
    {
        if (_hasSuggestSource is false) return null;

        var term = CurrentValueAsString;
        var isTermTooShort = CanSearch(term) is false;

        if (AnnouncementProvider is not null)
        {
            return AnnouncementProvider(new(term, _viewSuggestedItems, _isLoading, isTermTooShort, MinSuggestTriggerChars));
        }

        if (_isLoading) return LoadingText.HasValue() ? LoadingText : "Loading suggestions.";

        if (isTermTooShort)
        {
            // An empty field is the starting state rather than a failed search, so it says nothing.
            return term.HasNoValue()
                ? null
                : $"Type {MinSuggestTriggerChars} or more characters for suggestions.";
        }

        var count = _viewSuggestedItems.Count;

        if (count == 0) return NoResultsText.HasValue() ? NoResultsText : "No suggestion found.";

        return $"{count} suggestion{(count == 1 ? string.Empty : "s")} available. " +
               "Use the up and down arrow keys to review them and enter to select one.";
    }

    private IEnumerable<string> Limit(IEnumerable<string> items)
    {
        return MaxSuggestCount > 0 ? items.Take(MaxSuggestCount) : items;
    }

    private bool ShouldShowCallout()
    {
        if (_hasSuggestSource is false) return false;

        if (_viewSuggestedItems.Count > 0) return true;

        if (_searchTriggered is false) return false;

        if (_isLoading) return true;

        return NoResultsText.HasValue() || NoResultsTemplate is not null;
    }

    private async Task OpenOrCloseCallout(bool force = false)
    {
        if (IsEnabled is false || IsDisposed) return;

        // Without the focus check, a value changed from outside of the component
        // (e.g. by a two-way bound sibling) would pop the callout open out of nowhere.
        if (force is false && _inputHasFocus is false)
        {
            StateHasChanged();
            return;
        }

        if (ShouldShowCallout())
        {
            if (_isOpen is false)
            {
                _isOpen = true;

                StateHasChanged();

                await Task.Delay(100); // wait for UI to be rendered by Blazor before showing the callout so the calculation would be correct!

                await ToggleCallout();

                await OnSuggestItemsToggle.InvokeAsync(true);
            }

            StateHasChanged();
        }
        else
        {
            await CloseCallout();
        }
    }

    private async Task CloseCallout()
    {
        if (IsEnabled is false || IsDisposed) return;

        var wasOpen = _isOpen;

        _isOpen = false;

        // A dismissed list must come back without a leftover virtual focus on it, otherwise
        // reopening it would silently highlight whatever was highlighted the previous time.
        _selectedIndex = -1;

        // The live region is deliberately left alone here: a fruitless search and a term that is
        // still too short both end up closing the callout, and those are exactly the two outcomes
        // a screen reader user has no other way of learning about. A live region only speaks when
        // its text changes, so keeping the last message costs nothing.

        await ToggleCallout();

        if (IsDisposed) return;

        StateHasChanged();

        if (wasOpen)
        {
            await OnSuggestItemsToggle.InvokeAsync(false);
        }
    }

    private async Task ToggleCallout()
    {
        if (IsEnabled is false || IsDisposed) return;

        try
        {
            await _js.BitCalloutToggleCallout(
                dotnetObj: _dotnetObj,
                componentId: _Id,
                component: null,
                calloutId: _calloutId,
                callout: null,
                overlayId: _overlayId,
                isCalloutOpen: _isOpen,
                responsiveMode: BitResponsiveMode.None,
                dropDirection: BitDropDirection.TopAndBottom,
                isRtl: Dir is BitDir.Rtl,
                scrollContainerId: _scrollContainerId,
                scrollOffset: 0,
                headerId: string.Empty,
                footerId: string.Empty,
                setCalloutWidth: false,
                fixedCalloutWidth: FixedCalloutWidth,
                maxWindowWidth: 0);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private async Task ChangeSelectedItem(bool isArrowUp)
    {
        if (_hasSuggestSource is false) return;

        if (_isOpen is false)
        {
            var wasEmpty = _viewSuggestedItems.Count == 0;

            if (wasEmpty)
            {
                await SearchItems();
            }
            else
            {
                await OpenOrCloseCallout();
            }

            if (_viewSuggestedItems.Count == 0) return;

            if (wasEmpty)
            {
                _selectedIndex = isArrowUp ? _viewSuggestedItems.Count - 1 : 0;

                await ScrollSelectedItemIntoView();

                StateHasChanged();

                return;
            }
        }

        var count = _viewSuggestedItems.Count;

        if (count == 0) return;

        _selectedIndex += isArrowUp ? -1 : +1;

        if (_selectedIndex < 0)
        {
            _selectedIndex = count - 1;
        }

        if (_selectedIndex >= count)
        {
            _selectedIndex = 0;
        }

        await ScrollSelectedItemIntoView();

        StateHasChanged();
    }

    private async Task MoveSelectionToEdge(bool first)
    {
        // Home & End only take over the list navigation while an item is virtually focused,
        // otherwise they keep their default behavior of moving the caret inside the input.
        if (_isOpen is false || _selectedIndex < 0 || _viewSuggestedItems.Count == 0) return;

        _selectedIndex = first ? 0 : _viewSuggestedItems.Count - 1;

        await ScrollSelectedItemIntoView();

        StateHasChanged();
    }

    private async Task MoveSelectionByPage(bool up)
    {
        // Unlike Home & End, page up & page down have no meaning inside a single line text input,
        // so they take over as soon as the list is open even if no item is highlighted yet.
        if (_hasSuggestSource is false || _isOpen is false || _viewSuggestedItems.Count == 0) return;

        var count = _viewSuggestedItems.Count;
        var current = _selectedIndex < 0 ? (up ? count : -1) : _selectedIndex;

        // A page jump clamps at the edges instead of wrapping around, so holding the key
        // reliably walks to the first or the last item.
        _selectedIndex = Math.Clamp(current + (up ? -SuggestPageSize : +SuggestPageSize), 0, count - 1);

        await ScrollSelectedItemIntoView();

        StateHasChanged();
    }

    private async Task ScrollSelectedItemIntoView()
    {
        if (_selectedIndex < 0 || IsDisposed) return;

        try
        {
            await _js.BitSearchBoxScrollItemIntoView(_scrollContainerId, GetItemId(_selectedIndex));
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private List<BitSearchBoxHighlightSegment> GetHighlightSegments(string item)
    {
        var term = CurrentValueAsString;

        if (item.HasNoValue() || term.HasNoValue()) return [new(item, false)];

        var segments = new List<BitSearchBoxHighlightSegment>();
        var index = 0;

        while (index < item.Length)
        {
            var found = item.IndexOf(term!, index, StringComparison.OrdinalIgnoreCase);

            if (found < 0)
            {
                segments.Add(new(item[index..], false));
                break;
            }

            if (found > index)
            {
                segments.Add(new(item[index..found], false));
            }

            segments.Add(new(item.Substring(found, term!.Length), true));

            index = found + term.Length;
        }

        return segments;
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        await base.DisposeAsync(disposing);

        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();

        OnValueChanged -= HandleOnValueChanged;

        if (_dotnetObj is not null)
        {
            _dotnetObj.Dispose();

            try
            {
                await _js.BitCalloutClearCallout(_calloutId);
            }
            catch (JSDisconnectedException) { } // we can ignore this exception here
        }
    }
}
