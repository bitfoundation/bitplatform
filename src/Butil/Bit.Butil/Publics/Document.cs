using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

public class Document(IJSRuntime js)
{
    private const string ElementName = "document";

    public async Task AddEventListener<T>(
        string domEvent,
        Action<T> listener,
        bool useCapture = false,
        bool preventDefault = false,
        bool stopPropagation = false)
        => await DomEventDispatcher.AddEventListener(js, ElementName, domEvent, listener, useCapture, preventDefault, stopPropagation);

    public async Task RemoveEventListener<T>(string domEvent, Action<T> listener, bool useCapture = false)
        => await DomEventDispatcher.RemoveEventListener(js, ElementName, domEvent, listener, useCapture);

    /// <summary>
    /// Returns the character set being used by the document.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document/characterSet">https://developer.mozilla.org/en-US/docs/Web/API/Document/characterSet</see>
    /// </summary>
    public async Task<string> GetCharacterSet()
        => await js.FastInvokeAsync<string>("BitButil.document.characterSet");

    /// <summary>
    /// Indicates whether the document is rendered in quirks or strict mode.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document/compatMode">https://developer.mozilla.org/en-US/docs/Web/API/Document/compatMode</see>
    /// </summary>
    public async Task<CompatMode> GetCompatMode()
    {
        var mode = await js.FastInvokeAsync<string>("BitButil.document.compatMode");
        return mode switch
        {
            "BackCompat" => CompatMode.BackCompat,
            _ => CompatMode.CSS1Compat
        };
    }

    /// <summary>
    /// Returns the Content-Type from the MIME Header of the current document.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document/contentType">https://developer.mozilla.org/en-US/docs/Web/API/Document/contentType</see>
    /// </summary>
    public async Task<string> GetContentType()
        => await js.FastInvokeAsync<string>("BitButil.document.contentType");

    /// <summary>
    /// Returns the document location as a string.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document/documentURI">https://developer.mozilla.org/en-US/docs/Web/API/Document/documentURI</see>
    /// </summary>
    public async Task<string> GetDocumentURI()
        => await js.FastInvokeAsync<string>("BitButil.document.documentURI");

    /// <summary>
    /// Gets ability to edit the whole document.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document/designMode">https://developer.mozilla.org/en-US/docs/Web/API/Document/designMode</see>
    /// </summary>
    public async Task<DesignMode> GetDesignMode()
    {
        var mode = await js.FastInvokeAsync<string>("BitButil.document.getDesignMode");
        return mode switch
        {
            "on" => DesignMode.On,
            _ => DesignMode.Off
        };
    }
    /// <summary>
    /// Sets ability to edit the whole document.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document/designMode">https://developer.mozilla.org/en-US/docs/Web/API/Document/designMode</see>
    /// </summary>
    public async Task SetDesignMode(DesignMode mode)
        => await js.FastInvokeVoidAsync("BitButil.document.setDesignMode", mode.ToString());

    /// <summary>
    /// Gets directionality (rtl/ltr) of the document.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document/dir">https://developer.mozilla.org/en-US/docs/Web/API/Document/dir</see>
    /// </summary>
    public async Task<DocumentDir> GetDir()
    {
        var mode = await js.FastInvokeAsync<string>("BitButil.document.getDir");
        return mode switch
        {
            "rtl" => DocumentDir.Rtl,
            _ => DocumentDir.Ltr
        };
    }
    /// <summary>
    /// Sets directionality (rtl/ltr) of the document.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document/dir">https://developer.mozilla.org/en-US/docs/Web/API/Document/dir</see>
    /// </summary>
    public async Task SetDir(DocumentDir dir)
        => await js.FastInvokeVoidAsync("BitButil.document.setDir", dir.ToString());

    /// <summary>
    /// Returns the URI of the page that linked to this page.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document/referrer">https://developer.mozilla.org/en-US/docs/Web/API/Document/referrer</see>
    /// </summary>
    public async Task<string> GetReferrer()
        => await js.FastInvokeAsync<string>("BitButil.document.referrer");

    /// <summary>
    /// Gets the title of the current document.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document/title">https://developer.mozilla.org/en-US/docs/Web/API/Document/title</see>
    /// </summary>
    public async Task<string> GetTitle()
        => await js.FastInvokeAsync<string>("BitButil.document.getTitle");
    /// <summary>
    /// Sets the title of the current document.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document/title">https://developer.mozilla.org/en-US/docs/Web/API/Document/title</see>
    /// </summary>
    public async Task SetTitle(string title)
        => await js.FastInvokeVoidAsync("BitButil.document.setTitle", title);

    /// <summary>
    /// Returns the document location as a string.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document/URL">https://developer.mozilla.org/en-US/docs/Web/API/Document/URL</see>
    /// </summary>
    public async Task<string> GetUrl()
        => await js.FastInvokeAsync<string>("BitButil.document.URL");

    /// <summary>
    /// Stops document's fullscreen element from being displayed fullscreen.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document/exitFullscreen">https://developer.mozilla.org/en-US/docs/Web/API/Document/exitFullscreen</see>
    /// </summary>
    public async Task ExitFullscreen()
        => await js.FastInvokeVoidAsync("BitButil.document.exitFullscreen");

    /// <summary>
    /// Release the pointer lock.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document/exitPointerLock">https://developer.mozilla.org/en-US/docs/Web/API/Document/exitPointerLock</see>
    /// </summary>
    public async Task ExitPointerLock()
        => await js.FastInvokeVoidAsync("BitButil.document.exitPointerLock");
}
