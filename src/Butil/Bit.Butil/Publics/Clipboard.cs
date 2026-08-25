using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Reads from and writes to the system clipboard - text, and typed items such as images.
/// <br/>
/// More info: <see href="https://developer.mozilla.org/en-US/docs/Web/API/Clipboard">https://developer.mozilla.org/en-US/docs/Web/API/Clipboard</see>
/// </summary>
[ButilService(typeof(Clipboard))]
public class Clipboard(IJSRuntime js)
{
    /// <summary>True when the runtime exposes <c>navigator.clipboard</c>.</summary>
    /// <remarks>
    /// The Clipboard API is secure-context only, so this is <c>false</c> on a plain <c>http://</c>
    /// page even in a browser that implements it - which is the usual reason a copy button "does
    /// nothing" in development.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.clipboard.isSupported");

    /// <summary>
    /// True when the runtime also exposes the item-based half of the API -
    /// <c>read</c>, <c>write</c> and <c>ClipboardItem</c> - which is what
    /// <see cref="Read"/> and <see cref="Write"/> need.
    /// </summary>
    /// <remarks>
    /// Worth a separate check from <see cref="IsSupported"/>: Firefox ships
    /// <see cref="ReadText"/>/<see cref="WriteText"/> without the item-based pair, so a page that
    /// copies images has to gate on this one.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsItemSupported() => js.Invoke<bool>("BitButil.clipboard.isItemSupported");

    /// <summary>
    /// Requests text from the system clipboard, returning a Promise that 
    /// is fulfilled with a string containing the clipboard's text once it's available.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Clipboard/readText">https://developer.mozilla.org/en-US/docs/Web/API/Clipboard/readText</see>
    /// </summary>
    public async ValueTask<string> ReadText()
        => await js.Invoke<string>("BitButil.clipboard.readText");

    /// <summary>
    /// Writes text to the system clipboard, returning a Promise that is 
    /// resolved once the text is fully copied into the clipboard.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Clipboard/writeText">https://developer.mozilla.org/en-US/docs/Web/API/Clipboard/writeText</see>
    /// </summary>
    public async ValueTask WriteText(string text)
    {
        if (text is not null)
        {
            await js.InvokeVoid("BitButil.clipboard.writeText", text);
        }
    }

    /// <summary>
    /// Requests arbitrary data (such as images) from the clipboard, returning a Promise that 
    /// resolves with an array of ClipboardItem objects containing the clipboard's contents.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Clipboard/read">https://developer.mozilla.org/en-US/docs/Web/API/Clipboard/read</see>
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ClipboardItem))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ClipboardFormats))]
    public async ValueTask<ClipboardItem[]> Read(ClipboardFormats? formats = null)
        => await (formats is null ? js.Invoke<ClipboardItem[]>("BitButil.clipboard.read")
                                  : js.Invoke<ClipboardItem[]>("BitButil.clipboard.read", formats));

    /// <summary>
    /// Writes arbitrary data to the system clipboard, returning a Promise 
    /// that resolves when the operation completes.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Clipboard/write">https://developer.mozilla.org/en-US/docs/Web/API/Clipboard/write</see>
    /// </summary>
    public async ValueTask Write(ClipboardItem[] items)
    {
        if (items is not null)
        {
            await js.InvokeVoid("BitButil.clipboard.write", (object)items);
        }
    }
}
