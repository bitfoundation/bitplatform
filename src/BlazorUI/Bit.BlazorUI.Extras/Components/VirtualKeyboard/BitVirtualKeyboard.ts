namespace BitBlazorUI {
    interface BitVirtualKeyboardKeyOptions {
        id: string;
        ly: string;
        a: string;
        o?: string | null;
        l?: string | null;
        nl?: string | null;
        dk?: string | null;
        c?: string | null;
        lp?: string[] | null;
        lpd?: string | null;
        mt?: string[] | null;
        r: boolean;
        p: boolean;
    }

    interface BitVirtualKeyboardStringsOptions {
        alternates: string;
        layerAnnouncement: string;
        shiftOn: string;
        shiftLocked: string;
        shiftOff: string;
        capsLockOn: string;
        capsLockOff: string;
        layerNames: Record<string, string>;
    }

    interface BitVirtualKeyboardOptions {
        version: number;
        keysVersion: number;
        layoutId: string;
        baseLayer: string;
        shiftLayer?: string | null;
        capsLayer?: string | null;
        altGrLayer?: string | null;
        shiftAltGrLayer?: string | null;
        keys: BitVirtualKeyboardKeyOptions[];
        deadKeys: Record<string, Record<string, string>>;
        docked: boolean;
        open: boolean;
        valueMode: boolean;
        followFocus: boolean;
        targetSelector?: string | null;
        enterBehavior: string;
        allowedCharacters?: string | null;
        maxLength?: number | null;
        secure: boolean;
        showKeyPreview: boolean;
        sound: boolean;
        haptics: boolean;
        hasKeyPressHandler: boolean;
        hasInputHandler: boolean;
        longPressDelay: number;
        multiTapTimeout: number;
        repeatDelay: number;
        repeatInterval: number;
        autoForm: boolean;
        compact: boolean;
        compactBreakpoint: number;
        reserveSpace: boolean;
        disabled: boolean;
        setValue: boolean;
        value?: string | null;
        valueSeq: number;
        activeKeyClass?: string | null;
        activeKeyStyle?: string | null;
        popupKeyClass?: string | null;
        popupKeyStyle?: string | null;
        strings: BitVirtualKeyboardStringsOptions;
    }

    interface BitVirtualKeyboardKeyPressResult {
        cancel: boolean;
        text?: string | null;
    }

    type BitVirtualKeyboardTarget = HTMLInputElement | HTMLTextAreaElement;

    interface BitVirtualKeyboardPress {
        index: number;
        element: HTMLElement;
        repeat: number;
        long: number;
        acted: boolean;
        popup: boolean;
    }

    // The DOM side of a BitVirtualKeyboard: which layer shows, the modifiers and the pending dead key, the gestures of the
    // keys (repeat, long press, multitap), and the typing itself, into the input the keyboard is attached to or into its
    // own Value. Everything a key does happens here, so typing never waits for .NET; .NET is told what changed. The pure
    // parts of the logic are BitVirtualKeyboardEngine's.
    export class BitVirtualKeyboard {
        public static setup(root: HTMLElement, dotnet: DotNetObject, options: BitVirtualKeyboardOptions, target: HTMLElement | null) {
            const keyboard = new BitVirtualKeyboardInstance(root, dotnet, options);
            if (target) keyboard.attachExplicit(target as BitVirtualKeyboardTarget);

            return {
                update: (next: BitVirtualKeyboardOptions) => keyboard.update(next),
                attach: (element: HTMLElement | null) => keyboard.attachExplicit(element as BitVirtualKeyboardTarget | null),
                setLayer: (id: string) => keyboard.selectLayer(id, true),
                press: (keyId: string) => keyboard.pressById(keyId),
                setOpen: (open: boolean) => keyboard.setOpen(open, false),
                dispose: () => keyboard.dispose(),
            };
        }
    }

    class BitVirtualKeyboardInstance {
        private opts: BitVirtualKeyboardOptions;
        private readonly panel: HTMLElement;
        private readonly popup: HTMLElement | null;
        private readonly preview: HTMLElement | null;
        private readonly live: HTMLElement | null;
        private readonly reserve: HTMLElement | null;

        // The layer the layer keys chose, and the one shown (the chosen one through the modifiers).
        private selected: string;
        private shown = '';
        private shift: BitVirtualKeyboardShiftState = { mode: 'off', lastTap: 0 };
        private caps = false;
        private altGr = false;
        private pendingDead: string | null = null;
        // The last tap of a multitap key: the item it typed (empty when nothing was), and the input, the text and the caret
        // it left, in which the next tap replaces that item only while they are still as it left them.
        private multiTap: { key: number, item: number, time: number, typed: string, target: BitVirtualKeyboardTarget | null, value: string, caret: number } | null = null;

        private target: BitVirtualKeyboardTarget | null = null;
        private explicitTarget: BitVirtualKeyboardTarget | null = null;
        private saved: { inputMode: string | null, esc: string | null, claimed: boolean } | null = null;
        private committed = '';
        private atAttach = '';
        private numberBuffer = '';
        // The value of a type=number input as the keyboard last left it: a value that differs from it was typed or set some
        // other way, and is what the keyboard goes on from.
        private numberValue = '';

        // The Value of a keyboard with no input to type into, and the sequence number of its last change.
        private buffer = '';
        private seq = 0;
        // The Value the Cancel key puts back: the one the keyboard was given, opened with, or last accepted.
        private valueBaseline = '';
        private sendScheduled = false;

        private open = false;
        private compactReported: boolean | null = null;
        private dispatching = false;
        private disposed = false;

        private readonly presses = new Map<number, BitVirtualKeyboardPress>();
        private popupState: { index: number, items: string[], selected: number, moved: boolean } | null = null;
        private lastPointerAct = { index: -1, time: 0 };
        // The key the last Enter or Space was pressed on: the click it makes is the keyboard's, never the echo of a pointer.
        private keyActivation: HTMLElement | null = null;
        private audio: AudioContext | null = null;
        private readonly resizeObserver: ResizeObserver | null = null;
        private readonly activeStyles = new WeakMap<HTMLElement, string | null>();

        constructor(private readonly root: HTMLElement, private readonly dotnet: DotNetObject, options: BitVirtualKeyboardOptions) {
            this.opts = options;
            this.panel = root.querySelector(':scope > .bit-vkb-pnl') as HTMLElement;
            this.popup = this.panel.querySelector(':scope > .bit-vkb-pop');
            this.preview = this.panel.querySelector(':scope > .bit-vkb-prv');
            this.live = root.querySelector(':scope > .bit-vkb-liv');
            this.reserve = root.querySelector(':scope > .bit-vkb-rsv');
            this.selected = options.baseLayer;
            this.buffer = this.valueBaseline = options.value ?? '';

            root.addEventListener('pointerdown', this.onPointerDown);
            root.addEventListener('pointermove', this.onPointerMove);
            root.addEventListener('pointerup', this.onPointerUp);
            root.addEventListener('pointercancel', this.onPointerCancel);
            root.addEventListener('click', this.onClick);
            root.addEventListener('contextmenu', this.onContextMenu);
            root.addEventListener('keydown', this.onRootKeyDown);
            document.addEventListener('keydown', this.onDocumentKeyDown, true);
            document.addEventListener('keyup', this.onDocumentKeyUp, true);
            document.addEventListener('pointerdown', this.onDocumentPointerDown, true);

            if (typeof ResizeObserver === 'function') {
                this.resizeObserver = new ResizeObserver(() => this.measure());
                this.resizeObserver.observe(root);
            }

            this.applyFollowFocus(false, options.followFocus);
            this.render();
            if (options.docked) this.setOpen(options.open, false);
            if (options.valueMode === false && !this.target) root.setAttribute('data-bit-vkb-detached', '');
            root.setAttribute('data-bit-vkb-ready', '');
            root.setAttribute('data-bit-vkb-sync', String(options.keysVersion));
        }

        // ---------------------------------------------------------------- options

        public update(next: BitVirtualKeyboardOptions) {
            const previous = this.opts;
            this.opts = next;

            if (next.layoutId !== previous.layoutId || this.hasLayer(this.selected) === false) {
                this.selected = next.baseLayer;
                this.pendingDead = null;
                this.multiTap = null;
            }

            // a Value .NET holds that is not one this side sent it (or is a newer one), taken as the text
            if (next.setValue && next.valueSeq >= this.seq) {
                this.buffer = this.valueBaseline = next.value ?? '';
            }

            // a key held down repeats, or opens its alternates, only while the new table keeps it where it was: a press whose
            // index another key took is let go
            if (next.keysVersion !== previous.keysVersion) {
                for (const [pointerId, press] of Array.from(this.presses.entries())) {
                    const before = previous.keys[press.index];
                    const after = next.keys[press.index];
                    if (!before || !after || before.id !== after.id || before.a !== after.a || before.ly !== after.ly || before.o !== after.o) {
                        this.endPress(pointerId, press);
                    }
                }
            }

            if (next.followFocus !== previous.followFocus) this.applyFollowFocus(previous.followFocus, next.followFocus);

            this.render();

            if (next.docked && next.open !== this.open) this.setOpen(next.open, false);
            else if (next.docked === false && previous.docked) this.setOpen(false, false);

            this.root.toggleAttribute('data-bit-vkb-sec', this.secure);
            // the key table the script holds, which a test (or a page) can compare with data-bit-vkb-v
            this.root.setAttribute('data-bit-vkb-sync', String(next.keysVersion));
            this.measure();
        }

        private get secure(): boolean {
            return this.opts.secure || (this.target instanceof HTMLInputElement && this.target.type === 'password');
        }

        private hasLayer(id: string | null | undefined): boolean {
            return !!id && !!this.layerElement(id);
        }

        private layers(): HTMLElement[] {
            return Array.from(this.panel.querySelectorAll(':scope > .bit-vkb-lys > .bit-vkb-lyr')) as HTMLElement[];
        }

        private layerElement(id: string): HTMLElement | null {
            return this.layers().find(element => element.getAttribute('data-bit-vkb-layer') === id) ?? null;
        }

        // ---------------------------------------------------------------- layers

        private effectiveLayer(): string {
            const o = this.opts;
            if (this.selected !== o.baseLayer) return this.selected;

            const shifted = this.shift.mode !== 'off';
            if (this.altGr && o.altGrLayer) return shifted && o.shiftAltGrLayer ? o.shiftAltGrLayer : o.altGrLayer;
            if (shifted && o.shiftLayer) return o.shiftLayer;
            if (this.caps && o.capsLayer) return o.capsLayer;
            return o.baseLayer;
        }

        private render() {
            const layer = this.effectiveLayer();
            const changed = layer !== this.shown;
            this.shown = layer;

            for (const element of this.layers()) {
                element.toggleAttribute('data-bit-vkb-on', element.getAttribute('data-bit-vkb-layer') === layer);
            }

            // the modifiers and the pending dead key, on every layer they are on
            for (const element of Array.from(this.root.querySelectorAll('[data-bit-vkb-k]')) as HTMLElement[]) {
                const key = this.opts.keys[Number(element.getAttribute('data-bit-vkb-k'))];
                if (!key) continue;

                let active: boolean;
                if (key.a === 'Shift') active = this.shift.mode !== 'off';
                else if (key.a === 'CapsLock') active = this.caps;
                else if (key.a === 'AltGr') active = this.altGr;
                else if (key.a === 'DeadKey') active = this.pendingDead !== null && key.dk === this.pendingDead;
                else continue;

                if (element.hasAttribute('aria-pressed')) element.setAttribute('aria-pressed', active ? 'true' : 'false');
                this.setActive(element, active);
            }

            this.updateRoving();

            if (changed) Utils.notifyDotNet(this.dotnet, 'OnLayer', layer);
        }

        public selectLayer(id: string, announce: boolean) {
            if (this.hasLayer(id) === false) return;

            this.selected = id;
            if (id !== this.opts.baseLayer) {
                this.shift = { mode: 'off', lastTap: 0 };
                this.altGr = false;
            }
            this.render();

            if (announce) {
                const shown = this.effectiveLayer();
                this.announce(this.opts.strings.layerAnnouncement.replace('{0}', this.opts.strings.layerNames[shown] ?? shown));
            }
        }

        private setActive(element: HTMLElement, active: boolean) {
            if (element.hasAttribute('data-bit-vkb-act') === active) return;
            element.toggleAttribute('data-bit-vkb-act', active);

            for (const c of (this.opts.activeKeyClass ?? '').split(/\s+/).filter(c => c)) element.classList.toggle(c, active);

            const style = this.opts.activeKeyStyle;
            if (!style) return;

            if (active) {
                this.activeStyles.set(element, element.getAttribute('style'));
                element.setAttribute('style', `${element.getAttribute('style') ?? ''};${style}`);
            } else if (this.activeStyles.has(element)) {
                const saved = this.activeStyles.get(element);
                if (saved === null || saved === undefined) element.removeAttribute('style');
                else element.setAttribute('style', saved);
                this.activeStyles.delete(element);
            }
        }

        private announce(text: string) {
            const live = this.live;
            if (!live || this.secure || !text) return;

            live.textContent = '';
            window.setTimeout(() => { live.textContent = text; }, 50);
        }

        // ---------------------------------------------------------------- pointer

        private keyOf(target: EventTarget | null): { index: number, element: HTMLElement } | null {
            const element = (target instanceof Element ? target.closest('[data-bit-vkb-k]') : null) as HTMLElement | null;
            if (!element || this.root.contains(element) === false) return null;
            return { index: Number(element.getAttribute('data-bit-vkb-k')), element };
        }

        private itemOf(target: EventTarget | null): HTMLElement | null {
            const item = (target instanceof Element ? target.closest('.bit-vkb-ppk') : null) as HTMLElement | null;
            return item && this.popup?.contains(item) ? item : null;
        }

        // Blazor patches the keys before the script hears of the new layout: a press in between would read the new keys
        // against the old table, so it is left alone.
        private isCurrent(): boolean {
            return this.root.getAttribute('data-bit-vkb-v') === String(this.opts.keysVersion);
        }

        private readonly onPointerDown = (e: PointerEvent) => {
            if (this.disposed) return;
            this.keyActivation = null;

            if (this.itemOf(e.target)) {
                e.preventDefault();
                return;
            }

            const hit = this.keyOf(e.target);

            // the focus stays in the input the keyboard types into, and off the keys a pointer presses
            if (hit || this.target) e.preventDefault();

            if (!hit || this.opts.disabled || this.isCurrent() === false || e.button > 0) return;

            const key = this.opts.keys[hit.index];
            if (!key) return;

            try { hit.element.setPointerCapture(e.pointerId); } catch { /* a pointer already gone */ }

            const press: BitVirtualKeyboardPress = { index: hit.index, element: hit.element, repeat: 0, long: 0, acted: false, popup: false };
            this.presses.set(e.pointerId, press);

            if (this.secure === false) hit.element.setAttribute('data-bit-vkb-down', '');
            this.showPreview(hit.index, hit.element);

            if (key.r) {
                press.acted = true;
                const pressed = Date.now();
                // the repeats ask no handler, so a key repeats only once its first press acted (no handler cancelled it)
                this.act(hit.index, 'pointer').then(acted => {
                    if (acted === false || this.presses.get(e.pointerId) !== press || this.opts.repeatDelay <= 0) return;

                    press.repeat = window.setTimeout(() => {
                        press.repeat = window.setInterval(() => this.act(hit.index, 'repeat'), Math.max(16, this.opts.repeatInterval));
                    }, Math.max(0, this.opts.repeatDelay - (Date.now() - pressed)));
                });
            } else if (key.lp && key.lp.length > 0) {
                press.long = window.setTimeout(() => {
                    press.long = 0;
                    press.popup = true;
                    this.hidePreview();
                    this.openPopup(hit.index, hit.element, false);
                }, Math.max(100, this.opts.longPressDelay));
            }
        }

        private readonly onPointerMove = (e: PointerEvent) => {
            const press = this.presses.get(e.pointerId);
            if (!press || !press.popup || !this.popupState) return;

            const item = this.itemOf(document.elementFromPoint(e.clientX, e.clientY));
            if (item) {
                this.popupState.moved = true;
                this.selectPopupItem(Number(item.getAttribute('data-bit-vkb-i')));
            }
        }

        private readonly onPointerUp = (e: PointerEvent) => {
            const press = this.presses.get(e.pointerId);

            if (!press) {
                // a tap on an item of a popup left open
                const item = this.itemOf(e.target);
                if (item && this.popupState) this.choosePopupItem(Number(item.getAttribute('data-bit-vkb-i')));
                return;
            }

            this.endPress(e.pointerId, press);

            if (press.popup) {
                // slid onto an item and let go: that item; let go without sliding: the popup stays for a tap
                if (this.popupState?.moved) this.choosePopupItem(this.popupState.selected);
                return;
            }

            if (press.acted) return;

            // a key acts where it is let go of, so sliding off it takes the press back
            const over = this.keyOf(document.elementFromPoint(e.clientX, e.clientY));
            if (over && over.index === press.index) {
                this.lastPointerAct = { index: press.index, time: Date.now() };
                this.act(press.index, 'pointer');
            }
        }

        private readonly onPointerCancel = (e: PointerEvent) => {
            const press = this.presses.get(e.pointerId);
            if (press) this.endPress(e.pointerId, press);
        }

        private endPress(pointerId: number, press: BitVirtualKeyboardPress) {
            window.clearTimeout(press.repeat);
            window.clearInterval(press.repeat);
            window.clearTimeout(press.long);
            press.element.removeAttribute('data-bit-vkb-down');
            this.presses.delete(pointerId);
            this.hidePreview();
        }

        // A key activated without a pointer: the keyboard (Enter or Space on a focused key), or assistive technology.
        private readonly onClick = (e: MouseEvent) => {
            if (e.detail !== 0) return;

            const item = this.itemOf(e.target);
            if (item && this.popupState) {
                this.choosePopupItem(Number(item.getAttribute('data-bit-vkb-i')));
                return;
            }

            const hit = this.keyOf(e.target);
            const byKeyboard = hit !== null && this.keyActivation === hit.element;
            this.keyActivation = null;
            if (!hit || this.opts.disabled || this.isCurrent() === false) return;
            // assistive technology may send a pointer press and a click of its own for one activation; Enter or Space
            // pressed on the key right after a tap is a press of its own
            if (byKeyboard === false && this.lastPointerAct.index === hit.index && Date.now() - this.lastPointerAct.time < 500) return;

            this.act(hit.index, 'keyboard');
        }

        // The context menu of a key (Shift+F10, the menu key, a right click) opens its alternates.
        private readonly onContextMenu = (e: MouseEvent) => {
            const hit = this.keyOf(e.target);
            if (!hit) return;
            e.preventDefault();

            const key = this.opts.keys[hit.index];
            if (key?.lp && key.lp.length > 0 && this.popupState === null) this.openPopup(hit.index, hit.element, e.detail === 0);
        }

        private readonly onDocumentPointerDown = (e: PointerEvent) => {
            if (this.popupState && this.presses.size === 0 && (e.target instanceof Node && this.popup?.contains(e.target)) !== true) {
                this.closePopup();
            }
        }

        // ---------------------------------------------------------------- the keys

        public pressById(keyId: string) {
            const shown = this.effectiveLayer();
            let index = this.opts.keys.findIndex(k => k.id === keyId && k.ly === shown);
            if (index < 0) index = this.opts.keys.findIndex(k => k.id === keyId);
            if (index >= 0) this.act(index, 'api');
        }

        // Whether the key acted: a key press handler can cancel it.
        private async act(index: number, source: 'pointer' | 'repeat' | 'keyboard' | 'api' | 'popup', text?: string): Promise<boolean> {
            const key = this.opts.keys[index];
            if (!key || this.opts.disabled || this.disposed) return false;
            if (this.opts.valueMode === false && !this.target) return false;

            let typed = text ?? this.textOf(key, index);

            if (this.opts.hasKeyPressHandler && source !== 'repeat') {
                // what the press was made against, which the handler may outlive
                const target = this.target;
                const keysVersion = this.opts.keysVersion;
                // secure, nothing the handler is given tells the character: a key typing one goes without its id (a digit's
                // id is the digit)
                const id = this.secure && (key.a === 'Character' || key.a === 'DeadKey') ? '' : key.id;

                try {
                    const result = await Utils.invokeDotNet<BitVirtualKeyboardKeyPressResult>(this.dotnet, 'OnKeyPress', id, key.a, this.secure ? null : typed, key.ly);
                    if (result?.cancel) return false;
                    if (result && this.secure === false && result.text !== undefined && result.text !== typed) typed = result.text ?? '';
                } catch { /* a handler that failed leaves the key to act */ }

                // the keyboard moved on to another input or another key table, or was disabled, while the handler ran
                if (this.disposed || this.opts.disabled || this.target !== target || this.opts.keysVersion !== keysVersion) return false;
            }

            this.feedback();

            switch (key.a) {
                case 'Character':
                    this.typeCharacter(key, index, typed ?? '', source === 'popup');
                    break;
                case 'Space':
                    if (this.pendingDead !== null) {
                        const alone = BitVirtualKeyboardEngine.deadKeyAlone(this.opts.deadKeys[this.pendingDead]);
                        this.pendingDead = null;
                        this.insert(alone || ' ');
                    } else {
                        this.insert(typed ?? ' ');
                    }
                    this.afterTyping(key);
                    break;
                case 'Backspace':
                    if (this.pendingDead !== null) {
                        this.pendingDead = null;
                        this.render();
                        break;
                    }
                    this.multiTap = null;
                    this.withKeyEvents('Backspace', false, () => this.deleteBackward());
                    break;
                case 'Delete':
                    this.withKeyEvents('Delete', false, () => this.deleteForward());
                    break;
                case 'Enter':
                    this.enter();
                    break;
                case 'Tab':
                case 'ShiftTab':
                    this.tab(key.a === 'Tab');
                    break;
                case 'Shift':
                    this.shift = BitVirtualKeyboardEngine.tapShift(this.shift, Date.now());
                    this.render();
                    this.announce(this.shift.mode === 'latched' ? this.opts.strings.shiftOn
                        : this.shift.mode === 'locked' ? this.opts.strings.shiftLocked
                            : this.opts.strings.shiftOff);
                    break;
                case 'CapsLock':
                    this.caps = !this.caps;
                    this.render();
                    this.announce(this.caps ? this.opts.strings.capsLockOn : this.opts.strings.capsLockOff);
                    break;
                case 'AltGr':
                    this.altGr = !this.altGr;
                    this.render();
                    break;
                case 'SwitchLayer':
                    if (key.l) this.selectLayer(key.l, true);
                    break;
                case 'DeadKey':
                    if (this.pendingDead !== null) this.insert(BitVirtualKeyboardEngine.deadKeyAlone(this.opts.deadKeys[this.pendingDead]));
                    this.pendingDead = key.dk ?? null;
                    this.render();
                    break;
                case 'NextLayout':
                    Utils.notifyDotNet(this.dotnet, 'OnNextLayout');
                    break;
                case 'Hide':
                    this.commit();
                    this.setOpen(false, true);
                    break;
                case 'Clear':
                    this.clear();
                    break;
                case 'Accept':
                    this.commit();
                    if (this.opts.docked) this.setOpen(false, true);
                    break;
                case 'Cancel':
                    this.withKeyEvents('Escape', false, () => this.cancel());
                    break;
                case 'ArrowLeft':
                case 'ArrowRight':
                case 'ArrowUp':
                case 'ArrowDown':
                case 'Home':
                case 'End':
                    this.withKeyEvents(key.a, false, () => this.moveCaret(key.a));
                    break;
                case 'Custom':
                    Utils.notifyDotNet(this.dotnet, 'OnCommand', key.c ?? '', key.id);
                    break;
            }

            return true;
        }

        // A tap of the multitap key tapped last, in time, with the input, its text and its caret as that tap left them: anything
        // else moved since, and the tap starts a new cycle at the caret.
        private multiTapCycling(index: number): boolean {
            const m = this.multiTap;
            if (m === null || m.key !== index || Date.now() - m.time > this.opts.multiTapTimeout || m.target !== this.target) return false;

            const now = this.caretState();
            return now.value === m.value && now.start === m.caret && now.end === m.caret;
        }

        // The text the keyboard types into and the selection in it, as an edit reads them.
        private caretState(): { value: string, start: number, end: number } {
            const t = this.target;
            if (!t) return { value: this.buffer, start: this.buffer.length, end: this.buffer.length };

            if (t instanceof HTMLInputElement && t.type === 'number') {
                const value = t.value === this.numberValue ? this.numberBuffer : t.value;
                return { value, start: value.length, end: value.length };
            }

            const [start, end] = this.selection(t);
            return { value: t.value, start, end };
        }

        private textOf(key: BitVirtualKeyboardKeyOptions, index: number): string | null {
            if (key.a === 'Space') return ' ';
            if (key.a !== 'Character') return null;

            if (key.mt && key.mt.length > 0) {
                const item = this.multiTapCycling(index) ? BitVirtualKeyboardEngine.nextMultiTap(key.mt.length, this.multiTap!.item) : 0;
                return key.mt[item];
            }

            const output = key.o ?? '';
            return this.pendingDead !== null ? BitVirtualKeyboardEngine.resolveDeadKey(this.opts.deadKeys[this.pendingDead], output) : output;
        }

        private typeCharacter(key: BitVirtualKeyboardKeyOptions, index: number, text: string, alternate: boolean) {
            if (key.mt && key.mt.length > 0 && alternate === false) {
                const previous = this.multiTapCycling(index) ? this.multiTap! : null;
                const item = previous ? BitVirtualKeyboardEngine.nextMultiTap(key.mt.length, previous.item) : 0;
                // the next item replaces the one the last tap typed in one edit, which the input takes or refuses as a whole:
                // refused, the last item stays
                const typed = this.insert(text, previous ? previous.typed.length : 0) ?? (previous ? previous.typed : '');
                const now = this.caretState();
                this.multiTap = { key: index, item, time: Date.now(), typed, target: this.target, value: now.value, caret: now.end };
            } else {
                this.multiTap = null;
                this.pendingDead = null;
                this.insert(text);
            }

            this.afterTyping(key);
        }

        private afterTyping(key: BitVirtualKeyboardKeyOptions) {
            this.shift = BitVirtualKeyboardEngine.afterTyping(this.shift);
            this.altGr = false;
            if (key.nl && this.hasLayer(key.nl)) this.selected = key.nl;
            this.render();
        }

        // ---------------------------------------------------------------- the text

        private get selectable(): boolean {
            const t = this.target;
            return t instanceof HTMLTextAreaElement || (t instanceof HTMLInputElement && BitVirtualKeyboardEngine.selectableTypes.indexOf(t.type) >= 0);
        }

        private get maxLength(): number | null {
            if (this.target) return this.target.maxLength >= 0 ? this.target.maxLength : null;
            return this.opts.maxLength ?? null;
        }

        private selection(t: BitVirtualKeyboardTarget): [number, number] {
            const length = t.value.length;
            return this.selectable ? [t.selectionStart ?? length, t.selectionEnd ?? length] : [length, length];
        }

        // An input made read-only or disabled since the keyboard attached to it takes no more edits.
        private writable(t: BitVirtualKeyboardTarget): boolean {
            return t.readOnly === false && t.disabled === false;
        }

        // Whether a beforeinput listener left the input as the edit found it: an edit worked out from a value or a selection
        // it changed would land in the wrong place, so it is dropped.
        private unchanged(t: BitVirtualKeyboardTarget, value: string, start: number, end: number): boolean {
            if (this.target !== t || t.value !== value) return false;

            const [s, e] = this.selection(t);
            return s === start && e === end;
        }

        // Types the text at the caret: over the selection, or over the code units before the caret a multitap key's last tap
        // typed. Returns what was typed once maxlength took its share, or null when nothing was.
        private insert(text: string, replace = 0): string | null {
            if (!text) return null;
            if (BitVirtualKeyboardEngine.isAllowed(text, this.opts.allowedCharacters) === false) return null;

            const t = this.target;
            if (!t) {
                const end = this.buffer.length;
                const edit = BitVirtualKeyboardEngine.insert(this.buffer, end - replace, end, text, this.maxLength);
                if (!edit) return null;
                this.setBuffer(edit.text);
                return edit.inserted;
            }

            if (this.writable(t) === false) return null;

            if (t instanceof HTMLInputElement && t.type === 'number') {
                if (BitVirtualKeyboardEngine.isNumberCharacter(text) === false) return null;
                this.syncNumber(t);
                const kept = this.numberBuffer.slice(0, this.numberBuffer.length - replace);
                return this.setNumber(kept + text, 'insertText', text) ? text : null;
            }

            const value = t.value;
            const [caret, end] = this.selection(t);
            const start = caret - replace;
            const edit = BitVirtualKeyboardEngine.insert(value, start, end, text, this.maxLength);
            if (!edit) return null;

            if (this.dispatchBefore('insertText', edit.inserted) === false || this.unchanged(t, value, caret, end) === false) return null;

            if (this.selectable) t.setRangeText(edit.inserted, start, end, 'end');
            else t.value = edit.text;

            this.dispatchInput('insertText', edit.inserted);
            return edit.inserted;
        }

        private deleteBackward() {
            const t = this.target;
            if (!t) {
                const edit = BitVirtualKeyboardEngine.deleteBackward(this.buffer, this.buffer.length, this.buffer.length);
                if (edit) this.setBuffer(edit.text);
                return;
            }

            if (this.writable(t) === false) return;

            if (t instanceof HTMLInputElement && t.type === 'number') {
                this.syncNumber(t);
                const edit = BitVirtualKeyboardEngine.deleteBackward(this.numberBuffer, this.numberBuffer.length, this.numberBuffer.length);
                if (edit) this.setNumber(edit.text, 'deleteContentBackward', null);
                return;
            }

            const value = t.value;
            const [start, end] = this.selection(t);
            const edit = BitVirtualKeyboardEngine.deleteBackward(value, start, end);
            if (!edit) return;

            if (this.dispatchBefore('deleteContentBackward', null) === false || this.unchanged(t, value, start, end) === false) return;

            if (this.selectable) t.setRangeText('', edit.start, start === end ? start : end, 'end');
            else t.value = edit.text;

            this.dispatchInput('deleteContentBackward', null);
        }

        private deleteForward() {
            const t = this.target;
            if (!t || this.selectable === false || this.writable(t) === false) return;

            const value = t.value;
            const [start, end] = this.selection(t);
            const edit = BitVirtualKeyboardEngine.deleteForward(value, start, end);
            if (!edit) return;

            const to = start === end ? value.length - (edit.text.length - start) : end;
            if (this.dispatchBefore('deleteContentForward', null) === false || this.unchanged(t, value, start, end) === false) return;

            t.setRangeText('', start, to, 'end');
            this.dispatchInput('deleteContentForward', null);
        }

        private clear() {
            const t = this.target;
            if (!t) {
                this.setBuffer('');
                return;
            }

            if (this.writable(t) === false) return;

            // an empty type=number input may still hold what was typed into it and is not a number yet ("-")
            if (t.value === '') {
                this.numberBuffer = this.numberValue = '';
                return;
            }
            if (this.dispatchBefore('deleteContent', null) === false) return;

            t.value = '';
            this.numberBuffer = this.numberValue = '';
            this.dispatchInput('deleteContent', null);
        }

        // A type=number input sanitizes anything that is not a whole number to empty ("1." included), so what is typed
        // into it is kept here and written only once it is a number.
        private setNumber(buffer: string, inputType: string, data: string | null): boolean {
            const t = this.target as HTMLInputElement;
            if (buffer !== '' && BitVirtualKeyboardEngine.isNumber(buffer) === false) {
                this.numberBuffer = buffer;
                return true;
            }

            // a number goes to the input, and is kept, only once its beforeinput is let through
            if (this.dispatchBefore(inputType, data) === false) return false;
            this.numberBuffer = buffer;
            t.value = buffer;
            this.numberValue = t.value;
            this.dispatchInput(inputType, data);
            return true;
        }

        // A value typed on a physical keyboard, or set by the page, since the keyboard last wrote one is the text it goes on
        // from; what it holds back while that is not a number yet ("1.") is kept while the input shows what it last wrote.
        private syncNumber(t: HTMLInputElement) {
            if (t.value === this.numberValue) return;
            this.numberBuffer = this.numberValue = t.value;
        }

        private moveCaret(action: string) {
            const t = this.target;
            if (!t || this.selectable === false) return;

            const value = t.value;
            const caret = t.selectionDirection === 'backward' ? (t.selectionStart ?? 0) : (t.selectionEnd ?? 0);
            let next = caret;

            switch (action) {
                case 'ArrowLeft':
                case 'ArrowRight': {
                    // in a right-to-left input, the left arrow moves forward through the text
                    const rtl = getComputedStyle(t).direction === 'rtl';
                    const forward = (action === 'ArrowRight') !== rtl;
                    next = forward ? BitVirtualKeyboardEngine.nextGraphemeEnd(value, caret) : BitVirtualKeyboardEngine.previousGraphemeStart(value, caret);
                    break;
                }
                case 'Home':
                    next = t instanceof HTMLTextAreaElement ? BitVirtualKeyboardEngine.lineStart(value, caret) : 0;
                    break;
                case 'End': {
                    const lineEnd = t instanceof HTMLTextAreaElement ? value.indexOf('\n', caret) : -1;
                    next = lineEnd < 0 ? value.length : lineEnd;
                    break;
                }
                case 'ArrowUp':
                case 'ArrowDown': {
                    if (t instanceof HTMLTextAreaElement === false) return;
                    const lineStart = BitVirtualKeyboardEngine.lineStart(value, caret);
                    const column = caret - lineStart;
                    if (action === 'ArrowUp') {
                        if (lineStart === 0) return;
                        const previousStart = BitVirtualKeyboardEngine.lineStart(value, lineStart - 1);
                        next = Math.min(previousStart + column, lineStart - 1);
                    } else {
                        const lineEnd = value.indexOf('\n', caret);
                        if (lineEnd < 0) return;
                        const nextEnd = value.indexOf('\n', lineEnd + 1);
                        next = Math.min(lineEnd + 1 + column, nextEnd < 0 ? value.length : nextEnd);
                    }
                    break;
                }
            }

            t.setSelectionRange(next, next);
        }

        private setBuffer(text: string) {
            this.buffer = text;
            this.seq++;
            if (this.sendScheduled) return;

            this.sendScheduled = true;
            requestAnimationFrame(() => {
                this.sendScheduled = false;
                if (this.disposed) return;
                Utils.notifyDotNet(this.dotnet, 'OnValue', this.buffer, this.seq);
            });
        }

        private dispatchBefore(inputType: string, data: string | null): boolean {
            if (!this.target) return true;
            return this.target.dispatchEvent(new InputEvent('beforeinput', { inputType, data, bubbles: true, cancelable: true }));
        }

        private dispatchInput(inputType: string, data: string | null) {
            const t = this.target;
            if (!t) return;

            t.dispatchEvent(new InputEvent('input', { inputType, data, bubbles: true }));
            if (this.opts.hasInputHandler && this.secure === false) Utils.notifyDotNet(this.dotnet, 'OnTargetValue', t.value);
        }

        // A key the page may handle itself (Enter, Tab, Escape, the arrows, Backspace, Delete) is first sent to the input
        // as a keydown, and a page that prevents it keeps the key from acting. An untrusted key event does nothing by
        // itself, which is why the keyboard acts itself.
        private withKeyEvents(key: string, shiftKey: boolean, action: () => void) {
            const t = this.target;
            if (!t) {
                action();
                return;
            }

            this.dispatching = true;
            try {
                const proceed = t.dispatchEvent(new KeyboardEvent('keydown', { key, code: key, shiftKey, bubbles: true, cancelable: true }));
                if (proceed) action();
                t.dispatchEvent(new KeyboardEvent('keyup', { key, code: key, shiftKey, bubbles: true, cancelable: true }));
            } finally {
                this.dispatching = false;
            }
        }

        // ---------------------------------------------------------------- Enter, Tab, change

        private enter() {
            const t = this.target;
            const behavior = this.opts.enterBehavior;

            if (!t) {
                if (behavior === 'NewLine') this.insert('\n');
                if (behavior === 'Close' && this.opts.docked) this.setOpen(false, true);
                Utils.notifyDotNet(this.dotnet, 'OnEnter');
                return;
            }

            this.withKeyEvents('Enter', false, () => {
                const hint = (t.getAttribute('enterkeyhint') ?? '').toLowerCase();
                const textarea = t instanceof HTMLTextAreaElement;
                let mode = behavior;
                if (mode === 'Auto') mode = textarea ? 'NewLine' : hint === 'next' ? 'Next' : hint === 'previous' ? 'Previous' : t.form ? 'Submit' : 'None';

                switch (mode) {
                    case 'NewLine':
                        if (textarea) this.insert('\n');
                        break;
                    case 'Next':
                    case 'Previous':
                        this.commit();
                        this.focusSibling(mode === 'Next');
                        break;
                    case 'Submit': {
                        this.commit();
                        const form = t.form;
                        if (form) {
                            // never form.submit(), which skips the validation and the submit event
                            if (typeof form.requestSubmit === 'function') form.requestSubmit();
                            else (form.querySelector('[type=submit]') as HTMLElement | null)?.click();
                        }
                        break;
                    }
                    case 'Close':
                        this.commit();
                        this.setOpen(false, true);
                        break;
                }
            });

            Utils.notifyDotNet(this.dotnet, 'OnEnter');
        }

        private tab(forward: boolean) {
            if (!this.target) return;

            this.withKeyEvents('Tab', forward === false, () => {
                this.commit();
                this.focusSibling(forward);
            });
        }

        private focusSibling(forward: boolean) {
            const t = this.target;
            if (!t) return;

            Utils.findFocusable(document.body, t, forward, el => this.root.contains(el))?.focus();
        }

        // Fires change once the value differs from the one last committed: a value a script set never makes the browser
        // fire change on blur, while typing on a physical keyboard in between does - so the input's own change moves the
        // baseline, and no change is fired twice.
        private commit() {
            const t = this.target;
            if (!t) {
                this.valueBaseline = this.buffer;
                return;
            }
            if (t.value === this.committed) return;

            this.committed = t.value;
            t.dispatchEvent(new Event('change', { bubbles: true }));
        }

        private cancel() {
            const t = this.target;
            if (t && t.value !== this.atAttach && this.writable(t)) {
                t.value = this.atAttach;
                this.numberBuffer = this.numberValue = this.atAttach;
                this.dispatchInput('insertReplacementText', null);
                this.commit();
            } else if (!t && this.buffer !== this.valueBaseline) {
                this.setBuffer(this.valueBaseline);
            }

            if (this.opts.docked) this.setOpen(false, true);
        }

        // ---------------------------------------------------------------- attaching

        public attachExplicit(element: BitVirtualKeyboardTarget | null) {
            this.explicitTarget = element;
            if (element) this.attach(element);
            else if (this.target) this.detach();
        }

        private qualifies(element: Element | null): element is BitVirtualKeyboardTarget {
            if (element instanceof HTMLTextAreaElement) return !element.disabled && !element.readOnly;
            if (element instanceof HTMLInputElement) {
                // the type asked for, not the one the engine falls back to (WebKit on Windows reports an unsupported date as text)
                const asked = (element.getAttribute('type') ?? 'text').toLowerCase();
                const types = BitVirtualKeyboardEngine.qualifyingTypes;
                return types.indexOf(asked) >= 0 && types.indexOf(element.type) >= 0 && !element.disabled && !element.readOnly;
            }
            return false;
        }

        private matchesSelector(element: Element): boolean {
            try {
                return element.matches(this.opts.targetSelector || '[data-bit-vkb]');
            } catch {
                return false;
            }
        }

        private attach(element: BitVirtualKeyboardTarget) {
            if (this.target === element) return;
            if (this.target) this.detach();
            if (this.qualifies(element) === false) return;

            const inputMode = element.getAttribute('inputmode');
            this.saved = { inputMode, esc: element.getAttribute('data-bit-esc'), claimed: false };
            this.target = element;
            this.committed = this.atAttach = this.numberBuffer = this.numberValue = element.value;

            // the OS keyboard stays away while this one types
            element.setAttribute('inputmode', 'none');
            element.addEventListener('change', this.onTargetChange);
            element.addEventListener('blur', this.onTargetBlur);
            (element as HTMLElement).addEventListener('keydown', this.onTargetKeyDown);

            this.root.toggleAttribute('data-bit-vkb-sec', this.secure);
            this.root.removeAttribute('data-bit-vkb-detached');
            this.updateClaim();

            Utils.notifyDotNet(this.dotnet, 'OnTarget',
                element instanceof HTMLInputElement ? element.type : 'textarea',
                inputMode,
                element.getAttribute('enterkeyhint'),
                element.getAttribute('data-bit-vkb'),
                element instanceof HTMLInputElement && element.type === 'password',
                element.id || null);
        }

        private detach() {
            const t = this.target;
            if (!t) return;

            this.commit();
            this.closePopup();

            t.removeEventListener('change', this.onTargetChange);
            t.removeEventListener('blur', this.onTargetBlur);
            (t as HTMLElement).removeEventListener('keydown', this.onTargetKeyDown);

            const saved = this.saved;
            if (saved) {
                if (saved.inputMode === null) t.removeAttribute('inputmode');
                else t.setAttribute('inputmode', saved.inputMode);

                if (saved.claimed) {
                    if (saved.esc === null) t.removeAttribute('data-bit-esc');
                    else t.setAttribute('data-bit-esc', saved.esc);
                }
            }

            this.target = null;
            this.saved = null;
            this.pendingDead = null;
            this.multiTap = null;
            this.root.toggleAttribute('data-bit-vkb-sec', this.opts.secure);
            if (this.opts.valueMode === false) this.root.setAttribute('data-bit-vkb-detached', '');

            Utils.notifyDotNet(this.dotnet, 'OnTarget', null, null, null, null, false, null);
        }

        // Escape is pressed in the input, outside the keyboard: while the keyboard acts on it (a docked keyboard open, a
        // popup shown) its claim is put on the input itself, unless the input carries one of its own.
        private updateClaim() {
            const t = this.target;
            const saved = this.saved;
            if (!t || !saved || saved.esc !== null) return;

            const acts = (this.opts.docked && this.open) || this.popupState !== null;
            if (acts && saved.claimed === false) {
                t.setAttribute('data-bit-esc', 'claim');
                saved.claimed = true;
            } else if (acts === false && saved.claimed) {
                t.removeAttribute('data-bit-esc');
                saved.claimed = false;
            }
        }

        private readonly onTargetChange = (e: Event) => {
            if (e.isTrusted && this.target) this.committed = this.target.value;
        }

        // A value the keyboard typed is committed as the focus leaves the input (after the browser's own change, so the
        // baseline that one moved is read).
        private readonly onTargetBlur = () => {
            window.setTimeout(() => {
                if (this.disposed || !this.target || document.activeElement === this.target) return;
                this.commit();
            }, 0);
        }

        private readonly onTargetKeyDown = (e: KeyboardEvent) => {
            if (this.dispatching || e.key !== 'Escape' || Utils.isComposingKey(e)) return;
            if (e.altKey || e.ctrlKey || e.metaKey || e.shiftKey) return;

            // the keyboard's own claim, or - on an input claiming Escape for itself - a press no claim took
            const claimer = Utils.escapeClaimer(e);
            const ours = this.saved?.claimed ? claimer === this.target : claimer === null && e.defaultPrevented === false;
            if (!ours) return;

            if (this.popupState) {
                this.closePopup();
            } else if (this.opts.docked && this.open) {
                this.commit();
                this.setOpen(false, true);
            }
        }

        private readonly onFocusIn = (e: FocusEvent) => {
            const element = e.target as Element;
            if (this.root.contains(element) || this.qualifies(element) === false || this.matchesSelector(element) === false) return;

            this.attach(element);
            if (this.opts.docked) this.setOpen(true, true);
        }

        private readonly onFocusOut = () => {
            window.setTimeout(() => {
                if (this.disposed || !this.target || this.explicitTarget === this.target) return;

                const active = document.activeElement;
                if (active === this.target || (active && this.root.contains(active))) return;
                if (active && this.qualifies(active) && this.matchesSelector(active)) return;

                this.detach();
                if (this.opts.docked) this.setOpen(false, true);
            }, 0);
        }

        private applyFollowFocus(was: boolean, is: boolean) {
            if (was === is) return;

            if (is) {
                document.addEventListener('focusin', this.onFocusIn);
                document.addEventListener('focusout', this.onFocusOut);

                const active = document.activeElement;
                if (!this.target && active && this.qualifies(active) && this.matchesSelector(active)) this.attach(active);
            } else {
                document.removeEventListener('focusin', this.onFocusIn);
                document.removeEventListener('focusout', this.onFocusOut);
                if (this.target && this.target !== this.explicitTarget) this.detach();
            }
        }

        // ---------------------------------------------------------------- docked

        public setOpen(open: boolean, notify: boolean) {
            if (this.opts.docked === false) open = false;

            const changed = open !== this.open;
            this.open = open;
            if (changed && open && !this.target) this.valueBaseline = this.buffer;

            this.panel.toggleAttribute('data-bit-vkb-open', open);
            (this.panel as any).inert = this.opts.docked && open === false;
            if (open === false) this.closePopup();
            this.updateClaim();

            if (this.reserve) {
                this.reserve.style.height = open && this.opts.reserveSpace ? `${this.panel.offsetHeight}px` : '0px';
            }

            if (open && this.target) {
                const t = this.target;
                const previous = t.style.scrollMarginBottom;
                t.style.scrollMarginBottom = `${this.panel.offsetHeight + 16}px`;
                t.scrollIntoView({ block: 'nearest' });
                t.style.scrollMarginBottom = previous;
            }

            if (changed && notify) Utils.notifyDotNet(this.dotnet, 'OnOpen', open);
        }

        private measure() {
            if (this.opts.autoForm === false) return;

            const width = this.root.getBoundingClientRect().width;
            if (width <= 0) return;

            const compact = width < this.opts.compactBreakpoint;
            if (compact === this.opts.compact || compact === this.compactReported) return;

            this.compactReported = compact;
            Utils.notifyDotNet(this.dotnet, 'OnCompact', compact);
        }

        // ---------------------------------------------------------------- the popup and the preview

        private openPopup(index: number, element: HTMLElement, focus: boolean) {
            const key = this.opts.keys[index];
            const popup = this.popup;
            if (!popup || !key?.lp || key.lp.length === 0) return;

            this.closePopup();

            popup.textContent = '';
            popup.setAttribute('role', 'group');
            popup.setAttribute('aria-label', this.opts.strings.alternates.replace('{0}', element.getAttribute('aria-label') ?? key.o ?? ''));

            const classes = (this.opts.popupKeyClass ?? '').split(/\s+/).filter(c => c).join(' ');
            key.lp.forEach((text, i) => {
                const item = document.createElement('button');
                item.type = 'button';
                item.className = classes ? `bit-vkb-ppk ${classes}` : 'bit-vkb-ppk';
                if (this.opts.popupKeyStyle) item.setAttribute('style', this.opts.popupKeyStyle);
                item.tabIndex = -1;
                item.setAttribute('data-bit-vkb-i', String(i));
                item.textContent = text;
                popup.appendChild(item);
            });

            const selected = Math.max(0, key.lpd ? key.lp.indexOf(key.lpd) : 0);
            this.popupState = { index, items: key.lp, selected, moved: false };
            this.selectPopupItem(selected);

            popup.setAttribute('data-bit-vkb-show', '');
            this.place(popup, element, 4);
            this.updateClaim();

            // opened from the keyboard: the focus goes to the alternates, unless it is kept in an input
            if (focus && !this.target) (popup.children[selected] as HTMLElement | undefined)?.focus();
        }

        private selectPopupItem(index: number) {
            if (!this.popupState || !this.popup) return;

            this.popupState.selected = index;
            Array.from(this.popup.children).forEach((item, i) => item.toggleAttribute('data-bit-vkb-sel', i === index));
        }

        private choosePopupItem(index: number) {
            const state = this.popupState;
            if (!state) return;

            const text = state.items[index];
            this.closePopup();
            if (text !== undefined) this.act(state.index, 'popup', text);
        }

        private closePopup() {
            if (!this.popup || !this.popupState) return;

            const hadFocus = this.popup.contains(document.activeElement);
            const index = this.popupState.index;
            this.popupState = null;
            this.popup.removeAttribute('data-bit-vkb-show');
            this.popup.textContent = '';
            this.updateClaim();

            if (hadFocus) {
                const key = this.root.querySelector(`[data-bit-vkb-k="${index}"]`) as HTMLElement | null;
                key?.focus();
            }
        }

        private showPreview(index: number, element: HTMLElement) {
            const key = this.opts.keys[index];
            const preview = this.preview;
            if (!preview || !key?.p || this.opts.showKeyPreview === false || this.secure) return;

            preview.textContent = element.textContent?.trim() || key.o || '';
            preview.setAttribute('data-bit-vkb-show', '');
            this.place(preview, element, 2);
        }

        private hidePreview() {
            this.preview?.removeAttribute('data-bit-vkb-show');
        }

        // Places a popup above a key, inside the panel, or under it where there is no room above.
        private place(floating: HTMLElement, element: HTMLElement, gap: number) {
            const panel = this.panel.getBoundingClientRect();
            const key = element.getBoundingClientRect();
            const width = floating.offsetWidth;
            const height = floating.offsetHeight;

            const left = Math.min(Math.max(0, key.left - panel.left + key.width / 2 - width / 2), Math.max(0, panel.width - width));
            let top = key.top - panel.top - height - gap;
            if (key.top - height - gap < 0) top = key.bottom - panel.top + gap;

            floating.style.left = `${left}px`;
            floating.style.top = `${top}px`;
        }

        // ---------------------------------------------------------------- keyboard focus and physical keys

        // Typing into its own Value, the keys are a grid the focus moves through with the arrow keys.
        private readonly onRootKeyDown = (e: KeyboardEvent) => {
            if (Utils.isComposingKey(e)) return;

            if (e.key === 'Enter' || e.key === ' ') this.keyActivation = this.keyOf(e.target)?.element ?? null;

            if (e.key === 'Escape') {
                // a modified Escape, or one a part inside claimed first, is left to whoever it belongs to
                if (e.altKey || e.ctrlKey || e.metaKey || e.shiftKey) return;
                const claimer = Utils.escapeClaimer(e);
                if (claimer ? claimer !== this.root : e.defaultPrevented) return;

                if (this.popupState) {
                    this.closePopup();
                } else if (this.opts.valueMode && !this.target) {
                    if (this.opts.docked && this.open) this.setOpen(false, true);
                    else (document.activeElement as HTMLElement | null)?.blur();
                }
                return;
            }

            if (this.opts.valueMode === false || this.target) return;

            const item = this.itemOf(e.target);
            if (item) {
                if (e.key === 'ArrowLeft' || e.key === 'ArrowRight') {
                    const sibling = (e.key === 'ArrowRight' ? item.nextElementSibling : item.previousElementSibling) as HTMLElement | null;
                    sibling?.focus();
                    e.preventDefault();
                }
                return;
            }

            const hit = this.keyOf(e.target);
            if (!hit || ['ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown', 'Home', 'End'].indexOf(e.key) < 0) return;

            const next = this.neighbor(hit.element, e.key);
            if (next) {
                e.preventDefault();
                this.focusKey(next);
            }
        }

        private visibleKeys(): HTMLElement[] {
            const layer = this.layerElement(this.shown);
            return layer ? Array.from(layer.querySelectorAll(':scope > button[data-bit-vkb-k]')) as HTMLElement[] : [];
        }

        // The key next to another in a direction: the nearest along it, keeping to the same row or column.
        private neighbor(from: HTMLElement, direction: string): HTMLElement | null {
            const keys = this.visibleKeys();
            if (direction === 'Home') return keys[0] ?? null;
            if (direction === 'End') return keys[keys.length - 1] ?? null;

            const a = from.getBoundingClientRect();
            const ax = a.left + a.width / 2, ay = a.top + a.height / 2;
            let best: HTMLElement | null = null;
            let bestScore = Infinity;

            for (const element of keys) {
                if (element === from) continue;

                const b = element.getBoundingClientRect();
                const dx = b.left + b.width / 2 - ax, dy = b.top + b.height / 2 - ay;
                const along = direction === 'ArrowLeft' ? -dx : direction === 'ArrowRight' ? dx : direction === 'ArrowUp' ? -dy : dy;
                const across = direction === 'ArrowLeft' || direction === 'ArrowRight' ? Math.abs(dy) : Math.abs(dx);
                if (along <= 1) continue;

                const score = along + across * 3;
                if (score < bestScore) {
                    bestScore = score;
                    best = element;
                }
            }

            return best;
        }

        private focusKey(element: HTMLElement) {
            for (const key of Array.from(this.root.querySelectorAll('[data-bit-vkb-k]'))) key.setAttribute('tabindex', '-1');
            element.setAttribute('tabindex', '0');
            element.focus();
        }

        // The tab stop of a keyboard typing into its own Value stays on a key of the layer shown.
        private updateRoving() {
            if (this.opts.valueMode === false || this.target || this.opts.disabled) return;

            const keys = this.visibleKeys();
            if (keys.length === 0 || keys.some(k => k.getAttribute('tabindex') === '0')) return;

            const focused = this.root.contains(document.activeElement);
            for (const key of Array.from(this.root.querySelectorAll('[data-bit-vkb-k]'))) key.setAttribute('tabindex', '-1');
            keys[0].setAttribute('tabindex', '0');
            if (focused && document.activeElement instanceof HTMLElement && this.visibleKeys().indexOf(document.activeElement) < 0) keys[0].focus();
        }

        // A physical key held while the keyboard is attached lights its key; Caps Lock follows the physical one.
        private readonly onDocumentKeyDown = (e: KeyboardEvent) => this.onPhysicalKey(e, true);

        private readonly onDocumentKeyUp = (e: KeyboardEvent) => this.onPhysicalKey(e, false);

        private onPhysicalKey(e: KeyboardEvent, down: boolean) {
            if (this.dispatching || e.isTrusted === false || this.secure || !this.target || Utils.isComposingKey(e)) return;

            if (typeof e.getModifierState === 'function' && this.opts.capsLayer) {
                const caps = e.getModifierState('CapsLock');
                if (caps !== this.caps) {
                    this.caps = caps;
                    this.render();
                }
            }

            const action = e.key === ' ' ? 'Space' : e.key;
            for (const element of this.visibleKeys()) {
                const key = this.opts.keys[Number(element.getAttribute('data-bit-vkb-k'))];
                if (!key) continue;

                if (key.a === 'Character' ? key.o === e.key : key.a === action) {
                    if (down) element.setAttribute('data-bit-vkb-down', '');
                    else element.removeAttribute('data-bit-vkb-down');
                }
            }
        }

        // ---------------------------------------------------------------- feedback

        private feedback() {
            if (this.secure) return;

            if (this.opts.haptics && typeof navigator.vibrate === 'function') {
                try { navigator.vibrate(10); } catch { /* not allowed before a user activation */ }
            }

            if (this.opts.sound) {
                try {
                    const Context = window.AudioContext || (window as any).webkitAudioContext;
                    if (!Context) return;

                    const audio = this.audio = this.audio ?? new Context();
                    if (audio.state === 'suspended') audio.resume();

                    const oscillator = audio.createOscillator();
                    const gain = audio.createGain();
                    oscillator.frequency.value = 1100;
                    gain.gain.setValueAtTime(0.04, audio.currentTime);
                    gain.gain.exponentialRampToValueAtTime(0.0001, audio.currentTime + 0.03);
                    oscillator.connect(gain).connect(audio.destination);
                    oscillator.start();
                    oscillator.stop(audio.currentTime + 0.03);
                } catch { /* no audio */ }
            }
        }

        public dispose() {
            if (this.disposed) return;

            for (const [pointerId, press] of Array.from(this.presses.entries())) this.endPress(pointerId, press);
            this.explicitTarget = null;
            this.detach();
            this.applyFollowFocus(this.opts.followFocus, false);
            this.disposed = true;

            this.root.removeEventListener('pointerdown', this.onPointerDown);
            this.root.removeEventListener('pointermove', this.onPointerMove);
            this.root.removeEventListener('pointerup', this.onPointerUp);
            this.root.removeEventListener('pointercancel', this.onPointerCancel);
            this.root.removeEventListener('click', this.onClick);
            this.root.removeEventListener('contextmenu', this.onContextMenu);
            this.root.removeEventListener('keydown', this.onRootKeyDown);
            document.removeEventListener('keydown', this.onDocumentKeyDown, true);
            document.removeEventListener('keyup', this.onDocumentKeyUp, true);
            document.removeEventListener('pointerdown', this.onDocumentPointerDown, true);

            this.resizeObserver?.disconnect();
            this.audio?.close().catch(() => { });
            this.audio = null;
        }
    }
}
