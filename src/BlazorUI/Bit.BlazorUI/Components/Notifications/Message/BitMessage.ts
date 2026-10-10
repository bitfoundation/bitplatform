namespace BitBlazorUI {
    export class Message {
        private static _observers: Record<string, MessageOverflowObserver> = {};

        // Watches a truncated message for whether any of its text is actually clipped, so the button that
        // unfolds it is only offered where there is something folded away to unfold - or, with reflow, an
        // auto-multiline message for whether its single line is too short, so it only wraps where it has to.
        // The answer is measured again whenever the message resizes or anything inside it changes, and only a
        // change of it is sent. The .NET reference is the component's to dispose: it outlives an observer,
        // which is stopped and started again as the message leaves the page and comes back.
        public static observeOverflow(id: string, root: HTMLElement, dotnetObj: DotNetObject, reflow: boolean) {
            if (!root || !(root instanceof Element)) return;

            Message.dispose(id);

            try {
                const observer = new MessageOverflowObserver(root, dotnetObj, !!reflow);
                Message._observers[id] = observer;
                observer.start();
            } catch (err) {
                console.error("BitBlazorUI.Message.observeOverflow:", err);
            }
        }

        public static dispose(id: string) {
            const observer = Message._observers[id];
            if (!observer) return;

            delete Message._observers[id];
            observer.dispose();
        }
    }

    class MessageOverflowObserver {
        private _frame = 0;
        private _last: boolean | undefined;
        // The width the message had when its single line stopped fitting, while it is wrapped because of it.
        private _reflowedAt: number | undefined;
        private _resize: ResizeObserver | undefined;
        private _mutation: MutationObserver | undefined;
        private _schedule = () => this.schedule();

        constructor(private _root: HTMLElement, private _dotnetObj: DotNetObject, private _reflow: boolean) { }

        public start() {
            const schedule = this._schedule;

            this._resize = new ResizeObserver(schedule);
            this._resize.observe(this._root);

            // A change of the text, or of the classes that fold and unfold it, changes what is clipped
            // without necessarily changing the size of the message. Attributes only count on the elements the
            // fold is made of: the rest of the message (the paused progress bar, a component in the content
            // restyling itself every frame) changes them all the time without changing what is clipped.
            this._mutation = new MutationObserver(records => {
                if (records.some(r => this.affectsFold(r))) this.schedule();
            });
            this._mutation.observe(this._root, {
                subtree: true,
                childList: true,
                attributes: true,
                characterData: true,
                attributeFilter: ['class', 'style', 'aria-expanded']
            });

            // A web font that finishes loading changes how wide the text is, and neither of the above sees it.
            document.fonts?.addEventListener('loadingdone', schedule);

            this.schedule();
        }

        public dispose() {
            if (this._frame) cancelAnimationFrame(this._frame);
            this._frame = 0;

            this._resize?.disconnect();
            this._mutation?.disconnect();

            document.fonts?.removeEventListener('loadingdone', this._schedule);
        }

        private affectsFold(record: MutationRecord) {
            if (record.type !== 'attributes') return true;

            const target = record.target as Element;

            return target === this._root || target.matches('.bit-msg-ttl, .bit-msg-cnt, .bit-msg-exb');
        }

        // Resizing fires in bursts, so the measurement is taken once per frame at most.
        private schedule() {
            if (this._frame) return;

            this._frame = requestAnimationFrame(() => {
                this._frame = 0;
                this.measure();
            });
        }

        private measure() {
            if (this._root.isConnected === false) return;

            // A message can hold another one in its content, whose parts are not this one's to measure.
            const own = (el: Element) => el.closest('.bit-msg') === this._root;

            if (this._reflow) {
                this.measureReflow(own);
                return;
            }

            // An unfolded message is unclipped on purpose, which says nothing about the folded one, so it is
            // not measured - and the first measurement once it is folded again is always sent.
            const expander = Array.from(this._root.querySelectorAll('.bit-msg-exb')).find(own);
            if (expander && expander.getAttribute('aria-expanded') === 'true') {
                this._last = undefined;
                return;
            }

            this.send(this.isClipped(own));
        }

        // A wrapped message has nothing clipped on purpose, which says nothing about whether one line would fit it
        // again, so it is not measured for that. It goes back to one line once it is wider than it was when the line
        // stopped fitting, and is measured afresh there: still too short, and it wraps again, remembering the new
        // width. Each round needs more room than the last, so the two layouts never chase each other.
        private measureReflow(own: (el: Element) => boolean) {
            const width = this._root.getBoundingClientRect().width;

            if (this._reflowedAt !== undefined) {
                if (width <= this._reflowedAt) return;

                this._reflowedAt = undefined;
                this.send(false);
                return;
            }

            const clipped = this.isClipped(own);

            if (clipped) this._reflowedAt = width;

            this.send(clipped);
        }

        private isClipped(own: (el: Element) => boolean) {
            let clipped = false;

            this._root.querySelectorAll<HTMLElement>('.bit-msg-ttl, .bit-msg-cnt').forEach(el => {
                if (clipped || own(el) === false) return;

                // A capped multiline text is clipped at the bottom, a single line at the end of the line.
                clipped = el.classList.contains('bit-msg-clp')
                    ? el.scrollHeight > el.clientHeight + 1
                    : getComputedStyle(el).whiteSpace === 'nowrap' && el.scrollWidth > el.clientWidth + 1;
            });

            return clipped;
        }

        private send(clipped: boolean) {
            if (clipped === this._last) return;

            this._last = clipped;

            this._dotnetObj.invokeMethodAsync('OnOverflowChange', clipped).catch(() => { });
        }
    }
}
