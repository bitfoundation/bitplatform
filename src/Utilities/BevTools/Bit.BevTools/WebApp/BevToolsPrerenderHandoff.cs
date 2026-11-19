using Microsoft.AspNetCore.Components;

namespace Bit.BevTools;

/// <summary>Registers the persisting callback while prerendering, and takes the summary back once interactive.</summary>
internal static class BevToolsPrerenderHandoff
{
    /// <summary>
    /// Persists the prerendering session's summary when the response persists component state. Only with
    /// <see cref="BevToolsValueAccess.Full"/>: the summary travels in the page's HTML (in the clear for WebAssembly), and the
    /// server-side request URLs in it are not something a production page should carry to every visitor.
    /// </summary>
    public static IDisposable? RegisterWhilePrerendering(BevToolsSession session, IServiceProvider services, ComponentBase host, NavigationManager navigation)
    {
        if (session.ValueAccess != BevToolsValueAccess.Full || services.GetService(typeof(PersistentComponentState)) is not PersistentComponentState state)
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

                state.PersistAsJson(BevToolsPrerenderSummary.StateKey, BevToolsPrerenderSummary.From(session, path, BevToolsRenderModes.Describe(renderMode)));
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

    public static BevToolsPrerenderSummary? TakeWhenInteractive(IServiceProvider services)
    {
        try
        {
            return services.GetService(typeof(PersistentComponentState)) is PersistentComponentState state
                && state.TryTakeFromJson<BevToolsPrerenderSummary>(BevToolsPrerenderSummary.StateKey, out var summary)
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
        if (BevToolsBlazorReflection.ResolveRenderer(host, out _) is not { } renderer)
        {
            return null;
        }

        var state = BevToolsBlazorReflection.ResolveState(renderer, host);
        for (var hops = 0; state is not null && hops < 256; hops++)
        {
            if (BevToolsBlazorReflection.AssignedRenderMode(renderer, state.Component) is { } mode)
            {
                return mode;
            }

            state = state.ParentComponentState;
        }

        return null;
    }
}
