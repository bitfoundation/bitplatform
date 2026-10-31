import { highlight, runChecks } from './checks';
import { cancelInlinePick, connectFrames, pickInline, prepareFrame, setComponentNames, setMode, showPins } from './frames';

const host = window as any;
const isPreview = location.pathname.startsWith('/preview');

const observedStages = new WeakSet<Element>();
const seenFrames = new WeakSet<Element>();

const resizeObserver = new ResizeObserver(entries => entries.forEach(entry => fitStage(entry.target as HTMLElement)));

let shellReference: any = null;
let previewReference: any = null;
let isScreenReady = false;
let isReadyPending = false;

function fitStage(stage: HTMLElement) {
    if (stage.dataset.resizing === 'true') return;

    const scaler = stage.querySelector<HTMLElement>('.pane-scaler');
    const frameWidth = Number(stage.dataset.frameWidth);
    const frameHeight = Number(stage.dataset.frameHeight);

    if (!scaler || !frameWidth || !frameHeight) return;

    const zoom = stage.dataset.zoom;
    let scale = Number(zoom);

    if (!zoom || zoom === 'fit' || Number.isNaN(scale)) {
        const byHeight = stage.clientHeight / frameHeight;
        const byWidth = stage.dataset.fitWidth === 'true' ? stage.clientWidth / frameWidth : fitCanvasWidth(stage);
        scale = Math.min(1, byHeight, byWidth);
    }

    scale = Math.max(0.05, Math.floor(scale * 1000) / 1000);
    scaler.style.setProperty('--wb-scale', String(scale));

    const pane = stage.closest<HTMLElement>('.pane');
    if (pane) {
        pane.style.width = stage.dataset.fitWidth === 'true' ? '' : `${Math.ceil(frameWidth * scale) + 18}px`;
    }
}

function fitCanvasWidth(stage: HTMLElement) {
    const canvas = stage.closest<HTMLElement>('.canvas');
    if (!canvas) return Number.POSITIVE_INFINITY;

    const stages = Array.from(canvas.querySelectorAll<HTMLElement>('.pane-stage'));
    const totalFrameWidth = stages.reduce((sum, s) => sum + Number(s.dataset.frameWidth || 0), 0);
    const style = getComputedStyle(canvas);
    const chrome = stages.length * 20 + (stages.length - 1) * parseFloat(style.columnGap || '16') + parseFloat(style.paddingLeft) + parseFloat(style.paddingRight);

    return totalFrameWidth > 0 ? Math.max(0.2, (canvas.clientWidth - chrome) / totalFrameWidth) : Number.POSITIVE_INFINITY;
}

function scan() {
    document.querySelectorAll<HTMLElement>('.pane-stage').forEach(stage => {
        if (!observedStages.has(stage)) {
            observedStages.add(stage);
            resizeObserver.observe(stage);
        }

        fitStage(stage);

        const frame = stage.querySelector('iframe');
        if (frame && !seenFrames.has(frame)) {
            seenFrames.add(frame);
            stage.classList.remove('ready');
            frame.addEventListener('load', () => prepareFrame(frame));
        }
    });
}

function findFrame(paneId: string) {
    return document.querySelector<HTMLIFrameElement>(`iframe[data-pane-id="${CSS.escape(paneId)}"]`);
}

function frameOf(source: MessageEventSource | null) {
    return Array.from(document.querySelectorAll('iframe')).find(frame => frame.contentWindow === source);
}

function screenOf() {
    return document.querySelector<HTMLIFrameElement>('iframe.screen-frame')?.contentWindow as any;
}

function withTimeout<T>(promise: Promise<T>, milliseconds: number) {
    return Promise.race([promise, new Promise<null>(resolve => setTimeout(() => resolve(null), milliseconds))]);
}

function ownsDrag(target: Element) {
    if (target.closest('input, textarea, select, [contenteditable="true"], [draggable="true"]')) return true;

    for (let element: Element | null = target; element; element = element.parentElement) {
        const touchAction = getComputedStyle(element).touchAction;
        if (touchAction === 'none' || touchAction === 'pinch-zoom') return true;
    }

    return false;
}

function isOnScrollbar(target: Element, event: PointerEvent) {
    if (target === document.documentElement) return event.clientX >= target.clientWidth || event.clientY >= target.clientHeight;

    const rect = target.getBoundingClientRect();
    const x = event.clientX - rect.left - target.clientLeft;
    const y = event.clientY - rect.top - target.clientTop;

    return (target.clientWidth > 0 && (x < 0 || x > target.clientWidth)) || (target.clientHeight > 0 && y > target.clientHeight);
}

function canScroll(element: Element, axis: 'x' | 'y', delta: number): boolean {
    if (delta === 0) return canScroll(element, axis, 1) || canScroll(element, axis, -1);

    const style = getComputedStyle(element);
    const overflow = axis === 'x' ? style.overflowX : style.overflowY;
    const isRoot = element === document.scrollingElement;
    if (isRoot ? overflow === 'hidden' || overflow === 'clip' : overflow !== 'auto' && overflow !== 'scroll' && overflow !== 'overlay') return false;

    if (axis === 'y') return delta > 0 ? element.scrollTop < element.scrollHeight - element.clientHeight - 0.5 : element.scrollTop > 0.5;

    const isRtl = style.direction === 'rtl';
    const start = isRtl ? element.clientWidth - element.scrollWidth : 0;
    const end = isRtl ? 0 : element.scrollWidth - element.clientWidth;

    return delta > 0 ? element.scrollLeft < end - 0.5 : element.scrollLeft > start + 0.5;
}

function findScroller(target: Element, axis: 'x' | 'y', delta: number) {
    for (let element: Element | null = target; element; element = element.parentElement) {
        if (canScroll(element, axis, delta)) return element;
    }

    return null;
}

function emulateTouch() {
    let drag: { x: number; y: number; target: Element; pointerId: number; moved: boolean; scrollerX: Element | null; scrollerY: Element | null; samples: { x: number; y: number; time: number }[] } | null = null;
    let glideFrame = 0;
    let swallowClick = false;

    const scroll = (scrollerX: Element | null, scrollerY: Element | null, left: number, top: number) => {
        if (scrollerX && scrollerX === scrollerY) {
            scrollerX.scrollBy({ left, top, behavior: 'instant' });
            return;
        }

        scrollerX?.scrollBy({ left, behavior: 'instant' });
        scrollerY?.scrollBy({ top, behavior: 'instant' });
    };

    const end = () => {
        if (!drag) return;

        const { moved, pointerId, scrollerX, scrollerY, samples } = drag;
        drag = null;
        document.documentElement.classList.remove('wb-dragging');
        if (document.documentElement.hasPointerCapture(pointerId)) document.documentElement.releasePointerCapture(pointerId);

        if (!moved) return;

        swallowClick = true;
        setTimeout(() => swallowClick = false);

        const now = performance.now();
        const recent = samples.filter(sample => now - sample.time < 100);
        if (recent.length < 2) return;

        const first = recent[0];
        const last = recent[recent.length - 1];
        const elapsed = last.time - first.time;
        if (elapsed <= 0) return;

        let velocityX = (first.x - last.x) / elapsed;
        let velocityY = (first.y - last.y) / elapsed;
        let previous = now;

        const step = (time: number) => {
            const frame = Math.max(0, time - previous);
            previous = time;
            const decay = Math.pow(0.95, frame / 16);
            velocityX *= decay;
            velocityY *= decay;

            if (Math.abs(velocityX) < 0.02 && Math.abs(velocityY) < 0.02) return;

            scroll(scrollerX, scrollerY, velocityX * frame, velocityY * frame);
            glideFrame = requestAnimationFrame(step);
        };

        glideFrame = requestAnimationFrame(step);
    };

    window.addEventListener('pointerdown', event => {
        cancelAnimationFrame(glideFrame);

        const target = event.target instanceof Element ? event.target : null;
        if (event.pointerType !== 'mouse' || event.button !== 0 || event.defaultPrevented || !target) return;
        if (ownsDrag(target) || isOnScrollbar(target, event)) return;

        drag = { x: event.clientX, y: event.clientY, target, pointerId: event.pointerId, moved: false, scrollerX: null, scrollerY: null, samples: [] };
    });

    window.addEventListener('pointermove', event => {
        if (!drag || event.pointerId !== drag.pointerId) return;

        if ((event.buttons & 1) === 0) {
            end();
            return;
        }

        const left = drag.x - event.clientX;
        const top = drag.y - event.clientY;

        if (!drag.moved) {
            if (Math.hypot(left, top) < 6) return;

            drag.moved = true;
            drag.scrollerX = findScroller(drag.target, 'x', left);
            drag.scrollerY = findScroller(drag.target, 'y', top);
            document.documentElement.classList.add('wb-dragging');
            document.documentElement.setPointerCapture(event.pointerId);
            getSelection()?.removeAllRanges();
        }

        scroll(drag.scrollerX, drag.scrollerY, left, top);
        drag.x = event.clientX;
        drag.y = event.clientY;
        drag.samples = [...drag.samples.filter(sample => event.timeStamp - sample.time < 100), { x: event.clientX, y: event.clientY, time: event.timeStamp }];
    });

    window.addEventListener('pointerup', end);
    window.addEventListener('pointercancel', end);
    window.addEventListener('blur', end);
    window.addEventListener('wheel', () => cancelAnimationFrame(glideFrame), { passive: true });

    window.addEventListener('click', event => {
        if (!swallowClick) return;

        swallowClick = false;
        event.preventDefault();
        event.stopImmediatePropagation();
    }, true);

    window.addEventListener('dragstart', event => {
        if (event.target instanceof Element && event.target.closest('[draggable="true"]') === null) {
            event.preventDefault();
        }
    }, true);
}

if (!isPreview) {
    new MutationObserver(scan).observe(document.body, {
        subtree: true,
        childList: true,
        attributes: true,
        attributeFilter: ['data-zoom', 'data-frame-width', 'data-frame-height', 'data-fit-width']
    });

    window.addEventListener('message', event => {
        if (event.origin !== location.origin) return;

        const frame = frameOf(event.source);
        if (!frame) return;

        if (event.data?.type === 'workbench-ready') {
            frame.closest('.pane-stage')?.classList.add('ready');
            prepareFrame(frame);
        }

        if (event.data?.type === 'workbench-scenario' && frame.dataset.paneId) {
            shellReference?.invokeMethodAsync('ReportScenario', frame.dataset.paneId, String(event.data.query ?? ''));
        }

        if (event.data?.type === 'workbench-page') {
            shellReference?.invokeMethodAsync('ReportPage', String(event.data.pageKey ?? ''));
        }
    });
}

function resizeFrames() {
    document.addEventListener('pointerdown', event => {
        const handle = event.target instanceof Element ? event.target.closest<HTMLElement>('.resize-handle') : null;
        const device = handle?.closest<HTMLElement>('.device');
        const stage = handle?.closest<HTMLElement>('.pane-stage');
        const paneId = stage?.querySelector('iframe')?.dataset.paneId;

        if (!handle || !device || !stage || !paneId || event.button !== 0) return;

        event.preventDefault();
        event.stopPropagation();

        const axis = handle.dataset.axis ?? 'xy';
        const scale = Number(getComputedStyle(stage.querySelector('.pane-scaler')!).getPropertyValue('--wb-scale')) || 1;
        const read = (name: string) => parseFloat(device.style.getPropertyValue(name)) || 0;
        const bezelX = read('--wb-bezel-l') + read('--wb-bezel-r');
        const bezelY = read('--wb-bezel-t') + read('--wb-bezel-b');
        const start = { x: event.clientX, y: event.clientY, width: read('--wb-screen-w'), height: read('--wb-screen-h') };
        let size = { width: start.width, height: start.height };

        stage.dataset.resizing = 'true';
        document.documentElement.classList.add('wb-resizing');
        handle.setPointerCapture(event.pointerId);

        const move = (moveEvent: PointerEvent) => {
            const width = axis.includes('x') ? start.width + (moveEvent.clientX - start.x) / scale : start.width;
            const height = axis.includes('y') ? start.height + (moveEvent.clientY - start.y) / scale : start.height;

            size = { width: Math.round(Math.min(Math.max(width, 240), 3840)), height: Math.round(Math.min(Math.max(height, 240), 2400)) };

            device.style.width = `${size.width + bezelX}px`;
            device.style.height = `${size.height + bezelY}px`;
            device.style.setProperty('--wb-screen-w', `${size.width}px`);
            device.style.setProperty('--wb-screen-h', `${size.height}px`);
            stage.style.setProperty('--wb-frame-w', `${size.width + bezelX}px`);
            stage.style.setProperty('--wb-frame-h', `${size.height + bezelY}px`);
            device.dataset.size = `${size.width} × ${size.height}`;
        };

        const end = () => {
            handle.removeEventListener('pointermove', move);
            handle.removeEventListener('pointerup', end);
            handle.removeEventListener('pointercancel', end);
            document.documentElement.classList.remove('wb-resizing');
            delete device.dataset.size;
            delete stage.dataset.resizing;

            if (size.width !== start.width || size.height !== start.height) {
                shellReference?.invokeMethodAsync('ResizePane', paneId, size.width, size.height);
            } else {
                fitStage(stage);
            }
        };

        handle.addEventListener('pointermove', move);
        handle.addEventListener('pointerup', end);
        handle.addEventListener('pointercancel', end);
    }, true);
}

if (!isPreview) {
    resizeFrames();
    connectFrames({ invoke: (method, ...args) => shellReference?.invokeMethodAsync(method, ...args) });
}

if (isPreview && document.body.classList.contains('wb-touch')) {
    emulateTouch();
}

if (isPreview) {
    window.addEventListener('message', event => {
        const screen = screenOf();
        if (event.origin !== location.origin || !screen || event.source !== screen) return;

        if (event.data?.type === 'workbench-ready') {
            isScreenReady = true;
            if (isReadyPending) host.workbench.markReady();
            return;
        }

        if (window.parent === window) return;

        if (event.data?.type === 'workbench-scenario') {
            const query = new URLSearchParams(String(event.data.query ?? ''));
            const own = new URLSearchParams(location.search);
            ['backdrop', 'caption'].forEach(key => own.has(key) && query.set(key, own.get(key)!));
            window.parent.postMessage({ type: 'workbench-scenario', query: `?${query}` }, location.origin);
        }

        if (event.data?.type === 'workbench-page') {
            window.parent.postMessage(event.data, location.origin);
        }
    });
}

host.workbench = {
    markReady() {
        if (screenOf() && isScreenReady === false) {
            isReadyPending = true;
            return;
        }

        host.__workbenchReady = true;
        document.documentElement.setAttribute('data-workbench-ready', '');

        if (window.parent !== window) {
            window.parent.postMessage({ type: 'workbench-ready' }, location.origin);
        }
    },

    registerShell(reference: any) {
        shellReference = reference;
    },

    registerPreview(reference: any) {
        previewReference = reference;
    },

    showInFrame(paneId: string, url: string) {
        const frame = findFrame(paneId);
        const target = new URL(url, location.origin).href;

        if (!frame || frame.contentWindow?.location.href === target) return;

        frame.closest('.pane-stage')?.classList.remove('ready');
        frame.contentWindow?.location.replace(target);
    },

    async captureFrame(paneId: string): Promise<string | null> {
        try {
            const preview = findFrame(paneId)?.contentWindow as any;
            return await withTimeout<string>(preview?.workbench?.captureState() ?? Promise.resolve(null), 4000);
        } catch {
            return null;
        }
    },

    async resetFrame(paneId: string) {
        try {
            const preview = findFrame(paneId)?.contentWindow as any;
            await withTimeout(preview?.workbench?.resetState() ?? Promise.resolve(), 4000);
        } catch {
        }
    },

    captureState(): Promise<string | null> {
        const screen = screenOf();
        if (screen?.workbench) return screen.workbench.captureState();

        return previewReference ? previewReference.invokeMethodAsync('CaptureState') : Promise.resolve(null);
    },

    resetState() {
        const screen = screenOf();
        if (screen?.workbench) return screen.workbench.resetState();

        return previewReference?.invokeMethodAsync('ResetState');
    },

    reportScenario(url: string, query: string, isApplied: boolean) {
        const hasShell = window.parent !== window;

        if (isApplied) {
            history.replaceState(history.state, '', url);
        } else if (!hasShell) {
            location.replace(url);
            return;
        }

        if (hasShell) {
            window.parent.postMessage({ type: 'workbench-scenario', query }, location.origin);
        }
    },

    reportPage(pageKey: string) {
        if (window.parent !== window) {
            window.parent.postMessage({ type: 'workbench-page', pageKey }, location.origin);
        }
    },

    download(url: string) {
        const anchor = document.createElement('a');
        anchor.href = url;
        anchor.download = '';
        document.body.appendChild(anchor);
        anchor.click();
        anchor.remove();
    },

    copy(text: string) {
        return navigator.clipboard?.writeText(text);
    },

    runChecks() {
        return runChecks();
    },

    findTexts(text: string) {
        return previewReference ? previewReference.invokeMethodAsync('FindTexts', text) : Promise.resolve([]);
    },

    reloadFrames() {
        document.querySelectorAll<HTMLIFrameElement>('iframe[data-pane-id]').forEach(frame => {
            frame.closest('.pane-stage')?.classList.remove('ready');
            frame.contentWindow?.location.reload();
        });
    },

    highlight(selector: string) {
        return highlight(selector);
    },

    async checkFrame(paneId: string) {
        const preview = findFrame(paneId)?.contentWindow as any;
        return await withTimeout(preview?.workbench?.runChecks() ?? Promise.resolve(null), 30000);
    },

    highlightInFrame(paneId: string, selector: string) {
        const preview = findFrame(paneId)?.contentWindow as any;
        return preview?.workbench?.highlight(selector) ?? false;
    },

    setComponentNames(names: Record<string, string>, notFromTheme: string) {
        setComponentNames(names, notFromTheme);
    },

    setMode(mode: string) {
        setMode(mode);
    },

    showPins(pins: Record<string, never[]>) {
        showPins(pins);
    },

    pickInline() {
        pickInline(anchor => shellReference?.invokeMethodAsync('BeginInlineComment', anchor));
    },

    cancelInlinePick() {
        cancelInlinePick();
    },

    readTokens(names: string[]) {
        const probe = document.createElement('span');
        probe.style.display = 'none';
        document.body.appendChild(probe);

        const values: Record<string, string> = {};

        names.forEach(name => {
            probe.style.color = '';
            probe.style.color = `var(${name})`;
            const parts = getComputedStyle(probe).color.match(/[\d.]+/g)?.map(Number) ?? [];
            const hex = parts.slice(0, 3).map(part => Math.round(part).toString(16).padStart(2, '0')).join('');
            values[name] = parts.length >= 3 ? `#${hex}${parts.length === 4 && parts[3] < 1 ? ` · ${Math.round(parts[3] * 100)}%` : ''}` : '';
        });

        probe.remove();

        return values;
    },

    readBreakpoints() {
        const style = getComputedStyle(document.documentElement);

        return ['xs', 'sm', 'md', 'lg', 'xl', 'xxl']
            .map(name => ({ name, min: parseFloat(style.getPropertyValue(`--bit-bp-${name}`)) }))
            .filter(breakpoint => Number.isFinite(breakpoint.min));
    },

    setCulture(name: string) {
        document.cookie = `.AspNetCore.Culture=${encodeURIComponent(`c=${name}|uic=${name}`)}; path=/; max-age=31536000; samesite=lax`;
        location.reload();
    }
};

const culture = isPreview ? document.documentElement.lang : null;

host.Blazor.start({
    circuit: {
        configureSignalR: (builder: any) => {
            if (culture) {
                builder.withUrl(`_blazor?culture=${encodeURIComponent(culture)}`);
            }
        }
    }
});
