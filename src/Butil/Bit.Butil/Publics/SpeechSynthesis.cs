using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/SpeechSynthesis">SpeechSynthesis</see>
/// API for text-to-speech.
/// </summary>
public class SpeechSynthesis(IJSRuntime js)
{
    /// <summary>True when the runtime exposes <c>window.speechSynthesis</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.speech.isSupported");

    /// <summary>Returns the list of voices the platform makes available.</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(SpeechVoice))]
    public ValueTask<SpeechVoice[]> GetVoices() => js.Invoke<SpeechVoice[]>("BitButil.speech.getVoices");

    /// <summary>Speaks the configured utterance. Resolves once the engine has accepted it (not when speech finishes).</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(SpeechUtterance))]
    public ValueTask Speak(SpeechUtterance utterance) => js.InvokeVoid("BitButil.speech.speak", utterance);

    /// <summary>Quick-shorthand: speak <paramref name="text"/> with default voice and rate.</summary>
    public ValueTask Speak(string text) => Speak(new SpeechUtterance { Text = text });

    /// <summary>Cancels all pending utterances and stops any current speech.</summary>
    public ValueTask Cancel() => js.InvokeVoid("BitButil.speech.cancel");

    /// <summary>Pauses the current utterance.</summary>
    public ValueTask Pause() => js.InvokeVoid("BitButil.speech.pause");

    /// <summary>Resumes a paused utterance.</summary>
    public ValueTask Resume() => js.InvokeVoid("BitButil.speech.resume");

    /// <summary>True when the engine is currently speaking (or paused).</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSpeaking() => js.Invoke<bool>("BitButil.speech.isSpeaking");

    /// <summary>True when an utterance is queued.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsPending() => js.Invoke<bool>("BitButil.speech.isPending");
}
