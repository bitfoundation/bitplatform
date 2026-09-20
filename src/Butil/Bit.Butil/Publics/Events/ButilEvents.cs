namespace Bit.Butil;

/// <summary>
/// The DOM event names <c>AddEventListener</c> takes, as constants, so a typo is a compile error
/// rather than a listener that never fires. Nothing here is exhaustive - any event name the browser
/// knows works just as well as a plain string; these are the ones worth not misspelling.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/Events">Event reference</see>
/// </summary>
public class ButilEvents
{
    // ─── Mouse ────────────────────────────────────────────────────────────

    /// <summary><c>click</c> - a primary-button press and release on the same element.</summary>
    public const string Click = "click";

    /// <summary><c>dblclick</c> - two clicks close enough together to count as one gesture.</summary>
    public const string DblClick = "dblclick";

    /// <summary><c>mousedown</c> - a button was pressed over the element.</summary>
    public const string MouseDown = "mousedown";

    /// <summary><c>mouseup</c> - a button was released over the element.</summary>
    public const string MouseUp = "mouseup";

    /// <summary><c>mousemove</c> - the pointer moved while over the element. Fires very often; throttle the handler.</summary>
    public const string MouseMove = "mousemove";

    /// <summary><c>mouseenter</c> - the pointer entered the element. Does not bubble, and ignores movement between children.</summary>
    public const string MouseEnter = "mouseenter";

    /// <summary><c>mouseleave</c> - the pointer left the element. Does not bubble, and ignores movement between children.</summary>
    public const string MouseLeave = "mouseleave";

    /// <summary><c>mouseover</c> - the pointer entered the element or one of its descendants. Bubbles.</summary>
    public const string MouseOver = "mouseover";

    /// <summary><c>mouseout</c> - the pointer left the element or one of its descendants. Bubbles.</summary>
    public const string MouseOut = "mouseout";

    /// <summary><c>contextmenu</c> - the context menu was asked for. Cancel it to substitute your own.</summary>
    public const string ContextMenu = "contextmenu";

    // ─── Keyboard ─────────────────────────────────────────────────────────

    /// <summary><c>keydown</c> - a key went down. Repeats while the key is held.</summary>
    public const string KeyDown = "keydown";

    /// <summary><c>keyup</c> - a key came back up.</summary>
    public const string KeyUp = "keyup";

    /// <summary><c>keypress</c> - deprecated, and never fired for non-printing keys. Use <see cref="KeyDown"/>.</summary>
    public const string KeyPress = "keypress";

    // ─── Pointer ──────────────────────────────────────────────────────────

    /// <summary><c>pointerdown</c> - a pointer (mouse, pen or touch) went down.</summary>
    public const string PointerDown = "pointerdown";

    /// <summary><c>pointerup</c> - a pointer came back up.</summary>
    public const string PointerUp = "pointerup";

    /// <summary><c>pointermove</c> - a pointer moved. Fires very often; throttle the handler.</summary>
    public const string PointerMove = "pointermove";

    /// <summary><c>pointerenter</c> - a pointer entered the element. Does not bubble.</summary>
    public const string PointerEnter = "pointerenter";

    /// <summary><c>pointerleave</c> - a pointer left the element. Does not bubble.</summary>
    public const string PointerLeave = "pointerleave";

    /// <summary><c>pointerover</c> - a pointer entered the element or a descendant. Bubbles.</summary>
    public const string PointerOver = "pointerover";

    /// <summary><c>pointerout</c> - a pointer left the element or a descendant. Bubbles.</summary>
    public const string PointerOut = "pointerout";

    /// <summary><c>pointercancel</c> - the browser took the pointer over, e.g. to start a scroll. No up event follows.</summary>
    public const string PointerCancel = "pointercancel";

    /// <summary><c>gotpointercapture</c> - this element now receives every event from that pointer.</summary>
    public const string GotPointerCapture = "gotpointercapture";

    /// <summary><c>lostpointercapture</c> - the capture ended, whether released or taken away.</summary>
    public const string LostPointerCapture = "lostpointercapture";

    // ─── Touch ────────────────────────────────────────────────────────────

    /// <summary><c>touchstart</c> - a finger touched the surface.</summary>
    public const string TouchStart = "touchstart";

    /// <summary><c>touchend</c> - a finger left the surface.</summary>
    public const string TouchEnd = "touchend";

    /// <summary><c>touchmove</c> - a touching finger moved. Fires very often; throttle the handler.</summary>
    public const string TouchMove = "touchmove";

    /// <summary><c>touchcancel</c> - the browser took the touch over, e.g. to scroll.</summary>
    public const string TouchCancel = "touchcancel";

    // ─── Wheel / scroll ───────────────────────────────────────────────────

    /// <summary><c>wheel</c> - a wheel or trackpad scroll gesture, before any scrolling happens.</summary>
    public const string Wheel = "wheel";

    /// <summary><c>scroll</c> - the scroll position changed. Does not bubble from an element, though it does from the document.</summary>
    public const string Scroll = "scroll";

    // ─── Focus ────────────────────────────────────────────────────────────

    /// <summary><c>focus</c> - the element gained focus. Does not bubble; use <see cref="FocusIn"/> for a delegated handler.</summary>
    public const string Focus = "focus";

    /// <summary><c>focusin</c> - the element or a descendant gained focus. Bubbles.</summary>
    public const string FocusIn = "focusin";

    /// <summary><c>blur</c> - the element lost focus. Does not bubble; use <see cref="FocusOut"/> for a delegated handler.</summary>
    public const string Blur = "blur";

    /// <summary><c>focusout</c> - the element or a descendant lost focus. Bubbles.</summary>
    public const string FocusOut = "focusout";

    // ─── Input ────────────────────────────────────────────────────────────

    /// <summary><c>input</c> - the value changed, on every keystroke.</summary>
    public const string Input = "input";

    /// <summary><c>change</c> - the value changed and was committed: on blur for a text field, immediately for a checkbox or a select.</summary>
    public const string Change = "change";

    /// <summary><c>submit</c> - a form is being submitted. Cancel it to take over.</summary>
    public const string Submit = "submit";

    /// <summary><c>reset</c> - a form is being reset.</summary>
    public const string Reset = "reset";

    /// <summary><c>beforeinput</c> - the value is about to change. Cancellable, unlike <see cref="Input"/>.</summary>
    public const string BeforeInput = "beforeinput";

    // ─── Drag and drop ────────────────────────────────────────────────────

    /// <summary><c>dragstart</c> - a drag began on this element.</summary>
    public const string DragStart = "dragstart";

    /// <summary><c>drag</c> - the drag is in progress, fired repeatedly on the source.</summary>
    public const string Drag = "drag";

    /// <summary><c>dragend</c> - the drag finished, whether it was dropped or abandoned.</summary>
    public const string DragEnd = "dragend";

    /// <summary><c>dragenter</c> - a drag entered this element, a possible drop target.</summary>
    public const string DragEnter = "dragenter";

    /// <summary><c>dragleave</c> - a drag left this element.</summary>
    public const string DragLeave = "dragleave";

    /// <summary><c>dragover</c> - a drag is over this element. Cancel it, or the drop never happens.</summary>
    public const string DragOver = "dragover";

    /// <summary><c>drop</c> - a drag was released over this element.</summary>
    public const string Drop = "drop";

    // ─── Clipboard ────────────────────────────────────────────────────────

    /// <summary><c>copy</c> - the selection is being copied.</summary>
    public const string Copy = "copy";

    /// <summary><c>cut</c> - the selection is being cut.</summary>
    public const string Cut = "cut";

    /// <summary><c>paste</c> - clipboard content is being pasted in.</summary>
    public const string Paste = "paste";

    // ─── Composition ──────────────────────────────────────────────────────

    /// <summary><c>compositionstart</c> - an IME composition session began.</summary>
    public const string CompositionStart = "compositionstart";

    /// <summary><c>compositionupdate</c> - the text being composed changed.</summary>
    public const string CompositionUpdate = "compositionupdate";

    /// <summary><c>compositionend</c> - the composition was committed or abandoned.</summary>
    public const string CompositionEnd = "compositionend";

    // ─── Window-only ──────────────────────────────────────────────────────

    /// <summary><c>resize</c> - the window was resized. Fires on the window, not on an element.</summary>
    public const string Resize = "resize";

    /// <summary><c>online</c> - the browser believes it regained connectivity. It only knows about the local link, not about reachability.</summary>
    public const string Online = "online";

    /// <summary><c>offline</c> - the browser believes it lost connectivity.</summary>
    public const string Offline = "offline";

    /// <summary><c>hashchange</c> - the URL fragment changed.</summary>
    public const string HashChange = "hashchange";

    /// <summary><c>languagechange</c> - the preferred languages changed.</summary>
    public const string LanguageChange = "languagechange";

    /// <summary><c>load</c> - the page and all of its subresources finished loading.</summary>
    public const string Load = "load";

    /// <summary>
    /// <c>unload</c> - deprecated, and unreliable on mobile, where a backgrounded page is often
    /// killed without it ever firing. Use <see cref="VisibilityChange"/> to persist state.
    /// </summary>
    public const string Unload = "unload";

    // ─── Document-level visibility / fullscreen ───────────────────────────

    /// <summary><c>visibilitychange</c> - the document became hidden or visible. The reliable place to persist state.</summary>
    public const string VisibilityChange = "visibilitychange";

    /// <summary><c>fullscreenchange</c> - the document entered or left fullscreen.</summary>
    public const string FullscreenChange = "fullscreenchange";

    /// <summary><c>fullscreenerror</c> - a fullscreen request was refused.</summary>
    public const string FullscreenError = "fullscreenerror";

    /// <summary><c>pointerlockchange</c> - the pointer was locked to an element, or released.</summary>
    public const string PointerLockChange = "pointerlockchange";

    /// <summary><c>pointerlockerror</c> - a pointer-lock request was refused.</summary>
    public const string PointerLockError = "pointerlockerror";

    /// <summary><c>DOMContentLoaded</c> - the HTML is parsed. Note the capitalisation: it is the one event name here that is not lower-case.</summary>
    public const string DomContentLoaded = "DOMContentLoaded";
}
