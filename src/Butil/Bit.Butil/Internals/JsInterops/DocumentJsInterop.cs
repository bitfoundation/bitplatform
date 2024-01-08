using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

internal static class DocumentJsInterop
{
    internal static async Task<string> DocumentGetCharacterSet(this IJSRuntime js)
        => await js.InvokeAsync<string>("BitButil.document.characterSet");

    internal static async Task<CompatMode> DocumentGetCompatMode(this IJSRuntime js)
    {
        var mode = await js.InvokeAsync<string>("BitButil.document.compatMode");
        return mode switch
        {
            "BackCompat" => CompatMode.BackCompat,
            _ => CompatMode.CSS1Compat
        };
    }

    internal static async Task<string> DocumentGetContentType(this IJSRuntime js)
        => await js.InvokeAsync<string>("BitButil.document.contentType");

    internal static async Task<string> DocumentGetDocumentURI(this IJSRuntime js)
        => await js.InvokeAsync<string>("BitButil.document.documentURI");

    internal static async Task<DesignMode> DocumentGetDesignMode(this IJSRuntime js)
    {
        var mode = await js.InvokeAsync<string>("BitButil.document.getDesignMode");
        return mode switch
        {
            "on" => DesignMode.On,
            _ => DesignMode.Off
        };
    }
    internal static async Task DocumentSetDesignMode(this IJSRuntime js, DesignMode mode)
        => await js.InvokeVoidAsync("BitButil.document.setDesignMode", mode.ToString());

    internal static async Task<DocumentDir> DocumentGetDir(this IJSRuntime js)
    {
        var mode = await js.InvokeAsync<string>("BitButil.document.getDir");
        return mode switch
        {
            "rtl" => DocumentDir.Rtl,
            _ => DocumentDir.Ltr
        };
    }
    internal static async Task DocumentSetDir(this IJSRuntime js, DocumentDir dir)
        => await js.InvokeVoidAsync("BitButil.document.setDir", dir.ToString());

    internal static async Task<string> DocumentGetReferrer(this IJSRuntime js)
        => await js.InvokeAsync<string>("BitButil.document.referrer");

    internal static async Task<string> DocumentGetTitle(this IJSRuntime js)
        => await js.InvokeAsync<string>("BitButil.document.getTitle");
    internal static async Task DocumentSetTitle(this IJSRuntime js, string title)
        => await js.InvokeVoidAsync("BitButil.document.setTitle", title);

    internal static async Task<string> DocumentGetUrl(this IJSRuntime js)
        => await js.InvokeAsync<string>("BitButil.document.URL");

    internal static async Task DocumentExitFullscreen(this IJSRuntime js)
        => await js.InvokeVoidAsync("BitButil.document.exitFullscreen");

    internal static async Task DocumentExitPointerLock(this IJSRuntime js)
        => await js.InvokeVoidAsync("BitButil.document.exitPointerLock");
}
