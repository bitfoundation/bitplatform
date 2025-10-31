// Blazor types:
declare interface DotNetObject {
    invokeMethod<T>(methodIdentifier: string, ...args: any[]): T;
    invokeMethodAsync<T>(methodIdentifier: string, ...args: any[]): Promise<T>;
    dispose(): void;
}

// Bit.Butil types:
declare class BitButil {
    static webAuthn: {
        getCredential: (options: unknown) => Promise<unknown>,
        createCredential: (options: unknown) => Promise<unknown>
    }
}

// Bit.BlazorUI types:
declare class BitTheme { static init(options: BitThemeOptions): void; }

declare interface BitThemeOptions {
    system?: boolean;
    persist?: boolean;
    theme?: string | null;
    default?: string | null;
    darkTheme?: string | null;
    lightTheme?: string | null;
    onChange?: onThemeChangeType;
}

declare type onThemeChangeType = (newThemeName: string, oldThemeName: string) => void;