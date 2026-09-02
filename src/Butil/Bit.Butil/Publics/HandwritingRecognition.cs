using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Handwriting_Recognition_API">Handwriting Recognition API</see>:
/// turns strokes drawn with a finger, a stylus or a mouse into text, using a model already on the
/// device.
/// </summary>
/// <remarks>
/// The point is that nothing leaves the machine and nothing has to be downloaded: a signature pad, a
/// stylus note field or a form filled in on a tablet gets recognized locally, offline.
/// <br/>
/// Collect the strokes yourself - from pointer events on a canvas - and hand them over as a whole:
/// there is no long-lived recognizer to keep here, because by the time .NET asks for a result every
/// stroke is already known. The platform resources the call opens are released before it returns.
/// <br/>
/// Niche and Chromium-only, and even there it depends on the operating system shipping a model for
/// the language. <see cref="QuerySupport"/> is the check that matters - <see cref="IsSupported"/>
/// only says the API exists.
/// </remarks>
[ButilService(typeof(HandwritingRecognition))]
public class HandwritingRecognition(IJSRuntime js)
{
    /// <summary>True when the runtime exposes <c>navigator.createHandwritingRecognizer</c>.</summary>
    /// <remarks>
    /// Says nothing about whether a model for your language is installed - see
    /// <see cref="QuerySupport"/>.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.handwritingRecognition.isSupported");

    /// <summary>
    /// Whether this device can recognize these languages, and whether it implements the optional
    /// hints.
    /// </summary>
    /// <param name="languages">BCP-47 tags, e.g. <c>["en"]</c>, <c>["zh-CN"]</c>. Defaults to <c>["en"]</c>.</param>
    /// <param name="alternatives">Ask whether more than one candidate can be returned.</param>
    /// <param name="textContext">Ask whether preceding text can inform the recognition.</param>
    /// <returns>What is supported, or null when the runtime has no handwriting recognition at all.</returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(HandwritingSupport))]
    public ValueTask<HandwritingSupport?> QuerySupport(string[]? languages = null, bool alternatives = true, bool textContext = false)
        => js.Invoke<HandwritingSupport?>("BitButil.handwritingRecognition.querySupport", languages ?? ["en"], alternatives, textContext);

    /// <summary>
    /// Recognizes text from strokes.
    /// </summary>
    /// <param name="strokes">
    /// The strokes, in the order they were drawn. Stroke boundaries carry meaning, so keep one stroke
    /// per pointer-down to pointer-up rather than merging them.
    /// </param>
    /// <param name="languages">BCP-47 tags, e.g. <c>["en"]</c>. Defaults to <c>["en"]</c>.</param>
    /// <param name="recognitionType">
    /// What kind of content this is: <c>"text"</c>, <c>"email"</c>, <c>"number"</c>, <c>"per-character"</c>.
    /// A hint, not a filter - it biases the model rather than restricting the output.
    /// </param>
    /// <param name="inputType">How it was drawn: <c>"mouse"</c>, <c>"stylus"</c> or <c>"touch"</c>.</param>
    /// <param name="textContext">Text that precedes what is being written, where that helps the model.</param>
    /// <param name="alternatives">How many candidates to ask for, best first.</param>
    /// <returns>
    /// The candidates, best first, or an empty array - when there is no model for the language, when
    /// the feature is off, or when there is simply nothing readable in the strokes.
    /// </returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(HandwritingStroke))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(HandwritingPoint))]
    public ValueTask<string[]> Recognize(HandwritingStroke[] strokes,
                                          string[]? languages = null,
                                          string recognitionType = "text",
                                          string inputType = "mouse",
                                          string? textContext = null,
                                          int alternatives = 3)
    {
        ArgumentNullException.ThrowIfNull(strokes);

        return js.Invoke<string[]>("BitButil.handwritingRecognition.recognize",
            strokes, languages ?? ["en"], recognitionType, inputType, textContext, alternatives);
    }
}
