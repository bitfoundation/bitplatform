using System.Text.Json.Serialization;

namespace Bit.BlazorUI;

/// <summary>
/// Optional visual configuration for the timeline: axis/row dimensions, bar
/// sizing, fonts, colors and the content-pane surface. Every property is
/// nullable; those left <c>null</c> keep the value in force - the renderer's
/// default (or, for colors and fonts, the bit theme's) on the first apply, and
/// whatever an earlier instance set afterwards - so a partial instance
/// overrides only what it sets. Assign to the component's <c>Options</c>
/// parameter. Assigning a new instance re-applies the options (and re-lays-out
/// the timeline).
/// </summary>
public class BitResourceTimelineOptions
{
    /// <summary>
    /// Minimum height of each resource row, in pixels. Rows grow automatically
    /// when overlapping allocations stack taller than this so bars stay inside
    /// the row with the same top/bottom padding a single default-height bar has.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ResourceHeight { get; set; }

    /// <summary>Height of the time axis (both date and hour rows), in pixels.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? TimeAxisHeight { get; set; }

    /// <summary>Width of the resource axis (the left label column), in pixels.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ResourceAxisWidth { get; set; }

    /// <summary>
    /// Whether the user can drag (or keyboard-resize) the divider at the right
    /// edge of the resource column to change its width. Defaults to <c>true</c>.
    /// The starting width is <see cref="ResourceAxisWidth"/>; the committed
    /// width is reported via the component's <c>OnResourceAxisWidthChanged</c>
    /// callback so a host can persist it.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? ResourceAxisResizable { get; set; }

    /// <summary>
    /// Minimum width (in pixels) the resource column can be resized to.
    /// Defaults to 80. The viewport still keeps a 100px content area, so a
    /// very narrow host may not be able to reach this floor.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ResourceAxisMinWidth { get; set; }

    /// <summary>
    /// Maximum width (in pixels) the resource column can be resized to.
    /// <c>null</c> / unset means no host cap: the column still cannot grow
    /// past the viewport minus 100px of content area. Set an explicit value
    /// to stop earlier.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ResourceAxisMaxWidth { get; set; }

    /// <summary>Height of the date row within the time axis, in pixels.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? DateRowHeight { get; set; }

    /// <summary>Default bar height used when an allocation sets no explicit height, in pixels.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? BarHeight { get; set; }

    /// <summary>
    /// Vertical distance (in pixels) between allocation bars that overlap in time
    /// on the same resource row. Overlapping bars are stacked apart around the
    /// row's center line instead of being drawn on top of each other; this sets
    /// the gap between them (<c>0</c> stacks them touching). The row grows as
    /// needed so the stack keeps consistent top/bottom padding. Bars that
    /// overlap nothing stay centered in their row. Defaults to 2.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? BarMargin { get; set; }

    /// <summary>
    /// When <c>true</c> (the default), the gap between bars that overlap in time
    /// also covers the vertical room each bar's labels and icons need, so their
    /// text stays readable instead of being drawn over the neighbouring bar -
    /// <see cref="BarMargin"/> on its own only keeps the bars themselves apart.
    /// Rows grow to fit. The reserved room does not depend on the zoom level, so
    /// rows do not reflow while zooming. Set to <c>false</c> for the tighter
    /// stack of undecorated bars.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? StackLabelClearance { get; set; }

    /// <summary>
    /// When <c>true</c> (the default), bars are stacked into separate lanes when
    /// what they <i>paint</i> would collide - their labels, icons and delay bars,
    /// not just the bars - rather than only when their times overlap. Two bars a
    /// few minutes apart both draw their end/start labels into the gap between
    /// them, and a single lane cannot hold both legibly.
    /// <para>
    /// A collision is measured in pixels, so lane membership (and therefore row
    /// height) is recomputed when the zoom or the viewport width changes the
    /// horizontal scale. Bars too narrow to carry decorations at all (see
    /// <see cref="MinBarWidthForLabels"/>) claim no extra room, so zooming out
    /// collapses the stacks again rather than growing them.
    /// </para>
    /// Set to <c>false</c> to stack on a time overlap alone.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? StackOnLabelCollision { get; set; }

    /// <summary>Minimum drawn bar width so very short allocations stay visible, in pixels.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MinBarWidth { get; set; }

    /// <summary>
    /// Minimum main-bar width (in pixels) for its labels and icons to be drawn.
    /// Bars narrower than this skip decorations, keeping dense timelines readable.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MinBarWidthForLabels { get; set; }

    /// <summary>CSS font shorthand used for bar labels (for example <c>"11px sans-serif"</c>).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BarLabelFont { get; set; }

    /// <summary>Gap between a bar and its labels/icons, in pixels.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? BarLabelGap { get; set; }

    /// <summary>Default size of the square box a bar icon is drawn within, in pixels.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? BarIconSize { get; set; }

    /// <summary>
    /// CSS font shorthand for the day labels on the time axis (default <c>"12px sans-serif"</c>).
    /// The component measures day labels in this font to pin them while a day scrolls past, so
    /// it must match what is actually drawn.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DateLabelFont { get; set; }

    /// <summary>CSS font shorthand for the hour-of-day labels on the time axis (default <c>"12px sans-serif"</c>).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? HourLabelFont { get; set; }

    /// <summary>CSS font shorthand for leaf resource names in the resource column (default <c>"13px sans-serif"</c>).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ResourceLabelFont { get; set; }

    /// <summary>CSS font shorthand for group (parent) resource names (default <c>"bold 13px sans-serif"</c>).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ResourceGroupFont { get; set; }

    /// <summary>CSS font shorthand for the group expand/collapse chevron (default <c>"10px sans-serif"</c>).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ResourceChevronFont { get; set; }

    /// <summary>Horizontal gap between a group row's chevron and its name, in pixels.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ResourceChevronGap { get; set; }

    /// <summary>Pointer movement (in pixels) before a press becomes a marquee drag rather than a click.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? DragThreshold { get; set; }

    /// <summary>Extra pixels around a bar's drawn extent that still register as a hit.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? HitTolerance { get; set; }

    /// <summary>
    /// IANA time zone id the time axis is drawn in - day and hour boundaries and
    /// their labels are computed in this zone (for example <c>"UTC"</c> for
    /// aviation/Zulu time, or <c>"Europe/Berlin"</c>). Left unset it is the
    /// viewer's local zone; once a zone was applied, an empty string returns to the
    /// viewer's (<c>null</c> keeps the zone in force). Allocation times themselves
    /// are absolute instants and are unaffected; only the axis presentation changes.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TimeZone { get; set; }

    /// <summary>
    /// Adds a second hour row to the time axis, in UTC, drawn below the day
    /// labels and above the row that follows <see cref="TimeZone"/> (or the
    /// viewer's local zone). It is rendered exactly like that row - same ticks,
    /// same density, whole-hour labels - but on UTC's own hour boundaries, so
    /// its numbers sit shifted horizontally by any minutes in the axis zone's
    /// offset (half an hour's worth for <c>"Asia/Kolkata"</c> at +05:30). Under
    /// a whole-hour offset the two rows line up and differ only in their
    /// numbers, and with <see cref="TimeZone"/> set to <c>"UTC"</c> they read
    /// the same. The band under the day row is split evenly between them, so
    /// raise <see cref="TimeAxisHeight"/> to give them more room.
    /// Defaults to <c>false</c>.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? ShowUtcTime { get; set; }

    /// <summary>
    /// BCP 47 locale (for example <c>"de-DE"</c> or <c>"ja-JP"</c>) used to format
    /// day labels, tooltips and screen-reader announcements. Left unset it is the
    /// viewer's locale; once a locale was applied, an empty string returns to the
    /// viewer's (<c>null</c> keeps the locale in force). Hour ticks stay 24-hour
    /// unless <see cref="Hour12"/> is set.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Locale { get; set; }

    /// <summary>
    /// First day of the week (Sunday = 0). Left unset it is the week start of
    /// <see cref="Locale"/> (or of the viewer's locale) through <c>Intl.Locale</c>
    /// weekInfo where the browser has it, otherwise Monday.
    /// Applies to week-oriented banding when present; <c>PanByDaysAsync(7)</c>
    /// remains a 7-calendar-day step.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DayOfWeek? FirstDayOfWeek { get; set; }

    /// <summary>
    /// When <c>true</c>, hour-row labels use a 12-hour clock (for example
    /// <c>3 PM</c>). Tick positions stay on whole hours. Defaults to <c>false</c>
    /// (00–23).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Hour12 { get; set; }

    /// <summary>
    /// Weekdays shaded as non-working (0 = Sunday … 6 = Saturday). Empty /
    /// <c>null</c> draws no weekend wash. Visual only; does not change snap or
    /// scale.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int[]? NonWorkingDays { get; set; }

    /// <summary>
    /// Start of working hours, as minutes from local midnight (for example 540 for
    /// 09:00). <c>null</c> draws no off-hour bands. Visual only.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? WorkingHoursStart { get; set; }

    /// <summary>
    /// End of working hours, as minutes from local midnight (for example 1020 for
    /// 17:00). <c>null</c> draws no off-hour bands. Visual only.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? WorkingHoursEnd { get; set; }

    /// <summary>
    /// Maximum stacking lanes per overlapping cluster. <c>null</c> or <c>0</c>
    /// is unlimited (today's behaviour). Extra bars are hidden and a <c>+N</c>
    /// label is drawn at the cluster's trailing edge; clicking it selects them,
    /// and hovering it lists what is hidden (requires
    /// <see cref="ShowTooltips"/>). Row height is capped at the max-lane stack.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxStackLanes { get; set; }

    /// <summary>
    /// When <c>true</c>, <c>Delete</c> / <c>Backspace</c> on a focused, editable
    /// timeline asks <c>OnAllocationsDeleting</c> then removes the selected (or
    /// focused) bars. Defaults to <c>false</c>.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? AllowDelete { get; set; }

    /// <summary>
    /// Horizontal scale in pixels per hour. <c>null</c> auto-fits exactly one day
    /// to the viewport width. An explicit value is clamped to
    /// <see cref="MinPixelsPerHour"/>/<see cref="MaxPixelsPerHour"/>. Runtime zoom
    /// (the component's zoom methods) overrides this until reset.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? PixelsPerHour { get; set; }

    /// <summary>Lower bound for the scale when zooming, in pixels per hour.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? MinPixelsPerHour { get; set; }

    /// <summary>Upper bound for the scale when zooming, in pixels per hour.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? MaxPixelsPerHour { get; set; }

    /// <summary>
    /// Centers the current time in the view as soon as the first data load is
    /// laid out, so a timeline meant to open "at now" does not need a
    /// <c>GoToTodayAsync</c> call after rendering. The "now" line ends up in the
    /// middle of the content area, exactly as that method would leave it, but
    /// without the scroll animation. Only the first load is affected: later
    /// ones leave the viewport where the user left it (see
    /// <see cref="PreserveScrollOnReload"/>). Has no effect when the current
    /// time falls outside the loaded range. Defaults to <c>false</c>.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? AutoScrollToNow { get; set; }

    /// <summary>
    /// Keeps a data reload showing what it was showing - the time at the left
    /// edge of the content area and the row at the top - instead of letting a
    /// changed time range, scale or row list move the view. The view is restored
    /// by time and resource id rather than by pixel offset, so it survives a
    /// reload that shifts the overall range or re-orders the rows; if the
    /// anchored row is gone entirely, the vertical position is left as it was.
    /// Defaults to <c>false</c>.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? PreserveScrollOnReload { get; set; }

    /// <summary>
    /// How often (in milliseconds) the "now" indicator is repainted so it keeps
    /// up with the wall clock on a timeline nobody is interacting with.
    /// Defaults to 60000 (once a minute); lower it when the timeline is zoomed
    /// in far enough that a minute is a visible distance. Repaints are skipped
    /// while the tab is hidden and whenever the line would land on the same
    /// pixel, so a short interval costs nothing on a zoomed-out view. Set to
    /// <c>0</c> to stop the ticking entirely.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? NowLineRefreshMs { get; set; }

    /// <summary>
    /// When <c>true</c>, <c>PanByDaysAsync</c> lands the leading edge on a local
    /// midnight: a step of <c>1</c> goes to the start of the next calendar day
    /// in <see cref="TimeZone"/> (or the viewer's zone), and a step of <c>7</c>
    /// to the start of the day a week ahead. DST 23- and 25-hour days count as
    /// one step. When <c>false</c> (the default), each step is exactly 24 hours
    /// and the time of day at the leading edge stays put. A non-null
    /// <c>panToDayStart</c> argument on that method overrides this for the
    /// call.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? PanToDayStart { get; set; }

    /// <summary>
    /// Enables in-timeline editing: allocations can be dragged to move them in time
    /// (and, unless <see cref="AllowResourceChange"/> is <c>false</c>, onto another
    /// resource row) or grabbed near an edge to resize their start/end. Commits
    /// are reported via the component's <c>OnAllocationChanged</c> callback; a
    /// host can refuse one via <c>OnAllocationChanging</c>. Defaults to
    /// <c>false</c> (read-only).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Editable { get; set; }

    /// <summary>
    /// Snap increment (in minutes) applied to a move/resize while editing. Set to
    /// <c>0</c> for continuous (unsnapped) editing. Defaults to 15.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? EditSnapMinutes { get; set; }

    /// <summary>
    /// When <c>true</c> (the default), edit snaps land on wall-clock multiples of
    /// <see cref="EditSnapMinutes"/> from local midnight in <see cref="TimeZone"/>
    /// (DST 23- and 25-hour days have fewer or more snap points). Set
    /// <c>false</c> to keep the previous Unix-epoch grid.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? SnapToTimeZone { get; set; }

    /// <summary>
    /// Grab zone (in pixels) at each end of a bar within which a drag resizes
    /// that edge rather than moving the whole bar. Defaults to 6.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? EditResizeHandlePx { get; set; }

    /// <summary>
    /// Minimum allocation duration (in minutes) a resize can produce, so a bar
    /// cannot be shrunk to nothing. Defaults to 5.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? EditMinDurationMinutes { get; set; }

    /// <summary>
    /// Whether a move drag may reassign an allocation to a different resource row.
    /// Defaults to <c>true</c>. Only has an effect while <see cref="Editable"/> is set.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? AllowResourceChange { get; set; }

    /// <summary>
    /// Whether two unlocked bars on the same resource may occupy overlapping
    /// time. <c>true</c> (the default) stacks them as today. When <c>false</c>,
    /// a move or resize that would overlap another unlocked bar on that row is
    /// refused (touching end-to-start is still allowed). Locked bars are not
    /// occupancy. Only has an effect while <see cref="Editable"/> is set.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? AllowOverlap { get; set; }

    /// <summary>
    /// What a drag on empty content (no bar hit) does while
    /// <see cref="Editable"/> is set. <see cref="BitResourceTimelineEmptyDragAction.Marquee"/>
    /// (the default) rubber-bands. <see cref="BitResourceTimelineEmptyDragAction.Create"/>
    /// draws a new bar; the host must handle <c>OnAllocationCreating</c> and
    /// assign an id. Ctrl/Cmd-drag still marquees.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public BitResourceTimelineEmptyDragAction? EmptyDragAction { get; set; }

    /// <summary>
    /// Whether hovering a bar (mouse/pen) shows a tooltip. The tooltip text is the
    /// allocation's <see cref="BitResourceTimelineAllocation.Tooltip"/> when set,
    /// otherwise a default built from its labels, resource name and time range.
    /// Hovering a <c>+N</c> overflow marker (see <see cref="MaxStackLanes"/>)
    /// lists the bars hidden behind it. Defaults to <c>true</c>.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? ShowTooltips { get; set; }

    /// <summary>
    /// Delay (in milliseconds) after the pointer settles on a bar before its
    /// tooltip appears. Defaults to 300. Ignored when <see cref="ShowTooltips"/>
    /// is <c>false</c>.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? TooltipDelayMs { get; set; }

    /// <summary>
    /// On-demand loading: how many viewport-widths of extra time to fetch on each
    /// side of the visible range when requesting a window (see the component's
    /// <c>LoadAllocationsAsync</c> callback). Larger values fetch more per request
    /// but refetch less often. Defaults to 1.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? WindowBufferFactor { get; set; }

    /// <summary>
    /// On-demand loading: refetch once scrolling/zoom brings the visible range
    /// within this many viewport-widths of the loaded window's edge. Defaults to
    /// 0.25. Should be smaller than <see cref="WindowBufferFactor"/>.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? WindowRefetchThreshold { get; set; }

    /// <summary>
    /// On-demand loading: milliseconds to wait after the last scroll before
    /// checking whether a new window is needed, coalescing bursts. Defaults to 150.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? WindowDebounceMs { get; set; }

    /// <summary>
    /// Which renderer paints the timeline:
    /// <see cref="BitResourceTimelineRendererType.Canvas"/> (the default),
    /// <see cref="BitResourceTimelineRendererType.Svg"/> or
    /// <see cref="BitResourceTimelineRendererType.Html"/>. All renderers share
    /// the same engine, so data, interaction and events behave identically.
    /// Assigning a new Options instance with a different value switches the
    /// renderer at runtime.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public BitResourceTimelineRendererType? Renderer { get; set; }

    /// <summary>Color overrides. Individual colors left <c>null</c> keep their defaults.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public BitResourceTimelineColors? Colors { get; set; }

    /// <summary>
    /// Colors the content pane under the grid and bars: column stripes (alternating
    /// days, hours, weeks or shifts), row stripes, per-resource colors and explicit
    /// time bands. <c>null</c> leaves whatever surface was applied before, the same
    /// way other options do; assign an empty
    /// <see cref="BitResourceTimelineSurface"/> to clear it. The non-working
    /// wash, when enabled, paints over this.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public BitResourceTimelineSurface? Surface { get; set; }

    /// <summary>
    /// Colors the vertical time lines and the horizontal resource lines. <c>null</c>
    /// leaves whatever was applied before; assign an empty
    /// <see cref="BitResourceTimelineAxisLines"/> to clear it. Lines left
    /// uncolored keep <see cref="BitResourceTimelineColors.Grid"/>.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public BitResourceTimelineAxisLines? AxisLines { get; set; }
}
