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

        /**
         * @param query        A custom, verbatim media query (takes precedence when provided).
         * @param screenQuery  One of the predefined BitScreenQuery names (e.g. "Md", "LtLg", "GtSm").
         *                     When set (and no custom query), the query is built from the live
         *                     --bit-bp-* theme breakpoints so a customized theme is honored.
         */
        public static async setup(id: string, query: string | null, screenQuery: string | null, dotnetObj: DotNetObject) {
            if (!dotnetObj) return;

            const resolvedQuery = query || (screenQuery ? MediaQuery.buildScreenQuery(screenQuery, id) : '');
            if (!resolvedQuery) return;

            // The C# side re-invokes setup for screen queries on every render (the expression
            // depends on the live --bit-bp-* breakpoints); keep the existing listener when the
            // resolved expression is unchanged and only rebuild it when it actually differs.
            if (MediaQuery._abortControllers[id] && MediaQuery._resolvedQueries[id] === resolvedQuery) return;

            MediaQuery.dispose(id);

            const ac = new AbortController();
            MediaQuery._abortControllers[id] = ac;
            MediaQuery._resolvedQueries[id] = resolvedQuery;

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
                    MediaQuery.dispose(id);
                }
            }
        }

        public static dispose(id: string) {
            delete MediaQuery._resolvedQueries[id];

            const ac = MediaQuery._abortControllers[id];
            if (!ac) return;

            ac.abort();

            delete MediaQuery._abortControllers[id];
        }

        // Builds the media query for a predefined BitScreenQuery from the resolved theme breakpoints.
        // Range bounds are half-open (min inclusive, max exclusive), so the upper edge is one CSS
        // pixel below the next breakpoint - matching the packaged media-queries.scss mixins, whose
        // "screen and" media-type prefix is kept too so the query does not also match print.
        private static buildScreenQuery(screenQuery: string, id: string): string {
            const bp = MediaQuery.resolveBreakpoints(id);
            const min = (v: string) => `(min-width: ${v})`;
            const max = (v: string) => `(max-width: ${MediaQuery.below(v)})`;

            const build = () => {
                switch (screenQuery) {
                    case 'Xs': return `${min(bp.xs)} and ${max(bp.sm)}`;
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

        // Reads the --bit-bp-* breakpoint tokens from the queried element's themed scope, so the
        // breakpoints of an enclosing BitThemeProvider are honored. Custom properties inherit, so a
        // document-root definition still resolves through the element; the root itself is only read
        // directly when the element is not rendered (e.g. an OnChange-only usage with no content).
        // Built-in defaults fill in for any token that is unset everywhere.
        private static resolveBreakpoints(id: string): Record<BreakpointKey, string> {
            const element = document.getElementById(id) ?? document.documentElement;
            const styles = typeof getComputedStyle === 'function'
                ? getComputedStyle(element)
                : null;

            const read = (key: BreakpointKey) => {
                const value = styles?.getPropertyValue(`--bit-bp-${key}`)?.trim();
                return value || MediaQuery._defaultBreakpoints[key];
            };

            return { xs: read('xs'), sm: read('sm'), md: read('md'), lg: read('lg'), xl: read('xl'), xxl: read('xxl') };
        }

        // Returns the value one CSS pixel below `value`, for exclusive max-width bounds. A unitless
        // or px value is decremented numerically ("960px" -> "959px"); any other unit (em/rem/…) is
        // deferred to the browser via calc() so custom-unit breakpoints still work.
        private static below(value: string): string {
            const trimmed = value.trim();
            const match = /^(-?\d*\.?\d+)px$/i.exec(trimmed) ?? /^(-?\d*\.?\d+)$/.exec(trimmed);
            if (match) {
                return `${parseFloat(match[1]) - 1}px`;
            }

            return `calc(${trimmed} - 1px)`;
        }
    }
}
