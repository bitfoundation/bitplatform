// A Renderer is the framework's own extension point for running components outside a browser -
// bUnit and the static HTML renderer are built on it - and it lives in the RenderTree namespace the
// analyzer guards against everyday use. Hosting the real BdndProvider, BdndDraggable and
// BdndDropTarget is the whole point here.
#pragma warning disable BL0006

using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Bit.Bdnd.Demo.Server.Services;

/// <summary>
/// Runs one drag through the real components, in-process and with no browser behind them: a
/// <see cref="BdndProvider"/> holding a <see cref="BdndDraggable{TItem}"/> and a tree of
/// <see cref="BdndDropTarget{TItem}"/>s, rendered by a renderer of its own.
/// <para>
/// Every decision that matters lives inside those components and their manager, and most of it is
/// private - whether a target accepts the item, which hovered target is the shallow one, which
/// <c>OnDrop</c> runs and in what order, which <c>Result</c> survives. Driving the components is the
/// only way to answer "what happens when I drop here?" without writing a second copy of those rules,
/// and a second copy is exactly what drifts: it would keep describing a drag the shipped library has
/// since learnt to handle differently.
/// </para>
/// <para>
/// The browser half is a stand-in for <c>bit-bdnd.js</c>, and it does only what the script does:
/// it hands back a registration for each component and keeps the options the component pushes to it,
/// tags the drag's <c>DataTransfer</c> with the draggable's own type (only when the draggable can
/// drag), calls <c>HandleHoverChanged</c> on every target under the pointer, innermost first, as
/// <c>dragenter</c> bubbles, lets the browser fire <c>drop</c> only when some hovered target accepted the
/// <c>dragover</c> - the script's <c>applyDropEffect</c> - and reports the end of the drag through
/// <c>HandleDragEnd</c>. The managed events, <c>ondragstart</c> and <c>ondrop</c>, are dispatched through
/// the renderer itself, to the handlers the components rendered, innermost first as Blazor's own event
/// delegation bubbles them. The registration is not an in-process one, so the components take the code
/// path they take on Blazor Server and in a BlazorWebView; nothing a scenario can express differs on
/// WebAssembly.
/// </para>
/// </summary>
internal static class BdndDropHost
{
    /// <summary>
    /// The prefix of the <c>DataTransfer</c> type a draggable's registration tags its drags with - the
    /// draggable's handler id follows it. The same constant as the library's (internal) one.
    /// </summary>
    public const string SourceTypePrefix = "application/x-bit-bdnd-";

    /// <summary>The attribute each rendered component carries, so its element and its registration can be told apart.</summary>
    private const string LabAttribute = "data-bdnd-lab";

    private const string DraggableKey = "draggable";

    /// <summary>A scenario runs in milliseconds; this is only there so a component that never settles cannot hold a request forever.</summary>
    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(15);

    /// <summary>One drop target of the scenario, as it is rendered.</summary>
    public sealed class Target
    {
        /// <summary>The target's position in document order, which is what keys it - ids may repeat.</summary>
        public required int Index { get; init; }

        public required string Id { get; init; }

        public Target? Parent { get; init; }

        public required int Depth { get; init; }

        /// <summary>The item types handed to <c>AcceptTypes</c>, native names already resolved.</summary>
        public required string[] Accept { get; init; }

        /// <summary>False to render the target with a <c>CanDropPredicate</c> that answers false.</summary>
        public bool CanDrop { get; init; } = true;

        /// <summary>What its <c>OnDrop</c> sets as <c>args.Result</c>; null sets nothing.</summary>
        public string? Result { get; init; }

        public List<Target> Children { get; } = [];

        public string Key => $"t{Index}";
    }

    /// <summary>
    /// What is dragged: an in-app item of <paramref name="ItemType"/> started by a draggable, or -
    /// with a <paramref name="NativeToken"/> - a drag from outside the app carrying that DataTransfer type.
    /// </summary>
    public sealed record Drag(string ItemType, string? NativeToken, bool CanDrag, string DropEffect);

    /// <summary>One target's collected state while the item hovered, and what its callbacks saw.</summary>
    public sealed record TargetOutcome(bool CanDrop, bool IsOver, bool IsOverShallow);

    /// <summary>One <c>OnDrop</c> (or <c>OnNativeDrop</c>) that ran.</summary>
    public sealed record DropCall(Target Target, bool InnerAlreadyDropped, string? Result, string? NativeItemType);

    /// <summary>Everything one drag reported.</summary>
    public sealed record Outcome(
        bool DragStarted,
        bool BeginFired,
        string? EffectAllowed,
        string[] DataTransferTypes,
        bool DropFired,
        IReadOnlyList<DropCall> Drops,
        BdndDragEndContext<object>? End,
        IReadOnlyDictionary<int, TargetOutcome> Targets,
        IReadOnlyList<Exception> Exceptions);

    /// <summary>
    /// Renders the scenario and plays the drag: start it, hover the <paramref name="hoverChain"/>
    /// (outermost first), drop, end it.
    /// </summary>
    public static async Task<Outcome> RunAsync(Drag drag, IReadOnlyList<Target> roots, IReadOnlyList<Target> hoverChain)
    {
        var recorder = new Recorder();
        var js = new LabJSRuntime();

        await using var renderer = new LabRenderer(new ServiceProviderStub(js, new BdndInterop(js)));

        Outcome? outcome = null;

        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            await renderer.RenderAsync(Content(drag, roots, recorder));

            // dragenter fires on the element under the pointer first and bubbles out from there.
            var innermostFirst = hoverChain.Reverse().ToArray();

            outcome = drag.NativeToken is { } token
                ? await PlayNativeAsync(token, innermostFirst, js, recorder, renderer)
                : await PlayInAppAsync(innermostFirst, js, recorder, renderer);
        }).WaitAsync(_timeout);

        return outcome!;
    }

    private static async Task<Outcome> PlayInAppAsync(Target[] innermostFirst, LabJSRuntime js, Recorder recorder, LabRenderer renderer)
    {
        var options = js.DraggableRegistration?.Options;
        var handlerId = Option<string>(options, "Id");
        var effectAllowed = Option<string>(options, "EffectAllowed");

        // The script only tags a drag - and so only lets the draggable recognise it as its own - when
        // the registration says it can drag; a draggable that cannot renders draggable="false" anyway.
        string[] types = Option<bool>(options, "CanDrag") && handlerId is not null ? [SourceTypePrefix + handlerId] : [];

        if (renderer.FindEventHandler(DraggableKey, "ondragstart") is { } dragStart)
        {
            await renderer.DispatchAsync(dragStart, DragEvent("dragstart", types, effectAllowed));
        }

        await Task.Yield();

        if (recorder.DragState?.IsDragging is not true)
        {
            return new Outcome(false, recorder.BeginFired, effectAllowed, types, false, [], null, Snapshot(recorder), [.. renderer.Exceptions]);
        }

        foreach (var target in innermostFirst)
        {
            if (js.TargetOf(target) is { } registered) await registered.Component.HandleHoverChanged(true, types);
        }

        await Task.Yield();

        var hovering = Snapshot(recorder);

        // The browser fires drop only if some target under the pointer accepted the dragover - what the
        // script decides from the CanDrop the target last pushed to it - and never under effectAllowed
        // "none", which turns every drop operation off.
        var dropFired = effectAllowed != "none" && innermostFirst.Any(target => Option<bool>(js.TargetOf(target)?.Registration.Options, "CanDrop"));

        if (dropFired)
        {
            foreach (var target in innermostFirst)
            {
                if (renderer.FindEventHandler(target.Key, "ondrop") is { } drop)
                {
                    await renderer.DispatchAsync(drop, DragEvent("drop", types, effectAllowed));
                }
            }
        }

        // dragend reaches the source whether or not anything was dropped.
        if (js.Draggable is { } draggable) await draggable.HandleDragEnd();

        await Task.Yield();

        return new Outcome(true, recorder.BeginFired, effectAllowed, types, dropFired, [.. recorder.Drops], recorder.End, hovering, [.. renderer.Exceptions]);
    }

    private static async Task<Outcome> PlayNativeAsync(string token, Target[] innermostFirst, LabJSRuntime js, Recorder recorder, LabRenderer renderer)
    {
        string[] types = [token];

        foreach (var target in innermostFirst)
        {
            if (js.TargetOf(target) is { } registered) await registered.Component.HandleHoverChanged(true, types);
        }

        await Task.Yield();

        var hovering = Snapshot(recorder);

        // The script accepts a native dragover on a target whose registration lists the token, and
        // hands the drop to every such target the drop event bubbles through - it does not stop at the
        // first one.
        var accepting = innermostFirst.Where(target => Option<string[]>(js.TargetOf(target)?.Registration.Options, "NativeTypes")?.Contains(token) is true).ToArray();

        foreach (var target in accepting)
        {
            if (js.TargetOf(target) is { } registered) await registered.Component.HandleNativeDrop(NativePayload(token));
        }

        await Task.Yield();

        return new Outcome(false, false, null, types, accepting.Length > 0, [.. recorder.Drops], null, hovering, [.. renderer.Exceptions]);
    }

    /// <summary>What the script reads out of a native drop, for each kind of native drag.</summary>
    private static BdndNativeDropPayload NativePayload(string token) => token switch
    {
        "Files" => new() { Types = [token], Files = [new() { Name = "report.pdf", Size = 48_213, Type = "application/pdf" }] },
        "text/uri-list" => new() { Types = [token], Data = new() { [token] = "https://bitplatform.dev" } },
        "text/html" => new() { Types = [token], Data = new() { [token] = "<b>bit</b>" } },
        _ => new() { Types = [token], Data = new() { [token] = "bit" } },
    };

    private static DragEventArgs DragEvent(string type, string[] types, string? effectAllowed) => new()
    {
        Type = type,
        ClientX = 120,
        ClientY = 80,
        OffsetX = 20,
        OffsetY = 10,
        DataTransfer = new DataTransfer { Types = types, EffectAllowed = effectAllowed, DropEffect = "none", Files = [], Items = [] }
    };

    private static Dictionary<int, TargetOutcome> Snapshot(Recorder recorder)
        => recorder.States.ToDictionary(state => state.Key, state => new TargetOutcome(state.Value.CanDrop, state.Value.IsOver, state.Value.IsOverShallow));

    /// <summary>Reads one member of the options a component pushed to its registration - internal records of the library.</summary>
    private static T? Option<T>(object? options, string name)
        => options?.GetType().GetProperty(name)?.GetValue(options) is T value ? value : default;

    /// <summary>The provider, the draggable (unless the drag comes from outside the app) and the targets, as markup would declare them.</summary>
    private static RenderFragment Content(Drag drag, IReadOnlyList<Target> roots, Recorder recorder) => builder =>
    {
        if (drag.NativeToken is null)
        {
            builder.OpenComponent<BdndDraggable<object>>(0);
            builder.AddComponentParameter(1, nameof(BdndDraggable<object>.Type), drag.ItemType);
            builder.AddComponentParameter(2, nameof(BdndDraggable<object>.Item), drag.ItemType);
            builder.AddComponentParameter(3, nameof(BdndDraggable<object>.CanDrag), drag.CanDrag);
            builder.AddComponentParameter(4, nameof(BdndDraggable<object>.DropEffect), drag.DropEffect);
            builder.AddComponentParameter(5, nameof(BdndDraggable<object>.OnBegin), EventCallback.Factory.Create<object?>(recorder, _ => { recorder.BeginFired = true; }));
            builder.AddComponentParameter(6, nameof(BdndDraggable<object>.OnEnd), EventCallback.Factory.Create<BdndDragEndContext<object>>(recorder, context => { recorder.End = context; }));
            builder.AddComponentParameter(7, LabAttribute, DraggableKey);
            builder.AddComponentParameter(8, nameof(BdndDraggable<object>.ChildContent), (RenderFragment<BdndDragState>)(state => _ => { recorder.DragState = state; }));
            builder.CloseComponent();
        }

        foreach (var target in roots) RenderTarget(builder, target, recorder);
    };

    private static void RenderTarget(RenderTreeBuilder builder, Target target, Recorder recorder)
    {
        // A region per target, keyed by the target: the scenario is a loop over data, as a page's markup
        // would be, so the sequence numbers inside stay the literals the analyzer wants.
        builder.OpenRegion(0);
        builder.OpenComponent<BdndDropTarget<object>>(0);
        builder.SetKey(target.Index);
        builder.AddComponentParameter(1, nameof(BdndDropTarget<object>.AcceptTypes), target.Accept);

        if (target.CanDrop is false)
        {
            builder.AddComponentParameter(2, nameof(BdndDropTarget<object>.CanDropPredicate), (Func<object?, IBdndMonitor, bool>)((_, _) => false));
        }

        builder.AddComponentParameter(3, nameof(BdndDropTarget<object>.OnDrop), EventCallback.Factory.Create<BdndDropEventArgs<object>>(recorder, args =>
        {
            recorder.Drops.Add(new DropCall(target, args.Monitor.DidDrop(), target.Result, null));

            if (target.Result is not null) args.Result = target.Result;
        }));
        builder.AddComponentParameter(4, nameof(BdndDropTarget<object>.OnNativeDrop), EventCallback.Factory.Create<BdndNativeDropEventArgs>(recorder, args =>
        {
            recorder.Drops.Add(new DropCall(target, false, null, args.ItemType));
        }));
        builder.AddComponentParameter(5, LabAttribute, target.Key);
        builder.AddComponentParameter(6, nameof(BdndDropTarget<object>.ChildContent), (RenderFragment<BdndDropState<object>>)(state => content =>
        {
            // The state a page would render from - kept per target, the latest render winning.
            recorder.States[target.Index] = state;

            foreach (var child in target.Children) RenderTarget(content, child, recorder);
        }));
        builder.CloseComponent();
        builder.CloseRegion();
    }

    /// <summary>What the components handed their content and their callbacks while the drag ran.</summary>
    private sealed class Recorder
    {
        public BdndDragState? DragState { get; set; }

        public Dictionary<int, BdndDropState<object>> States { get; } = [];

        public bool BeginFired { get; set; }

        public BdndDragEndContext<object>? End { get; set; }

        public List<DropCall> Drops { get; } = [];
    }

    /// <summary>
    /// The renderer: a render batch goes nowhere, and an exception is kept for the report instead
    /// of tearing the request down - a component that throws on some scenario is a finding, not a crash.
    /// </summary>
    private sealed class LabRenderer(IServiceProvider services) : Renderer(services, NullLoggerFactory.Instance)
    {
        private readonly List<Exception> _exceptions = [];
        private int _rootId;

        public IReadOnlyList<Exception> Exceptions => _exceptions;

        public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

        // Interactive, because that is the only way the components are ever meant to run: it is what
        // a component asking ComponentBase.RendererInfo would be told in the browser.
        protected override RendererInfo RendererInfo { get; } = new("BdndDropLab", isInteractive: true);

        public async Task RenderAsync(RenderFragment content)
        {
            var provider = InstantiateComponent(typeof(BdndProvider));
            _rootId = AssignRootComponentId(provider);

            await RenderRootComponentAsync(_rootId, ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(BdndProvider.ChildContent)] = content
            }));
        }

        public Task DispatchAsync(ulong eventHandlerId, EventArgs eventArgs) => DispatchEventAsync(eventHandlerId, null, eventArgs);

        /// <summary>
        /// The id the renderer gave the handler of <paramref name="eventName"/> on the element of the
        /// component marked <paramref name="key"/>. Read afresh before every dispatch: a re-render may
        /// hand an element's handler a new id, and the old one then names nothing.
        /// </summary>
        public ulong? FindEventHandler(string key, string eventName) => Find(_rootId, key, eventName);

        private ulong? Find(int componentId, string key, string eventName)
        {
            var frames = GetCurrentRenderTreeFrames(componentId);

            for (var i = 0; i < frames.Count; i++)
            {
                var frame = frames.Array[i];

                // A child component's output is a render tree of its own; the attribute frames that
                // follow the component frame here are only the parameters it was given.
                if (frame.FrameType == RenderTreeFrameType.Component)
                {
                    if (Find(frame.ComponentId, key, eventName) is { } nested) return nested;
                    continue;
                }

                if (frame.FrameType != RenderTreeFrameType.Element) continue;

                string? marked = null;
                ulong handler = 0;

                // An element's attributes are the frames right after it.
                for (var j = i + 1; j < frames.Count && frames.Array[j].FrameType == RenderTreeFrameType.Attribute; j++)
                {
                    var attribute = frames.Array[j];

                    if (attribute.AttributeName == LabAttribute) marked = attribute.AttributeValue as string;
                    else if (attribute.AttributeName == eventName && attribute.AttributeEventHandlerId != 0) handler = attribute.AttributeEventHandlerId;
                }

                if (marked == key && handler != 0) return handler;
            }

            return null;
        }

        protected override void HandleException(Exception exception) => _exceptions.Add(exception);

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch) => Task.CompletedTask;
    }

    /// <summary>The components inject one service, BdndInterop, which needs one more, the JS runtime; nothing else is resolved.</summary>
    private sealed class ServiceProviderStub(IJSRuntime js, BdndInterop interop) : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(BdndInterop)) return interop;
            if (serviceType == typeof(IJSRuntime)) return js;

            return null;
        }
    }

    /// <summary>
    /// Stands in for <c>bit-bdnd.js</c>. The module is imported once and every registration goes
    /// through it; each one is answered with a <see cref="Registration"/> that keeps the options the
    /// component pushes, and the component behind it - reached the way the script reaches it, through
    /// the DotNetObjectReference it was handed - is kept by the attribute it was rendered with.
    /// </summary>
    private sealed class LabJSRuntime : IJSRuntime, IJSObjectReference
    {
        private readonly Dictionary<string, (BdndDropTarget<object> Component, Registration Registration)> _targets = new(StringComparer.Ordinal);

        public BdndDraggable<object>? Draggable { get; private set; }

        public Registration? DraggableRegistration { get; private set; }

        public (BdndDropTarget<object> Component, Registration Registration)? TargetOf(Target target)
            => _targets.TryGetValue(target.Key, out var registered) ? registered : null;

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            object? answer = identifier switch
            {
                // The module is this same object: one stand-in, one table of answers.
                "import" => this,
                "registerDragSource" => RegisterDragSource(args),
                "registerDropTarget" => RegisterDropTarget(args),
                _ => null
            };

            return ValueTask.FromResult(answer is TValue value ? value : default!);
        }

        private Registration RegisterDragSource(object?[]? args)
        {
            var registration = new Registration(args?.ElementAtOrDefault(2));

            Draggable = (args?.ElementAtOrDefault(1) as DotNetObjectReference<BdndDraggable<object>>)?.Value;
            DraggableRegistration = registration;

            return registration;
        }

        private Registration RegisterDropTarget(object?[]? args)
        {
            var registration = new Registration(args?.ElementAtOrDefault(2));

            if (args?.ElementAtOrDefault(1) is DotNetObjectReference<BdndDropTarget<object>> { Value: var component } &&
                component.AdditionalAttributes?.GetValueOrDefault(LabAttribute) is string key)
            {
                // A target registered again - its element re-created - replaces its old registration.
                _targets[key] = (component, registration);
            }

            return registration;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    /// <summary>
    /// One registration of the script: <c>update</c> replaces the options it acts on, and everything
    /// else - <c>getDragStartRect</c> included, which the draggable asks for once the drag is published -
    /// answers with nothing, the way the script answers for an element it never measured.
    /// </summary>
    private sealed class Registration(object? options) : IJSObjectReference
    {
        public object? Options { get; private set; } = options;

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            if (identifier == "update") Options = args?.ElementAtOrDefault(0);

            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
