using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Bit.Bdnd.Tests.Components;

/// <summary>
/// Renders the components against bUnit's JS interop with <c>bit-bdnd.js</c> and the registrations it hands out set
/// up, so every call a component makes to the script can be inspected, and drives drags the way the script and the
/// browser do: a tagged <c>dragstart</c>, then the <c>[JSInvokable]</c> callbacks and the managed drop events.
/// </summary>
public abstract class BdndComponentTestBase : BunitContext
{
    protected const string ModulePath = "./_content/Bit.Bdnd/bit-bdnd.js";

    protected BdndComponentTestBase()
    {
        Services.AddBitBdndServices();
        // loose: getDragStartRect answers null (the Blazor Server path) unless a test plans it
        JSInterop.Mode = JSRuntimeMode.Loose;
        Module = JSInterop.SetupModule(ModulePath);
        Sources = Module.SetupModule("registerDragSource", _ => true);
        Targets = Module.SetupModule("registerDropTarget", _ => true);
    }

    /// <summary>The imported <c>bit-bdnd.js</c> module.</summary>
    protected BunitJSModuleInterop Module { get; }

    /// <summary>The objects <c>registerDragSource</c> returns: <c>update</c>, <c>dispose</c>, <c>getDragStartRect</c> and <c>cancelDrag</c> land here.</summary>
    protected BunitJSModuleInterop Sources { get; }

    /// <summary>The objects <c>registerDropTarget</c> returns: <c>update</c> and <c>dispose</c> land here.</summary>
    protected BunitJSModuleInterop Targets { get; }

    /// <summary>The manager handed to the providers <see cref="RenderProvider"/> renders.</summary>
    protected BdndDragDropManager Manager { get; } = new();

    protected IRenderedComponent<BdndProvider> RenderProvider(Action<ComponentParameterCollectionBuilder<BdndProvider>> content)
        => Render<BdndProvider>(p =>
        {
            p.Add(x => x.Manager, Manager);
            content(p);
        });

    /// <summary>A component as a fragment, for content rendered conditionally or deep inside other components.</summary>
    protected static RenderFragment Fragment<TComponent>(params (string Name, object? Value)[] parameters)
        where TComponent : IComponent
        => builder =>
        {
            builder.OpenComponent<TComponent>(0);
            foreach (var (name, value) in parameters) builder.AddComponentParameter(1, name, value);
            builder.CloseComponent();
        };

    // ---- what reached the script ----

    private protected IReadOnlyList<BdndDragSourceOptions> SourceRegistrations =>
        [.. Module.Invocations["registerDragSource"].Select(i => (BdndDragSourceOptions)i.Arguments[2]!)];

    private protected IReadOnlyList<BdndDropTargetOptions> TargetRegistrations =>
        [.. Module.Invocations["registerDropTarget"].Select(i => (BdndDropTargetOptions)i.Arguments[2]!)];

    private protected IReadOnlyList<BdndDragSourceOptions> SourceUpdates =>
        [.. Sources.Invocations["update"].Select(i => (BdndDragSourceOptions)i.Arguments[0]!)];

    private protected IReadOnlyList<BdndDropTargetOptions> TargetUpdates =>
        [.. Targets.Invocations["update"].Select(i => (BdndDropTargetOptions)i.Arguments[0]!)];

    /// <summary>The <c>registerDragSource</c> call of <paramref name="draggable"/>.</summary>
    protected JSRuntimeInvocation SourceRegistrationCall<TItem>(BdndDraggable<TItem> draggable)
        => Module.Invocations["registerDragSource"].Last(i => Wraps(i.Arguments[1], draggable));

    /// <summary>The <c>registerDropTarget</c> call of <paramref name="target"/>.</summary>
    protected JSRuntimeInvocation TargetRegistrationCall<TItem>(BdndDropTarget<TItem> target)
        => Module.Invocations["registerDropTarget"].Last(i => Wraps(i.Arguments[1], target));

    /// <summary>The handler id <paramref name="draggable"/> registered with the script, which tags its drags.</summary>
    protected string SourceIdOf<TItem>(BdndDraggable<TItem> draggable)
        => ((BdndDragSourceOptions)SourceRegistrationCall(draggable).Arguments[2]!).Id;

    private static bool Wraps<TComponent>(object? dotNetRef, TComponent component) where TComponent : class
    {
        try
        {
            return dotNetRef is DotNetObjectReference<TComponent> reference && ReferenceEquals(reference.Value, component);
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
    }

    // ---- driving a drag ----

    /// <summary>The DataTransfer type the script tags the drags of a source with.</summary>
    protected static string SourceType(string sourceId) => "application/x-bit-bdnd-" + sourceId;

    /// <summary>
    /// The component's host element, in the component's own DOM: events dispatched on it do not bubble to elements
    /// of enclosing components. Find nested elements in the root component's DOM to let them bubble.
    /// </summary>
    protected static IElement HostOf(IRenderedComponent<IComponent> component)
        => component.Nodes.OfType<IElement>().First();

    protected static DragEventArgs PointerAt(double clientX, double clientY, params string[] types) => new()
    {
        ClientX = clientX,
        ClientY = clientY,
        DataTransfer = new DataTransfer { Types = types },
    };

    /// <summary>
    /// Starts a drag the way the browser does with the script loaded: the script tags the <c>dragstart</c> with the
    /// source's type, then Blazor dispatches it to the host element.
    /// </summary>
    protected Task StartDragAsync<TItem>(IRenderedComponent<BdndDraggable<TItem>> draggable,
        double clientX = 0, double clientY = 0, double offsetX = 0, double offsetY = 0)
    {
        var args = PointerAt(clientX, clientY, SourceType(SourceIdOf(draggable.Instance)));
        args.OffsetX = offsetX;
        args.OffsetY = offsetY;
        return HostOf(draggable).DragStartAsync(args);
    }

    /// <summary>The script reports the pointer entering (or leaving) a drop target.</summary>
    protected static Task HoverAsync<TItem>(IRenderedComponent<BdndDropTarget<TItem>> target, bool isOver = true, params string[] types)
        => target.InvokeAsync(() => target.Instance.HandleHoverChanged(isOver, types.Length > 0 ? types : [SourceType("in-app")]));

    /// <summary>The browser raises <c>drop</c> on the target's host element.</summary>
    protected static Task DropAsync<TItem>(IRenderedComponent<BdndDropTarget<TItem>> target, double clientX = 0, double clientY = 0)
        => DropAsync(HostOf(target), clientX, clientY);

    /// <summary>The browser raises <c>drop</c> on <paramref name="element"/>; it bubbles through the enclosing targets.</summary>
    protected static Task DropAsync(IElement element, double clientX = 0, double clientY = 0)
        => element.DropAsync(PointerAt(clientX, clientY, SourceType("in-app")));

    /// <summary>The script reports the end of the drag (the source's <c>dragend</c>).</summary>
    protected static Task EndDragAsync<TItem>(IRenderedComponent<BdndDraggable<TItem>> draggable)
        => draggable.InvokeAsync(() => draggable.Instance.HandleDragEnd());
}

/// <summary>Records the collected state a component hands its child content on every render.</summary>
internal sealed class StateLog<TState>
{
    private readonly List<TState> _states = [];

    public TState Last => _states[^1];

    public int Count => _states.Count;

    public RenderFragment<TState> Content(Func<TState, RenderFragment?>? inner = null) => state =>
    {
        _states.Add(state);
        return inner?.Invoke(state) ?? (_ => { });
    };
}
