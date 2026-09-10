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

            // A relative base ("/docs/") cannot be resolved by Uri, so the two are joined by
            // hand - which is all a site-relative prefix ever needed.
            return prefix + relative.TrimStart('/');
        });
    }

    public void Setup(BitMarkdownPipelineBuilder builder)
        => builder.AstProcessors.Add(new BitMarkdownUrlRewriteAstProcessor(_rewrite));
}
