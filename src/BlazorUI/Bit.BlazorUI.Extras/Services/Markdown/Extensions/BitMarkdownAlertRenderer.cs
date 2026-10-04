using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorUI;

/// <summary>
/// Renders a <see cref="BitMarkdownAlertNode"/> with the class names GitHub uses, so the
/// stylesheet can colour each kind without the renderer knowing about the palette.
/// </summary>
public sealed class BitMarkdownAlertRenderer : BitMarkdownNodeRenderer
{
    public override bool Accept(BitMarkdownNode node) => node is BitMarkdownAlertNode;

    // Fixed literal sequence numbers (see BitMarkdownCoreRenderer for the rationale).
    public override void Write(BitMarkdownRenderer r, RenderTreeBuilder b, BitMarkdownNode node)
    {
        var alert = (BitMarkdownAlertNode)node;
        string kind = alert.Kind.ToString().ToLowerInvariant();

        b.OpenElement(0, "div");
        b.AddAttribute(1, "class", $"markdown-alert markdown-alert-{kind}");
        // The kind is announced rather than left to colour alone, which a screen reader
        // and a monochrome print both miss.
        b.OpenElement(2, "p");
        b.AddAttribute(3, "class", "markdown-alert-title");
        b.AddMarkupContent(4, GetIcon(alert.Kind));
        b.AddContent(5, r.Texts.GetAlertTitle(alert.Kind));
        b.CloseElement();
        r.WriteNodes(b, alert.Children);
        b.CloseElement();
    }

    // The glyph beside each title, as GitHub draws one: a second cue to the kind next to the word, which is
    // decoration and so hidden from assistive technology. Stroked in currentColor, it takes the title's color
    // and the system's text color under forced colors, and prints, which a background image would not.
    private const string IconStart = "<svg class=\"bit-mdv-alert-icon\" viewBox=\"0 0 16 16\" width=\"16\" height=\"16\" aria-hidden=\"true\" focusable=\"false\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.5\" stroke-linecap=\"round\" stroke-linejoin=\"round\">";

    private const string NoteIcon = IconStart + "<circle cx=\"8\" cy=\"8\" r=\"6.25\"/><path d=\"M8 7.25v3.75M8 5h.01\"/></svg>";
    private const string TipIcon = IconStart + "<path d=\"M6 12.25h4M6.75 14.5h2.5M8 1.5a4.25 4.25 0 0 0-2.5 7.7c.32.24.5.6.5 1v.55h4V10.2c0-.4.18-.76.5-1A4.25 4.25 0 0 0 8 1.5z\"/></svg>";
    private const string ImportantIcon = IconStart + "<path d=\"M2.25 2.25h11.5v8.5H7.5l-3.25 3v-3h-2z\"/><path d=\"M8 4.5v3M8 9.25h.01\"/></svg>";
    private const string WarningIcon = IconStart + "<path d=\"M8 1.75 14.75 13.75H1.25z\"/><path d=\"M8 6.25v3.25M8 11.5h.01\"/></svg>";
    private const string CautionIcon = IconStart + "<path d=\"M5.4 1.5h5.2l3.9 3.9v5.2l-3.9 3.9H5.4l-3.9-3.9V5.4z\"/><path d=\"M8 4.75v3.5M8 10.75h.01\"/></svg>";

    private static string GetIcon(BitMarkdownAlertKind kind) => kind switch
    {
        BitMarkdownAlertKind.Tip => TipIcon,
        BitMarkdownAlertKind.Important => ImportantIcon,
        BitMarkdownAlertKind.Warning => WarningIcon,
        BitMarkdownAlertKind.Caution => CautionIcon,
        _ => NoteIcon
    };
}
