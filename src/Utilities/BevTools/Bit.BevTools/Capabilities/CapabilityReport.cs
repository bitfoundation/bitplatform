
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
                : "No HandleEvent activities observed yet. BevTools was attached at runtime, so the framework ActivitySource exists only if the app registered it at startup (AddComponentsTracing); until an event is seen, renders inside events are labeled StateHasChanged.";

        return
        [
            attachment is null
                ? new("Components", "Component instances", BevCapabilityStatus.Available, "Every component is created through the public IComponentActivator hook, so all instances are known without touching application code.")
                : new("Components", "Component instances", attachment is { ActivatorWrapped: true, ExistingComponentsRead: true } ? BevCapabilityStatus.Available : BevCapabilityStatus.Partial,
                    "BitBevTools.Attach attached BevTools to the running app: "
                    + (attachment.ExistingComponentsRead == true ? "the components that already existed were read from the renderer, and " : "the components that already existed could not be read from the renderer, and ")
                    + (attachment.ActivatorWrapped == true ? "its IComponentActivator was wrapped to see the ones created after." : "its IComponentActivator could not be wrapped, so the ones created after are not seen.")
                    + " What happened before the attach was not recorded."),
            new("Components", "Parent/child hierarchy", hierarchyOk ? BevCapabilityStatus.Available : BevCapabilityStatus.NotAvailable, hierarchyOk ? "Resolved from the renderer's ComponentState (public since .NET 8) through a cached reflection accessor." : "The renderer's component state map could not be accessed on this framework version."),
            new("Components", "Parameters and cascading values", BevCapabilityStatus.Available, "Read from [Parameter]/[CascadingParameter] properties on demand; values are inspected lazily and sensitive names are redacted."),
            new("Components", "Component state (fields)", BevCapabilityStatus.Partial, "Fields are read on demand and diffed per render for the selected component only. Changes made without a render are not observed."),
            new("Components", "Injected services", BevCapabilityStatus.Available, "[Inject] properties are listed with their registered lifetime."),
            new("Components", "Source location", BevCapabilityStatus.Partial, "Inferred from the type name (Foo.razor); exact file paths need PDBs and are only shown in exception stack traces."),
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
            new("State", "Cascading values", BevCapabilityStatus.Available, "Read from [CascadingParameter] properties of the selected component."),
            attachment is null
                ? new("Network", "HTTP requests via IHttpClientFactory", BevCapabilityStatus.Available, "A DelegatingHandler is added to every factory pipeline through IHttpMessageHandlerBuilderFilter. Hand-built HttpClient instances need BevToolsHttpMessageHandler added manually.")
                : new("Network", "HTTP requests", attachment.HttpClientWrapped ? BevCapabilityStatus.Partial : BevCapabilityStatus.RequiresIntegration, attachment.HttpClientWrapped
                    ? "Attached at runtime: the HttpClient registered in DI was wrapped, so its requests are recorded. IHttpClientFactory clients and other hand-built clients need BevToolsHttpMessageHandler added manually."
                    : "Attached at runtime, and no HttpClient registered in DI could be wrapped: add BevToolsHttpMessageHandler to the clients whose requests should be recorded."),
            new("Network", "Request/response bodies", BevCapabilityStatus.NotAvailable, "Never captured: bodies routinely contain credentials and personal data."),
            new("Network", "Originating component", BevCapabilityStatus.Partial, "Requests are linked to the UI event that issued them when one is executing; requests from lifecycle methods show no trigger."),
            new("JS interop", ".NET → JS calls from components", BevCapabilityStatus.RequiresInstrumentation, "IJSRuntime injected into tracked components is wrapped on the first render, the earliest point after the framework injects it: calls made from OnInitialized/OnInitializedAsync happen before that and are not attributed. Module references obtained through a wrapped runtime are wrapped too."),
            new("JS interop", ".NET → JS calls from services", BevCapabilityStatus.RequiresIntegration, "Services must call jsRuntime.WithBevToolsTracking(session). Decorating IJSRuntime globally is unsafe because the framework casts it to its concrete type."),
            new("JS interop", "JS → .NET calls", bridge ? BevCapabilityStatus.Available : BevCapabilityStatus.RequiresInstrumentation, "DotNet.invokeMethodAsync/invokeMethod are wrapped by the browser bridge and reported in batches."),
            new("Errors", "Render and event exceptions", BevCapabilityStatus.Available, "Render exceptions are caught in the wrapper; event exceptions and circuit failures arrive through Blazor's own logger categories."),
            new("Errors", "JS errors and unhandled promise rejections", bridge ? BevCapabilityStatus.Available : BevCapabilityStatus.RequiresInstrumentation, "window.onerror / unhandledrejection via the browser bridge."),
            new("Circuit", "Circuit lifecycle, connection state, inbound message count", isServer && attachment is null ? BevCapabilityStatus.RequiresInstrumentation : BevCapabilityStatus.NotAvailable, "Blazor Server only, through a CircuitHandler registered by AddBitBevToolsServerServices().") { Platforms = "Server" },
            new("Circuit", "Reconnect attempts", bridge && isServer ? BevCapabilityStatus.Available : BevCapabilityStatus.RequiresInstrumentation, "From the components:reconnect-state-changed browser event.") { Platforms = "Server" },
            new("Circuit", "SignalR message sizes and latency", BevCapabilityStatus.NotAvailable, "Not exposed by the framework; use SignalR logging or browser DevTools WS frames.") { Platforms = "Server" },
            attachment is null
                ? new("DI", "Registrations, lifetimes, constructor dependencies", BevCapabilityStatus.Available, "From the IServiceCollection captured at AddBitBevToolsServices time. Factory and instance registrations show no dependencies.")
                : new("DI", "Registrations, lifetimes, constructor dependencies", BevCapabilityStatus.NotAvailable, "BevTools was attached at runtime, after the container was built, so the service collection was not captured."),
            new("DI", "Runtime resolutions and consumers", BevCapabilityStatus.NotAvailable, "The container does not expose resolution events; consumers are inferred from constructors and [Inject] properties."),
            new("Browser", "Online/offline, visibility, storage keys, memory, timing", bridge ? BevCapabilityStatus.Available : BevCapabilityStatus.RequiresInstrumentation, "Reported by the browser bridge. Storage is read only on request, and values whose key looks sensitive or whose content looks like a token are redacted in the browser, so BevTools never receives them."),
            new("Browser", "Component ↔ DOM element mapping", BevCapabilityStatus.NotAvailable, "Blazor does not expose a component-to-DOM map to .NET code; highlighting elements is not implemented rather than approximated."),
            new("BevTools", "Overhead measurement", BevCapabilityStatus.Available, "Instrumentation time, BevTools UI render time, inspection and diagnostics time are tracked separately from application numbers."),
        ];
    }
}
