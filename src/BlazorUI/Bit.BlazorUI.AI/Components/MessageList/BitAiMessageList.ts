namespace BitBlazorUI {
    // The browser-side half of BitAiMessageList: it tells .NET whether the reader is at the newest message, and keeps
    // the transcript there while a reply grows - which is what lets auto-scroll follow new content only while the
    // reader has not scrolled up to re-read something.
    export class AiMessageList {
        // The distance from the bottom, in pixels, that still counts as being at the bottom: a reply that grows by a
        // line between two frames must not read as the reader having scrolled away.
        private static readonly THRESHOLD = 48;

        private static _observers = new Map<string, AiMessageListObserver>();

        // The list has been scrolled to its newest message by the time it is observed, so the observer starts pinned, as
        // .NET does, and reports only a transition away from there. autoScroll is BitAiUiOptions.AutoScroll: without it
        // the view never follows the content on its own, whatever the reader's position.
        public static observe(id: string, element: HTMLElement, dotnetObj: DotNetObject, autoScroll: boolean) {
            AiMessageList.dispose(id);

            if (!element) return;

            AiMessageList._observers.set(id, new AiMessageListObserver(element, dotnetObj, AiMessageList.THRESHOLD, autoScroll !== false));
        }

        // The options of a list can change while it is open (a BitAiOptionsProvider re-rendering with others).
        public static setAutoScroll(id: string, autoScroll: boolean) {
            AiMessageList._observers.get(id)?.setAutoScroll(autoScroll !== false);
        }

        public static scrollToBottom(element: HTMLElement, smooth: boolean) {
            if (!element) return;

            // An explicit behavior wins over the stylesheet, so the reduced-motion preference is honored here too: the
            // motion factor the stylesheets scale every duration by is 0 under it, and 1 again inside a ForceAnimation
            // (.bit-fam) subtree or wherever an app restores the motion tokens.
            const animate = smooth && AiMessageList._motionFactor(element) > 0;

            element.scrollTo({ top: element.scrollHeight, behavior: animate ? 'smooth' : 'auto' });
        }

        public static dispose(id: string) {
            const observer = AiMessageList._observers.get(id);
            if (!observer) return;

            AiMessageList._observers.delete(id);
            observer.dispose();
        }

        private static _motionFactor(element: HTMLElement): number {
            try {
                const value = parseFloat(getComputedStyle(element).getPropertyValue('--bit-mot-duration-factor'));

                if (isFinite(value)) return Math.max(0, value);
            } catch { }

            return 1;
        }
    }

    // Watches one transcript. The scroll container itself rarely changes size; what grows while a reply streams is the
    // content inside it. The resize observer therefore watches the first child, re-targeting if that child is replaced,
    // and a mutation observer catches growth that changes no element's box, such as text appended to a paragraph that
    // already spans the width. The position is reported to .NET only when it crosses the threshold, so scrolling costs
    // at most one interop call per transition rather than one per frame.
    // Growth is followed only while auto-scroll is on; while it is off the growth moves the newest message away from a
    // reader who stays put, which is a transition like a scroll, so it is measured and reported instead.
    class AiMessageListObserver {
        private _pinned = true;
        private _reportFrame = 0;
        private _followFrame = 0;
        private _observed: Element | null = null;
        private _disposed = false;
        private _resize: ResizeObserver;
        private _mutation: MutationObserver;
        private _onScroll: () => void;

        constructor(private _element: HTMLElement, private _dotnetObj: DotNetObject, private _threshold: number, private _autoScroll: boolean) {
            this._onScroll = () => this._scheduleReport();

            this._resize = new ResizeObserver(() => this._follow());

            this._mutation = new MutationObserver(() => {
                this._retarget();
                this._follow();
            });

            // No report as it starts: the list was scrolled to its newest message before it was observed, and measured
            // here, ahead of the layout of what the first render added, it would read as a reader who scrolled away and
            // came straight back (a false and then a true on every open). A growth since then is followed (or, without
            // auto-scroll, measured) from the observers' first callbacks.
            _element.addEventListener('scroll', this._onScroll, { passive: true });
            this._retarget();
            this._mutation.observe(_element, { childList: true, subtree: true, characterData: true });
        }

        public setAutoScroll(autoScroll: boolean) {
            this._autoScroll = autoScroll;
        }

        private _scheduleReport() {
            if (this._disposed || this._reportFrame) return;

            this._reportFrame = requestAnimationFrame(() => this._report());
        }

        private _report() {
            this._reportFrame = 0;

            if (this._disposed) return;

            const el = this._element;
            const pinned = el.scrollHeight - el.scrollTop - el.clientHeight <= this._threshold;

            if (pinned === this._pinned) return;

            this._pinned = pinned;
            this._dotnetObj.invokeMethodAsync('OnPinnedChange', pinned).catch(() => { });
        }

        private _follow() {
            if (this._disposed) return;

            if (!this._autoScroll) {
                this._scheduleReport();
                return;
            }

            if (!this._pinned || this._followFrame) return;

            this._followFrame = requestAnimationFrame(() => {
                this._followFrame = 0;

                if (this._disposed || !this._pinned || !this._autoScroll) return;

                this._element.scrollTop = this._element.scrollHeight;
            });
        }

        private _retarget() {
            const content = this._element.firstElementChild;
            if (content === this._observed) return;

            if (this._observed) {
                this._resize.unobserve(this._observed);
            }

            this._observed = content;

            if (content) {
                this._resize.observe(content);
            }
        }

        public dispose() {
            this._disposed = true;
            this._element.removeEventListener('scroll', this._onScroll);
            this._resize.disconnect();
            this._mutation.disconnect();

            if (this._reportFrame) cancelAnimationFrame(this._reportFrame);
            if (this._followFrame) cancelAnimationFrame(this._followFrame);

            this._reportFrame = 0;
            this._followFrame = 0;
            this._observed = null;
        }
    }
}

// bit-lazy: teardown AiMessageList.dispose
