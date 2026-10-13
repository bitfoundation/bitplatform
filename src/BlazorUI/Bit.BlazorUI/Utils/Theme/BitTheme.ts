// Attribute / storage names - kept aligned with BitThemeAttributeNames.cs and BitThemeSsr.cs in
// C#. If you rename a constant here, mirror the change there (the contract test under
// Bit.BlazorUI.Tests.Utils.Theme will catch a mismatch).
namespace BitBlazorUI {
    const ATTR_THEME = 'bit-theme';
    const ATTR_THEME_DEFAULT = 'bit-theme-default';
    const ATTR_THEME_SYSTEM = 'bit-theme-system';
    const ATTR_THEME_PERSIST = 'bit-theme-persist';
    const ATTR_THEME_PERSIST_COOKIE = 'bit-theme-persist-cookie';
    const ATTR_THEME_DARK = 'bit-theme-dark';
    const ATTR_THEME_LIGHT = 'bit-theme-light';
    // Opt-in marker: animate theme swaps with the View Transitions API (see swapThemeAttribute).
    const ATTR_THEME_VIEW_TRANSITION = 'bit-theme-view-transition';
    // Opt-in marker: keep <meta name="theme-color"> equal to a palette color of the live page (see
    // ThemeColorMeta). Its value names the custom property to read; empty means the default below.
    const ATTR_THEME_COLOR_META = 'bit-theme-color-meta';
    // Written by applyTheme on its target: the family alias groups the applied theme re-values, for the
    // stylesheets to re-declare the active theme's own aliases of there (Styles/theme-overlay.scss).
    const ATTR_THEME_OVERLAY = 'bit-theme-overlay';
    // The groups of family aliases ATTR_THEME_OVERLAY names, each with the custom properties it is built
    // from - kept aligned with BitThemeMapper.FamilyAliasOverlayGroups in C#, which a BitThemeProvider
    // renders the attribute from (a contract test pins the two to each other).
    const FAMILY_ALIAS_OVERLAY_GROUPS: [string, string[]][] = [
        ['radius', ['--bit-shp-brd-radius']],
        ['radius-control', ['--bit-shp-radius-control', '--bit-shp-radius-full']],
        ['shadow', ['--bit-shd-cal', '--bit-clr-brd-ter', '--bit-shp-brd-width', '--bit-clr-fg-pri']],
        ['tooltip', ['--bit-clr-bg-pri', '--bit-clr-bg-sec', '--bit-clr-fg-pri']],
        ['foreground', ['--bit-clr-fg-pri']],
        ['pri', ['--bit-clr-pri']],
        ['sec', ['--bit-clr-sec']],
        ['ter', ['--bit-clr-ter']],
        ['inf', ['--bit-clr-inf']],
        ['suc', ['--bit-clr-suc']],
        ['wrn', ['--bit-clr-wrn']],
        ['swr', ['--bit-clr-swr']],
        ['err', ['--bit-clr-err']],
        ['typography', ['--bit-tpg-fs-2xs', '--bit-tpg-fs-xs', '--bit-tpg-fs-sm', '--bit-tpg-fs-md', '--bit-tpg-fs-lg', '--bit-tpg-fs-xl', '--bit-tpg-fs-2xl', '--bit-tpg-fs-3xl', '--bit-tpg-fs-4xl',
                        '--bit-tpg-fw-light', '--bit-tpg-fw-regular', '--bit-tpg-fw-medium', '--bit-tpg-fw-semibold', '--bit-tpg-fw-bold']],
        ['motion', ['--bit-mot-duration-full', '--bit-mot-duration-short-full', '--bit-mot-duration-long-full', '--bit-mot-duration-factor-full',
            '--bit-mot-duration-spinner-full', '--bit-mot-easing-spinner-full', '--bit-mot-loop-factor-full']],
    ];
    // The page's own background - what a status bar sits above in nearly every app.
    const THEME_COLOR_VARIABLE = '--bit-clr-bg-pri';
    const STORAGE_KEY = 'bit-current-theme';
    // Kept aligned with BitThemeCookie.PreferenceCookieName in C#. When cookie persistence is
    // enabled, the client mirrors the persisted preference into this cookie so the server can read
    // the user's current choice (via BitThemeSsr.BuildRootThemeAttributes) and paint the matching
    // theme on first server render - keeping the client (localStorage) and server (cookie) stores
    // in sync instead of drifting apart.
    const COOKIE_NAME = 'bit-theme-preference';
    // ~400 days, the upper bound modern browsers clamp persistent cookies to.
    const COOKIE_MAX_AGE_SECONDS = 34560000;
    // Kept aligned with BitThemeAttributeNames.ThemePairStorageKey and BitThemeCookie.PairCookieName in C#.
    // What the stored preference resolves against, kept beside it and mirrored into the cookie the same way,
    // so the next visit - and the server - restore it: "<light> <dark>", the pair a visitor picked at runtime
    // (setPair); or "light" / "dark", when the preference is that theme of the page's own pair, so a later
    // change of that pair (the app switching presets) moves the preference with it. Absent otherwise, and a
    // preference stored without it - a theme the app pinned outside the pair, or one stored before this
    // key existed - is restored as it is.
    const PAIR_STORAGE_KEY = 'bit-theme-pair';
    const PAIR_COOKIE_NAME = 'bit-theme-pair';

    type onThemeChangeType = (newThemeName: string, oldThemeName: string) => void;

    export interface ThemeOptions {
        system?: boolean;
        persist?: boolean;
        /** Mirror the persisted preference into the COOKIE_NAME cookie so SSR stays in sync with client choices. */
        persistCookie?: boolean;
        theme?: string | null;
        default?: string | null;
        darkTheme?: string | null;
        lightTheme?: string | null;
        onChange?: onThemeChangeType;
        /**
         * Keep `<meta name="theme-color">` equal to a palette color of the live page: `true` (or the
         * empty string) for the default custom property, a `--bit-*` name to read another one,
         * `false` / `null` to turn it off again, omitted to leave it as it is. Same feature as the
         * bit-theme-color-meta attribute (see ThemeColorMeta).
         */
        themeColorMeta?: boolean | string | null;
    }

    interface ThemeSetOptions {
        /** When true, startup init so resolved light/dark does not disable OS sync from bit-theme-system. */
        fromInit?: boolean;
        /** Reserved for internal OS refresh paths (same as normal set without touching follow-system flags). */
        internalOsRefresh?: boolean;
        /**
         * Mirrors what another tab stored: applied without writing it back, since the storage already holds it -
         * and holds anything that tab wrote after it too, which a write-back of this older value would clobber.
         */
        fromOtherTab?: boolean;
    }

    export class Theme {
        private static SYSTEM_THEME = 'system';
        private static THEME_ATTRIBUTE = ATTR_THEME;
        private static THEME_STORAGE_KEY = STORAGE_KEY;

        /** DOM event dispatched on `document` after every theme change, so non-CSS consumers can react. */
        public static readonly THEME_CHANGE_EVENT = 'bit-theme-change';

        private static _persist = false;
        private static _persistCookie = false;
        private static _darkTheme: string = 'dark';
        private static _lightTheme: string = 'light';
        // The pair the page declares (bit-theme-light / bit-theme-dark, or init's lightTheme / darkTheme),
        // the core light / dark when it declares none.
        private static _configuredDarkTheme: string = 'dark';
        private static _configuredLightTheme: string = 'light';
        // The pair setPair switched to, while it differs from the configured one; null otherwise.
        private static _runtimePair: [string, string] | null = null;
        private static _initOptions: ThemeOptions = {};
        private static _currentTheme = Theme._lightTheme;
        private static _onThemeChange: onThemeChangeType = () => { };

        /** When true, user pinned an explicit theme via set (not system); disables following OS until set('system'). */
        private static _stopFollowingSystem = false;

        /**
         * When true, the user explicitly opted into following the OS at runtime (e.g. via useSystem()),
         * so we follow OS changes even without a persisted "system" preference or the bit-theme-system attribute.
         * Cleared automatically when the user pins a concrete theme via set(...).
         */
        private static _runtimeFollowSystem = false;

        private static _schemeMediaQuery: MediaQueryList | null = null;
        private static _onSchemeChange = () => Theme.applyResolvedSystemThemeFromOs();

        private static _storageListenerAttached = false;
        // Mirror a persisted-theme change made in ANOTHER tab into this one, so all tabs of the same
        // app stay in sync. The `storage` event fires only in tabs OTHER than the one that wrote, so
        // re-applying here can never loop back with our own set(). Gated on _persist because only the
        // localStorage-backed store raises this event (cookie-only persistence does not).
        private static _onStorage = (e: StorageEvent) => {
            if (!Theme._persist) return;
            // The other tab switched the pair (setPair writes it before the preference, so this arrives
            // first), or storage was cleared (e.key null): adopt what is stored now without writing it
            // back - the configured pair when no pair is (a scheme marker, or nothing at all).
            if (e.key === PAIR_STORAGE_KEY || e.key === null) {
                const pair = Theme.readPersistedPair().pair;
                const light = pair ? pair[0] : Theme._configuredLightTheme;
                const dark = pair ? pair[1] : Theme._configuredDarkTheme;
                if (light !== Theme._lightTheme || dark !== Theme._darkTheme) {
                    Theme.applyPair(light, dark, false);
                }
                if (e.key !== null) return;
            }
            // e.key is null when storage was cleared; otherwise it must be our key.
            if (e.key !== null && e.key !== Theme.THEME_STORAGE_KEY) return;
            const next = e.newValue;
            if (!next) return;
            // Skip when the other tab's concrete choice is already applied here: avoids a redundant
            // set() (re-persist + re-dispatch) rippling across 3+ tabs. "system" is always processed
            // because the OS scheme may differ from the currently painted attribute.
            if (next !== Theme.SYSTEM_THEME &&
                Theme.getActualTheme(next) === (document.documentElement.getAttribute(ATTR_THEME) || '')) {
                return;
            }
            // Mirror the other tab's intent (including a switch to "system"); set() resolves and
            // re-cascades, without writing back what the other tab stored.
            Theme.set(next, { fromOtherTab: true });
        };

        private static _dotnetNotifier: DotNetObject | null = null;

        private static _appliedVarKeys = new WeakMap<HTMLElement, string[]>();

        // The targets applyTheme wrote ATTR_THEME_OVERLAY on: the value it wrote, and the one the target carried
        // before (a BitThemeProvider's, or the app's markup), which it merges its groups into and puts back.
        private static _appliedOverlays = new WeakMap<HTMLElement, { base: string | null, written: string | null }>();

        public static init(options: ThemeOptions) {
            Object.assign(Theme._initOptions, options);

            let deferPersist = false;
            let deferPersistCookie = false;

            if (Theme._initOptions.onChange) {
                Theme._onThemeChange = Theme._initOptions.onChange;
            }

            Theme._configuredDarkTheme = Theme._initOptions.darkTheme || 'dark';
            Theme._configuredLightTheme = Theme._initOptions.lightTheme || 'light';

            // Cookie mirroring is independent of localStorage persistence: an SSR app may want the
            // server to read the preference cookie without necessarily enabling localStorage.
            // Derive from the merged _initOptions on every call (not just when truthy) so a later
            // init({ persistCookie: false }) can clear the flag, while an init that omits the key
            // keeps the previously merged value.
            Theme._persistCookie = !!Theme._initOptions.persistCookie;

            // Derive on every call (see persistCookie note above) so a later init({ persist: false })
            // clears the flag, while an omitted key preserves the merged value.
            Theme._persist = !!Theme._initOptions.persist;

            // A pair the visitor picked (setPair) outranks the configured one: stored, it survives the
            // visit; in memory, a later init. Same precedence as the SSR inline script in BitThemeSsr.cs.
            const stored = (Theme._persist || Theme._persistCookie) ? Theme.readPersistedPair() : { pair: null, scheme: null };
            const pair = stored.pair || Theme._runtimePair;
            Theme._runtimePair = pair && !Theme.isConfiguredPair(pair[0], pair[1]) ? pair : null;
            // A stored pair the page has since come to declare itself is no pick of the visitor's any
            // more: it is dropped from storage too, so a later change of the page's pair reaches them
            // again. set() below stores the scheme marker in its place when the preference is of the pair.
            if (stored.pair && !Theme._runtimePair) {
                Theme.writePersistedPair(null);
            }
            Theme._lightTheme = Theme._runtimePair ? Theme._runtimePair[0] : Theme._configuredLightTheme;
            Theme._darkTheme = Theme._runtimePair ? Theme._runtimePair[1] : Theme._configuredDarkTheme;
            if (Theme._runtimePair) {
                Theme.writePairAttributes();
            }

            let theme = Theme._initOptions.theme || Theme._initOptions.default || Theme._lightTheme;

            // A `system` opt-in comes from the JS `system: true` option / bit-theme-system attribute,
            // OR from the base theme itself resolving to the "system" keyword (bit-theme="system" or
            // bit-theme-default="system"). Capture it once so first-paint resolution, the persist
            // deferral below and runtime OS-follow all treat these spellings identically.
            const systemRequested = Theme._initOptions.system || theme === Theme.SYSTEM_THEME;

            // Resolve the first-paint theme with the SAME precedence as the SSR inline script in
            // BitThemeSsr.cs (BuildInlineScriptBody): a `system` opt-in follows the OS and takes
            // precedence over an explicit bit-theme / bit-theme-default at first paint. A persisted
            // preference (handled below) still wins over this.
            //
            // Previously this only resolved the OS theme when there was NO explicit theme/default,
            // so a document with both bit-theme="..." and bit-theme-system painted the explicit
            // theme on hydration while the SSR script painted the OS-resolved one - a flash of the
            // wrong theme. Keeping the two code paths in lockstep avoids that.
            if (systemRequested) {
                theme = Theme.isSystemDark() ? Theme._darkTheme : Theme._lightTheme;
            } else if (Theme._runtimePair) {
                // The page's base names a theme of the pair it declares; the visitor's pair stands in for it.
                theme = Theme.fitToPair(theme);
            }

            // Restore a previously persisted preference. getPersisted() reads localStorage first and
            // falls back to the preference cookie, so this also covers a cookie-only setup
            // (persistCookie without persist) that a pure client-side render (no SSR inline script)
            // would otherwise ignore - previously this read was gated on persist alone.
            if (Theme._persist || Theme._persistCookie) {
                const persisted = Theme.getPersisted();
                if (persisted) {
                    // A preference stored as the light or dark theme of the page's pair follows that pair
                    // - the app may have switched presets since - so the visitor keeps their light or dark
                    // choice; set() below then stores what it resolved to. Anything else is restored as it
                    // is: a theme pinned outside the pair is a choice of its own, kept on purpose.
                    theme = persisted !== Theme.SYSTEM_THEME && stored.scheme
                        ? (stored.scheme === 'dark' ? Theme._darkTheme : Theme._lightTheme)
                        : persisted;
                    // An explicit persisted preset (anything other than "system") means the user pinned a theme;
                    // stop following the OS even when <html bit-theme-system> is present.
                    Theme._stopFollowingSystem = persisted !== Theme.SYSTEM_THEME;
                } else if (systemRequested) {
                    // System mode is enabled but no value has been persisted yet. Avoid writing the
                    // resolved light/dark theme to storage during the initial set() - otherwise the next
                    // init would treat that concrete value as an explicit user choice and stop following
                    // the OS. Disable BOTH stores for the initial set() and re-enable them afterwards so
                    // SYSTEM_THEME remains the effective persisted indicator until the user picks one.
                    deferPersist = Theme._persist;
                    deferPersistCookie = Theme._persistCookie;
                    Theme._persist = false;
                    Theme._persistCookie = false;
                }
            }

            // Any `system` opt-in (system: true, bit-theme="system" or bit-theme-default="system")
            // must on its own enable OS follow, otherwise setups without the bit-theme-system
            // attribute and without a persisted "system" value would resolve the OS theme once at
            // init but never get the prefers-color-scheme listener attached, because
            // shouldFollowSystem() only considers the HTML attribute and persisted value. Runtime
            // follow is still cleared when the user later pins a concrete theme via set(...).
            if (systemRequested && !Theme._stopFollowingSystem) {
                Theme._runtimeFollowSystem = true;
            }

            Theme.set(theme, { fromInit: true });

            if (deferPersist) {
                Theme._persist = true;
            }

            if (deferPersistCookie) {
                Theme._persistCookie = true;
            }

            // After the first set(), so the tag is read from the theme this init resolved rather than
            // the one being replaced. An omitted key leaves the feature as it is (the self-init below
            // may already have turned it on from the attribute); false / null turns it back off.
            // Anything else turns it on - including the empty string, which enable() reads as "the
            // default custom property", the same as the valueless bit-theme-color-meta attribute the
            // self-init below maps to true. One value, one meaning.
            const themeColorMeta = Theme._initOptions.themeColorMeta;
            if (themeColorMeta === false || themeColorMeta === null) {
                ThemeColorMeta.disable();
            } else if (themeColorMeta !== undefined) {
                ThemeColorMeta.enable(typeof themeColorMeta === 'string' ? themeColorMeta : null);
            }

            Theme.attachStorageSyncListener();
        }

        private static attachStorageSyncListener() {
            if (Theme._storageListenerAttached) return;
            if (typeof window === 'undefined' || !window.addEventListener) return;
            Theme._storageListenerAttached = true;
            window.addEventListener('storage', Theme._onStorage);
        }

        public static onChange(fn: onThemeChangeType) {
            Theme._onThemeChange = fn;
        }

        public static get() {
            // Report the theme that is actually applied: the bit-theme attribute is the source of
            // truth (it is what is painted, and set()/OS-follow keep _currentTheme in sync with it).
            // The persisted *preference* is a separate concept exposed via getPersisted(); the two
            // can legitimately diverge - e.g. runtime OS-follow updates the attribute without
            // rewriting storage - so get() must not substitute the persisted value here.
            Theme._currentTheme = document.documentElement.getAttribute(Theme.THEME_ATTRIBUTE) || '';

            return Theme._currentTheme;
        }

        public static set(themeName: string, options?: ThemeSetOptions) {
            // Reject null / undefined / empty inputs up-front so we never call setAttribute(...) with
            // a value that coerces to the literal string "null" or "undefined". The non-null
            // assertion below was unsafe because getActualTheme can return null for null input.
            if (!themeName) return Theme._currentTheme;

            const fromInit = options?.fromInit === true;
            const internalOs = options?.internalOsRefresh === true;

            if (!fromInit && !internalOs) {
                if (themeName === Theme.SYSTEM_THEME) {
                    Theme._stopFollowingSystem = false;
                    Theme._runtimeFollowSystem = true;
                } else {
                    Theme._stopFollowingSystem = true;
                    Theme._runtimeFollowSystem = false;
                }
            }

            const resolved = Theme.getActualTheme(themeName);
            if (!resolved) return Theme._currentTheme;
            Theme._currentTheme = resolved;

            if (options?.fromOtherTab !== true) {
                Theme.writePersisted(themeName);
            }

            const oldTheme = document.documentElement.getAttribute(Theme.THEME_ATTRIBUTE) || '';

            // Never animate the startup application (there is nothing meaningful to transition at
            // boot, and a transition would add a frame capture to every page load).
            Theme.swapThemeAttribute(Theme._currentTheme, /* skipTransition: */ fromInit);

            Theme.dispatchThemeChange(Theme._currentTheme, oldTheme);

            Theme.syncSystemThemeListener();

            return Theme._currentTheme;
        }

        public static toggleDarkLight() {
            // Re-sync from the bit-theme attribute first (the source of truth, same as get()) so a
            // toggle issued after an external attribute write - a non-bit script or dev tooling
            // setting bit-theme directly - acts on what is actually painted instead of a stale
            // _currentTheme. Read the attribute directly (not via get(), which would overwrite
            // _currentTheme before the fallback could use it) and fall back to _currentTheme when
            // the attribute is absent - which also bridges the one-frame window in which a pending
            // view transition has not applied the new attribute yet, since set() updates
            // _currentTheme synchronously.
            const current = document.documentElement.getAttribute(Theme.THEME_ATTRIBUTE) || Theme._currentTheme;

            // Toggle relative to the configured dark theme: when the dark theme is active switch to
            // light, otherwise switch to dark. Anchoring on the dark theme (rather than the light
            // one) means a configured pair such as bit-theme-light="fluent-light" /
            // bit-theme-dark="fluent-dark" toggles correctly in BOTH directions, and any other
            // current value (a custom or unrecognized theme) resolves to the dark theme instead of
            // silently collapsing to light.
            Theme._currentTheme = current === Theme._darkTheme
                ? Theme._lightTheme
                : Theme._darkTheme;

            Theme.set(Theme._currentTheme);

            return Theme._currentTheme;
        }

        /** Pins storage (when persist is on) to <c>system</c> and follows OS light/dark until an explicit preset is set. */
        public static useSystem() {
            return Theme.set(Theme.SYSTEM_THEME);
        }

        /**
         * [applied theme, active light, active dark, declared light, declared dark], in one read - for
         * BitThemeSwitcher (BitThemeManager.GetThemeStateAsync), which decides from all of them which pair
         * a design system stands for. An array, so .NET reads it back without a type of its own to trim.
         */
        public static getState() {
            return [Theme.get(), Theme._lightTheme, Theme._darkTheme, Theme._configuredLightTheme, Theme._configuredDarkTheme];
        }

        /**
         * Flips the scheme within the given pair, making it the active pair first when it is not (stored
         * as setPair stores it) - one swap and one change notification either way. A theme that is
         * neither of the two flips by its name's "dark" ending, so a dark one always turns light.
         * Blank names, or names containing whitespace, make it a no-op. Returns the applied theme.
         */
        public static toggleDarkLightWithin(lightTheme: string, darkTheme: string) {
            const pair = Theme.validPair(lightTheme, darkTheme);
            if (!pair) return Theme._currentTheme;

            const [light, dark] = pair;
            if (light !== Theme._lightTheme || dark !== Theme._darkTheme) {
                Theme.switchPair(light, dark);
                // Ahead of the preference set() stores, for the other tabs - as applyPair orders them.
                Theme.writePersistedPair(Theme.pairStoreValue(null));
            }

            const current = document.documentElement.getAttribute(Theme.THEME_ATTRIBUTE) || Theme._currentTheme;
            const isDark = current === dark || (current !== light && /dark$/.test(current));

            return Theme.set(isDark ? light : dark);
        }

        /**
         * Changes which themes "light" and "dark" mean - what toggleDarkLight flips between and what
         * following the OS resolves to - for an app that lets its users pick a design system. The
         * visitor's own choice is kept: while following the OS the new pair's theme for the OS scheme is
         * applied and "system" stays stored; otherwise the applied theme moves to its counterpart in the
         * new pair, and so does a stored one. The pair is persisted beside the preference (and mirrored
         * into its cookie) when persistence is on, and written to bit-theme-light / bit-theme-dark.
         * Blank names, or names containing whitespace, are ignored. Returns the applied theme.
         */
        public static setPair(lightTheme: string, darkTheme: string) {
            const pair = Theme.validPair(lightTheme, darkTheme);
            if (!pair) return Theme._currentTheme;

            Theme.applyPair(pair[0], pair[1], true);

            return Theme._currentTheme;
        }

        /** Both names trimmed, or null when either is blank or contains whitespace. */
        private static validPair(lightTheme: string, darkTheme: string): [string, string] | null {
            const light = (lightTheme || '').trim();
            const dark = (darkTheme || '').trim();
            return !light || !dark || /\s/.test(light) || /\s/.test(dark) ? null : [light, dark];
        }

        /** Makes a pair the active one - in memory and on the attributes - without applying a theme. */
        private static switchPair(light: string, dark: string) {
            Theme._lightTheme = light;
            Theme._darkTheme = dark;
            Theme._runtimePair = Theme.isConfiguredPair(light, dark) ? null : [light, dark];
            Theme.writePairAttributes();
        }

        private static applyPair(light: string, dark: string, persist: boolean) {
            const oldLight = Theme._lightTheme;
            const oldDark = Theme._darkTheme;

            Theme.switchPair(light, dark);

            // Following the OS, "system" stays the preference and only what it resolves to changes.
            // Otherwise the applied theme moves to its counterpart in the new pair. Either way the switch
            // lands on one of the pair's two themes: one of neither (a high-contrast variant, an app's
            // "brand") goes by its name's "dark" ending, as the stylesheets classify it, and else to light.
            const current = document.documentElement.getAttribute(Theme.THEME_ATTRIBUTE) || Theme._currentTheme;
            const target = Theme.shouldFollowSystem() ? (Theme.isSystemDark() ? dark : light)
                : current === oldDark ? dark
                    : current === oldLight ? light
                        : current === dark || current === light ? current
                            : /dark$/.test(current) ? dark : light;

            if (persist) {
                // The visitor's pinned theme moves with it, so the next visit restores the same scheme.
                const persisted = Theme.getPersisted();
                const pinned = persisted && persisted !== Theme.SYSTEM_THEME ? target : null;

                // The pair before the preference, so another tab adopts the pair first and then reads the
                // preference against it. The configured pair is stored as no pair at all (a scheme marker
                // at most), so the page's own pair - a preset change of the app - reaches this visitor again.
                Theme.writePersistedPair(Theme.pairStoreValue(pinned));
                if (pinned && persisted !== pinned) {
                    Theme.writePreference(pinned);
                }
            }

            Theme.applyResolvedTheme(target);
        }

        /**
         * The theme of the active pair that stands for a theme outside it: the pair's dark theme for a
         * name ending in "dark" (the rule the stylesheets' [bit-theme$="dark"] selectors classify by), its
         * light theme for one ending in "light". Anything else - a member of the pair, "system", a theme
         * of neither scheme - is kept as it is. Used for the page's own base theme, which names a theme of
         * the pair the page declares, once the visitor's pair stands in for it.
         */
        private static fitToPair(theme: string) {
            if (theme === Theme.SYSTEM_THEME || theme === Theme._lightTheme || theme === Theme._darkTheme) return theme;
            if (/dark$/.test(theme)) return Theme._darkTheme;
            if (/light$/.test(theme)) return Theme._lightTheme;
            return theme;
        }

        private static isConfiguredPair(light: string, dark: string) {
            return light === Theme._configuredLightTheme && dark === Theme._configuredDarkTheme;
        }

        private static writePairAttributes() {
            document.documentElement.setAttribute(ATTR_THEME_LIGHT, Theme._lightTheme);
            document.documentElement.setAttribute(ATTR_THEME_DARK, Theme._darkTheme);
        }

        /**
         * What is stored under PAIR_STORAGE_KEY, from localStorage first and the cookie second -
         * getPersisted's order: a "<light> <dark>" pair, or a "light" / "dark" scheme marker.
         */
        private static readPersistedPair(): { pair: [string, string] | null, scheme: string | null } {
            let value: string | null = null;
            if (Theme._persist) {
                try {
                    value = localStorage.getItem(PAIR_STORAGE_KEY);
                } catch { /* fall through to cookie / null */ }
            }

            if (!value && Theme._persistCookie) {
                value = Theme.readCookie(PAIR_COOKIE_NAME);
            }

            const parts = (value || '').split(' ');
            return {
                pair: parts.length === 2 && parts[0] && parts[1] ? [parts[0], parts[1]] : null,
                scheme: parts.length === 1 && (parts[0] === 'light' || parts[0] === 'dark') ? parts[0] : null,
            };
        }

        /**
         * What PAIR_STORAGE_KEY holds beside a stored preference: the visitor's pair while one is active;
         * otherwise "light" / "dark" when the pinned preference is that theme of the page's pair, and
         * nothing for "system", for no preference, and for a theme outside the pair.
         */
        private static pairStoreValue(pinned: string | null) {
            if (Theme._runtimePair) return `${Theme._runtimePair[0]} ${Theme._runtimePair[1]}`;
            return pinned === Theme._darkTheme ? 'dark' : pinned === Theme._lightTheme ? 'light' : null;
        }

        private static writePersistedPair(value: string | null) {
            if (Theme._persist) {
                try {
                    if (value) {
                        localStorage.setItem(PAIR_STORAGE_KEY, value);
                    } else {
                        localStorage.removeItem(PAIR_STORAGE_KEY);
                    }
                } catch { /* persistence unavailable; continue without storing */ }
            }

            if (Theme._persistCookie) {
                Theme.writeCookie(PAIR_COOKIE_NAME, value);
            }
        }

        /** Stores a preference, and what it resolves against, in every store that is on. */
        private static writePersisted(themeName: string) {
            Theme.writePreference(themeName);
            Theme.writePersistedPair(Theme.pairStoreValue(themeName === Theme.SYSTEM_THEME ? null : themeName));
        }

        /** Stores a preference in every store that is on. */
        private static writePreference(themeName: string) {
            if (Theme._persist) {
                // localStorage can throw in Safari private mode, in iframes that block storage,
                // when over quota, or under restrictive document policies (e.g. file:// in some
                // browsers). Theme persistence is best-effort - never let it break theme switching.
                try {
                    localStorage.setItem(Theme.THEME_STORAGE_KEY, themeName);
                } catch { /* persistence unavailable; continue without storing */ }
            }

            // Mirror the preference into the cookie so the server (BitThemeSsr.BuildRootThemeAttributes)
            // can paint the matching theme on first render. Stored verbatim (the abstract key, e.g.
            // "system" / "dark" / "fluent-light"), matching what localStorage holds.
            if (Theme._persistCookie) {
                Theme.writeCookie(COOKIE_NAME, themeName);
            }
        }

        /** Applies a resolved theme without touching the preference or the OS-following flags. */
        private static applyResolvedTheme(themeName: string) {
            const oldTheme = document.documentElement.getAttribute(Theme.THEME_ATTRIBUTE) || '';

            Theme._currentTheme = themeName;

            if (themeName === oldTheme) return;

            Theme.swapThemeAttribute(themeName);
            Theme.dispatchThemeChange(themeName, oldTheme);
        }

        public static applyTheme(theme: Record<string, string>, element?: HTMLElement) {
            const el = element || document.body;
            const keys = Object.keys(theme);
            const prev = Theme._appliedVarKeys.get(el) || [];
            prev.filter(key => !keys.includes(key)).forEach(key => el.style.removeProperty(key));
            keys.forEach(key => el.style.setProperty(key, theme[key]));
            Theme._appliedVarKeys.set(el, keys);

            // The family alias groups these variables re-value: every theme stylesheet re-declares its own
            // aliases of each named group on the element carrying them (Styles/theme-overlay.scss).
            Theme.writeAppliedOverlay(el, FAMILY_ALIAS_OVERLAY_GROUPS
                .filter(([, inputs]) => inputs.some(input => keys.includes(input)))
                .map(([group]) => group));
        }

        // Marks the element with the groups applyTheme re-values, merged into the value it carried before, and
        // puts that value back once applyTheme re-values none. A value written over applyTheme's own since (a
        // re-render of a BitThemeProvider on the same element) is the new one to merge into and put back.
        private static writeAppliedOverlay(el: HTMLElement, groups: string[]) {
            const current = el.getAttribute(ATTR_THEME_OVERLAY);
            const applied = Theme._appliedOverlays.get(el);
            const base = applied && applied.written === current ? applied.base : current;

            if (groups.length === 0) {
                Theme._appliedOverlays.delete(el);
                Theme.setOverlayAttribute(el, base);
                return;
            }

            const merged = (base || '').split(/\s+/).filter(group => group);
            groups.forEach(group => merged.includes(group) || merged.push(group));
            const written = merged.join(' ');

            Theme._appliedOverlays.set(el, { base, written });
            Theme.setOverlayAttribute(el, written);
        }

        private static setOverlayAttribute(el: HTMLElement, value: string | null) {
            if (value === el.getAttribute(ATTR_THEME_OVERLAY)) return;

            if (value === null) {
                el.removeAttribute(ATTR_THEME_OVERLAY);
            } else {
                el.setAttribute(ATTR_THEME_OVERLAY, value);
            }
        }

        // Puts back the overlay marker the element carried before applyTheme, unless something else has written
        // over applyTheme's own since.
        private static clearAppliedOverlay(el: HTMLElement) {
            const applied = Theme._appliedOverlays.get(el);
            if (!applied) return;

            Theme._appliedOverlays.delete(el);
            if (el.getAttribute(ATTR_THEME_OVERLAY) === applied.written) {
                Theme.setOverlayAttribute(el, applied.base);
            }
        }

        /** Removes --bit-* properties previously applied by applyTheme on the target (default document.body), and its overlay marker. */
        public static clearAppliedTheme(element?: HTMLElement) {
            const el = element || document.body;
            Theme.clearAppliedOverlay(el);
            const keys = Theme._appliedVarKeys.get(el);
            if (!keys || keys.length === 0) return;
            keys.forEach(k => el.style.removeProperty(k));
            Theme._appliedVarKeys.delete(el);
        }

        public static isSystemDark() {
            return matchMedia('(prefers-color-scheme: dark)').matches;
        }

        public static getPersisted() {
            if (Theme._persist) {
                // Mirror the write side: localStorage.getItem can throw under the same conditions as
                // setItem (Safari private mode, blocked storage, etc.). Treat failure as "no persisted
                // value" so the rest of the resolution chain (system / default / lightTheme) takes over.
                try {
                    const stored = localStorage.getItem(Theme.THEME_STORAGE_KEY);
                    if (stored) return stored;
                } catch { /* fall through to cookie / null */ }
            }

            // Cookie-only persistence (or localStorage unavailable): read the same preference the
            // server uses so the client and server agree on the stored choice.
            if (Theme._persistCookie) {
                return Theme.readCookie(COOKIE_NAME);
            }

            return null;
        }

        public static registerDotNetNotifier(dotNetRef: DotNetObject) {
            Theme._dotnetNotifier = dotNetRef;
        }

        public static unregisterDotNetNotifier() {
            Theme._dotnetNotifier = null;
        }

        /** Writes a cookie, or expires it for a null value. */
        private static writeCookie(name: string, value: string | null) {
            try {
                if (typeof document === 'undefined' || value === '') return;
                const secure = location.protocol === 'https:' ? '; Secure' : '';
                document.cookie = value === null
                    ? `${name}=; path=/; max-age=0; SameSite=Lax${secure}`
                    : `${name}=${encodeURIComponent(value)}; path=/; max-age=${COOKIE_MAX_AGE_SECONDS}; SameSite=Lax${secure}`;
            } catch { /* cookies unavailable / blocked; best-effort like localStorage */ }
        }

        private static readCookie(name: string): string | null {
            try {
                if (typeof document === 'undefined' || !document.cookie) return null;
                const prefix = `${name}=`;
                const match = document.cookie
                    .split(';')
                    .map(c => c.trim())
                    .find(c => c.startsWith(prefix));
                return match ? decodeURIComponent(match.substring(prefix.length)) : null;
            } catch {
                return null;
            }
        }

        private static shouldFollowSystem(): boolean {
            if (typeof document === 'undefined') return false;
            if (Theme._stopFollowingSystem) return false;
            // An explicitly persisted theme (anything other than SYSTEM_THEME) wins over the
            // bit-theme-system attribute, otherwise a stale attribute could override the user's choice.
            // Consider the persisted preference whenever any persistence source is active - including
            // the cookie-only path (persistCookie without persist) that getPersisted() falls back to -
            // so a persisted SYSTEM_THEME still attaches the OS-follow listener in a cookie-backed setup.
            if (Theme._persist || Theme._persistCookie) {
                const persisted = Theme.getPersisted();
                if (persisted && persisted !== Theme.SYSTEM_THEME) return false;
                if (persisted === Theme.SYSTEM_THEME) return true;
            }
            if (Theme._runtimeFollowSystem) return true;
            if (document.documentElement.hasAttribute(ATTR_THEME_SYSTEM)) return true;
            return false;
        }

        private static syncSystemThemeListener() {
            Theme.detachSystemThemeListener();
            if (!Theme.shouldFollowSystem()) return;
            Theme.attachSystemThemeListener();
        }

        private static attachSystemThemeListener() {
            if (!window.matchMedia) return;
            Theme._schemeMediaQuery = matchMedia('(prefers-color-scheme: dark)');
            const mq = Theme._schemeMediaQuery as MediaQueryList & { addListener?: (cb: () => void) => void };
            if (typeof mq.addEventListener === 'function') {
                mq.addEventListener('change', Theme._onSchemeChange);
            } else {
                mq.addListener?.(Theme._onSchemeChange);
            }
        }

        private static detachSystemThemeListener() {
            if (!Theme._schemeMediaQuery) return;
            const mq = Theme._schemeMediaQuery as MediaQueryList & { removeListener?: (cb: () => void) => void };
            if (typeof mq.removeEventListener === 'function') {
                mq.removeEventListener('change', Theme._onSchemeChange);
            } else {
                mq.removeListener?.(Theme._onSchemeChange);
            }
            Theme._schemeMediaQuery = null;
        }

        private static applyResolvedSystemThemeFromOs() {
            if (!Theme.shouldFollowSystem()) return;

            Theme.applyResolvedTheme(Theme.isSystemDark() ? Theme._darkTheme : Theme._lightTheme);
        }

        /**
         * Writes the bit-theme attribute, optionally inside a View Transition so the whole page
         * cross-fades to the new palette instead of hard-swapping. Opt-in via the
         * bit-theme-view-transition attribute on the document element (checked live, so it can be
         * toggled at runtime). The transition is skipped - falling back to a plain synchronous
         * attribute write - when the API is unavailable or the user prefers reduced motion.
         *
         * Note: startViewTransition invokes its callback asynchronously (after capturing the old
         * frame), so with the transition active the attribute updates a frame later than the
         * Theme.set call. _currentTheme is always updated synchronously by the callers, and the
         * change notifications carry the explicit old/new names, so observers are unaffected.
         */
        private static swapThemeAttribute(themeName: string, skipTransition?: boolean) {
            const apply = () => document.documentElement.setAttribute(Theme.THEME_ATTRIBUTE, themeName);

            // A no-op swap (re-setting the already-applied theme, e.g. pinning the currently
            // resolved preset) must not run a transition: startViewTransition captures and
            // cross-fades even when the callback changes nothing visible.
            if (skipTransition || document.documentElement.getAttribute(Theme.THEME_ATTRIBUTE) === themeName) {
                apply();
                return;
            }

            try {
                const doc = document as Document & { startViewTransition?: (callback: () => void) => unknown };
                if (document.documentElement.hasAttribute(ATTR_THEME_VIEW_TRANSITION) &&
                    typeof doc.startViewTransition === 'function' &&
                    !(typeof matchMedia === 'function' && matchMedia('(prefers-reduced-motion: reduce)').matches)) {
                    doc.startViewTransition(apply);
                    return;
                }
            } catch { /* View Transition unavailable or failed to start; fall through to the direct swap */ }

            apply();
        }

        private static dispatchThemeChange(newTheme: string, oldTheme: string) {
            // Isolate the application callback: a throwing _onThemeChange must not abort the
            // _dotnetNotifier notification below (nor the Theme.set flow into syncSystemThemeListener()).
            try {
                Theme._onThemeChange?.(newTheme, oldTheme);
            } catch { /* application callback failed; not actionable here */ }

            // Broadcast a DOM event so non-CSS consumers (charts, canvas, iframes, third-party
            // widgets) can react without owning the single _onThemeChange callback. Best-effort:
            // a missing CustomEvent / document (non-browser host) must not break theme switching.
            try {
                if (typeof document !== 'undefined' && typeof CustomEvent === 'function') {
                    document.dispatchEvent(new CustomEvent(Theme.THEME_CHANGE_EVENT, {
                        detail: { newTheme, oldTheme },
                    }));
                }
            } catch { /* event dispatch unavailable; not actionable here */ }
            const n = Theme._dotnetNotifier;
            if (n) {
                // Swallow rejections so a disposed circuit / receiver does not surface as an
                // unhandled promise rejection. Theme dispatch is fire-and-forget by design.
                n.invokeMethodAsync('NotifyThemeChangedFromJs', newTheme, oldTheme)
                    .catch(() => { /* receiver gone or invocation failed; nothing actionable here */ });
            }
        }

        private static getActualTheme(theme: string | null) {
            if (theme === Theme.SYSTEM_THEME) {
                return Theme.isSystemDark() ? Theme._darkTheme : Theme._lightTheme;
            }

            return theme;
        }
    }

    /**
     * Keeps every `<meta name="theme-color">` tag equal to a palette color of the live page, so the
     * browser chrome an app cannot reach from CSS - an installed PWA's status bar, the address bar
     * on mobile - stays with the theme. Opted into with the bit-theme-color-meta attribute on
     * `<html>` (or `themeColorMeta` on init); off by default, since an app that hardcodes its tag
     * must keep the tag it wrote.
     *
     * The color is READ BACK from the page rather than mapped from the theme name, because a name
     * does not carry a color: the packaged design systems (Fluent 2, Material, Cupertino) each paint
     * their own surfaces, an app's stylesheet may re-declare the tokens, and an accent (see
     * BitAccentColor in Bit.BlazorUI.Extras) re-derives the whole palette - surfaces included - into
     * an applyTheme overlay on `<body>`. It is read off `<body>` for that last reason, and on a
     * schedule of its own rather than from the theme-change notification: with
     * bit-theme-view-transition the bit-theme attribute is only written a frame AFTER the change is
     * announced, so a listener would still see the outgoing palette. What it watches instead is the
     * DOM the color actually comes from.
     */
    export class ThemeColorMeta {
        private static _variable: string | null = null;
        private static _observer: MutationObserver | null = null;
        private static _pending = false;
        private static _loadListenerAttached = false;
        private static _linkListeners: { link: HTMLLinkElement, handler: () => void }[] = [];

        /**
         * Starts (or retargets) the sync. `variable` names the custom property to read and defaults
         * to THEME_COLOR_VARIABLE; a name without the leading `--` is accepted.
         */
        public static enable(variable?: string | null) {
            const name = (variable || '').trim() || THEME_COLOR_VARIABLE;
            ThemeColorMeta._variable = name.indexOf('--') === 0 ? name : `--${name}`;

            ThemeColorMeta.whenBodyReady(() => {
                // disable() may have run while this callback was still waiting on DOMContentLoaded.
                // It disconnected nothing (there was no observer yet) and has already let go of the
                // handle it would disconnect later, so an observer installed now would outlive the
                // sync it belongs to and watch the document for the rest of the page's life.
                if (!ThemeColorMeta._variable) return;

                ThemeColorMeta.observe();
                ThemeColorMeta.sync();
            });

            // A stylesheet still in flight leaves the custom property empty, and a pending stylesheet
            // changes no node, so nothing the observer watches would fire once it lands. One catch-up
            // after load covers the app that links its stylesheets with a script already running.
            if (!ThemeColorMeta._loadListenerAttached && typeof window !== 'undefined' && window.addEventListener) {
                ThemeColorMeta._loadListenerAttached = true;
                window.addEventListener('load', () => ThemeColorMeta.schedule(), { once: true } as any);
            }
        }

        /** Stops the sync and leaves the tags on whatever color they currently carry. */
        public static disable() {
            ThemeColorMeta._variable = null;
            ThemeColorMeta._observer?.disconnect();
            ThemeColorMeta._observer = null;
            ThemeColorMeta._linkListeners.forEach(entry => entry.link.removeEventListener('load', entry.handler));
            ThemeColorMeta._linkListeners = [];
        }

        /**
         * Re-reads once the given stylesheet has loaded. A `<link>` appended by ExternalTheme.attach()
         * is a `<head>` childList change, so the observer does schedule a read - but it runs while the
         * stylesheet is still in flight, on the outgoing palette, and a `<link>` REUSED by attach()
         * only has its href rewritten, which changes no node the observer watches at all. Either way
         * the arriving stylesheet moves nothing afterwards, so its load event is the only signal that
         * the new colors are readable. A no-op while the sync is off, so an app that never opted in
         * keeps no listener.
         */
        public static watchStylesheet(link: HTMLLinkElement) {
            if (!ThemeColorMeta._variable || !link?.addEventListener) return;
            // attach() can be called repeatedly on the same <link>; one listener per element is enough,
            // since the handler reads the live color rather than the href it was registered for.
            if (ThemeColorMeta._linkListeners.some(entry => entry.link === link)) return;

            const handler = () => ThemeColorMeta.schedule();
            link.addEventListener('load', handler);
            ThemeColorMeta._linkListeners.push({ link, handler });
        }

        /** Drops the listener of a stylesheet that is going away, so the detached node is not held. */
        public static unwatchStylesheet(link: Element) {
            ThemeColorMeta._linkListeners = ThemeColorMeta._linkListeners.filter(entry => {
                if (entry.link !== link) return true;
                entry.link.removeEventListener('load', entry.handler);
                return false;
            });
        }

        /**
         * Re-reads the color and rewrites the tags now. Public so an app can force a refresh after a
         * change none of the watched nodes shows - e.g. a stylesheet swapped through ExternalTheme.
         */
        public static sync() {
            ThemeColorMeta._pending = false;

            const variable = ThemeColorMeta._variable;
            if (!variable) return;

            // The overlay applyTheme writes lands on <body> (custom properties inherit, so a :root
            // declaration is read here just the same); before the body exists, nothing is painted yet.
            const source = document.body;
            if (!source) return;

            let color = '';
            try {
                color = getComputedStyle(source).getPropertyValue(variable).trim();
            } catch { return; /* computed styles unavailable (detached / hidden document) */ }

            // Empty means the stylesheet declaring it has not arrived (or the name is a typo): keep
            // whatever the document already carries rather than blanking a tag the app hand-wrote.
            if (!color) return;

            const tags = document.querySelectorAll('meta[name=theme-color]');

            // Nothing to keep in sync yet - an app that opted in without writing a tag gets one.
            if (tags.length === 0) {
                const meta = document.createElement('meta');
                meta.setAttribute('name', 'theme-color');
                meta.setAttribute('content', color);
                document.head.appendChild(meta);
                return;
            }

            // Every tag, so a document that splits them by media (a light one and a dark one, the
            // usual first-paint trick) has the pinned theme win over the OS on both. Writing only
            // what changed keeps this off the browser's chrome-repaint path on unrelated mutations.
            tags.forEach(tag => {
                if (tag.getAttribute('content') !== color) {
                    tag.setAttribute('content', color);
                }
            });
        }

        private static schedule = () => {
            if (ThemeColorMeta._pending || !ThemeColorMeta._variable) return;
            ThemeColorMeta._pending = true;

            // One read per frame at most: a theme swap moves an attribute, an accent moves the body
            // overlay and a <head> style within the same tick, and all of them want the same read.
            const run = () => ThemeColorMeta.sync();
            if (typeof requestAnimationFrame === 'function') {
                requestAnimationFrame(run);
            } else {
                setTimeout(run, 16);
            }
        };

        private static observe() {
            if (ThemeColorMeta._observer || typeof MutationObserver !== 'function') return;

            const observer = new MutationObserver(ThemeColorMeta.schedule);
            // <html> unfiltered: bit-theme and the accent's bit-accent are the two that matter here,
            // but an app's own attribute may select a palette too, and a coalesced read is cheap
            // enough that guessing the list would cost more than it saves.
            observer.observe(document.documentElement, { attributes: true });
            // The applyTheme overlay (an accent, or an app applying a theme object) as inline
            // custom properties, and a class that switches palettes.
            observer.observe(document.body, { attributes: true, attributeFilter: ['style', 'class'] });
            // The accent's first-paint <style> snapshot being injected or replaced. Also catches an
            // app's own late stylesheet. Appending the tag above is a childList change too, but the
            // read it schedules then finds the color unchanged and stops there.
            observer.observe(document.head, { childList: true });

            ThemeColorMeta._observer = observer;
        }

        private static whenBodyReady(action: () => void) {
            if (document.body) {
                action();
                return;
            }

            // The library script can be loaded from <head>; everything here needs the body element.
            document.addEventListener('DOMContentLoaded', action, { once: true } as any);
        }
    }

    /** Attach or swap alternate theme stylesheets at runtime (prefer same-origin / trusted URLs). */
    export class ExternalTheme {
        private static validateHref(href: string) {
            const trimmed = href?.trimStart();
            if (!trimmed) {
                throw new Error('Stylesheet href is required.');
            }
            if (trimmed.startsWith('//') || trimmed.startsWith('\\\\') ||
                trimmed.startsWith('/\\') || trimmed.startsWith('\\/')) {
                throw new Error('Stylesheet href must not be a protocol-relative URL.');
            }
            if (/^javascript:/i.test(trimmed) || /^data:/i.test(trimmed) || /^vbscript:/i.test(trimmed)) {
                throw new Error('Stylesheet href must not use a non-http scheme.');
            }
            let url: URL;
            try {
                url = new URL(href, document.baseURI);
            } catch {
                throw new Error('Stylesheet href is not a valid URL.');
            }
            if (url.protocol !== 'http:' && url.protocol !== 'https:') {
                throw new Error(`Stylesheet href scheme '${url.protocol}' is not allowed.`);
            }
            if (url.origin !== location.origin) {
                throw new Error('Stylesheet href must be same-origin.');
            }
        }

        public static attach(linkId: string, href: string) {
            ExternalTheme.validateHref(href);

            const existing = document.getElementById(linkId);
            let link: HTMLLinkElement;
            if (existing && existing.tagName === 'LINK') {
                link = existing as HTMLLinkElement;
            } else {
                // No element, or an element with the same id but a different tag (e.g. a stale
                // <style> or <meta>): replace/insert a fresh <link> rather than blindly mutating
                // an unrelated node which would break attach/detach invariants.
                existing?.remove();
                link = document.createElement('link');
                link.id = linkId;
                document.head.appendChild(link);
            }
            // Set rel on both the reuse and creation paths so a reused <link> that was created with a
            // different rel is corrected before we point it at the stylesheet href.
            link.rel = 'stylesheet';
            link.href = href;

            // The swapped-in stylesheet is what repaints the surfaces, so the theme-color tags have to
            // wait for it; registering after href is set is safe, load fires on a later task.
            ThemeColorMeta.watchStylesheet(link);
        }

        public static detach(linkId: string) {
            const el = document.getElementById(linkId);
            // Only remove the element if it's actually a <link>; we should not garbage-collect
            // unrelated nodes that happen to share the id.
            if (el && el.tagName === 'LINK') {
                ThemeColorMeta.unwatchStylesheet(el);
                el.remove();
            }
        }
    }

    // Self-init from <html> attributes.
    Theme.init({
        system: document.documentElement.hasAttribute(ATTR_THEME_SYSTEM),
        persist: document.documentElement.hasAttribute(ATTR_THEME_PERSIST),
        persistCookie: document.documentElement.hasAttribute(ATTR_THEME_PERSIST_COOKIE),
        theme: document.documentElement.getAttribute(ATTR_THEME),
        default: document.documentElement.getAttribute(ATTR_THEME_DEFAULT),
        darkTheme: document.documentElement.getAttribute(ATTR_THEME_DARK),
        lightTheme: document.documentElement.getAttribute(ATTR_THEME_LIGHT),
        // Absent stays undefined rather than false: an app that turns the sync on through init()
        // instead of the attribute must not have it turned back off by this call.
        themeColorMeta: document.documentElement.hasAttribute(ATTR_THEME_COLOR_META)
            ? (document.documentElement.getAttribute(ATTR_THEME_COLOR_META) || true)
            : undefined,
    });
}
