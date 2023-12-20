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
        => await js.DocumentGetCharacterSet();

    /// <summary>
    /// Indicates whether the document is rendered in quirks or strict mode.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document/compatMode">https://developer.mozilla.org/en-US/docs/Web/API/Document/compatMode</see>
    /// </summary>
    public async Task<CompatMode> GetCompatMode()
        => await js.DocumentGetCompatMode();

    /// <summary>
    /// Returns the Content-Type from the MIME Header of the current document.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document/contentType">https://developer.mozilla.org/en-US/docs/Web/API/Document/contentType</see>
    /// </summary>
    public async Task<string> GetContentType()
        => await js.DocumentGetContentType();

    /// <summary>
    /// Returns the document location as a string.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document/documentURI">https://developer.mozilla.org/en-US/docs/Web/API/Document/documentURI</see>
    /// </summary>
    public async Task<string> GetDocumentURI()
        => await js.DocumentGetDocumentURI();

    /// <summary>
    /// Gets ability to edit the whole document.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document/designMode">https://developer.mozilla.org/en-US/docs/Web/API/Document/designMode</see>
    /// </summary>
    public async Task<DesignMode> GetDesignMode()
        => await js.DocumentGetDesignMode();
    /// <summary>
    /// Sets ability to edit the whole document.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document/designMode">https://developer.mozilla.org/en-US/docs/Web/API/Document/designMode</see>
    /// </summary>
    public async Task SetDesignMode(DesignMode mode)
        => await js.DocumentSetDesignMode(mode);

    /// <summary>
    /// Gets directionality (rtl/ltr) of the document.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document/dir">https://developer.mozilla.org/en-US/docs/Web/API/Document/dir</see>
    /// </summary>
    public async Task<DocumentDir> GetDir()
        => await js.DocumentGetDir();
    /// <summary>
    /// Sets directionality (rtl/ltr) of the document.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document/dir">https://developer.mozilla.org/en-US/docs/Web/API/Document/dir</see>
    /// </summary>
    public async Task SetDir(DocumentDir dir)
        => await js.DocumentSetDir(dir);

    /// <summary>
    /// Returns the URI of the page that linked to this page.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document/referrer">https://developer.mozilla.org/en-US/docs/Web/API/Document/referrer</see>
    /// </summary>
    public async Task<string> GetReferrer()
        => await js.DocumentGetReferrer();

    /// <summary>
    /// Gets the title of the current document.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document/title">https://developer.mozilla.org/en-US/docs/Web/API/Document/title</see>
    /// </summary>
    public async Task<string> GetTitle()
        => await js.DocumentGetTitle();
    /// <summary>
    /// Sets the title of the current document.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document/title">https://developer.mozilla.org/en-US/docs/Web/API/Document/title</see>
    /// </summary>
    public async Task SetTitle(string title)
        => await js.DocumentSetTitle(title);

    /// <summary>
    /// Returns the document location as a string.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document/URL">https://developer.mozilla.org/en-US/docs/Web/API/Document/URL</see>
    /// </summary>
    public async Task<string> GetUrl()
        => await js.DocumentGetUrl();

    /// <summary>
    /// Stops document's fullscreen element from being displayed fullscreen.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document/exitFullscreen">https://developer.mozilla.org/en-US/docs/Web/API/Document/exitFullscreen</see>
    /// </summary>
    public async Task ExitFullscreen()
        => await js.DocumentExitFullscreen();

    /// <summary>
    /// Release the pointer lock.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document/exitPointerLock">https://developer.mozilla.org/en-US/docs/Web/API/Document/exitPointerLock</see>
    /// </summary>
    public async Task ExitPointerLock()
        => await js.DocumentExitPointerLock();
}
