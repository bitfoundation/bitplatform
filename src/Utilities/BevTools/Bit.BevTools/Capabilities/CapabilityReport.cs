
namespace Bit.BevTools;

/// <summary>Builds the honest capability matrix for a session, combining static facts with what was actually observed at runtime.</summary>
internal static class CapabilityReport
{
    /// <summary>
    /// Blazor WebAssembly builds with the System.Diagnostics.Metrics feature switch off, which silently turns every
    /// meter into a no-op. Reporting "not yet observed" in that case would be a lie the developer cannot act on.
    /// </summary>
    internal static bool MetricsAreSupported() =>
        !AppContext.TryGetSwitch("System.Diagnostics.Metrics.Meter.IsSupported", out var supported) || supported;

    /// <summary>Explains a missing measurement in terms the developer can act on.</summary>
    internal static string MetricsHint(bool metricsSupported) => metricsSupported
        ? "No aspnetcore.components.render_diff measurements seen yet; they arrive with the first render batch."
        : "System.Diagnostics.Metrics is compiled out of this app: Blazor WebAssembly disables it by default. "
          + "Add <MetricsSupport>true</MetricsSupport> to the WebAssembly project to get diff durations and lifecycle timings.";

    /// <summary>
    /// Whether the runtime this build targets has the framework's ActivitySource and meters at all: they arrived in
    /// .NET 10. On .NET 8 and 9 "not observed yet" would send the developer looking for a switch that does not exist.
    /// </summary>
    internal static bool FrameworkInstrumentationExists =>
#if NET10_0_OR_GREATER
        true;
#else
        false;
#endif

    internal const string EarlierRuntimeHint = "Requires .NET 10: the Microsoft.AspNetCore.Components ActivitySource and meters this comes from do not exist on earlier runtimes.";

    public static IReadOnlyList<BevToolsCapability> Build(BevToolsSession session)
    {
        var platform = session.Platform ?? "unknown";
        var isServer = platform == "Server";
        var reflectionOk = BlazorReflection.CanWrapRenderFragment;
        var hierarchyOk = BlazorReflection.CanResolveHierarchy;
        var batchesOk = BlazorReflection.CanDetectBatches;
        var renderQueueOk = BlazorReflection.CanReadRenderQueue;
        var eventsObserved = ActivityObserver.HandleEventActivitiesObserved;
        var navObserved = ActivityObserver.NavigateActivitiesObserved;
        var lifecycleObserved = ActivityObserver.LifecycleMetricsObserved;
        var batchMetricsObserved = ActivityObserver.BatchMetricsObserved;
        var diffAttributionLost = session.RenderTracker.DiffAttributionLost;
        var bridge = session.Browser.BridgeAttached;
        var metricsSupported = MetricsAreSupported();
        var metricsHint = FrameworkInstrumentationExists ? MetricsHint(metricsSupported) : EarlierRuntimeHint;
        var attachment = session.Attachment;
        var eventsHint = !FrameworkInstrumentationExists
            ? EarlierRuntimeHint + " Renders inside events are labeled StateHasChanged."
            : attachment is null
                ? "No HandleEvent activities observed yet: interact with the app once. Requires BitBevToolsOptions.UseFrameworkInstrumentation; until an event is seen, renders inside events are labeled StateHasChanged."
                : session.FrameworkInstrumentation.ActivitySource
                    ? "No HandleEvent activities observed yet: interact with the app once. Until an event is seen, renders inside events are labeled StateHasChanged."
                    : "BevTools was attached at runtime, and a renderer reads the framework ActivitySource only when it is created, from the services registered at startup: register it there (Bit.BevTools.Loader does, or call AddComponentsTracing). Until then renders inside events are labeled StateHasChanged.";

        return
        [
            attachment is null
                ? new("Components", "Component instances", BevCapabilityStatus.Available, "Every component is created through the public IComponentActivator hook, so all instances are known without touching application code.")
                : new("Components", "Component instances", attachment is { ActivatorAttached: true, ExistingComponentsRead: true } ? BevCapabilityStatus.Available : BevCapabilityStatus.Partial, AttachedComponents(attachment)),
            new("Components", "Parent/child hierarchy", hierarchyOk ? BevCapabilityStatus.Available : BevCapabilityStatus.NotAvailable, hierarchyOk ? "Resolved from the renderer's ComponentState (public since .NET 8) through a cached reflection accessor." : "The renderer's component state map could not be accessed on this framework version."),
            new("Components", "Parameters and cascading values", BevCapabilityStatus.Available, "Read from [Parameter]/[CascadingParameter] properties on demand; values are inspected lazily and sensitive names are redacted."),
            new("Components", "Component state (fields)", BevCapabilityStatus.Partial, "Fields are read on demand and diffed per render for the selected component only. Changes made without a render are not observed."),
            new("Components", "Injected services", BevCapabilityStatus.Available, "[Inject] properties are listed with their registered lifetime."),
            new("Components", "Source location", BevCapabilityStatus.Available, "With the app's symbols (its portable PDB, downloaded on WebAssembly): the .razor or .cs file a component is written in and the line of each event handler and callback it binds, as links that open an editor (Ui.EditorUrlTemplate). Without symbols (Release builds, PDBs not deployed) the location is inferred from the type name and labelled so."),
            new("Components", "Event handlers and callbacks it bound", BlazorReflection.CanReadRenderTree ? BevCapabilityStatus.RequiresInstrumentation : BevCapabilityStatus.NotAvailable, BlazorReflection.CanReadRenderTree
                ? "Read on demand from the frames of the component's latest render: DOM handlers, @bind change handlers and the callbacks it passes to child components, each named by the method it runs."
                : "The renderer's render tree could not be accessed on this framework version."),
            new("Components", "Render mode", BlazorReflection.GetComponentRenderModeMethod is not null ? BevCapabilityStatus.RequiresInstrumentation : BevCapabilityStatus.NotAvailable, BlazorReflection.GetComponentRenderModeMethod is not null
                ? "What the renderer assigned (@rendermode, [RenderMode*]), read on each component's first render; the tree marks where each subtree starts."
                : "The renderer's render mode lookup could not be accessed on this framework version."),
            new("Components", "Values (parameters, fields, state, claims)", session.ValueAccess == BevValueAccess.Full ? BevCapabilityStatus.Available : BevCapabilityStatus.Partial, session.ValueAccess switch
            {
                BevValueAccess.Full => "Read with redaction (names, [BevToolsSensitive], credential shapes); configuration and key material are never read.",
                BevValueAccess.Shallow => "ValueAccess is Shallow: scalars only, objects show their type; forced re-render detection is off.",
                _ => "ValueAccess is None: names and types only, because this process serves many users outside Development. Set BitBevToolsOptions.ValueAccess to read more.",
            }),
            new("Components", "Disposal", BevCapabilityStatus.Partial, "Detected when the renderer forgets the instance (checked on each refresh), so timing is approximate."),
            new("Rendering", "Render count and BuildRenderTree duration", reflectionOk ? BevCapabilityStatus.RequiresInstrumentation : BevCapabilityStatus.NotAvailable, reflectionOk ? "The private render fragment of each ComponentBase is wrapped at creation; components that implement IComponent directly are not measured." : "ComponentBase internals changed; render measurement is unavailable."),
            new("Rendering", "Render batches", batchesOk ? BevCapabilityStatus.RequiresInstrumentation : BevCapabilityStatus.NotReliable, batchesOk ? "Batch boundaries come from the renderer's batch builder; a batch is one diff sent to the browser." : "Falling back to idle-time heuristics for batch boundaries."),
            new("Rendering", "Batch diff duration and DOM edit count", diffAttributionLost ? BevCapabilityStatus.NotReliable : batchMetricsObserved ? BevCapabilityStatus.Available : metricsSupported ? BevCapabilityStatus.RequiresRuntimeSupport : BevCapabilityStatus.NotAvailable, diffAttributionLost ? "Attribution stopped after the browser acknowledgement backlog exceeded the bounded queue. Later measurements are omitted rather than attached to the wrong batch." : batchMetricsObserved ? "Observed through the aspnetcore.components.render_diff meters (.NET 10). On Blazor Server the measurement arrives when the browser acknowledges the render, so it is matched back to the batch it belongs to." : metricsHint),
            new("Rendering", "Render cause: parent re-render", hierarchyOk && batchesOk ? BevCapabilityStatus.Available : BevCapabilityStatus.NotReliable, "Attributed when an ancestor rendered earlier in the same batch (the renderer re-applies parameters)."),
            new("Rendering", "Render cause: UI event", !eventsObserved ? BevCapabilityStatus.RequiresRuntimeSupport : renderQueueOk ? BevCapabilityStatus.Available : BevCapabilityStatus.Partial, eventsObserved ? "Observed through the Microsoft.AspNetCore.Components.HandleEvent activity (.NET 10). AddBitBevToolsServices registers the framework ActivitySource itself, which is what makes this work outside server-side rendering. Work a handler starts without awaiting it (a timer, a fire-and-forget task) is not attributed to the event once the event has ended." + (renderQueueOk ? "" : " The renderer's render queue could not be read on this framework version, so the render a synchronous handler queued, which the framework processes after the event ended, is labeled StateHasChanged.") : eventsHint),
            new("Rendering", "Render cause: explicit StateHasChanged", BevCapabilityStatus.Partial, "Anything not attributable to a parent, event or navigation is reported as StateHasChanged (timer, async continuation, state notification). The caller is not identifiable without a stack walk."),
            new("Rendering", "Lifecycle method durations (SetParametersAsync)", lifecycleObserved ? BevCapabilityStatus.Available : metricsSupported ? BevCapabilityStatus.RequiresRuntimeSupport : BevCapabilityStatus.NotAvailable, lifecycleObserved ? "Per component type, from aspnetcore.components.update_parameters.duration (.NET 10)." : metricsHint),
            new("Events", "UI events with handler duration", eventsObserved ? BevCapabilityStatus.Available : BevCapabilityStatus.RequiresRuntimeSupport, FrameworkInstrumentationExists ? "From the framework ActivitySource; includes component type, method and attribute name." : EarlierRuntimeHint),
            new("Events", "Navigation", navObserved || session.LastNavigationEventId is not null ? BevCapabilityStatus.Available : BevCapabilityStatus.Partial, "From NavigationManager.LocationChanged and the Navigate activity when available."),
            new("State", "Registered state providers", BevCapabilityStatus.RequiresIntegration, "Implement IBevStateProvider (or register an adapter) to get snapshots, change history and before/after diffs."),
            new("State", "Authentication state", session.State.Entries.Any(e => e.Provider is BevAuthenticationStateProvider) ? BevCapabilityStatus.Available : BevCapabilityStatus.RequiresIntegration,
                "From the scope's AuthenticationStateProvider, with a change history; the app registers one when it uses authentication. Claim values follow ValueAccess."),
            new("Routing", "Current route, route values, query", session.Components.All.Any(r => r.Component is Microsoft.AspNetCore.Components.RouteView) ? BevCapabilityStatus.Available : BevCapabilityStatus.Partial,
                "From the RouteView's RouteData and NavigationManager. Without a RouteView in this session (the router runs during static rendering) only the URI is known."),
            new("Routing", "Matched route template", BevCapabilityStatus.Partial,
                (FrameworkInstrumentationExists ? "RouteData.Template when the router fills it in (.NET 10); otherwise" : "RouteData has no template before .NET 10, so it is")
                + " inferred for a page with a single @page, and labeled so; a page with several stays unknown."),
            new("Rendering", "What prerendering did", session.Prerender is not null ? BevCapabilityStatus.Available : BevCapabilityStatus.RequiresInstrumentation,
                session.Prerender is not null ? "Handed over from the prerendering session through persistent component state." : "Arrives when the page was prerendered with the BevTools panel in it, and ValueAccess is Full (the summary travels in the page)."),
            new("State", "Cascading values", BevCapabilityStatus.Available, "Read from [CascadingParameter] properties of the selected component."),
            attachment is null
                ? new("Network", "HTTP requests via IHttpClientFactory", BevCapabilityStatus.Available, "A DelegatingHandler is added to every factory pipeline through IHttpMessageHandlerBuilderFilter. Hand-built HttpClient instances need BevToolsHttpMessageHandler added manually.")
                : new("Network", "HTTP requests", attachment.HttpClient is BevHttpClientAttachment.InPipeline or BevHttpClientAttachment.Wrapped || attachment.FactoryClientsHooked ? BevCapabilityStatus.Partial : BevCapabilityStatus.RequiresIntegration, AttachedHttp(attachment)),
            new("Network", "Request/response bodies", BevCapabilityStatus.NotAvailable, "Never captured: bodies routinely contain credentials and personal data."),
            new("Network", "Originating component", session.Options.CaptureHttpCallers ? BevCapabilityStatus.Available : BevCapabilityStatus.NotAvailable, session.Options.CaptureHttpCallers
                ? "Each request is linked to the UI event that issued it when one is executing, and names the component method that sent it, read from the stack (Sent from: OrderRow.OnInitializedAsync via ProductApi.GetOrderDetailsAsync). Work no component method waits on - a timer, a background loop - has none, and says why."
                : "BitBevToolsOptions.CaptureHttpCallers is off: requests are linked to the UI event that issued them only."),
            new("Network", "Requests per endpoint", BevCapabilityStatus.Available, "Grouped by method, path and query parameter names, with the ids in the path as {id}: requests, failures, cancellations, p50/p95/max and the top sender."),
            new("JS interop", ".NET → JS calls from components", BevCapabilityStatus.RequiresInstrumentation, "IJSRuntime injected into tracked components is wrapped on the first render, the earliest point after the framework injects it: calls made from OnInitialized/OnInitializedAsync happen before that and are not attributed. Module references obtained through a wrapped runtime are wrapped too." + JSRuntimeReadinessGap()),
            new("JS interop", ".NET → JS calls from services", BevCapabilityStatus.RequiresIntegration, "Services must call jsRuntime.WithBevToolsTracking(session). Decorating IJSRuntime globally is unsafe because the framework casts it to its concrete type."),
            new("JS interop", "JS → .NET calls", bridge ? BevCapabilityStatus.Available : BevCapabilityStatus.RequiresInstrumentation, "DotNet.invokeMethodAsync/invokeMethod are wrapped by the browser bridge and reported in batches."),
            new("Errors", "Render and event exceptions", BevCapabilityStatus.Available, "Render exceptions are caught in the wrapper; event exceptions and circuit failures arrive through Blazor's own logger categories."),
            new("Errors", "JS errors and unhandled promise rejections", bridge ? BevCapabilityStatus.Available : BevCapabilityStatus.RequiresInstrumentation, "window.onerror / unhandledrejection via the browser bridge."),
            new("Errors", "Resources that failed to load, CSP violations", bridge ? BevCapabilityStatus.Available : BevCapabilityStatus.RequiresInstrumentation, "Capture-phase error events on elements and securitypolicyviolation, via the browser bridge; URLs are redacted in the browser."),
            new("Circuit", "Circuit lifecycle, connection state, inbound message count", isServer && attachment is null ? BevCapabilityStatus.RequiresInstrumentation : BevCapabilityStatus.NotAvailable, "Blazor Server only, through a CircuitHandler registered by AddBitBevToolsServerServices().") { Platforms = "Server" },
            new("Circuit", "Reconnect attempts", bridge && isServer ? BevCapabilityStatus.Available : BevCapabilityStatus.RequiresInstrumentation, "From Blazor's components-reconnect-state-changed event, which it dispatches on the app's own reconnect dialog (#components-reconnect-modal, in the .NET 10 templates); with Blazor's default dialog there is none, and only the circuit's Connection down and up are recorded.") { Platforms = "Server" },
            new("Network", "WebSocket and SignalR traffic", !session.Options.TrackWebSockets ? BevCapabilityStatus.NotAvailable : bridge && session.Browser.Sockets.Count > 0 ? BevCapabilityStatus.Available : BevCapabilityStatus.RequiresInstrumentation,
                session.Options.TrackWebSockets
                    ? "Counted in the browser bridge by direction and SignalR message kind, with render batch sizes and the browser's time to apply them; payloads are never read. A socket that existed before BevTools attached is seen from its next send."
                    : "BitBevToolsOptions.TrackWebSockets is off."),
            new("Circuit", "Network latency and message payloads", BevCapabilityStatus.NotAvailable, "Not exposed by the framework; payloads are never read. Message sizes are counted in the browser (Network → WebSockets).") { Platforms = "Server" },
            attachment is null
                ? new("DI", "Registrations, lifetimes, constructor dependencies", BevCapabilityStatus.Available, "From the IServiceCollection captured at AddBitBevToolsServices time. Factory and instance registrations show no dependencies.")
                : attachment.ThroughLoader
                    ? new("DI", "Registrations, lifetimes, constructor dependencies", BevCapabilityStatus.Available, "From the IServiceCollection Bit.BevTools.Loader kept at startup: BevTools itself was attached after the container was built. Factory and instance registrations show no dependencies.")
                    : new("DI", "Registrations, lifetimes, constructor dependencies", BevCapabilityStatus.NotAvailable, "BevTools was attached at runtime, after the container was built, so the service collection was not captured. Bit.BevTools.Loader keeps it from startup."),
            new("DI", "Runtime resolutions and consumers", BevCapabilityStatus.NotAvailable, "The container does not expose resolution events; consumers are inferred from constructors and [Inject] properties."),
            new("Browser", "Online/offline, visibility, storage keys, memory, timing", bridge ? BevCapabilityStatus.Available : BevCapabilityStatus.RequiresInstrumentation, "Reported by the browser bridge. Storage is read only on request, and values whose key looks sensitive or whose content looks like a token are redacted in the browser, so BevTools never receives them."),
            new("Browser", "Component ↔ page (pick, outline, flash renders)", !session.Options.EnableBrowserBridge ? BevCapabilityStatus.NotAvailable : BlazorReflection.CanReadRenderTree ? BevCapabilityStatus.RequiresInstrumentation : BevCapabilityStatus.NotAvailable, !session.Options.EnableBrowserBridge
                ? "BitBevToolsOptions.EnableBrowserBridge is off, and the page hub follows the components into the page."
                : BlazorReflection.CanReadRenderTree
                    ? "A component's place in the page is computed from its ancestors' render trees and followed through the logical element tree Blazor's browser renderer keeps, checking every step; a render not yet applied, or markup changed by script, is reported rather than guessed. Static (SSR) content and nodes added by script belong to no component."
                    : "The renderer's render tree could not be accessed on this framework version."),
            new("Browser", "Accessibility of the page", bridge ? BevCapabilityStatus.Available : BevCapabilityStatus.RequiresInstrumentation, "On request (Browser → Check the page): names of controls and fields, image alternatives, focusable content hidden from screen readers, tab order, broken id references, empty headings and the page's language, each with the component that rendered it. Contrast, focus order and whether a name makes sense need a person."),
            new("Forms", "Forms and validation", BevCapabilityStatus.RequiresInstrumentation, "Each EditForm's EditContext: fields, modified state and changes, what validates the form and what shows its messages, each validation's outcome. Field values are never read."),
            new("Memory", "Disposed components still alive", BevCapabilityStatus.RequiresInstrumentation, "Disposed components are remembered weakly and counted as retained after two full garbage collections, with what holds each one when it is found: a service's event handler or lambda, a DotNetObjectReference, a timer. A static or a cache is not found, and the finding says so."),
            new("Routing", "Navigation guards", BevCapabilityStatus.RequiresInstrumentation, "NavigationLock components and the LocationChanging handlers the NavigationManager holds, each with the component that registered it. Read only: a handler of BevTools' own would make every navigation wait for .NET."),
            new("Errors", "Groups of the same failure", BevCapabilityStatus.Available, "Errors whose messages differ only in what identifies an instance (ids, GUIDs, timestamps, quoted values) are one group when they share their source, type and where they were thrown."),
            new("Rendering", "Comparison of two recordings", BevCapabilityStatus.Available, "Profiler → Compare… with a saved export, per component type: renders and render time, regressed, improved, new or gone. bevtools compare does the same in CI."),
            new("BevTools", "Overhead measurement", BevCapabilityStatus.Available, "Instrumentation time, BevTools UI render time, inspection and diagnostics time are tracked separately from application numbers."),
        ];
    }

    /// <summary>A runtime that cannot take calls yet (prerendering, a circuit before the browser connected) is left unwrapped, so libraries still recognize it by its type.</summary>
    private static string JSRuntimeReadinessGap() => BlazorReflection.JSRuntimeReadinessGaps is { Count: > 0 } gaps
        ? " Whether " + string.Join(" or ", gaps) + " can take calls yet could not be read on this framework version, so it is wrapped even before it can (while prerendering, or before the circuit connected), and a library that tells those states apart by the runtime's type cannot then."
        : "";

    private static string AttachedComponents(BevRuntimeAttachment attachment)
    {
        var existing = attachment.ExistingComponentsRead switch
        {
            true => "the components that already existed were read from the renderer",
            false => "the components that already existed could not be read from the renderer",
            null => "the components that already exist are read from the renderer once a BevTools panel renders",
        };
        var later = attachment.ActivatorAttached switch
        {
            true when attachment.ActivatorHooked => "the ones created after go through the component activator hook Bit.BevTools.Loader installed at startup",
            true => "its IComponentActivator was replaced through reflection to see the ones created after",
            false => "its IComponentActivator could not be replaced, so the ones created after are not seen",
            null => "the ones created after are seen from then on",
        };

        return "BevTools was attached to the running app: " + existing + ", and " + later + ". What happened before the attach was not recorded.";
    }

    private static string AttachedHttp(BevRuntimeAttachment attachment)
    {
        var client = attachment.HttpClient switch
        {
            BevHttpClientAttachment.InPipeline => "The pipeline of the HttpClient registered in DI holds a BevTools handler, so its requests are recorded.",
            BevHttpClientAttachment.Wrapped => "The handler of the HttpClient registered in DI was wrapped through reflection, so its requests are recorded.",
            BevHttpClientAttachment.Transient => "The HttpClient registered in DI is transient, so wrapping one instance would miss the others: add Bit.BevTools.Loader's BevToolsHttpHookHandler to its pipeline.",
            _ => "No HttpClient registered in DI could be reached.",
        };
        var factory = attachment.FactoryClientsHooked
            ? " IHttpClientFactory clients go through the handlers Bit.BevTools.Loader added at startup."
            : " IHttpClientFactory clients are not seen: their pipelines were built without BevTools.";

        return "Attached at runtime. " + client + factory + " Other hand-built clients need BevToolsHttpHookHandler (or BevToolsHttpMessageHandler) in their pipeline.";
    }
}
