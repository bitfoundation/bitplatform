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

/** The handle `init` returns; .NET holds it as an IJSObjectReference and calls its members. */
interface BaletteHandle {
    isMac(): boolean;
    open(input: HTMLInputElement | null, selectAll: boolean): void;
    close(): void;
    focusInput(input: HTMLInputElement | null): void;
    scrollIntoView(list: HTMLElement | null, id: string, position?: number, count?: number): void;
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

/**
 * True while an input method editor is composing: the key belongs to the composition (a Japanese, Chinese or Korean
 * candidate being picked), not to a shortcut. Safari reports the Enter that commits a composition with isComposing
 * false but keyCode 229, so both are checked.
 */
function isComposing(e: KeyboardEvent): boolean {
    return e.isComposing || e.keyCode === 229;
}

/**
 * The key a chord is matched on. A single character outside ASCII comes from a non-Latin layout - Ctrl+K types 'л' on
 * a Russian one, 'ن' on a Persian one - or from Option/AltGr composing one ('Ω' for Option+Z): the physical letter or
 * digit key it was typed on stands for it as well, the way VS Code dispatches shortcuts by KeyboardEvent.code.
 */
function keysFor(e: KeyboardEvent): string[] {
    // Space is written out, as on the C# side: a literal ' ' is what separates the chords of a sequence.
    const key = e.key === ' ' ? 'space' : e.key.toLowerCase();
    const keys = [key];
    const composed = key.length === 1 && key.charCodeAt(0) > 127;
    // AltGr (reported on Windows as Ctrl+Alt) types a character - '@' on a German layout, 'ś' on a Polish one - and is
    // never a shortcut's modifier: its physical key must not stand in for what it typed.
    if (!isAltGraph(e) && (composed || (e.altKey && !/^[a-z0-9]$/.test(key)))) {
        const physical = physicalKey(e.code);
        if (physical && physical !== key) keys.push(physical);
    }
    return keys;
}

function isAltGraph(e: KeyboardEvent): boolean {
    return typeof e.getModifierState === 'function' && e.getModifierState('AltGraph');
}

/**
 * A character typed with AltGr, or with Option/Alt alone ('Ω' for Option+Z on a Mac, '@' for Option+L on a German one):
 * typing, when it lands in a text field, rather than a chord.
 */
function typesWithAlt(e: KeyboardEvent): boolean {
    if (isAltGraph(e)) return true;
    return e.altKey && !e.ctrlKey && !e.metaKey && e.key.length === 1 && !/^[a-z0-9]$/i.test(e.key);
}

/** The letter of a key pressed with Ctrl or ⌘ inside the dialog, read through the layout like keysFor. */
function chordLetter(e: KeyboardEvent): string {
    const keys = keysFor(e);
    return keys[keys.length - 1];
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
    const key = raw === ' ' ? 'space' : raw.toLowerCase();
    const keys = keysFor(e);

    const parts: string[] = [];
    if (e.ctrlKey) parts.push('ctrl');
    if (e.altKey) parts.push('alt');
    if (e.shiftKey) parts.push('shift');
    if (e.metaKey) parts.push('meta');

    // The 'mod' variant: Meta on macOS, Ctrl everywhere else.
    const modPressed = isMac ? e.metaKey : e.ctrlKey;
    const modParts = ['mod'];
    if (isMac && e.ctrlKey) modParts.push('ctrl');
    if (e.altKey) modParts.push('alt');
    if (e.shiftKey) modParts.push('shift');
    if (!isMac && e.metaKey) modParts.push('meta');

    const result: string[] = [];
    for (const k of keys) {
        result.push([...parts, k].join('+'));
        if (modPressed) result.push([...modParts, k].join('+'));
    }

    // A shifted single character also matches without its shift: a shortcut written "?" fires on
    // Shift+/ on a US layout without the author having to know that.
    if (e.shiftKey && key.length === 1 && !e.ctrlKey && !e.metaKey && !e.altKey) {
        result.push(key);
    }

    return result;
}

/** The key a letter or digit key ('KeyZ', 'Digit1') stands for whatever it types, or null for any other key. */
function physicalKey(code: string): string | null {
    const m = /^(?:Key([A-Z])|Digit([0-9]))$/.exec(code || '');
    return m ? (m[1] || m[2]).toLowerCase() : null;
}

/**
 * Scrolls the list just enough to show the element, with the group heading above a group's first item. Headings are
 * sticky, and a sticky element's box is where it is stuck rather than where it belongs, so a group's first item is
 * revealed by the top of its group (role="group", which holds the heading) - or, in a virtualized list whose headings
 * are plain siblings of the items, by the heading itself.
 */
function reveal(list: HTMLElement | null, el: Element): void {
    const prev = el.previousElementSibling;
    if (list && prev && prev.getAttribute('role') === 'presentation') {
        const parent = el.parentElement;
        const top = parent && parent.getAttribute('role') === 'group' ? parent : prev;
        const listTop = list.getBoundingClientRect().top;
        const groupTop = top.getBoundingClientRect().top;
        if (groupTop < listTop) {
            if (top === parent) {
                list.scrollTop -= listTop - groupTop;
            } else {
                top.scrollIntoView({ block: 'nearest' });
            }
            return;
        }
    }
    el.scrollIntoView({ block: 'nearest' });
}

/** Scrolls the list to where row `position` of `count` lies, estimated from the list's height. */
function scrollToEstimate(list: HTMLElement, position: number, count: number): void {
    const style = getComputedStyle(list);
    const padTop = parseFloat(style.paddingTop) || 0;
    const content = list.scrollHeight - padTop - (parseFloat(style.paddingBottom) || 0);
    const height = content / count;
    const top = padTop + height * position;
    if (top < list.scrollTop) {
        list.scrollTop = top;
    } else if (top + height > list.scrollTop + list.clientHeight) {
        list.scrollTop = top + height - list.clientHeight;
    }
}

// ---------------------------------------------------------------------------
// Entry point
// ---------------------------------------------------------------------------

/**
 * Called by Balette.razor.cs with plain values rather than one options object, whose shape the .NET serializer
 * would only find through reflection - which a trimmed app no longer has the metadata for.
 * `sequenceTimeout` is BaletteOptions.SequenceTimeoutMs, always positive.
 */
export function init(dialog: HTMLElement, dotnet: DotNetObjectReference, toggles: string[] | null, commands: string[] | null,
    suppressInEditable: boolean, sequenceTimeout: number, vim: boolean, inline?: boolean): BaletteHandle {
    const isMac = detectMac();
    // An inline palette is a section of the page, always shown: no modal to open or close, no backdrop, and no global
    // keys - the page's modal palette owns those.
    const isInline = inline === true;
    const modal = dialog as HTMLDialogElement;
    let toggleShortcuts = new Set(toggles || []);
    let commandShortcuts = new Set(commands || []);
    let vimBindings = vim === true;

    // Each entry holds the alternative chord strings of one key press (see chordsFor).
    let buffer: string[][] = [];
    let bufferTimer: ReturnType<typeof setTimeout> | null = null;
    let previouslyFocused: Element | null = null;
    let inputEl: HTMLInputElement | null = null;
    let disposed = false;
    let reportedSuspension: string | null = null;
    let scrollRequest = 0;

    const allShortcuts = () => [...toggleShortcuts, ...commandShortcuts];

    /** In-palette chords (Ctrl+N/P, Vim Ctrl+J/K, Mod+Shift+P to pin) win over a toggle bound to the same keys. */
    function isReservedInside(e: KeyboardEvent): boolean {
        const lower = e.key.length === 1 ? chordLetter(e) : e.key;
        const plainCtrl = e.ctrlKey && !e.shiftKey && !e.altKey && !e.metaKey;
        if (plainCtrl && (lower === 'n' || lower === 'p')) return true;
        if (vimBindings && plainCtrl && (lower === 'j' || lower === 'k')) return true;
        if (lower === 'p' && e.shiftKey && !e.altKey && (isMac ? e.metaKey : e.ctrlKey)) return true;
        return false;
    }

    // The chords of a sequence pressed so far, as .NET was last told of them; null when no sequence is pending.
    let reportedPending: string | null = null;

    /** Tells .NET which chords of a sequence are pending, or that none are - only when that changes. */
    function reportPending(chords: string | null): void {
        if (chords === reportedPending) return;
        reportedPending = chords;
        dotnet.invokeMethodAsync('OnSequencePending', chords);
    }

    function resetBuffer(): void {
        buffer = [];
        if (bufferTimer) {
            clearTimeout(bufferTimer);
            bufferTimer = null;
        }
        reportPending(null);
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

    /** A single-chord shortcut among the alternatives of one key press. */
    function singleChordMatch(alts: string[], shortcuts: Iterable<string>): string | null {
        for (const s of shortcuts) {
            if (!s.includes(' ') && alts.includes(s)) return s;
        }
        return null;
    }

    function onDocumentKeyDown(e: KeyboardEvent): void {
        if (disposed || e.defaultPrevented || isComposing(e)) return;
        const alts = chordsFor(e, isMac);
        if (alts.length === 0) return;

        // A held key repeats its keydown: a held Ctrl+K would open and close the palette many times a second, and a held
        // shortcut would run its command as often. The repeats are still claimed, so the browser does not act on them
        // either, but nothing runs and no sequence advances.
        if (e.repeat) {
            const claimed = modal.open
                ? singleChordMatch(alts, toggleShortcuts)
                : singleChordMatch(alts, allShortcuts());
            if (claimed && !(modal.open && isReservedInside(e))) {
                e.preventDefault();
            }
            return;
        }

        // While the palette is open only the single-chord toggles are global; the dialog's own
        // handler owns every other key.
        if (modal.open) {
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

        // Plain keys never fire from an editable element - typing "g" in a text box is typing - and neither does a
        // character composed with AltGr or Option; chords with a modifier still do.
        const hasModifier = e.ctrlKey || e.metaKey || e.altKey;
        if (isEditable(e.target) && suppressInEditable && (!hasModifier || typesWithAlt(e))) {
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
        } else {
            // The first chords of a sequence: the literal chord of each key press, which .NET renders for the platform.
            reportPending(buffer.map(alts => alts[0]).join(' '));
        }
    }

    function onDialogKeyDown(e: KeyboardEvent): void {
        if (disposed || isComposing(e)) return;
        // In a page, a key the modal palette already claimed (its toggle, say) is not also a key of this one.
        if (isInline && e.defaultPrevented) return;
        // In a page, Tab and Shift+Tab move the focus on, as everywhere else on it: an inline palette is no focus trap.
        if (isInline && e.key === 'Tab') return;
        // The input the key was typed in, which an inline palette - never opened - learns this way.
        const target = e.target;
        if (target instanceof HTMLInputElement && target.dataset.balette === 'input') inputEl = target;
        const ctrl = e.ctrlKey, shift = e.shiftKey, alt = e.altKey, meta = e.metaKey;
        // With Ctrl or ⌘ held a letter is a chord, so a non-Latin layout's letter is read as the key it sits on;
        // .NET is sent that letter, which is what its bindings are written with.
        const lower = e.key.length === 1 ? ((ctrl || meta) ? chordLetter(e) : e.key.toLowerCase()) : e.key;
        const key = e.key.length === 1 && (ctrl || meta) ? lower : e.key;
        const emptyQuery = inputEl !== null && inputEl.value.length === 0;

        let handled = false;
        if (isInline && key === 'Escape') {
            // Claimed only when it has something to do - disarm a confirmation, go back a page, clear the query - so a
            // drawer or dialog around the palette still closes on it.
            const depth = Number(dialog.querySelector('[data-page-depth]')?.getAttribute('data-page-depth') ?? '0');
            handled = !emptyQuery || depth > 0 || dialog.querySelector('[data-confirming="true"]') !== null;
        } else if (NAV_KEYS.has(key)) {
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
        dotnet.invokeMethodAsync('OnKeyDown', logicalArrow(key), ctrl, shift, alt, meta);
    }

    /**
     * .NET reads ArrowLeft as "back" and ArrowRight as "into the action panel": towards the start and the end of the line.
     * In right-to-left text the start is on the right, so the two arrows trade places.
     */
    function logicalArrow(key: string): string {
        if (key !== 'ArrowLeft' && key !== 'ArrowRight') return key;
        const rtl = getComputedStyle(inputEl ?? dialog).direction === 'rtl';
        return rtl ? (key === 'ArrowLeft' ? 'ArrowRight' : 'ArrowLeft') : key;
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

    // Hover selection is suspended while a new query moves rows under a resting pointer, and each suspension renders
    // a fresh value on the dialog. Only the first move of each crosses to .NET: every other one would be a round
    // trip under Blazor Server for nothing.
    function onPointerMove(): void {
        const suspension = dialog.getAttribute('data-hover-suspended');
        if (suspension === null || suspension === reportedSuspension) return;
        reportedSuspension = suspension;
        dotnet.invokeMethodAsync('OnPointerMoved');
    }

    // An inline palette tells .NET when the focus enters or leaves it, which announces result counts only while it has it.
    let inlineFocused = false;
    function onFocusIn(): void {
        if (inlineFocused) return;
        inlineFocused = true;
        dotnet.invokeMethodAsync('OnInlineFocusChanged', true);
    }
    function onFocusOut(e: FocusEvent): void {
        const next = e.relatedTarget;
        if (!inlineFocused || (next instanceof Node && dialog.contains(next))) return;
        inlineFocused = false;
        dotnet.invokeMethodAsync('OnInlineFocusChanged', false);
    }

    if (!isInline) {
        document.addEventListener('keydown', onDocumentKeyDown, true);
        dialog.addEventListener('cancel', onCancel);
        dialog.addEventListener('click', onDialogClick);
    } else {
        dialog.addEventListener('focusin', onFocusIn);
        dialog.addEventListener('focusout', onFocusOut);
    }
    dialog.addEventListener('keydown', onDialogKeyDown);
    dialog.addEventListener('pointermove', onPointerMove, { passive: true });

    return {
        isMac() {
            return isMac;
        },

        open(input, selectAll) {
            inputEl = input instanceof HTMLInputElement ? input : null;
            if (isInline) {
                if (inputEl) inputEl.focus({ preventScroll: true });
                return;
            }
            previouslyFocused = document.activeElement;
            if (!modal.open) {
                try {
                    modal.showModal();
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
            if (isInline) return;
            if (modal.open) {
                modal.close();
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

        scrollIntoView(list, id, position, count) {
            // Revealing a row scrolls every scrollable ancestor - for a section of the page, the page itself. An inline
            // palette nobody is using (its sources answering after the page loaded) must not pull the page to it.
            if (isInline && !dialog.contains(document.activeElement)) return;
            const request = ++scrollRequest;
            const el = document.getElementById(id);
            if (el) {
                reveal(list, el);
                return;
            }

            // Virtualized out of the DOM: scroll to where the row should be so that Virtualize renders it, then
            // reveal it once it is there. The estimate is redone while Virtualize is still measuring its rows.
            if (!list || typeof position !== 'number' || !count) return;
            let frames = 0;
            const step = () => {
                if (disposed || request !== scrollRequest || !list.isConnected) return;
                const found = document.getElementById(id);
                if (found) {
                    reveal(list, found);
                    return;
                }
                if (frames % 4 === 0) scrollToEstimate(list, position, count);
                if (++frames < 60) requestAnimationFrame(step);
            };
            step();
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
            dialog.removeEventListener('focusin', onFocusIn);
            dialog.removeEventListener('focusout', onFocusOut);
            document.documentElement.classList.remove('bit-balette-scroll-lock');
            resetBuffer();
        },
    };
}
