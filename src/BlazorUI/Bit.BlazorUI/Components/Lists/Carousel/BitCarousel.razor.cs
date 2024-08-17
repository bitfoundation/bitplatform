﻿namespace Bit.BlazorUI;

public partial class BitCarousel : BitComponentBase, IAsyncDisposable
{
    private bool _disposed;
    private int _pagesCount;
    private int _currentPage;
    private double _pointerX;
    private bool _isPointerDown;
    private int[] _othersIndices = [];
    private int[] _currentIndices = [];
    private int _internalScrollItemsCount = 1;
    private string _directionStyle = string.Empty;
    private ElementReference _carousel = default!;
    private string _resizeObserverId = string.Empty;
    private string _goLeftButtonStyle = string.Empty;
    private string _goRightButtonStyle = string.Empty;
    private readonly List<BitCarouselItem> _allItems = [];
    private System.Timers.Timer _autoPlayTimer = default!;
    private DotNetObjectReference<BitCarousel> _dotnetObjRef = default!;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// If enabled the carousel items will navigate in an infinite loop (first item comes after last item and last item comes before first item).
    /// </summary>
    [Parameter] public bool InfiniteScrolling { get; set; }

    /// <summary>
    /// Items of the carousel.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Shows or hides the Dots indicator at the bottom of the BitCarousel.
    /// </summary>
    [Parameter] public bool ShowDots { get; set; } = true;

    /// <summary>
    /// Shows or hides the Next/Prev buttons of the BitCarousel.
    /// </summary>
    [Parameter] public bool ShowNextPrev { get; set; } = true;

    /// <summary>
    /// Number of items that is visible in the carousel
    /// </summary>
    [Parameter] public int VisibleItemsCount { get; set; } = 1;

    /// <summary>
    /// Number of items that is going to be changed on navigation
    /// </summary>
    [Parameter] public int ScrollItemsCount { get; set; } = 1;

    /// <summary>
    /// Enables/disables the auto scrolling of the slides.
    /// </summary>
    [Parameter] public bool AutoPlay { get; set; }

    /// <summary>
    /// Sets the interval of the auto scrolling in milliseconds (the default value is 2000).
    /// </summary>
    [Parameter] public double AutoPlayInterval { get; set; } = 2000;

    /// <summary>
    /// Sets the duration of the scrolling animation in seconds (the default value is 0.5).
    /// </summary>
    [Parameter] public double AnimationDuration { get; set; } = 0.5;

    /// <summary>
    /// The event that will be called on carousel page navigation.
    /// </summary>
    [Parameter] public EventCallback<int> OnChange { get; set; }



    public async Task GoPrev()
    {
        await Prev();
    }

    public async Task GoNext()
    {
        await Next();
    }

    public async Task GoTo(int index)
    {
        await GotoPage(index - 1);
    }



    [JSInvokable("OnRootResize")]
    public async Task OnRootResize(ContentRect rect)
    {
        await ResetDimensionsAsync();
    }



    internal void RegisterItem(BitCarouselItem item)
    {
        item.Index = _allItems.Count;

        _allItems.Add(item);

        StateHasChanged();
    }

    internal void UnregisterItem(BitCarouselItem carouselItem)
    {
        _allItems.Remove(carouselItem);
    }



    protected override string RootElementClass => "bit-csl";

    protected override void OnInitialized()
    {
        _dotnetObjRef = DotNetObjectReference.Create(this);

        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        _internalScrollItemsCount = ScrollItemsCount;

        base.OnParametersSet();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        _directionStyle = Dir == BitDir.Rtl ? "direction:rtl" : "";

        await base.OnAfterRenderAsync(firstRender);

        if (firstRender is false) return;

        _resizeObserverId = await _js.BitObserversRegisterResize(RootElement, _dotnetObjRef, "OnRootResize");

        if (AutoPlay)
        {
            _autoPlayTimer = new System.Timers.Timer(AutoPlayInterval);
            _autoPlayTimer.Elapsed += AutoPlayTimerElapsed;
            _autoPlayTimer.Start();
        }

        if (ScrollItemsCount > VisibleItemsCount)
        {
            _internalScrollItemsCount = VisibleItemsCount;
        }

        await ResetDimensionsAsync();
    }



    private async Task ResetDimensionsAsync()
    {
        _currentIndices = Enumerable.Range(0, VisibleItemsCount).ToArray();
        _othersIndices = Enumerable.Range(0, _internalScrollItemsCount).ToArray();

        var itemsCount = _allItems.Count;
        var rect = await _js.GetBoundingClientRect(_carousel);
        if (rect is null) return;
        var sign = Dir == BitDir.Rtl ? -1 : 1;
        for (int i = 0; i < itemsCount; i++)
        {
            var item = _allItems[i];
            item.InternalStyle = FormattableString.Invariant($"width:{(rect.Width / VisibleItemsCount)}px;display:block");
            item.InternalTransformStyle = FormattableString.Invariant($"transform:translateX({sign * 100 * i}%)");
        }

        _pagesCount = (int)Math.Ceiling((decimal)itemsCount / VisibleItemsCount);

        SetNavigationButtonsVisibility();

        StateHasChanged();
    }

    private void SetNavigationButtonsVisibility()
    {
        _goLeftButtonStyle = (InfiniteScrolling is false && _currentIndices[_currentIndices.Length - 1] == _allItems.Count - 1) ? "display:none" : "";

        _goRightButtonStyle = (InfiniteScrolling is false && _currentIndices[0] == 0) ? "display:none" : "";
    }

    private async Task GoLeft() => await (Dir == BitDir.Rtl ? Prev() : Next());

    private async Task GoRight() => await (Dir == BitDir.Rtl ? Next() : Prev());

    private async Task Prev()
    {
        _othersIndices = Enumerable.Range(0, _internalScrollItemsCount).Select(i =>
        {
            var idx = _currentIndices[0] - (i + 1);
            if (InfiniteScrolling && idx < 0) idx += _allItems.Count;
            return idx;
        }).Where(i => i >= 0).Reverse().ToArray();

        await Go();
    }

    private async Task Next()
    {
        var itemsCount = _allItems.Count;
        _othersIndices = Enumerable.Range(0, _internalScrollItemsCount).Select(i =>
        {
            var idx = _currentIndices[_currentIndices.Length - 1] + (i + 1);
            if (InfiniteScrolling && idx > itemsCount - 1) idx -= itemsCount;
            return idx;
        }).Where(i => i < itemsCount).ToArray();

        await Go(true);
    }

    private async Task Go(bool isNext = false, int scrollCount = 0)
    {
        if (_othersIndices.Length == 0) return;

        if (scrollCount < 1)
        {
            scrollCount = _internalScrollItemsCount;
        }

        var diff = VisibleItemsCount - scrollCount;
        var newIndices = (isNext
            ? _currentIndices.Skip(VisibleItemsCount - diff).Take(diff).Concat(_othersIndices)
            : _othersIndices.Concat(_currentIndices.Take(diff))).ToArray();

        var currents = _currentIndices.Select(i => _allItems[i]).ToArray();
        var others = _othersIndices.Select(i => _allItems[i]).ToArray();

        var sign = isNext ? 1 : -1;
        var offset = isNext ? VisibleItemsCount : scrollCount;

        for (int i = 0; i < others.Length; i++)
        {
            var o = others[i];
            o.InternalTransitionStyle = "";
            var x = sign * 100 * (offset + (sign * i));
            x = Dir == BitDir.Rtl ? -x : x;
            o.InternalTransformStyle = FormattableString.Invariant($"transform:translateX({x}%)");
        }

        StateHasChanged();

        await Task.Delay(50);

        offset = isNext ? VisibleItemsCount - scrollCount : 0;

        for (int i = 0; i < currents.Length; i++)
        {
            var c = currents[i];
            c.InternalTransitionStyle = FormattableString.Invariant($"transition:all {AnimationDuration}s");
            var x = -sign * 100 * (scrollCount + (-sign * i));
            x = Dir == BitDir.Rtl ? -x : x;
            c.InternalTransformStyle = FormattableString.Invariant($"transform:translateX({x}%)");
        }

        for (int i = 0; i < others.Length; i++)
        {
            var o = others[i];
            o.InternalTransitionStyle = FormattableString.Invariant($"transition:all {AnimationDuration}s");
            var x = 100 * (offset + i);
            x = Dir == BitDir.Rtl ? -x : x;
            o.InternalTransformStyle = FormattableString.Invariant($"transform:translateX({x}%)");
        }

        _currentIndices = newIndices;
        _currentPage = (int)Math.Floor((decimal)_currentIndices[0] / VisibleItemsCount);
        _ = OnChange.InvokeAsync(_currentPage);

        SetNavigationButtonsVisibility();

        StateHasChanged();
    }

    private async Task GotoPage(int index)
    {
        if (index < 0)
        {
            index = InfiniteScrolling ? _pagesCount - 1 : 0;
        }
        else if (index >= _pagesCount)
        {
            index = InfiniteScrolling ? 0 : _pagesCount - 1;
        }

        if (_currentIndices[0] == index * VisibleItemsCount) return;

        var indices = Enumerable.Range(index * VisibleItemsCount, VisibleItemsCount);
        var isNext = false;
        if (index < _currentPage) // go prev
        {
            _othersIndices = indices.ToArray();
        }
        else // go next
        {
            isNext = true;
            var itemsCount = _allItems.Count;
            _othersIndices = indices.Select(idx =>
            {
                if (InfiniteScrolling && idx > itemsCount - 1) idx -= itemsCount;
                return idx;
            }).Where(i => i < itemsCount).ToArray();
        }

        await Go(isNext, VisibleItemsCount);
    }

    private async Task HandlePointerMove(MouseEventArgs e)
    {
        if (_isPointerDown is false) return;

        var delta = e.ClientX - _pointerX;
        if (Math.Abs(delta) <= 20) return;

        _isPointerDown = false;
        await _js.SetStyle(_carousel, "cursor", "");

        if (delta < 0)
        {
            await GoLeft();
        }
        else
        {
            await GoRight();
        }
    }

    private async Task HandlePointerDown(MouseEventArgs e)
    {
        _isPointerDown = true;
        _pointerX = e.ClientX;
        await _js.SetStyle(_carousel, "cursor", "grabbing");
        StateHasChanged();
    }

    private async Task HandlePointerUp(MouseEventArgs e)
    {
        _isPointerDown = false;
        await _js.SetStyle(_carousel, "cursor", "");
        StateHasChanged();
    }

    private async void AutoPlayTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        await InvokeAsync(Next);
    }



    public async ValueTask DisposeAsync()
    {
        await DisposeAsync(true);
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsync(bool disposing)
    {
        if (_disposed || disposing is false) return;

        if (_autoPlayTimer is not null)
        {
            _autoPlayTimer.Elapsed -= AutoPlayTimerElapsed;
            _autoPlayTimer.Dispose();
        }

        if (_dotnetObjRef is not null)
        {
            //_dotnetObjRef.Dispose(); // it is getting disposed in the following js call:
            try
            {
                await _js.BitObserversUnregisterResize(RootElement, _resizeObserverId, _dotnetObjRef);
            }
            catch (JSDisconnectedException) { } // we can ignore this exception here
        }

        _disposed = true;
    }
}
