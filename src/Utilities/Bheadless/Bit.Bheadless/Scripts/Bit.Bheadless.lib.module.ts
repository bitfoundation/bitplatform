// Bit.Bheadless - JS initializer.
//
// Blazor automatically loads `{AssemblyName}.lib.module.js` from every Razor
// class library and calls these hooks at startup. Importing the runtime here
// installs the global, declarative behaviours ([data-bit-bh-keys] key handling,
// dismissable layers, interaction attributes) before any component renders,
// so consumers never need a <script> tag. The hooks also register the library's
// own DOM events with Blazor (see BitBhEventHandlers in C#).

type RuntimeModule = typeof import('./bit-bheadless.js');

interface CustomEventTypeOptions {
    browserEventName: string;
    createEventArgs: (event: Event) => unknown;
}

interface BlazorInstance {
    addEventListener?(name: 'enhancedload', callback: () => void): void;
    registerCustomEventType?(eventName: string, options: CustomEventTypeOptions): void;
}

// What owns an event in a host of content of its own (a grid cell, a tree item): the host, or interactive
// content nested in it. A label counts as interactive, since it activates its control.
const OWNER_ATTRIBUTE = 'data-bit-bh-event-owner';
const INTERACTIVE = [
    'a[href]', 'area[href]', 'button', 'input', 'select', 'textarea', 'label', 'summary', 'iframe',
    'audio[controls]', 'video[controls]', '[contenteditable]:not([contenteditable="false"])', '[tabindex]',
    '[role="button"]', '[role="link"]', '[role="checkbox"]', '[role="switch"]', '[role="radio"]', '[role="slider"]',
    '[role="spinbutton"]', '[role="textbox"]', '[role="combobox"]', '[role="menuitem"]', '[role="option"]', '[role="tab"]',
].join(',');

/** The id of the nearest owner around the event's target, or '' when interactive content nested in it comes first. */
function eventOwnerId(target: EventTarget | null) {
    for (let el = target instanceof Element ? target : null; el; el = el.parentElement) {
        if (el.hasAttribute(OWNER_ATTRIBUTE)) return el.id;
        if (el.matches(INTERACTIVE)) return '';
    }
    return '';
}

let eventsRegistered = false;

function registerEvents(blazor?: BlazorInstance) {
    if (eventsRegistered || !blazor?.registerCustomEventType) return;
    eventsRegistered = true;

    blazor.registerCustomEventType('bitbhkeydown', {
        browserEventName: 'keydown',
        createEventArgs: (event) => {
            const e = event as KeyboardEvent;
            return {
                key: e.key, code: e.code, location: e.location, repeat: e.repeat,
                ctrlKey: e.ctrlKey, shiftKey: e.shiftKey, altKey: e.altKey, metaKey: e.metaKey,
                isComposing: e.isComposing, type: e.type,
                // An IME composition's keys belong to the composition, never to a host.
                ownerId: e.isComposing || e.keyCode === 229 ? '' : eventOwnerId(e.target),
            };
        },
    });

    blazor.registerCustomEventType('bitbhclick', {
        browserEventName: 'click',
        createEventArgs: (event) => {
            const e = event as MouseEvent;
            return {
                detail: e.detail, screenX: e.screenX, screenY: e.screenY, clientX: e.clientX, clientY: e.clientY,
                offsetX: e.offsetX, offsetY: e.offsetY, pageX: e.pageX, pageY: e.pageY,
                movementX: e.movementX, movementY: e.movementY, button: e.button, buttons: e.buttons,
                ctrlKey: e.ctrlKey, shiftKey: e.shiftKey, altKey: e.altKey, metaKey: e.metaKey, type: e.type,
                ownerId: eventOwnerId(e.target),
            };
        },
    });
}

let runtime: Promise<RuntimeModule | undefined> | undefined;

function load() {
    // A failed import is not cached: the next start hook (or the interop's own import) tries again.
    runtime ??= import('./bit-bheadless.js')
        .then((m) => { m.install(); return m; })
        .catch((error) => { runtime = undefined; console.error('[Bit.Bheadless] failed to load the runtime.', error); return undefined; });
    return runtime;
}

// Blazor Web App (.NET 8+). The events are registered before anything is awaited, so they are in place
// before the first interactive render attaches handlers to them.
export async function afterWebStarted(blazor?: BlazorInstance) {
    registerEvents(blazor);
    const m = await load();
    if (m) blazor?.addEventListener?.('enhancedload', m.onEnhancedLoad);
}

// Blazor Server / WebAssembly standalone / BlazorWebView
export async function afterStarted(blazor?: BlazorInstance) {
    registerEvents(blazor);
    await load();
}
