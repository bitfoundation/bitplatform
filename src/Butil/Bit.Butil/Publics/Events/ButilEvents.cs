namespace Bit.Butil;

public class ButilEvents
{
    // ─── Mouse ────────────────────────────────────────────────────────────
    public const string Click = "click";
    public const string DblClick = "dblclick";
    public const string MouseDown = "mousedown";
    public const string MouseUp = "mouseup";
    public const string MouseMove = "mousemove";
    public const string MouseEnter = "mouseenter";
    public const string MouseLeave = "mouseleave";
    public const string MouseOver = "mouseover";
    public const string MouseOut = "mouseout";
    public const string ContextMenu = "contextmenu";

    // ─── Keyboard ─────────────────────────────────────────────────────────
    public const string KeyDown = "keydown";
    public const string KeyUp = "keyup";
    public const string KeyPress = "keypress";

    // ─── Pointer ──────────────────────────────────────────────────────────
    public const string PointerDown = "pointerdown";
    public const string PointerUp = "pointerup";
    public const string PointerMove = "pointermove";
    public const string PointerEnter = "pointerenter";
    public const string PointerLeave = "pointerleave";
    public const string PointerOver = "pointerover";
    public const string PointerOut = "pointerout";
    public const string PointerCancel = "pointercancel";
    public const string GotPointerCapture = "gotpointercapture";
    public const string LostPointerCapture = "lostpointercapture";

    // ─── Touch ────────────────────────────────────────────────────────────
    public const string TouchStart = "touchstart";
    public const string TouchEnd = "touchend";
    public const string TouchMove = "touchmove";
    public const string TouchCancel = "touchcancel";

    // ─── Wheel / scroll ───────────────────────────────────────────────────
    public const string Wheel = "wheel";
    public const string Scroll = "scroll";

    // ─── Focus ────────────────────────────────────────────────────────────
    public const string Focus = "focus";
    public const string FocusIn = "focusin";
    public const string Blur = "blur";
    public const string FocusOut = "focusout";

    // ─── Input ────────────────────────────────────────────────────────────
    public const string Input = "input";
    public const string Change = "change";
    public const string Submit = "submit";
    public const string Reset = "reset";
    public const string BeforeInput = "beforeinput";

    // ─── Drag & drop ──────────────────────────────────────────────────────
    public const string DragStart = "dragstart";
    public const string Drag = "drag";
    public const string DragEnd = "dragend";
    public const string DragEnter = "dragenter";
    public const string DragLeave = "dragleave";
    public const string DragOver = "dragover";
    public const string Drop = "drop";

    // ─── Clipboard ────────────────────────────────────────────────────────
    public const string Copy = "copy";
    public const string Cut = "cut";
    public const string Paste = "paste";

    // ─── Composition ──────────────────────────────────────────────────────
    public const string CompositionStart = "compositionstart";
    public const string CompositionUpdate = "compositionupdate";
    public const string CompositionEnd = "compositionend";

    // ─── Window-only ──────────────────────────────────────────────────────
    public const string Resize = "resize";
    public const string Online = "online";
    public const string Offline = "offline";
    public const string HashChange = "hashchange";
    public const string LanguageChange = "languagechange";
    public const string Load = "load";
    public const string Unload = "unload";

    // ─── Document-level visibility / fullscreen ───────────────────────────
    public const string VisibilityChange = "visibilitychange";
    public const string FullscreenChange = "fullscreenchange";
    public const string FullscreenError = "fullscreenerror";
    public const string PointerLockChange = "pointerlockchange";
    public const string PointerLockError = "pointerlockerror";
    public const string DomContentLoaded = "DOMContentLoaded";
}
