using System.Timers;

namespace Bit.BlazorUI;

/// <summary>
/// Swipers (touch slider) let people show their slides in a swiping row.
/// </summary>
public partial class BitSwiper : BitComponentBase
{
    //private int _pagesCount;
    //private int _currentPage;

    private double _lastX;
    private double _lastDiffX;
    private double _translateX;
    private double _rootWidth;
    private bool _isPointerDown;
    private double _pointerDownX;
    private long _pointerDownTime;
    private double _effectiveWidth;
    private double _containerWidth;
    private int _internalScrollItemsCount = 1;
    private string _directionStyle = string.Empty;
    private string _leftButtonStyle = string.Empty;
    private string _rightButtonStyle = string.Empty;
    private readonly List<BitSwiperItem> _allItems = [];
    private ElementReference _swiperContainer = default!;
    private System.Timers.Timer _autoPlayTimer = default!;
    private DotNetObjectReference<BitSwiper> _dotnetObj = default!;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Sets the duration of the scrolling animation in seconds (the default value is 0.5).
    /// </summary>
    [Parameter] public double AnimationDuration { get; set; } = 0.5;

    ///// <summary>
    ///// Enables/disables the auto scrolling of the slides.
    ///// </summary>
    //[Parameter] public bool AutoPlay { get; set; }

    ///// <summary>
    ///// Sets the interval of the auto scrolling in milliseconds (the default value is 2000).
    ///// </summary>
    //[Parameter] public double AutoPlayInterval { get; set; } = 2000;

    /// <summary>
    /// Items of the swiper.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    ///// <summary>
    ///// Hides the Dots indicator at the bottom of the BitSwiper.
    ///// </summary>
    //[Parameter] public bool HideDots { get; set; }

    /// <summary>
    /// Hides the Next/Prev buttons of the BitSwiper.
    /// </summary>
    [Parameter] public bool HideNextPrev { get; set; }

    ///// <summary>
    ///// If enabled the swiper items will navigate in an infinite loop.
    ///// </summary>
    //[Parameter] public bool InfiniteScrolling { get; set; }

    /// <summary>
    /// Number of items that is going to be changed on navigation.
    /// </summary>
    [Parameter] public int ScrollItemsCount { get; set; } = 1;



    /// <summary>
    /// Navigates to the next swiper item.
    /// </summary>
    public async Task GoNext() => await Go(true);

    /// <summary>
    /// Navigates to the previous swiper item.
    /// </summary>
    public async Task GoPrev() => await Go(false);

    /// <summary>
    /// Navigates to the given swiper item index.
    /// </summary>
    public async Task GoTo(int index) => await GotoPage(index - 1);



    [JSInvokable("OnResize")]
    public async Task _OnResize(ContentRect _)
    {
        if (IsDisposed) return;

        await GetDimensions();
        await Swipe(0);
    }

    [JSInvokable("OnPointerLeave")]
    public async Task _OnPointerLeave(double clientX)
    {
        await HandlePointerLeave(clientX);
    }



    internal void RegisterItem(BitSwiperItem item)
    {
        item.Index = _allItems.Count;

        _allItems.Add(item);
    }

    internal void UnregisterItem(BitSwiperItem item) => _allItems.Remove(item);



    protected override string RootElementClass => "bit-swp";

    protected override async Task OnParametersSetAsync()
    {
        _directionStyle = Dir == BitDir.Rtl ? "direction:rtl" : string.Empty;

        await base.OnParametersSetAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (IsDisposed) return;

        await GetDimensions();

        var itemsCount = _allItems.Count;
        _internalScrollItemsCount = ScrollItemsCount < 1 ? 1
                                  : ScrollItemsCount > itemsCount ? itemsCount
                                  : ScrollItemsCount;

        if (firstRender)
        {
            _dotnetObj = DotNetObjectReference.Create(this);

            //if (AutoPlay)
            //{
            //    _autoPlayTimer = new System.Timers.Timer(AutoPlayInterval);
            //    _autoPlayTimer.Elapsed += AutoPlayTimerElapsed;
            //    _autoPlayTimer.Start();
            //}

            await _js.BitObserversRegisterResize(_Id, RootElement, _dotnetObj);

            await _js.BitSwiperSetup(_Id, RootElement, _dotnetObj);

            SetNavigationButtonsVisibility(_translateX);
        }

        await base.OnAfterRenderAsync(firstRender);
    }



    private async void AutoPlayTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        await InvokeAsync(async () => await Go(true));
    }

    private void SetNavigationButtonsVisibility(double translateX)
    {
        _rightButtonStyle = (/*InfiniteScrolling is false && */translateX == (Dir == BitDir.Rtl ? 0 : -_effectiveWidth)) ? "display:none" : string.Empty;
        _leftButtonStyle = (/*InfiniteScrolling is false && */translateX == (Dir == BitDir.Rtl ? _effectiveWidth : 0)) ? "display:none" : string.Empty;

        StateHasChanged();
    }

    private async Task Go(bool isNext)
    {
        if (IsDisposed) return;

        await GetDimensions();
        await _js.BitUtilsSetStyle(_swiperContainer, "transitionDuration", string.Empty);

        var sign = isNext ? -1 : 1;
        var scrollX = _containerWidth / _allItems.Count * _internalScrollItemsCount;
        var passedSlidesX = (int)(_translateX / scrollX);
        var x = sign * scrollX + passedSlidesX * scrollX;

        await Swipe(x);
    }

    private async Task GotoPage(int index)
    {

    }

    private async Task HandlePointerMove(MouseEventArgs e)
    {
        if (IsDisposed || _isPointerDown is false) return;

        var clientX = e.ClientX;
        var newDiffX = _lastX - clientX;

        if (newDiffX / Math.Abs(newDiffX) != _lastDiffX / Math.Abs(_lastDiffX))
        {
            _pointerDownTime = DateTime.Now.Ticks;
        }

        _lastDiffX = newDiffX;
        _lastX = clientX;

        var delta = clientX - _pointerDownX;
        var x = _translateX + delta;
        await Swipe(x);
    }

    private async Task HandlePointerDown(MouseEventArgs e)
    {
        if (IsDisposed) return;

        _isPointerDown = true;
        _pointerDownX = e.ClientX;
        _pointerDownTime = DateTime.Now.Ticks;

        await GetDimensions();

        await _js.BitUtilsSetStyle(_swiperContainer, "cursor", "grabbing");
        await _js.BitUtilsSetStyle(_swiperContainer, "transitionDuration", string.Empty);
    }

    private async Task HandlePointerUp(MouseEventArgs e)
    {
        if (IsDisposed) return;

        await HandlePointerLeave(e.ClientX);
    }

    public async Task HandlePointerLeave(double clientX)
    {
        if (IsDisposed || _isPointerDown is false) return;

        _isPointerDown = false;
        await _js.BitUtilsSetStyle(_swiperContainer, "cursor", string.Empty);

        var time = (DateTime.Now.Ticks - _pointerDownTime) / 10_000;
        var distance = Math.Abs(clientX - _pointerDownX);

        await GetDimensions();

        var swipeSpeed = distance / time;

        var transitionTime = swipeSpeed > 2 ? 300 : swipeSpeed > 1 ? 600 : 1000;
        await _js.BitUtilsSetStyle(_swiperContainer, "transitionDuration", FormattableString.Invariant($"{transitionTime}ms"));

        var x = -(_lastDiffX / Math.Abs(_lastDiffX)) * (_effectiveWidth * swipeSpeed / 10) + _translateX;
        await Swipe(x);
    }

    private async Task Swipe(double x)
    {
        if (IsDisposed || IsEnabled is false || _rootWidth > _containerWidth) return;

        if (Dir == BitDir.Rtl)
        {
            if (x < 0) x = 0;
            if (x > _effectiveWidth) x = _effectiveWidth;
        }
        else
        {
            if (x > 0) x = 0;
            if (x < -_effectiveWidth) x = -_effectiveWidth;
        }

        await _js.BitUtilsSetStyle(_swiperContainer, "transform", FormattableString.Invariant($"translateX({x}px)"));

        SetNavigationButtonsVisibility(x);
    }

    private async Task GetDimensions()
    {
        if (IsDisposed) return;

        var dimensions = await _js.BitSwiperGetDimensions(RootElement, _swiperContainer);
        _rootWidth = dimensions?.RootWidth ?? 0;
        _containerWidth = dimensions?.ContainerWidth ?? 0;
        _effectiveWidth = dimensions?.EffectiveWidth ?? 0;
        _translateX = dimensions?.TranslateX ?? 0;
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        await base.DisposeAsync(disposing);

        if (_autoPlayTimer is not null)
        {
            _autoPlayTimer.Elapsed -= AutoPlayTimerElapsed;
            _autoPlayTimer.Dispose();
        }

        if (_dotnetObj is not null)
        {
            //_dotnetObj.Dispose(); // it is getting disposed in the following js call:
            try
            {
                await _js.BitSwiperDispose(_Id);
                await _js.BitObserversUnregisterResize(UniqueId, RootElement, _dotnetObj);
            }
            catch (JSDisconnectedException) { } // we can ignore this exception here
        }
    }
}
