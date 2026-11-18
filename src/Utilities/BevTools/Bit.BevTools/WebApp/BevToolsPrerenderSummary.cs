using Microsoft.AspNetCore.Components;

namespace Bit.BevTools;

/// <summary>
/// What prerendering did for the page that is now interactive: it ran on the server, in a session of its own that
/// ended with the response, so the interactive session cannot see it. The prerendering panel hands this over through
/// <see cref="PersistentComponentState"/>, the framework's own channel from prerendering to interactivity; it is what
/// tells a developer that <c>OnInitializedAsync</c> ran twice and fetched the same data twice.
/// </summary>
public sealed class BevToolsPrerenderSummary
{
    /// <summary>The key the summary is persisted under.</summary>
    public const string StateKey = "bit-bevtools:prerender";

    private const int MaxComponents = 10;
    private const int MaxRequests = 50;
    private const int MaxErrors = 20;

    public DateTimeOffset At { get; set; }

    /// <summary>The prerendered path, redacted like every URL BevTools records.</summary>
    public string? Path { get; set; }

    /// <summary>From the start of the prerendering session to the moment the state was persisted (the end of rendering).</summary>
    public double DurationMs { get; set; }

    public int Components { get; set; }

    public int Renders { get; set; }

    public double RenderMs { get; set; }

    public List<BevToolsPrerenderComponent> Slowest { get; set; } = [];

    public List<BevToolsPrerenderRequest> Requests { get; set; } = [];

    public List<string> Errors { get; set; } = [];

    /// <summary>The render mode the interactive page was prerendered for ("InteractiveAuto"), when known.</summary>
    public string? RenderMode { get; set; }

    internal static BevToolsPrerenderSummary From(BevToolsSession session, string? path, string? renderMode)
    {
        var components = session.Components.All.Where(c => !c.IsBevTools && !c.IsHidden).ToArray();
        return new BevToolsPrerenderSummary
        {
            At = DateTimeOffset.UtcNow,
            Path = path,
            DurationMs = Math.Round((DateTimeOffset.UtcNow - session.StartedAt).TotalMilliseconds, 2),
            Components = components.Length,
            Renders = components.Sum(c => c.RenderCount),
            RenderMs = Math.Round(components.Sum(c => c.TotalRenderMs), 3),
            Slowest = components.Where(c => c.RenderCount > 0).OrderByDescending(c => c.TotalRenderMs).Take(MaxComponents)
                .Select(c => new BevToolsPrerenderComponent(c.DisplayName, c.RenderCount, Math.Round(c.TotalRenderMs, 3))).ToList(),
            Requests = session.Http.Snapshot().Take(MaxRequests)
                .Select(r => new BevToolsPrerenderRequest(r.Method, r.Url, r.StatusCode, r.DurationMs is { } d ? Math.Round(d, 2) : null, r.Error)).ToList(),
            Errors = session.Errors.Snapshot().Take(MaxErrors).Select(e => BevToolsErrorCenter.ShortTypeName(e.ExceptionType) + ": " + e.Message).ToList(),
            RenderMode = renderMode,
        };
    }

    /// <summary>"GET /api/products": what a duplicate is matched by, the query included (another page of results is another request).</summary>
    internal static string Signature(string method, string url) => method + " " + BevToolsHttpMessageHandler.StripOrigin(url);
}
