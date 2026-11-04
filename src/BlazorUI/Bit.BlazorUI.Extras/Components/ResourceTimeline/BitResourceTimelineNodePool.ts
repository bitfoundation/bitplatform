namespace BitBlazorUI {
    // Element reuse for the retained-mode renderers (HTML and SVG) of the BitResourceTimeline.
    //
    // Both renderers paint a fresh scene every frame, but the scene is viewport
    // culled and its shape barely changes between frames: the same bars, ticks and
    // grid lines, at slightly different coordinates. Rebuilding the subtree each
    // time meant thousands of createElement calls and a full tree teardown per
    // frame - by far the dominant cost in those renderers.
    //
    // A pool keeps the elements of one container across frames. Each frame walks
    // the scene in the same order and calls acquire() for each node it needs; the
    // element already sitting at that position is reused when it is the right kind,
    // so the common frame does no DOM construction at all and only writes the
    // properties that actually changed. Surplus elements left over from a busier
    // previous frame are hidden rather than removed, so they are ready to be reused
    // again as soon as the scene grows back.

    /**
     * The bookkeeping the pool and the renderers keep on the elements they reuse, as expando properties of the
     * element itself: what was last written to it, so an unchanged write can be skipped.
     */
    export interface ResourceTimelinePooledNodeState {
        /** The tag the pool created the element with. */
        __tag?: string;
        /** Whether the pool hid the element at the end of a frame that did not use it. */
        __hidden?: boolean;
        /** The display the element gets back when a hidden element is reused (unset: the default display). */
        __display?: string;
        /** The style properties last written by applyStyle. */
        __style?: { [key: string]: string };
        /** The attributes last written by applyAttrs. */
        __attrs?: { [key: string]: string | number };
        /** The text last written by setText. */
        __text?: string | null;
        /** The allocation id last written to the element's data-bar-id. */
        __barId?: string;
        /** The class last written to an HTML bar element. */
        __className?: string;
        /** The font last written to an SVG text element. */
        __font?: string;
        /** The source last written to an HTML icon image. */
        __src?: string;
    }

    /** An element of a pool: an HTML or an SVG element, carrying the pool's bookkeeping. */
    export type ResourceTimelinePooledNode = (HTMLElement | SVGElement) & ResourceTimelinePooledNodeState;

    // One pool per homogeneous run of elements - all the grid lines, all the bar
    // rects, all the hour labels. Keeping each pool to a single tag is what makes
    // reuse work: a pool fed a varying mix of tags mismatches as soon as the scene
    // shifts by one element, and then rebuilds most of its nodes every frame.
    //
    // Each pool keeps its elements in a container of its own, appended to the
    // parent it is given as the pool is created: an HTML <div> with display:
    // contents (it generates no box, so the elements are still positioned against
    // the parent's), or an SVG <g>. Pools sharing a parent - a bar's edges, rects,
    // sections, outlines, icons and labels - therefore stack in the order they
    // were created, which is the paint order, however each one grows. Appending to
    // the shared parent itself put every new element after all the others, so a
    // bar-rect pool growing on a busier frame covered the labels and outlines.
    export class ResourceTimelineNodePool<TNode extends ResourceTimelinePooledNode = ResourceTimelinePooledNode> {
        /** The pool's own container, a child of the parent it was created with. */
        public parent: Element;
        public namespace: string | null;
        public onHide: ((node: TNode) => void) | null;
        public nodes: TNode[];
        public used: number;

        // `namespace` is null for HTML elements, or the SVG namespace URI.
        // `onHide` is called for each element retired at the end of a frame, for
        // pools whose elements carry identity that must not linger (bar ids).
        constructor(parent: Element, namespace: string | null = null, onHide: ((node: TNode) => void) | null = null) {
            let container: Element;
            if (namespace) {
                container = document.createElementNS(namespace, 'g');
            } else {
                const div = document.createElement('div');
                div.style.display = 'contents';
                container = div;
            }
            parent.appendChild(container);
            this.parent = container;
            this.namespace = namespace;
            this.onHide = onHide;
            this.nodes = [];
            this.used = 0;
        }

        public begin(): void {
            this.used = 0;
        }

        // The next element of the given tag. Reuses the element already at this
        // position when the tag matches, which it does on virtually every frame;
        // otherwise creates one (replacing a mismatched element in place, so
        // document order always matches the order acquire() was called in).
        public acquire(tag: string): TNode {
            const index = this.used++;
            const existing = this.nodes[index];
            if (existing !== undefined && existing.__tag === tag) {
                if (existing.__hidden) {
                    existing.style.display = existing.__display || '';
                    existing.__hidden = false;
                }
                return existing;
            }

            const node = (this.namespace
                ? document.createElementNS(this.namespace, tag)
                : document.createElement(tag)) as TNode;
            node.__tag = tag;

            if (existing !== undefined) {
                this.parent.replaceChild(node, existing);
                this.nodes[index] = node;
            } else {
                this.parent.appendChild(node);
                this.nodes.push(node);
            }
            return node;
        }

        // Hides whatever the previous frame used and this one did not. Hiding
        // rather than removing keeps the elements available for reuse; the pool is
        // only trimmed when the surplus is large enough to be worth reclaiming.
        public end(): void {
            for (let i = this.used; i < this.nodes.length; i++) {
                const node = this.nodes[i];
                if (!node.__hidden) {
                    node.style.display = 'none';
                    node.__hidden = true;
                    if (this.onHide) this.onHide(node);
                }
            }
            // A scene that shrank a lot (scrolling into empty space, collapsing a
            // large group) should not pin its peak element count forever.
            if (this.nodes.length > 64 && this.nodes.length > this.used * 4) {
                for (let i = this.used; i < this.nodes.length; i++) {
                    this.parent.removeChild(this.nodes[i]);
                }
                this.nodes.length = this.used;
            }
        }

        public clear(): void {
            for (const node of this.nodes) {
                if (node.parentNode === this.parent) this.parent.removeChild(node);
            }
            this.nodes.length = 0;
            this.used = 0;
            this.parent.parentNode?.removeChild(this.parent);
        }

        // Applies style properties, writing only those that changed since this element
        // was last used. Unchanged writes are not free (each one touches the CSSOM and
        // can invalidate style), and most properties - colours, fonts, sizes - are
        // identical frame to frame while only coordinates move.
        //
        // Callers must pass the same set of keys for a given element every frame;
        // a key that disappears is not cleared. The renderers' builders all do.
        public static applyStyle(node: ResourceTimelinePooledNode, next: { [key: string]: string }): void {
            let prev = node.__style;
            if (prev === undefined) prev = node.__style = {};
            // The keys are camelCase style properties (boxSizing, whiteSpace), written as properties of the
            // declaration rather than through setProperty, which takes the hyphenated names.
            const style = node.style as unknown as { [key: string]: string };
            for (const key in next) {
                const value = next[key];
                if (prev[key] !== value) {
                    style[key] = value;
                    prev[key] = value;
                }
            }
        }

        // Same idea for SVG presentation attributes.
        public static applyAttrs(node: ResourceTimelinePooledNode, next: { [key: string]: string | number }): void {
            let prev = node.__attrs;
            if (prev === undefined) prev = node.__attrs = {};
            for (const key in next) {
                const value = next[key];
                if (prev[key] !== value) {
                    // String() is the very conversion setAttribute applies to a number itself.
                    node.setAttribute(key, String(value));
                    prev[key] = value;
                }
            }
        }

        // Text content changes far less often than position; skip the write when it
        // matches, since assigning textContent tears down and rebuilds the text node.
        public static setText(node: ResourceTimelinePooledNode, text: string | null): void {
            if (node.__text !== text) {
                node.textContent = text;
                node.__text = text;
            }
        }
    }
}
