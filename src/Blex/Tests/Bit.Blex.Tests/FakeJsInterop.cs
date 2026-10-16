using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Blex.Tests;

/// <summary>
/// A hand-written <see cref="IJSObjectReference"/> that records what was invoked on it.
/// </summary>
/// <remarks>
/// The Blazor bridges are the one part of Blex that cannot be exercised through the core types, and
/// they hold real logic: connection handles, watcher handles, which module function gets which
/// arguments. Faking the two interop interfaces keeps that testable without dragging a rendering
/// harness in.
/// </remarks>
public sealed class FakeJsModule : IJSObjectReference
{
    /// <summary>Every invocation, in order.</summary>
    public List<(string Identifier, object?[] Args)> Calls { get; } = [];

    /// <summary>Canned return values, keyed by module function name.</summary>
    public Dictionary<string, object?> Returns { get; } = [];

    /// <summary>Whether the module reference was disposed.</summary>
    public bool Disposed { get; private set; }

    /// <summary>When set, an invocation it returns an exception for throws that exception instead of answering.</summary>
    public Func<string, Exception?>? Failure { get; set; }

    /// <summary>The arguments of the first call to <paramref name="identifier"/>, or <c>null</c>.</summary>
    public object?[]? ArgsOf(string identifier)
        => Calls.FirstOrDefault(c => c.Identifier == identifier).Args;

    /// <summary>How many times <paramref name="identifier"/> was invoked.</summary>
    public int CountOf(string identifier) => Calls.Count(c => c.Identifier == identifier);

    /// <inheritdoc />
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
        Calls.Add((identifier, args ?? []));

        if (Failure?.Invoke(identifier) is { } failure)
            throw failure;

        return Returns.TryGetValue(identifier, out var value) && value is TValue typed
            ? new ValueTask<TValue>(typed)
            : new ValueTask<TValue>(default(TValue)!);
    }

    /// <inheritdoc />
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        => InvokeAsync<TValue>(identifier, args);

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        Disposed = true;
        return ValueTask.CompletedTask;
    }
}

/// <summary>An <see cref="IJSRuntime"/> whose only trick is handing out a <see cref="FakeJsModule"/> for <c>import</c>.</summary>
public sealed class FakeJsRuntime : IJSRuntime
{
    /// <summary>The module every <c>import</c> resolves to.</summary>
    public FakeJsModule Module { get; } = new();

    /// <summary>The URLs that were imported.</summary>
    public List<string> Imports { get; } = [];

    /// <summary>When set, <c>import</c> throws this instead of resolving (a torn-down circuit).</summary>
    public Func<Exception>? ImportFailure { get; set; }

    /// <inheritdoc />
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
        if (identifier != "import")
            return new ValueTask<TValue>(default(TValue)!);

        if (ImportFailure is { } failure)
            throw failure();

        Imports.Add(args?.OfType<string>().FirstOrDefault() ?? "");
        return new ValueTask<TValue>((TValue)(object)Module);
    }

    /// <inheritdoc />
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        => InvokeAsync<TValue>(identifier, args);
}
