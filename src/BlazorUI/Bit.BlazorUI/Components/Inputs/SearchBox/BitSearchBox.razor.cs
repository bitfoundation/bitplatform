using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

public partial class BitSearchBox : BitTextInputBase<string?>, IAsyncDisposable
{
    private bool _isOpen;
    private bool _disposed;
    private bool _inputHasFocus;
    private int _selectedIndex = -1;
    private string _inputId = string.Empty;
    private List<string> _searchItems = [];
    private string _calloutId = string.Empty;
    private string _scrollContainerId = string.Empty;
    private CancellationTokenSource _cancellationTokenSource = new();
    private DotNetObjectReference<BitSearchBox> _dotnetObj = default!;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Specifies the value of the autocomplete attribute of the input component.
    /// </summary>
    [Parameter] public string? Autocomplete { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitSearchBox.
    /// </summary>
    [Parameter] public BitSearchBoxClassStyles? Classes { get; set; }

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
    /// The maximum number of items or suggestions that will be displayed.
    /// </summary>
    [Parameter] public int MaxSuggestCount { get; set; } = 5;

    /// <summary>
    /// The minimum character requirement for doing a search in suggest items.
    /// </summary>
    [Parameter] public int MinSuggestTriggerChars { get; set; } = 3;

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
    /// Custom icon name for the search button.
    /// </summary>
    [Parameter] public string SearchButtonIconName { get; set; } = "ChromeBackMirrored";

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
    /// Custom search function to be used in place of the default search algorithm.
    /// </summary>
    [Parameter] public Func<string?, string?, bool>? SuggestFilterFunction { get; set; }

    /// <summary>
    /// The list of suggest items to display in the callout.
    /// </summary>
    [Parameter] public IEnumerable<string>? SuggestItems { get; set; }

    /// <summary>
    /// The function providing suggest items.
    /// </summary>
    [Parameter] public Func<string?, int, Task<ICollection<string>>>? SuggestItemProvider { get; set; }

    /// <summary>
    /// The custom template for rendering the suggest items of the BitSearchBox.
    /// </summary>
    [Parameter] public RenderFragment<string>? SuggestItemTemplate { get; set; }

    /// <summary>
    /// Whether or not the SearchBox is underlined.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Underlined { get; set; }



    [JSInvokable("CloseCallout")]
    public void CloseCalloutBeforeAnotherCalloutIsOpened()
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

        _dotnetObj = DotNetObjectReference.Create(this);

        await base.OnInitializedAsync();
    }

    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out string? result, [NotNullWhen(false)] out string? parsingErrorMessage)
    {
        result = value;
        parsingErrorMessage = null;
        return true;
    }



    private void HandleOnValueChanged(object? sender, EventArgs args)
    {
        _ = SearchItems();

        ClassBuilder.Reset();
    }

    private void HandleInputFocusIn()
    {
        _inputHasFocus = true;
        ClassBuilder.Reset();
        StyleBuilder.Reset();
    }

    private void HandleInputFocusOut()
    {
        _inputHasFocus = false;
        ClassBuilder.Reset();
        StyleBuilder.Reset();
    }

    private async Task HandleOnSearchButtonClick()
    {
        if (IsEnabled is false) return;

        await CloseCallout();

        await OnSearch.InvokeAsync(CurrentValue);
    }

    private async Task HandleOnClearButtonClick()
    {
        if (IsEnabled is false) return;

        await HandleOnStringValueChangeAsync(new() { Value = string.Empty });

        await InputElement.FocusAsync();

        await OnClear.InvokeAsync();
    }

    private async Task HandleOnKeyDown(KeyboardEventArgs eventArgs)
    {
        if (IsEnabled is false || InvalidValueBinding()) return;

        if (eventArgs.Key == "Escape")
        {
            CurrentValue = string.Empty;
            await CloseCallout();
            await OnEscape.InvokeAsync();
            await OnClear.InvokeAsync();
            //await InputElement.FocusAsync(); // is it required when the keydown event is captured on the input itself?
        }
        else if (eventArgs.Key == "Enter")
        {
            CurrentValue = await _js.GetProperty(InputElement, "value");
            await CloseCallout();
            await OnSearch.InvokeAsync(CurrentValue);
        }
        else if (eventArgs.Key == "ArrowUp")
        {
            await ChangeSelectedItem(true);
        }
        else if (eventArgs.Key == "ArrowDown")
        {
            await ChangeSelectedItem(false);
        }
    }

    private async Task ToggleCallout()
    {
        if (IsEnabled is false) return;

        await _js.ToggleCallout(_dotnetObj,
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
                                true,
                                RootElementClass);
    }

    private async Task CloseCallout()
    {
        if (IsEnabled is false) return;

        _isOpen = false;
        await ToggleCallout();

        StateHasChanged();
    }

    private async Task SearchItems()
    {
        if (CurrentValue.HasNoValue() || CurrentValue!.Length < MinSuggestTriggerChars)
        {
            _searchItems = [];
        }
        else if (SuggestItemProvider is not null)
        {
            _searchItems = [.. (await SuggestItemProvider.Invoke(CurrentValue, MaxSuggestCount)).Take(MaxSuggestCount)];
        }
        else if (SuggestItems is not null)
        {
            _searchItems = SuggestItems
                            .Where(i => SuggestFilterFunction is not null
                                        ? SuggestFilterFunction.Invoke(CurrentValue, i)
                                        : (i?.Contains(CurrentValue!, StringComparison.OrdinalIgnoreCase) ?? false))
                            .Take(MaxSuggestCount).ToList();
        }
        else
        {
            _searchItems = [];
        }

        await HandleCallout();
    }

    private async Task HandleCallout()
    {
        if (IsEnabled is false) return;

        if (_searchItems.Any())
        {
            _selectedIndex = _searchItems.FindIndex(i => i == CurrentValue);

            if (_isOpen is false)
            {
                _isOpen = true;
                await ToggleCallout();
            }
        }
        else
        {
            await CloseCallout();
        }
    }

    private async Task ChangeSelectedItem(bool isArrowUp)
    {
        if (_isOpen is false) return;
        if (_searchItems.Any() is false) return;

        var count = _searchItems.Count;

        if (_selectedIndex < 0 || count == 1)
        {
            _selectedIndex = isArrowUp ? count - 1 : 0;
        }
        else if (_selectedIndex == count - 1 && isArrowUp is false)
        {
            _selectedIndex = 0;
        }
        else if (_selectedIndex == 0 && isArrowUp)
        {
            _selectedIndex = count - 1;
        }
        else if (isArrowUp)
        {
            _selectedIndex--;
        }
        else
        {
            _selectedIndex++;
        }

        CurrentValue = _searchItems[_selectedIndex];

        await _js.BitSearchBoxMoveCursorToEnd(InputElement);
    }

    private async Task HandleOnItemClick(string item)
    {
        if (IsEnabled is false) return;
        if (InvalidValueBinding()) return;

        CurrentValue = item;

        await CloseCallout();

        await OnSearch.InvokeAsync(CurrentValueAsString);

        StateHasChanged();
    }

    private int? GetTotalItems()
    {
        if (_searchItems is null) return null;

        return _searchItems.Count;
    }

    private int? GetItemPosInSet(string item)
    {
        return _searchItems?.IndexOf(item) + 1;
    }

    private bool GetIsSelected(string item)
    {
        return _selectedIndex > -1 && _searchItems.IndexOf(item) == _selectedIndex;
    }



    public async ValueTask DisposeAsync()
    {
        await DisposeAsync(true);
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsync(bool disposing)
    {
        if (_disposed || disposing is false) return;

        if (_dotnetObj is not null)
        {
            _dotnetObj.Dispose();

            try
            {
                await _js.ClearCallout(_calloutId);
            }
            catch (JSDisconnectedException) { } // we can ignore this exception here
        }

        _cancellationTokenSource?.Dispose();

        OnValueChanged -= HandleOnValueChanged;

        _disposed = true;
    }
}
