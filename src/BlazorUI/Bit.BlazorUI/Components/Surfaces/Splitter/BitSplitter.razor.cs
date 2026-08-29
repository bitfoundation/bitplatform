using System.Globalization;

namespace Bit.BlazorUI;

/// <summary>
/// The BitSplitter component divides a container into two adjustable sections, either horizontally or vertically. Users can resize these sections by dragging the divider.
/// </summary>
public partial class BitSplitter : BitComponentBase
{
    private bool _isDragging;
    private string? _controllerId;
    private double? _percentBeforeCollapse;
    private ElementReference _gutterRef;
    private ElementReference _firstPanelRef;
    private ElementReference _secondPanelRef;
    private DotNetObjectReference<BitSplitter>? _dotnetObj;

    // What the JavaScript side was last told, so an update is only sent when something it acts on has
    // actually changed rather than after every render of the page around the splitter.
    private BitSplitterJsOptions? _jsOptions;

    private readonly record struct BitSplitterJsOptions(bool Vertical,
                                                        bool Disabled,
                                                        bool Collapsible,
                                                        bool Collapsed,
                                                        int CollapsedSize,
                                                        int KeyboardStep,
                                                        bool ResetOnDoubleClick,
                                                        bool NotifyResize,
                                                        double? Percent,
                                                        string? PersistKey,
                                                        bool PersistSession);



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Custom CSS classes for different parts of the BitSplitter.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSplitterClassStyles? Classes { get; set; }

    /// <summary>
    /// Whether the first panel is currently collapsed. It can be bound, so a collapse the user carries out
    /// on the gutter is reported back to the page.
    /// </summary>
    /// <remarks>
    /// A collapsed panel keeps its content in the DOM and is folded down to <see cref="CollapsedSize"/>,
    /// ignoring the minimum size it would otherwise hold. Expanding it puts it back where it was.
    /// </remarks>
    [Parameter, ResetClassBuilder, TwoWayBound]
    public bool Collapsed { get; set; }

    /// <summary>
    /// The size, in pixels, the first panel is folded down to while it is collapsed.
    /// <br />
    /// The default value is <strong>0</strong>.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public int CollapsedSize { get; set; }

    /// <summary>
    /// Lets the first panel be collapsed: pressing Enter on the gutter folds it away and opens it again,
    /// dragging the gutter close enough to the start of the splitter snaps it shut, and
    /// <see cref="Collapse"/> / <see cref="Expand"/> / <see cref="ToggleCollapse"/> do the same from code.
    /// </summary>
    [Parameter] public bool Collapsible { get; set; }

    /// <summary>
    /// The content for the first panel.
    /// </summary>
    [Parameter] public RenderFragment? FirstPanel { get; set; }

    /// <summary>
    /// The initial size of the first panel in pixels.
    /// </summary>
    /// <remarks>
    /// It is what the panel starts at; from the first drag on, the split is held as a percentage in
    /// <see cref="Percent"/>, which takes precedence over this and over <see cref="SecondPanelSize"/>.
    /// <see cref="ResetSize"/> gives this parameter the layout back.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public int? FirstPanelSize { get; set; }

    /// <summary>
    /// The max size of first panel in pixels.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public int? FirstPanelMaxSize { get; set; }

    /// <summary>
    /// The min size of first panel in pixels.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public int? FirstPanelMinSize { get; set; }

    /// <summary>
    /// The icon for the BitSplitter gutter using <see cref="BitIconInfo"/> for external icon library support.
    /// Takes precedence over <see cref="GutterIconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="GutterIconName"/> instead.
    /// When assigning a plain <see cref="string"/> to this property, it is converted to a <see cref="BitIconInfo"/> instance and
    /// treated as the raw CSS class name(s) for the external icon (for example, <c>"fa fa-home"</c>), not as a Fluent UI icon name.
    /// To render built-in Fluent UI icons, use <see cref="GutterIconName"/> instead; passing a Fluent icon name as a string to
    /// <see cref="GutterIcon"/> will compile but will not render a Fluent icon unless you also configure
    /// <see cref="BitIconInfo.BaseClass"/> and/or <see cref="BitIconInfo.Prefix"/> for a custom icon set.
    /// </remarks>
    [Parameter]
    public BitIconInfo? GutterIcon { get; set; }

    /// <summary>
    /// The name of the built-in Fluent UI icon to render in the BitSplitter gutter.
    /// Ignored when <see cref="GutterIcon"/> is also set.
    /// </summary>
    /// <remarks>
    /// The icon name should be from the Fluent UI icon set (e.g., <c>BitIconName.GripperDotsVertical</c>).
    /// For external icon libraries, use <see cref="GutterIcon"/> instead,
    /// where string values are interpreted as CSS class name(s)
    /// for the external icon rather than as Fluent UI icon identifiers.
    /// </remarks>
    [Parameter]
    public string? GutterIconName { get; set; }

    /// <summary>
    /// The size of BitSplitter gutter in pixels.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public int? GutterSize { get; set; }

    /// <summary>
    /// The custom content of the gutter, in place of the icon or of the default grip indicator.
    /// </summary>
    /// <remarks>
    /// The gutter is the separator itself - the element the pointer drags and the keyboard moves - so what
    /// goes in here is decoration rather than a control: a focusable element inside it would be a second tab
    /// stop on something a screen reader reports as a single separator.
    /// </remarks>
    [Parameter] public RenderFragment? GutterTemplate { get; set; }

    /// <summary>
    /// How far, in pixels, one press of an arrow key on the gutter moves the split.
    /// <br />
    /// The default value is <strong>10</strong>.
    /// </summary>
    /// <remarks>
    /// Page Up and Page Down, and an arrow key held with Shift, move the gutter ten of these steps at a
    /// time; Home and End take it all the way to the smallest and the largest size the panels allow.
    /// </remarks>
    [Parameter] public int KeyboardStep { get; set; } = 10;

    /// <summary>
    /// Keeps the gutter from resetting the splitter to the sizes its parameters declare when it is
    /// double-clicked.
    /// </summary>
    [Parameter] public bool NoResetOnDoubleClick { get; set; }

    /// <summary>
    /// The callback invoked when the first panel is collapsed or expanded.
    /// </summary>
    [Parameter] public EventCallback<bool> OnCollapsedChange { get; set; }

    /// <summary>
    /// The callback invoked continuously while the gutter is being dragged, with the new share of the
    /// splitter the first panel takes up, as a percentage.
    /// </summary>
    /// <remarks>
    /// It is coalesced to one call per animation frame. Leave it unset where only the final position
    /// matters: a splitter with no handler for it makes no interop call at all while it is being dragged.
    /// </remarks>
    [Parameter] public EventCallback<double> OnResize { get; set; }

    /// <summary>
    /// The callback invoked when a resize has finished, with the share of the splitter the first panel
    /// ended up taking, as a percentage.
    /// </summary>
    [Parameter] public EventCallback<double> OnResizeEnd { get; set; }

    /// <summary>
    /// The callback invoked when a resize starts, with the share of the splitter the first panel takes up
    /// at that moment, as a percentage.
    /// </summary>
    [Parameter] public EventCallback<double> OnResizeStart { get; set; }

    /// <summary>
    /// The key the splitter remembers its position under, so that a reader who has moved the gutter finds
    /// it where they left it the next time the page is opened. Leaving it unset remembers nothing.
    /// </summary>
    /// <remarks>
    /// Both the position and whether the first panel was folded away are kept, in the browser's local
    /// storage unless <see cref="PersistInSessionStorage"/> asks for the session instead. What is restored
    /// is offered to the component the way a drag is: a splitter whose <see cref="Percent"/> the page holds
    /// one way keeps what the page gave it. The key has to be unique to the splitter within the origin -
    /// two splitters sharing one key share one position.
    /// </remarks>
    [Parameter] public string? PersistKey { get; set; }

    /// <summary>
    /// Keeps what <see cref="PersistKey"/> remembers in the browser's session storage rather than its local
    /// storage, so the position lasts as long as the tab and no longer.
    /// </summary>
    [Parameter] public bool PersistInSessionStorage { get; set; }

    /// <summary>
    /// The share of the splitter the first panel takes up, as a percentage between 0 and 100.
    /// </summary>
    /// <remarks>
    /// It is the layout of the splitter held in a single value that survives the container being resized,
    /// and it can be bound, so every drag, key press and collapse is reported back to the page and the page
    /// can drive the split itself. While it has a value it takes precedence over
    /// <see cref="FirstPanelSize"/> and <see cref="SecondPanelSize"/>; <see cref="ResetSize"/> clears it
    /// and hands the layout back to them.
    /// </remarks>
    [Parameter, ResetStyleBuilder, TwoWayBound]
    public double? Percent { get; set; }

    /// <summary>
    /// Keeps the splitter as it is: the gutter is still shown and still looks like itself, but it cannot be
    /// dragged or moved from the keyboard.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="BitComponentBase.IsEnabled"/>, which dims the whole splitter, a read-only one is a
    /// layout that is simply not up for negotiation. The public methods still work in both cases.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool ReadOnly { get; set; }

    /// <summary>
    /// The content for the second panel.
    /// </summary>
    [Parameter] public RenderFragment? SecondPanel { get; set; }

    /// <summary>
    /// The initial size of the second panel in pixels.
    /// </summary>
    /// <remarks>
    /// Ignored while <see cref="Percent"/> has a value, which is the case from the first drag on.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public int? SecondPanelSize { get; set; }

    /// <summary>
    /// The max size of second panel in pixels.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public int? SecondPanelMaxSize { get; set; }

    /// <summary>
    /// The min size of second panel in pixels.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public int? SecondPanelMinSize { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitSplitter.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitSplitterClassStyles? Styles { get; set; }

    /// <summary>
    /// Sets the orientation of BitSplitter to vertical, stacking the two panels instead of placing them
    /// side by side.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Vertical { get; set; }



    /// <summary>
    /// Collapses the first panel. Does nothing if it is already collapsed.
    /// </summary>
    /// <remarks>
    /// A call of its own is not turned away by <see cref="Collapsible"/>, which is about what the reader is
    /// allowed to do to the gutter - the page can always fold its own panel away. A one-way bound
    /// <see cref="Collapsed"/> still owns the state, so nothing happens there.
    /// </remarks>
    public Task Collapse() => SetCollapsed(true);

    /// <summary>
    /// Expands the first panel back to the size it had before it was collapsed.
    /// </summary>
    /// <remarks>
    /// Not turned away by <see cref="Collapsible"/>; see <see cref="Collapse"/>.
    /// </remarks>
    public Task Expand() => SetCollapsed(false);

    /// <summary>
    /// Collapses the first panel if it is expanded and expands it if it is collapsed.
    /// </summary>
    /// <remarks>
    /// Not turned away by <see cref="Collapsible"/>; see <see cref="Collapse"/>.
    /// </remarks>
    public Task ToggleCollapse() => SetCollapsed(Collapsed is false);

    /// <summary>
    /// Gives the focus to the gutter, which is the control a splitter is driven by.
    /// </summary>
    public ValueTask FocusAsync() => _gutterRef.FocusAsync();

    /// <summary>
    /// Gives the focus to the gutter without scrolling it into view.
    /// </summary>
    public ValueTask FocusAsync(bool preventScroll) => _gutterRef.FocusAsync(preventScroll);

    /// <summary>
    /// Moves the split so that the first panel takes up the given share of the splitter, as a percentage
    /// between 0 and 100. The value is still held to the minimum and maximum sizes of both panels.
    /// </summary>
    public async Task SetPercent(double percent)
    {
        if (await AssignPercent(Math.Clamp(percent, 0, 100)) is false) return;

        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Clears <see cref="Percent"/> and hands the layout back to <see cref="FirstPanelSize"/> and
    /// <see cref="SecondPanelSize"/> - which is what a double-click on the gutter does.
    /// </summary>
    /// <remarks>
    /// A splitter whose <see cref="Percent"/> the page owns one way is not reset: the position is the
    /// page's to give up, and the panels stay where it put them.
    /// </remarks>
    public async Task ResetSize()
    {
        // The assignment is asked for first: clearing the sizes off the element ahead of a refusal would
        // take the layout away from a splitter that is going to go on holding the same position.
        if (await AssignPercent(null) is false) return;

        await SyncJsSize();

        await InvokeAsync(StateHasChanged);
    }



    [JSInvokable(nameof(HandleResizeStart))]
    public async Task HandleResizeStart(double percent)
    {
        _isDragging = true;
        ClassBuilder.Reset();

        await OnResizeStart.InvokeAsync(percent);

        await InvokeAsync(StateHasChanged);
    }

    [JSInvokable(nameof(HandleResize))]
    public Task HandleResize(double percent) => OnResize.InvokeAsync(percent);

    [JSInvokable(nameof(HandleResizeEnd))]
    public async Task HandleResizeEnd(double percent, bool collapsed)
    {
        _isDragging = false;
        ClassBuilder.Reset();

        // A drag that ended close enough to the start of the splitter is a collapse rather than a very small
        // panel, and the size it would have had is kept so that expanding it again puts it back there.
        if (Collapsible && collapsed != Collapsed)
        {
            if (collapsed)
            {
                _percentBeforeCollapse = Percent;
            }

            if (await AssignCollapsed(collapsed))
            {
                await OnCollapsedChange.InvokeAsync(collapsed);
            }
        }

        // Whatever was dragged is only kept where the component is free to keep it. A splitter whose
        // position the page owns one way, and one whose panel this drag folded away instead, both end up
        // somewhere other than where the pointer left the panels - so they are put back in step with the
        // position that did win rather than left showing a drag nothing acted on.
        var accepted = collapsed is false && await AssignPercent(percent);

        if (accepted is false)
        {
            await SyncJsSize();
        }

        await OnResizeEnd.InvokeAsync(percent);

        await InvokeAsync(StateHasChanged);
    }

    [JSInvokable(nameof(HandleResizeCancel))]
    public async Task HandleResizeCancel()
    {
        _isDragging = false;
        ClassBuilder.Reset();

        await InvokeAsync(StateHasChanged);
    }

    [JSInvokable(nameof(HandleToggleCollapse))]
    public Task HandleToggleCollapse() => SetCollapsed(Collapsed is false);

    [JSInvokable(nameof(HandleReset))]
    public Task HandleReset() => ResetSize();

    [JSInvokable(nameof(HandleRestore))]
    public async Task HandleRestore(double? percent, bool collapsed)
    {
        // What was remembered is offered rather than imposed: a page holding either of these one way keeps
        // what it declared, exactly as it would against a drag.
        if (percent.HasValue)
        {
            await AssignPercent(Math.Clamp(percent.Value, 0, 100));
        }

        if (collapsed != Collapsed)
        {
            if (collapsed) _percentBeforeCollapse = Percent;

            if (await AssignCollapsed(collapsed))
            {
                await OnCollapsedChange.InvokeAsync(collapsed);
            }
        }

        await InvokeAsync(StateHasChanged);
    }



    internal string _FirstPanelId => $"{_Id}-fpn";
    internal string _SecondPanelId => $"{_Id}-spn";

    // A gutter nobody can move is not a control any more, so it leaves the tab order and reports itself as
    // disabled rather than standing there as a stop that answers to nothing.
    private bool _IsInteractive => IsEnabled && ReadOnly is false;

    // The position the separator reports. It is only rendered from here once the page owns a value for it;
    // before that the setup call measures the panels and writes it onto the element itself, so a splitter
    // that has never been sized is not left claiming a position it does not have. A folded panel is not at
    // that position either - it is at its collapsed size - so the measurement is left to speak for it.
    private double? _ValueNow => Percent.HasValue && Collapsed is false
                               ? Math.Round(Math.Clamp(Percent.Value, 0, 100), 2)
                               : null;



    protected override string RootElementClass => "bit-spl";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Vertical ? "bit-spl-vrt" : string.Empty);

        ClassBuilder.Register(() => ReadOnly ? "bit-spl-rdo" : string.Empty);

        ClassBuilder.Register(() => Collapsed ? "bit-spl-col" : string.Empty);

        ClassBuilder.Register(() => _isDragging ? "bit-spl-drg" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => GutterSize.HasValue ? $"--gutter-size:{Math.Max(0, GutterSize.Value)}px" : string.Empty);

        // The size of the first panel is the flex basis of a flex item, so one variable carries the width of
        // a splitter laid out in a row and the height of one laid out in a column, and a share given as a
        // percentage keeps its proportions while the container is resized. The matching -grow variable is
        // what takes the panel off the equal split it starts at.
        StyleBuilder.Register(() => Percent.HasValue
                                  ? $"--first-panel:{Css(Math.Clamp(Percent.Value, 0, 100))}%;--first-panel-grow:0"
                                  : FirstPanelSize.HasValue
                                      ? $"--first-panel:{Math.Max(0, FirstPanelSize.Value)}px;--first-panel-grow:0"
                                      : string.Empty);
        StyleBuilder.Register(() => FirstPanelMaxSize.HasValue ? $"--first-panel-max:{Math.Max(0, FirstPanelMaxSize.Value)}px" : string.Empty);
        StyleBuilder.Register(() => FirstPanelMinSize.HasValue ? $"--first-panel-min:{Math.Max(0, FirstPanelMinSize.Value)}px" : string.Empty);

        // A splitter driven by the share of its first panel has nothing left to pin the second one with: the
        // second panel takes whatever is left over, which is what keeps the two of them adding up to the
        // splitter however wide it is.
        StyleBuilder.Register(() => Percent.HasValue is false && SecondPanelSize.HasValue
                                  ? $"--second-panel:{Math.Max(0, SecondPanelSize.Value)}px;--second-panel-grow:0"
                                  : string.Empty);
        StyleBuilder.Register(() => SecondPanelMaxSize.HasValue ? $"--second-panel-max:{Math.Max(0, SecondPanelMaxSize.Value)}px" : string.Empty);
        StyleBuilder.Register(() => SecondPanelMinSize.HasValue ? $"--second-panel-min:{Math.Max(0, SecondPanelMinSize.Value)}px" : string.Empty);

        StyleBuilder.Register(() => CollapsedSize > 0 ? $"--collapsed-size:{Math.Max(0, CollapsedSize)}px" : string.Empty);

        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _dotnetObj = DotNetObjectReference.Create(this);

            var options = CurrentJsOptions();
            _jsOptions = options;

            _controllerId = await _js.BitSplitterSetup(_dotnetObj,
                                                       RootElement,
                                                       _firstPanelRef,
                                                       _gutterRef,
                                                       _secondPanelRef,
                                                       options.Vertical,
                                                       options.Disabled,
                                                       options.Collapsible,
                                                       options.Collapsed,
                                                       options.CollapsedSize,
                                                       options.KeyboardStep,
                                                       options.ResetOnDoubleClick,
                                                       options.NotifyResize,
                                                       options.Percent,
                                                       options.PersistKey,
                                                       options.PersistSession);
        }
        else if (_controllerId.HasValue())
        {
            var options = CurrentJsOptions();

            if (_jsOptions != options)
            {
                _jsOptions = options;

                await _js.BitSplitterUpdate(_controllerId,
                                            options.Vertical,
                                            options.Disabled,
                                            options.Collapsible,
                                            options.Collapsed,
                                            options.CollapsedSize,
                                            options.KeyboardStep,
                                            options.ResetOnDoubleClick,
                                            options.NotifyResize,
                                            options.Percent,
                                            options.PersistKey,
                                            options.PersistSession);
            }
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        if (_dotnetObj is not null)
        {
            // The JavaScript side owns the listeners that hold the .NET reference, so it is told to drop them
            // first; whatever that call answers, the reference itself is released here so it is never left
            // registered against a component that is gone.
            try
            {
                if (_controllerId.HasValue())
                {
                    await _js.BitSplitterDispose(_controllerId);
                }
            }
            catch (JSException) { }
            catch (JSDisconnectedException) { }
            catch (ObjectDisposedException) { }
            catch (OperationCanceledException) { }
            finally
            {
                _dotnetObj.Dispose();
            }

            _dotnetObj = null;
        }

        await base.DisposeAsync(disposing);
    }



    private BitSplitterJsOptions CurrentJsOptions()
        => new(Vertical,
               _IsInteractive is false,
               Collapsible,
               Collapsed,
               Math.Max(0, CollapsedSize),
               Math.Max(1, KeyboardStep),
               NoResetOnDoubleClick is false,
               OnResize.HasDelegate,
               Percent,
               PersistKey,
               PersistInSessionStorage);

    private async Task SetCollapsed(bool value)
    {
        if (Collapsed == value) return;

        if (value)
        {
            _percentBeforeCollapse = Percent;
        }

        if (await AssignCollapsed(value) is false) return;

        // Expanding puts the panel back where it was rather than at the equal split it would fall back to,
        // which is what makes a collapse something the reader can undo.
        if (value is false)
        {
            if (_percentBeforeCollapse.HasValue)
            {
                await AssignPercent(_percentBeforeCollapse);
            }

            // The panel that comes back has to come back to the position the component holds rather than to
            // whatever the drag that folded it away left on the element.
            await SyncJsSize();
        }

        await OnCollapsedChange.InvokeAsync(value);

        await InvokeAsync(StateHasChanged);
    }

    // The inline properties a drag wrote onto the root are the JavaScript side's own copy of the layout, and
    // a render whose style attribute does not change leaves them standing - so whenever the component has
    // settled on something other than what was dragged, it says so.
    private async Task SyncJsSize()
    {
        if (_controllerId.HasNoValue()) return;

        try
        {
            await _js.BitSplitterSync(_controllerId, Percent);
        }
        catch (JSException) { }
        catch (JSDisconnectedException) { }
    }

    private static string Css(double value) => value.ToString(CultureInfo.InvariantCulture);
}
