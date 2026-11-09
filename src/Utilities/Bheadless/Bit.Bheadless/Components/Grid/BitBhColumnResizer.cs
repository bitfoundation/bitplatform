using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Bit.Bheadless;

/// <summary>
/// A handle that resizes a column of a <see cref="BitBhGrid"/> or any table (React Aria's ColumnResizer): a focusable
/// <c>role="separator"</c> with the width in <c>aria-valuenow</c>. ← / → change the width by <see cref="Step"/> (Shift: five
/// steps), Home / End go to <see cref="Min"/> / <see cref="Max"/>, Enter restores <see cref="DefaultWidth"/>; a pointer drags it.
/// Put it in the column's header cell and give the column the width:
///
/// <code>
/// &lt;BitBhGridCell Header="true" style="@($"width:{nameWidth}px")"&gt;
///     Name
///     &lt;BitBhColumnResizer @bind-Width="nameWidth" Min="80" Max="400" aria-label="Resize Name column" class="resizer" /&gt;
/// &lt;/BitBhGridCell&gt;
/// </code>
/// <para><b>Styling hooks:</b> <c>data-resizing</c> while dragged, <c>data-orientation="vertical"</c>. The handle has
/// <c>touch-action: none</c> and <c>cursor: col-resize</c> unless styled otherwise.</para>
/// </summary>
public class BitBhColumnResizer : BitBhComponentBase, IAsyncDisposable
{
    private double _width = 150;
    private bool _resizing;
    private double? _lastWidth;
    private DotNetObjectReference<Callbacks>? _ref;
    private int _handle;
    private bool _attaching;
    private bool _disposed;

    [CascadingParameter]
    private BitBhDirection CascadedDir { get; set; }

    /// <summary>The column width in pixels. Supplying it makes the resizer controlled (use <c>@bind-Width</c>).</summary>
    [Parameter]
    public double Width { get; set; }

    /// <summary>Two-way binding callback for <see cref="Width"/>.</summary>
    [Parameter]
    public EventCallback<double> WidthChanged { get; set; }

    /// <summary>The width Enter restores, and the initial one when uncontrolled. Default: 150.</summary>
    [Parameter]
    public double DefaultWidth { get; set; } = 150;

    /// <summary>Fires when a resize ends (pointer up, a key press).</summary>
    [Parameter]
    public EventCallback<double> OnResizeEnd { get; set; }

    /// <summary>The smallest width. Default: 40.</summary>
    [Parameter]
    public double Min { get; set; } = 40;

    /// <summary>The largest width. Default: 1000.</summary>
    [Parameter]
    public double Max { get; set; } = 1000;

    /// <summary>Pixels per arrow key. Default: 10.</summary>
    [Parameter]
    public double Step { get; set; } = 10;

    /// <summary>Reading direction (a right-to-left column grows to the left). Inherits from <see cref="BitBhDirectionProvider"/>.</summary>
    [Parameter]
    public BitBhDirection? Dir { get; set; }

    /// <summary>Optional content (a grip).</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "span";

    /// <summary>The current width.</summary>
    public double CurrentWidth => IsProvided(nameof(Width)) ? Width : _width;

    protected override void OnInitialized() => _width = DefaultWidth;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        _attaching = true;
        _ref = DotNetObjectReference.Create(new Callbacks(this));
        _handle = await Interop.ResizerAttachAsync(_ref, ComponentId, (Dir ?? CascadedDir) == BitBhDirection.Rtl);
        _attaching = false;
        if (_disposed) await Interop.ResizerDetachAsync(_handle);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "onkeydown", On<KeyboardEventArgs>("onkeydown", HandleKeyDownAsync));
        // A press on the handle is a resize, not a click on the header (a sort).
        builder.AddAttribute(31, "onclick", On<MouseEventArgs>("onclick", static () => { }));
        builder.AddEventStopPropagationAttribute(32, "onclick", true);
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.AddContent(50, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        string N(double v) => Math.Round(v).ToString(CultureInfo.InvariantCulture);
        attrs["role"] = "separator";
        attrs["aria-orientation"] = "vertical";
        attrs["aria-valuenow"] = N(CurrentWidth);
        attrs["aria-valuemin"] = N(Min);
        attrs["aria-valuemax"] = N(Max);
        attrs["aria-valuetext"] = string.Create(CultureInfo.CurrentCulture, $"{Math.Round(CurrentWidth)} px");
        attrs["tabindex"] = "0";
        attrs[BitBhKeyboard.KeysAttribute] = "ArrowLeft ArrowRight Home End Enter";
        attrs["data-bit-bh-resizer-width"] = N(CurrentWidth);
        // Functional: a touch drag resizes rather than scrolls.
        attrs["style"] = "touch-action:none;cursor:col-resize";
        SetDataValue(attrs, "orientation", "vertical");
        SetDataFlag(attrs, "resizing", _resizing);
        return attrs;
    }

    private async Task HandleKeyDownAsync(KeyboardEventArgs e)
    {
        if (e.CtrlKey || e.MetaKey || e.AltKey) return;
        var rtl = (Dir ?? CascadedDir) == BitBhDirection.Rtl;
        var step = e.ShiftKey ? Step * 5 : Step;
        double? next = e.Key switch
        {
            "ArrowRight" => CurrentWidth + (rtl ? -step : step),
            "ArrowLeft" => CurrentWidth - (rtl ? -step : step),
            "Home" => Min,
            "End" => Max,
            "Enter" => DefaultWidth,
            _ => null,
        };
        if (next is not { } width) return;
        await OnResizeEnd.InvokeAsync(await SetWidthAsync(width));
    }

    // Returns the width set: a controlled Width only catches up when the parent renders again.
    private async Task<double> SetWidthAsync(double width)
    {
        width = Math.Clamp(width, Min, Math.Max(Min, Max));
        _lastWidth = width;
        if (Math.Abs(width - CurrentWidth) < 0.5) return width;
        if (!IsProvided(nameof(Width))) _width = width;
        await WidthChanged.InvokeAsync(width);
        StateHasChanged();
        return width;
    }

    private sealed class Callbacks(BitBhColumnResizer owner)
    {
        /// <summary>The runtime calls it by this name: "move" with the dragged width, or "end".</summary>
        [JSInvokable]
        public Task OnResize(string phase, double width) => owner.RunFromRuntimeAsync(async () =>
        {
            if (owner._disposed) return;
            owner._resizing = phase != "end";
            if (phase == "end")
            {
                owner.StateHasChanged();
                await owner.OnResizeEnd.InvokeAsync(owner._lastWidth ?? owner.CurrentWidth);
                owner._lastWidth = null;
                return;
            }
            await owner.SetWidthAsync(width);
            owner.StateHasChanged();
        });
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        if (!_attaching && _handle > 0) await Interop.ResizerDetachAsync(_handle);
        _ref?.Dispose();
        GC.SuppressFinalize(this);
    }
}
