namespace BitBlazorUI {
    // Hover tooltip for the BitResourceTimeline.
    //
    // Owns a single position:fixed element appended to <body>, reused across
    // hovers, plus the show-delay timer and the flip-at-the-viewport-edge
    // positioning. Kept out of the engine because none of it depends on timeline
    // state: the engine decides *what* is hovered, this decides how it is shown.
    //
    // The element lives on <body> rather than inside the timeline so it is never
    // clipped by the scroll container or the sticky axes. Where the browser has
    // the Popover API it is a manual popover, shown into the top layer while it
    // is up: a timeline inside a modal <dialog> (which is in the top layer itself)
    // would otherwise have its tooltip painted under the dialog, whatever the
    // z-index. Its cosmetic look (size
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
        // better than making the user wait out the delay again. Empty content
        // shows nothing, and takes down a tooltip that is up.
        public show(subject: any, content: string, delayMs: number): void {
            if (subject === this._subject) {
                // The same subject may describe itself differently by now (a bar
                // whose times were edited, a +N marker hiding other bars): the
                // text is kept current, and rewritten only when it changed.
                if (content === this._content) {
                    if (this._visible) this._position();
                    return;
                }
                this._content = content;
                if (this._visible) {
                    this._render();
                } else if (content && this._timer === null) {
                    // Nothing was waiting to show (the subject had nothing to say
                    // until now): the delay starts over, as for a new subject. A
                    // show already waiting keeps its timer and takes the new text.
                    this._schedule(delayMs);
                }
                return;
            }
            this._subject = subject;
            this._content = content;
            if (this._visible) {
                this._render();
            } else {
                this._schedule(delayMs);
            }
        }

        public hide(): void {
            clearTimeout(this._timer as number);
            this._timer = null;
            this._subject = null;
            this._content = '';
            if (this._visible && this._el) {
                this._el.style.display = 'none';
                ResourceTimelineTooltip._setOpen(this._el, false);
            }
            this._visible = false;
        }

        // Replaces the presentation the engine's config controls (a theme or an
        // options change), on the element as well once it exists. A tooltip that
        // is up is placed again, since a new font changes its size.
        public setStyle(style: ResourceTimelineTooltipStyle): void {
            this.style = style;
            const el = this._el;
            if (!el) return;
            const s = el.style;
            s.font = style.font;
            s.background = style.background;
            s.color = style.color;
            if (this._visible) this._position();
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
            if (ResourceTimelineTooltip._hasPopover(el)) {
                el.setAttribute('popover', 'manual');
                // A popover is centered in the viewport by its UA style; this
                // one is placed from the left and the top instead.
                s.inset = 'auto';
                s.margin = '0';
            }
            document.body.appendChild(el);
            this._el = el;
            return el;
        }

        private static _hasPopover(el: HTMLElement): boolean {
            return typeof (el as any).showPopover === 'function';
        }

        // Puts the element into the top layer (or takes it out) where it is a
        // popover. Shown again on every show, so it goes above a dialog that was
        // opened since the last one.
        private static _setOpen(el: HTMLElement, open: boolean): void {
            if (!ResourceTimelineTooltip._hasPopover(el)) return;
            try {
                const isOpen = el.matches(':popover-open');
                if (open && !isOpen) (el as any).showPopover();
                else if (!open && isOpen) (el as any).hidePopover();
            } catch { /* not connected: shown (or hidden) as an element of <body> */ }
        }

        private _schedule(delayMs: number): void {
            clearTimeout(this._timer as number);
            this._timer = setTimeout(() => {
                this._timer = null;
                this._render();
            }, delayMs || 0);
        }

        // Shows the content, or takes the tooltip down while there is none (the
        // subject is kept, so the content it gets later shows after the delay).
        private _render(): void {
            if (!this._content) {
                if (this._visible && this._el) {
                    this._el.style.display = 'none';
                    ResourceTimelineTooltip._setOpen(this._el, false);
                }
                this._visible = false;
                return;
            }
            const el = this._ensureElement();
            el.textContent = this._content;
            el.style.display = 'block';
            if (!this._visible) ResourceTimelineTooltip._setOpen(el, true);
            this._visible = true;
            this._position();
        }

        // Positions near the pointer, flipping to the opposite side when it would
        // overflow the viewport.
        private _position(): void {
            const el = this._el;
            if (!el) return;
            const rect = el.getBoundingClientRect();
            const { left, top } = ResourceTimelineTooltip.place(rect.width, rect.height, this._clientX, this._clientY, 12, 12);
            el.style.left = left + 'px';
            el.style.top = top + 'px';
        }

        // Where a box of the given size goes beside the pointer, in viewport
        // coordinates: `gapX` / `gapY` past it, flipped to its other side where it
        // would overflow the viewport, and kept 4px inside its top and left edges.
        // Shared with the overlay of the TooltipTemplate, so both tooltips land
        // the same way.
        public static place(width: number, height: number, clientX: number, clientY: number, gapX: number, gapY: number): { left: number; top: number } {
            let left = clientX + gapX;
            let top = clientY + gapY;
            if (left + width > window.innerWidth - 4) left = clientX - width - gapX;
            if (top + height > window.innerHeight - 4) top = clientY - height - gapY;
            return { left: Math.max(4, left), top: Math.max(4, top) };
        }
    }
}
