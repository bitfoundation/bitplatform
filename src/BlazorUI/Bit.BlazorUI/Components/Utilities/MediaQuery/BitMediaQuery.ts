namespace BitBlazorUI {
    type BreakpointKey = 'xs' | 'sm' | 'md' | 'lg' | 'xl' | 'xxl';

    export class MediaQuery {
        private static _abortControllers: { [key: string]: AbortController } = {};

        // The resolved media-query expression each listener was created with, so a repeated setup
        // call can reuse the existing listener when the expression is unchanged and replace it when
        // the theme breakpoints have changed the resolved query for the same ScreenQuery name.
        private static _resolvedQueries: { [key: string]: string } = {};

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

        /**
         * @param key          The listener key. The component's own unique id rather than the id of
         *                     an element, so two components sharing an explicit Id cannot collide.
         * @param elementId    The id of the element whose themed scope the --bit-bp-* breakpoints
         *                     are read from, or null when the component renders no element of its
         *                     own (the document root is read instead).
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

            const resolvedQuery = query || (screenQuery ? MediaQuery.buildScreenQuery(screenQuery, elementId, breakpoints) : '');
            if (!resolvedQuery) {
                // Nothing resolves to listen for any more, so a listener a previous call left behind
                // would keep reporting a query this component is no longer asking about.
                MediaQuery.dispose(key);
                return;
            }

            // The .NET side re-invokes setup for screen queries on every render (the expression
            // depends on the live breakpoints); keep the existing listener when the resolved
            // expression is unchanged and only rebuild it when it actually differs.
            if (MediaQuery._abortControllers[key] && MediaQuery._resolvedQueries[key] === resolvedQuery) return;

            MediaQuery.dispose(key);

            const ac = new AbortController();
            MediaQuery._abortControllers[key] = ac;
            MediaQuery._resolvedQueries[key] = resolvedQuery;

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
                try {
                    await dotnetObj.invokeMethodAsync("OnMatchChange", matches);
                } catch {
                    // The .NET side is gone (the component or its circuit was disposed while the
                    // notification was in flight); stop listening instead of failing on every change.
                    // Only this listener though: a notification still in flight from the call before
                    // a rebuild would otherwise take the listener that replaced it down with it.
                    if (MediaQuery._abortControllers[key] === ac) {
                        MediaQuery.dispose(key);
                    }
                }
            }
        }

        public static dispose(key: string) {
            delete MediaQuery._resolvedQueries[key];

            const ac = MediaQuery._abortControllers[key];
            if (!ac) return;

            ac.abort();

            delete MediaQuery._abortControllers[key];
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
        // then the built-in defaults, for a token that is set nowhere. The document root is what is
        // read when there is no element: in no-wrapper mode, and when nothing is rendered at all
        // (an OnChange-only usage with no content).
        private static resolveBreakpoints(elementId: string | null, breakpoints: { [key: string]: string } | null): Record<BreakpointKey, string> {
            const element = (elementId ? document.getElementById(elementId) : null) ?? document.documentElement;
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
