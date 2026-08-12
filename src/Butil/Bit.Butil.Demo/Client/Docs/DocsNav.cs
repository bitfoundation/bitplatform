namespace Bit.Butil.Demo.Client.Docs;

/// <summary>
/// How widely the underlying browser API is implemented. This is about the web platform, not about
/// Butil: every wrapper on this site works everywhere Blazor does, but it can only expose what the
/// browser underneath it implements.
/// </summary>
public enum ApiSupport
{
    /// <summary>Not a browser API at all - a guide page.</summary>
    Guide,

    /// <summary>Implemented by every current engine.</summary>
    Broad,

    /// <summary>Implemented everywhere, but with members or behaviour that differ between engines.</summary>
    Partial,

    /// <summary>Chromium only (Chrome, Edge, Opera and friends).</summary>
    Chromium,

    /// <summary>Chromium on desktop only.</summary>
    ChromiumDesktop,

    /// <summary>Chromium on Android only.</summary>
    ChromiumMobile,
}

/// <summary>
/// The preconditions an API imposes on the calling page, beyond simply being implemented.
/// </summary>
[Flags]
public enum ApiNeeds
{
    None = 0,

    /// <summary>Only available over HTTPS or on localhost.</summary>
    SecureContext = 1,

    /// <summary>The browser prompts the user, and the call fails if permission is denied.</summary>
    Permission = 2,

    /// <summary>Must be called from a user-gesture handler such as a click.</summary>
    UserGesture = 4,

    /// <summary>Behind an experimental or origin-trial flag in at least one shipping engine.</summary>
    Experimental = 8,
}

public record DocLink(
    string Title,
    string Url,
    string Icon,
    string Summary,
    ApiSupport Support = ApiSupport.Broad,
    ApiNeeds Needs = ApiNeeds.None);

public record DocGroup(string Title, DocLink[] Links);

/// <summary>
/// The single source of truth for the site taxonomy: the sidebar, the home page feature grid,
/// the browser-support matrix and the prev/next pager are all rendered from this list.
/// </summary>
public static class DocsNav
{
    public static readonly DocGroup[] Groups =
    [
        new("Overview",
        [
            new("Getting started", "getting-started", "🚀", "Install the package, add the script, register the services.", ApiSupport.Guide),
            new("Render modes", "render-modes", "🧩", "How Butil behaves under WebAssembly, Server, Hybrid and prerendering.", ApiSupport.Guide),
            new("Browser support", "browser-support", "🧪", "Which APIs need a secure context, a permission or a specific engine.", ApiSupport.Guide),
            new("Troubleshooting", "troubleshooting", "🩺", "The errors people hit first, and what each one actually means.", ApiSupport.Guide),
        ]),
        new("Window & Browsing",
        [
            new("Window", "window", "🪟", "The DOM window object: events, dialogs, sizes and more."),
            new("Document", "document", "📄", "The DOM document object: title, cookies, fullscreen, design mode.", ApiSupport.Broad, ApiNeeds.UserGesture),
            new("History", "history", "🕘", "Session history: navigate back/forward and push/replace states."),
            new("Location", "location", "📍", "Read and mutate the current URL, reload or navigate."),
            new("Navigator", "navigator", "🧭", "Browser identity, languages, share, vibrate, badges and more.", ApiSupport.Partial, ApiNeeds.SecureContext | ApiNeeds.UserGesture),
            new("UserAgent", "user-agent", "🪪", "Parsed user-agent brands, platform and mobile-ness.", ApiSupport.Partial),
        ]),
        new("Screen & Diagnostics",
        [
            new("Screen", "screen", "🖥️", "Physical screen metrics, color depth and availability.", ApiSupport.Partial),
            new("ScreenOrientation", "screen-orientation", "🔄", "Read, lock and observe the screen orientation.", ApiSupport.Partial),
            new("VisualViewport", "visual-viewport", "🔍", "The visual viewport: scale, offsets and resize events."),
            new("Performance", "performance", "⏱️", "High-resolution timing, marks, measures and entries.", ApiSupport.Partial),
            new("Reporting", "reporting", "🧾", "Observe deprecation, intervention and crash reports.", ApiSupport.Chromium),
            new("Console", "console", "🖨️", "The full browser console API from C#: log, table, group, time."),
        ]),
        new("DOM & Interaction",
        [
            new("Element", "element", "🧱", "Attributes, scrolling, fullscreen, pointer capture and events on any ElementReference."),
            new("Animation", "animation", "✨", "Run and control Web Animations on any element, straight from C#."),
            new("Keyboard", "keyboard", "⌨️", "App-wide keyboard shortcuts with modifier support."),
            new("IntersectionObserver", "intersection-observer", "👁️", "Observe element visibility inside the viewport or a scroll container."),
            new("MutationObserver", "mutation-observer", "🧬", "Observe DOM tree, attribute and character-data mutations."),
            new("ResizeObserver", "resize-observer", "📐", "Observe element size changes with box-model detail."),
        ]),
        new("Storage",
        [
            new("Local & Session Storage", "storage", "💾", "Synchronous key/value storage scoped to the origin or the tab."),
            new("Cookie", "cookie", "🍪", "Read, set and remove document cookies with full options."),
            new("CookieStore", "cookie-store", "🗄️", "The async Cookie Store API with change events.", ApiSupport.Chromium, ApiNeeds.SecureContext),
            new("IndexedDb", "indexed-db", "🧮", "Structured, transactional client-side database."),
            new("CacheStorage", "cache-storage", "📦", "The service-worker Cache API: store and match requests.", ApiSupport.Broad, ApiNeeds.SecureContext),
            new("StorageManager", "storage-manager", "🧰", "Storage quota, usage estimates and persistence.", ApiSupport.Broad, ApiNeeds.SecureContext),
        ]),
        new("Files & Data",
        [
            new("FileReader", "file-reader", "📁", "Read user-selected files as text, data URLs or bytes."),
            new("ObjectUrls", "object-urls", "🔗", "Create and revoke blob: object URLs from C# data."),
            new("Clipboard", "clipboard", "📋", "Read and write text and typed items on the system clipboard.", ApiSupport.Partial, ApiNeeds.SecureContext | ApiNeeds.Permission | ApiNeeds.UserGesture),
            new("Crypto", "crypto", "🔐", "SubtleCrypto encryption, decryption, hashing and random values.", ApiSupport.Broad, ApiNeeds.SecureContext),
            new("Fetch", "fetch", "🌐", "The browser fetch API with full request/response control."),
        ]),
        new("Network & Workers",
        [
            new("ServiceWorker", "service-worker", "⚙️", "Register and inspect service workers.", ApiSupport.Broad, ApiNeeds.SecureContext),
            new("BackgroundSync", "background-sync", "🔁", "Defer work until the user has connectivity.", ApiSupport.Chromium, ApiNeeds.SecureContext),
            new("Push", "push", "📨", "Subscribe to web push notifications.", ApiSupport.Partial, ApiNeeds.SecureContext | ApiNeeds.Permission),
            new("NetworkInformation", "network-information", "📶", "Connection type, speed, save-data and change events.", ApiSupport.Chromium),
            new("BroadcastChannel", "broadcast-channel", "📡", "Message other tabs and windows of the same origin."),
            new("WebLocks", "web-locks", "🔒", "Cross-tab cooperative resource locking.", ApiSupport.Broad, ApiNeeds.SecureContext),
        ]),
        new("Device & Hardware",
        [
            new("Battery", "battery", "🔋", "Battery level, charging state and related events.", ApiSupport.Chromium, ApiNeeds.SecureContext),
            new("Geolocation", "geolocation", "🛰️", "Current position, watch positions and errors.", ApiSupport.Broad, ApiNeeds.SecureContext | ApiNeeds.Permission),
            new("MediaDevices", "media-devices", "🎥", "Enumerate cameras/microphones and query capabilities.", ApiSupport.Broad, ApiNeeds.SecureContext | ApiNeeds.Permission),
            new("Nfc", "nfc", "🏷️", "Read and write NDEF messages on NFC tags.", ApiSupport.ChromiumMobile, ApiNeeds.SecureContext | ApiNeeds.Permission | ApiNeeds.UserGesture | ApiNeeds.Experimental),
            new("WakeLock", "wake-lock", "☀️", "Keep the screen awake while your app needs it.", ApiSupport.Partial, ApiNeeds.SecureContext),
            new("IdleDetector", "idle-detector", "💤", "Detect user and screen idle state changes.", ApiSupport.Chromium, ApiNeeds.SecureContext | ApiNeeds.Permission | ApiNeeds.UserGesture | ApiNeeds.Experimental),
            new("ContactPicker", "contact-picker", "👥", "Let users pick contacts to share with your app.", ApiSupport.ChromiumMobile, ApiNeeds.SecureContext | ApiNeeds.UserGesture | ApiNeeds.Experimental),
            new("EyeDropper", "eye-dropper", "🎨", "Sample any pixel color on the screen.", ApiSupport.ChromiumDesktop, ApiNeeds.UserGesture | ApiNeeds.Experimental),
        ]),
        new("Identity & Permissions",
        [
            new("WebAuthn", "web-authn", "🔑", "Passkeys: create credentials and verify assertions.", ApiSupport.Broad, ApiNeeds.SecureContext | ApiNeeds.UserGesture),
            new("Permissions", "permissions", "✅", "Query the state of any browser permission.", ApiSupport.Partial),
            new("Notification", "notification", "🔔", "Request permission and show system notifications.", ApiSupport.Broad, ApiNeeds.SecureContext | ApiNeeds.Permission | ApiNeeds.UserGesture),
        ]),
        new("Media & Speech",
        [
            new("SpeechSynthesis", "speech-synthesis", "🗣️", "Text-to-speech with voices, pitch and rate.", ApiSupport.Broad, ApiNeeds.UserGesture),
            new("SpeechRecognition", "speech-recognition", "🎙️", "Speech-to-text with interim results and events.", ApiSupport.Partial, ApiNeeds.SecureContext | ApiNeeds.Permission),
            new("WebAudio", "web-audio", "🎵", "Play and control audio buffers with the Web Audio API.", ApiSupport.Broad, ApiNeeds.UserGesture),
        ]),
    ];

    public static IEnumerable<DocLink> AllLinks => Groups.SelectMany(g => g.Links);

    /// <summary>Every documented browser API - the guide pages in "Overview" are not APIs.</summary>
    public static IEnumerable<DocLink> ApiLinks => AllLinks.Where(l => l.Support != ApiSupport.Guide);

    /// <summary>
    /// The reading order the prev/next pager walks: the groups in the order they are declared,
    /// each group's links in the order they are declared.
    /// </summary>
    public static readonly DocLink[] ReadingOrder = [.. AllLinks];

    /// <summary>
    /// The links either side of <paramref name="url"/> in <see cref="ReadingOrder"/>, or nulls at
    /// the ends. Unknown urls - the home page, the error page - get a pair of nulls.
    /// </summary>
    public static (DocLink? Previous, DocLink? Next) Neighbours(string url)
    {
        var index = Array.FindIndex(ReadingOrder, l => string.Equals(l.Url, url, StringComparison.OrdinalIgnoreCase));
        if (index < 0) return (null, null);

        return (index > 0 ? ReadingOrder[index - 1] : null,
                index < ReadingOrder.Length - 1 ? ReadingOrder[index + 1] : null);
    }

    public static string Label(this ApiSupport support) => support switch
    {
        ApiSupport.Broad => "All engines",
        ApiSupport.Partial => "Varies by engine",
        ApiSupport.Chromium => "Chromium only",
        ApiSupport.ChromiumDesktop => "Chromium desktop",
        ApiSupport.ChromiumMobile => "Chromium on Android",
        _ => "Guide",
    };

    public static string CssClass(this ApiSupport support) => support switch
    {
        ApiSupport.Broad => "broad",
        ApiSupport.Partial => "partial",
        _ => "narrow",
    };
}
