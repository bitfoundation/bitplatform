// A Renderer is the framework's own extension point for running components outside a browser -
// bUnit and the static HTML renderer are built on it - and it lives in the RenderTree namespace the
// analyzer guards against everyday use. Hosting the real BlowCanvas is the whole point here.
#pragma warning disable BL0006

using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Bit.Blow.Demo.Server.Services;

/// <summary>
/// Renders a real <see cref="BlowCanvas"/> in-process, with no browser behind it, and reports what
/// the canvas itself raised through <see cref="BlowCanvas.OnError"/>.
/// <para>
/// The canvas checks the graph it is handed while it renders - duplicate node and edge ids, edges
/// that name a node that is not there, children whose parent is missing, handles that share an id -
/// and every one of those checks is private to it. Running the component is the only way to put a
/// flow through them without writing a second copy of each, and a second copy is exactly what
/// drifts: it would keep answering "fine" for a graph the shipped canvas has since learnt to object to.
/// </para>
/// <para>
/// The browser half is a stand-in that answers the one call whose answer matters, the DOM
/// measurement, with the sizes and handles the flow carries - which is what lets the canvas run its
/// handle check and resolve real node sizes. Every other call answers with nothing, the same way the
/// canvas sees an interop call during prerendering, which it is written to shrug off.
/// </para>
/// </summary>
internal static class BlowCanvasHost
{
    /// <summary>What one render of the canvas reported.</summary>
    public sealed record Result(IReadOnlyList<BlowError> Errors, IReadOnlyList<Exception> Exceptions);

    /// <summary>
    /// Renders the canvas over <paramref name="nodes"/> and <paramref name="edges"/> - the lists
    /// themselves, so the absolute positions and measured sizes the canvas resolves end up on these
    /// very node instances - and returns what it reported.
    /// </summary>
    public static async Task<Result> RenderAsync(List<BlowNode> nodes, List<BlowEdge> edges, BlowMeasuredNode[] measurement)
    {
        var errors = new List<BlowError>();

        var services = new ServiceProviderStub(new MeasuringJSRuntime(measurement));

        await using var renderer = new CanvasRenderer(services);

        await renderer.RenderAsync(new Dictionary<string, object?>
        {
            [nameof(BlowCanvas.Nodes)] = nodes,
            [nameof(BlowCanvas.Edges)] = edges,
            // A canvas only collects problems when somebody is listening for them.
            [nameof(BlowCanvas.OnError)] = EventCallback.Factory.Create<BlowError>(errors, errors.Add),
        });

        return new Result(errors, renderer.Exceptions);
    }

    /// <summary>
    /// The renderer: a render batch goes nowhere, and an exception is kept for the report instead
    /// of tearing the request down - a canvas that throws on some graph is a finding, not a crash.
    /// </summary>
    private sealed class CanvasRenderer(IServiceProvider services) : Renderer(services, NullLoggerFactory.Instance)
    {
        private readonly List<Exception> _exceptions = [];

        public IReadOnlyList<Exception> Exceptions => _exceptions;

        public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

        // Interactive, because that is the only way the canvas is ever meant to run: it is what a
        // component asking ComponentBase.RendererInfo would be told in the browser.
        protected override RendererInfo RendererInfo { get; } = new("BlowFlowLab", isInteractive: true);

        public Task RenderAsync(IDictionary<string, object?> parameters) => Dispatcher.InvokeAsync(async () =>
        {
            var canvas = InstantiateComponent(typeof(BlowCanvas));
            var id = AssignRootComponentId(canvas);

            await RenderRootComponentAsync(id, ParameterView.FromDictionary(parameters));
        });

        protected override void HandleException(Exception exception) => _exceptions.Add(exception);

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch) => Task.CompletedTask;
    }

    /// <summary>The canvas injects exactly one service, the JS runtime; nothing else is resolved.</summary>
    private sealed class ServiceProviderStub(IJSRuntime js) : IServiceProvider
    {
        public object? GetService(Type serviceType) => serviceType == typeof(IJSRuntime) ? js : null;
    }

    /// <summary>
    /// Stands in for the browser. The canvas imports its module once and calls everything through
    /// it; the module answers <c>measureNodes</c> - the call that reads node sizes and handle anchors
    /// out of the DOM - with the measurement the flow carries, and every other call with nothing.
    /// </summary>
    private sealed class MeasuringJSRuntime(BlowMeasuredNode[] measurement) : IJSRuntime, IJSObjectReference
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            object? answer = identifier switch
            {
                // The module is this same object: one stand-in, one table of answers.
                "import" when typeof(TValue) == typeof(IJSObjectReference) => this,
                "measureNodes" when typeof(TValue) == typeof(BlowMeasuredNode[]) => measurement,
                _ => null
            };

            // Nothing, for a call that answers with a value type, is its default: isEditableFocused
            // answers false, which is the truth for a canvas nobody is typing into.
            return ValueTask.FromResult(answer is TValue value ? value : default!);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
