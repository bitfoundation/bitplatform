using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// A link of a <see cref="BitBhTableOfContents"/> to the section <see cref="Target"/> on the current page; the current one has
/// <c>aria-current="location"</c> and <c>data-active</c>.
/// </summary>
public class BitBhTableOfContentsLink : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhTableOfContents? Toc { get; set; }

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    /// <summary>The id of the section the link goes to.</summary>
    [Parameter, EditorRequired]
    public string Target { get; set; } = string.Empty;

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "a";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddElementReferenceCapture(20, CaptureElement);
        builder.AddContent(30, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        // The current page's address with the fragment: a bare "#target" resolves against <base href> - another page.
        if (Tag.Equals("a", StringComparison.OrdinalIgnoreCase))
        {
            var uri = Navigation.Uri;
            var hash = uri.IndexOf('#');
            attrs["href"] = $"{(hash >= 0 ? uri[..hash] : uri)}#{Target}";
        }
        var active = Toc?.IsActive(Target) ?? false;
        if (active) attrs["aria-current"] = "location";
        attrs["data-bit-bh-interactive"] = "";
        SetDataFlag(attrs, "active", active);
        return attrs;
    }
}
