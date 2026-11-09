using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Bit.Bheadless;

/// <summary>
/// A headless Splitter: resizable panels with handles between them (WAI-ARIA Window Splitter pattern).
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>Any number of panels</b> — <see cref="BitBhSplitterPanel"/>s with a <see cref="BitBhSplitterHandle"/> between each pair; splitters nest (a vertical one in a panel of a horizontal one).</item>
///   <item><b>Sizes in percent</b> — <c>@bind-Sizes</c>, <see cref="DefaultSizes"/> or each panel's <c>DefaultSize</c>; <see cref="OnSizesChange"/> while resizing, <see cref="OnSizesCommit"/> once it ends (pointer up, a key).</item>
///   <item><b>Constraints</b> — a panel's <c>MinSize</c> / <c>MaxSize</c>; a <c>Collapsible</c> panel collapses to its <c>CollapsedSize</c> when dragged past half its minimum, or with Enter.</item>
///   <item><b>Keyboard</b> — every handle is a focusable <c>role="separator"</c>: arrows resize by <see cref="KeyboardStep"/> (Shift: <see cref="LargeKeyboardStep"/>), Home / End go to the smallest / largest size of the panel before it, Enter collapses or restores a collapsible neighbor; Left / Right mirror in RTL.</item>
///   <item><b>Pointer</b> — drag a handle (pointer capture, touch too); Escape while dragging puts the panels back.</item>
///   <item><b>Persistence</b> — <see cref="AutoSaveId"/> keeps the layout in <c>localStorage</c>.</item>
/// </list>
///
/// <para>
/// Panels are laid out with flex (inline <c>display: flex</c> on the root, <c>flex: &lt;size&gt; 1 0px</c> on each
/// panel), so the sizes share the space the handles leave: give the root a size (a vertical splitter a height)
/// and the handles their thickness. When a resize moves past a panel's minimum, the next panels give way in turn.
/// </para>
///
/// <para>
/// <b>Styling hooks:</b> <c>data-orientation</c> on every part, <c>data-disabled</c>, <c>data-dragging</c> on the root
/// and the dragged handle, <c>data-state="expanded|collapsed"</c> / <c>data-collapsed</c> on panels.
/// </para>
///
/// <code>
/// &lt;BitBhSplitter @bind-Sizes="sizes" class="splitter"&gt;
///     &lt;BitBhSplitterPanel MinSize="15" Collapsible="true"&gt;Sidebar&lt;/BitBhSplitterPanel&gt;
///     &lt;BitBhSplitterHandle class="handle" aria-label="Resize sidebar" /&gt;
///     &lt;BitBhSplitterPanel MinSize="30"&gt;Editor&lt;/BitBhSplitterPanel&gt;
/// &lt;/BitBhSplitter&gt;
/// </code>
/// </summary>
public class BitBhSplitter : BitBhComponentBase, IAsyncDisposable
{
    private const double Epsilon = 0.0001;

    private readonly BitBhCollection<BitBhSplitterPanel> _panels = new(BitBhId.New("bit-bh-split-panels"));
    private readonly BitBhCollection<BitBhSplitterHandle> _handles = new(BitBhId.New("bit-bh-split-handles"));
    // Uncontrolled sizes, once the user (or a saved layout) set them. Until then the layout is the default one,
    // computed afresh, so panels that join one by one on the first render all get their default share.
    private Dictionary<BitBhSplitterPanel, double>? _userSizes;
    private BitBhSplitterHandle? _dragHandle;
    private double[]? _dragStart;
    private double[]? _dragLast;
    private int _handle;
    private bool _attachTried;
    private bool _syncing;
    private bool _disposed;
    private bool _loaded;
    private string? _sentOptions;
    private DotNetObjectReference<PointerCallbacks>? _callbacksRef;

    [CascadingParameter]
    private BitBhDirection CascadedDir { get; set; }

    /// <summary>
    /// The panel sizes in percent (one per panel, summing to 100). Supplying it makes the splitter controlled
    /// (use <c>@bind-Sizes</c>). A list of the wrong length is ignored for the default layout.
    /// </summary>
    [Parameter]
    public IReadOnlyList<double>? Sizes { get; set; }

    /// <summary>Two-way binding callback for <see cref="Sizes"/>.</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<double>> SizesChanged { get; set; }

    /// <summary>
    /// Initial sizes in percent when uncontrolled (one per panel). Without them each panel's
    /// <see cref="BitBhSplitterPanel.DefaultSize"/> is used, and the panels without one share the rest.
    /// </summary>
    [Parameter]
    public IReadOnlyList<double>? DefaultSizes { get; set; }

    /// <summary>Fires whenever the sizes change - on every pointer move while dragging.</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<double>> OnSizesChange { get; set; }

    /// <summary>Fires when the user finishes resizing (pointer up, a key press) - the place to save the layout.</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<double>> OnSizesCommit { get; set; }

    /// <summary>
    /// Horizontal (default): the panels sit side by side and the handles are vertical bars (Left / Right resize).
    /// Vertical: the panels are stacked (Up / Down resize).
    /// </summary>
    [Parameter]
    public BitBhOrientation Orientation { get; set; } = BitBhOrientation.Horizontal;

    /// <summary>How many percentage points an arrow key resizes by. Default: 5.</summary>
    [Parameter]
    public double KeyboardStep { get; set; } = 5;

    /// <summary>How many percentage points Shift + an arrow key resizes by. Default: 20.</summary>
    [Parameter]
    public double LargeKeyboardStep { get; set; } = 20;

    /// <summary>Disables every handle (the panels keep their sizes).</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// Reading direction: a horizontal splitter lays its panels out from the right in RTL, and Left / Right
    /// mirror. Inherits from <see cref="BitBhDirectionProvider"/>; either renders <c>dir</c> on the root.
    /// </summary>
    [Parameter]
    public BitBhDirection? Dir { get; set; }

    /// <summary>
    /// Keeps the layout in <c>localStorage</c> under this id (one entry per set of panels, by their
    /// <see cref="BitBhSplitterPanel.Key"/> or position): it is saved on every commit and restored once the page
    /// is interactive.
    /// </summary>
    [Parameter]
    public string? AutoSaveId { get; set; }

    /// <summary>The panels and the handles between them.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    private BitBhDirection EffectiveDir => Dir ?? CascadedDir;

    /// <summary>The current sizes in percent, one per panel in order.</summary>
    public IReadOnlyList<double> CurrentSizes => Layout();

    internal BitBhOrientation SplitterOrientation => Orientation;

    internal bool SplitterDisabled => Disabled;

    internal string RootId => ComponentId;

    internal bool IsDragging(BitBhSplitterHandle handle) => ReferenceEquals(_dragHandle, handle);

    internal BitBhCollection<BitBhSplitterPanel> PanelCollection => _panels;

    internal BitBhCollection<BitBhSplitterHandle> HandleCollection => _handles;

    protected override void OnInitialized()
    {
        // Panels and handles join on their first render, one by one: the layout is laid out again with all of them.
        _panels.Changed += OnPartsChanged;
        _handles.Changed += OnPartsChanged;
    }

    private void OnPartsChanged()
    {
        if (!_disposed) StateHasChanged();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.OpenComponent<CascadingValue<BitBhSplitter>>(40);
        builder.AddComponentParameter(41, "Value", this);
        // Not fixed: the panels and handles follow the sizes.
        builder.AddComponentParameter(42, "IsFixed", false);
        builder.AddComponentParameter(43, "ChildContent", ChildContent);
        builder.CloseComponent();
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        // The direction decides which side the first panel is on, so a cascaded right-to-left one is written out
        // too: a provider sets no dir attribute of its own.
        if (Dir is { } dir) attrs["dir"] = dir.ToAttribute();
        else if (CascadedDir == BitBhDirection.Rtl) attrs["dir"] = "rtl";
        SetDataValue(attrs, "orientation", Orientation.ToAttribute());
        SetDataFlag(attrs, "disabled", Disabled);
        SetDataFlag(attrs, "dragging", _dragHandle is not null);
        // Functional: the panels share the root's main axis by their flex-grow.
        attrs["style"] = Orientation == BitBhOrientation.Vertical ? "display:flex;flex-direction:column" : "display:flex;flex-direction:row";
        return attrs;
    }

    // ── Runtime ──────────────────────────────────────────────────────────────

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await _panels.EnsureOrderAsync(Interop);
        await _handles.EnsureOrderAsync(Interop);
        await SyncAsync();
        if (!_loaded && !_disposed && _panels.Count > 0)
        {
            _loaded = true;
            await RestoreAsync();
        }
    }

    // Renders can arrive while the attach is in flight. One loop applies them in order, and a splitter disposed
    // meanwhile detaches what the in-flight call attached.
    private async Task SyncAsync()
    {
        if (_syncing) return;
        _syncing = true;
        try
        {
            while (_disposed is false)
            {
                var options = Orientation.ToAttribute();
                if (_handle == 0)
                {
                    if (_attachTried) break;
                    _attachTried = true;
                    _callbacksRef ??= DotNetObjectReference.Create(new PointerCallbacks(this));
                    _handle = await Interop.SplitterAttachAsync(_callbacksRef, ComponentId, RuntimeOptions());
                    _sentOptions = options;
                    continue;
                }
                if (options == _sentOptions) break;
                _sentOptions = options;
                await Interop.SplitterUpdateAsync(_handle, RuntimeOptions());
            }
        }
        finally
        {
            _syncing = false;
        }
        if (_disposed) await DetachAsync();
    }

    private Dictionary<string, object?> RuntimeOptions() => new()
    {
        ["orientation"] = Orientation.ToAttribute(),
    };

    private async Task DetachAsync()
    {
        if (_handle <= 0) return;
        var handle = _handle;
        _handle = 0;
        await Interop.SplitterDetachAsync(handle);
    }

    private string? StorageKey
    {
        get
        {
            if (string.IsNullOrEmpty(AutoSaveId)) return null;
            var panels = Panels();
            return $"bit-bh-splitter:{AutoSaveId}:" + string.Join(',', panels.Select((p, i) => p.Key ?? p.Id ?? i.ToString(CultureInfo.InvariantCulture)));
        }
    }

    private async Task RestoreAsync()
    {
        if (StorageKey is not { } key) return;
        var saved = await Interop.SplitterLoadAsync(key);
        var panels = Panels();
        if (_disposed || saved is null || saved.Length != panels.Count || saved.Any(s => !double.IsFinite(s))) return;
        var sizes = Validate(saved, panels);
        if (SameSizes(sizes, Layout())) return;
        await SetSizesAsync(sizes, commit: false);
    }

    // ── Panels and layout ────────────────────────────────────────────────────

    /// <summary>The panels in order: by their <see cref="BitBhSplitterPanel.Order"/>, then document order.</summary>
    internal IReadOnlyList<BitBhSplitterPanel> Panels()
    {
        var items = _panels.Items;
        if (items.All(p => p.Order is null)) return items;
        return items.Select((p, i) => (p, i)).OrderBy(x => x.p.Order ?? int.MaxValue).ThenBy(x => x.i).Select(x => x.p).ToArray();
    }

    private IReadOnlyList<double> Layout()
    {
        var panels = Panels();
        if (panels.Count == 0) return [];
        if (IsProvided(nameof(Sizes)))
            return Sizes is { } sizes && sizes.Count == panels.Count ? Validate(sizes, panels) : DefaultLayout(panels);
        if (_userSizes is null) return DefaultLayout(panels);

        // Panels that left gave their space up; panels that joined take their default size from the others.
        var joined = panels.Where(p => !_userSizes.ContainsKey(p)).ToArray();
        if (joined.Length == 0 && _userSizes.Count == panels.Count) return Validate(panels.Select(p => _userSizes[p]).ToArray(), panels);
        var share = 100d / panels.Count;
        var joinedTotal = joined.Sum(p => p.DefaultSize ?? share);
        var keptTotal = panels.Where(_userSizes.ContainsKey).Sum(p => _userSizes[p]);
        var scale = keptTotal > Epsilon ? Math.Max(0, 100 - joinedTotal) / keptTotal : 0;
        return Validate(panels.Select(p => _userSizes.TryGetValue(p, out var s) ? s * scale : p.DefaultSize ?? share).ToArray(), panels);
    }

    private double[] DefaultLayout(IReadOnlyList<BitBhSplitterPanel> panels)
    {
        if (DefaultSizes is { } defaults && defaults.Count == panels.Count) return Validate(defaults, panels);
        var given = panels.Where(p => p.DefaultSize is not null).Sum(p => p.DefaultSize!.Value);
        var rest = panels.Count(p => p.DefaultSize is null);
        var each = rest > 0 ? Math.Max(0, 100 - given) / rest : 0;
        return Validate(panels.Select(p => p.DefaultSize ?? each).ToArray(), panels);
    }

    /// <summary>
    /// Brings sizes within the panels' constraints and to a total of 100: the difference is shared by the panels
    /// that can still give or take; when the constraints can't add up to 100, the sizes are scaled.
    /// </summary>
    private static double[] Validate(IReadOnlyList<double> input, IReadOnlyList<BitBhSplitterPanel> panels)
    {
        var n = panels.Count;
        var sizes = new double[n];
        for (var i = 0; i < n; i++)
        {
            var requested = i < input.Count && double.IsFinite(input[i]) ? Math.Max(0, input[i]) : 0;
            sizes[i] = Constrain(panels[i], requested, requested, keyboard: false);
        }

        for (var pass = 0; pass <= n; pass++)
        {
            var diff = 100 - sizes.Sum();
            if (Math.Abs(diff) < Epsilon) return sizes;
            var open = Enumerable.Range(0, n)
                .Where(i => !panels[i].IsCollapsedAt(sizes[i]) && (diff > 0 ? sizes[i] < panels[i].EffectiveMax - Epsilon : sizes[i] > panels[i].EffectiveMin + Epsilon))
                .ToArray();
            if (open.Length == 0) break;
            var share = diff / open.Length;
            foreach (var i in open) sizes[i] = Math.Clamp(sizes[i] + share, panels[i].EffectiveMin, panels[i].EffectiveMax);
        }

        // Constraints that can't add up to 100 (every maximum too small): proportions win over constraints.
        var total = sizes.Sum();
        if (Math.Abs(total - 100) >= Epsilon)
        {
            for (var i = 0; i < n; i++) sizes[i] = total > Epsilon ? sizes[i] * 100 / total : 100d / n;
        }
        return sizes;
    }

    /// <summary>
    /// The size a panel takes when <paramref name="requested"/> is asked of it, coming from <paramref name="previous"/>.
    /// A collapsible panel asked for less than its minimum collapses past the halfway point (a drag, Home, Enter),
    /// or - for an arrow key - stops at the minimum first and collapses with the next step; a collapsed one opens
    /// straight to its minimum.
    /// </summary>
    private static double Constrain(BitBhSplitterPanel panel, double requested, double previous, bool keyboard)
    {
        var min = panel.EffectiveMin;
        var max = panel.EffectiveMax;
        if (panel.Collapsible && requested < min - Epsilon)
        {
            var collapsed = panel.EffectiveCollapsedSize;
            if (!keyboard) return requested < (collapsed + min) / 2 ? collapsed : min;
            if (previous <= collapsed + Epsilon) return requested > collapsed + Epsilon ? min : collapsed;
            return previous > min + Epsilon ? min : collapsed;
        }
        return Math.Clamp(requested, min, max);
    }

    /// <summary>
    /// Moves handle <paramref name="handleIndex"/> by <paramref name="delta"/> percentage points (positive: towards
    /// the end, growing the panel before it). The panel on the growing side grows up to its maximum; the panels on
    /// the other side give the space, the nearest first, each down to its minimum (or collapsed). A move that does
    /// not fit leaves the sizes as they were.
    /// </summary>
    private double[] Resize(IReadOnlyList<double> start, IReadOnlyList<BitBhSplitterPanel> panels, int handleIndex, double delta, bool keyboard)
    {
        var sizes = start.ToArray();
        var n = sizes.Length;
        if (n != panels.Count || handleIndex < 0 || handleIndex + 1 >= n || Math.Abs(delta) < Epsilon || !double.IsFinite(delta)) return sizes;

        var growIndex = delta > 0 ? handleIndex : handleIndex + 1;
        var target = Constrain(panels[growIndex], start[growIndex] + Math.Abs(delta), start[growIndex], keyboard);
        var need = target - start[growIndex];
        if (need <= Epsilon) return sizes;

        var taken = 0d;
        var order = delta > 0
            ? Enumerable.Range(handleIndex + 1, n - handleIndex - 1)
            : Enumerable.Range(0, handleIndex + 1).Reverse();
        foreach (var k in order)
        {
            var remaining = need - taken;
            if (remaining <= Epsilon) break;
            var next = Math.Min(sizes[k], Constrain(panels[k], sizes[k] - remaining, sizes[k], keyboard));
            taken += sizes[k] - next;
            sizes[k] = next;
        }
        if (taken <= Epsilon) return start.ToArray();

        var grown = Constrain(panels[growIndex], start[growIndex] + taken, start[growIndex], keyboard);
        if (Math.Abs(grown - start[growIndex] - taken) > Epsilon) return start.ToArray();
        sizes[growIndex] = grown;
        return sizes;
    }

    private static bool SameSizes(IReadOnlyList<double> a, IReadOnlyList<double> b)
        => a.Count == b.Count && a.Zip(b).All(x => Math.Abs(x.First - x.Second) < Epsilon);

    /// <summary>
    /// Applies new sizes: stores them when uncontrolled, reports them and - for a commit - fires
    /// <see cref="OnSizesCommit"/> and saves them.
    /// </summary>
    private async Task SetSizesAsync(IReadOnlyList<double> sizes, bool commit)
    {
        var panels = Panels();
        // Sizes laid out for other panels (one left or joined during a drag) are not applied.
        if (sizes.Count != panels.Count)
        {
            StateHasChanged();
            return;
        }
        var previous = Layout();
        var changed = !SameSizes(previous, sizes);
        if (changed)
        {
            for (var i = 0; i < panels.Count && i < previous.Count; i++)
            {
                if (!panels[i].IsCollapsedAt(previous[i]) && panels[i].IsCollapsedAt(sizes[i])) panels[i].RestoreSize = previous[i];
            }
            if (!IsProvided(nameof(Sizes)))
                _userSizes = panels.Select((p, i) => (p, i)).ToDictionary(x => x.p, x => sizes[x.i]);
            var list = sizes.ToArray();
            await SizesChanged.InvokeAsync(list);
            await OnSizesChange.InvokeAsync(list);
            for (var i = 0; i < panels.Count && i < previous.Count; i++)
            {
                var was = panels[i].IsCollapsedAt(previous[i]);
                var now = panels[i].IsCollapsedAt(sizes[i]);
                if (was != now) await panels[i].OnCollapsedChange.InvokeAsync(now);
            }
        }
        if (commit)
        {
            var list = sizes.ToArray();
            await OnSizesCommit.InvokeAsync(list);
            if (StorageKey is { } key) await Interop.SplitterSaveAsync(key, list);
        }
        StateHasChanged();
    }

    // ── Parts' state ─────────────────────────────────────────────────────────

    internal double SizeOf(BitBhSplitterPanel panel)
    {
        var panels = Panels();
        var index = IndexOf(panels, panel);
        var sizes = Layout();
        return index >= 0 && index < sizes.Count ? sizes[index] : 0;
    }

    private static int IndexOf(IReadOnlyList<BitBhSplitterPanel> panels, BitBhSplitterPanel panel)
    {
        for (var i = 0; i < panels.Count; i++)
            if (ReferenceEquals(panels[i], panel)) return i;
        return -1;
    }

    /// <summary>What a handle shows: the panel before it, its size and the range the handle can give it.</summary>
    internal BitBhSplitterHandleState? StateOf(BitBhSplitterHandle handle)
    {
        var index = _handles.IndexOf(handle);
        var panels = Panels();
        if (index < 0 || index + 1 >= panels.Count) return null;
        var sizes = Layout();
        var before = panels[index];
        var after = panels[index + 1];
        var now = sizes[index];
        // The panel before can shrink to its own minimum (or collapse), as far as the panel after can grow;
        // it can grow until every panel after the handle is at its minimum (or collapsed).
        var min = Math.Max(before.Collapsible ? before.EffectiveCollapsedSize : before.EffectiveMin, now - Math.Max(0, after.EffectiveMax - sizes[index + 1]));
        var room = 0d;
        for (var k = index + 1; k < panels.Count; k++)
            room += Math.Max(0, sizes[k] - (panels[k].Collapsible ? panels[k].EffectiveCollapsedSize : panels[k].EffectiveMin));
        var max = Math.Min(before.EffectiveMax, now + room);
        return new BitBhSplitterHandleState(index, before, after, now, Math.Min(min, now), Math.Max(max, now));
    }

    // ── Keyboard ─────────────────────────────────────────────────────────────

    internal async Task HandleKeyDownAsync(BitBhSplitterHandle handle, KeyboardEventArgs e)
    {
        if (Disabled || handle.IsHandleDisabled || StateOf(handle) is not { } state) return;
        var panels = Panels();
        var sizes = Layout();
        var step = e.ShiftKey ? LargeKeyboardStep : KeyboardStep;
        if (!(step > 0) || !double.IsFinite(step)) step = 5;
        var rtl = EffectiveDir == BitBhDirection.Rtl;
        var horizontal = Orientation == BitBhOrientation.Horizontal;

        double[] next;
        switch (e.Key)
        {
            case "ArrowRight" when horizontal:
                next = Resize(sizes, panels, state.Index, rtl ? -step : step, keyboard: true);
                break;
            case "ArrowLeft" when horizontal:
                next = Resize(sizes, panels, state.Index, rtl ? step : -step, keyboard: true);
                break;
            case "ArrowDown" when !horizontal:
                next = Resize(sizes, panels, state.Index, step, keyboard: true);
                break;
            case "ArrowUp" when !horizontal:
                next = Resize(sizes, panels, state.Index, -step, keyboard: true);
                break;
            case "Home":
                next = Resize(sizes, panels, state.Index, state.Min - state.Now, keyboard: false);
                break;
            case "End":
                next = Resize(sizes, panels, state.Index, state.Max - state.Now, keyboard: false);
                break;
            case "Enter":
                next = ToggleCollapse(sizes, panels, state);
                break;
            default:
                return;
        }
        // A key at the edge changes nothing, and there is nothing to commit.
        if (!SameSizes(next, sizes)) await SetSizesAsync(next, commit: true);
    }

    /// <summary>The panel Enter collapses or restores: the one before the handle when collapsible, else the one after.</summary>
    internal static BitBhSplitterPanel? CollapseTarget(BitBhSplitterHandleState state)
        => state.Before.Collapsible ? state.Before : state.After.Collapsible ? state.After : null;

    private double[] ToggleCollapse(IReadOnlyList<double> sizes, IReadOnlyList<BitBhSplitterPanel> panels, BitBhSplitterHandleState state)
    {
        if (CollapseTarget(state) is not { } panel) return sizes.ToArray();
        var before = ReferenceEquals(panel, state.Before);
        var index = before ? state.Index : state.Index + 1;
        var current = sizes[index];
        var target = panel.IsCollapsedAt(current)
            ? Math.Max(panel.EffectiveMin, panel.RestoreSize ?? panel.DefaultSize ?? panel.EffectiveMin)
            : panel.EffectiveCollapsedSize;
        // Growing the panel before the handle moves the handle forward; growing the one after moves it back.
        var delta = before ? target - current : current - target;
        return Resize(sizes, panels, state.Index, delta, keyboard: false);
    }

    // ── Programmatic ─────────────────────────────────────────────────────────

    /// <summary>Sets the sizes (one per panel, in percent); they are brought within the panels' constraints.</summary>
    public Task SetSizesAsync(IReadOnlyList<double> sizes)
    {
        var panels = Panels();
        if (sizes.Count != panels.Count) return Task.CompletedTask;
        return SetSizesAsync(Validate(sizes, panels), commit: true);
    }

    internal Task ResizePanelAsync(BitBhSplitterPanel panel, double size)
    {
        var panels = Panels();
        var index = IndexOf(panels, panel);
        if (index < 0 || panels.Count < 2) return Task.CompletedTask;
        var sizes = Layout();
        // The handle after the panel moves, or - for the last panel - the one before it.
        var next = index < panels.Count - 1
            ? Resize(sizes, panels, index, size - sizes[index], keyboard: false)
            : Resize(sizes, panels, index - 1, sizes[index] - size, keyboard: false);
        return SameSizes(next, sizes) ? Task.CompletedTask : SetSizesAsync(next, commit: true);
    }

    internal Task SetCollapsedAsync(BitBhSplitterPanel panel, bool collapsed)
    {
        if (!panel.Collapsible || panel.IsCollapsedAt(SizeOf(panel)) == collapsed) return Task.CompletedTask;
        var target = collapsed
            ? panel.EffectiveCollapsedSize
            : Math.Max(panel.EffectiveMin, panel.RestoreSize ?? panel.DefaultSize ?? panel.EffectiveMin);
        return ResizePanelAsync(panel, target);
    }

    // ── Pointer ──────────────────────────────────────────────────────────────

    /// <summary>
    /// The runtime's pointer callback. A separate object, not the splitter: a reference to the component would
    /// root it for the runtime and make its JS-invokable method part of its public API.
    /// </summary>
    private sealed class PointerCallbacks(BitBhSplitter splitter)
    {
        /// <summary>
        /// A drag of a handle: "start", "move" and "end" (or "cancel" on Escape), with the distance moved in
        /// percentage points (positive: towards the end). The runtime calls it by this name.
        /// </summary>
        [JSInvokable]
        public Task OnSplitterPointer(string phase, string handleId, double delta)
            => splitter.RunFromRuntimeAsync(() => splitter.HandlePointerAsync(phase, handleId, delta));
    }

    private async Task HandlePointerAsync(string phase, string handleId, double delta)
    {
        if (_disposed) return;
        if (phase == "start")
        {
            var handle = _handles.FindById(handleId);
            if (Disabled || handle is null || handle.IsHandleDisabled || StateOf(handle) is null) return;
            _dragHandle = handle;
            _dragStart = Layout().ToArray();
            _dragLast = _dragStart;
            StateHasChanged();
            return;
        }
        if (_dragHandle is null || _dragStart is null || StateOf(_dragHandle) is not { } state) return;

        var panels = Panels();
        var next = phase == "cancel" ? _dragStart : Resize(_dragStart, panels, state.Index, delta, keyboard: false);
        var moved = !SameSizes(next, _dragLast!);
        _dragLast = next;
        if (phase is "end" or "cancel")
        {
            var committed = phase == "end" && !SameSizes(next, _dragStart);
            _dragHandle = null;
            _dragStart = null;
            _dragLast = null;
            if (moved || committed) await SetSizesAsync(next, commit: committed);
            else StateHasChanged();
            return;
        }
        if (moved) await SetSizesAsync(next, commit: false);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        _panels.Changed -= OnPartsChanged;
        _handles.Changed -= OnPartsChanged;
        // An attach in flight is undone by SyncAsync once it returns.
        if (_syncing is false) await DetachAsync();
        _callbacksRef?.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>What a <see cref="BitBhSplitterHandle"/> controls: the panels on either side and the before panel's size range.</summary>
internal sealed record BitBhSplitterHandleState(int Index, BitBhSplitterPanel Before, BitBhSplitterPanel After, double Now, double Min, double Max);
