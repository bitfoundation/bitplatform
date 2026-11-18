using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// Headless copy-to-clipboard (Ark Clipboard): a trigger that copies <see cref="Value"/>, a "copied" state that lasts
/// <see cref="Timeout"/>, and an announcement so screen-reader users hear that it worked.
///
/// <code>
/// &lt;BitBhClipboard Value="@apiKey" class="clipboard"&gt;
///     &lt;BitBhClipboardInput aria-label="API key" /&gt;
///     &lt;BitBhClipboardTrigger&gt;
///         &lt;Template Context="copied"&gt;@(copied ? "Copied" : "Copy")&lt;/Template&gt;
///     &lt;/BitBhClipboardTrigger&gt;
/// &lt;/BitBhClipboard&gt;
/// </code>
/// <para><b>Styling hooks:</b> <c>data-copied</c> on every part while the copied state lasts.</para>
/// </summary>
public class BitBhClipboard : BitBhComponentBase, IDisposable
{
    private CancellationTokenSource? _reset;
    private bool _copied;

    /// <summary>The text to copy.</summary>
    [Parameter, EditorRequired]
    public string Value { get; set; } = string.Empty;

    /// <summary>How long the copied state lasts (ms). Default: 2000.</summary>
    [Parameter]
    public int Timeout { get; set; } = 2000;

    /// <summary>Fires after a copy, with whether the browser allowed it.</summary>
    [Parameter]
    public EventCallback<bool> OnCopy { get; set; }

    /// <summary>Announces a successful copy (<see cref="BitBhStrings.Copied"/>). Default: true.</summary>
    [Parameter]
    public bool Announce { get; set; } = true;

    /// <summary>The parts.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    /// <summary>Whether the value was just copied.</summary>
    public bool IsCopied => _copied;

    /// <summary>Copies <see cref="Value"/> to the clipboard.</summary>
    public async Task CopyAsync()
    {
        var ok = await Interop.CopyToClipboardAsync(Value);
        await OnCopy.InvokeAsync(ok);
        if (!ok) return;

        _copied = true;
        StateHasChanged();
        if (Announce && GetService<IBitBhAnnouncer>() is { } announcer) await announcer.AnnounceAsync(Strings.Copied);

        _reset?.Cancel();
        _reset?.Dispose();
        var cts = _reset = new CancellationTokenSource();
        try
        {
            await Task.Delay(Math.Max(0, Timeout), cts.Token);
            _copied = false;
            StateHasChanged();
        }
        catch (OperationCanceledException)
        {
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddElementReferenceCapture(20, CaptureElement);
        builder.OpenComponent<CascadingValue<BitBhClipboard>>(30);
        builder.AddComponentParameter(31, "Value", this);
        builder.AddComponentParameter(32, "IsFixed", false);
        builder.AddComponentParameter(33, "ChildContent", ChildContent);
        builder.CloseComponent();
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataFlag(attrs, "copied", _copied);
        return attrs;
    }

    public void Dispose()
    {
        _reset?.Cancel();
        _reset?.Dispose();
        _reset = null;
    }
}
