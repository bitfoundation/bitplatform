using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

public partial class BitSearchBox : BitInputBase<string?>
{
    private bool isOpen;
    private bool hideIcon;
    private bool fixedIcon;
    private bool isUnderlined;
    private bool inputHasFocus;
    private bool showSearchButton;
    private bool disableAnimation;

    private int _selectedIndex = -1;
    private string _inputId = string.Empty;
    private List<string> _searchItems = [];
    private string _calloutId = string.Empty;
    private string _scrollContainerId = string.Empty;
    private CancellationTokenSource _cancellationTokenSource = new();
    private DotNetObjectReference<BitSearchBox> _dotnetObj = default!;

    private bool _inputHasFocus
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
    /// Whether or not the icon is visible.
    /// </summary>
    [Parameter]
    public bool HideIcon
    {
        get => hideIcon;
        set
        {
            if (hideIcon == value) return;

            hideIcon = value;
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
    /// Custom search function to be used in place of the default search algorithm.
    /// </summary>
    [Parameter] public Func<string?, string?, bool>? SuggestFilterFunction { get; set; }

    /// <summary>
    /// The delay, in milliseconds, applied to the search functionality.
    /// </summary>
    [Parameter] public int SuggestThrottleTime { get; set; } = 400;

    /// <summary>
    /// The maximum number of items or suggestions that will be displayed.
    /// </summary>
    [Parameter] public int MaxSuggestCount { get; set; } = 5;

    /// <summary>
    /// The minimum character requirement for doing a search in suggest items.
    /// </summary>
    [Parameter] public int MinSuggestTriggerChars { get; set; } = 3;

    /// <summary>
    /// Custom icon name for the search button.
    /// </summary>
    [Parameter] public string SearchButtonIconName { get; set; } = "ChromeBackMirrored";

    /// <summary>
    /// Whether to show the search button.
    /// </summary>
    [Parameter]
    public bool ShowSearchButton
    {
        get => showSearchButton;
        set
        {
            if (showSearchButton == value) return;

            showSearchButton = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Whether to hide the clear button when the BitSearchBox has value.
    /// </summary>
    [Parameter] public bool HideClearButton { get; set; }



    protected override string RootElementClass => "bit-srb";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => CurrentValue.HasValue() ? $"{RootElementClass}-{(FixedIcon ? "fic-" : string.Empty)}hvl" : string.Empty);

        ClassBuilder.Register(() => DisableAnimation ? $"{RootElementClass}-nan" : string.Empty);

        ClassBuilder.Register(() => IsUnderlined ? $"{RootElementClass}-und" : string.Empty);

        ClassBuilder.Register(() => _inputHasFocus ? $"{RootElementClass}-{(FixedIcon ? "fic-" : string.Empty)}foc" : string.Empty);

        ClassBuilder.Register(() => ShowSearchButton ? $"{RootElementClass}-ssb" : string.Empty);

        ClassBuilder.Register(() => HideIcon ? $"{RootElementClass}-hic" : string.Empty);
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

        if (CurrentValue.HasNoValue() && DefaultValue.HasValue())
        {
            SetCurrentValueAsStringAsync(DefaultValue);
        }

        OnValueChanged += HandleOnValueChanged;

        _dotnetObj = DotNetObjectReference.Create(this);

        return base.OnInitializedAsync();
    }

    private void HandleOnValueChanged(object? sender, EventArgs args) => ClassBuilder.Reset();

    private void HandleInputFocusIn() => _inputHasFocus = true;

    private void HandleInputFocusOut() => _inputHasFocus = false;

    private async Task HandleOnSearchButtonClick()
    {
        if (IsEnabled is false) return;

        await OnSearch.InvokeAsync(CurrentValue);

        await CloseCallout();
    }

    private async Task HandleOnClearButtonClick()
    {
        if (IsEnabled is false) return;

        await HandleOnChange(new() { Value = string.Empty });

        await InputElement.FocusAsync();

        await OnClear.InvokeAsync();
    }

    private async Task HandleOnChange(ChangeEventArgs e)
    {
        if (IsEnabled is false) return;
        if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;

        CurrentValue = e.Value?.ToString();

        ThrottleSearch();
    }

    private async Task HandleOnKeyDown(KeyboardEventArgs eventArgs)
    {
        if (IsEnabled is false) return;
        if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;

        if (eventArgs.Key == "Escape")
        {
            CurrentValue = string.Empty;
            //await InputElement.FocusAsync(); // is it required when the keydown event is captured on the input itself?
            await OnEscape.InvokeAsync();
            await OnClear.InvokeAsync();
            await CloseCallout();
        }
        else if (eventArgs.Key == "Enter")
        {
            CurrentValue = await _js.GetProperty(InputElement, "value");
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

        isOpen = false;
        await ToggleCallout();

        StateHasChanged();
    }

    private async Task SearchItems()
    {
        if (SuggestItemProvider is not null)
        {
            _searchItems = [.. (await SuggestItemProvider.Invoke(CurrentValue, MaxSuggestCount)).Distinct().Take(MaxSuggestCount)];
        }
        else
        {
            _searchItems = CurrentValue.HasNoValue() || CurrentValue!.Length < MinSuggestTriggerChars
                ? []
                : SuggestItems?.Where(i => SuggestFilterFunction is not null ?
                                    SuggestFilterFunction.Invoke(CurrentValue, i) :
                                    (i?.Contains(CurrentValue!, StringComparison.OrdinalIgnoreCase) ?? false))
                        .Distinct().Take(MaxSuggestCount).ToList() ?? [];

        }

        await ChangeStateCallout();
    }

    private void ThrottleSearch()
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
        _cancellationTokenSource = new();

        Task.Run(async () =>
        {
            await Task.Delay(SuggestThrottleTime, _cancellationTokenSource.Token);
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
            _selectedIndex = _searchItems.FindIndex(i => i == CurrentValue);

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

        CurrentValue = _searchItems[_selectedIndex];

        await _js.BitSearchBoxMoveCursorToEnd(InputElement);
    }

    private async Task HandleOnItemClick(string item)
    {
        if (IsEnabled is false) return;
        if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;

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

    [JSInvokable("CloseCallout")]
    public void CloseCalloutBeforeAnotherCalloutIsOpened()
    {
        if (IsEnabled is false) return;

        isOpen = false;
        StateHasChanged();
    }

    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out string? result, [NotNullWhen(false)] out string? parsingErrorMessage)
    {
        result = value;
        parsingErrorMessage = null;
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
