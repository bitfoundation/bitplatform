namespace BitBlazorUI {
    export class LongPress {
        private static _longPresses: BitLongPress[] = [];

        public static setup(
            id: string,
            element: HTMLElement,
            delay: number,
            moveTolerance: number,
            touchOnly: boolean,
            keyboardTrigger: boolean,
            dotnetObj: DotNetObject) {

            // A setup for an id that is still registered would leave the previous listeners attached and its .NET
            // reference undisposed, so the old long press is torn down before the new one takes over.
            LongPress.dispose(id);

            let timer = 0;
            let pointerId = -1;
            let pointerType = '';
            let startX = 0;
            let startY = 0;
            // The click the release of a long press makes is swallowed, so the content under it is not also clicked;
            // it is only waited for briefly, since a release that makes no click must not swallow the next one.
            let swallowClick = false;
            let swallowClickTimer = 0;
            // The contextmenu event a held touch also raises (Android) and the one the Menu key raises after its
            // keydown (Windows) belong to the long press, not to the browser's own menu.
            let swallowContextMenu = false;

            const isDisabled = () => element.classList.contains('bit-dis');

            // Everything a press holds is let go of in one place, so a release, a move, a scroll and a dispose cannot
            // each forget a different part of it.
            const cancel = () => {
                if (timer) window.clearTimeout(timer);
                timer = 0;
                pointerId = -1;
                element.classList.remove('bit-lpr-prs');
                window.removeEventListener('scroll', cancel, true);
            };

            const fire = () => {
                const x = startX;
                const y = startY;
                const type = pointerType;

                timer = 0;
                swallowClick = true;
                swallowContextMenu = type !== 'mouse';
                element.classList.remove('bit-lpr-prs');
                window.removeEventListener('scroll', cancel, true);

                Utils.notifyDotNet(dotnetObj, 'OnLongPress', x, y, type);
            };

            const onPointerDown = (e: PointerEvent) => {
                // A second finger is a pinch or a two-finger scroll, never a long press of the first one.
                if (!e.isPrimary) {
                    cancel();
                    return;
                }
                if (isDisabled()) return;
                if (e.pointerType === 'mouse' && (touchOnly || e.button !== 0)) return;

                cancel();
                swallowClick = false;
                swallowContextMenu = false;

                pointerId = e.pointerId;
                pointerType = e.pointerType;
                startX = e.clientX;
                startY = e.clientY;

                element.classList.add('bit-lpr-prs');
                // A scroll of the page or of any scrolling ancestor moves the content away from under the press.
                window.addEventListener('scroll', cancel, { capture: true, passive: true });
                timer = window.setTimeout(fire, delay);
            };

            const onPointerMove = (e: PointerEvent) => {
                if (!timer || e.pointerId !== pointerId) return;

                const dx = e.clientX - startX;
                const dy = e.clientY - startY;
                if (Math.sqrt(dx * dx + dy * dy) > moveTolerance) cancel();
            };

            const onPointerUp = (e: PointerEvent) => {
                if (e.pointerId !== pointerId && pointerId !== -1) return;

                cancel();

                if (swallowClick) {
                    if (swallowClickTimer) window.clearTimeout(swallowClickTimer);
                    swallowClickTimer = window.setTimeout(() => { swallowClick = false; swallowClickTimer = 0; }, 400);
                }
            };

            // The browser cancels the pointer of a touch it takes for a scroll or a zoom, which calls the press off.
            const onPointerCancel = (e: PointerEvent) => {
                if (e.pointerId !== pointerId) return;
                cancel();
            };

            const onClick = (e: MouseEvent) => {
                if (!swallowClick) return;

                swallowClick = false;
                e.preventDefault();
                e.stopPropagation();
            };

            const onContextMenu = (e: MouseEvent) => {
                // A held touch or pen raises a contextmenu of its own on some browsers, before or after the press is long:
                // the press is the long press's, so the menu of the browser is not brought up over it.
                if ((timer && pointerType !== 'mouse') || swallowContextMenu) {
                    swallowContextMenu = false;
                    e.preventDefault();
                }
            };

            const isTypingTarget = (target: EventTarget | null) => {
                if (!(target instanceof HTMLElement)) return false;
                if (target.isContentEditable) return true;

                const tag = target.tagName;
                if (tag === 'TEXTAREA' || tag === 'SELECT') return true;
                if (tag !== 'INPUT') return false;

                const type = (target as HTMLInputElement).type;
                return type !== 'button' && type !== 'submit' && type !== 'reset' && type !== 'checkbox' && type !== 'radio' &&
                       type !== 'range' && type !== 'color' && type !== 'file' && type !== 'image';
            };

            // The keyboard's long press: the keys that bring up a context menu. Taken on the root and on anything in it
            // but a field being typed in, whose own context menu is the editing one; a key another long press inside
            // this one already took is not taken twice.
            const onKeyDown = (e: KeyboardEvent) => {
                if (!keyboardTrigger || isDisabled()) return;
                if (e.defaultPrevented || e.repeat || e.altKey || e.ctrlKey || e.metaKey) return;

                const isMenuKey = e.key === 'ContextMenu' || (e.key === 'F10' && e.shiftKey);
                if (!isMenuKey || isTypingTarget(e.target)) return;

                e.preventDefault();
                swallowContextMenu = true;

                // The point a menu opened from the keyboard sits at: the middle of what has the focus, kept on screen.
                const active = document.activeElement;
                const target = active instanceof Element && element.contains(active) ? active : element;
                const rect = target.getBoundingClientRect();
                const left = Math.max(rect.left, 0);
                const right = Math.max(left, Math.min(rect.right, window.innerWidth));
                const top = Math.max(rect.top, 0);
                const bottom = Math.max(top, Math.min(rect.bottom, window.innerHeight));

                Utils.notifyDotNet(dotnetObj, 'OnLongPress', Math.round((left + right) / 2), Math.round((top + bottom) / 2), 'keyboard');
            };

            // A key that raised no contextmenu (macOS has no such key, and not every browser raises one) must not leave
            // the next right-click swallowed.
            const onKeyUp = (e: KeyboardEvent) => {
                if (e.key === 'ContextMenu' || e.key === 'F10') {
                    window.setTimeout(() => { if (!timer) swallowContextMenu = false; }, 0);
                }
            };

            element.addEventListener('pointerdown', onPointerDown);
            element.addEventListener('pointermove', onPointerMove);
            element.addEventListener('pointerup', onPointerUp);
            element.addEventListener('pointercancel', onPointerCancel);
            element.addEventListener('contextmenu', onContextMenu);
            element.addEventListener('keydown', onKeyDown);
            element.addEventListener('keyup', onKeyUp);
            // The click is swallowed in the capture phase so it never reaches the child it was aimed at.
            element.addEventListener('click', onClick, true);

            const longPress = new BitLongPress(id, dotnetObj, () => {
                element.removeEventListener('pointerdown', onPointerDown);
                element.removeEventListener('pointermove', onPointerMove);
                element.removeEventListener('pointerup', onPointerUp);
                element.removeEventListener('pointercancel', onPointerCancel);
                element.removeEventListener('contextmenu', onContextMenu);
                element.removeEventListener('keydown', onKeyDown);
                element.removeEventListener('keyup', onKeyUp);
                element.removeEventListener('click', onClick, true);

                cancel(); // a dispose mid-press must not fire, nor leave the pressing class behind
                if (swallowClickTimer) window.clearTimeout(swallowClickTimer);
            });

            LongPress._longPresses.push(longPress);
        }

        public static dispose(id: string) {
            const longPress = LongPress._longPresses.find(l => l.id === id);
            if (!longPress) return;

            LongPress._longPresses = LongPress._longPresses.filter(l => l.id !== id);
            longPress.dispose();
        }
    }

    class BitLongPress {
        constructor(public id: string, private dotnetObj: DotNetObject, private removeHandlers: () => void) { }

        public dispose() {
            this.removeHandlers();
            this.dotnetObj?.dispose();
        }
    }
}

// bit-lazy: teardown LongPress.dispose
