using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

public class Clipboard(IJSRuntime js)
{
    /// <summary>
    /// Requests text from the system clipboard, returning a Promise that 
    /// is fulfilled with a string containing the clipboard's text once it's available.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Clipboard/readText">https://developer.mozilla.org/en-US/docs/Web/API/Clipboard/readText</see>
    /// </summary>
    public async ValueTask<string> ReadText()
        => await js.InvokeAsync<string>("BitButil.clipboard.readText");

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
            await js.InvokeVoidAsync("BitButil.clipboard.writeText", text);
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
        => await (formats is null ? js.InvokeAsync<ClipboardItem[]>("BitButil.clipboard.read")
                                  : js.InvokeAsync<ClipboardItem[]>("BitButil.clipboard.read", formats));

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
            await js.InvokeVoidAsync("BitButil.clipboard.write", (object)items);
        }
    }
}
