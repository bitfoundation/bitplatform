namespace Bit.Butil.Demo.Server.Dtos;

/// <summary>One row of the browser-support matrix: an API, and what it needs from the page.</summary>
public record ButilCapabilityDto
{
    /// <summary>The documented API, e.g. "Clipboard" or "Local &amp; Session Storage".</summary>
    public required string Api { get; init; }

    /// <summary>The Bit.Butil types behind it - what you actually inject or call.</summary>
    public required string[] Services { get; init; }

    /// <summary>Which engines implement it: "All engines", "Varies by engine", "Chromium only", ...</summary>
    public required string BrowserSupport { get; init; }

    /// <summary>The preconditions the calling page has to satisfy, one sentence each.</summary>
    public required string[] Requires { get; init; }

    public required string Summary { get; init; }

    /// <summary>The documentation page for the API, on the live site.</summary>
    public required string DocsUrl { get; init; }
}
