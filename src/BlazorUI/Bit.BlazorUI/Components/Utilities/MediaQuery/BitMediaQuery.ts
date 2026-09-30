namespace BitBlazorUI {
    type BreakpointKey = 'xs' | 'sm' | 'md' | 'lg' | 'xl' | 'xxl';

    type MediaQueryListener = {
        ac: AbortController;
        // The resolved media-query expression the listener was created with, so a repeated setup call
        // can reuse the existing listener when the expression is unchanged and replace it when the
        // theme breakpoints have changed the resolved query for the same ScreenQuery name.
        resolvedQuery: string;
        elementId: string | null;
        query: string | null;
        screenQuery: string | null;
        breakpoints: { [key: string]: string } | null;
        dotnetObj: DotNetObject;
        // The state last reported to .NET, carried over when the listener is rebuilt for a new
        // expression, so a rebuild that leaves the answer as it was does not report it again.
        matches?: boolean;
    };

    export class MediaQuery {
        private static _listeners: { [key: string]: MediaQueryListener } = {};

        // Watches what can re-value the --bit-bp-* breakpoints of the whole document without any
        // component rendering again: BitThemeManager applying a theme (an inline style on the body or
        // the root), and a switch between named themes (the bit-theme attribute, and the event the
        // theme runtime dispatches). Only alive while a ScreenQuery listener exists to re-resolve.
        private static _themeObserver: MutationObserver | null = null;
        private static _themeFrame = 0;

        // Fallback breakpoints (px), used only when the corresponding --bit-bp-* CSS variable is
        // not resolvable. Kept in sync with the defaults published by media-queries.scss.
        private static _defaultBreakpoints: Record<BreakpointKey, string> = {
            xs: '0',
            sm: '600px',
            md: '960px',
            lg: '1280px',
            xl: '1920px',
            xxl: '2560px',
        };

        // The distance the exclusive upper bound of a range is kept below the next breakpoint. A
        // whole pixel would leave a gap no side of the scale matches: a viewport is not always a
        // whole number of CSS pixels (a zoomed page, a fractional device pixel ratio, a scrollbar
        // taking a fraction of the width), and a width of 959.5px is neither "max-width: 959px" nor
        // "min-width: 960px". A hundredth of a pixel closes that gap while still keeping the two
        // sides from ever matching at once; two hundredths rather than one because Safari rounds a
        // fractional media-query bound and one is not always enough to stay below the edge.
        private static _rangeEpsilon = 0.02;

        // The elements the focus can be handed to when the content it was on is swapped out.
        private static _focusables = 'a[href], area[href], button, input, select, textarea, iframe, summary, audio[controls], video[controls], [contenteditable]:not([contenteditable="false"]), [tabindex]';

        /**
         * @param key          The listener key. The component's own unique id rather than the id of
         *                     an element, so two components sharing an explicit Id cannot collide.
         * @param elementId    The id of the element whose themed scope the --bit-bp-* breakpoints
         *                     are read from, or null when the component renders no element of its
         *                     own (the body is read instead).
         * @param query        A custom, verbatim media query (takes precedence when provided).
         * @param screenQuery  One of the predefined BitScreenQuery names (e.g. "Md", "LtLg", "GtSm").
         *                     When set (and no custom query), the query is built from the live
         *                     theme breakpoints so a customized theme is honored.
         * @param breakpoints  The breakpoints of an enclosing BitThemeProvider, resolved on the .NET
         *                     side from the cascading theme. They win over the CSS variables, which
         *                     is what makes a scoped theme reachable with no element to read from.
         */
        public static async setup(key: string,
                                  elementId: string | null,
                                  query: string | null,
                                  screenQuery: string | null,
                                  breakpoints: { [key: string]: string } | null,
                                  dotnetObj: DotNetObject) {
            if (!dotnetObj) return;

            // Everything below is the browser's own matchMedia; an environment without it (a
            // non-browser host, a stripped down webview) has no media state to report at all, so
            // the component simply keeps whatever DefaultMatched asked for.
            if (typeof window === 'undefined' || typeof window.matchMedia !== 'function') return;

            await MediaQuery.listen(key, elementId, query, screenQuery, breakpoints, dotnetObj);
        }

        public static dispose(key: string) {
            const listener = MediaQuery._listeners[key];
            if (!listener) return;

            listener.ac.abort();

            delete MediaQuery._listeners[key];

            MediaQuery.syncThemeObserver();
        }

        private static async listen(key: string,
                                    elementId: string | null,
                                    query: string | null,
                                    screenQuery: string | null,
                                    breakpoints: { [key: string]: string } | null,
                                    dotnetObj: DotNetObject) {
            const resolvedQuery = query || (screenQuery ? MediaQuery.buildScreenQuery(screenQuery, elementId, breakpoints) : '');
            if (!resolvedQuery) {
                // Nothing resolves to listen for any more, so a listener a previous call left behind
                // would keep reporting a query this component is no longer asking about.
                MediaQuery.dispose(key);
                return;
            }

            // The .NET side re-invokes setup for screen queries on every render (the expression
            // depends on the live breakpoints); keep the existing listener when the resolved
            // expression is unchanged and only rebuild it when it actually differs. What the
            // listener re-resolves with later is refreshed either way: the id or the scoped
            // breakpoints can change without changing the expression they resolve to today.
            const existing = MediaQuery._listeners[key];
            if (existing && existing.resolvedQuery === resolvedQuery) {
                existing.elementId = elementId;
                existing.query = query;
                existing.screenQuery = screenQuery;
                existing.breakpoints = breakpoints;
                existing.dotnetObj = dotnetObj;
                MediaQuery.syncThemeObserver();
                return;
            }

            MediaQuery.dispose(key);

            const ac = new AbortController();
            const listener: MediaQueryListener = { ac, resolvedQuery, elementId, query, screenQuery, breakpoints, dotnetObj, matches: existing?.matches };
            MediaQuery._listeners[key] = listener;

            MediaQuery.syncThemeObserver();

            const queryList = window.matchMedia(resolvedQuery);

            // matchMedia never throws; a query it cannot parse silently becomes "not all", which
            // simply never matches. Surface that as a warning so a typo in a custom query is
            // diagnosable instead of just rendering the NotMatched content forever.
            if (queryList.media === 'not all' && resolvedQuery.trim() !== 'not all') {
                console.warn(`BitMediaQuery: the provided query '${resolvedQuery}' is not a valid media query.`);
            }

            queryList.addEventListener('change', async e => {
                await handleMatchChange(e.matches);
            }, { signal: ac.signal });

            await handleMatchChange(queryList.matches);

            async function handleMatchChange(matches: boolean) {
                if (listener.matches === matches) return;
                listener.matches = matches;

                // A flip swaps the content the component renders, and the element the focus is on
                // can go with it - most often while the page is zoomed, which is what crosses a
                // breakpoint for a keyboard user. The focus is watched across the swap, so it lands
                // in the content that replaced it rather than falling back to the top of the page.
                const stopKeepingFocus = MediaQuery.keepFocus(listener.elementId);

                try {
                    await listener.dotnetObj.invokeMethodAsync("OnMatchChange", matches);
                } catch {
                    stopKeepingFocus();

                    // The .NET side is gone (the component or its circuit was disposed while the
                    // notification was in flight); stop listening instead of failing on every change.
                    // Only this listener though: a notification still in flight from the call before
                    // a rebuild would otherwise take the listener that replaced it down with it.
                    if (MediaQuery._listeners[key] === listener) {
                        MediaQuery.dispose(key);
                    }
                }
            }
        }

        // Starts or stops watching the document for re-valued breakpoints, by whether any listener
        // is built from them; a custom query is verbatim and has nothing to re-resolve.
        private static syncThemeObserver() {
            const needed = Object.keys(MediaQuery._listeners).some(k => !!MediaQuery._listeners[k].screenQuery);

            if (needed && !MediaQuery._themeObserver) {
                if (typeof MutationObserver !== 'function' || typeof document === 'undefined') return;

                const observer = new MutationObserver(MediaQuery.scheduleThemeRefresh);
                const options = { attributes: true, attributeFilter: ['style', 'class', 'bit-theme'] };
                observer.observe(document.documentElement, options);
                if (document.body) {
                    observer.observe(document.body, options);
                }
                document.addEventListener('bit-theme-change', MediaQuery.scheduleThemeRefresh);

                MediaQuery._themeObserver = observer;
            } else if (!needed && MediaQuery._themeObserver) {
                MediaQuery._themeObserver.disconnect();
                MediaQuery._themeObserver = null;
                document.removeEventListener('bit-theme-change', MediaQuery.scheduleThemeRefresh);
            }
        }

        // Coalesces a burst of mutations (a theme applies dozens of properties one by one) into one
        // pass, taken once the style has settled.
        private static scheduleThemeRefresh() {
            if (MediaQuery._themeFrame) return;

            MediaQuery._themeFrame = requestAnimationFrame(() => {
                MediaQuery._themeFrame = 0;

                Object.keys(MediaQuery._listeners).forEach(key => {
                    const l = MediaQuery._listeners[key];
                    if (!l || !l.screenQuery) return;

                    // Unchanged expressions keep their listener: listen() only rebuilds (and reports)
                    // the ones the new breakpoints actually moved.
                    MediaQuery.listen(key, l.elementId, l.query, l.screenQuery, l.breakpoints, l.dotnetObj);
                });
            });
        }

        // Watches the updates of the component's element for the one that removes the focused
        // element, and hands the focus to the content that replaced it: an element with the same
        // id when there is one (the same control, rendered for the other side of the query), else
        // the first focusable one, else the element itself. Other updates can come first (the
        // content re-rendering on its own while the notification is on its way), so it is not the
        // first update that ends the watch but the focus leaving the element, or a timeout.
        // Returns what stops watching.
        private static keepFocus(elementId: string | null): () => void {
            const none = () => { };

            if (!elementId || typeof MutationObserver !== 'function') return none;

            const element = document.getElementById(elementId);
            const focused = document.activeElement as HTMLElement | null;
            if (!element || !focused || focused === element || !element.contains(focused)) return none;

            const focusedId = focused.id;

            let timer = 0;
            const observer = new MutationObserver(() => {
                // Still in place: either the update this waits for has not come yet, or it is one
                // that keeps the focused element (a Template, whose content is updated rather than
                // replaced). Only once the focus has moved on is there nothing left to keep.
                if (focused.isConnected) {
                    if (document.activeElement !== focused) stop();
                    return;
                }

                stop();

                // Something else already took the focus on (the new content focusing itself).
                const active = document.activeElement;
                if (active && active !== document.body && active !== document.documentElement) return;
                if (!element.isConnected) return;

                const candidates = Array.from(element.querySelectorAll<HTMLElement>(MediaQuery._focusables)).filter(MediaQuery.isFocusable);
                const target = (focusedId && candidates.find(c => c.id === focusedId)) || candidates[0];

                if (target) {
                    target.focus({ preventScroll: true });
                    return;
                }

                // Nothing inside can take the focus; the element holds it for the time being, so
                // the next Tab carries on from where the content was rather than from the top.
                if (element.hasAttribute('tabindex')) {
                    element.focus({ preventScroll: true });
                    return;
                }

                element.setAttribute('tabindex', '-1');
                element.addEventListener('blur', () => {
                    if (element.getAttribute('tabindex') === '-1') element.removeAttribute('tabindex');
                }, { once: true });
                element.focus({ preventScroll: true });
            });

            // The update this waits for is the render the notification causes; one that never comes
            // (an IsMatched frozen by a one-way binding, a state that did not change) is not waited on.
            function stop() {
                observer.disconnect();
                clearTimeout(timer);
            }

            observer.observe(element, { childList: true, subtree: true });
            timer = setTimeout(stop, 5000) as unknown as number;

            return stop;
        }

        private static isFocusable(el: HTMLElement): boolean {
            if (el.tabIndex < 0 || (el as HTMLButtonElement).disabled) return false;
            if (el.closest('[inert]')) return false;

            return el.getClientRects().length > 0;
        }

        // Builds the media query for a predefined BitScreenQuery from the resolved theme breakpoints.
        // Range bounds are half-open (min inclusive, max exclusive), so the upper edge sits just
        // below the next breakpoint - the shape of the packaged media-queries.scss mixins, whose
        // "screen and" media-type prefix is kept too so the query does not also match print. Only the
        // distance to that edge differs: a stylesheet compiled ahead of time is written in whole
        // pixels, while the bound built here is the finer one below, which no width can fall into.
        private static buildScreenQuery(screenQuery: string, elementId: string | null, breakpoints: { [key: string]: string } | null): string {
            const bp = MediaQuery.resolveBreakpoints(elementId, breakpoints);
            const min = (v: string) => `(min-width: ${v})`;
            const max = (v: string) => `(max-width: ${MediaQuery.below(v)})`;
            // A range that starts at the bottom of the scale needs no lower bound: every width is
            // at or above zero, so the bound would only be noise in the query the browser reports.
            const from = (v: string) => MediaQuery.isZero(v) ? '' : `${min(v)} and `;

            const build = () => {
                switch (screenQuery) {
                    case 'Xs': return `${from(bp.xs)}${max(bp.sm)}`;
                    case 'Sm': return `${min(bp.sm)} and ${max(bp.md)}`;
                    case 'Md': return `${min(bp.md)} and ${max(bp.lg)}`;
                    case 'Lg': return `${min(bp.lg)} and ${max(bp.xl)}`;
                    case 'Xl': return `${min(bp.xl)} and ${max(bp.xxl)}`;
                    case 'Xxl': return min(bp.xxl);

                    case 'LtSm': return max(bp.sm);
                    case 'LtMd': return max(bp.md);
                    case 'LtLg': return max(bp.lg);
                    case 'LtXl': return max(bp.xl);
                    case 'LtXxl': return max(bp.xxl);

                    case 'GtXs': return min(bp.sm);
                    case 'GtSm': return min(bp.md);
                    case 'GtMd': return min(bp.lg);
                    case 'GtLg': return min(bp.xl);
                    case 'GtXl': return min(bp.xxl);

                    case 'SmToMd': return `${min(bp.sm)} and ${max(bp.lg)}`;
                    case 'SmToLg': return `${min(bp.sm)} and ${max(bp.xl)}`;
                    case 'SmToXl': return `${min(bp.sm)} and ${max(bp.xxl)}`;
                    case 'MdToLg': return `${min(bp.md)} and ${max(bp.xl)}`;
                    case 'MdToXl': return `${min(bp.md)} and ${max(bp.xxl)}`;
                    case 'LgToXl': return `${min(bp.lg)} and ${max(bp.xxl)}`;

                    default: return '';
                }
            };

            const query = build();
            return query ? `screen and ${query}` : '';
        }

        // Resolves the breakpoints of the scale the query is built on, most specific first: the
        // breakpoints of an enclosing BitThemeProvider, which the .NET side reads off the cascading
        // theme and which is the only source that stays reachable when the component renders no
        // element of its own; then the --bit-bp-* tokens of the queried element's themed scope,
        // which is how a theme applied to the document (or to any ancestor) is picked up - custom
        // properties inherit, so a document-root definition still resolves through the element;
        // then the built-in defaults, for a token that is set nowhere. The body is what is read when
        // there is no element - in no-wrapper mode, and when nothing is rendered at all (an
        // OnChange-only usage with no content) - rather than the root: it inherits everything the
        // root declares, and it is where BitThemeManager applies a theme unless told otherwise.
        private static resolveBreakpoints(elementId: string | null, breakpoints: { [key: string]: string } | null): Record<BreakpointKey, string> {
            const element = (elementId ? document.getElementById(elementId) : null) ?? document.body ?? document.documentElement;
            const styles = typeof getComputedStyle === 'function'
                ? getComputedStyle(element)
                : null;

            const read = (key: BreakpointKey) => {
                const themed = breakpoints?.[key]?.trim();
                if (themed) return themed;

                const value = styles?.getPropertyValue(`--bit-bp-${key}`)?.trim();
                return value || MediaQuery._defaultBreakpoints[key];
            };

            return { xs: read('xs'), sm: read('sm'), md: read('md'), lg: read('lg'), xl: read('xl'), xxl: read('xxl') };
        }

        // Returns the value just below `value`, for exclusive max-width bounds. A unitless or px
        // value is decremented numerically ("960px" -> "959.98px"); any other unit (em/rem/…) is
        // deferred to the browser via calc() so custom-unit breakpoints still work.
        private static below(value: string): string {
            const trimmed = value.trim();
            const match = /^(-?\d*\.?\d+)px$/i.exec(trimmed) ?? /^(-?\d*\.?\d+)$/.exec(trimmed);
            if (match) {
                // Rounded back to the hundredth the subtraction is written in, since binary
                // floating point turns 600 - 0.02 into 599.9800000000001 on its own.
                return `${Math.round((parseFloat(match[1]) - MediaQuery._rangeEpsilon) * 100) / 100}px`;
            }

            return `calc(${trimmed} - ${MediaQuery._rangeEpsilon}px)`;
        }

        // Whether a breakpoint sits at the bottom of the scale. Zero is the one length that may be
        // written without a unit, so every spelling of it - "0", "0px", "0.0rem" - is the same edge.
        private static isZero(value: string): boolean {
            const match = /^(-?\d*\.?\d+)(px|r?em|%|v[wh])?$/i.exec(value.trim());

            return match ? parseFloat(match[1]) === 0 : false;
        }
    }
}
