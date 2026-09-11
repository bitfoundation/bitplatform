using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorUI;

/// <summary>
/// Renders a <see cref="BitMarkdownContainerNode"/> as a <c>div</c> carrying its name as a class,
/// so a stylesheet decides what a container called "warning" or "grid" looks like without the
/// renderer knowing about any of them.
/// </summary>
public sealed class BitMarkdownContainerRenderer : BitMarkdownNodeRenderer
{
    public override bool Accept(BitMarkdownNode node) => node is BitMarkdownContainerNode;

    /// <summary>The container name that renders as a collapsible <c>&lt;details&gt;</c>.</summary>
    /// <remarks>
    /// Markdown has no syntax of its own for a collapsible section, and the convention every docs
    /// site settled on is a container called "details" - so <c>:::details How it works</c> is the
    /// one name this renderer treats as more than a class.
    /// </remarks>
    public const string DetailsName = "details";

    // Fixed literal sequence numbers (see BitMarkdownCoreRenderer for the rationale).
    public override void Write(BitMarkdownRenderer r, RenderTreeBuilder b, BitMarkdownNode node)
    {
        var container = (BitMarkdownContainerNode)node;

        if (container.Name == DetailsName)
        {
            WriteDetails(r, b, container);
            return;
        }

        b.OpenElement(0, "div");
        b.AddAttribute(1, "class", container.Name.Length == 0
            ? "markdown-container"
            : $"markdown-container markdown-container-{container.Name}");

        if (string.IsNullOrEmpty(container.Title) is false)
        {
            b.OpenElement(2, "p");
            b.AddAttribute(3, "class", "markdown-container-title");
            b.AddContent(4, container.Title);
            b.CloseElement();
        }

        r.WriteNodes(b, container.Children);
        b.CloseElement();
    }

    private static void WriteDetails(BitMarkdownRenderer r, RenderTreeBuilder b, BitMarkdownContainerNode container)
    {
        b.OpenElement(5, "details");
        b.AddAttribute(6, "class", "markdown-container markdown-container-details");
        // A <details> with no <summary> is labelled "Details" by the browser in whatever language
        // it likes; writing the title ourselves keeps the label the author's.
        b.OpenElement(7, "summary");
        b.AddContent(8, string.IsNullOrEmpty(container.Title) ? container.Name : container.Title);
        b.CloseElement();
        r.WriteNodes(b, container.Children);
        b.CloseElement();
    }
}
