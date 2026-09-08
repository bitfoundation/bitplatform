using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps <see href="https://developer.mozilla.org/en-US/docs/Web/URI/Fragment/Text_fragments">text fragments</see>:
/// URLs that scroll to and highlight a <b>phrase</b> rather than an anchor - <c>#:~:text=…</c>.
/// </summary>
/// <remarks>
/// The point is deep-linking into content you don't control the markup of: no <c>id</c> has to
/// exist, and the link keeps working when the page is re-flowed, as long as the words survive.
/// This is what a browser's "Copy link to highlight" produces.
/// <br/>
/// <b>How the browser treats it:</b> everything after <c>:~:</c> is a <i>fragment directive</i>, and
/// the browser strips it off the URL while navigating - so it is gone from <c>location</c> before any
/// page script runs, and <see cref="Parse"/> reads a URL you still hold rather than the current one.
/// A directive is only acted on during a navigation, so <see cref="Navigate"/> is what makes the
/// browser scroll; writing the URL into history does nothing.
/// <br/>
/// Style the result with the <c>::target-text</c> pseudo-element. Chromium and Safari implement
/// this; Firefox does not, and there the link simply loads the page without scrolling.
/// </remarks>
[ButilService(typeof(TextFragment))]
public class TextFragment(IJSRuntime js)
{
    /// <summary>True when the runtime exposes <c>document.fragmentDirective</c>.</summary>
    /// <remarks>
    /// A false here doesn't make the links unusable - they just load the page without scrolling, so
    /// this is a "will the highlight happen" check rather than a gate on generating them.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.textFragment.isSupported");

    /// <summary>
    /// Encodes directives into the fragment string, <c>:~:text=…</c>, with the escaping the spec
    /// requires.
    /// </summary>
    /// <returns>The fragment, or an empty string when no directive had a <see cref="TextFragmentDirective.Start"/>.</returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(TextFragmentDirective))]
    public ValueTask<string> Build(params TextFragmentDirective[] directives)
        => js.Invoke<string>("BitButil.textFragment.build", (object)directives);

    /// <summary>
    /// Appends directives to a URL, replacing any it already carries and keeping an ordinary
    /// <c>#anchor</c> in front of them.
    /// </summary>
    /// <param name="url">The URL to link to.</param>
    /// <param name="directives">What to highlight. More than one highlights more than one place.</param>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(TextFragmentDirective))]
    public ValueTask<string> BuildUrl(string url, params TextFragmentDirective[] directives)
        => js.Invoke<string>("BitButil.textFragment.buildUrl", url, directives);

    /// <summary>
    /// Reads the directives out of a URL - the inverse of <see cref="BuildUrl"/>, for inspecting or
    /// rewriting a highlight link before navigating to it.
    /// </summary>
    /// <param name="url">A URL that may carry a <c>:~:text=…</c> fragment directive.</param>
    /// <returns>The directives, or an empty array when the URL carries none.</returns>
    /// <remarks>
    /// This takes a URL rather than reading the current one because the current one no longer has a
    /// directive to read: the browser strips the fragment directive off while navigating, so it is
    /// gone from <c>location.hash</c> <i>and</i> from <c>location.href</c> by the time any script
    /// runs, and <c>document.fragmentDirective</c> deliberately exposes nothing. To know what a
    /// visitor was linked to, pass the URL through your own app - a query parameter, say - or parse
    /// the link before you navigate to it.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(TextFragmentDirective))]
    public ValueTask<TextFragmentDirective[]> Parse(string url)
        => js.Invoke<TextFragmentDirective[]>("BitButil.textFragment.parse", url);

    /// <summary>
    /// Turns the user's current selection into a directive - the "copy link to this passage" button.
    /// </summary>
    /// <returns>The directive, or null when nothing is selected.</returns>
    /// <remarks>
    /// A short selection becomes an exact match; a long one becomes a start/end range, so the URL
    /// stays a reasonable length without matching any less precisely.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(TextFragmentDirective))]
    public ValueTask<TextFragmentDirective?> FromSelection()
        => js.Invoke<TextFragmentDirective?>("BitButil.textFragment.fromSelection");

    /// <summary>
    /// Navigates to a URL so the browser acts on its text directive.
    /// </summary>
    /// <param name="url">A URL built by <see cref="BuildUrl"/>.</param>
    /// <param name="replace">Replace the current history entry instead of pushing a new one.</param>
    /// <remarks>
    /// This is a real navigation - in a Blazor app that means a full reload unless the URL is the
    /// current page with a different fragment. Scroll-to-text has no scripted equivalent: the
    /// browser only performs it as part of navigating.
    /// </remarks>
    public ValueTask Navigate(string url, bool replace = false)
        => js.InvokeVoid("BitButil.textFragment.navigate", url, replace);
}
