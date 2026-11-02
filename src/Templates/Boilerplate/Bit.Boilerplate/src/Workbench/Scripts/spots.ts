/** A box on a screen, as fractions of its viewport. */
type Spot = { x: number; y: number; width: number; height: number };

/** Elements a tap acts on; a pick takes the nearest one around the pointer. */
const tappable = 'button, a[href], input, select, textarea, label, summary, [role=button], [role=link], [role=tab], [role=menuitem], [role=option], [role=switch], [role=checkbox], [role=radio], [tabindex]:not([tabindex="-1"])';
/** Events kept from the page while picking, so a click picks instead of acting. */
const blockedEvents = ['pointerdown', 'pointerup', 'mousedown', 'mouseup', 'dblclick', 'contextmenu', 'submit', 'keydown'];

/** Ends the pick in progress, if there is one. */
let finishPick: ((spot: Spot | null) => void) | null = null;

/** The tappable element under an event, or its target when nothing around it is tappable. */
function targetOf(event: Event) {
    const element = event.target as Element | null;

    return element?.closest?.(tappable) ?? element;
}

/** The element's visible box as fractions of the viewport, or null when none of it is on screen. */
function spotOf(element: Element, view: Window): Spot | null {
    const rect = element.getBoundingClientRect();
    const left = Math.max(0, rect.left);
    const top = Math.max(0, rect.top);
    const right = Math.min(view.innerWidth, rect.right);
    const bottom = Math.min(view.innerHeight, rect.bottom);

    if (right <= left || bottom <= top) return null;

    return { x: left / view.innerWidth, y: top / view.innerHeight, width: (right - left) / view.innerWidth, height: (bottom - top) / view.innerHeight };
}

/** Lets the user click the spot a flow arrow starts from on a flow screen; resolves with it, or with null when cancelled. */
export function pickSpot(screenId: string): Promise<Spot | null> {
    cancelSpotPick();

    const frame = document.querySelector<HTMLIFrameElement>(`iframe[data-screen-id="${CSS.escape(screenId)}"]`);
    const doc = frame?.contentDocument;
    const view = frame?.contentWindow;

    // A screen whose page hasn't loaded fails the pick, so the board can say so; a cancel resolves with null instead.
    if (!doc?.body || !view) return Promise.reject(new Error('The screen has not loaded yet.'));

    return new Promise(resolve => {
        const style = doc.createElement('style');
        style.textContent = 'html, html * { cursor: crosshair !important; }';
        doc.head.appendChild(style);

        const box = doc.createElement('div');
        box.style.cssText = 'position: fixed; display: none; z-index: 2147483646; pointer-events: none; border-radius: 8px; outline: 3px solid var(--bit-clr-pri); background: color-mix(in srgb, var(--bit-clr-pri) 15%, transparent);';
        doc.documentElement.appendChild(box);

        const hover = (event: PointerEvent) => {
            const rect = targetOf(event)?.getBoundingClientRect();
            if (!rect) return;

            Object.assign(box.style, { display: 'block', left: `${rect.left}px`, top: `${rect.top}px`, width: `${rect.width}px`, height: `${rect.height}px` });
        };

        const block = (event: Event) => {
            event.preventDefault();
            event.stopImmediatePropagation();
        };

        // Escape, in the screen or on the board around it, cancels the pick.
        const escape = (event: KeyboardEvent) => {
            if (event.key !== 'Escape') return;

            block(event);
            finish(null);
        };

        const pick = (event: MouseEvent) => {
            block(event);

            const target = targetOf(event);
            finish(target ? spotOf(target, view) : null);
        };

        const finish = (spot: Spot | null) => {
            view.removeEventListener('pointermove', hover, true);
            view.removeEventListener('click', pick, true);
            view.removeEventListener('keydown', escape, true);
            window.removeEventListener('keydown', escape, true);
            blockedEvents.forEach(type => view.removeEventListener(type, block, true));
            style.remove();
            box.remove();
            finishPick = null;
            resolve(spot);
        };

        view.addEventListener('pointermove', hover, true);
        view.addEventListener('click', pick, true);
        view.addEventListener('keydown', escape, true);
        window.addEventListener('keydown', escape, true);
        blockedEvents.forEach(type => view.addEventListener(type, block, true));
        finishPick = finish;
    });
}

/** Cancels the pick in progress, resolving it with null. */
export function cancelSpotPick() {
    finishPick?.(null);
}
