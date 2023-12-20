﻿using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// The Window interface represents a window containing a DOM document; 
/// the document property points to the DOM document loaded in that window.
/// <br />
/// More info: <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window">https://developer.mozilla.org/en-US/docs/Web/API/Window</see>
/// </summary>
public class Window(IJSRuntime js)
{
    private const string ElementName = "window";

    public async Task AddEventListener<T>(string domEvent, Action<T> listener, bool useCapture = false)
        => await DomEventDispatcher.AddEventListener(js, ElementName, domEvent, listener, useCapture);

    public async Task RemoveEventListener<T>(string domEvent, Action<T> listener, bool useCapture = false)
        => await DomEventDispatcher.RemoveEventListener(js, ElementName, domEvent, listener, useCapture);

    /// <summary>
    /// The beforeunload event is fired when the current window, contained document, and associated resources are about to be unloaded. 
    /// The document is still visible and the event is still cancelable at this point.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/beforeunload_event">https://developer.mozilla.org/en-US/docs/Web/API/Window/beforeunload_event</see>
    /// </summary>
    public async Task AddBeforeUnload()
        => await js.WindowAddBeforeUnload();

    /// <summary>
    /// The beforeunload event is fired when the current window, contained document, and associated resources are about to be unloaded. 
    /// The document is still visible and the event is still cancelable at this point.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/beforeunload_event">https://developer.mozilla.org/en-US/docs/Web/API/Window/beforeunload_event</see>
    /// </summary>
    public async Task RemoveBeforeUnload()
        => await js.WindowRemoveBeforeUnload();

    /// <summary>
    /// Gets the height of the content area of the browser window in px including, if rendered, the horizontal scrollbar.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/innerHeight">https://developer.mozilla.org/en-US/docs/Web/API/Window/innerHeight</see>
    /// </summary>
    public async Task<float> GetInnerHeight()
        => await js.WindowGetInnerHeight();

    /// <summary>
    /// Gets the width of the content area of the browser window in px including, if rendered, the vertical scrollbar.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/innerWidth">https://developer.mozilla.org/en-US/docs/Web/API/Window/innerWidth</see>
    /// </summary>
    public async Task<float> GetInnerWidth()
        => await js.WindowGetInnerWidth();

    /// <summary>
    /// Returns a boolean indicating whether the current context is secure (true) or not (false).
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/isSecureContext">https://developer.mozilla.org/en-US/docs/Web/API/isSecureContext</see>
    /// </summary>
    public async Task<bool> IsSecureContext()
        => await js.WindowIsSecureContext();

    /// <summary>
    /// Returns the locationbar object. For privacy and interoperability reasons, 
    /// the value of the visible property is now false if this Window is a popup, and true otherwise.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/locationbar">https://developer.mozilla.org/en-US/docs/Web/API/Window/locationbar</see>
    /// </summary>
    public async Task<BarProp> GetLocationBar()
        => await js.WindowLocationBar();

    /// <summary>
    /// Gets the name of the window.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/name">https://developer.mozilla.org/en-US/docs/Web/API/Window/name</see>
    /// </summary>
    public async Task<string> GetName()
        => await js.WindowGetName();
    /// <summary>
    /// Sets the name of the window.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/name">https://developer.mozilla.org/en-US/docs/Web/API/Window/name</see>
    /// </summary>
    public async Task SetName(string value)
        => await js.WindowSetName(value);

    /// <summary>
    /// Returns the global object's origin, serialized as a string.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/origin">https://developer.mozilla.org/en-US/docs/Web/API/origin</see>
    /// </summary>
    public async Task<string> GetOrigin()
        => await js.WindowGetOrigin();

    /// <summary>
    /// Gets the height of the outside of the browser window in px.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/outerHeight">https://developer.mozilla.org/en-US/docs/Web/API/Window/outerHeight</see>
    /// </summary>
    public async Task<float> GetOuterHeight()
        => await js.WindowGetOuterHeight();

    /// <summary>
    /// Gets the width of the outside of the browser window in px.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/outerWidth">https://developer.mozilla.org/en-US/docs/Web/API/Window/outerWidth</see>
    /// </summary>
    public async Task<float> GetOuterWidth()
        => await js.WindowGetOuterWidth();

    /// <summary>
    /// Returns the horizontal distance in px from the left border of the user's browser viewport to the left side of the screen.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/screenX">https://developer.mozilla.org/en-US/docs/Web/API/Window/screenX</see>
    /// </summary>
    public async Task<float> GetScreenX()
        => await js.WindowGetScreenX();

    /// <summary>
    /// Returns the vertical distance in px from the top border of the user's browser viewport to the top side of the screen.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/screenY">https://developer.mozilla.org/en-US/docs/Web/API/Window/screenY</see>
    /// </summary>
    public async Task<float> GetScreenY()
        => await js.WindowGetScreenY();

    /// <summary>
    /// Returns the number of pixels that the document has already been scrolled horizontally.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/scrollX">https://developer.mozilla.org/en-US/docs/Web/API/Window/scrollX</see>
    /// </summary>
    public async Task<float> GetScrollX()
        => await js.WindowGetScrollX();

    /// <summary>
    /// Returns the number of pixels that the document has already been scrolled vertically.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/scrollY">https://developer.mozilla.org/en-US/docs/Web/API/Window/scrollY</see>
    /// </summary>
    public async Task<float> GetScrollY()
        => await js.WindowGetScrollY();

    /// <summary>
    /// Decodes a string of data which has been encoded using base-64 encoding.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/atob">https://developer.mozilla.org/en-US/docs/Web/API/atob</see>
    /// </summary>
    public async Task<string> Atob(string data)
        => await js.WindowAtob(data);

    /// <summary>
    /// Displays an alert dialog.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/alert">https://developer.mozilla.org/en-US/docs/Web/API/Window/alert</see>
    /// </summary>
    public async Task Alert(string? message = null)
        => await js.WindowAlert(message);

    /// <summary>
    /// Sets focus away from the window.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/blur">https://developer.mozilla.org/en-US/docs/Web/API/Window/blur</see>
    /// </summary>
    public async Task Blur()
        => await js.WindowBlur();

    /// <summary>
    /// Creates a base-64 encoded ASCII string from a string of binary data.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/btoa">https://developer.mozilla.org/en-US/docs/Web/API/btoa</see>
    /// </summary>
    public async Task<string> Btoa(string data)
        => await js.WindowBtoa(data);

    /// <summary>
    /// Closes the current window.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/close">https://developer.mozilla.org/en-US/docs/Web/API/Window/close</see>
    /// </summary>
    public async Task Close()
        => await js.WindowClose();

    /// <summary>
    /// Displays a dialog with a message that the user needs to respond to.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/confirm">https://developer.mozilla.org/en-US/docs/Web/API/Window/confirm</see>
    /// </summary>
    public async Task<bool> Confirm(string? message = null)
        => await js.WindowConfirm(message);

    /// <summary>
    /// Searches for a given string in a window.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/find">https://developer.mozilla.org/en-US/docs/Web/API/Window/find</see>
    /// </summary>
    public async Task<bool> Find(string? text = null,
        bool? caseSensitive = null,
        bool? backward = null,
        bool? wrapAround = null,
        bool? wholeWord = null,
        bool? searchInFrame = null)
        => await js.WindowFind(text, caseSensitive, backward, wrapAround, wholeWord, searchInFrame);

    /// <summary>
    /// Sets focus on the current window.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/focus">https://developer.mozilla.org/en-US/docs/Web/API/Window/focus</see>
    /// </summary>
    public async Task Focus()
        => await js.WindowFocus();

    /// <summary>
    /// Returns the selection text representing the selected item(s).
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/getSelection">https://developer.mozilla.org/en-US/docs/Web/API/Window/getSelection</see>
    /// </summary>
    public async Task<string> GetSelection()
        => await js.WindowGetSelection();

    /// <summary>
    /// Returns a MediaQueryList object representing the specified media query string.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/matchMedia">https://developer.mozilla.org/en-US/docs/Web/API/Window/matchMedia</see>
    /// </summary>
    public async Task<MediaQueryList> MatchMedia(string query)
        => await js.WindowMatchMedia(query);

    /// <summary>
    /// Opens a new window.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/open">https://developer.mozilla.org/en-US/docs/Web/API/Window/open</see>
    /// </summary>
    public async Task<bool> Open(string? url = null, string? target = null, string? windowFeatures = null)
        => await js.WindowOpen(url, target, windowFeatures);
    /// <summary>
    /// Opens a new window.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/open">https://developer.mozilla.org/en-US/docs/Web/API/Window/open</see>
    /// </summary>
    public async Task<bool> Open(string? url = null, string? target = null, WindowFeatures? windowFeatures = null)
        => await js.WindowOpen(url, target, windowFeatures);

    /// <summary>
    /// Opens the Print Dialog to print the current document.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/print">https://developer.mozilla.org/en-US/docs/Web/API/Window/print</see>
    /// </summary>
    public async Task Print()
        => await js.WindowPrint();

    /// <summary>
    /// Returns the text entered by the user in a prompt dialog.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/prompt">https://developer.mozilla.org/en-US/docs/Web/API/Window/prompt</see>
    /// </summary>
    public async Task<string> Prompt(string? message, string? defaultValue)
        => await js.WindowPrompt(message, defaultValue);

    /// <summary>
    /// Scrolls the window to a particular place in the document.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/scroll">https://developer.mozilla.org/en-US/docs/Web/API/Window/scroll</see>
    /// </summary>
    public async Task Scroll(ScrollToOptions? options)
        => await js.WindowScroll(options, null, null);
    /// <summary>
    /// Scrolls the window to a particular place in the document.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/scroll">https://developer.mozilla.org/en-US/docs/Web/API/Window/scroll</see>
    /// </summary>
    public async Task Scroll(float? x, float? y)
        => await js.WindowScroll(null, x, y);
    /// <summary>
    /// Scrolls the window to a particular place in the document.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/scroll">https://developer.mozilla.org/en-US/docs/Web/API/Window/scroll</see>
    /// </summary>
    public async Task Scroll(ScrollToOptions? options, float? x, float? y)
        => await js.WindowScroll(options, x, y);

    /// <summary>
    /// Scrolls the document in the window by the given amount.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/scrollBy">https://developer.mozilla.org/en-US/docs/Web/API/Window/scrollBy</see>
    /// </summary>
    public async Task ScrollBy(ScrollToOptions? options)
        => await js.WindowScrollBy(options, null, null);
    /// <summary>
    /// Scrolls the document in the window by the given amount.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/scrollBy">https://developer.mozilla.org/en-US/docs/Web/API/Window/scrollBy</see>
    /// </summary>
    public async Task ScrollBy(float? x, float? y)
        => await js.WindowScrollBy(null, x, y);
    /// <summary>
    /// Scrolls the document in the window by the given amount.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/scrollBy">https://developer.mozilla.org/en-US/docs/Web/API/Window/scrollBy</see>
    /// </summary>
    public async Task ScrollBy(ScrollToOptions? options, float? x, float? y)
        => await js.WindowScrollBy(options, x, y);

    /// <summary>
    /// This method stops window loading.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/stop">https://developer.mozilla.org/en-US/docs/Web/API/Window/stop</see>
    /// </summary>
    public async Task Stop()
        => await js.WindowStop();
}
