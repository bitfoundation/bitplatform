type Spot = { x: number; y: number; width: number; height: number };

const tappable = 'button, a[href], input, select, textarea, label, summary, [role=button], [role=link], [role=tab], [role=menuitem], [role=option], [role=switch], [role=checkbox], [role=radio], [tabindex]:not([tabindex="-1"])';
const blockedEvents = ['pointerdown', 'pointerup', 'mousedown', 'mouseup', 'dblclick', 'contextmenu', 'submit', 'keydown'];

let finishPick: ((spot: Spot | null) => void) | null = null;

function targetOf(event: Event) {
    const element = event.target as Element | null;

    return element?.closest?.(tappable) ?? element;
}

function spotOf(element: Element, view: Window): Spot | null {
    const rect = element.getBoundingClientRect();
    const left = Math.max(0, rect.left);
    const top = Math.max(0, rect.top);
    const right = Math.min(view.innerWidth, rect.right);
    const bottom = Math.min(view.innerHeight, rect.bottom);

    if (right <= left || bottom <= top) return null;

    return { x: left / view.innerWidth, y: top / view.innerHeight, width: (right - left) / view.innerWidth, height: (bottom - top) / view.innerHeight };
}

export function pickSpot(screenId: string): Promise<Spot | null> {
    cancelSpotPick();

    const frame = document.querySelector<HTMLIFrameElement>(`iframe[data-screen-id="${CSS.escape(screenId)}"]`);
    const doc = frame?.contentDocument;
    const view = frame?.contentWindow;

    if (!doc?.body || !view) return Promise.resolve(null);

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

        const pick = (event: MouseEvent) => {
            block(event);

            const target = targetOf(event);
            finish(target ? spotOf(target, view) : null);
        };

        const finish = (spot: Spot | null) => {
            view.removeEventListener('pointermove', hover, true);
            view.removeEventListener('click', pick, true);
            blockedEvents.forEach(type => view.removeEventListener(type, block, true));
            style.remove();
            box.remove();
            finishPick = null;
            resolve(spot);
        };

        view.addEventListener('pointermove', hover, true);
        view.addEventListener('click', pick, true);
        blockedEvents.forEach(type => view.addEventListener(type, block, true));
        finishPick = finish;
    });
}

export function cancelSpotPick() {
    finishPick?.(null);
}
