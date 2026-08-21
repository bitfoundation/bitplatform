# bit Butil

**The browser platform, in C#.** Butil wraps the Web APIs a Blazor app actually needs - the DOM,
storage, media, sensors, crypto, workers - as injectable, strongly-typed, XML-documented services,
so you can stop writing `IJSRuntime.InvokeVoidAsync("someGlobal.someFunction", ...)` and start
writing C#.

Works on Blazor WebAssembly, Blazor Server, Blazor Hybrid and under prerendering, on .NET 8, 9
and 10.

---

## Getting started

Install the package:

```
dotnet add package Bit.Butil
```

Add its script tag to your host page, **before** the Blazor script so `window.BitButil` exists by
the time the app boots:

```html
<script src="_content/Bit.Butil/bit-butil.js"></script>
<script src="_framework/blazor.web.js"></script>
```

Register the services:

```csharp
using Bit.Butil;

builder.Services.AddBitButilServices();
```

Then inject whatever you need:

```razor
@inject Bit.Butil.Window window
@inject Bit.Butil.LocalStorage localStorage
@inject Bit.Butil.Clipboard clipboard

@code {
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender is false) return;

        await localStorage.SetItem("last-visit", DateTime.UtcNow.ToString("O"));
        await clipboard.WriteText("copied from C#");
        await window.AddEventListener(ButilEvents.KeyDown, args => { /* ... */ });
    }
}
```

---

## What's in the box

Every wrapper below is an injectable service in the `Bit.Butil` namespace. They are registered as
**scoped**, which matches Blazor's one-circuit-or-one-WASM-app-per-user model.

`AddBitButilServices` is trimming-aware: it discovers the services by reflecting over the `Bit.Butil`
assembly for classes marked `[ButilService]` rather than naming them in `AddScoped<T>()` calls. A
hard-coded call is a static reference that roots the class, so listing all of them would force every
published app to carry all of them; reflecting instead lets the trimmer remove the classes your code
never injects, and what it removed is simply not there to register. Injecting a Butil class from code
the trimmer removed - or resolving one purely by reflection - therefore fails at runtime rather than
at build time. Untrimmed apps (Blazor Server, and the prerendering host of a WebAssembly app) keep
registering everything.

### Window & browsing

| Service | What it wraps |
| --- | --- |
| `Window` | The DOM `window`: events, dialogs, sizes, `matchMedia`, scrolling, printing, selection |
| `Document` | The DOM `document`: title, cookies, fullscreen, visibility, design mode, pointer lock |
| `History` | Session history: back/forward, `pushState`/`replaceState`, `popstate` |
| `Navigation` | The Navigation API: read the history entry list, traverse to a key, and know whether you can go back |
| `Location` | Read and mutate the current URL, reload, navigate |
| `Navigator` | Identity, languages, `share`, `vibrate`, badges, `sendBeacon`, device memory |
| `UserAgent` | Parsed user-agent brands, platform and mobile-ness (UA Client Hints) |

### Screen & diagnostics

| Service | What it wraps |
| --- | --- |
| `Screen` | Physical screen metrics, colour depth, availability |
| `ScreenOrientation` | Read, lock and observe the screen orientation |
| `VisualViewport` | The visual viewport: scale, offsets, resize and scroll events |
| `Performance` | High-resolution timing, marks, measures, `PerformanceObserver` |
| `Reporting` | `ReportingObserver`: deprecation, intervention and crash reports |
| `Console` | The full browser console API: log, table, group, time, count, profile |

### DOM & interaction

| Service | What it wraps |
| --- | --- |
| `ElementReference` extensions | Attributes, scrolling, fullscreen, pointer capture, per-element events |
| Animation extensions | The Web Animations API on any element |
| `Keyboard` | App-wide keyboard shortcuts with modifier support |
| `IntersectionObserver` | Element visibility inside the viewport or a scroll container |
| `MutationObserver` | DOM tree, attribute and character-data mutations |
| `ResizeObserver` | Element size changes with box-model detail |
| `PictureInPicture` | Float a `<video>` in an always-on-top window |
| `ViewTransition` | Animate between two states of the page, the browser doing the work |
| Media element extensions | Play, pause, seek, volume and rate on any `<audio>`/`<video>` |

### Storage

| Service | What it wraps |
| --- | --- |
| `LocalStorage` / `SessionStorage` | Synchronous key/value storage, per origin or per tab |
| `Cookie` | Read, set and remove document cookies with full options |
| `CookieStore` | The async Cookie Store API, with change events |
| `IndexedDb` | Structured, transactional client-side database |
| `CacheStorage` | The service-worker Cache API |
| `StorageManager` | Quota, usage estimates and persistence |
| `StorageAccess` | Ask for unpartitioned storage from inside a third-party iframe |

### Files & data

| Service | What it wraps |
| --- | --- |
| `FileReader` | Read user-selected files as text, data URLs or bytes |
| `FileSystem` | The File System Access API: pick real files/folders and write back to them |
| `ObjectUrls` | Create and revoke `blob:` object URLs from C# data |
| `Clipboard` | Read and write text and typed items on the system clipboard |
| `Crypto` | SubtleCrypto: encryption, decryption, hashing, key generation, random values |
| `Fetch` | The fetch API with full request/response control and progress |
| `Compression` | Gzip and deflate through the browser's native codec |

### Network & workers

| Service | What it wraps |
| --- | --- |
| `ServiceWorker` | Register and inspect service workers, and message them |
| `BackgroundSync` | Defer work until the user has connectivity (one-shot and periodic) |
| `Push` | Web push subscriptions |
| `NetworkInformation` | Connection type, speed, save-data and change events |
| `BroadcastChannel` | Message other tabs and windows of the same origin |
| `WebLocks` | Cross-tab cooperative resource locking |
| `EventSource` | Server-sent events, with reconnection built into the browser |

### Device & hardware

| Service | What it wraps |
| --- | --- |
| `Battery` | Battery level, charging state and related events |
| `Geolocation` | Current position, watched positions and errors |
| `MediaDevices` | Cameras and microphones, plus screen capture (`getDisplayMedia`) |
| `Gamepad` | Game controllers: buttons, sticks, triggers and rumble |
| `DeviceOrientation` | Tilt, acceleration and rotation from the device's own sensors |
| `Nfc` | Read and write NDEF messages on NFC tags |
| `WakeLock` | Keep the screen awake, with an auto-reacquiring persistent mode |
| `IdleDetector` | User and screen idle-state changes |
| `ContactPicker` | Let users pick contacts to share with your app |
| `EyeDropper` | Sample any pixel colour on the screen |
| `BarcodeDetector` | Find QR codes and barcodes in a camera frame or an image |

### Identity & permissions

| Service | What it wraps |
| --- | --- |
| `WebAuthn` | Passkeys: create credentials and verify assertions |
| `Permissions` | Query the state of any browser permission |
| `Notification` | Request permission and show system notifications |

### Media & speech

| Service | What it wraps |
| --- | --- |
| `SpeechSynthesis` | Text-to-speech with voices, pitch and rate |
| `SpeechRecognition` | Speech-to-text with interim results and events |
| `WebAudio` | Play and control audio buffers |
| `MediaRecorder` | Record a camera, microphone or screen share to a file |
| `MediaSession` | Lock-screen metadata and hardware media-key handlers |

---

## The patterns worth knowing

### Prerendering is safe by default

During static SSR / prerender there is no JS runtime. Rather than throwing, reads return a **safe
default** - `""` for strings, `[]` for arrays, `default(T)` for everything else - and void calls are
no-ops. That means a read in `OnInitializedAsync` won't crash your prerender pass.

The trade-off: a `false` from `IsSupported()` during prerender is indistinguishable from a genuine
`false`. If you branch on a result, do the read in `OnAfterRenderAsync` instead:

```csharp
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender is false) return;

    if (await wakeLock.IsSupported())
    {
        await wakeLock.Request();
    }
}
```

### Subscriptions are disposable

Anything that attaches a listener returns a `ButilSubscription`. Dispose it to detach:

```csharp
private ButilSubscription? _subscription;

_subscription = await resizeObserver.Observe(_element, entries => { /* ... */ });

// later - idempotent, and safe during teardown:
await _subscription.DisposeAsync();
```

If you forget, the owning service detaches everything it registered when its scope is torn down.
That's a safety net, not a plan.

### Handles own hardware

`MediaStreamHandle`, `MediaRecordingHandle`, `WakeLock`'s persistent handle and the File System
handles all represent something the browser is holding open. Dispose them:

```csharp
await using var stream = await mediaDevices.GetUserMedia(audio: false, video: true);
await stream!.AttachTo(_videoElement);
// the camera light goes out when the handle is disposed
```

### Gestures and secure contexts

Many APIs only work from inside a user-gesture handler (a click), or only over HTTPS. Butil doesn't
hide that - each method's XML docs say which preconditions apply, and calls that the browser
refuses come back as `false`/`null` rather than as exceptions where dismissal is a normal outcome.

### Optional fast invoke

On Blazor WebAssembly, the handful of APIs backed by genuinely synchronous JS functions -
`LocalStorage`, `SessionStorage`, `Cookie`, `Console`, `Location` - can skip the async marshalling:

```csharp
BitButil.UseFastInvoke();
```

Everything wrapping a Promise-returning API keeps running asynchronously regardless, so this can't
break those calls. On Blazor Server it's a no-op.

---

## Trimming and AOT

The package is marked `IsTrimmable`. Types crossing the interop boundary carry
`[DynamicDependency]` annotations, so trimming a published WASM app keeps what the serializer
needs. The public `FastInvoke*` extensions are annotated `[RequiresUnreferencedCode]` so a trimming
consumer gets the warning at their own call site.

---

## Samples and docs

The `Bit.Butil.Demo` project in this repository is a full documentation site: one page per API,
with runnable samples, an API reference table, and a browser-support matrix. Run it to try any of
the above in your own browser.

That same server hosts an **MCP server** at `/mcp`, so an AI agent can work against Butil's real
API instead of guessing at it. Point an MCP client at `https://localhost:5253/mcp`; every tool is
also a plain HTTP GET under `/api/mcp/...` if you just want to look. It offers:

- **Search** across everything at once (`SearchButil`) - this guide, the docs pages, every public
  member, the browser-support matrix and the demo's sources - with the exact follow-up call on each
  hit. The name a task suggests is rarely the name the web platform chose, which is what this is for.
- **The exact API** (`GetButilApiDetails`), reflected out of the shipped assembly: every service,
  every signature, every default argument, with the XML documentation.
- **What an API needs before it works** (`PlanButilFeature`, for one API or the whole set a feature
  uses): the engines that implement it, whether it wants HTTPS, a permission prompt or a user
  gesture, what has to be disposed, and how it behaves under prerendering - the mistakes that
  compile and then do nothing.
- **Setup** per hosting model (`GetButilSetupGuide`), as the real files of a working project.
- **The docs, this guide and the demo's source** as text (`GetButilDocsPage`,
  `GetButilGuideSection`, `GetButilSourceFile`) - each of them returns the list of what it can hand
  out when called with no argument, and the docs index doubles as the browser-support matrix.
- **Resources** (`butil://guide/...`, `butil://api/...`, `butil://docs/...`, `butil://support`) and
  **prompts** for the four common jobs: adding Butil to an app, implementing a feature with it,
  replacing hand-written JS interop, and debugging a call that silently does nothing.

There are seven tools, and no more: a tool description is paid for in every request of every
session, so a listing is not a tool of its own, a single-item lookup is not a tool when one that
takes a set already resolves each member, and nothing restates the server's `instructions`, which
the client has had in context since `initialize`. Start with `SearchButil`. See
[McpController](Bit.Butil.Demo/Server/Controllers/McpController.cs).

The site's own **`/mcp-server` page** is a working client for it: it handshakes with this server on
load, lists its tools, and lets you call one and read both halves of the exchange - the JSON-RPC
request, the response, and the text a model would be handed. It is the fastest way to see what a
tool actually returns before wiring an agent up to it.

Every public member carries XML documentation with a link to the corresponding MDN page, so IntelliSense
is the reference of record.
