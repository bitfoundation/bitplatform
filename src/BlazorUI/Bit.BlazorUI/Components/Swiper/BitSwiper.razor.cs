using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.AspNetCore.Components;

namespace Bit.BlazorUI;

public partial class BitSwiper : IDisposable
{
    private double _lastX;
    private bool _disposed;
    private int _pagesCount;
    private int _currentPage;
    private double _lastDiffX;
    private double _translateX;
    private double _rootWidth;
    private double _swiperWidth;
    private bool _isPointerDown;
    private double _pointerDownX;
    private long _pointerDownTime;
    private double _swiperEffectiveWidth;
    private int _internalScrollItemsCount = 1;
    private string _directionStyle = string.Empty;
    private string _leftButtonStyle = string.Empty;
    private string _rightButtonStyle = string.Empty;

    private ElementReference _swiper = default!;
    private System.Timers.Timer _autoPlayTimer = default!;

    private readonly List<BitSwiperItem> AllItems = new();

    [Inject] private IJSRuntime _js { get; set; } = default!;

    /// <summary>
    /// If enabled the swiper items will navigate in an infinite loop.
    /// </summary>
    //[Parameter] public bool InfiniteScrolling { get; set; }

    /// <summary>
    /// Items of the carousel.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Shows or hides the Dots indicator at the bottom of the BitCarousel.
    /// </summary>
    //[Parameter] public bool ShowDots { get; set; } = true;

    /// <summary>
    /// Shows or hides the Next/Prev buttons of the BitCarousel.
    /// </summary>
    [Parameter] public bool ShowNextPrev { get; set; } = true;

    /// <summary>
    /// Number of items that is going to be changed on navigation
    /// </summary>
    [Parameter] public int ScrollItemsCount { get; set; } = 1;

    /// <summary>
    /// Enables/disables the auto scrolling of the slides.
    /// </summary>
    //[Parameter] public bool AutoPlay { get; set; }

    /// <summary>
    /// Sets the interval of the auto scrolling in milliseconds (the default value is 2000).
    /// </summary>
    //[Parameter] public double AutoPlayInterval { get; set; } = 2000;

    /// <summary>
    /// Sets the duration of the scrolling animation in seconds (the default value is 0.5).
    /// </summary>
    [Parameter] public double AnimationDuration { get; set; } = 0.5;

    /// <summary>
    /// Sets the direction of the scrolling (the default value is LeftToRight).
    /// </summary>
    [Parameter] public BitDirection Direction { get; set; } = BitDirection.LeftToRight;

    public async Task GoPrev() => await Go(false);

    public async Task GoNext() => await Go(true);

    public async Task GoTo(int index)
    {
        await GotoPage(index - 1);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }


    internal void RegisterItem(BitSwiperItem item)
    {
        item.Index = AllItems.Count;

        AllItems.Add(item);
    }

    internal void UnregisterItem(BitSwiperItem carouselItem)
    {
        AllItems.Remove(carouselItem);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing && _autoPlayTimer is not null)
        {
            _autoPlayTimer.Elapsed -= AutoPlayTimerElapsed;
            _autoPlayTimer.Dispose();
            _disposed = true;
        }
    }

    protected override string RootElementClass => "bit-swp";

    protected override async Task OnParametersSetAsync()
    {
        _directionStyle = Direction == BitDirection.RightToLeft ? "direction:rtl" : "";

        var itemsCount = AllItems.Count;

        _internalScrollItemsCount = ScrollItemsCount < 0 ? 0
                                    : ScrollItemsCount > itemsCount ? itemsCount : ScrollItemsCount;

        await base.OnParametersSetAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await GetDimensions();

        if (firstRender)
        {
            //if (AutoPlay)
            //{
            //    _autoPlayTimer = new System.Timers.Timer(AutoPlayInterval);
            //    _autoPlayTimer.Elapsed += AutoPlayTimerElapsed;
            //    _autoPlayTimer.Start();
            //}

            await _js.RegisterPointerLeave(RootElement, DotNetObjectReference.Create(this));

            SetNavigationButtonsVisibility(_translateX);
        }

        await base.OnAfterRenderAsync(firstRender);
    }


    private async void AutoPlayTimerElapsed(object? sender, ElapsedEventArgs e) => await InvokeAsync(async () => await Go(true));

    private void SetNavigationButtonsVisibility(double translateX)
    {
        _rightButtonStyle = (/*InfiniteScrolling is false && */translateX == (Direction == BitDirection.LeftToRight ? -_swiperEffectiveWidth : 0)) ? "display:none" : "";
        _leftButtonStyle = (/*InfiniteScrolling is false && */translateX == (Direction == BitDirection.LeftToRight ? 0 : _swiperEffectiveWidth)) ? "display:none" : "";

        StateHasChanged();
    }

    private async Task Go(bool isNext)
    {
        await GetDimensions();
        await _js.SetStyle(_swiper, "transitionDuration", "");

        var sign = isNext ? -1 : 1;
        var scrollX = _swiperWidth / AllItems.Count * _internalScrollItemsCount;
        var passedSlidesX = (int)(_translateX / scrollX);
        var x = sign * scrollX + passedSlidesX * scrollX;

        await Swipe(x);
    }

    private async Task GotoPage(int index)
    {

    }

    private async Task HandlePointerMove(MouseEventArgs e)
    {
        if (_isPointerDown is false) return;

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
        _isPointerDown = true;
        _pointerDownX = e.ClientX;
        _pointerDownTime = DateTime.Now.Ticks;

        await GetDimensions();

        await _js.SetStyle(_swiper, "cursor", "grabbing");
        await _js.SetStyle(_swiper, "transitionDuration", "");
    }

    private async Task HandlePointerUp(MouseEventArgs e)
    {
        await HandlePointerLeave(e.ClientX);
    }

    [JSInvokable]
    public async Task HandlePointerLeave(double clientX)
    {
        if (_isPointerDown is false) return;

        _isPointerDown = false;
        await _js.SetStyle(_swiper, "cursor", "");

        var time = (DateTime.Now.Ticks - _pointerDownTime) / 10_000;
        var distance = Math.Abs(clientX - _pointerDownX);

        await GetDimensions();

        var swipeSpeed = distance / time;

        var transitionTime = swipeSpeed > 2 ? 300 : swipeSpeed > 1 ? 600 : 1000;
        await _js.SetStyle(_swiper, "transitionDuration", $"{transitionTime}ms");

        var x = -(_lastDiffX / Math.Abs(_lastDiffX)) * (_swiperEffectiveWidth * swipeSpeed / 10) + _translateX;
        await Swipe(x);
    }

    private async Task Swipe(double x)
    {
        if (_rootWidth > _swiperWidth || IsEnabled is false) return;

        if (Direction == BitDirection.LeftToRight)
        {
            if (x > 0) x = 0;
            if (x < -_swiperEffectiveWidth) x = -_swiperEffectiveWidth;
        }
        else
        {
            if (x < 0) x = 0;
            if (x > _swiperEffectiveWidth) x = _swiperEffectiveWidth;
        }

        await _js.SetStyle(_swiper, "transform", $"translateX({x}px)");

        SetNavigationButtonsVisibility(x);
    }

    private async Task GetDimensions()
    {
        var dimensions = await _js.GetDimensions(RootElement, _swiper);
        _rootWidth = dimensions?.RootWidth ?? 0;
        _swiperWidth = dimensions?.SwiperWidth ?? 0;
        _swiperEffectiveWidth = dimensions?.EffectiveSwiperWidth ?? 0;
        _translateX = dimensions?.SwiperTranslateX ?? 0;
    }

}
