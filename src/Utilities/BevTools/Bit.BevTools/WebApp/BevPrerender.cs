using Microsoft.AspNetCore.Components;

namespace Bit.BevTools;

public sealed record BevPrerenderComponent(string Name, int Renders, double TotalMs);

public sealed record BevPrerenderRequest(string Method, string Url, int? StatusCode, double? DurationMs, string? Error);

/// <summary>
/// What prerendering did for the page that is now interactive: it ran on the server, in a session of its own that
/// ended with the response, so the interactive session cannot see it. The prerendering panel hands this over through
/// <see cref="PersistentComponentState"/>, the framework's own channel from prerendering to interactivity; it is what
/// tells a developer that <c>OnInitializedAsync</c> ran twice and fetched the same data twice.
/// </summary>
public sealed class BevPrerenderSummary
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

    public List<BevPrerenderComponent> Slowest { get; set; } = [];

    public List<BevPrerenderRequest> Requests { get; set; } = [];

    public List<string> Errors { get; set; } = [];

    /// <summary>The render mode the interactive page was prerendered for ("InteractiveAuto"), when known.</summary>
    public string? RenderMode { get; set; }

    internal static BevPrerenderSummary From(BevToolsSession session, string? path, string? renderMode)
    {
        var components = session.Components.All.Where(c => !c.IsBevTools && !c.IsHidden).ToArray();
        return new BevPrerenderSummary
        {
            At = DateTimeOffset.UtcNow,
            Path = path,
            DurationMs = Math.Round((DateTimeOffset.UtcNow - session.StartedAt).TotalMilliseconds, 2),
            Components = components.Length,
            Renders = components.Sum(c => c.RenderCount),
            RenderMs = Math.Round(components.Sum(c => c.TotalRenderMs), 3),
            Slowest = components.Where(c => c.RenderCount > 0).OrderByDescending(c => c.TotalRenderMs).Take(MaxComponents)
                .Select(c => new BevPrerenderComponent(c.DisplayName, c.RenderCount, Math.Round(c.TotalRenderMs, 3))).ToList(),
            Requests = session.Http.Snapshot().Take(MaxRequests)
                .Select(r => new BevPrerenderRequest(r.Method, r.Url, r.StatusCode, r.DurationMs is { } d ? Math.Round(d, 2) : null, r.Error)).ToList(),
            Errors = session.Errors.Snapshot().Take(MaxErrors).Select(e => ErrorCenter.ShortTypeName(e.ExceptionType) + ": " + e.Message).ToList(),
            RenderMode = renderMode,
        };
    }

    /// <summary>"GET /api/products": what a duplicate is matched by, the query included (another page of results is another request).</summary>
    internal static string Signature(string method, string url) => method + " " + BevToolsHttpMessageHandler.StripOrigin(url);
}

/// <summary>Registers the persisting callback while prerendering, and takes the summary back once interactive.</summary>
internal static class BevPrerenderHandoff
{
    /// <summary>
    /// Persists the prerendering session's summary when the response persists component state. Only with
    /// <see cref="BevValueAccess.Full"/>: the summary travels in the page's HTML (in the clear for WebAssembly), and the
    /// server-side request URLs in it are not something a production page should carry to every visitor.
    /// </summary>
    public static IDisposable? RegisterWhilePrerendering(BevToolsSession session, IServiceProvider services, ComponentBase host, NavigationManager navigation)
    {
        if (session.ValueAccess != BevValueAccess.Full || services.GetService(typeof(PersistentComponentState)) is not PersistentComponentState state)
        {
            return null;
        }

        var renderMode = InteractiveRenderModeOf(host);
        try
        {
            Func<Task> persist = () =>
            {
                string? path = null;
                try
                {
                    path = session.Redactor.RedactUrl("/" + navigation.ToBaseRelativePath(navigation.Uri));
                }
                catch
                {
                    // a NavigationManager that was not initialized; the summary goes without a path.
                }

                state.PersistAsJson(BevPrerenderSummary.StateKey, BevPrerenderSummary.From(session, path, BevRenderModes.Describe(renderMode)));
                return Task.CompletedTask;
            };

            // The state must be persisted for the render mode the page becomes interactive in; without one the framework
            // has to infer it, which it cannot do on a page that uses both Server and WebAssembly.
            return renderMode is null ? state.RegisterOnPersisting(persist) : state.RegisterOnPersisting(persist, renderMode);
        }
        catch
        {
            return null;
        }
    }

    public static BevPrerenderSummary? TakeWhenInteractive(IServiceProvider services)
    {
        try
        {
            return services.GetService(typeof(PersistentComponentState)) is PersistentComponentState state
                && state.TryTakeFromJson<BevPrerenderSummary>(BevPrerenderSummary.StateKey, out var summary)
                ? summary
                : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// The interactive render mode that applies to the panel: assigned to it, or to the nearest ancestor that has one
    /// (<c>&lt;Routes @rendermode="InteractiveAuto" /&gt;</c>, with the panel inside).
    /// </summary>
    private static IComponentRenderMode? InteractiveRenderModeOf(ComponentBase host)
    {
        if (BlazorReflection.ResolveRenderer(host, out _) is not { } renderer)
        {
            return null;
        }

        var state = BlazorReflection.ResolveState(renderer, host);
        for (var hops = 0; state is not null && hops < 256; hops++)
        {
            if (BlazorReflection.AssignedRenderMode(renderer, state.Component) is { } mode)
            {
                return mode;
            }

            state = state.ParentComponentState;
        }

        return null;
    }
}
