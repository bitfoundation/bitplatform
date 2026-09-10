using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorUI;

/// <summary>
/// Renders a <see cref="BitMarkdownMathNode"/> as the element a TeX typesetter looks for: a
/// <c>span.math-inline</c>, or a <c>math-display</c> that is a <c>div</c> when the run stood alone
/// as a block and a <c>span</c> when it was written inside a paragraph - either way holding the TeX
/// with its delimiters.
/// </summary>
/// <remarks>
/// The delimiters are re-emitted on purpose. KaTeX's <c>renderMathInElement</c> and MathJax both
/// find math by its delimiters, so the output typesets with either of them and with no
/// configuration; the classes are there for a processor - or a stylesheet - that would rather look
/// for those. With no typesetter loaded at all, the TeX simply reads as the text it is.
/// </remarks>
public sealed class BitMarkdownMathRenderer : BitMarkdownNodeRenderer
{
    public override bool Accept(BitMarkdownNode node) => node is BitMarkdownMathNode;

    // Fixed literal sequence numbers (see BitMarkdownCoreRenderer for the rationale).
    public override void Write(BitMarkdownRenderer r, RenderTreeBuilder b, BitMarkdownNode node)
    {
        var math = (BitMarkdownMathNode)node;

        if (math.Block)
        {
            b.OpenElement(0, "div");
            b.AddAttribute(1, "class", "math math-display");
            b.AddContent(2, "$$" + math.Content + "$$");
            b.CloseElement();
            return;
        }

        // Display style written inside a paragraph still renders as a span: only the element
        // changes, so the markup stays valid and the class a typesetter looks for stays right.
        b.OpenElement(3, "span");
        b.AddAttribute(4, "class", math.Display ? "math math-display" : "math math-inline");
        b.AddContent(5, math.Display ? "$$" + math.Content + "$$" : "$" + math.Content + "$");
        b.CloseElement();
    }
}
