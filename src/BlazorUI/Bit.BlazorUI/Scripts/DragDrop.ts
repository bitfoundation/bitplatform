namespace BitBlazorUI {
    export class DragDrop {
        private static _listeners: { [key: string]: any } = {};

        // What a pointerdown must not turn into a drag. The drag surface of a Dialog or a Modal can be the
        // whole container, which puts every control the consumer put inside it under the same handler:
        // without this, reaching for a field, dragging a slider or selecting a word would move the window.
        private static readonly _interactives =
            'a[href], button, input, select, textarea, label, summary, ' +
            '[contenteditable]:not([contenteditable="false"]), ' +
            '[role="button"], [role="link"], [role="checkbox"], [role="radio"], [role="switch"], ' +
            '[role="slider"], [role="spinbutton"], [role="textbox"], [role="combobox"], ' +
            '[role="tab"], [role="menuitem"], [role="option"]';

        public static setup(key: string, containerSelector: string, dragElementSelector: string) {
            DragDrop.remove(key, dragElementSelector);

            const element = document.querySelector(containerSelector) as HTMLElement;
            // The drag selector may point to the container itself (the default) or to a descendant.
            const dragElement = (element?.matches(dragElementSelector) ? element : element?.querySelector(dragElementSelector)) as HTMLElement;
            if (!element || !dragElement) return;

            const listeners: any = {};
            DragDrop._listeners[key] = listeners;

            let x = 0;
            let y = 0;

            listeners['pointerdown'] = handlePointerDown;
            listeners['dragElement'] = dragElement;
            listeners['element'] = element;
            dragElement.addEventListener('pointerdown', handlePointerDown);
            dragElement.style.cursor = 'move';
            dragElement.classList.add('bit-mdl-nta');

            function handlePointerDown(e: PointerEvent) {
                // Only the primary button drags: a right-click is a context menu and a middle-click is a
                // paste on some platforms, and neither is a request to move the window.
                if (e.button !== 0) return;

                if (DragDrop.isInteractive(e.target as Element | null, dragElement)) return;

                x = e.clientX;
                y = e.clientY;

                const { width } = element.getBoundingClientRect();
                element.style.width = `${width}px`;

                document.addEventListener('pointermove', handlePointerMove);
                listeners['pointermove'] = handlePointerMove;

                document.addEventListener('pointerup', handlePointerUp);
                listeners['pointerup'] = handlePointerUp;

                // A pointercancel (the browser taking the gesture over for a scroll, the pointer leaving
                // the window) never fires a pointerup, which would otherwise leave the move handler bound
                // to the document and the surface following the pointer with no button held down.
                document.addEventListener('pointercancel', handlePointerUp);
                listeners['pointercancel'] = handlePointerUp;
            }

            function handlePointerMove(e: PointerEvent) {
                e.preventDefault();

                let left = element.offsetLeft - (x - e.clientX);
                let top = element.offsetTop - (y - e.clientY);

                // Dragging is not a way to lose the surface: it stays inside the area it was positioned
                // in, so it can never be pushed past an edge and out of reach of the pointer that would
                // have to bring it back. A surface larger than that area is pinned to the top left of it
                // rather than clamped to a negative bound.
                const parent = element.offsetParent as HTMLElement | null;
                if (parent) {
                    left = Math.max(0, Math.min(left, parent.clientWidth - element.offsetWidth));
                    top = Math.max(0, Math.min(top, parent.clientHeight - element.offsetHeight));
                }

                element.style.left = `${left}px`;
                element.style.top = `${top}px`;

                x = e.clientX;
                y = e.clientY;
            }

            function handlePointerUp() {
                document.removeEventListener('pointermove', handlePointerMove);
                document.removeEventListener('pointerup', handlePointerUp);
                document.removeEventListener('pointercancel', handlePointerUp);
            }
        }

        // Whether the pointerdown landed on something the user was reaching for rather than on the drag
        // surface itself. The walk stops at the drag element, which is excluded: it is the handle, and on a
        // fully draggable surface it is the container - which carries a tabindex of its own.
        private static isInteractive(target: Element | null, dragElement: HTMLElement) {
            try {
                let node = target;
                while (node && node !== dragElement) {
                    if (node.matches?.(DragDrop._interactives)) return true;
                    node = node.parentElement;
                }
                return false;
            } catch (e) {
                console.error("BitBlazorUI.DragDrop.isInteractive:", e);
                return false;
            }
        }

        public static remove(key: string, dragElementSelector: string) {
            const listeners = DragDrop._listeners[key];
            if (!listeners) return;

            // Use the originally-bound drag element so cleanup still targets the
            // correct element even if the selector resolves differently now.
            const dragElement = (listeners['dragElement'] as HTMLElement) ?? (document.querySelector(dragElementSelector) as HTMLElement);
            if (dragElement) {
                dragElement.removeEventListener('pointerdown', listeners['pointerdown']);
                dragElement.style.cursor = '';
                dragElement.classList.remove('bit-mdl-nta');
            }

            // The drag writes the surface's position onto it - and pins its width, so the box cannot reflow
            // under the pointer mid-move - and both outlive the surface wherever it is kept in the DOM
            // between showings. A surface that comes back is a new showing, so it comes back where it was
            // laid out rather than where it was last left, which is what it already does everywhere it is
            // unmounted instead.
            const element = listeners['element'] as HTMLElement;
            if (element) {
                element.style.left = '';
                element.style.top = '';
                element.style.width = '';
            }

            document.removeEventListener('pointermove', listeners['pointermove']);
            document.removeEventListener('pointerup', listeners['pointerup']);
            document.removeEventListener('pointercancel', listeners['pointercancel']);

            delete listeners['pointerdown'];
            delete listeners['pointermove'];
            delete listeners['pointerup'];
            delete listeners['pointercancel'];
            delete listeners['dragElement'];
            delete listeners['element'];
            delete DragDrop._listeners[key];
        }
    }
}
