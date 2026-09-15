namespace Bit.BlazorUI;

/// <summary>Fluent helpers for enabling the built-in Markdown flavors.</summary>
public static class BitMarkdownPipelineBuilderExtensions
{
    /// <summary>Adds GitHub-style pipe tables.</summary>
    public static BitMarkdownPipelineBuilder UsePipeTables(this BitMarkdownPipelineBuilder b)
        => b.Use(new BitMarkdownPipeTableExtension());

    /// <summary>Adds <c>~~strikethrough~~</c>.</summary>
    public static BitMarkdownPipelineBuilder UseStrikethrough(this BitMarkdownPipelineBuilder b)
        => b.Use(new BitMarkdownStrikethroughExtension());

    /// <summary>Adds GitHub task lists (<c>- [ ]</c> / <c>- [x]</c>).</summary>
    public static BitMarkdownPipelineBuilder UseTaskLists(this BitMarkdownPipelineBuilder b)
        => b.Use(new BitMarkdownTaskListExtension());

    /// <summary>Adds autolink literals (bare URLs and emails become links).</summary>
    public static BitMarkdownPipelineBuilder UseAutoLinks(this BitMarkdownPipelineBuilder b)
        => b.Use(new BitMarkdownAutoLinkExtension());

    /// <summary>Adds <c>:shortcode:</c> emoji replacement.</summary>
    public static BitMarkdownPipelineBuilder UseEmojis(this BitMarkdownPipelineBuilder b)
        => b.Use(new BitMarkdownEmojiExtension());

    /// <summary>Adds <c>:shortcode:</c> emoji replacement with per-pipeline emoji overrides.</summary>
    /// <remarks>
    /// A flavor is configured once. Call this before <see cref="UseAdvanced"/> - which adds emoji
    /// itself, and defers to a configuration already made - rather than after it, where the second
    /// call would be a second configuration of the same flavor and throws.
    /// </remarks>
    public static BitMarkdownPipelineBuilder UseEmojis(this BitMarkdownPipelineBuilder b, IReadOnlyDictionary<string, string> overrides)
        => b.Use(new BitMarkdownEmojiExtension(overrides));

    /// <summary>
    /// Adds the emphasis flavors beyond <c>*</c>, <c>_</c> and <c>~~</c>: <c>~subscript~</c>,
    /// <c>^superscript^</c>, <c>++inserted++</c> and <c>==highlighted==</c>. Implies strikethrough,
    /// since subscript shares the <c>~</c> character with it.
    /// </summary>
    public static BitMarkdownPipelineBuilder UseEmphasisExtras(this BitMarkdownPipelineBuilder b)
        => b.Use(new BitMarkdownEmphasisExtrasExtension());

    /// <summary>
    /// Adds YAML (<c>---</c>) and TOML (<c>+++</c>) front matter, so a metadata block at the top of
    /// the document is read out of the source instead of rendered as a rule and a heading.
    /// </summary>
    public static BitMarkdownPipelineBuilder UseFrontMatter(this BitMarkdownPipelineBuilder b)
        => b.Use(new BitMarkdownFrontMatterExtension());

    /// <summary>
    /// Adds typographic replacement: curly quotes, en and em dashes, ellipses and guillemets.
    /// Code spans, code blocks and URLs are left exactly as written.
    /// </summary>
    public static BitMarkdownPipelineBuilder UseSmartyPants(this BitMarkdownPipelineBuilder b)
        => b.Use(new BitMarkdownSmartyPantsExtension());

    /// <summary>
    /// Adds custom containers: <c>:::name optional title</c> ... <c>:::</c> renders as a
    /// <c>div</c> classed after the name, which is how documentation sites write admonitions and
    /// layout blocks. Containers nest.
    /// </summary>
    public static BitMarkdownPipelineBuilder UseContainers(this BitMarkdownPipelineBuilder b)
        => b.Use(new BitMarkdownContainerExtension());

    /// <summary>
    /// Adds definition lists: a term on its own line followed by <c>: its definition</c> renders
    /// as a real <c>&lt;dl&gt;</c>.
    /// </summary>
    public static BitMarkdownPipelineBuilder UseDefinitionLists(this BitMarkdownPipelineBuilder b)
        => b.Use(new BitMarkdownDefinitionListExtension());

    /// <summary>
    /// Adds abbreviations: <c>*[HTML]: HyperText Markup Language</c> declares a term once and
    /// every whole-word occurrence of it becomes an <c>&lt;abbr&gt;</c> carrying the expansion.
    /// </summary>
    public static BitMarkdownPipelineBuilder UseAbbreviations(this BitMarkdownPipelineBuilder b)
        => b.Use(new BitMarkdownAbbreviationExtension());

    /// <summary>
    /// Adds mathematics: <c>$inline$</c> and <c>$$display$$</c> are kept verbatim and marked for a
    /// client-side typesetter such as KaTeX or MathJax.
    /// </summary>
    public static BitMarkdownPipelineBuilder UseMathematics(this BitMarkdownPipelineBuilder b)
        => b.Use(new BitMarkdownMathExtension());

    /// <summary>
    /// Adds figures: an image alone in a paragraph and written with a title renders as a
    /// <c>&lt;figure&gt;</c> with that title as its <c>&lt;figcaption&gt;</c>.
    /// </summary>
    public static BitMarkdownPipelineBuilder UseFigures(this BitMarkdownPipelineBuilder b)
        => b.Use(new BitMarkdownFigureExtension());

    /// <summary>Adds automatic heading id slugs.</summary>
    public static BitMarkdownPipelineBuilder UseAutoIdentifiers(this BitMarkdownPipelineBuilder b)
        => b.Use(new BitMarkdownAutoIdentifierExtension());

    /// <summary>
    /// Adds automatic heading id slugs and, when <paramref name="anchorLinks"/> is <c>true</c>, a
    /// permalink after each heading so a reader can copy a link straight to that section.
    /// </summary>
    /// <remarks>
    /// A flavor is configured once. Call this before <see cref="UseAdvanced"/> - which adds
    /// auto-identifiers itself, and defers to a configuration already made - rather than after it,
    /// where the second call would be a second configuration of the same flavor and throws.
    /// </remarks>
    public static BitMarkdownPipelineBuilder UseAutoIdentifiers(this BitMarkdownPipelineBuilder b, bool anchorLinks)
        => b.Use(new BitMarkdownAutoIdentifierExtension(anchorLinks));

    /// <summary>Adds GitHub-style footnotes (<c>[^1]</c> plus a <c>[^1]: ...</c> definition).</summary>
    public static BitMarkdownPipelineBuilder UseFootnotes(this BitMarkdownPipelineBuilder b)
        => b.Use(new BitMarkdownFootnoteExtension());

    /// <summary>Adds GitHub alerts (<c>&gt; [!NOTE]</c>, <c>&gt; [!TIP]</c>, ...).</summary>
    public static BitMarkdownPipelineBuilder UseAlerts(this BitMarkdownPipelineBuilder b)
        => b.Use(new BitMarkdownAlertExtension());

    /// <summary>
    /// Rewrites every link and image destination through a function of your own. The result is
    /// sanitized again on the way out, so a rewriter can never reintroduce an unsafe destination;
    /// returning <c>null</c> drops the destination and keeps the text.
    /// </summary>
    /// <remarks>
    /// Unlike the other flavors this one may be added more than once, each rewrite running in turn
    /// in the order it was added - so a base URL and a rewriter of your own compose, rather than
    /// one of them being the only one that applies.
    /// </remarks>
    public static BitMarkdownPipelineBuilder UseUrlRewriter(
        this BitMarkdownPipelineBuilder b, Func<BitMarkdownUrlRewriteContext, string?> rewrite)
        => b.Use(new BitMarkdownUrlRewriteExtension(rewrite));

    /// <summary>
    /// Resolves every relative link and image destination against <paramref name="baseUrl"/> -
    /// what a README needs before it can be rendered anywhere but the repository it came from.
    /// Absolute destinations and in-page fragments are left alone, and so are root-relative ones,
    /// which are already resolved against the site root.
    /// </summary>
    /// <remarks>
    /// This is a URL rewrite like <see cref="UseUrlRewriter"/>, so the two compose: a base URL
    /// followed by a rewriter of your own runs both, in that order.
    /// </remarks>
    public static BitMarkdownPipelineBuilder UseBaseUrl(this BitMarkdownPipelineBuilder b, string baseUrl)
        => b.Use(BitMarkdownUrlRewriteExtension.ForBaseUrl(baseUrl));

    /// <summary>
    /// Chooses the <c>target</c> and <c>rel</c> a rendered link carries, replacing the defaults
    /// (external links open in a new tab with <c>noopener noreferrer</c>). Pass
    /// <c>"noopener noreferrer nofollow ugc"</c> for links a site's own readers wrote.
    /// </summary>
    public static BitMarkdownPipelineBuilder UseLinkOptions(
        this BitMarkdownPipelineBuilder b,
        BitMarkdownLinkTarget externalTarget = BitMarkdownLinkTarget.Blank,
        string? externalRel = "noopener noreferrer",
        BitMarkdownLinkTarget internalTarget = BitMarkdownLinkTarget.Self,
        string? internalRel = null)
        => b.Use(new BitMarkdownLinkOptionsExtension(externalTarget, externalRel, internalTarget, internalRel));

    /// <summary>Renders every single newline as a line break, like a chat or comment box.</summary>
    public static BitMarkdownPipelineBuilder UseSoftLineAsHardLine(this BitMarkdownPipelineBuilder b)
        => b.Use(new BitMarkdownSoftLineAsHardLineExtension());

    /// <summary>
    /// Sets the words the renderers write themselves - alert titles, footnote back-links, the
    /// accessible names of the regions and controls the markup adds - so a rendered document can be
    /// in a language other than English.
    /// </summary>
    public static BitMarkdownPipelineBuilder UseTexts(this BitMarkdownPipelineBuilder b, BitMarkdownTexts texts)
    {
        ArgumentNullException.ThrowIfNull(b);
        ArgumentNullException.ThrowIfNull(texts);
        b.Texts = texts;
        return b;
    }

    /// <summary>Adds the full GitHub Flavored Markdown bundle: tables, strikethrough, task lists, autolinks, footnotes and alerts.</summary>
    public static BitMarkdownPipelineBuilder UseGitHubFlavored(this BitMarkdownPipelineBuilder b)
        => b.Use(new BitMarkdownGitHubFlavoredExtension());

    /// <summary>
    /// Adds the GitHub flavors plus front matter, the emphasis extras, containers, definition
    /// lists, abbreviations, figures, emoji and auto-identifiers - everything that reads a
    /// real-world document the way its author meant it, short of the two flavors that change what
    /// was written: SmartyPants, which rewrites characters, and mathematics, which needs a
    /// typesetter on the page to be worth anything.
    /// </summary>
    /// <remarks>
    /// The two flavors here that take options - emoji overrides and the auto-identifier's
    /// permalinks - are added only if they are not already on, so configuring one before the bundle
    /// keeps that configuration instead of being overruled by the bundle's default.
    /// </remarks>
    public static BitMarkdownPipelineBuilder UseAdvanced(this BitMarkdownPipelineBuilder b)
    {
        ArgumentNullException.ThrowIfNull(b);

        return b.UseGitHubFlavored()
            .UseFrontMatter()
            .UseEmphasisExtras()
            .UseContainers()
            .UseDefinitionLists()
            .UseAbbreviations()
            .UseFigures()
            .UseIfAbsent(new BitMarkdownEmojiExtension())
            .UseIfAbsent(new BitMarkdownAutoIdentifierExtension());
    }
}
