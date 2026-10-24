namespace Bit.BlazorUI;

/// <summary>
/// Which images the views fetch: those of Markdown, image parts, image artifacts, and the favicons of sources.
/// </summary>
/// <remarks>
/// <para>
/// A model can be talked into emitting an image URL, and a browser fetching that URL leaks the
/// user's address and the fact that they read the message to whoever controls the host. The
/// default allows images, as a chat UI is expected to; a host handling sensitive conversations can
/// restrict them to its own CDN, ask before loading, or block them outright. A favicon is never
/// worth a placeholder of its own, so one the policy does not show outright is left out.
/// </para>
/// <para>
/// Script-capable URL schemes are neutralised regardless of the policy. Inline <c>data:</c> images
/// are fetched from nowhere, so they are shown under every policy except <see cref="BitAiImageMode.Block"/>.
/// </para>
/// </remarks>
public sealed record BitAiImagePolicy
{
    private readonly HashSet<string> _hosts = new(StringComparer.OrdinalIgnoreCase);



    /// <summary>Show every image whose URL scheme is safe. The default.</summary>
    public static BitAiImagePolicy Allow { get; } = new() { Mode = BitAiImageMode.Allow };

    /// <summary>Show no remote images.</summary>
    public static BitAiImagePolicy Block { get; } = new() { Mode = BitAiImageMode.Block };

    /// <summary>Load each remote image only when the user asks.</summary>
    public static BitAiImagePolicy ClickToLoad { get; } = new() { Mode = BitAiImageMode.ClickToLoad };



    /// <summary>Whether inline <c>data:</c> images are shown. They involve no network request.</summary>
    public bool AllowDataImages { get; init; } = true;

    /// <summary>The hosts allowed under <see cref="BitAiImageMode.AllowListedHosts"/>.</summary>
    public IReadOnlyCollection<string> Hosts => _hosts;

    /// <summary>How images are treated.</summary>
    public BitAiImageMode Mode { get; init; } = BitAiImageMode.Allow;



    /// <summary>Show remote images only from the given hosts, and a placeholder for the rest.</summary>
    /// <param name="hosts">Host names, compared without regard to case. A leading <c>*.</c> matches subdomains.</param>
    /// <returns>The policy.</returns>
    /// <remarks>
    /// A relative image is fetched from the host of the document's base address, which is what the views ask about it:
    /// list that host to show the app's own images.
    /// </remarks>
    public static BitAiImagePolicy AllowListedHosts(params string[] hosts)
    {
        ArgumentNullException.ThrowIfNull(hosts);

        var policy = new BitAiImagePolicy { Mode = BitAiImageMode.AllowListedHosts };
        foreach (var host in hosts)
        {
            if (string.IsNullOrWhiteSpace(host) is false) policy._hosts.Add(host.Trim());
        }

        return policy;
    }

    /// <summary>
    /// Decides what to do with one image.
    /// </summary>
    /// <param name="url">
    /// The image source, already checked for a safe scheme. The views ask about the address the browser fetches: a
    /// relative or protocol-relative source resolved against the base address of the document. One handed in still
    /// relative has no host, so no host list shows it.
    /// </param>
    /// <returns>Whether to show it, show a placeholder that loads it on request, or hide it.</returns>
    public BitAiImageDecision Decide(string url)
    {
        if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return AllowDataImages && Mode != BitAiImageMode.Block ? BitAiImageDecision.Show : BitAiImageDecision.Hide;
        }

        return Mode switch
        {
            BitAiImageMode.Allow => BitAiImageDecision.Show,
            BitAiImageMode.Block => BitAiImageDecision.Hide,
            BitAiImageMode.ClickToLoad => BitAiImageDecision.AskFirst,
            _ => IsListed(url) ? BitAiImageDecision.Show : BitAiImageDecision.AskFirst,
        };
    }



    private bool IsListed(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri) is false || uri.Host.Length == 0) return false;

        var host = uri.Host;
        if (_hosts.Contains(host)) return true;

        foreach (var listed in _hosts)
        {
            if (listed.StartsWith("*.", StringComparison.Ordinal)
                && host.EndsWith(listed.AsSpan(1), StringComparison.OrdinalIgnoreCase)
                && host.Length > listed.Length - 1)
            {
                return true;
            }
        }

        return false;
    }
}
