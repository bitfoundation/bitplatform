using Microsoft.JSInterop;

namespace Bit.Bdnd.Tests.Fakes;

/// <summary>A JS runtime whose module imports answer from a queue: a module, or an exception to fail with.</summary>
internal sealed class FakeJSRuntime : IJSRuntime
{
    private readonly Queue<object> _imports = new();

    public List<(string Identifier, object?[]? Args)> Calls { get; } = [];

    public int ImportCount => Calls.Count(c => c.Identifier == "import");

    /// <summary>Queues the answer of the next <c>import</c>: an <see cref="IJSObjectReference"/> or an <see cref="Exception"/>.</summary>
    public FakeJSRuntime NextImport(object moduleOrException)
    {
        _imports.Enqueue(moduleOrException);
        return this;
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        Calls.Add((identifier, args));
        if (identifier != "import" || _imports.Count == 0)
            return ValueTask.FromException<TValue>(new InvalidOperationException($"Unexpected call to '{identifier}'."));

        return _imports.Dequeue() switch
        {
            Exception exception => ValueTask.FromException<TValue>(exception),
            var module => ValueTask.FromResult((TValue)module),
        };
    }
}
