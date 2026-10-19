using Microsoft.JSInterop;

namespace Bit.Bdnd.Tests.Fakes;

/// <summary>
/// A JS object (a module, or a registration returned by <c>registerDragSource</c>/<c>registerDropTarget</c>)
/// that records every call and answers, fails or holds back per identifier.
/// </summary>
internal sealed class FakeJSObjectReference : IJSObjectReference
{
    private readonly Dictionary<string, Func<object?[]?, object?>> _results = [];
    private readonly Dictionary<string, Exception> _failures = [];
    private readonly Dictionary<string, Task> _gates = [];

    public List<(string Identifier, object?[]? Args)> Calls { get; } = [];

    public int DisposeCount { get; private set; }

    public Exception? DisposeException { get; set; }

    public IEnumerable<object?[]?> CallsTo(string identifier) =>
        Calls.Where(c => c.Identifier == identifier).Select(c => c.Args);

    public FakeJSObjectReference Returns(string identifier, Func<object?[]?, object?> result)
    {
        _results[identifier] = result;
        return this;
    }

    public FakeJSObjectReference Fails(string identifier, Exception exception)
    {
        _failures[identifier] = exception;
        return this;
    }

    /// <summary>Calls to <paramref name="identifier"/> no longer fail.</summary>
    public FakeJSObjectReference Succeeds(string identifier)
    {
        _failures.Remove(identifier);
        return this;
    }

    /// <summary>Calls to <paramref name="identifier"/> complete only once <paramref name="gate"/> does.</summary>
    public FakeJSObjectReference WaitsFor(string identifier, Task gate)
    {
        _gates[identifier] = gate;
        return this;
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        Calls.Add((identifier, args));
        if (_gates.TryGetValue(identifier, out var gate)) await gate;
        if (_failures.TryGetValue(identifier, out var failure)) throw failure;
        return _results.TryGetValue(identifier, out var result) ? (TValue)result(args)! : default!;
    }

    public ValueTask DisposeAsync()
    {
        DisposeCount++;
        return DisposeException is null ? ValueTask.CompletedTask : ValueTask.FromException(DisposeException);
    }
}
