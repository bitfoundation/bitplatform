using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// A utility button pre-wired to close the nearest enclosing
/// <see cref="BitBhDialog"/>, <see cref="BitBhPopover"/>, or <see cref="BitBhDisclosure"/>.
///
/// <para>Saves boilerplate for dismiss actions — just drop it inside any closeable
/// component and it will find and invoke the appropriate close callback.</para>
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>Polymorphic rendering</b> — renders as &lt;button&gt; by default;
///     set <see cref="BitBhComponentBase.As"/> to render as any element.</item>
///   <item><b>Automatic context detection</b> — walks up the cascading parameter
///     chain to find the nearest <see cref="IBitBhCloseContext"/> (Dialog, Popover,
///     or Disclosure).</item>
///   <item><b>Keyboard accessible</b> — non-native elements receive
///     <c>role="button"</c>, <c>tabindex="0"</c>, and Enter/Space handling.</item>
///   <item><b>Disabled support</b> — respects the <see cref="Disabled"/> parameter.</item>
/// </list>
///
/// <para><b>Usage inside a Dialog:</b></para>
/// <code>
/// &lt;BitBhDialog Open="@isOpen" OnClose="() =&gt; isOpen = false"&gt;
///     &lt;BitBhDialogPanel&gt;
///         &lt;BitBhDialogTitle&gt;Confirm&lt;/BitBhDialogTitle&gt;
///         &lt;BitBhCloseButton&gt;Cancel&lt;/BitBhCloseButton&gt;
///     &lt;/BitBhDialogPanel&gt;
/// &lt;/BitBhDialog&gt;
/// </code>
///
/// <para><b>Usage inside a Popover:</b></para>
/// <code>
/// &lt;BitBhPopover&gt;
///     &lt;BitBhPopoverButton&gt;Menu&lt;/BitBhPopoverButton&gt;
///     &lt;BitBhPopoverPanel&gt;
///         &lt;a href="/home"&gt;Home&lt;/a&gt;
///         &lt;BitBhCloseButton As="a" href="/about"&gt;About&lt;/BitBhCloseButton&gt;
///     &lt;/BitBhPopoverPanel&gt;
/// &lt;/BitBhPopover&gt;
/// </code>
///
/// <para><b>Usage inside a Disclosure:</b></para>
/// <code>
/// &lt;BitBhDisclosure&gt;
///     &lt;BitBhDisclosureButton&gt;Details&lt;/BitBhDisclosureButton&gt;
///     &lt;BitBhDisclosurePanel&gt;
///         Some content.
///         &lt;BitBhCloseButton&gt;Close&lt;/BitBhCloseButton&gt;
///     &lt;/BitBhDisclosurePanel&gt;
/// &lt;/BitBhDisclosure&gt;
/// </code>
/// </summary>
public class BitBhCloseButton : BitBhComponentBase
{
    /// <summary>
    /// The nearest closeable context (Dialog, Popover, or Disclosure).
    /// Resolved automatically via cascading parameters.
    /// </summary>
    [CascadingParameter]
    private IBitBhCloseContext? CloseContext { get; set; }

    /// <summary>Whether the close button is disabled.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// Child content rendered inside the button.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "button";

    private bool IsNativeButton =>
        Tag.Equals("button", StringComparison.OrdinalIgnoreCase)
        || Tag.Equals("input", StringComparison.OrdinalIgnoreCase);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);

        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        builder.AddAttribute(30, "onclick",
            On<MouseEventArgs>("onclick", HandleClickAsync));

        if (!IsNativeButton)
        {
            builder.AddAttribute(40, "onkeydown",
                On<KeyboardEventArgs>("onkeydown", HandleKeyDownAsync));
        }

        if (Ref is not null)
            builder.AddElementReferenceCapture(50, Ref);

        if (ChildContent is not null)
            builder.AddContent(60, ChildContent);

        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();

        if (IsNativeButton)
        {
            attrs["type"] = "button";
            if (Disabled)
                attrs["disabled"] = true;
        }
        else
        {
            attrs["role"] = "button";
            attrs["tabindex"] = Disabled ? "-1" : "0";
            attrs[BitBhKeyboard.KeysAttribute] = "Enter Space";
            if (Disabled)
                attrs["aria-disabled"] = "true";
        }

        attrs["data-bit-bh-interactive"] = "";
        SetDataFlag(attrs, "disabled", Disabled);

        return attrs;
    }

    private async Task HandleClickAsync(MouseEventArgs args)
    {
        if (Disabled || CloseContext is null) return;
        await CloseContext.CloseAsync();
    }

    private async Task HandleKeyDownAsync(KeyboardEventArgs args)
    {
        if (Disabled || CloseContext is null) return;

        if (args.Key is "Enter" or " ")
            await CloseContext.CloseAsync();
    }
}
