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



    /// <summary>
    /// Announces the progress to screen readers as it advances, through a live region of its own.
    /// The announcement is made once per <see cref="AnnounceStep"/> crossed rather than on every
    /// change, since a bar that speaks on every percent is a bar nobody can listen to.
    /// </summary>
    [Parameter] public bool AnnounceProgress { get; set; }

    /// <summary>
    /// How far the progress has to advance, in percentage points, before it is announced again.
    /// Completion is always announced, whatever the step divides into.
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
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

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
    /// and ends. <see cref="Reversed"/> mirrors the gauge, so it swaps a Start gap with an End one and
    /// leaves a Top or a Bottom one where it is.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitProgressGapPosition GapPosition { get; set; }

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
    /// </summary>
    [Parameter] public string PercentNumberFormat { get; set; } = "{0:F0} %";

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

        ClassBuilder.Register(() => Color switch
        {
            BitColor.Primary => "bit-prb-pri",
            BitColor.Secondary => "bit-prb-sec",
            BitColor.Tertiary => "bit-prb-ter",
            BitColor.Info => "bit-prb-inf",
            BitColor.Success => "bit-prb-suc",
            BitColor.Warning => "bit-prb-wrn",
            BitColor.SevereWarning => "bit-prb-swr",
            BitColor.Error => "bit-prb-err",
            BitColor.PrimaryBackground => "bit-prb-pbg",
            BitColor.SecondaryBackground => "bit-prb-sbg",
            BitColor.TertiaryBackground => "bit-prb-tbg",
            BitColor.PrimaryForeground => "bit-prb-pfg",
            BitColor.SecondaryForeground => "bit-prb-sfg",
            BitColor.TertiaryForeground => "bit-prb-tfg",
            BitColor.PrimaryBorder => "bit-prb-pbr",
            BitColor.SecondaryBorder => "bit-prb-sbr",
            BitColor.TertiaryBorder => "bit-prb-tbr",
            _ => "bit-prb-pri"
        });

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-prb-sm",
            BitSize.Medium => "bit-prb-md",
            BitSize.Large => "bit-prb-lg",
            _ => string.Empty
        });

        ClassBuilder.Register(() => Rounded ? "bit-prb-rnd" : string.Empty);

        ClassBuilder.Register(() => Reversed ? "bit-prb-rev" : string.Empty);

        ClassBuilder.Register(() => _HasSegments ? "bit-prb-seg" : string.Empty);

        ClassBuilder.Register(() => _HasGap is false ? string.Empty : GapPosition switch
        {
            BitProgressGapPosition.Top => "bit-prb-gap bit-prb-gpt",
            BitProgressGapPosition.Start => "bit-prb-gap bit-prb-gps",
            BitProgressGapPosition.End => "bit-prb-gap bit-prb-gpe",
            _ => "bit-prb-gap"
        });

        ClassBuilder.Register(() => _IsVertical ? "bit-prb-ver" : string.Empty);
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

    protected override void OnParametersSet()
    {
        UpdateAnnouncement();

        base.OnParametersSet();
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

    private string? _RootStyle
    {
        get
        {
            var prefix = _SegmentStyle + _GapStyle + _ColorStyle;

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
    private string? _AriaValueNow => Indeterminate ? null : Css(Value.HasValue ? Math.Clamp(Value.Value, Min, Math.Max(Min, Max)) : _Percent);

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
    // is defended, since a null one would take the whole render down with it.
    private string FormatPercent(double percent) => string.Format(PercentNumberFormat ?? "{0:F0} %", percent);

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

        _announcement = Label.HasValue() ? $"{Label}: {value}" : value;

        // A progress that was reset and climbed back reaches the same milestone with the same words,
        // and a live region that ends up holding the text it already held is a change of nothing. The
        // generation is the key of the element carrying it, so each announcement is a new element.
        _announcementGeneration++;
    }

    private static double Normalize(double? value) => Math.Clamp(value.GetValueOrDefault(), 0, 100);

    private double ToPercent(double value)
    {
        var max = Math.Max(Min, Max);
        var range = max - Min;

        return range <= 0 ? 0 : Math.Clamp((value - Min) / range * 100, 0, 100);
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

    private string? GetCircleStyle()
    {
        // Pinning the token to the explicit diameter turns the stylesheet's floor into an exact size,
        // so a Diameter smaller than the size default still shrinks the ring.
        return Diameter.HasValue ? $"--bit-prb-diameter: {GetDiameter()}px;" : null;
    }

    // What "thick" means depends on which way the bar runs: the height of a horizontal one, the stroke
    // of a ring, and - for a vertical one - the width, which the container already carries for all
    // three of its children.
    private string _ThicknessDeclaration => Circular
        ? $"stroke-width: {GetThickness()}px;"
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
