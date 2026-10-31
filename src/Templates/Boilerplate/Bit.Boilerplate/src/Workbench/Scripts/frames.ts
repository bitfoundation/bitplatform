type Pin = { id: string; number: string; selector: string; text: string | null; x: number; y: number; status: string; selected: boolean };

type FrameHost = { invoke(method: string, ...args: unknown[]): void };

const frameCss = `
.wb-layer { position: fixed; inset: 0; z-index: 2147483646; pointer-events: none; }
.wb-hover { position: fixed; display: none; pointer-events: none; border-radius: 2px; outline: 2px solid var(--bit-clr-pri); background: color-mix(in srgb, var(--bit-clr-pri) 10%, transparent); }
.wb-pad { position: fixed; display: none; pointer-events: none; border-style: solid; border-color: color-mix(in srgb, var(--bit-clr-suc) 30%, transparent); }
.wb-info { position: fixed; display: none; pointer-events: none; max-width: 340px; padding: 6px 9px; border-radius: 8px; font: 12px/1.5 system-ui, sans-serif; color: var(--bit-clr-bg-pri); background: var(--bit-clr-fg-pri); box-shadow: 0 6px 20px rgb(0 0 0 / 30%); }
.wb-info b { font-weight: 700; }
.wb-info i { opacity: 0.75; font-style: normal; }
.wb-pin { position: fixed; width: 26px; height: 26px; margin: -26px 0 0 0; padding: 0; cursor: pointer; pointer-events: auto; border-radius: 50% 50% 50% 4px; font: 700 12px/22px system-ui, sans-serif; text-align: center; color: var(--bit-clr-pri-text); background: var(--bit-clr-pri); border: 2px solid var(--bit-clr-bg-pri); box-shadow: 0 2px 8px rgb(0 0 0 / 35%); transition: transform 0.12s ease; transform-origin: bottom left; }
.wb-pin.working { color: var(--bit-clr-wrn-text); background: var(--bit-clr-wrn); }
.wb-pin.done { color: var(--bit-clr-suc-text); background: var(--bit-clr-suc); }
.wb-pin.selected { transform: scale(1.3); }
.wb-pin[hidden] { display: none; }
html[data-wb-mode="comment"], html[data-wb-mode="comment"] * { cursor: crosshair !important; }
html[data-wb-mode="inspect"], html[data-wb-mode="inspect"] *, html[data-wb-mode="text"] [data-wb-text] { cursor: default !important; }
html[data-wb-mode] .wb-pin { cursor: pointer !important; }
`;

const blockedEvents = ['pointerdown', 'pointerup', 'mousedown', 'mouseup', 'touchstart', 'touchend', 'dblclick', 'contextmenu', 'submit', 'keydown'];

const preparedDocuments = new WeakSet<Document>();
const themeColors = new WeakMap<Document, Map<string, string>>();

let mode = 'browse';
let host: FrameHost | null = null;
let componentNames: Record<string, string> = {};
let notFromThemeLabel = '';
let pinsByPane: Record<string, Pin[]> = {};
let pinFrame = 0;

const isPicking = () => mode === 'comment' || mode === 'inspect' || mode === 'text';

export function connectFrames(frameHost: FrameHost) {
    host = frameHost;

    setInterval(() => {
        if (mode === 'comment' || Object.values(pinsByPane).some(pins => pins.length > 0)) schedulePins();
    }, 700);
}

export function setComponentNames(names: Record<string, string>, notFromTheme: string) {
    componentNames = names ?? {};
    notFromThemeLabel = notFromTheme;
}

export function setMode(next: string) {
    if (mode === next) return;

    mode = next;
    paneFrames().forEach(prepareFrame);
}

export function showPins(pins: Record<string, unknown[]>) {
    pinsByPane = (pins ?? {}) as Record<string, Pin[]>;
    paneFrames().forEach(prepareFrame);
}

export function prepareFrame(frame: HTMLIFrameElement) {
    const doc = frame.contentDocument;
    const win = frame.contentWindow as (Window & typeof globalThis) | null;

    if (!doc || !win || !doc.body || doc.location.pathname.startsWith('/preview') === false) return;

    if (!preparedDocuments.has(doc)) {
        preparedDocuments.add(doc);

        const style = doc.createElement('style');
        style.textContent = frameCss;
        doc.head.appendChild(style);

        const layer = doc.createElement('div');
        layer.className = 'wb-layer';
        layer.innerHTML = '<div class="wb-hover"></div><div class="wb-pad"></div><div class="wb-info"></div>';
        doc.documentElement.appendChild(layer);

        const block = (event: Event) => {
            if (!isPicking()) return;
            if (event instanceof win.KeyboardEvent && event.key !== 'Enter' && event.key !== ' ') return;
            if ((event.target as Element | null)?.closest?.('.wb-pin')) return;

            event.preventDefault();
            event.stopImmediatePropagation();
        };

        blockedEvents.forEach(type => win.addEventListener(type, block, true));
        win.addEventListener('click', event => onClick(frame, event), true);
        win.addEventListener('mousemove', event => onHover(frame, event), true);
        doc.documentElement.addEventListener('mouseleave', () => hideHover(doc));
        win.addEventListener('scroll', schedulePins, true);
        win.addEventListener('resize', schedulePins);
    }

    if (mode === 'browse') {
        delete doc.documentElement.dataset.wbMode;
        hideHover(doc);
    } else {
        doc.documentElement.dataset.wbMode = mode;
    }

    renderPins(frame);
}

function paneFrames() {
    return Array.from(document.querySelectorAll<HTMLIFrameElement>('iframe[data-pane-id]'));
}

function onClick(frame: HTMLIFrameElement, event: MouseEvent) {
    if (!isPicking()) return;

    const target = event.target as Element | null;
    if (!target) return;

    event.preventDefault();
    event.stopImmediatePropagation();

    const pin = target.closest<HTMLElement>('.wb-pin');

    if (pin?.dataset.id) {
        host?.invoke('OpenThread', pin.dataset.id);
        return;
    }

    if (mode === 'comment' && frame.dataset.paneId) {
        host?.invoke('BeginComment', frame.dataset.paneId, anchorOf(target, event.clientX, event.clientY));
    }

    if (mode === 'text' && frame.dataset.paneId) {
        const paneId = frame.dataset.paneId;
        const text = textOf(target);
        const anchor = anchorOf(target, event.clientX, event.clientY);
        const preview = frame.contentWindow as any;

        Promise.resolve(text ? preview?.workbench?.findTexts(text) : [])
            .catch(() => [])
            .then(matches => host?.invoke('BeginTextEdit', paneId, text, matches ?? [], anchor));
    }
}

function textOf(element: Element) {
    const input = element.closest('input, textarea') as HTMLInputElement | null;
    const labelled = element.closest('[aria-label], [title]');
    const text = input?.placeholder || (element as HTMLElement).innerText || labelled?.getAttribute('aria-label') || labelled?.getAttribute('title') || '';

    return text.trim().replace(/\s+/g, ' ').slice(0, 500);
}

function onHover(frame: HTMLIFrameElement, event: MouseEvent) {
    const doc = frame.contentDocument;
    const target = event.target as Element | null;

    if (!doc || !target || target.closest('.wb-layer')) return;

    if (!isPicking()) {
        hideHover(doc);
        return;
    }

    const rect = target.getBoundingClientRect();
    const hover = doc.querySelector<HTMLElement>('.wb-hover')!;

    Object.assign(hover.style, { display: 'block', left: `${rect.left}px`, top: `${rect.top}px`, width: `${rect.width}px`, height: `${rect.height}px` });

    const pad = doc.querySelector<HTMLElement>('.wb-pad')!;
    const info = doc.querySelector<HTMLElement>('.wb-info')!;

    if (mode !== 'inspect') {
        pad.style.display = 'none';
        info.style.display = 'none';
        return;
    }

    const style = getComputedStyle(target);
    Object.assign(pad.style, {
        display: 'block',
        left: `${rect.left}px`,
        top: `${rect.top}px`,
        width: `${rect.width}px`,
        height: `${rect.height}px`,
        boxSizing: 'border-box',
        borderWidth: `${style.paddingTop} ${style.paddingRight} ${style.paddingBottom} ${style.paddingLeft}`
    });

    info.innerHTML = describe(doc, target, rect, style);
    info.style.display = 'block';

    const width = info.offsetWidth;
    const height = info.offsetHeight;
    const viewport = doc.documentElement.clientWidth;
    const below = rect.bottom + 8 + height < doc.documentElement.clientHeight;

    info.style.left = `${Math.max(4, Math.min(rect.left, viewport - width - 4))}px`;
    info.style.top = `${below ? rect.bottom + 8 : Math.max(4, rect.top - height - 8)}px`;
}

function hideHover(doc: Document) {
    doc.querySelectorAll<HTMLElement>('.wb-hover, .wb-pad, .wb-info').forEach(element => element.style.display = 'none');
}

function describe(doc: Document, element: Element, rect: DOMRect, style: CSSStyleDeclaration) {
    const escape = (value: string) => value.replace(/[&<>"]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]!));
    const component = componentOf(element);
    const lines = [
        `<b>${escape(component ?? element.tagName.toLowerCase())}</b>${component ? ` <i>${escape(element.tagName.toLowerCase())}</i>` : ''}`,
        `${Math.round(rect.width)} × ${Math.round(rect.height)}`,
        `<i>padding</i> ${box(style.paddingTop, style.paddingRight, style.paddingBottom, style.paddingLeft)} · <i>margin</i> ${box(style.marginTop, style.marginRight, style.marginBottom, style.marginLeft)}`,
        `<i>font</i> ${escape(style.fontSize)} ${escape(style.fontWeight)} ${escape(style.fontFamily.split(',')[0].replace(/["']/g, ''))}`,
        `<i>color</i> ${escape(colorName(doc, style.color))}`
    ];

    if (style.backgroundColor !== 'rgba(0, 0, 0, 0)') lines.push(`<i>background</i> ${escape(colorName(doc, style.backgroundColor))}`);
    if (parseFloat(style.borderTopWidth) > 0) lines.push(`<i>border</i> ${escape(style.borderTopWidth)} ${escape(colorName(doc, style.borderTopColor))}`);
    if (style.borderTopLeftRadius !== '0px') lines.push(`<i>radius</i> ${escape(style.borderTopLeftRadius)}`);

    return lines.join('<br>');
}

function box(top: string, right: string, bottom: string, left: string) {
    const values = [top, right, bottom, left].map(value => Math.round(parseFloat(value)));
    return values.every(value => value === values[0]) ? `${values[0]}` : values.join(' ');
}

function colorName(doc: Document, color: string) {
    const names = themeColorsOf(doc);
    return names.get(color) ?? `${color} · ${notFromThemeLabel}`;
}

function themeColorsOf(doc: Document) {
    let colors = themeColors.get(doc);
    if (colors) return colors;

    colors = new Map<string, string>();
    const probe = doc.createElement('span');
    probe.style.display = 'none';
    doc.body.appendChild(probe);

    for (const name of customProperties(doc, name => name.startsWith('--bit-clr-'))) {
        probe.style.color = `var(${name})`;
        const value = getComputedStyle(probe).color;
        if (value && !colors.has(value)) colors.set(value, name);
    }

    probe.remove();
    themeColors.set(doc, colors);

    return colors;
}

export function customProperties(doc: Document, accept: (name: string) => boolean) {
    const names = new Set<string>();

    for (const sheet of Array.from(doc.styleSheets)) {
        let rules: CSSRuleList;
        try {
            rules = sheet.cssRules;
        } catch {
            continue;
        }

        for (const rule of Array.from(rules)) {
            const style = (rule as CSSStyleRule).style;
            if (!style) continue;

            for (let i = 0; i < style.length; i++) {
                if (accept(style[i])) names.add(style[i]);
            }
        }
    }

    return names;
}

export function componentOf(element: Element | null) {
    for (let current = element; current; current = current.parentElement) {
        for (const name of Array.from(current.classList)) {
            if (componentNames[name]) return componentNames[name];
        }
    }

    return null;
}

function anchorOf(target: Element, x: number, y: number) {
    const rect = target.getBoundingClientRect();
    const labelled = target.closest('[aria-label], [title]');
    const text = ((target as HTMLElement).innerText || labelled?.getAttribute('aria-label') || labelled?.getAttribute('title') || '').trim().replace(/\s+/g, ' ');

    return {
        selector: selectorOf(target),
        component: componentOf(target),
        text: text.slice(0, 120) || null,
        x: rect.width > 0 ? (x - rect.left) / rect.width : 0.5,
        y: rect.height > 0 ? (y - rect.top) / rect.height : 0.5
    };
}

function selectorOf(element: Element) {
    const parts: string[] = [];

    for (let current: Element | null = element; current && current.tagName !== 'BODY' && current.tagName !== 'HTML'; current = current.parentElement) {
        let index = 1;

        for (let sibling = current.previousElementSibling; sibling; sibling = sibling.previousElementSibling) {
            if (sibling.tagName === current.tagName) index++;
        }

        parts.unshift(`${current.tagName.toLowerCase()}:nth-of-type(${index})`);
    }

    return parts.length ? `body > ${parts.join(' > ')}` : 'body';
}

function resolve(doc: Document, pin: Pin) {
    let element: Element | null = null;

    try {
        element = doc.querySelector(pin.selector);
    } catch {
    }

    const text = pin.text?.slice(0, 40);
    if (element && (!text || ((element as HTMLElement).innerText ?? '').includes(text))) return element;
    if (!text) return element;

    const tag = pin.selector.split('>').pop()?.trim().split(':')[0] || '*';

    return Array.from(doc.querySelectorAll(tag)).find(candidate => ((candidate as HTMLElement).innerText ?? '').trim().replace(/\s+/g, ' ').startsWith(text)) ?? element;
}

function renderPins(frame: HTMLIFrameElement) {
    const doc = frame.contentDocument;
    const layer = doc?.querySelector('.wb-layer');
    if (!doc || !layer) return;

    const pins = (pinsByPane[frame.dataset.paneId ?? ''] ?? []).filter(pin => mode === 'comment' || pin.selected);
    const existing = new Map(Array.from(layer.querySelectorAll<HTMLElement>('.wb-pin')).map(element => [element.dataset.id!, element]));

    for (const pin of pins) {
        let element = existing.get(pin.id);
        existing.delete(pin.id);

        if (!element) {
            const button = doc.createElement('button');
            button.type = 'button';
            button.dataset.id = pin.id;
            layer.appendChild(button);
            element = button;
        }

        element.textContent = pin.number;
        element.className = `wb-pin ${pin.status}${pin.selected ? ' selected' : ''}`;
        element.dataset.pin = JSON.stringify(pin);
    }

    existing.forEach(element => element.remove());
    positionPins(doc);
}

function schedulePins() {
    if (pinFrame) return;

    pinFrame = requestAnimationFrame(() => {
        pinFrame = 0;
        paneFrames().forEach(frame => frame.contentDocument && positionPins(frame.contentDocument));
    });
}

function positionPins(doc: Document) {
    doc.querySelectorAll<HTMLElement>('.wb-pin').forEach(element => {
        const pin = JSON.parse(element.dataset.pin ?? '{}') as Pin;
        const target = resolve(doc, pin);
        const rect = target?.getBoundingClientRect();

        if (!rect || (rect.width === 0 && rect.height === 0)) {
            element.hidden = true;
            return;
        }

        element.hidden = false;
        element.style.left = `${rect.left + rect.width * pin.x}px`;
        element.style.top = `${rect.top + rect.height * pin.y}px`;
    });
}
