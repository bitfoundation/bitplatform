using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/SpeechRecognition">SpeechRecognition</see>
/// API (Web Speech, prefixed as <c>webkitSpeechRecognition</c> on Chromium).
/// </summary>
public class SpeechRecognition(IJSRuntime js)
{
    /// <summary>True when the runtime exposes a SpeechRecognition implementation.</summary>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.speechRecognition.isSupported");

    /// <summary>
    /// Starts recognition. Returns an <see cref="IAsyncDisposable"/> that calls <see cref="Stop"/> when disposed.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(SpeechRecognitionResult))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(SpeechRecognitionOptions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(SpeechRecognitionListenersManager))]
    public async Task<IAsyncDisposable> Start(SpeechRecognitionOptions options,
                                              Action<SpeechRecognitionResult>? onResult = null,
                                              Action<string>? onError = null,
                                              Action? onEnd = null)
    {
        if (onResult is null && onError is null && onEnd is null)
            throw new ArgumentException("At least one of onResult/onError/onEnd must be provided.");

        var listener = new SpeechRecognitionListenersManager.Listener
        {
            OnResult = onResult,
            OnError = onError,
            OnEnd = onEnd
        };
        var id = SpeechRecognitionListenersManager.Add(listener);

        await js.InvokeVoid("BitButil.speechRecognition.start",
            id,
            options ?? new SpeechRecognitionOptions(),
            SpeechRecognitionListenersManager.ResultMethodName,
            SpeechRecognitionListenersManager.ErrorMethodName,
            SpeechRecognitionListenersManager.EndMethodName);

        return new RecognitionHandle(js, id);
    }

    /// <summary>Stops the matching recognition session early. Equivalent to disposing the handle.</summary>
    public ValueTask Stop(Guid id) => js.InvokeVoid("BitButil.speechRecognition.stop", id);

    private sealed class RecognitionHandle(IJSRuntime js, Guid id) : IAsyncDisposable
    {
        private bool _disposed;

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;
            SpeechRecognitionListenersManager.Remove(id);
            try { await js.InvokeVoid("BitButil.speechRecognition.stop", id); }
            catch (JSDisconnectedException) { }
        }
    }
}
