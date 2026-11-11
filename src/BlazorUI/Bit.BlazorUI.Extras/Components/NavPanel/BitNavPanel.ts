namespace BitBlazorUI {
    export class NavPanel {
        // Kept in step with the defaults BitMediaQuery falls back to (and media-queries.scss publishes).
        private static _defaultBreakpoints: Record<string, string> = { sm: '600px', md: '960px', lg: '1280px', xl: '1920px' };

        /**
         * Whether the screen is one the panel is a drawer on, built the way the media query the panel renders builds
         * its LtSm / LtMd / LtLg / LtXl: below the breakpoint of an enclosing BitThemeProvider, else the --bit-bp-*
         * token the document resolves, else the default. Called synchronously before the panel first renders, so it
         * answers false whenever it cannot tell, which leaves the content rendered.
         */
        public static isDrawer(breakpoint: string, themedBreakpoint: string | null): boolean {
            try {
                if (typeof window.matchMedia !== 'function') return false;

                const element = document.body ?? document.documentElement;
                const value = themedBreakpoint?.trim()
                    || getComputedStyle(element).getPropertyValue(`--bit-bp-${breakpoint}`).trim()
                    || NavPanel._defaultBreakpoints[breakpoint];
                if (!value) return false;

                // The exclusive upper bound BitMediaQuery writes: a hundredth or two below the breakpoint.
                const match = /^(-?\d*\.?\d+)(px)?$/i.exec(value);
                const below = match
                    ? `${Math.round((parseFloat(match[1]) - 0.02) * 100) / 100}px`
                    : `calc(${value} - 0.02px)`;

                return window.matchMedia(`screen and (max-width: ${below})`).matches;
            } catch {
                return false;
            }
        }
    }
}
