using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// Shared behaviour of <see cref="BitBhNumberFieldIncrement"/> and <see cref="BitBhNumberFieldDecrement"/>: a
/// button out of the tab order (the input is the field's one tab stop and its arrow keys do the same) that
/// steps once when pressed, keeps stepping while it is held, never takes the focus from the input and is
/// disabled at the bound it moves toward.
/// </summary>
public abstract class BitBhNumberFieldStepperBase : BitBhComponentBase, IDisposable
{
    private CancellationTokenSource? _repeat;
    private bool _pointerPressed;

    [CascadingParameter]
    private BitBhNumberFieldContext? FieldContext { get; set; }

    /// <summary>The content (an icon, "+" / "−").</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Delay before a held button starts repeating (ms). Default: 400.</summary>
    [Parameter]
    public int RepeatDelay { get; set; } = 400;

    /// <summary>Interval between repeats while the button is held (ms). Default: 60.</summary>
    [Parameter]
    public int RepeatInterval { get; set; } = 60;

    protected override string DefaultTag => "button";

    /// <summary>+1 or -1.</summary>
    protected abstract int Direction { get; }

    /// <summary>The default accessible name ("Increase" / "Decrease"); an <c>aria-label</c> you set replaces it.</summary>
    protected abstract string DefaultLabel { get; }

    private bool IsDisabled => FieldContext is not { } context || (Direction > 0 ? !context.CanIncrement : !context.CanDecrement);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "onpointerdown", On<PointerEventArgs>("onpointerdown", HandlePointerDownAsync));
        builder.AddAttribute(31, "onpointerup", On<PointerEventArgs>("onpointerup", () => StopAsync(clickFollows: true)));
        builder.AddAttribute(32, "onpointerleave", On<PointerEventArgs>("onpointerleave", () => StopAsync(clickFollows: false)));
        builder.AddAttribute(33, "onpointercancel", On<PointerEventArgs>("onpointercancel", () => StopAsync(clickFollows: false)));
        // Pressing the button leaves the focus where it is (the input).
        builder.AddAttribute(34, "onmousedown", On<MouseEventArgs>("onmousedown", static () => { }));
        builder.AddEventPreventDefaultAttribute(35, "onmousedown", true);
        builder.AddAttribute(36, "onclick", On<MouseEventArgs>("onclick", HandleClickAsync));
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.AddContent(50, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        var disabled = IsDisabled;
        if (Tag.Equals("button", StringComparison.OrdinalIgnoreCase))
        {
            attrs["type"] = "button";
            if (disabled) attrs["disabled"] = true;
        }
        else
        {
            attrs["role"] = "button";
            if (disabled) attrs["aria-disabled"] = "true";
        }
        attrs["tabindex"] = "-1";
        attrs["aria-label"] = DefaultLabel;
        if (FieldContext is { } field)
        {
            attrs["aria-controls"] = field.InputId;
            SetDataFlag(attrs, "readonly", field.IsReadOnly);
            SetDataFlag(attrs, "invalid", field.IsInvalid);
            SetDataFlag(attrs, "scrubbing", field.IsScrubbing);
        }
        attrs["data-bit-bh-interactive"] = "";
        SetDataFlag(attrs, "disabled", disabled);
        SetDataFlag(attrs, "pressed", _repeat is not null);
        return attrs;
    }

    private async Task HandlePointerDownAsync(PointerEventArgs e)
    {
        if (e.Button != 0 || IsDisabled || FieldContext is not { } field) return;
        _pointerPressed = true;
        CancelRepeat();
        var cts = _repeat = new CancellationTokenSource();
        field.SetScrubbing(true);
        // A mouse press puts the keyboard in the input, so the arrow keys carry on from the button. A touch
        // press does not: it would open the on-screen keyboard.
        if (e.PointerType != "touch" && !field.IsFocused) await Interop.FocusAsync(field.InputId);
        await field.StepAsync(Direction, BitBhStepSize.Normal, commit: false);
        _ = RepeatAsync(field, cts.Token);
    }

    private async Task RepeatAsync(BitBhNumberFieldContext field, CancellationToken token)
    {
        try
        {
            await Task.Delay(Math.Max(0, RepeatDelay), token);
            while (!token.IsCancellationRequested)
            {
                var stepped = false;
                await InvokeAsync(async () =>
                {
                    if (token.IsCancellationRequested) return;
                    // At the bound the button is disabled, and a disabled button gets no pointerup: the hold
                    // ends here instead.
                    if (IsDisabled)
                    {
                        await StopAsync(clickFollows: false);
                        return;
                    }
                    await field.StepAsync(Direction, BitBhStepSize.Normal, commit: false);
                    stepped = true;
                });
                if (!stepped) break;
                await Task.Delay(Math.Max(16, RepeatInterval), token);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            await DispatchExceptionAsync(ex);
        }
    }

    private async Task StopAsync(bool clickFollows)
    {
        // A click follows a release over the (enabled) button only: one that never comes must not swallow the
        // next keyboard activation.
        if (!clickFollows || IsDisabled) _pointerPressed = false;
        if (_repeat is null) return;
        CancelRepeat();
        if (FieldContext is { } field)
        {
            field.SetScrubbing(false);
            await field.CommitAsync();
        }
        StateHasChanged();
    }

    private Task HandleClickAsync(MouseEventArgs e)
    {
        // A pointer press already stepped; a click without one is a keyboard or assistive-technology activation.
        if (_pointerPressed)
        {
            _pointerPressed = false;
            return Task.CompletedTask;
        }
        return IsDisabled || FieldContext is not { } field ? Task.CompletedTask : field.StepAsync(Direction, BitBhStepSize.Normal, commit: true);
    }

    private void CancelRepeat()
    {
        var cts = _repeat;
        _repeat = null;
        cts?.Cancel();
        cts?.Dispose();
    }

    public void Dispose() => CancelRepeat();
}

/// <summary>
/// The button that steps a <see cref="BitBhNumberField{TValue}"/> up (named "Increase" unless you give it an
/// <c>aria-label</c>). Out of the tab order, repeats while held, disabled at the maximum.
/// </summary>
public class BitBhNumberFieldIncrement : BitBhNumberFieldStepperBase
{
    protected override int Direction => 1;

    protected override string DefaultLabel => Strings.Increase;
}

/// <summary>
/// The button that steps a <see cref="BitBhNumberField{TValue}"/> down (named "Decrease" unless you give it an
/// <c>aria-label</c>). Out of the tab order, repeats while held, disabled at the minimum.
/// </summary>
public class BitBhNumberFieldDecrement : BitBhNumberFieldStepperBase
{
    protected override int Direction => -1;

    protected override string DefaultLabel => Strings.Decrease;
}
