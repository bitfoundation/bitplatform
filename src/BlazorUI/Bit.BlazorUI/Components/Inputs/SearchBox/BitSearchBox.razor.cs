using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// A search box (SearchBox) provides an input field for searching content within a site or app to find specific items.
/// </summary>
public partial class BitSearchBox : BitTextInputBase<string?>
{
    private bool _isOpen;
    private string? _inputMode;
    private bool _inputHasFocus;
    private int _selectedIndex = -1;
    private string _inputId = string.Empty;
    private string _calloutId = string.Empty;
    private List<string> _viewSuggestedItems = [];
    private string _scrollContainerId = string.Empty;
    private CancellationTokenSource? _cancellationTokenSource;
    private DotNetObjectReference<BitSearchBox> _dotnetObj = default!;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// The accent color kind of the search box.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColorKind? Accent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitSearchBox.
    /// </summary>
    [Parameter] public BitSearchBoxClassStyles? Classes { get; set; }

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
    /// The default value of the text in the SearchBox, in the case of an uncontrolled component.
    /// </summary>
    [Parameter] public string? DefaultValue { get; set; }

    /// <summary>
    /// Whether or not to animate the search box icon on focus.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool DisableAnimation { get; set; }

    /// <summary>
    /// Whether or not to make the icon be always visible (it hides by default when the search box is focused).
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FixedIcon { get; set; }

    /// <summary>
    /// Whether or not the icon is visible.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool HideIcon { get; set; }

    /// <summary>
    /// Whether to hide the clear button when the BitSearchBox has value.
    /// </summary>
    [Parameter] public bool HideClearButton { get; set; }

    /// <summary>
    /// The icon name for the icon shown at the beginning of the search box.
    /// </summary>
    [Parameter] public string IconName { get; set; } = "Search";

    /// <summary>
    /// Sets the inputmode html attribute of the input element.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(SetInputMode))]
    public BitInputMode? InputMode { get; set; }

    /// <summary>
    /// The maximum number of items or suggestions that will be displayed.
    /// </summary>
    [Parameter] public int MaxSuggestCount { get; set; } = 5;

    /// <summary>
    /// The minimum character requirement for doing a search in suggest items.
    /// </summary>
    [Parameter] public int MinSuggestTriggerChars { get; set; } = 3;

    /// <summary>
    /// Removes the overlay of suggest items callout.
    /// </summary>
    [Parameter] public bool Modeless { get; set; }

    /// <summary>
    /// Removes the default border of the search box.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool NoBorder { get; set; }

    /// <summary>
    /// Callback executed when the user clears the search box by either clicking 'X' or hitting escape.
    /// </summary>
    [Parameter] public EventCallback OnClear { get; set; }

    /// <summary>
    /// Callback executed when the user presses escape in the search box.
    /// </summary>
    [Parameter] public EventCallback OnEscape { get; set; }

    /// <summary>
    /// Callback executed when the user presses enter in the search box.
    /// </summary>
    [Parameter] public EventCallback<string> OnSearch { get; set; }

    /// <summary>
    /// Placeholder for the search box.
    /// </summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>
    /// Prefix text displayed before the searchbox input. This is not included in the value.
    /// </summary>
    [Parameter] public string? Prefix { get; set; }

    /// <summary>
    /// The custom template for the prefix of the searchbox.
    /// </summary>
    [Parameter] public RenderFragment? PrefixTemplate { get; set; }

    /// <summary>
    /// Custom icon name for the search button.
    /// </summary>
    [Parameter] public string SearchButtonIconName { get; set; } = "ChromeBackMirrored";

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
    /// Custom CSS styles for different parts of the BitSearchBox.
    /// </summary>
    [Parameter] public BitSearchBoxClassStyles? Styles { get; set; }

    /// <summary>
    /// Suffix text displayed after the searchbox input. This is not included in the value. 
    /// </summary>
    [Parameter] public string? Suffix { get; set; }

    /// <summary>
    /// The custom template for the suffix of the searchbox.
    /// </summary>
    [Parameter] public RenderFragment? SuffixTemplate { get; set; }

    /// <summary>
    /// Custom search function to be used in place of the default search algorithm.
    /// </summary>
    [Parameter] public Func<string?, string?, bool>? SuggestFilterFunction { get; set; }

    /// <summary>
    /// The list of suggest items to display in the callout.
    /// </summary>
    [Parameter] public IEnumerable<string>? SuggestItems { get; set; }

    /// <summary>
    /// The item provider function providing suggest items.
    /// </summary>
    [Parameter] public BitSearchBoxSuggestItemsProvider? SuggestItemsProvider { get; set; }

    /// <summary>
    /// The custom template for rendering the suggest items of the BitSearchBox.
    /// </summary>
    [Parameter] public RenderFragment<string>? SuggestItemTemplate { get; set; }

    /// <summary>
    /// Whether or not the SearchBox is underlined.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Underlined { get; set; }



    /// <summary>
    /// Clears the input element. 
    /// </summary>
    public async Task Clear()
    {
        await SetCurrentValueAsync(null);
        await _js.BitUtilsSetProperty(InputElement, "value", null);
    }



    [JSInvokable("CloseCallout")]
    public void _CloseCalloutBeforeAnotherCalloutIsOpened()
    {
        if (IsEnabled is false) return;

        _isOpen = false;
        StateHasChanged();
    }



    protected override string RootElementClass => "bit-srb";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => CurrentValue.HasValue() ? $"bit-srb-{(FixedIcon ? "fic-" : string.Empty)}hvl" : string.Empty);

        ClassBuilder.Register(() => DisableAnimation ? "bit-srb-nan" : string.Empty);

        ClassBuilder.Register(() => Underlined ? "bit-srb-und" : string.Empty);

        ClassBuilder.Register(() => _inputHasFocus ? $"bit-srb-{(FixedIcon ? "fic-" : string.Empty)}foc {Classes?.Focused}" : string.Empty);

        ClassBuilder.Register(() => ShowSearchButton ? "bit-srb-ssb" : string.Empty);

        ClassBuilder.Register(() => HideIcon ? "bit-srb-hic" : string.Empty);

        ClassBuilder.Register(() => NoBorder ? "bit-srb-nbr" : string.Empty);

        ClassBuilder.Register(() => Accent switch
        {
            BitColorKind.Primary => "bit-srb-apri",
            BitColorKind.Secondary => "bit-srb-asec",
            BitColorKind.Tertiary => "bit-srb-ater",
            BitColorKind.Transparent => "bit-srb-atra",
            _ => string.Empty
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
        _scrollContainerId = $"BitSearchBox-{UniqueId}-scroll-container";
        _inputId = $"BitSearchBox-{UniqueId}-input";

        if (CurrentValue.HasNoValue() && DefaultValue.HasValue())
        {
            await SetCurrentValueAsStringAsync(DefaultValue);
        }

        OnValueChanged += HandleOnValueChanged;

        await base.OnInitializedAsync();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            _dotnetObj = DotNetObjectReference.Create(this);
        }

        base.OnAfterRender(firstRender);
    }

    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out string? result, [NotNullWhen(false)] out string? parsingErrorMessage)
    {
        result = value;
        parsingErrorMessage = null;
        return true;
    }



    private void HandleOnValueChanged(object? sender, EventArgs args)
    {
        if (_selectedIndex == -1)
        {
            _ = SearchItems();
        }

        _selectedIndex = -1;

        ClassBuilder.Reset();
    }

    private async Task HandleInputFocusIn()
    {
        _inputHasFocus = true;
        ClassBuilder.Reset();
        StyleBuilder.Reset();

        await Task.Delay(100);
        await OpenOrCloseCallout();
    }

    private async Task HandleInputFocusOut()
    {
        _inputHasFocus = false;
        ClassBuilder.Reset();
        StyleBuilder.Reset();

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

        await OnSearch.InvokeAsync(CurrentValue);
    }

    private async Task HandleOnClearButtonClick()
    {
        if (IsEnabled is false || ReadOnly) return;

        await HandleOnStringValueChangeAsync(new() { Value = string.Empty });

        await InputElement.FocusAsync();

        await OnClear.InvokeAsync();
    }

    private async Task HandleOnKeyDown(KeyboardEventArgs eventArgs)
    {
        if (IsEnabled is false || InvalidValueBinding()) return;

        if (eventArgs.Key == "Enter")
        {
            if (_selectedIndex > -1 && _viewSuggestedItems.Count > _selectedIndex)
            {
                await HandleOnSuggestedItemClick(_viewSuggestedItems[_selectedIndex]);
            }
            else
            {
                CurrentValue = await _js.BitUtilsGetProperty(InputElement, "value");

                await CloseCallout();

                await OnSearch.InvokeAsync(CurrentValue);
            }

            return;
        }

        if (ReadOnly) return;

        if (eventArgs.Key == "Escape")
        {
            if (_isOpen)
            {
                await CloseCallout();
                return;
            }

            CurrentValue = string.Empty;

            await CloseCallout();

            await OnEscape.InvokeAsync();

            await OnClear.InvokeAsync();

            //await InputElement.FocusAsync(); // is it required when the keydown event is captured on the input itself?

            return;
        }

        if (eventArgs.Key == "ArrowUp")
        {
            await ChangeSelectedItem(true);

            return;
        }

        if (eventArgs.Key == "ArrowDown")
        {
            await ChangeSelectedItem(false);

            return;
        }
    }

    private async Task HandleOnSuggestedItemClick(string item)
    {
        if (IsEnabled is false || ReadOnly || InvalidValueBinding()) return;

        await CloseCallout();

        _selectedIndex = 0;

        CurrentValue = item;

        await OnSearch.InvokeAsync(CurrentValueAsString);

        await SearchItems(false);
    }

    private void SetInputMode()
    {
        _inputMode = InputMode?.ToString().ToLower();
    }

    private async Task SearchItems(bool openCallout = true)
    {
        if (CurrentValue.HasNoValue() || CurrentValue!.Length < MinSuggestTriggerChars)
        {
            _viewSuggestedItems = [];
        }
        else if (SuggestItemsProvider is not null)
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new();
            _viewSuggestedItems = [.. (await SuggestItemsProvider(new(CurrentValue, MaxSuggestCount, _cancellationTokenSource.Token))).Take(MaxSuggestCount)];
        }
        else if (SuggestItems is not null)
        {
            _viewSuggestedItems = [.. SuggestItems
                            .Where(i => SuggestFilterFunction is not null
                                        ? SuggestFilterFunction.Invoke(CurrentValue, i)
                                        : (i?.Contains(CurrentValue!, StringComparison.OrdinalIgnoreCase) ?? false))
                            .Take(MaxSuggestCount)];
        }
        else
        {
            _viewSuggestedItems = [];
        }

        if (openCallout)
        {
            await OpenOrCloseCallout();
        }
    }

    private async Task OpenOrCloseCallout()
    {
        if (IsEnabled is false) return;

        if (_viewSuggestedItems.Any())
        {
            if (_isOpen is false)
            {
                _isOpen = true;

                StateHasChanged();

                await Task.Delay(100); // wait for UI to be rendered by Blazor before showing the callout so the calculation would be correct!

                await ToggleCallout();
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
        if (IsEnabled is false) return;

        _isOpen = false;
        await ToggleCallout();

        StateHasChanged();
    }

    private async Task ToggleCallout()
    {
        if (IsEnabled is false || IsDisposed) return;

        await _js.BitCalloutToggleCallout(_dotnetObj,
                                          _Id,
                                          null,
                                          _calloutId,
                                          null,
                                          _isOpen,
                                          BitResponsiveMode.None,
                                          BitDropDirection.TopAndBottom,
                                          Dir is BitDir.Rtl,
                                          _scrollContainerId,
                                          0,
                                          string.Empty,
                                          string.Empty,
                                          true);
    }

    private async Task ChangeSelectedItem(bool isArrowUp)
    {
        if (_viewSuggestedItems.Any() is false) return;

        if (_isOpen is false)
        {
            await OpenOrCloseCallout();
        }

        _selectedIndex += isArrowUp ? -1 : +1;

        var count = _viewSuggestedItems.Count;

        if (_selectedIndex < 0)
        {
            _selectedIndex = count - 1;
        }

        if (_selectedIndex >= count)
        {
            _selectedIndex = 0;
        }

        await _js.BitSearchBoxMoveCursorToEnd(InputElement);
    }

    private int? GetTotalItems()
    {
        if (_viewSuggestedItems is null) return null;

        return _viewSuggestedItems.Count;
    }

    private int? GetItemPosInSet(string item)
    {
        return _viewSuggestedItems?.IndexOf(item) + 1;
    }

    private bool GetIsSelected(string item)
    {
        return _selectedIndex > -1 && _viewSuggestedItems.IndexOf(item) == _selectedIndex;
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        await base.DisposeAsync(disposing);

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
