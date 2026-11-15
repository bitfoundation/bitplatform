namespace BitBlazorUI {
    export class AiBrowserTimeZone {
        // The IANA id of the time zone the browser runs in - the user's own, which on Blazor Server is not the one
        // TimeZoneInfo.Local reads there (the server's). Null where the engine cannot tell.
        public static get(): string | null {
            try {
                return Intl.DateTimeFormat().resolvedOptions().timeZone || null;
            } catch {
                return null;
            }
        }
    }
}
