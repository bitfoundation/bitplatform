namespace BitBlazorUI {
    // The name constants below must stay aligned with BitAccentColorNames.cs and the inline script
    // in BitAccentColorSsr.cs - the three read and write the same attribute and stores.
    const ATTRIBUTE = 'bit-accent';
    const STORAGE_KEY = 'bit-accent-color';
    const COOKIE_NAME = 'bit-accent-color';
    const CSS_STORAGE_KEY = 'bit-accent-css';
    const STYLE_ELEMENT_ID = 'bit-accent-css';
    // ~400 days, the upper bound modern browsers clamp persistent cookies to. Matches what the
    // core library writes for its own theme-preference cookie.
    const COOKIE_MAX_AGE_SECONDS = 34560000;

    // The BitAccentColorPersistence flags, as the C# enum serializes them.
    const PERSIST_LOCAL_STORAGE = 1;
    const PERSIST_COOKIE = 2;

    export class AccentColor {
        /**
         * The persisted accent token, read from the stores the persistence flags enable -
         * localStorage first and the cookie second: when both are written they only diverge when
         * one of them is unavailable (localStorage throws in blocked-storage setups, and a visitor
         * can clear cookies alone), so either one on its own restores the accent.
         */
        public static getPersisted(persistence: number): string | null {
            if (persistence & PERSIST_LOCAL_STORAGE) {
                try {
                    const stored = localStorage.getItem(STORAGE_KEY);
                    if (stored) return stored;
                } catch { }
            }

            return (persistence & PERSIST_COOKIE) ? AccentColor.readCookie() : null;
        }

        /**
         * Applies and persists an accent token. Each store the persistence flags enable is
         * (re)written - which also self-heals a divergence left by an unavailable store on an
         * earlier visit - and each disabled one is removed, so changing the persistence
         * configuration leaves no stale copy behind. With setAttribute (the StaticCss and StoredCss
         * strategies) the bit-accent attribute is set for the StaticCss stylesheet to key on;
         * without it (the None strategy) the attribute is removed instead. A css payload (the
         * StoredCss strategy) refreshes the localStorage snapshot (persistence permitting) and the
         * in-document style element; no payload (the other strategies) drops both - so switching
         * strategies leaves no stale attribute or snapshot behind either.
         */
        public static apply(token: string, css: string | null, version: string | null, setAttribute: boolean, persistence: number) {
            if (!token) return;

            if (persistence & PERSIST_LOCAL_STORAGE) {
                try { localStorage.setItem(STORAGE_KEY, token); } catch { }
            } else {
                try { localStorage.removeItem(STORAGE_KEY); } catch { }
            }

            if (persistence & PERSIST_COOKIE) {
                AccentColor.writeCookie(token);
            } else {
                AccentColor.deleteCookie();
            }

            if (setAttribute) {
                document.documentElement.setAttribute(ATTRIBUTE, token);
            } else {
                document.documentElement.removeAttribute(ATTRIBUTE);
            }

            if (css) {
                // The snapshot is a localStorage entry, so it honors the same flag as the token;
                // the in-document style element is not persistence, it just keeps this page's
                // pre-paint style current.
                if (persistence & PERSIST_LOCAL_STORAGE) {
                    try { localStorage.setItem(CSS_STORAGE_KEY, JSON.stringify({ v: version || '', a: token, css: css })); } catch { }
                } else {
                    try { localStorage.removeItem(CSS_STORAGE_KEY); } catch { }
                }
                AccentColor.upsertStyleElement(css);
            } else {
                try { localStorage.removeItem(CSS_STORAGE_KEY); } catch { }
                AccentColor.removeStyleElements();
            }
        }

        /**
         * Reverts to the packaged palette: removes the attribute, both stores, the snapshot and any
         * accent style element. With nothing persisted, the next load simply paints the default.
         */
        public static clear() {
            document.documentElement.removeAttribute(ATTRIBUTE);

            try { localStorage.removeItem(STORAGE_KEY); } catch { }
            try { localStorage.removeItem(CSS_STORAGE_KEY); } catch { }

            AccentColor.deleteCookie();

            AccentColor.removeStyleElements();
        }

        private static writeCookie(value: string) {
            // Cookies are how the preference reaches the server, which needs it to prerender the page
            // the way the visitor left it - localStorage is unreachable from there. Lax + the 400-day
            // cap browsers clamp to, mirroring the core library's theme-preference cookie.
            try {
                const secure = location.protocol === 'https:' ? '; Secure' : '';
                document.cookie = `${COOKIE_NAME}=${encodeURIComponent(value)}; path=/; max-age=${COOKIE_MAX_AGE_SECONDS}; SameSite=Lax${secure}`;
            } catch { }
        }

        private static deleteCookie() {
            try {
                const secure = location.protocol === 'https:' ? '; Secure' : '';
                document.cookie = `${COOKIE_NAME}=; path=/; max-age=0; SameSite=Lax${secure}`;
            } catch { }
        }

        private static readCookie(): string | null {
            try {
                const prefix = `${COOKIE_NAME}=`;
                const match = document.cookie.split(';').map(c => c.trim()).find(c => c.startsWith(prefix));
                if (!match) return null;

                try {
                    return decodeURIComponent(match.substring(prefix.length));
                } catch {
                    return match.substring(prefix.length);
                }
            } catch {
                return null;
            }
        }

        private static upsertStyleElement(css: string) {
            // A single element owns the pre-paint palette, whether it was emitted by the server,
            // injected by the inline head script, or created here - so drop every existing one
            // (duplicates are possible when a server-emitted style and a script-injected snapshot
            // met in one document) and append one fresh element. textContent keeps the payload
            // inert - it is never parsed as markup.
            // The element being replaced carries the CSP nonce the host page stamped on it (see
            // BitAccentColorHead.Nonce), so carry it over - dropping it would get the replacement
            // blocked under a style-src 'nonce-...' policy on the first accent pick. Browsers hide
            // the nonce content attribute, hence the IDL property first.
            const existing = document.getElementById(STYLE_ELEMENT_ID);
            const nonce = (existing && ((existing as HTMLElement).nonce || existing.getAttribute('nonce'))) || '';

            AccentColor.removeStyleElements();

            const element = document.createElement('style');
            element.id = STYLE_ELEMENT_ID;
            if (nonce) element.setAttribute('nonce', nonce);
            element.textContent = css;
            document.head.appendChild(element);
        }

        private static removeStyleElements() {
            document.querySelectorAll(`style[id="${STYLE_ELEMENT_ID}"]`).forEach(element => element.remove());
        }
    }
}
