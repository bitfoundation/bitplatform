/*!
 * Bit.Balette browser layer.
 *
 * This module does only what the DOM alone can do: open and close the native <dialog>, move focus in
 * and out of it, scroll the active row into view, detect the platform, and listen for keys. Matching,
 * ranking, paging and every other decision stays in C#.
 *
 * Global shortcuts are matched here rather than in .NET: the registered chords are synced as
 * normalized strings ('mod+k', 'g i'), and only a key press that completes one of them crosses the
 * interop boundary, so typing anywhere on the page never produces a stream of round trips under
 * Blazor Server. Inside the dialog, the navigation keys are claimed with preventDefault before .NET
 * sees them, because Blazor's own preventDefault is all-or-nothing and, over SignalR, too late: Tab
 * would already have left the dialog and Backspace would already have deleted text.
 */

// ---------------------------------------------------------------------------
// Contracts shared with the C# interop
// ---------------------------------------------------------------------------

interface DotNetObjectReference {
    invokeMethodAsync(method: string, ...args: unknown[]): Promise<unknown>;
}

/** The options Balette.razor.cs sends to `init` (camelCase JSON of an anonymous object). */
interface BaletteInitOptions {
    toggleShortcuts?: string[] | null;
    commandShortcuts?: string[] | null;
    suppressInEditable?: boolean;
    sequenceTimeout?: number;
    vimBindings?: boolean;
}

/** The handle `init` returns; .NET holds it as an IJSObjectReference and calls its members. */
interface BaletteHandle {
    isMac(): boolean;
    open(input: HTMLInputElement | null, selectAll: boolean): void;
    close(): void;
    focusInput(input: HTMLInputElement | null): void;
    scrollIntoView(list: HTMLElement | null, id: string): void;
    setCommandShortcuts(list: string[] | null): void;
    setToggleShortcuts(list: string[] | null): void;
    setVimBindings(enabled: boolean): void;
    afterPaint(): Promise<void>;
    openInNewTab(url: string): void;
    dispose(): void;
}

// ---------------------------------------------------------------------------
// Key handling helpers
// ---------------------------------------------------------------------------

const NAV_KEYS = new Set(['ArrowDown', 'ArrowUp', 'Home', 'End', 'PageDown', 'PageUp', 'Enter', 'Tab', 'Escape']);
const MOD_KEYS = new Set(['Control', 'Shift', 'Alt', 'Meta', 'OS']);

function detectMac(): boolean {
    const uaData = (navigator as Navigator & { userAgentData?: { platform?: string } }).userAgentData;
    if (uaData && uaData.platform) {
        return /mac|iphone|ipad|ipod/i.test(uaData.platform);
    }
    return /mac|iphone|ipad|ipod/i.test(navigator.platform || navigator.userAgent || '');
}

function isEditable(el: EventTarget | null): boolean {
    if (!(el instanceof Element)) return false;
    const tag = el.tagName;
    if (tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT') return true;
    return (el as HTMLElement).isContentEditable === true;
}

/**
 * Turns a KeyboardEvent into the chord strings it can stand for: the literal one ('ctrl+k') and, when
 * the platform's primary modifier is held, the 'mod' variant ('mod+k') - the same canonical form
 * BaletteKeyChord.Normalized writes on the C# side.
 */
function chordsFor(e: KeyboardEvent, isMac: boolean): string[] {
    const raw = e.key;
    if (!raw) return [];
    if (MOD_KEYS.has(raw)) return [];
    const key = raw.toLowerCase();

    const parts: string[] = [];
    if (e.ctrlKey) parts.push('ctrl');
    if (e.altKey) parts.push('alt');
    if (e.shiftKey) parts.push('shift');
    if (e.metaKey) parts.push('meta');

    const result = [[...parts, key].join('+')];

    // The 'mod' variant: Meta on macOS, Ctrl everywhere else.
    const modPressed = isMac ? e.metaKey : e.ctrlKey;
    if (modPressed) {
        const modParts = ['mod'];
        if (isMac && e.ctrlKey) modParts.push('ctrl');
        if (e.altKey) modParts.push('alt');
        if (e.shiftKey) modParts.push('shift');
        if (!isMac && e.metaKey) modParts.push('meta');
        result.push([...modParts, key].join('+'));
    }

    // A shifted single character also matches without its shift: a shortcut written "?" fires on
    // Shift+/ on a US layout without the author having to know that.
    if (e.shiftKey && key.length === 1 && !e.ctrlKey && !e.metaKey && !e.altKey) {
        result.push(key);
    }

    return result;
}

// ---------------------------------------------------------------------------
// Entry point
// ---------------------------------------------------------------------------

export function init(dialog: HTMLDialogElement, dotnet: DotNetObjectReference, options: BaletteInitOptions): BaletteHandle {
    const isMac = detectMac();
    let toggleShortcuts = new Set(options.toggleShortcuts || []);
    let commandShortcuts = new Set(options.commandShortcuts || []);
    const suppressInEditable = options.suppressInEditable !== false;
    const sequenceTimeout = options.sequenceTimeout || 1000;
    let vimBindings = options.vimBindings === true;

    // Each entry holds the alternative chord strings of one key press (see chordsFor).
    let buffer: string[][] = [];
    let bufferTimer: ReturnType<typeof setTimeout> | null = null;
    let previouslyFocused: Element | null = null;
    let inputEl: HTMLInputElement | null = null;
    let disposed = false;

    const allShortcuts = () => [...toggleShortcuts, ...commandShortcuts];

    /** In-palette chords (Ctrl+N/P, Vim Ctrl+J/K, Mod+Shift+P to pin) win over a toggle bound to the same keys. */
    function isReservedInside(e: KeyboardEvent): boolean {
        const lower = e.key.length === 1 ? e.key.toLowerCase() : e.key;
        const plainCtrl = e.ctrlKey && !e.shiftKey && !e.altKey && !e.metaKey;
        if (plainCtrl && (lower === 'n' || lower === 'p')) return true;
        if (vimBindings && plainCtrl && (lower === 'j' || lower === 'k')) return true;
        if (lower === 'p' && e.shiftKey && !e.altKey && (isMac ? e.metaKey : e.ctrlKey)) return true;
        return false;
    }

    function resetBuffer(): void {
        buffer = [];
        if (bufferTimer) {
            clearTimeout(bufferTimer);
            bufferTimer = null;
        }
    }

    /** Matches the tail of the buffer against every registered sequence, and says whether a longer one could still match. */
    function matchBuffer(): { matched: string | null; prefixPossible: boolean } {
        let prefixPossible = false;
        for (const seq of allShortcuts()) {
            const chords = seq.split(' ');
            if (chords.length > buffer.length) {
                let ok = true;
                for (let i = 0; i < buffer.length; i++) {
                    if (!buffer[i].includes(chords[i])) { ok = false; break; }
                }
                if (ok) prefixPossible = true;
                continue;
            }

            const offset = buffer.length - chords.length;
            let ok = true;
            for (let i = 0; i < chords.length; i++) {
                if (!buffer[offset + i].includes(chords[i])) { ok = false; break; }
            }
            if (ok) {
                return { matched: seq, prefixPossible };
            }
        }
        return { matched: null, prefixPossible };
    }

    function onDocumentKeyDown(e: KeyboardEvent): void {
        if (disposed || e.defaultPrevented || e.isComposing) return;
        const alts = chordsFor(e, isMac);
        if (alts.length === 0) return;

        // While the palette is open only the single-chord toggles are global; the dialog's own
        // handler owns every other key.
        if (dialog.open) {
            if (isReservedInside(e)) return;
            for (const t of toggleShortcuts) {
                if (!t.includes(' ') && alts.includes(t)) {
                    e.preventDefault();
                    dotnet.invokeMethodAsync('OnToggleShortcut');
                    return;
                }
            }
            return;
        }

        // Plain keys never fire from an editable element - typing "g" in a text box is typing - but
        // chords with a modifier still do.
        const hasModifier = e.ctrlKey || e.metaKey || e.altKey;
        if (isEditable(e.target) && suppressInEditable && !hasModifier) {
            resetBuffer();
            return;
        }

        buffer.push(alts);
        if (bufferTimer) clearTimeout(bufferTimer);
        bufferTimer = setTimeout(resetBuffer, sequenceTimeout);

        const { matched, prefixPossible } = matchBuffer();
        if (matched) {
            e.preventDefault();
            e.stopPropagation();
            resetBuffer();
            if (toggleShortcuts.has(matched)) {
                dotnet.invokeMethodAsync('OnToggleShortcut');
            } else {
                dotnet.invokeMethodAsync('OnGlobalShortcut', matched);
            }
            return;
        }
        if (!prefixPossible) {
            resetBuffer();
        }
    }

    function onDialogKeyDown(e: KeyboardEvent): void {
        if (disposed || e.isComposing) return;
        const key = e.key;
        const ctrl = e.ctrlKey, shift = e.shiftKey, alt = e.altKey, meta = e.metaKey;
        const lower = key.length === 1 ? key.toLowerCase() : key;
        const emptyQuery = inputEl !== null && inputEl.value.length === 0;

        let handled = false;
        if (NAV_KEYS.has(key)) {
            handled = true;
        } else if (ctrl && !shift && !alt && !meta && (lower === 'n' || lower === 'p')) {
            handled = true;
        } else if (vimBindings && ctrl && !shift && !alt && !meta && (lower === 'j' || lower === 'k')) {
            handled = true;
        } else if (key === 'Backspace' && emptyQuery) {
            handled = true;
        } else if ((key === 'ArrowLeft' || key === 'ArrowRight') && emptyQuery) {
            handled = true;
        } else if (key === ' ' && emptyQuery && dialog.dataset.multiselect === 'true') {
            handled = true;
        } else if (key === 'Delete' && shift) {
            handled = true;
        } else if (lower === 'p' && shift && (isMac ? meta : ctrl)) {
            handled = true;
        }

        if (!handled) return;
        e.preventDefault();
        e.stopPropagation();
        dotnet.invokeMethodAsync('OnKeyDown', key, ctrl, shift, alt, meta);
    }

    // Escape is handled by .NET (cancel a confirmation, then go back a page, then close), so the
    // dialog's own close-on-Escape is cancelled.
    function onCancel(e: Event): void {
        e.preventDefault();
    }

    // A click whose target is the dialog element itself landed on the ::backdrop.
    function onDialogClick(e: MouseEvent): void {
        if (e.target === dialog) {
            dotnet.invokeMethodAsync('OnBackdropClick');
        }
    }

    function onPointerMove(): void {
        dotnet.invokeMethodAsync('OnPointerMoved');
    }

    document.addEventListener('keydown', onDocumentKeyDown, true);
    dialog.addEventListener('keydown', onDialogKeyDown);
    dialog.addEventListener('cancel', onCancel);
    dialog.addEventListener('click', onDialogClick);
    dialog.addEventListener('pointermove', onPointerMove, { passive: true });

    return {
        isMac() {
            return isMac;
        },

        open(input, selectAll) {
            inputEl = input instanceof HTMLInputElement ? input : null;
            previouslyFocused = document.activeElement;
            if (!dialog.open) {
                try {
                    dialog.showModal();
                } catch {
                    dialog.setAttribute('open', '');
                }
            }
            document.documentElement.classList.add('bit-balette-scroll-lock');
            if (inputEl) {
                inputEl.focus({ preventScroll: true });
                if (selectAll) {
                    // A query left from the last time is selected, so typing replaces it.
                    inputEl.select();
                } else {
                    // A query passed in on purpose (often a mode prefix such as '>') is kept: typing goes after it.
                    const len = inputEl.value.length;
                    try { inputEl.setSelectionRange(len, len); } catch { /* not a text input */ }
                }
            }
        },

        close() {
            if (dialog.open) {
                dialog.close();
            }
            document.documentElement.classList.remove('bit-balette-scroll-lock');
            if (previouslyFocused instanceof HTMLElement || previouslyFocused instanceof SVGElement) {
                try { previouslyFocused.focus({ preventScroll: true }); } catch { /* the element is gone */ }
            }
            previouslyFocused = null;
        },

        focusInput(input) {
            // An element reference .NET captured before the input was rendered arrives as something else entirely.
            if (input instanceof HTMLInputElement) inputEl = input;
            if (inputEl && inputEl.isConnected) {
                inputEl.focus({ preventScroll: true });
                const len = inputEl.value.length;
                try { inputEl.setSelectionRange(len, len); } catch { /* not a text input */ }
            }
        },

        scrollIntoView(list, id) {
            const el = document.getElementById(id);
            if (!el) return;
            // Keep the group heading above the first item of a group in view as well.
            const prev = el.previousElementSibling;
            const anchor = prev && prev.getAttribute('role') === 'presentation' ? prev : el;
            if (anchor !== el && list) {
                const r = list.getBoundingClientRect();
                const a = anchor.getBoundingClientRect();
                if (a.top < r.top) {
                    anchor.scrollIntoView({ block: 'nearest' });
                    return;
                }
            }
            el.scrollIntoView({ block: 'nearest' });
        },

        setCommandShortcuts(list) {
            commandShortcuts = new Set(list || []);
        },

        setToggleShortcuts(list) {
            toggleShortcuts = new Set(list || []);
        },

        setVimBindings(enabled) {
            vimBindings = enabled === true;
        },

        /** Resolves once the browser has had a chance to paint, or after 100 ms when frames are throttled. */
        afterPaint() {
            return new Promise<void>(resolve => {
                const fallback = setTimeout(resolve, 100);
                requestAnimationFrame(() => setTimeout(() => {
                    clearTimeout(fallback);
                    resolve();
                }, 0));
            });
        },

        openInNewTab(url) {
            window.open(url, '_blank', 'noopener,noreferrer');
        },

        dispose() {
            disposed = true;
            document.removeEventListener('keydown', onDocumentKeyDown, true);
            dialog.removeEventListener('keydown', onDialogKeyDown);
            dialog.removeEventListener('cancel', onCancel);
            dialog.removeEventListener('click', onDialogClick);
            dialog.removeEventListener('pointermove', onPointerMove);
            document.documentElement.classList.remove('bit-balette-scroll-lock');
            resetBuffer();
        },
    };
}
