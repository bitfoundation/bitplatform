using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;

namespace Bit.Blow.Benchmarks;

/// <summary>
/// Blazor's renderer with nothing on the other end: it builds and diffs render trees exactly as the WebAssembly and
/// Server renderers do, and then drops the batch instead of sending it to a browser. That isolates the part of a
/// frame Blow is responsible for. (bUnit's renderer is not used here: it regenerates the HTML of every parent of
/// each component that renders, which is quadratic in the number of nodes and would swamp the measurement.)
/// </summary>
internal sealed class HeadlessRenderer : Renderer
{
    public HeadlessRenderer() : base(new Services(), NullLoggerFactory.Instance) { }

    public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

    /// <summary>How many components rendered in the most recent batch.</summary>
    public int LastBatchComponents { get; private set; }

    /// <summary>Renders <typeparamref name="T"/> as a root component with the given parameters.</summary>
    public async Task<T> RenderAsync<T>(Dictionary<string, object?> parameters) where T : IComponent
    {
        var component = (T)InstantiateComponent(typeof(T));
        var id = AssignRootComponentId(component);
        await Dispatcher.InvokeAsync(() => RenderRootComponentAsync(id, ParameterView.FromDictionary(parameters)));
        return component;
    }

    protected override void HandleException(Exception exception) => throw exception;

    protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
    {
        LastBatchComponents = renderBatch.UpdatedComponents.Count;
        return Task.CompletedTask;
    }

    /// <summary>Provides the canvas's one injected service, a JS runtime that answers every call with a default.</summary>
    private sealed class Services : IServiceProvider
    {
        private readonly NullJSRuntime _js = new();

        public object? GetService(Type serviceType) => serviceType == typeof(IJSRuntime) ? _js : null;
    }

    /// <summary>
    /// Stands in for the browser layer: the module import returns a module whose every function returns the
    /// default of its result type, which is what the canvas sees before anything has been measured.
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
