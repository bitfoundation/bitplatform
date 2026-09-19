//+:cnd:noEmit

// Blazor types:
declare interface DotNetObject {
    invokeMethod<T>(methodIdentifier: string, ...args: any[]): T;
    invokeMethodAsync<T>(methodIdentifier: string, ...args: any[]): Promise<T>;
    dispose(): void;
}

// Bit.Butil types (bit-butil.js, see Scripts/webAuthn.ts in Bit.Butil):
declare namespace BitButil {
    /** The JSON-serializable `PublicKeyCredential`, with every binary field base64url-encoded. */
    interface WebAuthnCredential<TResponse> {
        id: string;
        rawId: string;
        type: string;
        clientExtensionResults: AuthenticationExtensionsClientOutputs;
        response: TResponse;
    }

    interface WebAuthnAttestationResponse {
        attestationObject: string;
        clientDataJSON: string;
        transports: string[];
    }

    interface WebAuthnAssertionResponse {
        authenticatorData: string;
        clientDataJSON: string;
        userHandle: string | null;
        signature: string;
    }

    const webAuthn: {
        isSupported(): boolean;
        isAvailable(): boolean;
        /** Accepts the server's JSON options, whose binary fields are base64url strings. */
        createCredential(options: unknown): Promise<WebAuthnCredential<WebAuthnAttestationResponse>>;
        /** Accepts the server's JSON options, whose binary fields are base64url strings. */
        getCredential(options: unknown): Promise<WebAuthnCredential<WebAuthnAssertionResponse>>;
    };
}

// Bit.Bswup types (bit-bswup.js and bit-bswup.progress.js, see Scripts in Bit.Bswup):
declare namespace BitBswup {
    interface Api {
        version: string;
        /** Checks the server for a new version of the service worker. */
        checkForUpdate(): Promise<void>;
        /** Activates a downloaded update and reloads the page; resolves `false` when there is nothing to activate. */
        skipWaiting(): Promise<boolean>;
        /** Clears the app's caches (or the ones matching the filter), unregisters its service worker and reloads the page. */
        forceRefresh(cacheFilter?: string | RegExp | ((key: string) => boolean)): Promise<void>;
        /** Requests eviction-resistant storage for the origin and resolves with whether it is now persistent. */
        persistStorage(): Promise<boolean>;
    }

    interface ProgressConfigs {
        autoReload?: boolean;
        showLogs?: boolean;
        showAssets?: boolean;
        hideApp?: boolean;
        autoHide?: boolean;
        showOnUpdate?: boolean;
    }

    interface ProgressApi {
        start(autoReload: boolean, showLogs: boolean, showAssets: boolean, appContainerSelector: string,
            hideApp: boolean, autoHide: boolean, handler?: string | null, showOnUpdate?: boolean): void;
        /** Overrides the options passed to `start` at runtime. */
        config(configs: ProgressConfigs): void;
    }
}

// Bit.BlazorUI types (bit.blazorui.js, see Utils/Theme/BitTheme.ts in Bit.BlazorUI):
declare namespace BitBlazorUI {
    type ThemeChangeHandler = (newThemeName: string, oldThemeName: string) => void;

    interface ThemeOptions {
        /** Follows the OS light/dark preference (same as the `bit-theme-system` attribute). */
        system?: boolean;
        /** Persists the chosen theme in localStorage (same as the `bit-theme-persist` attribute). */
        persist?: boolean;
        /** Mirrors the chosen theme into a cookie so SSR paints it on first render (same as the `bit-theme-persist-cookie` attribute). */
        persistCookie?: boolean;
        /** The theme to apply (same as the `bit-theme` attribute). */
        theme?: string | null;
        /** The fallback theme when nothing else resolves one (same as the `bit-theme-default` attribute). */
        default?: string | null;
        /** The theme used for dark mode, `dark` by default (same as the `bit-theme-dark` attribute). */
        darkTheme?: string | null;
        /** The theme used for light mode, `light` by default (same as the `bit-theme-light` attribute). */
        lightTheme?: string | null;
        /** The single callback invoked after every theme change; use the `bit-theme-change` DOM event for more listeners. */
        onChange?: ThemeChangeHandler;
    }

    interface ThemeChangeEventDetail {
        newTheme: string;
        oldTheme: string;
    }

    class Theme {
        /** The name of the DOM event dispatched on `document` after every theme change. */
        static readonly THEME_CHANGE_EVENT: 'bit-theme-change';

        /** Merges the options into the ones already applied (the script self-inits from the `<html>` attributes) and re-applies the theme. */
        static init(options: ThemeOptions): void;
        /** Replaces the callback invoked after every theme change. */
        static onChange(fn: ThemeChangeHandler): void;
        /** Returns the theme currently applied to the document. */
        static get(): string;
        /** Applies a theme (`system` follows the OS) and returns the resolved theme name. */
        static set(themeName: string): string;
        /** Switches between the configured dark and light themes and returns the new theme name. */
        static toggleDarkLight(): string;
        /** Follows the OS light/dark preference until another theme is set, and returns the resolved theme name. */
        static useSystem(): string;
        /** Applies the given CSS variables (e.g. `--bit-clr-pri`) inline on the element (default: `document.body`). */
        static applyTheme(theme: Record<string, string>, element?: HTMLElement): void;
        /** Removes the CSS variables previously applied by `applyTheme` from the element (default: `document.body`). */
        static clearAppliedTheme(element?: HTMLElement): void;
        static isSystemDark(): boolean;
        /** Returns the persisted theme preference (which may be `system`), or `null` when nothing is persisted. */
        static getPersisted(): string | null;
        static registerDotNetNotifier(dotNetRef: DotNetObject): void;
        static unregisterDotNetNotifier(): void;
    }

    /** Attaches or swaps alternate same-origin theme stylesheets at runtime. */
    class ExternalTheme {
        static attach(linkId: string, href: string): void;
        static detach(linkId: string): void;
    }
}

// eruda types (loaded on demand by App.openDevTools, https://github.com/liriliri/eruda):
declare interface Eruda {
    init(): void;
    show(): void;
}

//#if (ads == true)
// Google Publisher Tag types (the subset Ads.ts uses, https://developers.google.com/publisher-tag/reference):
declare namespace GooglePublisherTag {
    interface Slot {
        addService(service: PubAdsService): Slot;
    }

    interface RewardedPayload {
        amount: number;
        type: string;
    }

    interface RewardedSlotReadyEvent {
        slot: Slot;
        makeRewardedVisible(): boolean;
    }

    interface RewardedSlotGrantedEvent {
        slot: Slot;
        payload: RewardedPayload | null;
    }

    interface SlotRenderEndedEvent {
        slot: Slot;
        isEmpty: boolean;
    }

    interface EventTypeMap {
        rewardedSlotReady: RewardedSlotReadyEvent;
        rewardedSlotGranted: RewardedSlotGrantedEvent;
        rewardedSlotClosed: { slot: Slot };
        slotRenderEnded: SlotRenderEndedEvent;
    }

    interface PubAdsService {
        addEventListener<K extends keyof EventTypeMap>(eventType: K, listener: (event: EventTypeMap[K]) => void): PubAdsService;
    }

    interface Googletag {
        /** A plain array until gpt.js loads and replaces it with one that runs every pushed command. */
        cmd: Array<() => void> | { push(...commands: Array<() => void>): number };
        enums: { OutOfPageFormat: { REWARDED: number } };
        defineOutOfPageSlot(adUnitPath: string, format: number): Slot | null;
        pubads(): PubAdsService;
        enableServices(): void;
        display(slot: Slot): void;
        destroySlots(slots?: Slot[]): boolean;
    }
}
//#endif

interface Window {
    /** Only defined when bit-bswup.js is loaded (Blazor WebAssembly). */
    BitBswup?: BitBswup.Api;
    /** Only defined when bit-bswup.progress.js is loaded (Blazor WebAssembly). */
    BitBswupProgress?: BitBswup.ProgressApi;
    /** Only defined once App.openDevTools has loaded eruda. */
    eruda?: Eruda;
    //#if (ads == true)
    /** A command queue stub until gpt.js loads. */
    googletag?: GooglePublisherTag.Googletag;
    //#endif
}

interface DocumentEventMap {
    'bit-theme-change': CustomEvent<BitBlazorUI.ThemeChangeEventDetail>;
}
