// A Renderer is the framework's own extension point for running components outside a browser -
// bUnit and the static HTML renderer are built on it - and it lives in the RenderTree namespace the
// analyzer guards against everyday use. Hosting the real Bortable is the whole point here, and so is
// reading back the frames it rendered: they are where its event handlers are found.
#pragma warning disable BL0006

using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bit.Bortable.Demo.Server.Services;

/// <summary>
/// Renders real <see cref="Bortable{TItem}"/> lists in-process, with no browser behind them, and drives
/// a drag through them the way the browser layer does: by calling each list's bridge - the object a list
/// registers with <c>bit-bortable.js</c> - when a drag starts, when the drop position changes and when the
/// drag ends.
/// <para>
/// Every decision a drag involves is private to the component: whether a list takes an item
/// (<c>CanAccept</c>, <c>CanReceiveFrom</c>), where the item goes, what is moved, cloned or swapped,
/// which events fire and in which order. Running the component is the only way to put a drag through
/// all of it without writing a second copy of each rule, and a second copy is exactly what drifts: it
/// would keep answering "accepted" for a drop the shipped list has since learnt to refuse.
/// </para>
/// <para>
/// The browser layer is a stand-in. The lists register with it after their first render, handing it their
/// bridge and their options; the lab then calls the bridge's <c>[JSInvokable]</c> methods by name, exactly as
/// <c>bit-bortable.js</c> does, with the item keys the lists rendered (<c>data-bortable-key</c>) and the drop
/// positions the caller asked for - the hit test is the one thing a list cannot be told in C#. A click of a
/// multi-drag selection is a Blazor event, dispatched to the handler id found in the render tree, which is read
/// again before each call since every render hands out new ids.
/// </para>
/// </summary>
internal sealed class BortableListHost : IAsyncDisposable
{
    /// <summary>
    /// The longest one event may take. Every await in the component completes at once against the
    /// stand-in, so a handler still running after this is stuck - on its gate, most likely - and the
    /// lab says so instead of holding the request open.
    /// </summary>
    private static readonly TimeSpan _eventTimeout = TimeSpan.FromSeconds(10);

    /// <summary>One list as the host renders it: the parameters it is given, and the items bound to it.</summary>
    public sealed class ListSetup
    {
        public required string Id { get; init; }

        /// <summary>The bound list - replaced by what ItemsChanged hands back, as <c>@bind-Items</c> does.</summary>
        public required IList<string> Items { get; set; }

        public string? Group { get; init; }

        public BortablePull Pull { get; init; }

        public bool Put { get; init; } = true;

        public bool Sort { get; init; } = true;

        public bool Disabled { get; init; }

        public bool Swap { get; init; }

        public bool MultiDrag { get; init; }

        public Func<string, bool>? Filter { get; init; }

        public Func<string, string>? Clone { get; init; }
    }

    /// <summary>One callback a list raised, with the step that raised it.</summary>
    public sealed record Callback(int Step, string Event, string List, BortableEventArgs<string>? Args, string[]? Items);

    /// <summary>An item wrapper, or the drop placeholder, as the list rendered it - with its key and its click handler id (0: none).</summary>
    public sealed record RenderedItem(string Item, string Class, bool Placeholder, bool Draggable, string? Key, ulong Click);

    /// <summary>A list as it rendered: its container's class and element reference, and its wrappers in DOM order.</summary>
    public sealed record RenderedList(string Id, string Class, string? Element, RenderedItem[] Items);

    private readonly ServiceProvider _services;
    private readonly AsyncServiceScope _scope;
    private readonly BrowserStandIn _browser = new();
    private readonly ListRenderer _renderer;
    private readonly List<Callback> _callbacks = [];

    /// <summary>
    /// Each list's element reference id. The renderer writes a capture's id into the frame only when
    /// the element is created - a later render of the same element leaves it out, as there is nothing
    /// new to capture - so the ids are taken from the first render and remembered.
    /// </summary>
    private readonly Dictionary<string, string> _elements = new(StringComparer.Ordinal);

    private int _hostId;

    private BortableListHost()
    {
        // The library's own registration, so the lists use the coordinator an app would give them -
        // one scope, like one circuit, so every list of the drag shares it.
        _services = new ServiceCollection()
            .AddBitBortableServices()
            .AddSingleton<IJSRuntime>(_browser)
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        _scope = _services.CreateAsyncScope();
        _renderer = new ListRenderer(_scope.ServiceProvider);
    }

    /// <summary>The step every callback raised from now on is filed under.</summary>
    public int Step { get; set; }

    /// <summary>Every callback the lists raised so far, in the order they raised them.</summary>
    public IReadOnlyList<Callback> Callbacks => _callbacks;

    /// <summary>What a list threw while it rendered or handled an event - a finding, not a crash.</summary>
    public IReadOnlyList<Exception> Exceptions => _renderer.Exceptions;

    /// <summary>How many times the lists imported their browser module: once, after the first render, as they all share it.</summary>
    public int ModuleImports => _browser.Imports;

    /// <summary>The ids of the lists that registered with the browser layer.</summary>
    public IReadOnlyCollection<string> Registered => _browser.Bridges.Keys;

    /// <summary>Renders <paramref name="lists"/>, in order, as the children of one host component.</summary>
    public static async Task<BortableListHost> StartAsync(IReadOnlyList<ListSetup> lists)
    {
        var host = new BortableListHost();

        await host._renderer.Dispatcher.InvokeAsync(async () =>
        {
            host._hostId = host._renderer.Mount(new ListsHost(lists, host));

            await host._renderer.RenderRootAsync(host._hostId);
        }).WaitAsync(_eventTimeout);

        // Read once while the frames still carry the element reference ids of the first render.
        await host.ReadAsync();

        return host;
    }

    /// <summary>The calls the browser layer made into the lists since the last time this was asked, with the answers.</summary>
    public string[] TakeBrowserCalls()
    {
        var calls = _browser.Calls.ToArray();
        _browser.Calls.Clear();
        return calls;
    }

    /// <summary>
    /// Dispatches one DOM event to a handler the lists rendered, and waits for the handler - and every
    /// render and callback it causes - to finish.
    /// </summary>
    public async Task DispatchAsync(ulong handler, EventArgs args)
    {
        await _renderer.Dispatcher.InvokeAsync(() => _renderer.DispatchEventAsync(handler, null, args)).WaitAsync(_eventTimeout);
    }

    /// <summary>
    /// A drag of the item rendered with <paramref name="key"/> starts in <paramref name="list"/>. Returns the ids of
    /// the lists that take the drag, or null when the list refused it.
    /// </summary>
    public async Task<string[]?> StartAsync(string list, string item, string key)
    {
        var result = await CallAsync(list, "Start", key, "mouse");
        var lists = result?.GetType().GetProperty("Lists")?.GetValue(result) as string[];

        _browser.Calls.Add($"Start('{list}', '{item}') -> {(lists is null ? "refused" : $"taken by {string.Join(", ", lists.Select(id => $"'{id}'"))}")}");
        return lists;
    }

    /// <summary>The pointer reached drop position <paramref name="index"/> of <paramref name="list"/>; returns whether a list changed.</summary>
    public async Task<bool> OverAsync(string list, int index)
    {
        var result = await CallAsync(list, "Over", index, 0) as int? ?? 0;

        _browser.Calls.Add($"Over('{list}', {index}) -> {(result > 0 ? "changed" : result < 0 ? "refused" : "unchanged")}");
        return result > 0;
    }

    /// <summary>The drag ended (<c>drop</c>, <c>release</c> or <c>cancel</c>), reported through <paramref name="list"/>.</summary>
    public async Task EndAsync(string list, string how)
    {
        await CallAsync(list, "End", how, 0);

        _browser.Calls.Add($"End('{how}')");
    }

    /// <summary>
    /// Calls a <c>[JSInvokable]</c> method of the bridge <paramref name="list"/> registered, by name, as the browser
    /// layer does, and waits for it - and every render and callback it causes - to finish.
    /// </summary>
    private async Task<object?> CallAsync(string list, string method, params object[] args)
    {
        if (_browser.Bridges.TryGetValue(list, out var bridge) is false)
            throw new InvalidOperationException($"The list '{list}' never registered with the browser layer.");

        var target = bridge.GetType().GetProperty("Value")!.GetValue(bridge)!;
        var invokable = target.GetType().GetMethods()
            .Single(candidate => candidate.Name == method && candidate.IsDefined(typeof(JSInvokableAttribute), inherit: false));

        return await _renderer.Dispatcher.InvokeAsync(async () =>
        {
            var task = (Task)invokable.Invoke(target, args)!;
            await task;
            return invokable.ReturnType.IsGenericType ? invokable.ReturnType.GetProperty("Result")!.GetValue(task) : null;
        }).WaitAsync(_eventTimeout);
    }

    /// <summary>Every list as it renders right now, read off the render tree.</summary>
    public Task<RenderedList[]> ReadAsync() => _renderer.Dispatcher.InvokeAsync(() =>
    {
        var lists = new List<RenderedList>();
        var frames = _renderer.Frames(_hostId);

        for (var i = 0; i < frames.Count; i++)
        {
            var frame = frames.Array[i];

            if (frame.FrameType != RenderTreeFrameType.Component || frame.Component is not Bortable<string> list) continue;

            var parsed = Parse(list.Id, _renderer.Frames(frame.ComponentId));

            if (string.IsNullOrEmpty(parsed.Element) is false) _elements[list.Id] = parsed.Element;
            else if (_elements.TryGetValue(list.Id, out var known)) parsed = parsed with { Element = known };

            lists.Add(parsed);
        }

        return lists.ToArray();
    });

    private void Record(string name, string list, BortableEventArgs<string>? args, IList<string>? items)
        => _callbacks.Add(new Callback(Step, name, list, args, items is null ? null : [.. items]));

    /// <summary>
    /// Reads one list out of its frames: the container and the wrappers are the elements carrying the
    /// data attributes the browser layer looks for too - <c>data-bortable-list</c> and
    /// <c>data-bortable-item</c> - so this finds exactly what bit-bortable.js would.
    /// </summary>
    private static RenderedList Parse(string id, ArrayRange<RenderTreeFrame> frames)
    {
        var array = frames.Array;
        var containerClass = string.Empty;
        string? element = null;
        var items = new List<RenderedItem>();

        for (var i = 0; i < frames.Count; i++)
        {
            if (array[i].FrameType != RenderTreeFrameType.Element) continue;

            var end = i + array[i].ElementSubtreeLength;
            var attributes = new Dictionary<string, RenderTreeFrame>(StringComparer.Ordinal);

            var next = i + 1;
            for (; next < end && array[next].FrameType == RenderTreeFrameType.Attribute; next++) attributes[array[next].AttributeName] = array[next];

            if (attributes.ContainsKey("data-bortable-list"))
            {
                containerClass = Text(attributes, "class");

                // The @ref capture follows the attributes: its id is the ElementReference the list hands
                // the browser layer when it registers.
                if (next < end && array[next].FrameType == RenderTreeFrameType.ElementReferenceCapture) element = array[next].ElementReferenceCaptureId;
            }
            else if (attributes.ContainsKey("data-bortable-item"))
            {
                var placeholder = attributes.ContainsKey("data-bortable-placeholder");

                // A wrapper is keyed by its item; the placeholder is not, and renders the dragged item's
                // template - the text the lab's template writes is the item itself.
                var item = placeholder ? TextWithin(array, i, end) : array[i].ElementKey as string ?? TextWithin(array, i, end);

                items.Add(new RenderedItem(
                    item,
                    Text(attributes, "class"),
                    placeholder,
                    Text(attributes, "draggable") == "true",
                    attributes.ContainsKey("data-bortable-key") ? Text(attributes, "data-bortable-key") : null,
                    Handler(attributes, "onclick")));
            }
        }

        return new RenderedList(id, containerClass, element, [.. items]);
    }

    private static string Text(Dictionary<string, RenderTreeFrame> attributes, string name)
        => attributes.TryGetValue(name, out var frame) ? frame.AttributeValue?.ToString() ?? string.Empty : string.Empty;

    private static ulong Handler(Dictionary<string, RenderTreeFrame> attributes, string name)
        => attributes.TryGetValue(name, out var frame) ? frame.AttributeEventHandlerId : 0;

    private static string TextWithin(RenderTreeFrame[] array, int start, int end)
    {
        for (var i = start; i < end; i++)
        {
            if (array[i].FrameType == RenderTreeFrameType.Text) return array[i].TextContent;
        }

        return string.Empty;
    }

    public async ValueTask DisposeAsync()
    {
        await _renderer.DisposeAsync();
        await _scope.DisposeAsync();
        await _services.DisposeAsync();
    }

    /// <summary>
    /// The page the lists sit on: one <see cref="Bortable{TItem}"/> per list, bound the way
    /// <c>@bind-Items</c> binds one - ItemsChanged hands the list back and the page re-renders - and
    /// every lifecycle callback wired to the log. The callbacks' receiver is this component, as it is
    /// the page for a handler written in markup, so a callback re-renders it exactly as it would there.
    /// </summary>
    private sealed class ListsHost(IReadOnlyList<ListSetup> lists, BortableListHost owner) : ComponentBase
    {
        // The item itself as text: what the lab reads back out of the placeholder, and what a page
        // writing <ItemTemplate Context="item">@item</ItemTemplate> renders.
        private static readonly RenderFragment<string> _template = item => builder => builder.AddContent(0, item);

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            foreach (var list in lists)
            {
                builder.OpenComponent<Bortable<string>>(0);
                builder.SetKey(list.Id);
                builder.AddComponentParameter(1, nameof(Bortable<string>.Id), list.Id);
                builder.AddComponentParameter(2, nameof(Bortable<string>.Items), list.Items);
                builder.AddComponentParameter(3, nameof(Bortable<string>.ItemsChanged), EventCallback.Factory.Create<IList<string>>(this, items =>
                {
                    list.Items = items;
                    owner.Record("ItemsChanged", list.Id, null, items);
                }));
                builder.AddComponentParameter(4, nameof(Bortable<string>.ItemTemplate), _template);
                if (list.Group is not null) builder.AddComponentParameter(5, nameof(Bortable<string>.Group), list.Group);
                builder.AddComponentParameter(6, nameof(Bortable<string>.Pull), list.Pull);
                builder.AddComponentParameter(7, nameof(Bortable<string>.Put), list.Put);
                builder.AddComponentParameter(8, nameof(Bortable<string>.Sort), list.Sort);
                builder.AddComponentParameter(9, nameof(Bortable<string>.Disabled), list.Disabled);
                builder.AddComponentParameter(10, nameof(Bortable<string>.Swap), list.Swap);
                builder.AddComponentParameter(11, nameof(Bortable<string>.MultiDrag), list.MultiDrag);
                if (list.Filter is not null) builder.AddComponentParameter(12, nameof(Bortable<string>.Filter), list.Filter);
                if (list.Clone is not null) builder.AddComponentParameter(13, nameof(Bortable<string>.Clone), list.Clone);
                builder.AddComponentParameter(14, nameof(Bortable<string>.OnStart), Log(list, "OnStart"));
                builder.AddComponentParameter(15, nameof(Bortable<string>.OnChange), Log(list, "OnChange"));
                builder.AddComponentParameter(16, nameof(Bortable<string>.OnUpdate), Log(list, "OnUpdate"));
                builder.AddComponentParameter(17, nameof(Bortable<string>.OnRemove), Log(list, "OnRemove"));
                builder.AddComponentParameter(18, nameof(Bortable<string>.OnAdd), Log(list, "OnAdd"));
                builder.AddComponentParameter(19, nameof(Bortable<string>.OnSort), Log(list, "OnSort"));
                builder.AddComponentParameter(20, nameof(Bortable<string>.OnEnd), Log(list, "OnEnd"));
                builder.CloseComponent();
            }
        }

        private EventCallback<BortableEventArgs<string>> Log(ListSetup list, string name)
            => EventCallback.Factory.Create<BortableEventArgs<string>>(this, args => owner.Record(name, list.Id, args, null));
    }

    /// <summary>
    /// The renderer: a render batch goes nowhere, and an exception is kept for the report instead of
    /// tearing the request down - a list that throws on some drag is a finding, not a crash.
    /// </summary>
    private sealed class ListRenderer(IServiceProvider services) : Renderer(services, NullLoggerFactory.Instance)
    {
        private readonly List<Exception> _exceptions = [];

        public IReadOnlyList<Exception> Exceptions => _exceptions;

        public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

        // Interactive, because that is the only way a list is ever meant to answer a drag: it is what a
        // component asking ComponentBase.RendererInfo would be told in the browser.
        protected override RendererInfo RendererInfo { get; } = new("BortableDragLab", isInteractive: true);

        public int Mount(IComponent component) => AssignRootComponentId(component);

        public Task RenderRootAsync(int componentId) => RenderRootComponentAsync(componentId);

        public ArrayRange<RenderTreeFrame> Frames(int componentId) => GetCurrentRenderTreeFrames(componentId);

        protected override void HandleException(Exception exception) => _exceptions.Add(exception);

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch) => Task.CompletedTask;
    }

    /// <summary>
    /// Stands in for the browser. The lists import its module once, share it and register with it, handing it
    /// their bridge; it keeps each bridge by the list's id, for the lab to call, and answers every other call
    /// (the FLIP animation) with nothing.
    /// </summary>
    private sealed class BrowserStandIn : IJSRuntime, IJSObjectReference
    {
        public int Imports { get; private set; }

        /// <summary>The bridge (a <c>DotNetObjectReference</c>) each list registered, by the list's id.</summary>
        public Dictionary<string, object> Bridges { get; } = new(StringComparer.Ordinal);

        /// <summary>The calls the lab made into the lists, as the browser layer would have made them.</summary>
        public List<string> Calls { get; } = [];

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            switch (identifier)
            {
                // The module is this same object: one stand-in, one table of lists.
                case "import" when typeof(TValue) == typeof(IJSObjectReference):
                    Imports++;
                    return ValueTask.FromResult((TValue)(object)this);

                // register(listElement, bridge, options): the options carry the list's id.
                case "register" when args is { Length: 3 } && args[1] is { } bridge && IdOf(args[2]) is { } id:
                    Bridges[id] = bridge;
                    break;
            }

            return ValueTask.FromResult(default(TValue)!);
        }

        private static string? IdOf(object? options) => options?.GetType().GetProperty("Id")?.GetValue(options) as string;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
