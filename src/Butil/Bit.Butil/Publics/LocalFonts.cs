using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Local_Font_Access_API">Local Font Access API</see>
/// (<c>queryLocalFonts</c>): list the fonts installed on the user's machine, and read the raw font
/// file behind one of them.
/// </summary>
/// <remarks>
/// Chromium only, and only over HTTPS. The first query prompts for the <c>local-fonts</c>
/// permission and so must run inside a user gesture; a dismissed prompt yields an empty list rather
/// than an error.
/// <br/>
/// The point of the API is a design tool whose font picker matches the desktop app's - and, with
/// <see cref="GetData"/>, one that can embed the picked face in what it exports.
/// </remarks>
[ButilService(typeof(LocalFonts))]
public class LocalFonts(IJSRuntime js)
{
    /// <summary>True when the runtime exposes <c>window.queryLocalFonts</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.localFonts.isSupported");

    /// <summary>
    /// The installed fonts, or the subset named by <paramref name="postscriptNames"/>. Empty when
    /// the API is missing or the user dismissed the prompt - the two are indistinguishable on
    /// purpose, so a refusal cannot be detected as such.
    /// </summary>
    /// <param name="postscriptNames">
    /// PostScript names to filter by. Pass none for every installed face, which on a designer's
    /// machine can be thousands.
    /// </param>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(LocalFont))]
    public ValueTask<LocalFont[]> Query(params string[] postscriptNames)
        => js.Invoke<LocalFont[]>("BitButil.localFonts.query", (object)postscriptNames);

    /// <summary>
    /// The raw SFNT bytes of one font file, by PostScript name - what an exporter needs in order to
    /// embed the face. Null when the font is not installed or permission was refused.
    /// </summary>
    /// <remarks>
    /// A font file is routinely several megabytes, so fetch one only for a face the user actually
    /// chose, never for a whole list.
    /// </remarks>
    public ValueTask<byte[]?> GetData(string postscriptName)
        => js.Invoke<byte[]?>("BitButil.localFonts.getData", postscriptName);
}
