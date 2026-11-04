namespace BitBlazorUI {
    // Hover tooltip for the BitResourceTimeline.
    //
    // Owns a single position:fixed element appended to <body>, reused across
    // hovers, plus the show-delay timer and the flip-at-the-viewport-edge
    // positioning. Kept out of the engine because none of it depends on timeline
    // state: the engine decides *what* is hovered, this decides how it is shown.
    //
    // The element lives on <body> rather than inside the timeline so it is never
    // clipped by the scroll container or the sticky axes. Its cosmetic look (size
    // cap, padding, corners, line height, shadow, the pre-line wrapping its
    // multi-line text needs) comes from the stylesheet's bit-rtm-ttp class; only
    // what positions it and what the engine's config controls is written inline.

    /** The presentation of the tooltip that the engine's config controls. */
    export interface ResourceTimelineTooltipStyle {
        font: string;
        background: string;
        color: string;
    }

    export class ResourceTimelineTooltip {
        public style: ResourceTimelineTooltipStyle;

        private _el: HTMLElement | null;
        private _timer: number | null;
        private _visible: boolean;
        // The subject currently described. Compared by identity so moving
        // within one bar does not rebuild or re-arm anything.
        private _subject: any;
        private _content: string;
        private _clientX: number;
        private _clientY: number;

        // `style` supplies the presentation the engine's config controls:
        // { font, background, color }.
        constructor(style: ResourceTimelineTooltipStyle) {
            this.style = style;
            this._el = null;
            this._timer = null;
            this._visible = false;
            this._subject = null;
            this._content = '';
            this._clientX = 0;
            this._clientY = 0;
        }

        // Follows the pointer. Cheap; safe to call on every move.
        public trackPointer(clientX: number, clientY: number): void {
            this._clientX = clientX;
            this._clientY = clientY;
        }

        // Shows `content` for `subject` after `delayMs`. Moving to a different
        // subject while a tooltip is already up swaps it immediately, which reads
        // better than making the user wait out the delay again.
        public show(subject: any, content: string, delayMs: number): void {
            if (subject === this._subject) {
                if (this._visible) this._position();
                return;
            }
            this._subject = subject;
            this._content = content;
            if (this._visible) {
                this._render();
            } else {
                clearTimeout(this._timer as number);
                this._timer = setTimeout(() => this._render(), delayMs || 0);
            }
        }

        public hide(): void {
            clearTimeout(this._timer as number);
            this._timer = null;
            this._subject = null;
            this._content = '';
            if (this._visible && this._el) {
                this._el.style.display = 'none';
            }
            this._visible = false;
        }

        // Replaces the presentation the engine's config controls (a theme or an
        // options change), on the element as well once it exists.
        public setStyle(style: ResourceTimelineTooltipStyle): void {
            this.style = style;
            const el = this._el;
            if (!el) return;
            const s = el.style;
            s.font = style.font;
            s.background = style.background;
            s.color = style.color;
        }

        public dispose(): void {
            clearTimeout(this._timer as number);
            this._timer = null;
            if (this._el) {
                this._el.remove();
                this._el = null;
            }
            this._visible = false;
            this._subject = null;
        }

        private _ensureElement(): HTMLElement {
            if (this._el) return this._el;
            const el = document.createElement('div');
            el.className = 'bit-rtm-ttp';
            const s = el.style;
            s.position = 'fixed';
            s.zIndex = '2147483647';
            s.pointerEvents = 'none';
            s.font = this.style.font;
            s.background = this.style.background;
            s.color = this.style.color;
            s.display = 'none';
            document.body.appendChild(el);
            this._el = el;
            return el;
        }

        private _render(): void {
            if (!this._content) return;
            const el = this._ensureElement();
            el.textContent = this._content;
            el.style.display = 'block';
            this._visible = true;
            this._position();
        }

        // Positions near the pointer, flipping to the opposite side when it would
        // overflow the viewport.
        private _position(): void {
            const el = this._el;
            if (!el) return;
            const pad = 12;
            const rect = el.getBoundingClientRect();
            let left = this._clientX + pad;
            let top = this._clientY + pad;
            if (left + rect.width > window.innerWidth - 4) left = this._clientX - rect.width - pad;
            if (top + rect.height > window.innerHeight - 4) top = this._clientY - rect.height - pad;
            el.style.left = Math.max(4, left) + 'px';
            el.style.top = Math.max(4, top) + 'px';
        }
    }
}
