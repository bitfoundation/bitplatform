using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace Bit.Bheadless;

/// <summary>
/// An area of a <see cref="BitBhNumberField{TValue}"/> that changes the value when dragged across - the
/// "scrub" handle of design tools: a label or an icon next to the input, dragged right (or up) to increase.
/// Every <c>ScrubPixelSensitivity</c> pixels of movement is one step; Shift makes it a large step. The value
/// is committed when the pointer is released. The area is a pointer convenience only - it is not focusable
/// and hidden from assistive technology, which uses the input's arrow keys.
///
/// <code>
/// &lt;BitBhNumberField @bind-Value="width"&gt;
///     &lt;BitBhNumberFieldScrubArea class="scrub"&gt;W&lt;/BitBhNumberFieldScrubArea&gt;
///     &lt;BitBhNumberFieldInput aria-label="Width" /&gt;
/// &lt;/BitBhNumberField&gt;
/// </code>
/// <para><b>Styling hooks:</b> <c>data-scrubbing</c> while dragged (style <c>cursor: ew-resize</c>).</para>
/// </summary>
public class BitBhNumberFieldScrubArea : BitBhComponentBase, IAsyncDisposable
{
    private DotNetObjectReference<Callbacks>? _ref;
    private int _handle;
    private bool _attaching;
    private bool _disposed;

    [CascadingParameter]
    private BitBhNumberFieldContext? FieldContext { get; set; }

    /// <summary>Drag direction: <see cref="BitBhOrientation.Horizontal"/> (right increases, default) or vertical (up increases).</summary>
    [Parameter]
    public BitBhOrientation Direction { get; set; }

    /// <summary>The content (a label, an icon).</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "span";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.AddContent(40, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["aria-hidden"] = "true";
        if (FieldContext is { } field)
        {
            var disabled = field.IsDisabled || field.IsReadOnly;
            SetDataFlag(attrs, "disabled", disabled);
            SetDataFlag(attrs, "scrubbing", field.IsScrubbing);
            SetDataValue(attrs, "orientation", Direction.ToAttribute());
            // Functional, not visual: a touch drag must scrub rather than scroll the page or select text.
            attrs["style"] = "touch-action:none;user-select:none;-webkit-user-select:none";
        }
        return attrs;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || FieldContext is null) return;
        _attaching = true;
        _ref = DotNetObjectReference.Create(new Callbacks(this));
        _handle = await Interop.ScrubAttachAsync(_ref, ComponentId, new Dictionary<string, object?>
        {
            ["sensitivity"] = FieldContext.ScrubPixelSensitivity,
            ["vertical"] = Direction == BitBhOrientation.Vertical,
        });
        _attaching = false;
        if (_disposed) await Interop.ScrubDetachAsync(_handle);
    }

    private sealed class Callbacks(BitBhNumberFieldScrubArea owner)
    {
        /// <summary>The runtime calls it by this name: "start", "move" (with whole steps) or "end".</summary>
        [JSInvokable]
        public Task OnScrub(string phase, int steps, bool large) => owner.RunFromRuntimeAsync(() => owner.HandleScrubAsync(phase, steps, large));
    }

    private async Task HandleScrubAsync(string phase, int steps, bool large)
    {
        if (FieldContext is not { } field) return;
        // The end of a scrub always ends it - even when the field was disabled meanwhile - so data-scrubbing does
        // not stick and a change already made is still committed.
        if (phase is not ("start" or "move"))
        {
            field.SetScrubbing(false);
            await field.CommitAsync();
            return;
        }
        if (field.IsDisabled || field.IsReadOnly) return;
        switch (phase)
        {
            case "start":
                field.SetScrubbing(true);
                break;
            case "move":
                var size = large ? BitBhStepSize.Large : BitBhStepSize.Normal;
                for (var i = 0; i < Math.Abs(steps); i++)
                    await field.StepAsync(Math.Sign(steps), size, commit: false);
                break;
            default:
                field.SetScrubbing(false);
                await field.CommitAsync();
                break;
        }
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        if (!_attaching && _handle > 0) await Interop.ScrubDetachAsync(_handle);
        _ref?.Dispose();
        GC.SuppressFinalize(this);
    }
}
