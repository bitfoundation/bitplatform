using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Bit.Bheadless;

/// <summary>
/// A headless carousel (WAI-ARIA Carousel pattern): slides shown one (or <see cref="SlidesPerView"/>) at a time, previous /
/// next buttons, slide indicators, swiping, and optional automatic rotation with the rotation control the APG asks for.
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>ARIA</b> — the root is a region with <c>aria-roledescription="carousel"</c> (name it with <c>aria-label</c>); slides
///     are groups with <c>aria-roledescription="slide"</c> named "2 of 5"; slides out of view are <c>inert</c> and hidden from
///     assistive technology; the slides announce changes politely unless rotating.</item>
///   <item><b>Autoplay</b> — <see cref="Autoplay"/> rotates every <see cref="AutoplayInterval"/> ms, pauses while hovered, stops for good
///     when keyboard focus enters the carousel (APG) and is started / stopped by a <see cref="BitBhCarouselAutoplayToggle"/>.</item>
///   <item><b>Swipe</b> — a pointer drag on the <see cref="BitBhCarouselSlides"/> moves to the next / previous slide; while dragging,
///     <c>--bit-bh-carousel-drag</c> (px) follows the pointer.</item>
///   <item><b>Styling</b> — the root exposes <c>--bit-bh-carousel-index</c>, <c>--bit-bh-carousel-count</c> and
///     <c>--bit-bh-carousel-slides-per-view</c>: <c>transform: translateX(calc(var(--bit-bh-carousel-index) * -100% / var(--bit-bh-carousel-slides-per-view)))</c>
///     on the track is a carousel.</item>
/// </list>
///
/// <code>
/// &lt;BitBhCarousel aria-label="Featured products" Loop="true" class="carousel"&gt;
///     &lt;BitBhCarouselSlides class="track"&gt;
///         &lt;BitBhCarouselSlide&gt;…&lt;/BitBhCarouselSlide&gt;
///         &lt;BitBhCarouselSlide&gt;…&lt;/BitBhCarouselSlide&gt;
///     &lt;/BitBhCarouselSlides&gt;
///     &lt;BitBhCarouselPrevious&gt;‹&lt;/BitBhCarouselPrevious&gt;
///     &lt;BitBhCarouselNext&gt;›&lt;/BitBhCarouselNext&gt;
/// &lt;/BitBhCarousel&gt;
/// </code>
/// </summary>
public class BitBhCarousel : BitBhComponentBase, IAsyncDisposable
{
    // Document order, not registration order: a slide added before the others later still gets its place.
    private readonly BitBhCollection<BitBhCarouselSlide> _slides = new(BitBhId.New("bit-bh-slides"));
    private int _index;
    private bool _playing;
    private bool _autoplay;
    private bool _hovered;
    private bool _disposed;
    private BitBhDelayedAction? _timer;
    private BitBhCarouselContext? _context;

    [CascadingParameter]
    private BitBhDirection CascadedDir { get; set; }

    /// <summary>The first slide in view (0-based). Supplying it makes the carousel controlled (use <c>@bind-Index</c>).</summary>
    [Parameter]
    public int Index { get; set; }

    /// <summary>Two-way binding callback for <see cref="Index"/>.</summary>
    [Parameter]
    public EventCallback<int> IndexChanged { get; set; }

    /// <summary>Initial slide when uncontrolled.</summary>
    [Parameter]
    public int DefaultIndex { get; set; }

    /// <summary>Fires whenever the slide changes.</summary>
    [Parameter]
    public EventCallback<int> OnIndexChange { get; set; }

    /// <summary>Goes from the last slide to the first (and back). Default: false.</summary>
    [Parameter]
    public bool Loop { get; set; }

    /// <summary>Slides in view at once. Default: 1.</summary>
    [Parameter]
    public int SlidesPerView { get; set; } = 1;

    /// <summary>Slides moved by the previous / next buttons and swipes. Default: 1.</summary>
    [Parameter]
    public int SlidesPerMove { get; set; } = 1;

    /// <summary>Layout direction (<c>data-orientation</c>; a vertical carousel swipes up / down). Default: horizontal.</summary>
    [Parameter]
    public BitBhOrientation Orientation { get; set; }

    /// <summary>Rotates the slides automatically. Default: false.</summary>
    [Parameter]
    public bool Autoplay { get; set; }

    /// <summary>Time each slide is shown while rotating (ms). Default: 5000.</summary>
    [Parameter]
    public int AutoplayInterval { get; set; } = 5000;

    /// <summary>Reading direction (mirrors swipes). Inherits from <see cref="BitBhDirectionProvider"/>.</summary>
    [Parameter]
    public BitBhDirection? Dir { get; set; }

    /// <summary>The parts.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>The parts, rendered with the carousel's state.</summary>
    [Parameter]
    public RenderFragment<BitBhCarouselContext>? Template { get; set; }

    protected override string DefaultTag => "section";

    /// <summary>The number of slides.</summary>
    public int Count => _slides.Count;

    /// <summary>The first slide in view.</summary>
    public int CurrentIndex => Math.Clamp(IsProvided(nameof(Index)) ? Index : _index, 0, MaxIndex);

    /// <summary>Whether the carousel is rotating.</summary>
    public bool IsPlaying => _playing;

    internal int MaxIndex => Math.Max(0, _slides.Count - Math.Max(1, SlidesPerView));
    internal bool CanGoPrevious => Loop ? Count > 1 : CurrentIndex > 0;
    internal bool CanGoNext => Loop ? Count > 1 : CurrentIndex < MaxIndex;
    internal string SlidesId => $"{ComponentId}-slides";
    internal BitBhDirection EffectiveDir => Dir ?? CascadedDir;

    protected override void OnInitialized()
    {
        _index = DefaultIndex;
        _playing = _autoplay = Autoplay;
    }

    protected override void OnParametersSet()
    {
        // A consumer that turns Autoplay on or off starts or stops the rotation; a re-render with the same value
        // leaves what the user chose with the rotation control alone.
        if (_autoplay == Autoplay) return;
        _autoplay = Autoplay;
        SetPlaying(Autoplay);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && _playing) Schedule();
        var version = _slides.Version;
        await _slides.EnsureOrderAsync(Interop);
        if (_slides.Version != version && !_disposed) StateHasChanged();
    }

    internal bool IsInView(int index) => index >= CurrentIndex && index < CurrentIndex + Math.Max(1, SlidesPerView);

    internal string SlideKey => _slides.Key;

    internal void Register(BitBhCarouselSlide slide)
    {
        var count = _slides.Count;
        _slides.Register(slide);
        if (_slides.Count == count) return;
        StateHasChanged();
        // Slides that arrive after the first render (loaded data) still start the rotation.
        if (_playing) Schedule();
    }

    internal void Unregister(BitBhCarouselSlide slide)
    {
        var count = _slides.Count;
        _slides.Unregister(slide);
        if (_slides.Count != count && !_disposed) StateHasChanged();
    }

    internal int IndexOf(BitBhCarouselSlide slide) => _slides.IndexOf(slide);

    /// <summary>Shows slide <paramref name="index"/> (wrapped with <see cref="Loop"/>, clamped otherwise).</summary>
    public async Task GoToAsync(int index)
    {
        if (Count == 0) return;
        var max = MaxIndex;
        index = Loop ? ((index % (max + 1)) + max + 1) % (max + 1) : Math.Clamp(index, 0, max);
        if (index == CurrentIndex) return;
        if (!IsProvided(nameof(Index))) _index = index;
        await IndexChanged.InvokeAsync(index);
        await OnIndexChange.InvokeAsync(index);
        StateHasChanged();
        if (_playing) Schedule();
    }

    /// <summary>Shows the next slides.</summary>
    public Task NextAsync()
    {
        var step = Math.Max(1, SlidesPerMove);
        // Looping from the end goes back to the start, not to a partial view.
        if (Loop && CurrentIndex + step > MaxIndex) return GoToAsync(CurrentIndex >= MaxIndex ? 0 : MaxIndex);
        return GoToAsync(CurrentIndex + step);
    }

    /// <summary>Shows the previous slides.</summary>
    public Task PreviousAsync()
    {
        var step = Math.Max(1, SlidesPerMove);
        if (Loop && CurrentIndex - step < 0) return GoToAsync(CurrentIndex <= 0 ? MaxIndex : 0);
        return GoToAsync(CurrentIndex - step);
    }

    /// <summary>Starts or stops the automatic rotation.</summary>
    public void SetPlaying(bool playing)
    {
        if (_playing == playing) return;
        _playing = playing;
        if (playing) Schedule();
        else StopTimer();
        StateHasChanged();
    }

    private void Schedule()
    {
        if (_disposed || !_playing || _hovered || Count < 2) return;
        _timer ??= new BitBhDelayedAction(a => InvokeAsync(a), GetService<TimeProvider>());
        _timer.Schedule(Math.Max(500, AutoplayInterval), async () =>
        {
            if (!CanGoNext && !Loop) await GoToAsync(0);
            else await NextAsync();
            Schedule();
        });
    }

    private void StopTimer() => _timer?.Cancel();

    private void SetHovered(bool hovered)
    {
        _hovered = hovered;
        if (hovered) StopTimer();
        else Schedule();
    }

    // APG: rotation stops for good when keyboard focus enters the carousel; the rotation control restarts it.
    private void HandleFocusIn() => SetPlaying(false);

    internal Task HandleSwipeAsync(int direction) => direction > 0 ? NextAsync() : PreviousAsync();

    // On the renderer, with exceptions surfacing like those of the other runtime callbacks.
    internal Task SwipeFromRuntimeAsync(int direction) => RunFromRuntimeAsync(() => HandleSwipeAsync(direction));

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        _context ??= new BitBhCarouselContext(this);
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "onpointerenter", On<PointerEventArgs>("onpointerenter", e => { if (e.PointerType != "touch") SetHovered(true); }));
        builder.AddAttribute(31, "onpointerleave", On<PointerEventArgs>("onpointerleave", () => SetHovered(false)));
        builder.AddAttribute(32, "onfocusin", On<FocusEventArgs>("onfocusin", HandleFocusIn));
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.OpenComponent<CascadingValue<BitBhCarouselContext>>(50);
        builder.AddComponentParameter(51, "Value", _context);
        builder.AddComponentParameter(52, "IsFixed", false);
        builder.AddComponentParameter(53, "ChildContent", BitBhContent.Pick(Template, ChildContent, _context));
        builder.CloseComponent();
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (!Tag.Equals("section", StringComparison.OrdinalIgnoreCase)) attrs["role"] = "region";
        attrs["aria-roledescription"] = "carousel";
        if (Dir is { } dir) attrs["dir"] = dir.ToAttribute();
        string I(int n) => n.ToString(CultureInfo.InvariantCulture);
        attrs["style"] = $"--bit-bh-carousel-index:{I(CurrentIndex)};--bit-bh-carousel-count:{I(Count)};--bit-bh-carousel-slides-per-view:{I(Math.Max(1, SlidesPerView))}";
        SetDataValue(attrs, "orientation", Orientation.ToAttribute());
        SetDataValue(attrs, "index", I(CurrentIndex));
        SetDataFlag(attrs, "playing", _playing);
        return attrs;
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        _timer?.Dispose();
        await Task.CompletedTask;
        GC.SuppressFinalize(this);
    }
}
