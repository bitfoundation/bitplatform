using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// The image element of an <see cref="BitBhAvatar"/>. Only displayed once it has
/// loaded successfully, so consumers never see a broken-image icon. Until then
/// (or if loading fails) the <see cref="BitBhAvatarFallback"/> is shown instead.
///
/// <para><b>How it works (adapted from Radix UI for Blazor):</b></para>
/// <para>
/// Radix probes a detached <c>Image</c> object before mounting the real
/// <c>&lt;img&gt;</c>. In Blazor the element is rendered immediately — so the
/// browser downloads it and fires native <c>load</c>/<c>error</c> events — but it
/// stays hidden via the <c>hidden</c> attribute until the load succeeds. The net
/// visual behaviour is identical to Radix: the fallback shows while loading and
/// the image only appears once ready.
/// </para>
///
/// <para>
/// The component reports every status transition to the parent
/// <see cref="BitBhAvatar"/> (so the fallback can coordinate) and to the optional
/// <see cref="OnLoadingStatusChange"/> callback.
/// </para>
/// </summary>
public class BitBhAvatarImage : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhAvatarContext AvatarContext { get; set; } = default!;

    /// <summary>The image source URL.</summary>
    [Parameter]
    public string? Src { get; set; }

    /// <summary>Alternative text for the image. Strongly recommended for accessibility.</summary>
    [Parameter]
    public string? Alt { get; set; }

    /// <summary>
    /// Optional callback invoked whenever the image loading status changes
    /// (<c>Loading</c> → <c>Loaded</c> / <c>Error</c>). Mirrors Radix's
    /// <c>onLoadingStatusChange</c>.
    /// </summary>
    [Parameter]
    public EventCallback<BitBhAvatarImageLoadingStatus> OnLoadingStatusChange { get; set; }

    /// <summary>Renders an <c>&lt;img&gt;</c> element.</summary>
    protected override string DefaultTag => "img";

    // The source we last reported a status for, so re-renders with the same src
    // don't re-trigger the "loading" transition.
    private string? _reportedSrc;

    private BitBhAvatarImageLoadingStatus Status => AvatarContext?.Status ?? BitBhAvatarImageLoadingStatus.Idle;

    protected override async Task OnParametersSetAsync()
    {
        if (string.IsNullOrEmpty(Src))
        {
            // No source: mirror Radix's resolveLoadingStatus which reports "error".
            if (_reportedSrc is not null || Status != BitBhAvatarImageLoadingStatus.Error)
            {
                _reportedSrc = null;
                await ReportStatusAsync(BitBhAvatarImageLoadingStatus.Error);
            }
            return;
        }

        if (_reportedSrc != Src)
        {
            // A new source begins loading.
            _reportedSrc = Src;
            await ReportStatusAsync(BitBhAvatarImageLoadingStatus.Loading);
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        // With no usable source there is nothing to render (the fallback shows).
        if (string.IsNullOrEmpty(Src)) return;

        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        builder.AddAttribute(30, "src", Src);
        if (Alt is not null)
            builder.AddAttribute(31, "alt", Alt);

        builder.AddAttribute(40, "onload",
            On<ProgressEventArgs>("onload", HandleLoad));
        builder.AddAttribute(41, "onerror",
            On<Microsoft.AspNetCore.Components.Web.ErrorEventArgs>("onerror", HandleError));

        // Keep the element in the DOM (so it downloads and fires load/error),
        // but hide it until it has successfully loaded.
        if (Status != BitBhAvatarImageLoadingStatus.Loaded)
            builder.AddAttribute(50, "hidden", true);

        if (Ref is not null)
            builder.AddElementReferenceCapture(60, Ref);

        builder.CloseElement();
    }

    private Task HandleLoad(ProgressEventArgs args) => ReportStatusAsync(BitBhAvatarImageLoadingStatus.Loaded);

    private Task HandleError(Microsoft.AspNetCore.Components.Web.ErrorEventArgs args) => ReportStatusAsync(BitBhAvatarImageLoadingStatus.Error);

    private Task ReportStatusAsync(BitBhAvatarImageLoadingStatus status)
    {
        AvatarContext?.SetStatus(status);
        return OnLoadingStatusChange.InvokeAsync(status);
    }
}
