using System.Text.Json;
using Microsoft.JSInterop;

namespace Bit.Bsqlite.Interop.Tests;

/// <summary>
/// A scripted <see cref="IJSRuntime"/> that plays back envelopes and binary replies the way the
/// real host's <c>call</c> and <c>executeBinary</c> methods would, so the transport can be tested
/// without a browser.
/// </summary>
internal sealed class ScriptedJsRuntime : IJSRuntime
{
    private readonly Queue<object> _replies = new();

    public List<JsCall> Calls { get; } = [];

    public ScriptedJsObject Module { get; }

    public ScriptedJsObject Host { get; }

    public bool HostDisposed { get; private set; }

    /// <summary>The .NET object and method the host module was asked to forward committed writes to.</summary>
    public (object Target, string Method)? Subscription { get; private set; }

    public ScriptedJsRuntime()
    {
        Module = new ScriptedJsObject(this, "module");
        Host = new ScriptedJsObject(this, "host");
    }

    /// <summary>Queues a JSON envelope for the next <c>call</c>.</summary>
    public void EnqueueEnvelope(string json)
    {
        using var document = JsonDocument.Parse(json);
        _replies.Enqueue(document.RootElement.Clone());
    }

    /// <summary>Queues a binary reply for the next <c>executeBinary</c>.</summary>
    public void EnqueueReply(byte[] reply) => _replies.Enqueue(reply);

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    public ValueTask<TValue> InvokeAsync<TValue>(
        string identifier,
        CancellationToken cancellationToken,
        object?[]? args)
    {
        Calls.Add(new JsCall(identifier, args ?? []));

        object result = identifier switch
        {
            "import" => Module,
            _ => throw new InvalidOperationException($"Unexpected runtime call '{identifier}'."),
        };

        return ValueTask.FromResult((TValue)result);
    }

    internal ValueTask<TValue> InvokeObjectAsync<TValue>(
        string objectName,
        string identifier,
        object?[]? args)
    {
        Calls.Add(new JsCall($"{objectName}.{identifier}", args ?? []));

        if (objectName == "module" && identifier == "createHost")
        {
            return ValueTask.FromResult((TValue)(object)Host);
        }

        // Subscribes the transport to committed writes. Nothing to play back - the recorded call
        // in Calls, and the target it names, are what the tests assert on.
        if (objectName == "module" && identifier == "listen")
        {
            Subscription = (args![1]!, args.Length > 3 ? (string)args[3]! : "OnTablesChanged");
            return ValueTask.FromResult(default(TValue)!);
        }

        if (objectName == "host" && identifier is "call" or "executeBinary")
        {
            if (_replies.Count == 0)
            {
                throw new InvalidOperationException("The test did not enqueue a reply for this call.");
            }

            return ValueTask.FromResult((TValue)_replies.Dequeue());
        }

        if (objectName == "host" && identifier == "dispose")
        {
            HostDisposed = true;
            return ValueTask.FromResult(default(TValue)!);
        }

        // Web Locks. Nothing to play back - the recorded calls are what the tests assert on.
        if (objectName == "host" && identifier is "acquireLock" or "releaseLock")
        {
            return ValueTask.FromResult(default(TValue)!);
        }

        throw new InvalidOperationException($"Unexpected call '{objectName}.{identifier}'.");
    }
}

internal sealed class ScriptedJsObject(ScriptedJsRuntime runtime, string name) : IJSObjectReference
{
    /// <summary>Whether <see cref="DisposeAsync"/> ran. A call after it throws, as a real reference's does.</summary>
    public bool Disposed { get; private set; }

    /// <summary>
    /// Runs inside <see cref="DisposeAsync"/>, once the reference is disposed: the moment work still
    /// unwinding from the worker's termination can reach it in a browser.
    /// </summary>
    public Func<Task>? Disposing { get; set; }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    public ValueTask<TValue> InvokeAsync<TValue>(
        string identifier,
        CancellationToken cancellationToken,
        object?[]? args)
    {
        ObjectDisposedException.ThrowIf(Disposed, this);
        return runtime.InvokeObjectAsync<TValue>(name, identifier, args);
    }

    public async ValueTask DisposeAsync()
    {
        Disposed = true;

        if (Disposing is { } disposing)
        {
            await disposing();
        }
    }
}

internal sealed record JsCall(string Identifier, object?[] Args);
