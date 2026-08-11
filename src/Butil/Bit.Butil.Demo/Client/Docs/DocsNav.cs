namespace Bit.Butil.Demo.Client.Docs;

public record DocLink(string Title, string Url, string Icon, string Summary);

public record DocGroup(string Title, DocLink[] Links);

/// <summary>
/// The single source of truth for the site taxonomy: the sidebar, the home page feature grid
/// and the prev/next pager are all rendered from this list.
/// </summary>
public static class DocsNav
{
    public static readonly DocGroup[] Groups =
    [
        new("Window & Browsing",
        [
            new("Window", "window", "🪟", "The DOM window object: events, dialogs, sizes and more."),
            new("Document", "document", "📄", "The DOM document object: title, cookies, fullscreen, design mode."),
            new("History", "history", "🕘", "Session history: navigate back/forward and push/replace states."),
            new("Location", "location", "📍", "Read and mutate the current URL, reload or navigate."),
            new("Navigator", "navigator", "🧭", "Browser identity, languages, share, vibrate, badges and more."),
            new("UserAgent", "user-agent", "🪪", "Parsed user-agent brands, platform and mobile-ness."),
        ]),
        new("Screen & Diagnostics",
        [
            new("Screen", "screen", "🖥️", "Physical screen metrics, color depth and availability."),
            new("ScreenOrientation", "screen-orientation", "🔄", "Read, lock and observe the screen orientation."),
            new("VisualViewport", "visual-viewport", "🔍", "The visual viewport: scale, offsets and resize events."),
            new("Performance", "performance", "⏱️", "High-resolution timing, marks, measures and entries."),
            new("Reporting", "reporting", "🧾", "Observe deprecation, intervention and crash reports."),
            new("Console", "console", "🖨️", "The full browser console API from C#: log, table, group, time."),
        ]),
        new("DOM & Interaction",
        [
            new("Element", "element", "🧱", "Attributes, scrolling, fullscreen, pointer capture and events on any ElementReference."),
            new("Keyboard", "keyboard", "⌨️", "App-wide keyboard shortcuts with modifier support."),
            new("IntersectionObserver", "intersection-observer", "👁️", "Observe element visibility inside the viewport or a scroll container."),
            new("MutationObserver", "mutation-observer", "🧬", "Observe DOM tree, attribute and character-data mutations."),
            new("ResizeObserver", "resize-observer", "📐", "Observe element size changes with box-model detail."),
        ]),
        new("Storage",
        [
            new("Local & Session Storage", "storage", "💾", "Synchronous key/value storage scoped to the origin or the tab."),
            new("Cookie", "cookie", "🍪", "Read, set and remove document cookies with full options."),
            new("CookieStore", "cookie-store", "🗄️", "The async Cookie Store API with change events."),
            new("IndexedDb", "indexed-db", "🧮", "Structured, transactional client-side database."),
            new("CacheStorage", "cache-storage", "📦", "The service-worker Cache API: store and match requests."),
            new("StorageManager", "storage-manager", "🧰", "Storage quota, usage estimates and persistence."),
        ]),
        new("Files & Data",
        [
            new("FileReader", "file-reader", "📁", "Read user-selected files as text, data URLs or bytes."),
            new("ObjectUrls", "object-urls", "🔗", "Create and revoke blob: object URLs from C# data."),
            new("Clipboard", "clipboard", "📋", "Read and write text and typed items on the system clipboard."),
            new("Crypto", "crypto", "🔐", "SubtleCrypto encryption, decryption, hashing and random values."),
            new("Fetch", "fetch", "🌐", "The browser fetch API with full request/response control."),
        ]),
        new("Network & Workers",
        [
            new("ServiceWorker", "service-worker", "⚙️", "Register and inspect service workers."),
            new("BackgroundSync", "background-sync", "🔁", "Defer work until the user has connectivity."),
            new("Push", "push", "📨", "Subscribe to web push notifications."),
            new("NetworkInformation", "network-information", "📶", "Connection type, speed, save-data and change events."),
            new("BroadcastChannel", "broadcast-channel", "📡", "Message other tabs and windows of the same origin."),
            new("WebLocks", "web-locks", "🔒", "Cross-tab cooperative resource locking."),
        ]),
        new("Device & Hardware",
        [
            new("Battery", "battery", "🔋", "Battery level, charging state and related events."),
            new("Geolocation", "geolocation", "🛰️", "Current position, watch positions and errors."),
            new("MediaDevices", "media-devices", "🎥", "Enumerate cameras/microphones and query capabilities."),
            new("Nfc", "nfc", "🏷️", "Read and write NDEF messages on NFC tags."),
            new("WakeLock", "wake-lock", "☀️", "Keep the screen awake while your app needs it."),
            new("IdleDetector", "idle-detector", "💤", "Detect user and screen idle state changes."),
            new("ContactPicker", "contact-picker", "👥", "Let users pick contacts to share with your app."),
            new("EyeDropper", "eye-dropper", "🎨", "Sample any pixel color on the screen."),
        ]),
        new("Identity & Permissions",
        [
            new("WebAuthn", "web-authn", "🔑", "Passkeys: create credentials and verify assertions."),
            new("Permissions", "permissions", "✅", "Query the state of any browser permission."),
            new("Notification", "notification", "🔔", "Request permission and show system notifications."),
        ]),
        new("Media & Speech",
        [
            new("SpeechSynthesis", "speech-synthesis", "🗣️", "Text-to-speech with voices, pitch and rate."),
            new("SpeechRecognition", "speech-recognition", "🎙️", "Speech-to-text with interim results and events."),
            new("WebAudio", "web-audio", "🎵", "Play and control audio buffers with the Web Audio API."),
        ]),
    ];

    public static IEnumerable<DocLink> AllLinks => Groups.SelectMany(g => g.Links);
}
