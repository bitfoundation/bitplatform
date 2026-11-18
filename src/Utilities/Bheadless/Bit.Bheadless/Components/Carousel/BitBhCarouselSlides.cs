using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace Bit.Bheadless;

/// <summary>
/// The container of a <see cref="BitBhCarousel"/>'s slides (the track): announces slide changes politely unless the carousel
/// is rotating, and takes swipes (a pointer drag sets <c>data-dragging</c> and <c>--bit-bh-carousel-drag</c> px).
/// </summary>
public class BitBhCarouselSlides : BitBhComponentBase, IAsyncDisposable
{
    private DotNetObjectReference<Callbacks>? _ref;
    private int _handle;
    private bool _attaching;
    private bool _disposed;

    [CascadingParameter]
    private BitBhCarouselContext? Context { get; set; }

    /// <summary>The slides.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Lets a pointer drag change the slide. Default: true.</summary>
    [Parameter]
    public bool Swipe { get; set; } = true;

    protected override string DefaultTag => "div";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", Context?.Owner.SlidesId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.AddContent(40, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Context?.Owner is not { } owner) return attrs;
        attrs["aria-live"] = owner.IsPlaying ? "off" : "polite";
        attrs["aria-atomic"] = "false";
        SetDataValue(attrs, "orientation", owner.Orientation.ToAttribute());
        if (Swipe) attrs["style"] = owner.Orientation == BitBhOrientation.Vertical ? "touch-action:pan-x" : "touch-action:pan-y";
        return attrs;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || !Swipe || Context?.Owner is not { } owner) return;
        _attaching = true;
        _ref = DotNetObjectReference.Create(new Callbacks(owner));
        _handle = await Interop.CarouselAttachAsync(_ref, owner.SlidesId, owner.Orientation == BitBhOrientation.Vertical);
        _attaching = false;
        if (_disposed) await Interop.CarouselDetachAsync(_handle);
    }

    private sealed class Callbacks(BitBhCarousel owner)
    {
        /// <summary>The runtime calls it by this name: +1 for the next slides, -1 for the previous ones.</summary>
        [JSInvokable]
        public Task OnCarouselSwipe(int direction) => owner.SwipeFromRuntimeAsync(direction);
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        if (!_attaching && _handle > 0) await Interop.CarouselDetachAsync(_handle);
        _ref?.Dispose();
        GC.SuppressFinalize(this);
    }
}
