// Text control bridge for the collaborative input components.
//
// This is the only JavaScript in bit Bcrdt. It exists because two things a collaborative text control needs are not
// available to .NET: the caret position, and a way to update a control's value without overwriting what the user
// typed a moment ago. The CRDT engine, the synchronization protocol, the presence system and every data structure
// are pure C# and never call into here.
//
// Positions are UTF-16 code units throughout, which is what both the browser's selection API and .NET strings use.

type TextControl = HTMLTextAreaElement | HTMLInputElement;

interface DotNetReference {
    invokeMethodAsync(method: string, ...args: unknown[]): Promise<unknown>;
}

interface ControlState {
    ref: DotNetReference;
    generation: number;
    start: number;
    end: number;
    backward: boolean;
    applied: number;
    composing: boolean;
    onInput: (event?: Event) => void;
    onSelection: () => void;
    onBlur: () => void;
    onCompositionStart: () => void;
    onCompositionEnd: () => void;
}

const states = new WeakMap<TextControl, ControlState>();
const selectionEvents = ['select', 'keyup', 'mouseup', 'focus'];

// Starts reporting a control's input and selection to .NET.
// `generation` identifies which text field the control is bound to; every report carries it, so .NET can discard
// reports meant for a field it has since been rebound away from. `value` is what .NET rendered into the control. If
// the control holds something else, the user typed between the render and now; that is reported as input rather than
// overwritten.
export function attach(element: TextControl | null, dotnetRef: DotNetReference, generation: number, value: string): void {
    if (!element) return;
    detach(element);

    const send = (method: string, ...args: unknown[]) => {
        try {
            const pending = state.ref.invokeMethodAsync(method, state.generation, ...args);
            if (pending && typeof pending.catch === 'function') pending.catch(() => { });
        } catch {
            // The .NET side has gone away; there is nobody left to tell.
        }
    };

    const read = () => {
        state.start = element.selectionStart ?? 0;
        state.end = element.selectionEnd ?? 0;
        state.backward = element.selectionDirection === 'backward';
    };

    // `applied` is the identifier of the last update from .NET this control applied. It travels with every report, so
    // .NET can settle an update whose answer was lost on the way back.
    const state: ControlState = {
        ref: dotnetRef,
        generation,
        start: -1,
        end: -1,
        backward: false,
        applied: 0,
        composing: false,

        onInput: (event?: Event) => {
            // While an input method is composing, the value holds provisional text that the next keystroke may
            // replace. The settled value is reported when the composition ends.
            if (state.composing || (event && (event as InputEvent).isComposing)) return;
            read();
            send('OnControlInput', element.value, state.start, state.end, state.backward, state.applied);
        },

        onSelection: () => {
            const start = element.selectionStart ?? 0;
            const end = element.selectionEnd ?? 0;
            const backward = element.selectionDirection === 'backward';
            if (start === state.start && end === state.end && backward === state.backward) return;
            read();
            send('OnControlSelectionChanged', state.start, state.end, state.backward, state.applied);
        },

        onBlur: () => {
            state.start = -1;
            state.end = -1;
            send('OnControlBlur');
        },

        onCompositionStart: () => {
            state.composing = true;
        },

        onCompositionEnd: () => {
            state.composing = false;
            // Some browsers follow this with an input event and some do not; reporting here covers both, and a
            // repeated report of the same value changes nothing.
            state.onInput();
        },
    };

    element.addEventListener('input', state.onInput);
    for (const type of selectionEvents) element.addEventListener(type, state.onSelection);
    element.addEventListener('blur', state.onBlur);
    element.addEventListener('compositionstart', state.onCompositionStart);
    element.addEventListener('compositionend', state.onCompositionEnd);

    states.set(element, state);

    if (element.value !== value) {
        // Reported after this call returns, so .NET is not re-entered while it waits on it.
        queueMicrotask(() => {
            if (states.get(element) === state) state.onInput();
        });
    }
}

// Rebinds an attached control to a different text field and shows that field's value. Unlike attaching, this
// replaces the content: whatever was there belonged to the previous field.
export function reset(element: TextControl | null, generation: number, value: string): void {
    const state = element ? states.get(element) : undefined;
    if (!element || !state) return;
    state.generation = generation;
    state.applied = 0;
    element.value = value;
}

// Replaces the control's value - but only if it still holds `expected` and no input method is composing. Otherwise
// the user has typed since .NET computed the update, or is about to; that input reaches .NET first, so the update is
// refused rather than allowed to overwrite it. `id` is recorded when the update is applied (or found already in
// place) and reported back with every later input.
export function setValue(element: TextControl | null, generation: number, expected: string, value: string, start: number, end: number, id: number): boolean {
    const state = element ? states.get(element) : undefined;
    if (!element || !state || state.generation !== generation) return false;
    if (state.composing) return false;
    if (element.value !== expected) return false;

    if (element.value !== value) {
        const focused = document.activeElement === element;
        element.value = value;
        if (focused) {
            const length = value.length;
            const from = Math.max(0, Math.min(start, length));
            const to = Math.max(from, Math.min(end, length));
            element.setSelectionRange(from, to, state.backward ? 'backward' : 'forward');
            state.start = from;
            state.end = to;
        }
    }

    state.applied = id;
    return true;
}

// The identifier of the last update the control applied, for settling one whose answer was lost. Zero if it has
// applied none; -1 if the control is no longer bound to `generation`.
export function appliedUpdate(element: TextControl | null, generation: number): number {
    const state = element ? states.get(element) : undefined;
    if (!state || state.generation !== generation) return -1;
    return state.applied;
}

// Stops reporting a control's input. Safe to call for a control that was never attached.
export function detach(element: TextControl | null): void {
    const state = element ? states.get(element) : undefined;
    if (!element || !state) return;

    element.removeEventListener('input', state.onInput);
    for (const type of selectionEvents) element.removeEventListener(type, state.onSelection);
    element.removeEventListener('blur', state.onBlur);
    element.removeEventListener('compositionstart', state.onCompositionStart);
    element.removeEventListener('compositionend', state.onCompositionEnd);
    states.delete(element);
}

export function getSelection(element: TextControl | null): { start: number; end: number } | null {
    if (!element) return null;
    return { start: element.selectionStart ?? 0, end: element.selectionEnd ?? 0 };
}

export function setSelection(element: TextControl | null, start: number, end?: number | null): void {
    if (!element) return;
    // Guard against a caret mapped past the end after a concurrent deletion.
    const length = element.value ? element.value.length : 0;
    const from = Math.max(0, Math.min(start, length));
    const to = Math.max(0, Math.min(end ?? start, length));
    element.setSelectionRange(from, to);
}

export function setValuePreservingSelection(element: TextControl | null, value: string): void {
    if (!element) return;
    // Assigning `value` resets the caret to the end, so capture and restore it around the write.
    const start = element.selectionStart;
    const end = element.selectionEnd;
    element.value = value;
    if (start !== null && start !== undefined) {
        const length = value.length;
        element.setSelectionRange(Math.min(start, length), Math.min(end ?? start, length));
    }
}

export function focus(element: TextControl | null): void {
    if (element) element.focus();
}

// The longhand properties copied onto the mirror: computed shorthands such as `padding` are not reliably serialized.
const mirroredProperties = [
    'fontFamily', 'fontSize', 'fontWeight', 'fontStyle', 'fontVariant', 'letterSpacing',
    'wordSpacing', 'lineHeight', 'textTransform', 'textIndent', 'tabSize',
    'paddingTop', 'paddingRight', 'paddingBottom', 'paddingLeft'
] as const;

// Reports the pixel rectangle of a character index, so remote cursors can be drawn over a textarea. Uses a mirrored,
// invisible div because textareas expose no per-character geometry. Coordinates are relative to the textarea's
// padding box, adjusted for its scroll position.
export function measureCaret(element: TextControl | null, index: number): { top: number; left: number; height: number } | null {
    if (!element) return null;

    const style = window.getComputedStyle(element);
    const mirror = document.createElement('div');

    for (const property of mirroredProperties) {
        mirror.style[property] = style[property];
    }

    // Wrap at the same width the textarea's text does. clientWidth is the padding box without the border or a vertical
    // scrollbar, so the mirror's lines break exactly where the textarea's do even when it is scrolled.
    mirror.style.boxSizing = 'border-box';
    mirror.style.border = '0';
    mirror.style.width = `${element.clientWidth}px`;
    mirror.style.position = 'absolute';
    mirror.style.visibility = 'hidden';
    mirror.style.whiteSpace = 'pre-wrap';
    mirror.style.overflowWrap = 'break-word';

    const value = element.value ?? '';
    mirror.textContent = value.slice(0, index);

    const marker = document.createElement('span');
    marker.textContent = value.slice(index, index + 1) || '.';
    mirror.appendChild(marker);

    document.body.appendChild(mirror);
    const top = marker.offsetTop;
    const left = marker.offsetLeft;
    const height = marker.offsetHeight;
    document.body.removeChild(mirror);

    return { top: top - element.scrollTop, left: left - element.scrollLeft, height };
}
