using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace Bit.BlazorUI;

/// <summary>
/// BitProgress is used to show the completion status of an operation lasting more than 2 seconds.
/// </summary>
public partial class BitProgress : BitComponentBase
{
    private string _labelId = string.Empty;
    private string _descriptionId = string.Empty;
    private double? _lastAnnouncedStep;
    private string? _announcement;
    private int _announcementGeneration;
    private bool _delayDecided;
    private bool _isDelaying;
    private bool _holdsForInteractivity;
    private int _delayInEffect;
    private CancellationTokenSource? _delayCts;



    /// <summary>
    /// Gets or sets the cascading parameters for the progress component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple progress components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitProgressParams.ParamName)]
    public BitProgressParams? CascadingParameters { get; set; }



    /// <summary>
    /// Announces the progress to screen readers as it advances, through a live region of its own.
    /// The announcement is made once per <see cref="AnnounceStep"/> crossed rather than on every
    /// change, since a bar that speaks on every percent is a bar nobody can listen to.
    /// </summary>
    [Parameter] public bool AnnounceProgress { get; set; }

    /// <summary>
    /// How far the progress has to advance, in percentage points, before it is announced again.
    /// Completion is always announced, whatever the step divides into. The first observed value,
    /// including 100%, is recorded without announcement. A zero or negative value is treated as 25.
    /// </summary>
    [Parameter] public double AnnounceStep { get; set; } = 25;

    /// <summary>
    /// Text alternative of the progress status, used by screen readers for reading the value of the progress.
    /// </summary>
    [Parameter] public string? AriaValueText { get; set; }

    /// <summary>
    /// The color of the bar itself, as any CSS color. It replaces the palette the <see cref="Color"/> role
    /// would have given, and everything derived from it follows: the stroke of the ring, the faint tint of
    /// the <see cref="Buffer"/> and the fill of a <see cref="Striped"/> bar.
    /// </summary>
    /// <remarks>
    /// It wins over the --bit-Progress-bar-color CSS variable. The buffer tint follows it only while
    /// --bit-Progress-buffer-color is unset: no parameter paints the buffer, so that variable keeps it.
    /// </remarks>
    [Parameter] public string? BarColor { get; set; }

    /// <summary>
    /// The secondary, buffered progress rendered behind the main bar, for an operation that loads ahead of
    /// what it has already played or processed (the buffered part of a video, the downloaded part of a file).
    /// It is read on the same scale as <see cref="Value"/> (between <see cref="Min"/> and <see cref="Max"/>)
    /// when a Value is set, and as a percentage between 0 and 100 otherwise. Ignored while
    /// <see cref="Indeterminate"/> is true.
    /// </summary>
    [Parameter] public double? Buffer { get; set; }

    /// <summary>
    /// Draws the progress as a ring instead of as a bar, which is the shape for a compact spot - inside a
    /// button, in a card corner, beside a row - where a full-width bar has nowhere to go. A circular
    /// indeterminate progress is what is usually called a spinner.
    /// </summary>
    /// <remarks>
    /// Segments, the vertical orientation and the gauge gap each apply to one shape only, so the class
    /// list has to be rebuilt when the shape itself changes.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Circular { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitProgress.
    /// </summary>
    [Parameter] public BitProgressClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the BitProgress.
    /// </summary>
    /// <remarks>
    /// An explicit value wins over the --bit-Progress-bar-color and --bit-Progress-bar-text-color CSS variables; left
    /// unset, the progress is primary unless those variables say otherwise. The buffer tint follows it only while
    /// --bit-Progress-buffer-color is unset, since no parameter paints the buffer.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// How long, in milliseconds, the progress stays hidden after it is first rendered. An operation that finishes
    /// within that window never shows an indicator at all, which is better than one that flashes up and vanishes.
    /// The space it takes is kept, so nothing moves when it appears, and it is hidden from assistive technology
    /// for as long as it is hidden from sight.
    /// </summary>
    /// <remarks>
    /// The window opens once, with the first render, and is never opened again: a Delay given to a progress that
    /// is already on screen does not hide it, and one taken away while the window is open shows it at once. A
    /// progress prerendered ahead of an interactive render stays hidden until that render has run its window,
    /// rather than showing up in the prerendered page and vanishing again when the interactive one takes over.
    /// </remarks>
    [Parameter] public int Delay { get; set; }

    /// <summary>
    /// Text describing or supplementing the operation.
    /// </summary>
    [Parameter] public string? Description { get; set; }

    /// <summary>
    /// Custom template for describing or supplementing the operation.
    /// </summary>
    [Parameter] public RenderFragment? DescriptionTemplate { get; set; }

    /// <summary>
    /// The diameter of the circular progress in pixels. When not set, the diameter falls back to the
    /// theme value of the current <see cref="Size"/>, growing beyond it only when
    /// <see cref="Thickness"/> multiplied by <see cref="Radius"/> asks for more room.
    /// </summary>
    [Parameter] public int? Diameter { get; set; }

    /// <summary>
    /// How thick the indicator is drawn, in pixels: the height of a horizontal bar, the width of a
    /// <see cref="Vertical"/> one and the stroke of the ring. When not set it follows the <see cref="Size"/>,
    /// which is what keeps a page of indicators in step with each other and with the theme.
    /// </summary>
    [Parameter] public int? Thickness { get; set; }

    /// <summary>
    /// Cuts a gap of this many degrees out of the bottom of the circular progress, which turns the ring
    /// into a gauge. Between 0 (a closed ring, the default) and 295; a value of 180 leaves a half circle.
    /// Has no effect on the linear progress.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public double GapDegree { get; set; }

    /// <summary>
    /// Where the <see cref="GapDegree"/> gap sits, which is also where the stroke of the gauge begins
    /// and ends (default is the bottom, which is where a gauge is normally opened). <see cref="Reversed"/>
    /// mirrors the gauge, so it swaps a Start gap with an End one and leaves a Top or a Bottom one where it is.
    /// </summary>
    /// <remarks>
    /// Only Top, Bottom, Start and End are meaningful here; any other side leaves the gap at the bottom.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitPlacement GapPlacement { get; set; } = BitPlacement.Bottom;

    /// <summary>
    /// Reports that something is running without saying how far along it is: the bar sweeps and the ring spins
    /// instead of filling. No value is published to assistive technology in this mode - which is what tells a
    /// screen reader the progress is indeterminate - and the percentage readout is hidden. Switch to a
    /// determinate value as soon as one exists.
    /// </summary>
    [Parameter] public bool Indeterminate { get; set; }

    /// <summary>
    /// Label to display above the BitProgress.
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// Custom label template to display above the BitProgress.
    /// </summary>
    [Parameter] public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// How long a <see cref="Vertical"/> bar is, as a CSS length. A horizontal bar takes the width of
    /// whatever it is put in, so this has no effect there.
    /// </summary>
    [Parameter] public string? Length { get; set; }

    /// <summary>
    /// The lowest value of the range the <see cref="Value"/> is read against. It has no effect while
    /// <see cref="Value"/> is null, in which case <see cref="Percent"/> is already a percentage.
    /// </summary>
    [Parameter] public double Min { get; set; }

    /// <summary>
    /// The highest value of the range the <see cref="Value"/> is read against. It has no effect while
    /// <see cref="Value"/> is null, in which case <see cref="Percent"/> is already a percentage.
    /// </summary>
    [Parameter] public double Max { get; set; } = 100;

    /// <summary>
    /// Reports the indicator as a meter rather than as a progress bar. A progress bar says how far along a
    /// task is and only ever moves forward; a meter is a reading taken within a known range - a disk that is
    /// 60% full, a temperature, a score - which can move either way and is never "finished". This is what the
    /// ARIA practices ask for when the number is a measurement rather than progress, and it pairs with the
    /// gauge shape and with <see cref="Value"/>, <see cref="Min"/> and <see cref="Max"/>. An
    /// <see cref="Indeterminate"/> indicator stays a progress bar, since a meter always has a value.
    /// </summary>
    [Parameter] public bool Meter { get; set; }

    /// <summary>
    /// Percentage of the operation's completeness, numerically between 0 and 100.
    /// Ignored when <see cref="Value"/> is set.
    /// </summary>
    [Parameter] public double Percent { get; set; }

    /// <summary>
    /// The composite format string the percentage readout is written with, applied to the percentage itself -
    /// "{0:F0} %" by default. It is formatted on the current culture, since it is text the reader sees.
    /// A malformed format string falls back to the default rather than failing the render.
    /// </summary>
    [Parameter] public string PercentNumberFormat { get; set; } = DefaultPercentNumberFormat;

    /// <summary>
    /// Where the percentage readout of a linear progress is placed: under the bar aligned to its end
    /// (the default), to its start, in the middle, or on the bar itself. The readout of a circular
    /// progress is always in the middle of the ring, so this has no effect there.
    /// </summary>
    [Parameter] public BitProgressPercentPosition PercentNumberPosition { get; set; }

    /// <summary>
    /// Custom template for the percentage display, receiving the current percentage as its context.
    /// It replaces the text that <see cref="PercentNumberFormat"/> would have produced.
    /// </summary>
    [Parameter] public RenderFragment<double>? PercentNumberTemplate { get; set; }

    /// <summary>
    /// The multiplier applied to the <see cref="Thickness"/> to size the circular progress. The
    /// resulting diameter never falls below the theme value of the current <see cref="Size"/>, and
    /// setting <see cref="Diameter"/> replaces this calculation altogether.
    /// </summary>
    [Parameter] public int Radius { get; set; } = 6;

    /// <summary>
    /// Fills the progress from the end of the container towards its start, mirroring the direction of
    /// the linear bar and turning the circular one counter-clockwise.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Reversed { get; set; }

    /// <summary>
    /// Rounds the ends of the bar: a pill-shaped track and bar in linear mode, and a round stroke cap
    /// in circular mode.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Rounded { get; set; }

    /// <summary>
    /// Cuts the linear bar into this many equal segments, for an operation made of a known number of
    /// discrete steps. The bar still fills continuously - the segments are how far apart the steps are
    /// drawn, not how the value is rounded. Has no effect on the circular progress.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public int? Segments { get; set; }

    /// <summary>
    /// The gap between two <see cref="Segments"/>, in pixels.
    /// </summary>
    [Parameter] public int SegmentGap { get; set; } = 4;

    /// <summary>
    /// Writes the percentage beside the bar, or in the middle of the ring. <see cref="PercentNumberPosition"/>
    /// says where it goes and <see cref="PercentNumberFormat"/> how it reads. It is hidden while
    /// <see cref="Indeterminate"/> is true, since there is no number to show.
    /// </summary>
    [Parameter] public bool ShowPercentNumber { get; set; }

    /// <summary>
    /// The size of the BitProgress.
    /// </summary>
    /// <remarks>
    /// An explicit value wins over the --bit-Progress-font-size, --bit-Progress-description-font-size,
    /// --bit-Progress-thickness and --bit-Progress-diameter CSS variables; left unset, the progress keeps its own
    /// defaults - the thinnest track and ring stroke, the medium type - unless those variables say otherwise.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// Paints diagonal stripes over the linear bar, which is the conventional way of saying that the
    /// operation behind a determinate bar is still running. Set <see cref="StripedAnimation"/> to make
    /// the stripes travel. Has no effect on the circular or the indeterminate progress.
    /// </summary>
    [Parameter] public bool Striped { get; set; }

    /// <summary>
    /// Animates the stripes of a <see cref="Striped"/> bar so they travel along it.
    /// </summary>
    [Parameter] public bool StripedAnimation { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitProgress.
    /// </summary>
    [Parameter] public BitProgressClassStyles? Styles { get; set; }

    /// <summary>
    /// The color of the unfilled part of the indicator, as any CSS color: the track behind the bar, the ring
    /// behind the stroke, and the two ends the indeterminate sweep fades into.
    /// </summary>
    [Parameter] public string? TrackColor { get; set; }

    /// <summary>
    /// Stands the linear bar on its end, filling it from the bottom up - or from the top down when it
    /// is also <see cref="Reversed"/>. A vertical bar has no width to take from its container, so its
    /// height comes from <see cref="Length"/>. Has no effect on the circular progress.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Vertical { get; set; }

    /// <summary>
    /// The completeness of the operation expressed in its own unit, read against <see cref="Min"/> and
    /// <see cref="Max"/>. When set, it takes the place of <see cref="Percent"/> and is what the screen
    /// reader is given, so an operation counted in files or in bytes is announced in files or in bytes.
    /// </summary>
    [Parameter] public double? Value { get; set; }


    protected override string RootElementClass => "bit-prb";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        // Color and Size publish nothing while they are unset, which is what lets the stylesheet tell a default from a
        // choice: the public --bit-Progress-* variables restyle the default and never an explicit value.
        ClassBuilder.Register(() => BitCssClasses.Color(Color, "bit-prb"));

        ClassBuilder.Register(() => BitCssClasses.Size(Size, "bit-prb"));

        ClassBuilder.Register(() => Rounded ? "bit-prb-rnd" : string.Empty);

        ClassBuilder.Register(() => Reversed ? "bit-prb-rev" : string.Empty);

        ClassBuilder.Register(() => _HasSegments ? "bit-prb-seg" : string.Empty);

        ClassBuilder.Register(() => _HasGap is false ? string.Empty : GapPlacement switch
        {
            BitPlacement.Top => "bit-prb-gap bit-prb-gpt",
            BitPlacement.Start => "bit-prb-gap bit-prb-gps",
            BitPlacement.End => "bit-prb-gap bit-prb-gpe",
            _ => "bit-prb-gap"
        });

        ClassBuilder.Register(() => _IsVertical ? "bit-prb-ver" : string.Empty);

        ClassBuilder.Register(() => _isDelaying ? (_holdsForInteractivity ? "bit-prb-dlh" : "bit-prb-dly") : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override Task OnInitializedAsync()
    {
        _labelId = $"BitProgress-{UniqueId}-label";
        _descriptionId = $"BitProgress-{UniqueId}-description";

        return base.OnInitializedAsync();
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitProgressParams))]
    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        UpdateDelay();

        UpdateAnnouncement();

        base.OnParametersSet();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        // The window is timed from the first render that has an after-render at all, which is an interactive one:
        // a static page has no one to end it for, and reveals itself through the stylesheet instead.
        if (firstRender is false || _isDelaying is false) return;

        _delayCts = new CancellationTokenSource();

        _ = WaitOutDelayAsync(_delayCts.Token);
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (disposing)
        {
            _delayCts?.Cancel();
            _delayCts?.Dispose();
            _delayCts = null;
        }

        await base.DisposeAsync(disposing);
    }


    // The window is decided on the first pass, once the cascade has had its say, and only ever closed after that.
    // Opening it again later would hide a progress the reader is already watching; and since the stylesheet's
    // reveal replays on any new element, the class is dropped as soon as the window is over, so nothing that
    // recreates the root afterwards can hide it again.
    private void UpdateDelay()
    {
        if (_delayDecided is false)
        {
            _delayDecided = true;

            if (Delay <= 0) return;

            _isDelaying = true;
            _delayInEffect = Delay;

            // A prerender is followed by an interactive render that builds the root anew and so starts the reveal
            // over, which would show the prerendered progress and then hide it again. The prerendered one is held
            // hidden instead, and the interactive render runs the window. A statically rendered page has no render
            // mode and nothing to follow it, so it reveals itself through the stylesheet.
            _holdsForInteractivity = IsPrerendering();

            ClassBuilder.Reset();

            return;
        }

        if (_isDelaying && Delay <= 0)
        {
            EndDelay();
        }
    }

    // Only from net9.0 on does the framework say how a component is rendered; before it, a prerender cannot be told
    // from a static render, and both reveal themselves through the stylesheet.
    private bool IsPrerendering()
    {
#if NET9_0_OR_GREATER
        return AssignedRenderMode is not null && RendererInfo.IsInteractive is false;
#else
        return false;
#endif
    }

    private void EndDelay()
    {
        _isDelaying = false;

        _delayCts?.Cancel();
        _delayCts?.Dispose();
        _delayCts = null;

        ClassBuilder.Reset();
    }

    private async Task WaitOutDelayAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(_delayInEffect, token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (IsDisposed || token.IsCancellationRequested) return;

        await InvokeAsync(() =>
        {
            if (IsDisposed || _isDelaying is false) return;

            EndDelay();

            StateHasChanged();
        });
    }



    private bool _HasLabel => LabelTemplate is not null || Label.HasValue();

    private bool _HasDescription => DescriptionTemplate is not null || Description.HasValue();

    private bool _ShowsPercentNumber => Indeterminate is false && (ShowPercentNumber || PercentNumberTemplate is not null);

    private bool _HasBuffer => Indeterminate is false && Buffer.HasValue;

    private bool _HasSegments => Circular is false && Segments is > 1;

    // The inside readout sits over the bar, which is inside the container; every other position keeps
    // it a sibling of the container, out of reach of the segment mask.
    private bool _IsPercentNumberInside => Circular is false && PercentNumberPosition == BitProgressPercentPosition.Inside;

    // The top readout shares the label's row instead of taking a line of its own, so it is rendered in the
    // header rather than under the bar. Nothing is put there when there is no readout to place.
    private bool _IsPercentNumberTop => Circular is false && _ShowsPercentNumber && PercentNumberPosition == BitProgressPercentPosition.Top;

    // The label keeps a wrapper of its own whether or not the readout joins it, so the row the two share is
    // the same element the label sits in alone.
    private bool _HasHeader => _HasLabel || _IsPercentNumberTop;

    // A meter is a reading, not a task: it needs a value, so an indeterminate indicator stays a progress bar.
    private string _Role => Meter && Indeterminate is false ? "meter" : "progressbar";

    private string _PercentNumberClass => PercentNumberPosition switch
    {
        BitProgressPercentPosition.Start => "bit-prb-pct bit-prb-pcs",
        BitProgressPercentPosition.Center => "bit-prb-pct bit-prb-pcc",
        BitProgressPercentPosition.Inside => "bit-prb-pct bit-prb-pci",
        BitProgressPercentPosition.Top => "bit-prb-pct bit-prb-pco",
        // The default carries a class of its own so a rule that wants to move only the readout nobody
        // placed by hand - the vertical one, which is centred over its narrow bar - can say so without
        // outranking an explicit alignment.
        _ => "bit-prb-pct bit-prb-pce"
    };

    private bool _IsVertical => Circular is false && Vertical;

    // A horizontal bar takes the width of its container and is only told how thick it is; a vertical
    // one has no height to take, so it is given both.
    private string? _BarContainerStyle => Circular
        ? Styles?.BarContainer
        : _IsVertical
            ? $"width: {GetThicknessStyleValue()};height: {(Length.HasValue() ? Length : "var(--bit-prb-length)")};{Styles?.BarContainer}"
            : $"min-height: {GetThicknessStyleValue()};{Styles?.BarContainer}";

    private string? _SegmentStyle => _HasSegments
        ? $"--bit-prb-segments: {Segments};--bit-prb-segment-gap: {Math.Max(0, SegmentGap)}px;"
        : null;

    // 295 is where Ant Design stops too: past it the ring is more gap than gauge and the stroke gets
    // too short to read as an arc at all.
    private bool _HasGap => Circular && GapDegree > 0;

    private double _GapDegree => Math.Clamp(GapDegree, 0, 295);

    private string? _GapStyle => _HasGap
        ? $"--bit-prb-gap: {Css(_GapDegree)}deg;--bit-prb-arc: {Css((360 - _GapDegree) / 360)};"
        : null;

    // Both colors are declared on the root as custom properties rather than written onto the parts: the bar
    // color is read by the bar, the ring stroke, the buffer tint and the stripes, and one declaration keeps
    // all of them in step. An inline declaration also outranks the role class, which is what lets a custom
    // color replace the palette Color would have given.
    private string? _ColorStyle
    {
        get
        {
            if (BarColor.HasNoValue() && TrackColor.HasNoValue()) return null;

            StringBuilder sb = new();

            if (BarColor.HasValue())
            {
                sb.Append($"--bit-prb-bar-color: {BarColor};");
            }

            if (TrackColor.HasValue())
            {
                sb.Append($"--bit-prb-track-color: {TrackColor};");
            }

            return sb.Length == 0 ? null : sb.ToString();
        }
    }

    // The length the window opened with, not the current Delay: a change of it mid-window would retime the reveal.
    private string? _DelayStyle => _isDelaying && _holdsForInteractivity is false ? $"--bit-prb-delay: {_delayInEffect}ms;" : null;

    private string? _RootStyle
    {
        get
        {
            var prefix = _SegmentStyle + _GapStyle + _DiameterStyle + _ColorStyle + _DelayStyle;

            return prefix.HasNoValue() ? StyleBuilder.Value : prefix + StyleBuilder.Value;
        }
    }

    /// <summary>
    /// The width of the bar, always a percentage: either the <see cref="Value"/> read against the
    /// Min/Max range, or the <see cref="Percent"/> taken as it is.
    /// </summary>
    private double _Percent => Value.HasValue ? ToPercent(Value.Value) : Normalize(Percent);

    private double _BufferPercent => Buffer.HasValue
        ? (Value.HasValue ? ToPercent(Buffer.Value) : Normalize(Buffer.Value))
        : 0;

    /// <summary>
    /// What the screen reader is told the progress currently is. With a <see cref="Value"/> it is that
    /// value in its own unit, so the range it is read against is the Min/Max pair rather than 0..100.
    /// </summary>
    private string? _AriaValueNow => Indeterminate ? null : Css(Value.HasValue ? (double.IsNaN(Value.Value) ? Min : Math.Clamp(Value.Value, Min, Math.Max(Min, Max))) : _Percent);

    private string? _AriaValueMin => Indeterminate ? null : Css(Value.HasValue ? Min : 0);

    private string? _AriaValueMax => Indeterminate ? null : Css(Value.HasValue ? Math.Max(Min, Max) : 100);

    private string? _AriaLabelledBy => _HasLabel && AriaLabel.HasNoValue() ? _labelId : null;

    private string? _AriaDescribedBy => _HasDescription ? _descriptionId : null;

    private string _BarClass
    {
        get
        {
            StringBuilder sb = new("bit-prb-bar");

            if (_ShowsPercentNumber && _IsPercentNumberInside)
            {
                sb.Append(" bit-prb-bri");
            }

            if (Indeterminate)
            {
                sb.Append(" bit-prb-ind");
            }
            else if (Striped)
            {
                // The indeterminate sweep paints the bar with a gradient of its own, so the stripes are
                // put on the bar element itself rather than in a descendant rule that would outrank it.
                sb.Append(" bit-prb-stp");

                if (StripedAnimation)
                {
                    sb.Append(" bit-prb-sta");
                }
            }

            return sb.ToString();
        }
    }

    // Numbers that end up in a style attribute or an aria value are formatted invariantly: a culture
    // with a comma decimal separator would otherwise emit "width: 52,5%", which no engine parses.
    private static string Css(double value) => value.ToString(CultureInfo.InvariantCulture);

    // The readout is consumer-facing text, so it stays on the current culture - only the format string
    // is defended, since a null or malformed one ("{0:F0 %", "{1}") would take the whole render down with it.
    private string FormatPercent(double percent)
    {
        try
        {
            return string.Format(PercentNumberFormat ?? DefaultPercentNumberFormat, percent);
        }
        catch (FormatException)
        {
            return string.Format(DefaultPercentNumberFormat, percent);
        }
    }

    private const string DefaultPercentNumberFormat = "{0:F0} %";

    // The live region says something once per step crossed, and once more at completion. Announcing
    // every change instead would make a screen reader unusable for as long as the operation runs; the
    // value itself is on the progressbar all along for anyone who asks for it.
    private void UpdateAnnouncement()
    {
        if (AnnounceProgress is false || Indeterminate)
        {
            _lastAnnouncedStep = null;
            _announcement = null;
            return;
        }

        var step = AnnounceStep > 0 ? AnnounceStep : 25;
        var percent = _Percent;
        var milestone = percent >= 100 ? 100 : Math.Floor(percent / step) * step;

        // The first sight of a value is where the progress started, not something it just reached, and
        // a value that went backwards is a reset rather than an advance: both are recorded in silence.
        if (_lastAnnouncedStep is null || milestone <= _lastAnnouncedStep)
        {
            _lastAnnouncedStep = milestone;
            return;
        }

        _lastAnnouncedStep = milestone;

        var value = AriaValueText.HasValue() ? AriaValueText! : FormatPercent(milestone);

        // The announcement is prefixed with the name the bar is known by, which is the AriaLabel when there is
        // one - it wins over the visible label for the name too - so a bar without a visible label still says
        // what it is that advanced.
        var name = AriaLabel.HasValue() ? AriaLabel : Label;

        _announcement = name.HasValue() ? $"{name}: {value}" : value;

        // A progress that was reset and climbed back reaches the same milestone with the same words,
        // and a live region that ends up holding the text it already held is a change of nothing. The
        // generation is the key of the element carrying it, so each announcement is a new element.
        _announcementGeneration++;
    }

    // A percentage computed as done / total is NaN while the total is still zero, and a NaN clamps to itself: it
    // is read as nothing done rather than written out as "width: NaN%" and aria-valuenow="NaN".
    private static double Normalize(double? value) => double.IsNaN(value.GetValueOrDefault()) ? 0 : Math.Clamp(value.GetValueOrDefault(), 0, 100);

    private double ToPercent(double value)
    {
        var max = Math.Max(Min, Max);
        var range = max - Min;

        return range <= 0 || double.IsNaN(value) ? 0 : Math.Clamp((value - Min) / range * 100, 0, 100);
    }

    private int GetThickness() => Math.Max(0, Thickness ?? Size switch
    {
        BitSize.Small => 2,
        BitSize.Medium => 4,
        BitSize.Large => 8,
        _ => 2
    });

    // The linear bar reads its thickness from the theme's track tokens (--bit-siz-track-* via the
    // size classes in BitProgress.scss) unless an explicit Thickness overrides it. The circular
    // variant keeps the numeric value: SVG geometry (height/width attributes) cannot consume a CSS
    // custom property.
    private string GetThicknessStyleValue() => Thickness is not null ? $"{GetThickness()}px" : "var(--bit-prb-thickness)";

    // An explicit Diameter is the whole answer; otherwise the SVG keeps its historical size - the
    // thickness multiplied by the Radius - and the stylesheet's min-width/min-height floor it at the
    // diameter token of the current size.
    private int GetDiameter() => Diameter.HasValue ? Math.Max(0, Diameter.Value) : GetThickness() * Math.Max(0, Radius);

    // Pinning the token to the explicit diameter turns the stylesheet's floor into an exact size, so a
    // Diameter smaller than the size default still shrinks the ring.
    private string? _DiameterStyle => Circular && Diameter.HasValue ? $"--bit-prb-diameter: {GetDiameter()}px;" : null;

    // The readout in the middle of the ring is set on a step of the type ramp picked by the size the ring is drawn
    // at, so a large gauge carries a number that can be read from across the room while every size stays on the
    // ramp a preset re-skins. Only the step is chosen here; its size is the theme's.
    private string _RingReadoutClass
    {
        get
        {
            var step = GetReadoutStep(GetDiameter());

            // Without a Diameter the ring is never drawn below the diameter token of its size, and the large one's
            // is already big enough for the step above the smallest.
            if (Diameter.HasValue is false && Size == BitSize.Large)
            {
                step = Math.Max(step, 1);
            }

            return _RingReadoutSteps[step];
        }
    }

    // The readout box is 60% of the ring wide and a default readout ("100 %", "85.7 %") is about three of its em
    // wide, so a step fits once the ring is five times its font size across: 70px for the 14px step of the default
    // ramp, 160px for the 32px one. A ramp a preset makes wider than that is cut off with an ellipsis rather than
    // silently.
    private static int GetReadoutStep(int diameter) => diameter switch
    {
        >= 160 => 7,
        >= 140 => 6,
        >= 120 => 5,
        >= 100 => 4,
        >= 90 => 3,
        >= 80 => 2,
        >= 70 => 1,
        _ => 0
    };

    private static readonly string[] _RingReadoutSteps =
    [
        "bit-prb-ctx bit-prb-fxs",
        "bit-prb-ctx bit-prb-fsm",
        "bit-prb-ctx bit-prb-fmd",
        "bit-prb-ctx bit-prb-flg",
        "bit-prb-ctx bit-prb-fxl",
        "bit-prb-ctx bit-prb-f2x",
        "bit-prb-ctx bit-prb-f3x",
        "bit-prb-ctx bit-prb-f4x"
    ];

    // What "thick" means depends on which way the bar runs: the height of a horizontal one, the stroke
    // of a ring, and - for a vertical one - the width, which the container already carries for all
    // three of its children. A ring is not drawn from the track tokens: a design system sizes its spinner
    // stroke apart from its bar track (Material has 4px tracks and 4px spinners, Fluent 2 1px tracks), so an
    // unset Thickness falls back to the stroke the stylesheet resolves on the root: the per-size multiple of the spinner
    // stroke token an explicit Size publishes, then --bit-Progress-thickness, then the spinner stroke token itself.
    // The circle is drawn at 40% of the diameter, so a stroke wider than 20% of it would spill past the edge of the
    // svg and be cut off; a percentage stroke is read against the size the ring is actually drawn at, which is what
    // keeps a thick stroke on a small ring (or a ring floored by the diameter token) whole.
    private string _ThicknessDeclaration => Circular
        ? (Thickness is null ? "stroke-width: min(var(--bit-prb-ring-width), 20%);" : $"stroke-width: min({GetThickness()}px, 20%);")
        : _IsVertical ? string.Empty : $"height: {GetThicknessStyleValue()};";

    // ... and so does the axis the value is drawn along.
    private string _FillProperty => Circular ? "--bit-prb-percent" : _IsVertical ? "height" : "width";

    private string GetTrackStyle()
    {
        StringBuilder sb = new();

        sb.Append(_ThicknessDeclaration);

        // The custom styles come last so what the consumer wrote wins over the computed geometry.
        sb.Append(Styles?.Track);

        return sb.ToString();
    }

    private string GetBufferStyle()
    {
        StringBuilder sb = new();

        sb.Append(_ThicknessDeclaration);

        sb.Append($"{(Circular ? "--bit-prb-buffer" : _FillProperty)}: {Css(_BufferPercent)}%;");

        sb.Append(Styles?.Buffer);

        return sb.ToString();
    }

    private string GetProgressStyle()
    {
        StringBuilder sb = new();

        sb.Append(_ThicknessDeclaration);

        if (Indeterminate is false)
        {
            sb.Append($"{_FillProperty}: {Css(_Percent)}%;");
        }

        sb.Append(Styles?.Bar);

        return sb.ToString();
    }
}
