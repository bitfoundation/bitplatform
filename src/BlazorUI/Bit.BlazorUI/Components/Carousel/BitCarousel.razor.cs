using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Bit.BlazorUI;

public partial class BitCarousel
{
    private ElementReference _carousel = default!;
    private int[] _currentIndices = Array.Empty<int>();
    private int[] _othersIndices = Array.Empty<int>();
    private int _pagesCount;
    private int _currentPage;

    private readonly List<BitCarouselItem> AllItems = new();

    [Inject] private IJSRuntime _js { get; set; } = default!;

    /// <summary>
    /// If enabled the carousel items will navigate in a loop (first item comes after last item and last item comes before first item).
    /// </summary>
    [Parameter] public bool InfiniteSliding { get; set; }

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


    internal void RegisterItem(BitCarouselItem item)
    {
        item.Index = AllItems.Count;

        AllItems.Add(item);

        StateHasChanged();
    }

    internal void UnregisterItem(BitCarouselItem carouselItem)
    {
        AllItems.Remove(carouselItem);
    }


    protected override string RootElementClass => "bit-crsl";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await _js.PreventDefault(_carousel, "touchmove");

            _currentIndices = Enumerable.Range(0, VisibleItemsCount).ToArray();
            _othersIndices = Enumerable.Range(0, ScrollItemsCount).ToArray();

            var itemsCount = AllItems.Count;
            var rect = await _js.GetBoundingClientRect(_carousel);

            for (int i = 0; i < itemsCount; i++)
            {
                var item = AllItems[i];
                item.InternalStyle = $"width:{rect.Width / VisibleItemsCount}px; display:block";
                item.InternalTransformStyle = $"transform:translateX({100 * i}%)";
            }

            _pagesCount = (int)Math.Ceiling((decimal)itemsCount / VisibleItemsCount);

            StateHasChanged();
        }

        await base.OnAfterRenderAsync(firstRender);
    }


    private async Task Prev()
    {
        _othersIndices = Enumerable.Range(0, ScrollItemsCount).Select(i =>
        {
            var idx = _currentIndices[0] - (i + 1);
            if (idx < 0) idx = AllItems.Count + idx;
            return idx;
        }).Reverse().ToArray();

        await Go();
    }

    private async Task Next()
    {
        _othersIndices = Enumerable.Range(0, ScrollItemsCount).Select(i =>
        {
            var idx = _currentIndices[VisibleItemsCount - 1] + (i + 1);
            if (idx > AllItems.Count - 1) idx = idx - AllItems.Count;
            return idx;
        }).ToArray();

        await Go(true);
    }

    private async Task Go(bool isNext = false, int scrollCount = 0)
    {
        if (InfiniteSliding is false && ((_currentPage == 0 && isNext == false)) || (_currentPage == (_pagesCount -1) && isNext))
            return;

        if (scrollCount < 1)
        {
            scrollCount = ScrollItemsCount;
        }

        var diff = VisibleItemsCount - scrollCount;
        var newIndices = (isNext
            ? (_currentIndices.Skip(VisibleItemsCount - diff).Take(diff)).Concat(_othersIndices)
            : _othersIndices.Concat(_currentIndices.Take(diff))).ToArray();

        var currents = _currentIndices.Select(i => AllItems[i]).ToArray();
        var others = _othersIndices.Select(i => AllItems[i]).ToArray();

        var sign = isNext ? 1 : -1;
        var offset = isNext ? VisibleItemsCount : scrollCount;

        for (int i = 0; i < others.Length; i++)
        {
            var o = others[i];
            o.InternalTransitionStyle = "";
            o.InternalTransformStyle = $"transform:translateX({sign * 100 * (offset + (sign * i))}%)";
        }

        StateHasChanged();

        await Task.Delay(50);

        offset = isNext ? VisibleItemsCount - scrollCount : 0;

        for (int i = 0; i < currents.Length; i++)
        {
            var c = currents[i];
            c.InternalTransitionStyle = $"transition:all 0.5s";
            c.InternalTransformStyle = $"transform:translateX({-sign * 100 * (scrollCount + (-sign * i))}%)";
        }

        for (int i = 0; i < others.Length; i++)
        {
            var o = others[i];
            o.InternalTransitionStyle = $"transition:all 0.5s";
            o.InternalTransformStyle = $"transform:translateX({100 * (offset + i)}%)";
        }

        _currentIndices = newIndices;
        _currentPage = (int)Math.Floor((decimal)(_currentIndices[0]) / VisibleItemsCount);

        StateHasChanged();

    }

    private async Task GotoPage(int index)
    {
        if (index < 0)
        {
            index =  0;
        }
        else if (index >= _pagesCount)
        {
            index = _pagesCount - 1;
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
            _othersIndices = indices.Select(idx =>
            {
                if (idx > AllItems.Count - 1) idx = idx - AllItems.Count;
                return idx;
            }).ToArray();
        }

        await Go(isNext, VisibleItemsCount);
    }


    private double _pointerX;
    private bool _isPointerDown;
    private async Task HandlePointerMove(MouseEventArgs e)
    {
        if (_isPointerDown is false) return;

        var delta = e.ClientX - _pointerX;
        if (Math.Abs(delta) <= 20) return;

        _isPointerDown = false;
        await _js.SetStyle(_carousel, "cursor", "");
        if (delta < 0)
        {
            await Next();
        }
        else
        {
            await Prev();
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
}
