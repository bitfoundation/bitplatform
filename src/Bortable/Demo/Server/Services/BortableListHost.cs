// A Renderer is the framework's own extension point for running components outside a browser -
// bUnit and the static HTML renderer are built on it - and it lives in the RenderTree namespace the
// analyzer guards against everyday use. Hosting the real Bortable is the whole point here, and so is
// reading back the frames it rendered: they are where its event handlers are found.
#pragma warning disable BL0006

using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bit.Bortable.Demo.Server.Services;

/// <summary>
/// Renders real <see cref="Bortable{TItem}"/> lists in-process, with no browser behind them, and drives
/// a drag through them by dispatching the DOM events a browser would - to the very event handlers the
/// lists rendered.
/// <para>
/// Every decision a drag involves is private to the component: whether a list takes an item
/// (<c>CanAccept</c>, <c>CanReceiveFrom</c>), where the item goes, what is moved, cloned or swapped,
/// which events fire and in which order. Running the component is the only way to put a drag through
/// all of it without writing a second copy of each rule, and a second copy is exactly what drifts: it
/// would keep answering "accepted" for a drop the shipped list has since learnt to refuse.
/// </para>
/// <para>
/// The handlers are found the way the browser's half of Blazor finds them: in the render tree. Each
/// list's frames are walked for its container (the element carrying <c>data-bortable-list</c>, with its
/// <c>ondragover</c> and <c>ondrop</c>) and its item wrappers (keyed by their item, with
/// <c>ondragstart</c>, <c>ondragend</c> and, in multi-drag mode, <c>onclick</c>), and an event is
/// dispatched to the handler id found there. Every render hands out new ids, so the frames are read
/// again before each event.
/// </para>
/// <para>
/// The browser layer is a stand-in that answers the three calls whose answers matter - the hit tests
/// <c>dropIndex</c> and <c>itemIndexAt</c>, and <c>wouldNest</c> - with the position the caller asked
/// for, for the list under the pointer only, exactly as the real module answers for one element at a
/// time. Every other call (the FLIP animation, the drag handle) answers with nothing, the way the
/// component sees an interop call during prerendering, which it is written to shrug off.
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

    /// <summary>An item wrapper, or the drop placeholder, as the list rendered it - with its handler ids (0: none).</summary>
    public sealed record RenderedItem(string Item, string Class, bool Placeholder, bool Draggable, ulong DragStart, ulong DragEnd, ulong Click);

    /// <summary>A list as it rendered: its container's class, element reference and handler ids, and its wrappers in DOM order.</summary>
    public sealed record RenderedList(string Id, string Class, string? Element, ulong DragOver, ulong Drop, RenderedItem[] Items);

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
        // The library's own registration, so the lists inject the coordinator an app would give them -
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

    /// <summary>How many lists imported their browser module - every one should have, after its first render.</summary>
    public int ModuleImports => _browser.Imports;

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

    /// <summary>
    /// Puts the pointer over a list for the next dragover: the browser layer answers its hit tests
    /// with these positions, for that list's element and no other. -1 is "over no item" - what the real
    /// module answers for a dead zone.
    /// </summary>
    public void PointAt(RenderedList list, int dropIndex, int itemIndex)
    {
        _browser.Over = list.Element;
        _browser.OverList = list.Id;
        _browser.DropIndex = dropIndex;
        _browser.ItemIndex = itemIndex;
    }

    /// <summary>The calls the lists made into the browser layer since the last time this was asked, with the answers.</summary>
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
        ulong dragOver = 0, drop = 0;
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
                dragOver = Handler(attributes, "ondragover");
                drop = Handler(attributes, "ondrop");

                // The @ref capture follows the attributes: its id is the ElementReference the list hands
                // the browser layer, which is how the stand-in knows which list a hit test is about.
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
                    Handler(attributes, "ondragstart"),
                    Handler(attributes, "ondragend"),
                    Handler(attributes, "onclick")));
            }
        }

        return new RenderedList(id, containerClass, element, dragOver, drop, [.. items]);
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
    /// Stands in for the browser. Each list imports its module once and calls everything through it;
    /// the module answers the hit tests for the list under the pointer with the position the lab put
    /// there, and every other call with nothing.
    /// </summary>
    private sealed class BrowserStandIn : IJSRuntime, IJSObjectReference
    {
        /// <summary>The element reference id of the list under the pointer.</summary>
        public string? Over { get; set; }

        /// <summary>That list's Id, for the record of the calls.</summary>
        public string? OverList { get; set; }

        public int DropIndex { get; set; } = -1;

        public int ItemIndex { get; set; } = -1;

        public int Imports { get; private set; }

        public List<string> Calls { get; } = [];

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            // Every call a list makes into its module passes the list's own element first.
            var element = args is { Length: > 0 } && args[0] is ElementReference reference ? reference.Id : null;
            var over = element is not null && element == Over;

            object? answer = identifier switch
            {
                // The module is this same object: one stand-in, one table of answers.
                "import" when typeof(TValue) == typeof(IJSObjectReference) => Import(),
                "dropIndex" => Answer(identifier, over, over ? DropIndex : -1),
                "itemIndexAt" => Answer(identifier, over, over ? ItemIndex : -1),
                // Lists are never nested here, so no list is ever inside the item being dragged.
                "wouldNest" => Answer(identifier, over, false),
                _ => null
            };

            // Nothing, for a call that answers with a value type, is its default.
            return ValueTask.FromResult(answer is TValue value ? value : default!);
        }

        private IJSObjectReference Import()
        {
            Imports++;
            return this;
        }

        private object Answer(string identifier, bool over, object value)
        {
            var list = over ? $"'{OverList}'" : "a list not under the pointer";
            Calls.Add($"{identifier}({list}) -> {(value is bool flag ? (flag ? "true" : "false") : value)}");
            return value;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
