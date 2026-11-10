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

/// <summary>The button that copies a <see cref="BitBhClipboard"/>'s value (named "Copy" when it has no text of its own).</summary>
public class BitBhClipboardTrigger : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhClipboard? Clipboard { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Content that receives whether the value was just copied.</summary>
    [Parameter]
    public RenderFragment<bool>? Template { get; set; }

    /// <summary>Names the button "Copy" (<see cref="BitBhStrings.Copy"/>) - for an icon-only button. Default: true when it has no content.</summary>
    [Parameter]
    public bool? UseDefaultLabel { get; set; }

    protected override string DefaultTag => "button";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddAttribute(20, "onclick", On<MouseEventArgs>("onclick", () => Clipboard?.CopyAsync() ?? Task.CompletedTask));
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.AddContent(40, BitBhContent.Pick(Template, ChildContent, Clipboard?.IsCopied ?? false));
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Tag.Equals("button", StringComparison.OrdinalIgnoreCase)) attrs["type"] = "button";
        if (UseDefaultLabel ?? (ChildContent is null && Template is null)) attrs["aria-label"] = Strings.Copy;
        attrs["data-bit-bh-interactive"] = "";
        SetDataFlag(attrs, "copied", Clipboard?.IsCopied ?? false);
        return attrs;
    }
}

/// <summary>A read-only input showing a <see cref="BitBhClipboard"/>'s value, selected on focus. Name it with <c>aria-label</c>.</summary>
public class BitBhClipboardInput : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhClipboard? Clipboard { get; set; }

    protected override string DefaultTag => "input";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddElementReferenceCapture(20, CaptureElement);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["type"] = "text";
        attrs["readonly"] = true;
        attrs["value"] = Clipboard?.Value ?? string.Empty;
        attrs["data-bit-bh-select-on-focus"] = "";
        SetDataFlag(attrs, "copied", Clipboard?.IsCopied ?? false);
        return attrs;
    }
}

/// <summary>Content of a <see cref="BitBhClipboard"/> shown only while the value was just copied, or only while it wasn't (<see cref="WhenCopied"/> = false).</summary>
public class BitBhClipboardIndicator : ComponentBase
{
    [CascadingParameter]
    private BitBhClipboard? Clipboard { get; set; }

    /// <summary>Show the content while copied (true, default) or while not.</summary>
    [Parameter]
    public bool WhenCopied { get; set; } = true;

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if ((Clipboard?.IsCopied ?? false) == WhenCopied) builder.AddContent(0, ChildContent);
    }
}
