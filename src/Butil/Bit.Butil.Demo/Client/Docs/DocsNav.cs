using Microsoft.AspNetCore.Components;
using Bit.Butil.Demo.Client.Pages;

namespace Bit.Butil.Demo.Client.Docs;

/// <summary>
/// The single source of truth for the site taxonomy: the sidebar, the home page feature grid,
/// the browser-support matrix and the prev/next pager are all rendered from this list.
/// </summary>
public static class DocsNav
{
    public static readonly DocGroup[] Groups =
    [
        new("Overview", "overview",
        [
            new("Getting started", "getting-started", "Install the package, add the script, register the services.", typeof(GettingStartedPage), ApiSupport.Guide),
            new("Render modes", "render-modes", "How Butil behaves under WebAssembly, Server, Hybrid and prerendering.", typeof(RenderModesPage), ApiSupport.Guide),
            new("JavaScript trimming", "javascript-trimming", "Ship only the JavaScript your app calls: the switch, the three signals behind it, and what none of them can see.", typeof(JavaScriptTrimmingPage), ApiSupport.Guide),
            new("Browser support", "browser-support", "Which APIs need a secure context, a permission or a specific engine.", typeof(BrowserSupportPage), ApiSupport.Guide),
            new("Troubleshooting", "troubleshooting", "The errors people hit first, and what each one actually means.", typeof(TroubleshootingPage), ApiSupport.Guide),
            new("MCP server", "mcp-server", "This site as tools an AI agent can call - and a live client to try them in.", typeof(McpServerPage), ApiSupport.Guide),
        ]),
        new("Window & Browsing", "window",
        [
            new("Window", "window", "The DOM window object: events, dialogs, sizes and more.", typeof(WindowPage)),
            new("Document", "document", "The DOM document object: title, cookies, fullscreen, design mode.", typeof(DocumentPage), ApiSupport.Broad, ApiNeeds.UserGesture),
            new("History", "history", "Session history: navigate back/forward and push/replace states.", typeof(HistoryPage)),
            new("Navigation", "navigation", "The modern successor to History - read the entry list, and finally know whether you can go back.", typeof(NavigationPage)),
            new("Location", "location", "Read and mutate the current URL, reload or navigate.", typeof(LocationPage)),
            new("Navigator", "navigator", "Browser identity, languages, share, vibrate, badges and more.", typeof(NavigatorPage), ApiSupport.Partial, ApiNeeds.SecureContext | ApiNeeds.UserGesture),
            new("UserAgent", "user-agent", "Parsed user-agent brands, platform and mobile-ness.", typeof(UserAgentPage), ApiSupport.Partial),
        ]),
        new("Screen & Diagnostics", "diagnostics",
        [
            new("Screen", "screen", "Physical screen metrics, color depth and availability.", typeof(ScreenPage), ApiSupport.Partial),
            new("WindowManagement", "window-management", "Every attached screen, and placing windows or fullscreen content on a chosen one.", typeof(WindowManagementPage), ApiSupport.ChromiumDesktop, ApiNeeds.SecureContext | ApiNeeds.Permission | ApiNeeds.UserGesture),
            new("ScreenOrientation", "screen-orientation", "Read, lock and observe the screen orientation.", typeof(ScreenOrientationPage), ApiSupport.Partial),
            new("VisualViewport", "visual-viewport", "The visual viewport: scale, offsets and resize events.", typeof(VisualViewportPage)),
            new("Performance", "performance", "High-resolution timing, marks, measures and entries.", typeof(PerformancePage), ApiSupport.Partial),
            new("Reporting", "reporting", "Observe deprecation, intervention and crash reports.", typeof(ReportingPage), ApiSupport.Chromium),
            new("Console", "console", "The full browser console API from C#: log, table, group, time.", typeof(ConsolePage)),
        ]),
        new("DOM & Interaction", "dom",
        [
            new("Element", "element", "Attributes, ARIA, classes, content, scrolling, fullscreen, pointer capture and events on any ElementReference.", typeof(ElementPage), ApiSupport.Broad, ApiNeeds.None,
                ["ElementReferenceExtensions", "ElementReferenceDomExtensions", "ElementReferenceStateExtensions", "ElementReferenceAriaExtensions", "ElementReferenceEventExtensions", "ElementReferenceMediaExtensions"]),
            new("Animation", "animation", "Run and control Web Animations on any element, straight from C#.", typeof(AnimationPage), ApiSupport.Broad, ApiNeeds.None,
                ["ElementReferenceAnimationExtensions", "AnimationHandle"]),
            new("Css", "css", "Computed styles, CSS.supports, stylesheets you can edit, and highlights painted over text without touching the DOM.", typeof(CssPage), ApiSupport.Partial, ApiNeeds.None,
                ["Css", "StyleSheetHandle"]),
            new("Dom", "dom", "Find and create elements Blazor did not render, walk the tree, and bridge back to ElementReference.", typeof(DomPage), ApiSupport.Broad, ApiNeeds.None,
                ["Dom", "DomHandle"]),
            new("ShadowDom", "shadow-dom", "Attach a shadow root, style it in isolation, and read into an open one - your own, or any that was not closed.", typeof(ShadowDomPage), ApiSupport.Broad, ApiNeeds.None,
                ["ShadowDom", "ShadowRootHandle"]),
            new("Canvas", "canvas", "Draw a video frame or an image onto a canvas, then export it as a data URL or a byte[].", typeof(CanvasPage), ApiSupport.Broad, ApiNeeds.None,
                ["Canvas", "CanvasSize", "CanvasDrawOptions"]),
            new("PictureInPicture", "picture-in-picture", "Float a video in an always-on-top window outside the page.", typeof(PictureInPicturePage), ApiSupport.Broad, ApiNeeds.UserGesture),
            new("ViewTransition", "view-transition", "Let the browser animate between two states of the page.", typeof(ViewTransitionPage), ApiSupport.Partial),
            new("Keyboard", "keyboard", "App-wide keyboard shortcuts with modifier support.", typeof(KeyboardPage)),
            new("IntersectionObserver", "intersection-observer", "Observe element visibility inside the viewport or a scroll container.", typeof(IntersectionObserverPage), ApiSupport.Broad, ApiNeeds.None,
                ["IntersectionObserverExtensions", "IntersectionObserverOptions"]),
            new("MutationObserver", "mutation-observer", "Observe DOM tree, attribute and character-data mutations.", typeof(MutationObserverPage), ApiSupport.Broad, ApiNeeds.None,
                ["MutationObserverExtensions", "MutationObserverOptions"]),
            new("ResizeObserver", "resize-observer", "Observe element size changes with box-model detail.", typeof(ResizeObserverPage), ApiSupport.Broad, ApiNeeds.None,
                ["ResizeObserverExtensions"]),
        ]),
        new("Storage", "storage",
        [
            new("Local & Session Storage", "storage", "Synchronous key/value storage scoped to the origin or the tab.", typeof(StoragePage), ApiSupport.Broad, ApiNeeds.None,
                ["LocalStorage", "SessionStorage"]),
            new("Cookie", "cookie", "Read, set and remove document cookies with full options.", typeof(CookiePage)),
            new("CookieStore", "cookie-store", "The async Cookie Store API with change events.", typeof(CookieStorePage), ApiSupport.Chromium, ApiNeeds.SecureContext),
            new("IndexedDb", "indexed-db", "Structured, transactional client-side database.", typeof(IndexedDbPage)),
            new("CacheStorage", "cache-storage", "The service-worker Cache API: store and match requests.", typeof(CacheStoragePage), ApiSupport.Broad, ApiNeeds.SecureContext),
            new("StorageManager", "storage-manager", "Storage quota, usage estimates and persistence.", typeof(StorageManagerPage), ApiSupport.Broad, ApiNeeds.SecureContext),
            new("StorageAccess", "storage-access", "Ask for unpartitioned storage from inside a third-party iframe.", typeof(StorageAccessPage), ApiSupport.Broad, ApiNeeds.UserGesture),
        ]),
        new("Files & Data", "files",
        [
            new("FileReader", "file-reader", "Read user-selected files as text, data URLs or bytes.", typeof(FileReaderPage)),
            new("ObjectUrls", "object-urls", "Create and revoke blob: object URLs from C# data.", typeof(ObjectUrlsPage)),
            new("Clipboard", "clipboard", "Read and write text and typed items on the system clipboard.", typeof(ClipboardPage), ApiSupport.Partial, ApiNeeds.SecureContext | ApiNeeds.Permission | ApiNeeds.UserGesture),
            new("Crypto", "crypto", "SubtleCrypto encryption, decryption, hashing and random values.", typeof(CryptoPage), ApiSupport.Broad, ApiNeeds.SecureContext),
            new("Fetch", "fetch", "The browser fetch API with full request/response control.", typeof(FetchPage)),
            new("FileSystem", "file-system", "Pick real files and folders, then read and write them back.", typeof(FileSystemPage), ApiSupport.Chromium, ApiNeeds.UserGesture),
            new("DataTransfer", "data-transfer", "What a drag is carrying: dropped files, text and links, the effect and the drag image.", typeof(DataTransferPage), ApiSupport.Broad, ApiNeeds.None,
                ["DataTransfer", "DropPayload", "DroppedFile"]),
            new("Compression", "compression", "Gzip and deflate through the browser's native codec.", typeof(CompressionPage), ApiSupport.Broad),
            new("LocalFonts", "local-fonts", "List the fonts installed on the machine, and read one's raw font file.", typeof(LocalFontsPage), ApiSupport.ChromiumDesktop, ApiNeeds.SecureContext | ApiNeeds.Permission | ApiNeeds.UserGesture),
        ]),
        new("Network & Workers", "network",
        [
            new("ServiceWorker", "service-worker", "Register and inspect service workers.", typeof(ServiceWorkerPage), ApiSupport.Broad, ApiNeeds.SecureContext),
            new("BackgroundSync", "background-sync", "Defer work until the user has connectivity.", typeof(BackgroundSyncPage), ApiSupport.Chromium, ApiNeeds.SecureContext),
            new("Push", "push", "Subscribe to web push notifications.", typeof(PushPage), ApiSupport.Partial, ApiNeeds.SecureContext | ApiNeeds.Permission),
            new("NetworkInformation", "network-information", "Connection type, speed, save-data and change events.", typeof(NetworkInformationPage), ApiSupport.Chromium),
            new("BroadcastChannel", "broadcast-channel", "Message other tabs and windows of the same origin.", typeof(BroadcastChannelPage)),
            new("WebLocks", "web-locks", "Cross-tab cooperative resource locking.", typeof(WebLocksPage), ApiSupport.Broad, ApiNeeds.SecureContext),
            new("EventSource", "event-source", "Server-sent events, with reconnection built into the browser.", typeof(EventSourcePage)),
            new("Worker", "worker", "Dedicated and shared workers: JavaScript on a thread of its own, so long work stops freezing the page.", typeof(WorkerPage), ApiSupport.Partial, ApiNeeds.None,
                ["Worker", "WorkerHandle", "SharedWorkerHandle", "WorkerOptions", "WorkerError"]),
            new("WindowMessaging", "window-messaging", "window.postMessage: talk to an embedded iframe, the page that embedded you, or a window you opened.", typeof(WindowMessagingPage), ApiSupport.Broad, ApiNeeds.None,
                ["WindowMessaging", "WindowMessageTarget", "WindowMessage"]),
            new("MessageChannel", "message-channel", "A two-ended pipe: hand one end to a worker or an iframe and talk privately.", typeof(MessageChannelPage), ApiSupport.Broad, ApiNeeds.None,
                ["MessageChannel", "MessageChannelHandle", "MessagePortHandle", "ButilMessage"]),
            new("WebRtc", "web-rtc", "Media and data straight between two browsers: peer connections, data channels and stats.", typeof(WebRtcPage), ApiSupport.Broad, ApiNeeds.SecureContext,
                ["WebRtc", "PeerConnectionHandle", "RtcDataChannelHandle", "RtcIceServer", "RtcSessionDescription", "RtcStat"]),
            new("WebSocket", "web-socket", "A two-way connection that stays open - binary frames, close codes, sub-protocols and back-pressure.", typeof(WebSocketPage), ApiSupport.Broad, ApiNeeds.None,
                ["WebSocket", "WebSocketHandle", "WebSocketMessage", "WebSocketClose"]),
        ]),
        new("Async & Scheduling", "async",
        [
            new("AbortController", "abort-controller", "One cancellation signal, shared by as many operations as you like - plus deadlines and composition.", typeof(AbortControllerPage), ApiSupport.Broad, ApiNeeds.None,
                ["AbortController", "AbortControllerHandle", "ButilAbortSignal"]),
            new("Scheduler", "scheduler", "Animation frames, idle callbacks, prioritized tasks, and yielding so a long loop stops freezing the page.", typeof(SchedulerPage), ApiSupport.Partial, ApiNeeds.None,
                ["Scheduler", "IdleDeadline", "SchedulerPriority"]),
            new("Streams", "streams", "Read a response as it arrives, split it, pipe it through the browser's codec, and take the bytes in C#.", typeof(StreamsPage), ApiSupport.Partial, ApiNeeds.None,
                ["Streams", "ReadableStreamHandle", "WritableStreamHandle", "TransformStreamHandle", "StreamedResponse", "StreamChunk"]),
        ]),
        new("Device & Hardware", "device",
        [
            new("Battery", "battery", "Battery level, charging state and related events.", typeof(BatteryPage), ApiSupport.Chromium, ApiNeeds.SecureContext),
            new("Geolocation", "geolocation", "Current position, watch positions and errors.", typeof(GeolocationPage), ApiSupport.Broad, ApiNeeds.SecureContext | ApiNeeds.Permission),
            new("MediaDevices", "media-devices", "Enumerate cameras/microphones, capture the screen, and open streams.", typeof(MediaDevicesPage), ApiSupport.Broad, ApiNeeds.SecureContext | ApiNeeds.Permission | ApiNeeds.UserGesture),
            new("Gamepad", "gamepad", "Read game controllers - buttons, sticks and rumble.", typeof(GamepadPage), ApiSupport.Broad),
            new("DeviceOrientation", "device-orientation", "Tilt, acceleration and rotation from the device's own sensors.", typeof(DeviceOrientationPage), ApiSupport.Partial, ApiNeeds.SecureContext | ApiNeeds.Permission | ApiNeeds.UserGesture),
            new("Nfc", "nfc", "Read and write NDEF messages on NFC tags.", typeof(NfcPage), ApiSupport.ChromiumMobile, ApiNeeds.SecureContext | ApiNeeds.Permission | ApiNeeds.UserGesture | ApiNeeds.Experimental),
            new("Sensors", "sensors", "The Generic Sensor API: accelerometer, gyroscope, magnetometer, orientation, gravity, linear acceleration and ambient light.", typeof(SensorsPage), ApiSupport.Chromium, ApiNeeds.SecureContext | ApiNeeds.Permission),
            new("Bluetooth", "bluetooth", "Pick a Bluetooth LE device and read, write or subscribe to its GATT characteristics.", typeof(BluetoothPage), ApiSupport.Chromium, ApiNeeds.SecureContext | ApiNeeds.Permission | ApiNeeds.UserGesture,
                ["Bluetooth", "BluetoothDevice"]),
            new("Usb", "usb", "Pick a USB device, claim an interface and run control, bulk or interrupt transfers.", typeof(UsbPage), ApiSupport.Chromium, ApiNeeds.SecureContext | ApiNeeds.Permission | ApiNeeds.UserGesture,
                ["Usb", "UsbDevice"]),
            new("Serial", "serial", "Open a serial port with the device's line settings, then read and write bytes.", typeof(SerialPage), ApiSupport.Chromium, ApiNeeds.SecureContext | ApiNeeds.Permission | ApiNeeds.UserGesture,
                ["Serial", "SerialPort"]),
            new("Hid", "hid", "Exchange input, output and feature reports with a human-interface device.", typeof(HidPage), ApiSupport.Chromium, ApiNeeds.SecureContext | ApiNeeds.Permission | ApiNeeds.UserGesture,
                ["Hid", "HidDevice"]),
            new("Midi", "midi", "MIDI inputs and outputs: listen to a controller, send notes to a synth.", typeof(MidiPage), ApiSupport.Chromium, ApiNeeds.SecureContext | ApiNeeds.Permission),
            new("ComputePressure", "compute-pressure", "Shed work before the machine stutters, by watching CPU and thermal pressure.", typeof(ComputePressurePage), ApiSupport.Chromium, ApiNeeds.SecureContext | ApiNeeds.Experimental),
            new("DevicePosture", "device-posture", "Whether a foldable device is currently flat or folded across its hinge.", typeof(DevicePosturePage), ApiSupport.Chromium, ApiNeeds.SecureContext | ApiNeeds.Experimental),
            new("WakeLock", "wake-lock", "Keep the screen awake while your app needs it.", typeof(WakeLockPage), ApiSupport.Partial, ApiNeeds.SecureContext),
            new("IdleDetector", "idle-detector", "Detect user and screen idle state changes.", typeof(IdleDetectorPage), ApiSupport.Chromium, ApiNeeds.SecureContext | ApiNeeds.Permission | ApiNeeds.UserGesture | ApiNeeds.Experimental),
            new("ContactPicker", "contact-picker", "Let users pick contacts to share with your app.", typeof(ContactPickerPage), ApiSupport.ChromiumMobile, ApiNeeds.SecureContext | ApiNeeds.UserGesture | ApiNeeds.Experimental),
            new("EyeDropper", "eye-dropper", "Sample any pixel color on the screen.", typeof(EyeDropperPage), ApiSupport.ChromiumDesktop, ApiNeeds.UserGesture | ApiNeeds.Experimental),
            new("BarcodeDetector", "barcode-detector", "Find QR codes and barcodes in a camera frame or an image.", typeof(BarcodeDetectorPage), ApiSupport.Chromium, ApiNeeds.Experimental),
        ]),
        new("Identity & Permissions", "identity",
        [
            new("WebAuthn", "web-authn", "Passkeys: create credentials and verify assertions.", typeof(WebAuthnPage), ApiSupport.Broad, ApiNeeds.SecureContext | ApiNeeds.UserGesture),
            new("Permissions", "permissions", "Query the state of any browser permission.", typeof(PermissionsPage), ApiSupport.Partial),
            new("Notification", "notification", "Request permission and show system notifications.", typeof(NotificationPage), ApiSupport.Broad, ApiNeeds.SecureContext | ApiNeeds.Permission | ApiNeeds.UserGesture),
        ]),
        new("Media & Speech", "media",
        [
            new("SpeechSynthesis", "speech-synthesis", "Text-to-speech with voices, pitch and rate.", typeof(SpeechSynthesisPage), ApiSupport.Broad, ApiNeeds.UserGesture),
            new("SpeechRecognition", "speech-recognition", "Speech-to-text with interim results and events.", typeof(SpeechRecognitionPage), ApiSupport.Partial, ApiNeeds.SecureContext | ApiNeeds.Permission),
            new("WebAudio", "web-audio", "Play and control audio buffers with the Web Audio API.", typeof(WebAudioPage), ApiSupport.Broad, ApiNeeds.UserGesture),
            new("MediaRecorder", "media-recorder", "Record a camera, microphone or screen share to a file.", typeof(MediaRecorderPage), ApiSupport.Broad, ApiNeeds.SecureContext | ApiNeeds.Permission),
            new("MediaSession", "media-session", "Lock-screen metadata and hardware media-key handlers.", typeof(MediaSessionPage), ApiSupport.Partial),
            new("AudioOutput", "audio-output", "Route a media element's sound to a chosen speaker or headset.", typeof(AudioOutputPage), ApiSupport.Partial, ApiNeeds.SecureContext | ApiNeeds.UserGesture),
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
    /// The current location as it is written in <see cref="DocLink.Url"/>: base-relative, without
    /// the query or the fragment, and without surrounding slashes. The home page is the empty
    /// string. Everything that matches a route against this list has to normalize the same way.
    /// </summary>
    public static string RouteKey(this NavigationManager navManager) =>
        navManager.ToBaseRelativePath(navManager.Uri).Split('?', '#')[0].Trim('/');

    /// <summary>Finds the entry documenting <paramref name="url"/> ("clipboard"), or null.</summary>
    public static DocLink? FindByUrl(string? url)
    {
        var trimmed = (url ?? string.Empty).Trim('/');

        return AllLinks.FirstOrDefault(l => string.Equals(l.Url, trimmed, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// The Bit.Butil public types a page documents. A guide page documents none; every other page
    /// either states them explicitly or is named after the single type it wraps.
    /// </summary>
    public static string[] TypeNames(this DocLink link) => link switch
    {
        { Support: ApiSupport.Guide } => [],
        { Services: { Length: > 0 } services } => services,
        _ => [link.Title.Replace(" ", string.Empty, StringComparison.Ordinal)]
    };

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

    /// <summary>
    /// The preconditions spelled out, one sentence each - what a caller has to arrange before the
    /// API will do anything. An API with none returns an empty array.
    /// </summary>
    public static string[] Labels(this ApiNeeds needs)
    {
        var labels = new List<string>(4);

        if (needs.HasFlag(ApiNeeds.SecureContext)) labels.Add("Secure context: only available over HTTPS or on localhost.");
        if (needs.HasFlag(ApiNeeds.Permission)) labels.Add("Permission: the browser prompts the user, and a denial fails the call.");
        if (needs.HasFlag(ApiNeeds.UserGesture)) labels.Add("User gesture: must be called from a click or another user-initiated handler.");
        if (needs.HasFlag(ApiNeeds.Experimental)) labels.Add("Experimental: behind a flag or an origin trial in at least one shipping engine.");

        return [.. labels];
    }

    /// <summary>
    /// The chip class for a support level. A guide gets its own rather than falling into "narrow":
    /// the narrow class is the one that colours a chip as a warning, and "this page is prose" is
    /// not a warning about anything.
    /// </summary>
    public static string CssClass(this ApiSupport support) => support switch
    {
        ApiSupport.Guide => "guide",
        ApiSupport.Broad => "broad",
        ApiSupport.Partial => "partial",
        _ => "narrow",
    };
}
