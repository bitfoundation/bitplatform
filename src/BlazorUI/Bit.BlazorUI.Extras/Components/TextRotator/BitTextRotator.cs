using Microsoft.AspNetCore.Components.CompilerServices;

namespace Bit.BlazorUI;

/// <summary>
/// BitTextRotator cycles through a list of words or phrases in one place, moving each one out as the next one comes
/// in - the rotating word of a hero heading ("Build apps for web / mobile / desktop") or of a command line example.
/// </summary>
/// <remarks>
/// All the items are laid over one another in the same cell, so the rotator always takes the room of its widest item
/// and the text around it never moves as the items change. The first item is shown as rendered, also when the
/// component is prerendered or rendered statically, and the rotation starts once the component is interactive: one
/// item every <see cref="Interval"/>, with an entrance and an exit drawn by the stylesheet through the public
/// "--bit-TextRotator-*" custom properties.
/// <br />
/// Under a reduced motion preference only the first item is shown and nothing moves, unless the rotator, or an
/// ancestor of it, opts out through <see cref="BitComponentBase.ForceAnimation"/>: the rotation does not start, so the
/// item shown is the current one and carries the CurrentItem class and style. The preference, and the opt-out, are
/// read once the component is interactive.
/// <br />
/// The rotation is not announced: the items that are seen are hidden from assistive technologies, which are given all
/// the items at once instead, as one stable text - so a screen reader reads every word the rotator stands for, and
/// is never interrupted by the next one coming in.
/// </remarks>
public partial class BitTextRotator : BitComponentBase
{
    private const string DefaultElement = "span";
    private const double DefaultInterval = 3000;

    // The tags that are laid out as blocks of their own rather than as a run of text, which the rotator follows by
    // laying itself out as a block too, so it keeps the line breaks and the spacing the tag has.
    private static readonly HashSet<string> _BlockElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "address", "article", "aside", "blockquote", "caption", "dd", "details", "dialog", "div", "dl", "dt",
        "fieldset", "figcaption", "figure", "footer", "form", "h1", "h2", "h3", "h4", "h5", "h6", "header", "hgroup",
        "legend", "li", "main", "nav", "ol", "p", "pre", "section", "summary", "ul"
    };

    private string?[] _items = [];
    private int _current;
    // The item that is moving out as the current one comes in. It is -1 until the first turn, so the item shown first
    // is shown at rest rather than played in - on the first render and on the one that takes over a prerendered page.
    private int _previous = -1;
    private System.Timers.Timer? _timer;
    // Whether the reader asked for less motion, read off the root once it is rendered interactively; until it is read
    // nothing rotates, so a rotator under the preference never moves on before it is known.
    private bool _reducedMotion;
    private bool _reducedMotionRead;

    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Gets or sets the cascading parameters for the text rotator component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings (the pace, the tag, or a "pause animations"
    /// switch for a whole region) to be applied to multiple text rotator components through the
    /// <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitTextRotatorParams.ParamName)]
    public BitTextRotatorParams? CascadingParameters { get; set; }



    /// <summary>
    /// Custom CSS classes for the different parts of the text rotator.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitTextRotatorClassStyles? Classes { get; set; }

    /// <summary>
    /// The custom html element used for the root node. The default is "span".
    /// </summary>
    /// <remarks>
    /// The rotator is most of the time a word inside a sentence, a heading or a button, where only phrasing content is
    /// valid; use a heading tag for a heading that is the rotator alone.
    /// <br />
    /// An empty or whitespace value falls back to the default tag, and so does a value that is not a name a tag can
    /// have: one that does not begin with an ASCII letter, or that carries anything but letters, digits and the "-",
    /// "_", "." and ":" that join them. A void element (such as "img" or "br") is defined to hold no content, so no
    /// item is rendered into one.
    /// </remarks>
    [Parameter] public string? Element { get; set; }

    /// <summary>
    /// The time each item is shown for before the next one comes in, in ms.
    /// <br />
    /// The default value is <strong>3000</strong>.
    /// </summary>
    /// <remarks>
    /// It is counted from the moment an item starts to come in, so it includes the entrance, which takes
    /// "--bit-TextRotator-duration"; keep it well above that, and long enough for the item to be read. A value that is
    /// not a positive number falls back to the default.
    /// </remarks>
    [Parameter] public double Interval { get; set; } = DefaultInterval;

    /// <summary>
    /// The words or phrases to rotate through, in order. The first one is the one shown before the rotation starts.
    /// </summary>
    /// <remarks>
    /// The rotation starts over from the first item whenever the items change. With a single item the rotator is that
    /// item, at rest.
    /// </remarks>
    [Parameter] public IEnumerable<string>? Items { get; set; }

    /// <summary>
    /// Holds the rotation on the item that is shown.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// The rotation carries on from that item once this is turned off again, after a whole <see cref="Interval"/>.
    /// Offering a way to set it is what lets a reader stop text that keeps changing (WCAG 2.2.2); a cascaded one
    /// (<see cref="BitTextRotatorParams"/>) is a single switch for a whole region. A disabled rotator does not rotate
    /// either.
    /// </remarks>
    [Parameter] public bool Paused { get; set; }

    /// <summary>
    /// Custom CSS styles for the different parts of the text rotator.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitTextRotatorClassStyles? Styles { get; set; }



    protected override string RootElementClass => "bit-trt";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => _BlockElements.Contains(ResolveElement()) ? "bit-trt-blk" : string.Empty);

        ClassBuilder.Register(() => Classes?.Root);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        var items = Items?.ToArray() ?? [];

        // New items are a new rotation, started from the first of them and at rest, the same as the first render -
        // and given a whole Interval, so the count of the rotation they replace does not cut the first one short.
        if (items.SequenceEqual(_items, StringComparer.Ordinal) is false)
        {
            _current = 0;
            _previous = -1;
            _timer?.Stop();
        }

        _items = items;

        base.OnParametersSet();

        UpdateTimer();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        // The rotation starts here rather than with the parameters: there is no render after the first one while
        // prerendering or rendering statically, so a timer started before it would tick on the server against a
        // rotator that is not on screen.
        if (firstRender is false) return;

        // Under reduced motion the stylesheet holds the first item still, so the rotation does not start at all:
        // the classes and styles of the current item stay on the item that is seen, and no render goes out every
        // Interval to change nothing on screen.
        try
        {
            _reducedMotion = await _js.BitUtilsPrefersReducedMotion(RootElement);
        }
        catch (JSDisconnectedException) { return; } // the circuit went away; there is nothing left to rotate
        catch (JSException) { } // the preference could not be read, so the rotator rotates as it would without it

        _reducedMotionRead = true;

        UpdateTimer();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var element = ResolveElement();

        builder.OpenElement(0, element);
        // The splatted attributes come first so everything the component builds itself is written over them. The
        // values the component would otherwise write as null are resolved against them below, since a null written
        // over a splatted attribute does not leave that attribute alone - it removes it.
        builder.AddMultipleAttributes(1, RuntimeHelpers.TypeCheck(HtmlAttributes));
        builder.AddAttribute(2, "id", Id.HasValue() ? Id : (GetSplattedAttribute("id") ?? _Id));
        builder.AddAttribute(3, "style", JoinStyles(GetSplattedAttribute("style"), StyleBuilder.Value));
        builder.AddAttribute(4, "class", JoinClasses(ClassBuilder.Value, GetSplattedAttribute("class")));
        builder.AddAttribute(5, "dir", Dir?.ToString().ToLowerInvariant() ?? GetSplattedAttribute("dir"));
        builder.AddAttribute(6, "aria-label", ResolveAriaLabel());
        builder.AddAttribute(7, "tabindex", TabIndex ?? GetSplattedAttribute("tabindex"));
        // A disabled rotator is dimmed and stops, which says the text stands for something unavailable, so assistive
        // technologies are told the same - on the text itself, whatever its tag, as on BitTextShimmer.
        builder.AddAttribute(8, "aria-disabled", Disabled ? "true" : GetSplattedAttribute("aria-disabled"));
        builder.AddElementReferenceCapture(9, v => RootElement = v);
        // A void element is defined to hold no content: the static renderer writes it self-closed, so anything put
        // inside it would either be dropped or end up as a sibling of the element in the rendered markup.
        if (IsVoidElement(element) is false)
        {
            for (var i = 0; i < _items.Length; i++)
            {
                var isCurrent = i == _current;

                // Every item is hidden from assistive technologies: the one that is seen changes on its own, and a
                // screen reader reads the stable text below instead.
                builder.OpenElement(10, "span");
                builder.AddAttribute(11, "style", JoinStyles(Styles?.Item, isCurrent ? Styles?.CurrentItem : null));
                builder.AddAttribute(12, "class", JoinClasses(JoinClasses(GetItemClass(i), Classes?.Item), isCurrent ? Classes?.CurrentItem : null));
                builder.AddAttribute(13, "aria-hidden", "true");
                builder.AddContent(14, _items[i]);
                builder.CloseElement();
            }

            // All the items at once, for assistive technologies: one text that does not change while it is read, and
            // that says every word the rotator stands for rather than whichever one happened to be on screen.
            var text = string.Join(", ", _items.Where(item => item.HasValue()));
            if (text.HasValue())
            {
                builder.OpenElement(15, "span");
                builder.AddAttribute(16, "class", "bit-trt-srt");
                builder.AddContent(17, text);
                builder.CloseElement();
            }
        }
        builder.CloseElement();

        base.BuildRenderTree(builder);
    }

    protected override ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return ValueTask.CompletedTask;

        if (_timer is not null)
        {
            _timer.Elapsed -= TimerElapsed;
            _timer.Dispose();
            _timer = null;
        }

        return base.DisposeAsync(disposing);
    }



    private string ResolveElement()
    {
        var element = Element?.Trim();

        return element.HasNoValue() || IsValidElement(element!) is false ? DefaultElement : element!;
    }

    private string GetItemClass(int index)
    {
        if (index == _current)
        {
            // The item shown first stays at rest; every item that takes its place after that comes in.
            return _previous < 0 ? "bit-trt-itm bit-trt-cur" : "bit-trt-itm bit-trt-cur bit-trt-ent";
        }

        return index == _previous ? "bit-trt-itm bit-trt-prv" : "bit-trt-itm";
    }

    private bool ShouldRotate() => Paused is false && Disabled is false && _reducedMotion is false && _items.Length > 1;

    private void UpdateTimer()
    {
        // A timer created once the disposal has started would be left ticking for the rest of the process.
        if (IsDisposing || IsDisposed) return;

        // Before the first render there is nothing on screen to rotate, and until the motion preference is read
        // nothing is known to be allowed to move (see OnAfterRenderAsync).
        if (_reducedMotionRead is false) return;

        if (ShouldRotate() is false)
        {
            _timer?.Stop();
            return;
        }

        // The timer itself rejects an interval beyond int.MaxValue and one that is not positive, so anything it would
        // refuse falls back to the default instead of throwing in the middle of a render.
        var interval = Interval > 0 ? Math.Min(Interval, int.MaxValue) : DefaultInterval;

        if (_timer is null)
        {
            _timer = new System.Timers.Timer(interval);
            _timer.Elapsed += TimerElapsed;
        }
        else if (_timer.Interval != interval)
        {
            // Setting the interval of a running timer starts its count over, so the new pace starts from now.
            _timer.Interval = interval;
        }

        // A timer that is running already is left alone: a parent rendering again must not hold the rotation back.
        if (_timer.Enabled is false)
        {
            _timer.Start();
        }
    }

    private async void TimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        if (IsDisposed) return;

        try
        {
            await InvokeAsync(() =>
            {
                if (IsDisposed || ShouldRotate() is false) return;

                _previous = _current;
                _current = (_current + 1) % _items.Length;

                StateHasChanged();
            });
        }
        catch (ObjectDisposedException) { } // the renderer went away between the tick and the dispatch
        catch (Exception ex)
        {
            // An exception that escapes an async void handler takes the whole process down, so anything else is
            // handed back to the renderer, where it is treated like an exception of any ordinary event handler.
            try
            {
                await DispatchExceptionAsync(ex);
            }
            catch { } // the renderer itself went away; there is nothing left to report the exception to
        }
    }
}
