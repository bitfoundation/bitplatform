using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// The callback relay shared by the built-in AI services: model download progress, and the chunks of
/// a streaming run. One instance per service, so its <see cref="DotNetObjectReference{T}"/> - and
/// everything a caller's handler captures - is released when the service's scope is torn down.
/// </summary>
internal sealed class AiInterop : IDisposable
{
    internal const string ChunkMethodName = nameof(InvokeAiChunk);
    internal const string DoneMethodName = nameof(InvokeAiDone);
    internal const string ProgressMethodName = nameof(InvokeAiProgress);

    private readonly ConcurrentDictionary<Guid, Action<string>> _chunkHandlers = new();
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<string>> _completions = new();
    private readonly ConcurrentDictionary<Guid, Action<double>> _progressHandlers = new();

    private DotNetObjectReference<AiInterop>? _dotNetRef;

    internal DotNetObjectReference<AiInterop> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>
    /// Registers a streaming run and hands back the task that completes with the whole text once the
    /// stream ends. The chunk handler is optional - a caller that only wants the final text can pass
    /// null and simply await.
    /// </summary>
    internal (Guid Id, Task<string> Completion) BeginStream(Action<string>? onChunk)
    {
        var id = Guid.NewGuid();
        if (onChunk is not null) _chunkHandlers[id] = onChunk;
        // RunContinuationsAsynchronously so an awaiting caller's continuation never runs inline on
        // the interop dispatch that completed it.
        _completions[id] = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        return (id, _completions[id].Task);
    }

    /// <summary>
    /// Drops a streaming run that never started. The JS side never got as far as reporting done, so
    /// the registration has to be taken back here or the caller's task would wait forever.
    /// </summary>
    internal void EndStream(Guid id, Exception error)
    {
        _chunkHandlers.TryRemove(id, out _);
        if (_completions.TryRemove(id, out var completion)) completion.TrySetException(error);
    }

    /// <summary>Registers a download-progress handler for one <c>create</c> call.</summary>
    internal Guid BeginProgress(Action<double>? onProgress)
    {
        var id = Guid.NewGuid();
        if (onProgress is not null) _progressHandlers[id] = onProgress;
        return id;
    }

    internal void EndProgress(Guid id) => _progressHandlers.TryRemove(id, out _);

    /// <summary>Invoked from JS for each chunk of a streaming run.</summary>
    [JSInvokable(ChunkMethodName)]
    public void InvokeAiChunk(Guid id, string chunk)
    {
        if (_chunkHandlers.TryGetValue(id, out var handler)) handler.Invoke(chunk ?? string.Empty);
    }

    /// <summary>
    /// Invoked from JS when a streaming run ends, with the text accumulated so far and an error
    /// message when it ended badly.
    /// </summary>
    [JSInvokable(DoneMethodName)]
    public void InvokeAiDone(Guid id, string text, string error)
    {
        _chunkHandlers.TryRemove(id, out _);
        if (_completions.TryRemove(id, out var completion) is false) return;

        // TrySet* rather than Set*: a stream that reports done twice must not throw out of an
        // interop dispatch.
        if (string.IsNullOrEmpty(error)) completion.TrySetResult(text ?? string.Empty);
        else completion.TrySetException(new InvalidOperationException(error));
    }

    /// <summary>Invoked from JS as the model downloads, with a 0-1 fraction.</summary>
    [JSInvokable(ProgressMethodName)]
    public void InvokeAiProgress(Guid id, double loaded)
    {
        if (_progressHandlers.TryGetValue(id, out var handler)) handler.Invoke(loaded);
    }

    public void Dispose()
    {
        _chunkHandlers.Clear();
        _progressHandlers.Clear();

        // Anything still awaiting a stream is waiting on a page that is going away; faulting the
        // task is what stops that await from hanging forever.
        foreach (var id in _completions.Keys)
        {
            if (_completions.TryRemove(id, out var completion))
                completion.TrySetException(new ObjectDisposedException(nameof(AiInterop), "The AI service was disposed while a stream was running."));
        }

        _dotNetRef?.Dispose();
        _dotNetRef = null;
    }
}
