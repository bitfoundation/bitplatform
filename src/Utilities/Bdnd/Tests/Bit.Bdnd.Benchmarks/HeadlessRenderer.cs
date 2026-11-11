using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;

namespace Bit.Bdnd.Benchmarks;

/// <summary>
/// Blazor's renderer with nothing on the other end: it builds and diffs render trees exactly as the WebAssembly and
/// Server renderers do, and then drops the batch instead of sending it to a browser. That isolates the part of a
/// drag Bdnd is responsible for in .NET - the manager's state changes, which components they re-render, and what
/// those renders cost. (bUnit's renderer is not used: it regenerates the HTML of every parent of each component that
/// renders, which would swamp the measurement.)
/// </summary>
internal sealed class HeadlessRenderer : Renderer
{
    public HeadlessRenderer() : base(new Services(), NullLoggerFactory.Instance) { }

    public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

    /// <summary>How many components rendered since <see cref="ResetCount"/>, over every batch.</summary>
    public int RenderedComponents { get; private set; }

    public void ResetCount() => RenderedComponents = 0;

    /// <summary>Renders <typeparamref name="T"/> as a root component with the given parameters.</summary>
    public T Render<T>(Dictionary<string, object?> parameters) where T : IComponent
    {
        var component = (T)InstantiateComponent(typeof(T));
        var id = AssignRootComponentId(component);
        Run(() => RenderRootComponentAsync(id, ParameterView.FromDictionary(parameters)));
        return component;
    }

    /// <summary>Runs <paramref name="work"/> on the renderer's dispatcher, as a JS-invoked call is, and waits for it.</summary>
    public void Run(Func<Task> work) => Dispatcher.InvokeAsync(work).GetAwaiter().GetResult();

    protected override void HandleException(Exception exception) => throw exception;

    protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
    {
        RenderedComponents += renderBatch.UpdatedComponents.Count;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Provides what the components resolve: the interop, over a JS runtime that answers every call with a default.
    /// </summary>
    private sealed class Services : IServiceProvider
    {
        private readonly NullJSRuntime _js = new();
        private readonly BdndInterop _interop;

        public Services() => _interop = new BdndInterop(_js);

        public object? GetService(Type serviceType) =>
            serviceType == typeof(BdndInterop) ? _interop
            : serviceType == typeof(IJSRuntime) ? _js
            : serviceType == typeof(IServiceProvider) ? this
            : null;
    }

    /// <summary>
    /// Stands in for the browser module: an import returns a module whose every function returns the default of its
    /// result type - a registration is this object again, a keyboard move lands nowhere.
    /// </summary>
    private sealed class NullJSRuntime : IJSRuntime, IJSObjectReference
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => new(typeof(TValue) == typeof(IJSObjectReference) ? (TValue)(object)this : default!);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => InvokeAsync<TValue>(identifier, args);

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
