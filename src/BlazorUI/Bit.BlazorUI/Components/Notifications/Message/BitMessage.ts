namespace BitBlazorUI {
    export class Message {
        private static _observers: Record<string, MessageOverflowObserver> = {};

        // Watches a truncated message for whether any of its text is actually clipped, so the button that
        // unfolds it is only offered where there is something folded away to unfold. The answer is measured
        // again whenever the message resizes or anything inside it changes, and only a change of it is sent.
        // The .NET reference is the component's to dispose: it outlives an observer, which is stopped and
        // started again as the message leaves the page and comes back.
        public static observeOverflow(id: string, root: HTMLElement, dotnetObj: DotNetObject) {
            if (!root || !(root instanceof Element)) return;

            Message.dispose(id);

            try {
                const observer = new MessageOverflowObserver(root, dotnetObj);
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
        private _resize: ResizeObserver | undefined;
        private _mutation: MutationObserver | undefined;

        constructor(private _root: HTMLElement, private _dotnetObj: DotNetObject) { }

        public start() {
            const schedule = () => this.schedule();

            this._resize = new ResizeObserver(schedule);
            this._resize.observe(this._root);

            // A change of the text, or of the classes that fold and unfold it, changes what is clipped
            // without necessarily changing the size of the message.
            this._mutation = new MutationObserver(schedule);
            this._mutation.observe(this._root, {
                subtree: true,
                childList: true,
                attributes: true,
                characterData: true,
                attributeFilter: ['class', 'style', 'aria-expanded']
            });

            this.schedule();
        }

        public dispose() {
            if (this._frame) cancelAnimationFrame(this._frame);
            this._frame = 0;

            this._resize?.disconnect();
            this._mutation?.disconnect();
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

            // An unfolded message is unclipped on purpose, which says nothing about the folded one, so it is
            // not measured - and the first measurement once it is folded again is always sent.
            const expander = Array.from(this._root.querySelectorAll('.bit-msg-exb')).find(own);
            if (expander && expander.getAttribute('aria-expanded') === 'true') {
                this._last = undefined;
                return;
            }

            let clipped = false;

            this._root.querySelectorAll<HTMLElement>('.bit-msg-ttl, .bit-msg-cnt').forEach(el => {
                if (clipped || own(el) === false) return;

                // A capped multiline text is clipped at the bottom, a single line at the end of the line.
                clipped = el.classList.contains('bit-msg-clp')
                    ? el.scrollHeight > el.clientHeight + 1
                    : getComputedStyle(el).whiteSpace === 'nowrap' && el.scrollWidth > el.clientWidth + 1;
            });

            if (clipped === this._last) return;

            this._last = clipped;

            this._dotnetObj.invokeMethodAsync('OnOverflowChange', clipped).catch(() => { });
        }
    }
}
