import { highlight, parseColor, runChecks } from './checks';
import { cancelInlinePick, connectFrames, inspectAgain, pickInline, prepareFrame, setComponentNames, setMode, showPins } from './frames';
import { cancelSpotPick, pickSpot } from './spots';
import { forceStates } from './states';

/** The window, typed loosely to reach Blazor and the workbench interop object. */
const host = window as any;
/** True inside a view's page (/preview/...), false on the shell, the home page and the flows board. */
const isPreview = location.pathname.startsWith('/preview');

/** Device stages the resize observer already watches, and iframes whose load is already handled. */
const observedStages = new WeakSet<Element>();
const seenFrames = new WeakSet<Element>();

/** The fallback timer of each view still loading, and the document each view had when it was sent to a new address. */
const readyTimers = new WeakMap<HTMLIFrameElement, number>();
const staleDocuments = new WeakMap<HTMLIFrameElement, Document | null>();

/** The address each view was last sent to, for reloading a view that shows a page of another origin. */
const shownUrls = new WeakMap<HTMLIFrameElement, string>();

/** Refits a device stage whenever its box changes size. */
const resizeObserver = new ResizeObserver(entries => entries.forEach(entry => fitStage(entry.target as HTMLElement)));

/** The .NET objects of the shell page and of the preview page, which this script calls back. */
let shellReference: any = null;
let previewReference: any = null;
/** In a store composition: whether the inner screen has reported ready, and whether this page's own ready waits for it. */
let isScreenReady = false;
let isReadyPending = false;

/** Scales a device to fit its stage, or to the chosen zoom, and sizes its pane to match. */
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

/** The scale that fits every view side by side in the canvas width, after the panes' padding and gaps. */
function fitCanvasWidth(stage: HTMLElement) {
    const canvas = stage.closest<HTMLElement>('.canvas');
    if (!canvas) return Number.POSITIVE_INFINITY;

    const stages = Array.from(canvas.querySelectorAll<HTMLElement>('.pane-stage'));
    const totalFrameWidth = stages.reduce((sum, s) => sum + Number(s.dataset.frameWidth || 0), 0);
    const style = getComputedStyle(canvas);
    const chrome = stages.length * 20 + (stages.length - 1) * parseFloat(style.columnGap || '16') + parseFloat(style.paddingLeft) + parseFloat(style.paddingRight);

    return totalFrameWidth > 0 ? Math.max(0.2, (canvas.clientWidth - chrome) / totalFrameWidth) : Number.POSITIVE_INFINITY;
}

/** Watches new device stages, refits all of them, and shows the loading state of each new iframe until it reports ready. */
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
            frame.addEventListener('load', () => onFrameLoad(frame));
        }

        if (frame && hasReported(frame)) markStageReady(frame);
    });
}

/** Prepares a view's new document, and ends its loading state at once when it is ready or isn't a preview, or after 25 seconds. */
function onFrameLoad(frame: HTMLIFrameElement) {
    prepareFrame(frame);
    clearTimeout(readyTimers.get(frame));

    const doc = frame.contentDocument;

    if (hasReported(frame) || doc === null || doc.location.pathname.startsWith('/preview') === false) {
        markStageReady(frame);
        return;
    }

    // A page that fails before it reports ready then shows itself, or its error, instead of a spinner.
    readyTimers.set(frame, setTimeout(() => markStageReady(frame), 25000));
}

/** Whether the document a view holds now has reported ready, so a missed message doesn't leave it loading. */
function hasReported(frame: HTMLIFrameElement) {
    try {
        const doc = frame.contentDocument;
        return doc !== null && doc !== staleDocuments.get(frame) && (frame.contentWindow as any)?.__workbenchReady === true;
    } catch {
        return false;
    }
}

/** Ends a view's loading state. */
function markStageReady(frame: HTMLIFrameElement) {
    clearTimeout(readyTimers.get(frame));
    readyTimers.delete(frame);
    frame.closest('.pane-stage')?.classList.add('ready');
}

/** Shows a view's loading state until the document it is being sent to reports ready. */
function expectNewDocument(frame: HTMLIFrameElement) {
    clearTimeout(readyTimers.get(frame));
    readyTimers.delete(frame);
    staleDocuments.set(frame, frame.contentDocument);
    frame.closest('.pane-stage')?.classList.remove('ready');
}

/** The iframe of the view with this pane id. */
function findFrame(paneId: string) {
    return document.querySelector<HTMLIFrameElement>(`iframe[data-pane-id="${CSS.escape(paneId)}"]`);
}

/** The iframe whose window sent a message. */
function frameOf(source: MessageEventSource | null) {
    return Array.from(document.querySelectorAll('iframe')).find(frame => frame.contentWindow === source);
}

/** The inner screen a store composition shows its page in, when this page is one. */
function screenFrame() {
    return document.querySelector<HTMLIFrameElement>('iframe.screen-frame');
}

/** The window of the inner screen a store composition shows its page in, when this page is one. */
function screenOf() {
    return screenFrame()?.contentWindow as any;
}

/** The workbench object of a frame's page, or null while it loads or once it shows a page of another origin. */
function workbenchOf(frame: HTMLIFrameElement | null | undefined): any {
    try {
        return (frame?.contentWindow as any)?.workbench ?? null;
    } catch {
        return null;
    }
}

/** The address a frame shows, or null once it shows a page of another origin. */
function addressOf(frame: HTMLIFrameElement) {
    try {
        return frame.contentWindow?.location.href ?? null;
    } catch {
        return null;
    }
}

/** Calls the shell page's .NET object; a failed call, as after the page has gone, is logged instead of left unhandled. */
function callShell(method: string, ...args: unknown[]) {
    shellReference?.invokeMethodAsync(method, ...args).catch((error: unknown) => console.warn(error));
}

/** Calls the preview page's .NET object, answering the fallback when there is none or the call fails. */
function callPreview<T>(fallback: T, method: string, ...args: unknown[]): Promise<T> {
    if (!previewReference) return Promise.resolve(fallback);

    return previewReference.invokeMethodAsync(method, ...args).catch((error: unknown) => {
        console.warn(error);
        return fallback;
    });
}

/** Resolves with the promise's value, or with null once the time runs out. */
function withTimeout<T>(promise: Promise<T>, milliseconds: number) {
    return Promise.race([promise, new Promise<null>(resolve => setTimeout(() => resolve(null), milliseconds))]);
}

/** Whether the page handles a drag on this element itself: a field, a draggable item or a touch-action that takes the gesture. */
function ownsDrag(target: Element) {
    if (target.closest('input, textarea, select, [contenteditable="true"], [draggable="true"]')) return true;

    for (let element: Element | null = target; element; element = element.parentElement) {
        const touchAction = getComputedStyle(element).touchAction;
        if (touchAction === 'none' || touchAction === 'pinch-zoom') return true;
    }

    return false;
}

/** Whether the pointer went down on the element's scrollbar rather than on its content. */
function isOnScrollbar(target: Element, event: PointerEvent) {
    if (target === document.documentElement) return event.clientX >= target.clientWidth || event.clientY >= target.clientHeight;

    const rect = target.getBoundingClientRect();
    const x = event.clientX - rect.left - target.clientLeft;
    const y = event.clientY - rect.top - target.clientTop;

    return (target.clientWidth > 0 && (x < 0 || x > target.clientWidth)) || (target.clientHeight > 0 && y > target.clientHeight);
}

/** Whether the element can scroll further along the axis in the drag's direction; a zero delta means in either direction. */
function canScroll(element: Element, axis: 'x' | 'y', delta: number): boolean {
    if (delta === 0) return canScroll(element, axis, 1) || canScroll(element, axis, -1);

    const style = getComputedStyle(element);
    const overflow = axis === 'x' ? style.overflowX : style.overflowY;
    const isRoot = element === document.scrollingElement;
    if (isRoot ? overflow === 'hidden' || overflow === 'clip' : overflow !== 'auto' && overflow !== 'scroll' && overflow !== 'overlay') return false;

    if (axis === 'y') return delta > 0 ? element.scrollTop < element.scrollHeight - element.clientHeight - 0.5 : element.scrollTop > 0.5;

    // In right-to-left, scrollLeft runs from a negative start up to zero.
    const isRtl = style.direction === 'rtl';
    const start = isRtl ? element.clientWidth - element.scrollWidth : 0;
    const end = isRtl ? 0 : element.scrollWidth - element.clientWidth;

    return delta > 0 ? element.scrollLeft < end - 0.5 : element.scrollLeft > start + 0.5;
}

/** The nearest element, from the target up, that can scroll along the axis in the drag's direction. */
function findScroller(target: Element, axis: 'x' | 'y', delta: number) {
    for (let element: Element | null = target; element; element = element.parentElement) {
        if (canScroll(element, axis, delta)) return element;
    }

    return null;
}

/** Makes a mouse drag scroll a handheld view the way a finger does, with momentum, and keeps the drag from clicking. */
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

        // The click that ends a drag is not a tap, so it is swallowed.
        swallowClick = true;
        setTimeout(() => swallowClick = false);

        // Glides on with the speed of the last 100 ms of the drag, slowing down each frame.
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

        // A drag starts only after 6px, so a plain click still clicks.
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

    // A native drag of an image or a link would take over the gesture, so only what the page marks draggable may start one.
    window.addEventListener('dragstart', event => {
        if (event.target instanceof Element && event.target.closest('[draggable="true"]') === null) {
            event.preventDefault();
        }
    }, true);
}

// On the shell: refit the stages as views come and go or change size, and handle what the views report.
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

        // The view has settled: hide its loading state and set up the comment and inspect tools in it. A ready from
        // the document a view is leaving is ignored.
        if (event.data?.type === 'workbench-ready' && frame.contentDocument !== staleDocuments.get(frame)) {
            markStageReady(frame);
            prepareFrame(frame);
        }

        if (event.data?.type === 'workbench-scenario' && frame.dataset.paneId) {
            callShell('ReportScenario', frame.dataset.paneId, String(event.data.query ?? ''));
        }

        if (event.data?.type === 'workbench-sign-in-guide' && frame.dataset.paneId) {
            callShell('ShowSignInGuide', frame.dataset.paneId);
        }

        // Only a view moves the shell to another page; a flow screen doesn't.
        if (event.data?.type === 'workbench-page' && frame.dataset.paneId) {
            callShell('ReportPage', String(event.data.pageKey ?? ''));
        }
    });
}

/** Lets the edge and corner handles of a device resize its screen, and reports the new size to the shell when the drag ends. */
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

        // The pointer moves in scaled pixels, so the change is divided by the stage's scale.
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
                callShell('ResizePane', paneId, size.width, size.height);
            } else {
                fitStage(stage);
            }
        };

        handle.addEventListener('pointermove', move);
        handle.addEventListener('pointerup', end);
        handle.addEventListener('pointercancel', end);
    }, true);
}

// Resizing devices and the comment, inspect and text tools run on the shell, over its views.
if (!isPreview) {
    resizeFrames();
    connectFrames({ invoke: callShell });
}

// Touch emulation runs in handheld views without a backdrop, whose body the server marks with wb-touch.
if (isPreview && document.body.classList.contains('wb-touch')) {
    emulateTouch();
}

// A store composition waits for its inner screen and passes the screen's reports on to the shell.
if (isPreview) {
    window.addEventListener('message', event => {
        const screen = screenOf();
        if (event.origin !== location.origin || !screen || event.source !== screen) return;

        // The inner screen has settled, so this page can report ready if it was waiting.
        if (event.data?.type === 'workbench-ready') {
            isScreenReady = true;
            if (isReadyPending) host.workbench.markReady();
            return;
        }

        if (window.parent === window) return;

        // The inner screen doesn't know the backdrop and caption, so they are taken from this page's address.
        if (event.data?.type === 'workbench-scenario') {
            const query = new URLSearchParams(String(event.data.query ?? ''));
            const own = new URLSearchParams(location.search);
            ['backdrop', 'caption'].forEach(key => own.has(key) && query.set(key, own.get(key)!));
            window.parent.postMessage({ type: 'workbench-scenario', query: `?${query}` }, location.origin);
        }

        if (event.data?.type === 'workbench-page' || event.data?.type === 'workbench-sign-in-guide') {
            window.parent.postMessage(event.data, location.origin);
        }
    });
}

/** The functions .NET calls through JS interop as workbench.<name>, on the shell and in previews alike. */
host.workbench = {
    /** Marks the preview ready for screenshots and tells the shell, after a composition's inner screen is ready. */
    markReady() {
        if (screenOf() && isScreenReady === false) {
            isReadyPending = true;
            return;
        }

        host.__workbenchReady = true;

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

    /** Points a view's iframe at a new address without a history entry, unless it is already there. */
    showInFrame(paneId: string, url: string) {
        const frame = findFrame(paneId);
        const target = new URL(url, location.origin).href;

        if (!frame) return;

        shownUrls.set(frame, target);
        if (addressOf(frame) === target) return;

        expectNewDocument(frame);
        frame.contentWindow?.location.replace(target);
    },

    /** A view's saved page state for a screenshot, or null when the view doesn't answer in time. */
    async captureFrame(paneId: string): Promise<string | null> {
        try {
            return await withTimeout<string>(workbenchOf(findFrame(paneId))?.captureState() ?? Promise.resolve(null), 4000);
        } catch {
            return null;
        }
    },

    /** Puts a view's page state back to its defaults. */
    async resetFrame(paneId: string) {
        try {
            await withTimeout(workbenchOf(findFrame(paneId))?.resetState() ?? Promise.resolve(), 4000);
        } catch {
        }
    },

    /** This preview's page state as JSON, read from the inner screen when this page is a store composition. */
    captureState(): Promise<string | null> {
        const screen = workbenchOf(screenFrame());
        if (screen) return screen.captureState();

        return callPreview<string | null>(null, 'CaptureState');
    },

    /** Puts this preview's page state back to its defaults, in the inner screen when there is one. */
    resetState(): Promise<void> {
        const screen = workbenchOf(screenFrame());
        if (screen) return screen.resetState();

        return callPreview<void>(undefined, 'ResetState');
    },

    /** Records a scenario change made in the page: in the address bar when the page already shows it, else by a reload, and tells the shell. */
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

    /** Asks the shell to point at its user picker, from a view or from a page the shell renders itself on a phone. */
    askForSignInGuide() {
        if (window.parent !== window) {
            window.parent.postMessage({ type: 'workbench-sign-in-guide' }, location.origin);
        } else {
            callShell('ShowSignInGuide', null);
        }
    },

    /** Scrolls an element of the shell into sight, for a guide that points at it along with something else. */
    scrollIntoSight(selector: string) {
        document.querySelector(selector)?.scrollIntoView({ block: 'nearest' });
    },

    /** Marks the step the tour is on, or none when it ends, for the styles that lay out the shell for it on a phone. */
    setTourStep(stepId: string | null) {
        if (stepId) {
            document.documentElement.dataset.wbTour = stepId;
        } else {
            delete document.documentElement.dataset.wbTour;
        }
    },

    /** Tells the shell the view moved to another Workbench page. */
    reportPage(pageKey: string) {
        if (window.parent !== window) {
            window.parent.postMessage({ type: 'workbench-page', pageKey }, location.origin);
        }
    },

    /** Downloads a URL through a temporary link. */
    download(url: string) {
        const anchor = document.createElement('a');
        anchor.href = url;
        anchor.download = '';
        document.body.appendChild(anchor);
        anchor.click();
        anchor.remove();
    },

    runChecks() {
        return runChecks();
    },

    forceStates() {
        forceStates();
    },

    /** The resource entries whose text matches, for Edit text mode. */
    findTexts(text: string) {
        return callPreview<unknown[]>([], 'FindTexts', text);
    },

    /** The bit BlazorUI component whose root element has the id, with its parameters, for Inspect mode. */
    describeComponent(elementId: string) {
        return callPreview<unknown>(null, 'DescribeComponent', elementId);
    },

    /** Reads the component picked in Inspect mode again, once a change to its code reached the views. */
    inspectAgain() {
        inspectAgain();
    },

    /** Reloads every view and shows its loading state until it reports ready again. */
    reloadFrames() {
        document.querySelectorAll<HTMLIFrameElement>('iframe[data-pane-id]').forEach(frame => {
            expectNewDocument(frame);

            // A frame showing a page of another origin can't be reloaded from here, so it goes back to the view's address.
            try {
                frame.contentWindow?.location.reload();
            } catch {
                frame.contentWindow?.location.replace(shownUrls.get(frame) ?? frame.src);
            }
        });
    },

    highlight(selector: string) {
        return highlight(selector);
    },

    /** Runs the accessibility and theme checks inside a view, giving up after 30 seconds. */
    async checkFrame(paneId: string) {
        return await withTimeout(workbenchOf(findFrame(paneId))?.runChecks() ?? Promise.resolve(null), 30000);
    },

    /** Scrolls to and flashes an element in a view, for a check finding. */
    highlightInFrame(paneId: string, selector: string) {
        return workbenchOf(findFrame(paneId))?.highlight(selector) ?? false;
    },

    /** Gives Inspect mode the component name of each root class and the label for colors outside the theme. */
    setComponentNames(names: Record<string, string>, notFromTheme: string) {
        setComponentNames(names, notFromTheme);
    },

    setMode(mode: string) {
        setMode(mode);
    },

    /** Shows the comment pins of each view, keyed by pane id. */
    showPins(pins: Record<string, never[]>) {
        showPins(pins);
    },

    /** Waits for a click on the inline preview to start a comment there. */
    pickInline() {
        pickInline(anchor => callShell('BeginInlineComment', anchor));
    },

    cancelInlinePick() {
        cancelInlinePick();
    },

    /** Lets the user pick the spot a flow arrow starts from on a flow screen. */
    pickSpot(screenId: string) {
        return pickSpot(screenId);
    },

    cancelSpotPick() {
        cancelSpotPick();
    },

    /** Each theme token's resolved color as hex, with its opacity when it isn't opaque. */
    readTokens(names: string[]) {
        const probe = document.createElement('span');
        probe.style.display = 'none';
        document.body.appendChild(probe);

        const values: Record<string, string> = {};

        names.forEach(name => {
            probe.style.color = '';
            probe.style.color = `var(${name})`;
            const color = parseColor(getComputedStyle(probe).color);
            const hex = color?.slice(0, 3).map(part => Math.min(255, Math.max(0, Math.round(part * 255))).toString(16).padStart(2, '0')).join('');
            values[name] = color ? `#${hex}${color[3] < 1 ? ` · ${Math.round(color[3] * 100)}%` : ''}` : '';
        });

        probe.remove();

        return values;
    },

    /** The theme's breakpoints, read from bit BlazorUI's --bit-bp-* variables. */
    readBreakpoints() {
        const style = getComputedStyle(document.documentElement);

        return ['xs', 'sm', 'md', 'lg', 'xl', 'xxl']
            .map(name => ({ name, min: parseFloat(style.getPropertyValue(`--bit-bp-${name}`)) }))
            .filter(breakpoint => Number.isFinite(breakpoint.min));
    }
};

// A preview takes its culture from its query string, so its circuit is told the culture; the shell uses the cookie.
const culture = isPreview ? document.documentElement.lang : null;

// blazor.web.js loads with autostart off, so Blazor starts here, once the workbench object exists.
host.Blazor.start({
    circuit: {
        configureSignalR: (builder: any) => {
            if (culture) {
                builder.withUrl(`_blazor?culture=${encodeURIComponent(culture)}`);
            }
        }
    }
});
