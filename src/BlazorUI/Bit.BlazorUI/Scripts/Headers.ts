namespace BitBlazorUI {
    export class Headers {
        private static _entries = new Map<string, {
            element: HTMLElement,
            scrollHandler: () => void,
            layoutHandler: () => void,
            focusHandler: () => void,
            // The scroller is held in a box as well, since it is re-resolved whenever the layout changes
            // and dispose has to take the scroll listener off the target it is bound to at that moment.
            target: { current: HTMLElement | Window },
            observer?: ResizeObserver,
            // Gives up the claims of this header on the scroller (its scroll padding, its published height),
            // whichever box is carrying them at that moment, and puts each property back once no header is
            // holding it anymore.
            clearMeasures: () => void,
            // The pending frame is held in a box rather than in a plain field so the handler can keep
            // writing to the same object that dispose reads from.
            frame: { handle: number },
            // The frame the boxes are watched again in, once the scroller moved (see setup).
            reobserve: { handle: number },
            // The frame the height is written onto the scroller in, once the resize observer reported a change (see setup).
            remeasure: { handle: number }
        }>();

        // The properties a header writes onto the box of its scroller are shared ground: two pinned headers
        // over the same scroller (an app shell whose pane carries both) would each otherwise capture the
        // write of the other as "the value it had before", so the first one to go would wipe the value the
        // second still needs and the second would hand the box back a value the page never had. The value
        // found before any of them touched the box is kept here instead, per property, together with the
        // value each header holding it measured last - in the order they last changed it, so the box carries
        // the one of the header that changed size last, and gets the one of the header that is left when
        // another lets go - and only the last one to let go puts the value found back.
        private static _claims = new WeakMap<HTMLElement, Map<string, { previous: string, priority: string, owners: Map<string, string> }>>();

        // The scroll padding that keeps what is scrolled to out from under the header (ScrollPadding).
        private static readonly SCROLL_PADDING = 'scroll-padding-block-start';

        // The live height of the header, which the content under it offsets itself by (PublishHeight).
        private static readonly HEIGHT_VARIABLE = '--bit-env-header-height';

        // Scroll deltas below this many pixels are ignored, so the rubber-banding of touch devices and
        // the sub-pixel jitter of a trackpad cannot flip the header back and forth on every frame.
        private static readonly THRESHOLD = 4;

        // Drives the two scroll behaviors of the header: sliding it out of the view while the page is
        // scrolled down and bringing it back while it is scrolled up (the classic "reveal" of an app bar),
        // and telling it once the page has left its top (the scrolled state, which the "elevate on scroll"
        // shadow, the Scrolled class-style part, the scrolled variables and OnScrolledChanged all key off -
        // trackScrolled says whether anything does). Both states live here and only cross the interop
        // boundary when they actually flip, so a scroll never costs more than a comparison. It also keeps
        // the scroll padding of the scroller in step with the height of the header, so nothing scrolled to
        // lands underneath a pinned one.
        public static setup(id: string, dotnetObj: DotNetObject, revealOffset: number, elevateOffset: number, reveal: boolean, trackScrolled: boolean, scrollTarget: string | null, scrollPadding: boolean, publishHeight: boolean) {
            Headers.dispose(id);

            const element = document.getElementById(id);
            if (!element) return;

            // What keeps a revealed header on the screen is a focus the keyboard put inside it (see evaluate).
            if (reveal) {
                Utils.watchFocusOrigin();
            }

            // An explicitly named scroller wins over the walk up the tree, which is what a header that
            // does not sit inside the box it reacts to (an app shell whose header and content are
            // siblings) needs. A selector that matches nothing falls back to the walk rather than
            // leaving the header without a scroller at all.
            const resolveTarget = (): HTMLElement | Window => {
                if (scrollTarget) {
                    // A selector that is not valid CSS makes querySelector throw rather than return
                    // nothing, and this runs again on every resize and every layout change, so a typo in
                    // the parameter would otherwise break the setup and then every re-resolution after it.
                    // It is treated as the selector that matched nothing it is.
                    try {
                        const found = document.querySelector(scrollTarget);

                        if (found) return found as HTMLElement;
                    } catch { }
                }

                return Headers.scrollParent(element);
            };

            // The scroller is not always the window: an app shell (and any pane with its own overflow)
            // scrolls its own box, and a scroll event on an element does not bubble to the window.
            const target = { current: resolveTarget() };

            // A negative offset would keep the header hidden at the very top of the scroller, where the
            // rule below is what has to win.
            const offset = Math.max(0, revealOffset || 0);
            const elevation = Math.max(0, elevateOffset || 0);

            let hidden = false;
            // Unknown until the first evaluation, which always reports what it finds: a header set up again (a new
            // offset, scroller or id) may still be marked as scrolled on the .NET side from its previous registration,
            // and a first check finding it at rest has to say so rather than match a guess of "not scrolled".
            let scrolled: boolean | null = null;
            let lastY = Headers.scrollTop(target.current);

            // requestAnimationFrame never hands out a 0 handle, so it doubles as the "no frame pending" mark.
            const frame = { handle: 0 };

            // The properties of the scroller this header keeps in step with its own height.
            const measures: string[] = [];

            if (scrollPadding) {
                measures.push(Headers.SCROLL_PADDING);
            }

            if (publishHeight) {
                measures.push(Headers.HEIGHT_VARIABLE);
            }

            // The box this header is writing its height onto, if it is writing it anywhere.
            let measured: HTMLElement | undefined;

            const clearMeasures = () => {
                if (!measured) return;

                for (const property of measures) {
                    Headers.release(measured, property, id);
                }

                measured = undefined;
            };

            // A pinned header covers the top of the scroller, so anything scrolled to by an anchor, by
            // the browser bringing a focused control into the view, or by scrollIntoView, lands
            // underneath it (WCAG 2.4.11). Reserving the height of the header as the scroll padding of
            // the scroller is what stops the scroll short of it. The same height is published as a
            // custom property of the scroller, which the content inside it offsets itself by instead of
            // a copy of the height a stylesheet would have to keep in step. Both are on the element
            // that scrolls - the documentElement when that is the page - and they are re-read whenever
            // the header or the layout changes size, so a header that shrinks on scroll stays in step.
            const applyMeasures = () => {
                if (measures.length === 0) return;

                const box = target.current === window
                    ? document.documentElement
                    : target.current as HTMLElement;

                if (measured && measured !== box) {
                    clearMeasures();
                }

                measured = box;

                // One measurement serves every property (the published height, the scroll padding), so they
                // never disagree about how tall the header is (see borderBoxHeight).
                const height = `${Headers.borderBoxHeight(element)}px`;

                for (const property of measures) {
                    Headers.claim(box, property, id, height);
                }
            };

            const applyHidden = (next: boolean) => {
                if (!reveal || next === hidden) return;

                hidden = next;

                // The reference is disposed before the listeners are, so a flip of the very last frame can
                // land on a dead reference, and a scroll or a focus as Firefox leaves the page calls over a
                // connection that is already closed. Neither is an error of the application (Utils.notifyDotNet).
                Utils.notifyDotNet(dotnetObj, 'OnRevealChange', hidden);
            };

            const applyScrolled = (next: boolean) => {
                if (!trackScrolled || next === scrolled) return;

                scrolled = next;

                Utils.notifyDotNet(dotnetObj, 'OnScrollChange', scrolled);
            };

            const evaluate = () => {
                frame.handle = 0;

                const y = Headers.scrollTop(target.current);
                const delta = y - lastY;

                let next = hidden;

                if (Math.abs(delta) >= Headers.THRESHOLD) {
                    next = delta > 0;
                    lastY = y;
                }

                // The top always shows the header: within the offset there is nothing to make room for yet,
                // which is also what keeps a header from flickering away on the first few pixels of a scroll
                // that has barely started. Unlike a footer there is no matching rule at the far end - a header
                // that dropped back over the content the moment the page bottomed out would cover the very
                // thing the user scrolled down to reach.
                if (y <= offset) {
                    next = false;
                    lastY = y;
                }

                // Revealing the header when the focus enters it is not enough on its own: a keyboard user
                // who scrolls the page (the arrow keys and the space bar scroll it from a focused button)
                // would roll the focused control out of the view right after (WCAG 2.4.11). The header
                // stays revealed while it holds the keyboard focus. A focus left behind by a click - a text
                // field's included - does not count, or a mouse user who clicked into the bar could never
                // scroll it away again (see Utils.hasKeyboardFocus).
                if (next && Utils.hasKeyboardFocus(element)) {
                    next = false;
                }

                applyHidden(next);
                applyScrolled(y > elevation);
            };

            // rAF coalescing keeps a burst of scroll events down to one evaluation per painted frame.
            const scrollHandler = () => {
                if (frame.handle) return;

                frame.handle = requestAnimationFrame(evaluate);
            };

            // A hidden header is only translated out of the view, so everything inside it is still in the
            // tab order: a keyboard user tabbing into it lands on a control they cannot see. Revealing the
            // header as soon as anything inside it takes the focus keeps that control visible, and the
            // scroll baseline is re-read so the next scroll is measured from here.
            const focusHandler = () => {
                lastY = Headers.scrollTop(target.current);

                applyHidden(false);
            };

            let observer: ResizeObserver | undefined;

            // The scroller is watched as well as the page, since a pane only becomes the scroller of the
            // header once its own content overflows it. The header itself is watched only when its height
            // is being mirrored onto that scroller (its scroll padding, its published height).
            const observe = () => {
                if (!observer) return;

                observer.disconnect();
                observer.observe(document.documentElement);

                if (target.current !== window) {
                    const box = target.current as HTMLElement;

                    observer.observe(box);

                    // A pane of a fixed height keeps the same border box however much content is put into
                    // it, so watching the box alone never reports the growth that turns it into the scroller
                    // of the header. Its content wrapper is the box that actually grows with the content.
                    if (box.firstElementChild) {
                        observer.observe(box.firstElementChild);
                    }
                }

                // Watched by its border box, which is what is measured: the default content box misses a change
                // of the paddings alone - the top safe area inset of the device, a Size that only pads the bar.
                if (measures.length > 0) {
                    observer.observe(element, { box: 'border-box' });
                }
            };

            // The boxes are watched again in a frame of their own rather than right away, since the layout handler
            // runs from the resize observer: a box observed from inside its callback is owed a first notification, and
            // the root element - shallower than any box the callback was delivered for - is one the browser can only
            // deliver in the next frame, which it reports as a "ResizeObserver loop completed with undelivered
            // notifications" error (WebKit as an uncaught one).
            const reobserve = { handle: 0 };

            const scheduleObserve = () => {
                if (reobserve.handle) return;

                reobserve.handle = requestAnimationFrame(() => {
                    reobserve.handle = 0;

                    observe();
                });
            };

            // The height is written in a frame of its own too when the resize observer reports the change, for the
            // same reason: the published height moves the layout of the content offset by it, which resizes the root
            // element (or the content wrapper of a pane) - boxes shallower than the header the callback was delivered
            // for - so a write from inside the callback would raise the same loop error.
            const remeasure = { handle: 0 };

            const scheduleMeasures = () => {
                if (remeasure.handle) return;

                remeasure.handle = requestAnimationFrame(() => {
                    remeasure.handle = 0;

                    applyMeasures();
                });
            };

            // Which box scrolls the header is not settled once and for all: a pane that had nothing to
            // scroll at setup time (so the walk landed on the window) becomes the scroller as soon as its
            // content outgrows it, and the other way round. The scroller is re-resolved whenever the
            // layout or the content moves, and the scroll listener follows it.
            const relayout = (fromObserver: boolean) => {
                const next = resolveTarget();

                if (next !== target.current) {
                    target.current.removeEventListener('scroll', scrollHandler);

                    target.current = next;

                    lastY = Headers.scrollTop(next);

                    next.addEventListener('scroll', scrollHandler, { passive: true });

                    scheduleObserve();
                }

                if (fromObserver) {
                    scheduleMeasures();
                } else {
                    applyMeasures();
                }

                scrollHandler();
            };

            const layoutHandler = () => relayout(false);

            target.current.addEventListener('scroll', scrollHandler, { passive: true });
            window.addEventListener('resize', layoutHandler, { passive: true });
            element.addEventListener('focusin', focusHandler);

            // Content that grows or shrinks on its own (a list that loads more rows, an expanding panel)
            // moves the scroll of its box without any scroll event to announce it. Watching the box that
            // holds the content picks those up, so a header does not stay lifted above a page that just
            // became short enough not to scroll at all.
            if (typeof ResizeObserver !== 'undefined') {
                observer = new ResizeObserver(() => relayout(true));

                observe();
            }

            Headers._entries.set(id, { element, scrollHandler, layoutHandler, focusHandler, target, observer, clearMeasures, frame, reobserve, remeasure });

            applyMeasures();

            // The scroller can already be scrolled when the header arrives (a restored scroll position, a
            // deep link to an anchor, a header rendered into a pane the user has been reading), so the
            // states are settled once up front instead of waiting for a scroll that may never come.
            evaluate();
        }

        public static dispose(id: string) {
            const entry = Headers._entries.get(id);
            if (!entry) return;

            entry.target.current.removeEventListener('scroll', entry.scrollHandler);
            window.removeEventListener('resize', entry.layoutHandler);
            entry.element.removeEventListener('focusin', entry.focusHandler);

            entry.observer?.disconnect();

            entry.clearMeasures();

            // A frame scheduled by the last scroll before the disposal would still evaluate and call back
            // into a component that is on its way out, so it is dropped along with the listeners.
            if (entry.frame.handle) {
                cancelAnimationFrame(entry.frame.handle);

                entry.frame.handle = 0;
            }

            // A watch or a write scheduled by the last layout change would observe the boxes again after the
            // disconnect above, or claim the scroller again after its claims were released.
            if (entry.reobserve.handle) {
                cancelAnimationFrame(entry.reobserve.handle);

                entry.reobserve.handle = 0;
            }

            if (entry.remeasure.handle) {
                cancelAnimationFrame(entry.remeasure.handle);

                entry.remeasure.handle = 0;
            }

            Headers._entries.delete(id);
        }

        // Writes a property onto the box of a scroller on behalf of a header, keeping the value the box had
        // before the first header touched it. A header that measures what it measured last writes nothing: the
        // published height moves the layout, which reports every header on the box again, and two headers of
        // different heights rewriting their own values on each report would hand the box back and forth on
        // every frame. So the box carries the value of the header that changed size last.
        private static claim(box: HTMLElement, property: string, id: string, value: string) {
            let claims = Headers._claims.get(box);

            if (!claims) {
                claims = new Map();

                Headers._claims.set(box, claims);
            }

            let shared = claims.get(property);

            if (!shared) {
                shared = {
                    previous: box.style.getPropertyValue(property),
                    priority: box.style.getPropertyPriority(property),
                    owners: new Map()
                };

                claims.set(property, shared);
            }

            if (shared.owners.get(id) === value) return;

            // Re-inserted rather than updated, so the order of the map stays the order the headers last changed in.
            shared.owners.delete(id);
            shared.owners.set(id, value);

            box.style.setProperty(property, value);
        }

        // Gives up the claim of a header on a property of a box. The header leaving is not necessarily the one
        // that wrote the value that is on the box right now, and the ones still holding it may never change size
        // again to write theirs back, so the box is handed the value of the one that changed last of them. The
        // value found before any of them is only put back once nothing is holding the property anymore.
        private static release(box: HTMLElement, property: string, id: string) {
            const claims = Headers._claims.get(box);
            const shared = claims?.get(property);

            if (!claims || !shared || !shared.owners.has(id)) return;

            shared.owners.delete(id);

            if (shared.owners.size > 0) {
                let latest = '';

                for (const value of shared.owners.values()) {
                    latest = value;
                }

                box.style.setProperty(property, latest);

                return;
            }

            // An empty value removes the property, which is what the box had when nothing was set on it.
            box.style.setProperty(property, shared.previous, shared.priority);

            claims.delete(property);

            if (claims.size === 0) {
                Headers._claims.delete(box);
            }
        }

        // The height of the border box of an element in CSS pixels, untransformed and with its fractions: offsetHeight
        // rounds to a whole pixel, and a header sized by rem paddings, wrapped lines or a fractional safe area inset is
        // often not one, which would leave a hairline seam (or overlap) between the header and what is offset by it;
        // getBoundingClientRect is scaled by every transform of an ancestor (a zoomed preview, a scaled frame), while
        // what is offset by it is laid out in the same untransformed pixels the header is. The computed height is
        // neither, and is a border box once its paddings and borders are added where box-sizing leaves them out.
        private static borderBoxHeight(element: HTMLElement): number {
            const style = getComputedStyle(element);

            let height = parseFloat(style.height);

            // A box with no used height of its own (not rendered, display: contents) computes to "auto".
            if (!isFinite(height)) return element.offsetHeight;

            if (style.boxSizing !== 'border-box') {
                height += (parseFloat(style.paddingTop) || 0) + (parseFloat(style.paddingBottom) || 0) +
                    (parseFloat(style.borderTopWidth) || 0) + (parseFloat(style.borderBottomWidth) || 0);
            }

            return height;
        }

        private static scrollParent(element: HTMLElement): HTMLElement | Window {
            let node = element.parentElement;

            while (node && node !== document.body && node !== document.documentElement) {
                const overflowY = getComputedStyle(node).overflowY;

                // A scrollable overflow that has nothing to scroll is not the scroller of the header: it
                // never fires a scroll event and it reads as sitting at its top forever, which would pin
                // the header revealed and flat. Walking past it lands on the box that really scrolls.
                if ((overflowY === 'auto' || overflowY === 'scroll' || overflowY === 'overlay') &&
                    node.scrollHeight > node.clientHeight) return node;

                node = node.parentElement;
            }

            return window;
        }

        private static scrollTop(target: HTMLElement | Window): number {
            if (target === window) return window.scrollY || document.documentElement.scrollTop || 0;

            return (target as HTMLElement).scrollTop;
        }
    }
}

// bit-lazy: teardown Headers.dispose
