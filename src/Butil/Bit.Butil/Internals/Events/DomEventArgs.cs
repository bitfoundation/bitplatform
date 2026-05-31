using System;

namespace Bit.Butil;

internal class DomEventArgs
{
    internal static Type TypeOf(string domEvent)
    {
        return domEvent switch
        {
            // Mouse
            ButilEvents.Click => typeof(ButilMouseEventArgs),
            ButilEvents.DblClick => typeof(ButilMouseEventArgs),
            ButilEvents.MouseDown => typeof(ButilMouseEventArgs),
            ButilEvents.MouseUp => typeof(ButilMouseEventArgs),
            ButilEvents.MouseMove => typeof(ButilMouseEventArgs),
            ButilEvents.MouseEnter => typeof(ButilMouseEventArgs),
            ButilEvents.MouseLeave => typeof(ButilMouseEventArgs),
            ButilEvents.MouseOver => typeof(ButilMouseEventArgs),
            ButilEvents.MouseOut => typeof(ButilMouseEventArgs),
            ButilEvents.ContextMenu => typeof(ButilMouseEventArgs),

            // Keyboard
            ButilEvents.KeyDown => typeof(ButilKeyboardEventArgs),
            ButilEvents.KeyUp => typeof(ButilKeyboardEventArgs),
            ButilEvents.KeyPress => typeof(ButilKeyboardEventArgs),

            // Pointer
            ButilEvents.PointerDown => typeof(ButilPointerEventArgs),
            ButilEvents.PointerUp => typeof(ButilPointerEventArgs),
            ButilEvents.PointerMove => typeof(ButilPointerEventArgs),
            ButilEvents.PointerEnter => typeof(ButilPointerEventArgs),
            ButilEvents.PointerLeave => typeof(ButilPointerEventArgs),
            ButilEvents.PointerOver => typeof(ButilPointerEventArgs),
            ButilEvents.PointerOut => typeof(ButilPointerEventArgs),
            ButilEvents.PointerCancel => typeof(ButilPointerEventArgs),
            ButilEvents.GotPointerCapture => typeof(ButilPointerEventArgs),
            ButilEvents.LostPointerCapture => typeof(ButilPointerEventArgs),

            // Touch
            ButilEvents.TouchStart => typeof(ButilTouchEventArgs),
            ButilEvents.TouchEnd => typeof(ButilTouchEventArgs),
            ButilEvents.TouchMove => typeof(ButilTouchEventArgs),
            ButilEvents.TouchCancel => typeof(ButilTouchEventArgs),

            // Wheel
            ButilEvents.Wheel => typeof(ButilWheelEventArgs),

            // Focus
            ButilEvents.Focus => typeof(ButilFocusEventArgs),
            ButilEvents.Blur => typeof(ButilFocusEventArgs),
            ButilEvents.FocusIn => typeof(ButilFocusEventArgs),
            ButilEvents.FocusOut => typeof(ButilFocusEventArgs),

            // Input
            ButilEvents.Input => typeof(ButilInputEventArgs),
            ButilEvents.BeforeInput => typeof(ButilInputEventArgs),

            // Drag
            ButilEvents.DragStart => typeof(ButilDragEventArgs),
            ButilEvents.Drag => typeof(ButilDragEventArgs),
            ButilEvents.DragEnd => typeof(ButilDragEventArgs),
            ButilEvents.DragEnter => typeof(ButilDragEventArgs),
            ButilEvents.DragLeave => typeof(ButilDragEventArgs),
            ButilEvents.DragOver => typeof(ButilDragEventArgs),
            ButilEvents.Drop => typeof(ButilDragEventArgs),

            // Clipboard
            ButilEvents.Copy => typeof(ButilClipboardEventArgs),
            ButilEvents.Cut => typeof(ButilClipboardEventArgs),
            ButilEvents.Paste => typeof(ButilClipboardEventArgs),

            // Composition
            ButilEvents.CompositionStart => typeof(ButilCompositionEventArgs),
            ButilEvents.CompositionUpdate => typeof(ButilCompositionEventArgs),
            ButilEvents.CompositionEnd => typeof(ButilCompositionEventArgs),

            _ => typeof(object),
        };
    }
}
