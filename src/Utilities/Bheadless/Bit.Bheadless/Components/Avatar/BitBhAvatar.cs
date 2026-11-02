using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// An image element with a graceful fallback for representing a user or entity.
/// A faithful port of Radix UI's <c>Avatar</c> primitive.
///
/// <para><b>Compound API:</b></para>
/// <list type="bullet">
///   <item><see cref="BitBhAvatar"/> — the root container (renders a <c>&lt;span&gt;</c>).</item>
///   <item><see cref="BitBhAvatarImage"/> — the image; only shown once it loads successfully.</item>
///   <item><see cref="BitBhAvatarFallback"/> — rendered until the image loads (or if it errors), with an optional delay.</item>
/// </list>
///
/// <para><b>Key features (inspired by Radix UI):</b></para>
/// <list type="bullet">
///   <item>
///     <b>Automatic fallback</b> — the image is only displayed after it loads,
///     avoiding broken-image icons. While loading or on error the fallback shows.
///   </item>
///   <item>
///     <b>Loading-status coordination</b> — the root tracks the image's
///     <see cref="BitBhAvatarImageLoadingStatus"/> and shares it via cascading
///     context so the fallback knows when to hide.
///   </item>
///   <item>
///     <b>Polymorphic rendering</b> — renders a <c>&lt;span&gt;</c> by default;
///     set <see cref="BitBhComponentBase.As"/> to render as any element.
///   </item>
/// </list>
///
/// <para><b>Usage:</b></para>
/// <code>
/// &lt;BitBhAvatar class="avatar"&gt;
///     &lt;BitBhAvatarImage Src="/user.jpg" Alt="Jane Doe" class="avatar-image" /&gt;
///     &lt;BitBhAvatarFallback DelayMs="600" class="avatar-fallback"&gt;JD&lt;/BitBhAvatarFallback&gt;
/// &lt;/BitBhAvatar&gt;
/// </code>
/// </summary>
public class BitBhAvatar : BitBhComponentBase
{
    private BitBhAvatarImageLoadingStatus _status = BitBhAvatarImageLoadingStatus.Idle;

    /// <summary>
    /// The avatar content — typically a <see cref="BitBhAvatarImage"/> and a
    /// <see cref="BitBhAvatarFallback"/>.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>The root renders a <c>&lt;span&gt;</c> by default, matching Radix.</summary>
    protected override string DefaultTag => "span";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var context = new BitBhAvatarContext
        {
            Status = _status,
            SetStatus = SetStatus,
        };

        builder.OpenComponent<CascadingValue<BitBhAvatarContext>>(0);
        builder.AddComponentParameter(1, "Value", context);
        builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(inner =>
        {
            inner.OpenElement(0, Tag);
            inner.AddAttribute(10, "id", ComponentId);
            inner.AddMultipleAttributes(20, GetFinalAttributes());

            if (Ref is not null)
                inner.AddElementReferenceCapture(30, Ref);

            inner.AddContent(40, ChildContent);
            inner.CloseElement();
        }));
        builder.CloseComponent();
    }

    private void SetStatus(BitBhAvatarImageLoadingStatus status)
    {
        if (_status == status) return;
        _status = status;
        StateHasChanged();
    }
}
