using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

public partial class BitSearchBox
{
    private bool isOpen;
    private bool disableAnimation;
    private bool isUnderlined;
    private bool inputHasFocus;
    private bool fixedIcon;

    private string? _searchValue;
    private string _inputId = string.Empty;
    private string _calloutId = string.Empty;
    private string _scrollContainerId = string.Empty;
    private ElementReference _inputRef = default!;
    private Virtualize<string>? _virtualizeElement = default!;
    private CancellationTokenSource _cancellationTokenSource = new();
    private DotNetObjectReference<BitSearchBox> _dotnetObj = default!;
    private List<string> _items = [];
    private List<string> _searchItems = [];
    private int _selectedIndex = -1;
    private int? _totalItems;
    private bool _refreshItems;

    private bool InputHasFocus
    {
        get => inputHasFocus;
        set
        {
            if (inputHasFocus == value) return;

            inputHasFocus = value;
            ClassBuilder.Reset();
        }
    }

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
    [Parameter]
    public bool DisableAnimation
    {
        get => disableAnimation;
        set
        {
            if (disableAnimation == value) return;

            disableAnimation = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Whether or not to make the icon be always visible (it hides by default when the search box is focused).
    /// </summary>
    [Parameter]
    public bool FixedIcon
    {
        get => fixedIcon;
        set
        {
            if (fixedIcon == value) return;

            fixedIcon = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Whether or not the SearchBox is underlined.
    /// </summary>
    [Parameter]
    public bool IsUnderlined
    {
        get => isUnderlined;
        set
        {
            if (isUnderlined == value) return;

            isUnderlined = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// The icon name for the icon shown at the beginning of the search box.
    /// </summary>
    [Parameter] public string IconName { get; set; } = "Search";

    /// <summary>
    /// Callback for when the input value changes.
    /// </summary>
    [Parameter] public EventCallback<string?> OnChange { get; set; }

    /// <summary>
    /// Callback executed when the user presses escape in the search box.
    /// </summary>
    [Parameter] public EventCallback OnEscape { get; set; }

    /// <summary>
    /// Callback executed when the user clears the search box by either clicking 'X' or hitting escape.
    /// </summary>
    [Parameter] public EventCallback OnClear { get; set; }

    /// <summary>
    /// Callback executed when the user presses enter in the search box.
    /// </summary>
    [Parameter] public EventCallback<string> OnSearch { get; set; }

    /// <summary>
    /// Placeholder for the search box.
    /// </summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitSearchBox.
    /// </summary>
    [Parameter] public BitSearchBoxClassStyles? Styles { get; set; }

    /// <summary>
    /// Enables virtualization to render only the visible items.
    /// </summary>
    [Parameter] public bool Virtualize { get; set; }

    /// <summary>
    /// The template for items that have not yet been rendered in virtualization mode.
    /// </summary>
    [Parameter] public RenderFragment<PlaceholderContext>? VirtualizePlaceholder { get; set; }

    /// <summary>
    /// The list of suggested items to display in the callout.
    /// </summary>
    [Parameter] public ICollection<string>? SuggestedItems { get; set; }

    /// <summary>
    /// The height of each item in pixels for virtualization.
    /// </summary>
    [Parameter] public int SuggestedItemSize { get; set; } = 35;

    /// <summary>
    /// The function providing items to the list for virtualization.
    /// </summary>
    [Parameter] public BitSearchBoxSuggestedItemsProvider<string>? SuggestedItemsProvider { get; set; }

    /// <summary>
    /// The custom template for rendering the items of the BitSearchBox.
    /// </summary>
    [Parameter] public RenderFragment<string>? SuggestedItemTemplate { get; set; }

    /// <summary>
    /// Custom search function to be used in place of the default search algorithm.
    /// </summary>
    [Parameter] public Func<ICollection<string>, string, ICollection<string>>? SearchFunction { get; set; }

    /// <summary>
    /// The delay, in milliseconds, applied to the search functionality.
    /// </summary>
    [Parameter] public int SearchDelay { get; set; } = 400;

    /// <summary>
    /// The maximum number of items or suggestions that will be displayed.
    /// </summary>
    [Parameter] public int MaxSuggestedItems { get; set; } = 5;

    /// <summary>
    /// The minimum character requirement for doing a search in suggested items.
    /// </summary>
    [Parameter] public int MinSearchLength { get; set; } = 3;

    /// <summary>
    /// Alias of ChildContent.
    /// </summary>
    [Parameter] public RenderFragment? Options { get; set; }

    /// <summary>
    /// The content of the Dropdown, a list of BitDropdownOption components.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    protected override string RootElementClass => "bit-srb";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => CurrentValue.HasValue() ? $"{RootElementClass}-{(FixedIcon ? "fic-" : string.Empty)}hvl" : string.Empty);

        ClassBuilder.Register(() => DisableAnimation ? $"{RootElementClass}-nan" : string.Empty);

        ClassBuilder.Register(() => IsUnderlined ? $"{RootElementClass}-und" : string.Empty);

        ClassBuilder.Register(() => InputHasFocus ? $"{RootElementClass}-{(FixedIcon ? "fic-" : string.Empty)}foc" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override Task OnInitializedAsync()
    {
        _calloutId = $"BitSearchBox-{UniqueId}-callout";
        _scrollContainerId = $"BitSearchBox-{UniqueId}-scroll-container";
        _inputId = $"BitSearchBox-{UniqueId}-input";

        if (CurrentValueAsString.HasNoValue() && DefaultValue.HasValue())
        {
            CurrentValueAsString = DefaultValue;
        }

        _searchValue = CurrentValueAsString;

        OnValueChanged += HandleOnValueChanged;

        _dotnetObj = DotNetObjectReference.Create(this);

        return base.OnInitializedAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        if (ChildContent is not null) return;

        if (SuggestedItems is not null)
        {
            _items = [.. SuggestedItems.Distinct()];
        }

        if (CurrentValueAsString.HasNoValue())
        {
            _searchValue = CurrentValueAsString;
        }
    }

    private void HandleOnValueChanged(object? sender, EventArgs args) => ClassBuilder.Reset();

    private void HandleInputFocusIn() => InputHasFocus = true;

    private void HandleInputFocusOut() => InputHasFocus = false;

    private async Task HandleOnClear()
    {
        await HandleOnChange(new() { Value = string.Empty });

        await _inputRef.FocusAsync();

        await OnClear.InvokeAsync();
    }

    private async Task HandleOnChange(ChangeEventArgs e)
    {
        if (IsEnabled is false) return;
        if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;

        CurrentValueAsString = _searchValue = e.Value?.ToString();

        ThrottleSearch();

        await OnChange.InvokeAsync(CurrentValue);
    }

    private async Task HandleOnKeyDown(KeyboardEventArgs eventArgs)
    {
        if (IsEnabled is false) return;

        if (eventArgs.Key == "Escape")
        {
            CurrentValueAsString = _searchValue = string.Empty;
            //await _inputRef.FocusAsync(); // is it required when the keydown event is captured on the input itself?
            await OnEscape.InvokeAsync();
            await OnClear.InvokeAsync();
            await CloseCallout();
        }
        else if (eventArgs.Key == "Enter")
        {
            CurrentValueAsString = _searchValue = await _js.GetProperty(_inputRef, "value");
            await OnSearch.InvokeAsync(CurrentValue);
            await CloseCallout();
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
                                _calloutId,
                                isOpen,
                                BitResponsiveMode.None,
                                BitDropDirection.TopAndBottom,
                                false,
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

        isOpen = false;
        await ToggleCallout();

        StateHasChanged();
    }

    private async ValueTask<ItemsProviderResult<string>> InternalItemsProvider(ItemsProviderRequest request)
    {
        if (SuggestedItemsProvider is null) return default;

        // Debounce the requests. This eliminates a lot of redundant queries at the cost of slight lag after interactions.
        // TODO: Consider making this configurable, or smarter (e.g., doesn't delay on first call in a batch, then the amount
        // of delay increases if you rapidly issue repeated requests, such as when scrolling a long way)
        await Task.Delay(100);

        if (request.CancellationToken.IsCancellationRequested) return default;

        if (_refreshItems)
        {
            return new ItemsProviderResult<string>(_searchItems, _totalItems.GetValueOrDefault(_searchItems.Count));
        }

        if (_searchValue.HasNoValue() || _searchValue!.Length < MinSearchLength)
        {
            _selectedIndex = -1;
            _searchItems?.Clear();
            await ChangeStateCallout();
            return new ItemsProviderResult<string>();
        }

        var countRequest = request.Count > MaxSuggestedItems || request.Count == 0 ? MaxSuggestedItems : request.Count;

        // Combine the query parameters from Virtualize with the ones from PaginationState
        var providerRequest = new BitSearchBoxSuggestedItemsProviderRequest<string>(0, countRequest, _searchValue, request.CancellationToken);
        var providerResult = await SuggestedItemsProvider(providerRequest);

        if (request.CancellationToken.IsCancellationRequested) return default;

        _totalItems = countRequest;
        _searchItems = providerResult.Items.Distinct().Take(countRequest).ToList();

        if (CurrentValue.HasValue())
        {
            _selectedIndex = _searchItems.FindIndex(i => i == CurrentValue);
        }
        else
        {
            _selectedIndex = -1;
        }

        await ChangeStateCallout();

        return new ItemsProviderResult<string>(_searchItems, countRequest);
    }

    private async Task SearchItems()
    {
        if (Virtualize && SuggestedItemsProvider is not null)
        {
            await _virtualizeElement!.RefreshDataAsync();
        }
        else
        {
            _searchItems = _searchValue.HasNoValue() || _searchValue!.Length < MinSearchLength
                ? []
                : SearchFunction is not null
                    ? [.. SearchFunction.Invoke(_items, _searchValue!).Distinct().Take(MaxSuggestedItems)]
                    : [.. _items.Where(i => i?.Contains(_searchValue!, StringComparison.OrdinalIgnoreCase) ?? false).Take(MaxSuggestedItems)];

            await ChangeStateCallout();
        }
    }

    private void ThrottleSearch()
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
        _cancellationTokenSource = new();

        Task.Run(async () =>
        {
            await Task.Delay(SearchDelay, _cancellationTokenSource.Token);
            await InvokeAsync(async () =>
            {
                if (_cancellationTokenSource.IsCancellationRequested) return;

                await SearchItems();
                StateHasChanged();
            });
        }, _cancellationTokenSource.Token);
    }

    private async Task ChangeStateCallout()
    {
        if (IsEnabled is false) return;

        if (_searchItems.Any())
        {
            _selectedIndex = _searchItems.FindIndex(i => i == _searchValue);

            if (isOpen is false)
            {
                isOpen = true;
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
        if (IsEnabled is false) return;
        if (isOpen is false) return;
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

        CurrentValueAsString = _searchValue = _searchItems[_selectedIndex];
        await OnChange.InvokeAsync(CurrentValueAsString);

        if (Virtualize && SuggestedItemsProvider is not null)
        {
            _refreshItems = true;
            try
            {
                await _virtualizeElement!.RefreshDataAsync();
            }
            finally
            {
                _refreshItems = false;
            }
        }
        await _js.InvokeVoidAsync("BitSearchBox.moveCursorToEnd", _inputRef);
    }

    internal async Task HandleOnItemClick(string item)
    {
        if (IsEnabled is false) return;
        if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;

        CurrentValueAsString = _searchValue = item;

        await CloseCallout();

        await OnChange.InvokeAsync(CurrentValueAsString);

        await OnSearch.InvokeAsync(CurrentValueAsString);

        StateHasChanged();
    }

    internal int? GetTotalItems()
    {
        if (_items is null) return null;

        if (_totalItems.HasValue is false)
        {
            _totalItems = _items.Count;
        }

        return _totalItems.Value;
    }

    internal int? GetItemPosInSet(string item)
    {
        return _searchItems?.IndexOf(item) + 1;
    }

    internal bool GetIsSelected(string item)
    {
        return _selectedIndex > -1 && _searchItems.IndexOf(item) == _selectedIndex;
    }

    [JSInvokable("CloseCallout")]
    public void CloseCalloutBeforeAnotherCalloutIsOpened()
    {
        if (IsEnabled is false) return;

        isOpen = false;
        StateHasChanged();
    }

    protected override bool TryParseValueFromString(string? value, out string? result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        result = value;
        validationErrorMessage = null;
        return true;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            OnValueChanged -= HandleOnValueChanged;
            _dotnetObj.Dispose();
            _cancellationTokenSource.Dispose();
        }

        base.Dispose(disposing);
    }
}
