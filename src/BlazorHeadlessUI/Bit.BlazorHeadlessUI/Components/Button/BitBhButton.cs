using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// A headless Button with disabled and loading states and interaction data attributes.
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>Polymorphic</b> — <c>&lt;button&gt;</c> by default; other elements get <c>role="button"</c>,
///     <c>tabindex</c> and Enter / Space activation (links rendered with <c>As="a" href</c> keep link semantics).</item>
///   <item><b>Loading</b> — <c>aria-busy</c> and <c>aria-disabled</c> (focus is kept, clicks are ignored).</item>
///   <item><b>Interaction attributes</b> — <c>data-hover</c>, <c>data-active</c>, <c>data-focus</c>,
///     <c>data-focus-visible</c> maintained in the browser (no server round-trips), ignoring touch "sticky hover".</item>
///   <item><b>Render-prop context</b> — <see cref="ChildContent"/> receives <see cref="BitBhButtonContext"/>.</item>
/// </list>
///
/// <code>
/// &lt;BitBhButton OnClick="Save" Loading="@isSaving" Context="btn"&gt;
///     @(btn.Loading ? "Saving…" : "Save")
/// &lt;/BitBhButton&gt;
/// </code>
/// </summary>
public class BitBhButton : BitBhComponentBase
{
    /// <summary>Disables the button (native <c>disabled</c> on buttons).</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Loading state: clicks are ignored, <c>aria-busy</c> is set, focus is kept.</summary>
    [Parameter]
    public bool Loading { get; set; }

    /// <summary>The <c>type</c> of a native button. Default: <c>"button"</c>.</summary>
    [Parameter]
    public string Type { get; set; } = "button";

    /// <summary>Click handler. Not invoked while disabled or loading.</summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Content that receives the render state (<c>BitBhButtonContext</c>). Use it instead of
    /// <see cref="ChildContent"/> when the markup depends on that state:
    /// <c>&lt;Template Context="state"&gt;…&lt;/Template&gt;</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<BitBhButtonContext>? Template { get; set; }

    protected override string DefaultTag => "button";

    private bool IsNativeButton =>
        Tag.Equals("button", StringComparison.OrdinalIgnoreCase)
        || Tag.Equals("input", StringComparison.OrdinalIgnoreCase);

    private bool IsLink => Tag.Equals("a", StringComparison.OrdinalIgnoreCase) && GetAdditionalAttribute("href") is not null;

    private bool IsInteractive => !Disabled && !Loading;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "onclick",
            On<MouseEventArgs>("onclick", HandleClickAsync));
        if (!IsInteractive)
            builder.AddEventPreventDefaultAttribute(31, "onclick", true);
        if (!IsNativeButton && !IsLink)
            builder.AddAttribute(40, "onkeydown",
                On<KeyboardEventArgs>("onkeydown", HandleKeyDownAsync));
        builder.AddElementReferenceCapture(50, CaptureElement);
        if (Template is not null || ChildContent is not null)
            builder.AddContent(60, BitBhContent.Pick(Template, ChildContent, new BitBhButtonContext { Disabled = Disabled, Loading = Loading, Type = Type }));
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["data-bit-bh-interactive"] = "";

        if (IsNativeButton)
        {
            attrs["type"] = Type;
            if (Disabled) attrs["disabled"] = true;
            else if (Loading) attrs["aria-disabled"] = "true";
        }
        else
        {
            if (!IsLink)
            {
                attrs["role"] = "button";
                attrs[BitBhKeyboard.KeysAttribute] = "Enter Space";
            }
            attrs["tabindex"] = Disabled ? "-1" : "0";
            if (!IsInteractive) attrs["aria-disabled"] = "true";
        }

        if (Loading)
            attrs["aria-busy"] = "true";

        SetDataFlag(attrs, "disabled", Disabled);
        SetDataFlag(attrs, "loading", Loading);
        return attrs;
    }

    private Task HandleClickAsync(MouseEventArgs args)
        => IsInteractive ? OnClick.InvokeAsync(args) : Task.CompletedTask;

    private Task HandleKeyDownAsync(KeyboardEventArgs args)
        => IsInteractive && args.Key is "Enter" or " " ? OnClick.InvokeAsync(new MouseEventArgs()) : Task.CompletedTask;
}
