using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// A language detector, created by <see cref="LanguageDetector.Create"/>.
/// </summary>
/// <remarks>Dispose it when you are done - see <see cref="AiSession"/>.</remarks>
public sealed class LanguageDetectorSession : AiSession
{
    internal LanguageDetectorSession(IJSRuntime js, AiInterop interop, Guid id) : base(js, interop, id) { }

    /// <summary>
    /// Ranks the languages a piece of text might be in, most confident first.
    /// </summary>
    /// <returns>
    /// The candidates, or an empty array once the session has been disposed. A candidate of
    /// <c>"und"</c> is the detector saying it could not decide - short input often lands there.
    /// </returns>
    /// <remarks>
    /// This is the natural front half of <see cref="Translator"/>: detect, then create a translator
    /// from the winning tag to the user's own language.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(LanguageDetectionResult))]
    public ValueTask<LanguageDetectionResult[]> Detect(string input)
        => Js.Invoke<LanguageDetectionResult[]>("BitButil.ai.detect", Id, input);
}
