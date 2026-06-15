using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/SpeechRecognition">SpeechRecognition</see>
/// API (Web Speech, prefixed as <c>webkitSpeechRecognition</c> on Chromium).
/// </summary>
public class SpeechRecognition(IJSRuntime js) : IAsyncDisposable
{
    internal const string ResultMethodName = nameof(InvokeSpeechRecognitionResult);
    internal const string ErrorMethodName = nameof(InvokeSpeechRecognitionError);
    internal const string EndMethodName = nameof(InvokeSpeechRecognitionEnd);

    private readonly ConcurrentDictionary<Guid, Listener> _listeners = new();

    // Per-instance callback reference (see Keyboard): sessions are isolated per circuit / WASM app
    // and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<SpeechRecognition>? _dotNetRef;
    private DotNetObjectReference<SpeechRecognition> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>True when the runtime exposes a SpeechRecognition implementation.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.speechRecognition.isSupported");

    /// <summary>
    /// Invoked from JS for each recognition result. Public + <see cref="JSInvokableAttribute"/> so it
    /// can be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(ResultMethodName)]
    public void InvokeSpeechRecognitionResult(Guid id, SpeechRecognitionResult result)
    {
        if (_listeners.TryGetValue(id, out var l)) l.OnResult?.Invoke(result);
    }

    /// <summary>Invoked from JS on a recognition error. See <see cref="InvokeSpeechRecognitionResult"/>.</summary>
    [JSInvokable(ErrorMethodName)]
    public void InvokeSpeechRecognitionError(Guid id, string message)
    {
        if (_listeners.TryGetValue(id, out var l)) l.OnError?.Invoke(message);
    }

    /// <summary>Invoked from JS when recognition ends. See <see cref="InvokeSpeechRecognitionResult"/>.</summary>
    [JSInvokable(EndMethodName)]
    public void InvokeSpeechRecognitionEnd(Guid id)
    {
        if (_listeners.TryGetValue(id, out var l)) l.OnEnd?.Invoke();
    }

    /// <summary>
    /// Starts recognition. Returns an <see cref="IAsyncDisposable"/> that calls <see cref="Stop"/> when disposed.
    /// </summary>
    [DynamicDependency(nameof(InvokeSpeechRecognitionResult), typeof(SpeechRecognition))]
    [DynamicDependency(nameof(InvokeSpeechRecognitionError), typeof(SpeechRecognition))]
    [DynamicDependency(nameof(InvokeSpeechRecognitionEnd), typeof(SpeechRecognition))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(SpeechRecognitionResult))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(SpeechRecognitionOptions))]
    public async Task<IAsyncDisposable> Start(SpeechRecognitionOptions options,
                                              Action<SpeechRecognitionResult>? onResult = null,
                                              Action<string>? onError = null,
                                              Action? onEnd = null)
    {
        if (onResult is null && onError is null && onEnd is null)
            throw new ArgumentException("At least one of onResult/onError/onEnd must be provided.");

        var id = Guid.NewGuid();
        _listeners.TryAdd(id, new Listener { OnResult = onResult, OnError = onError, OnEnd = onEnd });

        await js.InvokeVoid("BitButil.speechRecognition.start",
            id,
            options ?? new SpeechRecognitionOptions(),
            DotNetRef);

        return new RecognitionHandle(this, js, id);
    }

    /// <summary>Stops the matching recognition session early. Equivalent to disposing the handle.</summary>
    public ValueTask Stop(Guid id)
    {
        _listeners.TryRemove(id, out _);
        return js.InvokeVoid("BitButil.speechRecognition.stop", id);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            var ids = _listeners.Keys.ToArray();
            _listeners.Clear();
            foreach (var id in ids)
            {
                await js.InvokeVoid("BitButil.speechRecognition.stop", id);
            }
        }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
        finally
        {
            _dotNetRef?.Dispose();
            _dotNetRef = null;
        }
        GC.SuppressFinalize(this);
    }

    private class Listener
    {
        public Action<SpeechRecognitionResult>? OnResult { get; set; }
        public Action<string>? OnError { get; set; }
        public Action? OnEnd { get; set; }
    }

    private sealed class RecognitionHandle(SpeechRecognition owner, IJSRuntime js, Guid id) : IAsyncDisposable
    {
        private bool _disposed;

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;
            owner._listeners.TryRemove(id, out _);
            try { await js.InvokeVoid("BitButil.speechRecognition.stop", id); }
            catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
        }
    }
}
