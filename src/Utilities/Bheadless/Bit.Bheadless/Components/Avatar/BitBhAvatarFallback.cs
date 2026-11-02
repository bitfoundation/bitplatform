using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// The fallback content of an <see cref="BitBhAvatar"/>, shown while the
/// <see cref="BitBhAvatarImage"/> is loading or when it fails to load. Typically the
/// user's initials or a placeholder icon. A faithful port of Radix UI's
/// <c>Avatar.Fallback</c>.
///
/// <para><b>Behaviour:</b></para>
/// <list type="bullet">
///   <item>
///     Rendered only while the image's status is not <c>Loaded</c>. Once the
///     image loads, the fallback is removed from the DOM.
///   </item>
///   <item>
///     <b>Optional delay</b> — set <see cref="DelayMs"/> to wait before showing
///     the fallback. This avoids a flash of fallback content for fast-loading
///     images on quick connections.
///   </item>
///   <item>
///     <b>Polymorphic rendering</b> — renders a <c>&lt;span&gt;</c> by default;
///     set <see cref="BitBhComponentBase.As"/> to render as any element.
///   </item>
/// </list>
/// </summary>
public class BitBhAvatarFallback : BitBhComponentBase, IDisposable
{
    [CascadingParameter]
    private BitBhAvatarContext AvatarContext { get; set; } = default!;

    /// <summary>
    /// Optional delay in milliseconds before the fallback becomes visible. When
    /// omitted the fallback renders immediately. Use a small delay (e.g. 600ms)
    /// to prevent a flash of fallback for images that load quickly.
    /// </summary>
    [Parameter]
    public int? DelayMs { get; set; }

    /// <summary>The fallback content (initials, an icon, etc.).</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Renders a <c>&lt;span&gt;</c> by default, matching Radix.</summary>
    protected override string DefaultTag => "span";

    private bool _canRender;
    private Timer? _timer;

    private BitBhAvatarImageLoadingStatus Status => AvatarContext?.Status ?? BitBhAvatarImageLoadingStatus.Idle;

    protected override void OnInitialized()
    {
        if (DelayMs is null)
        {
            _canRender = true;
            return;
        }

        _timer = new Timer(_ =>
        {
            _canRender = true;
            _ = InvokeAsync(StateHasChanged);
        }, null, DelayMs.Value, Timeout.Infinite);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        // Mirror Radix: render only once allowed and while the image isn't loaded.
        if (!_canRender || Status == BitBhAvatarImageLoadingStatus.Loaded)
            return;

        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        if (Ref is not null)
            builder.AddElementReferenceCapture(30, Ref);

        builder.AddContent(40, ChildContent);
        builder.CloseElement();
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _timer = null;
    }
}
