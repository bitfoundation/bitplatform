namespace Bit.BlazorUI;

/// <summary>
/// Rewrites every link and image destination in the document through a function of your own -
/// resolving a README's relative paths against the repository it came from, pointing images at a
/// CDN, or stripping tracking parameters.
/// </summary>
public sealed class BitMarkdownUrlRewriteExtension : IBitMarkdownExtension
{
    private readonly Func<BitMarkdownUrlRewriteContext, string?> _rewrite;

    /// <summary>Creates the extension around the rewrite to run.</summary>
    /// <remarks>
    /// A pipeline is shared across concurrent parses, so the function must be pure and
    /// thread-safe: given the same URL it has to return the same result, whoever is asking.
    /// </remarks>
    public BitMarkdownUrlRewriteExtension(Func<BitMarkdownUrlRewriteContext, string?> rewrite)
    {
        ArgumentNullException.ThrowIfNull(rewrite);
        _rewrite = rewrite;
    }

    /// <summary>
    /// Creates an extension that resolves every relative destination against
    /// <paramref name="baseUrl"/>, leaving absolute ones and in-page fragments alone.
    /// </summary>
    public static BitMarkdownUrlRewriteExtension ForBaseUrl(string baseUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);

        // Resolved once, so every document parsed through this pipeline pays for it once.
        if (Uri.TryCreate(baseUrl, UriKind.Absolute, out var absoluteBase) is false)
        {
            absoluteBase = null;
        }
        string prefix = baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/";

        return new BitMarkdownUrlRewriteExtension(context =>
        {
            if (context.IsRelative is false) return context.Url;

            string relative = context.Url.StartsWith("./", StringComparison.Ordinal)
                ? context.Url[2..]
                : context.Url;

            if (absoluteBase is not null && Uri.TryCreate(absoluteBase, relative, out var resolved))
            {
                return resolved.ToString();
            }

            // A root-relative destination is already resolved against the site root, which is
            // exactly what the absolute-base branch above does with one ("/img/x" under
            // "https://b.com/r/" is "https://b.com/img/x"). A site-relative prefix therefore
            // leaves it alone rather than prepending itself and pointing at nothing.
            if (relative.StartsWith('/')) return relative;

            // A relative base ("/docs/") cannot be resolved by Uri, so the two are joined by
            // hand - which is all a site-relative prefix ever needed.
            return prefix + relative;
        });
    }

    // Several rewrites may be registered, each running in turn, so a base URL and a rewriter of
    // one's own compose. Refusing the second - which is the only other honest option, since the
    // extension is nothing but the function it was handed - would rule out the one combination
    // this pair is most often written as.
    public bool AllowsMultiple => true;

    public void Setup(BitMarkdownPipelineBuilder builder)
        => builder.AstProcessors.Add(new BitMarkdownUrlRewriteAstProcessor(_rewrite));
}
