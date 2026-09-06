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
the time the app boots (or skip the tag entirely with lazy scripts - see
[Shipping only the JavaScript you use](#shipping-only-the-javascript-you-use)):

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
| `WindowManagement` | Every attached screen, and placing windows or fullscreen content on a chosen one |
| `ScreenOrientation` | Read, lock and observe the screen orientation |
| `VisualViewport` | The visual viewport: scale, offsets, resize and scroll events |
| `Performance` | High-resolution timing, marks, measures, `PerformanceObserver` |
| `Reporting` | `ReportingObserver`: deprecation, intervention and crash reports |
| `Console` | The full browser console API: log, table, group, time, count, profile |

### DOM & interaction

| Service | What it wraps |
| --- | --- |
| `ElementReference` extensions | Attributes (namespaced too), ARIA and `role`, classes, `data-*`, inline style, content insertion, scrolling, layout metrics, fullscreen, popovers, pointer capture, per-element events |
| Animation extensions | The Web Animations API on any element |
| `Keyboard` | App-wide keyboard shortcuts with modifier support |
| `IntersectionObserver` | Element visibility inside the viewport or a scroll container |
| `MutationObserver` | DOM tree, attribute and character-data mutations |
| `ResizeObserver` | Element size changes with box-model detail |
| `Css` | `getComputedStyle`, `CSS.supports`/`escape`/`registerProperty`, stylesheet rules, the CSS Custom Highlight API |
| `Dom` | `querySelector`, `getElementById`, `createElement` and node traversal for elements Blazor did not render - with a bridge back to `ElementReference` |
| `ShadowDom` | `attachShadow`, scoped styles, and querying into any open shadow root - a closed one is closed to you too |
| `Canvas` | `drawImage` from a video/image/canvas, then `toDataURL`/`toBlob` - screenshots and thumbnails as `byte[]` |
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
| `DataTransfer` | Drag-and-drop payloads: dropped files, `getData`/`setData` items, `dropEffect`, `setDragImage` |
| `LocalFonts` | List installed fonts, and read one's raw font file |

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
| `WebRtc` | `RTCPeerConnection`, `RTCDataChannel` and `getStats`: media and data straight between two browsers |
| `WebSocket` | A two-way connection that stays open: binary frames, close codes, sub-protocol negotiation, `bufferedAmount` |
| `Worker` | Dedicated and shared workers running a script you supply, with transferable binary payloads |
| `MessageChannel` | `MessageChannel`/`MessagePort`: a private two-ended pipe, transferable to a worker or an iframe |
| `WindowMessaging` | `window.postMessage`: cross-document messaging with an embedded iframe, the parent, the opener or a popup |

### Async & scheduling

| Service | What it wraps |
| --- | --- |
| `AbortController` | `AbortController`/`AbortSignal`: one signal shared by many operations, plus `AbortSignal.timeout` and `AbortSignal.any` |
| `Scheduler` | `requestAnimationFrame` (single and looping), `requestIdleCallback`, `scheduler.postTask`/`yield`, `isInputPending` |
| `Streams` | The Streams API: a fetch body read as it arrives, `tee`, `pipeThrough` the native codecs, and `pipeTo` a C# sink |

### Device & hardware

| Service | What it wraps |
| --- | --- |
| `Battery` | Battery level, charging state and related events |
| `Geolocation` | Current position, watched positions and errors |
| `MediaDevices` | Cameras and microphones, plus screen capture (`getDisplayMedia`) |
| `Gamepad` | Game controllers: buttons, sticks, triggers and rumble |
| `DeviceOrientation` | Tilt, acceleration and rotation from the device's own sensors |
| `Nfc` | Read and write NDEF messages on NFC tags |
| `Sensors` | The Generic Sensor API: accelerometer, gyroscope, magnetometer, orientation, gravity, linear acceleration, ambient light |
| `Bluetooth` | Web Bluetooth: pick a BLE device, then read, write or subscribe to its GATT characteristics |
| `Usb` | WebUSB: claim an interface and run control, bulk or interrupt transfers |
| `Serial` | Web Serial: open a port with the device's line settings, then read and write bytes |
| `Hid` | WebHID: input, output and feature reports |
| `Midi` | Web MIDI: inputs, outputs, incoming messages and note sending |
| `ComputePressure` | CPU and thermal pressure, for shedding work before the machine stutters |
| `DevicePosture` | Whether a foldable device is flat or folded across its hinge |
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
| `AudioOutput` | Route a media element's sound to a chosen speaker or headset |

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

### Shipping only the JavaScript you use

`bit-butil.js` covers every API in the package. The C# side of an unused API is trimmed away from a
published app (see `AddBitButilServices` above); the JavaScript side can be tree-shaken too. There are two
ways of tree-shaking it, both set in the app's csproj, and both working from the same per-module build of
the scripts (one `Scripts/*.ts` file is one module, `BitButil.clipboard` for `Clipboard` and so on).

**Publish-time bundle trimming - the default, nothing to add.** Keep the script tag. When the app is
published trimmed - a Blazor WebAssembly publish is - the package's build logic reads the trimmed
`Bit.Butil.dll`, finds which `BitButil.<module>.*` identifiers survived (every interop call goes
through such a literal, so the trimmed assembly is the exact list of modules the app can still reach)
and replaces `bit-butil.js` with a bundle assembled from only those modules and their dependencies.
Fingerprints, integrity hashes and compressed variants are computed from the new content. An app
that injects `Clipboard`, `LocalStorage` and `Window` ships about 8 KB of JavaScript instead of the
110 KB bundle. It is on by default only in a Blazor WebAssembly project - a standalone app or PWA - because
that is where the assembly being trimmed is the assembly calling the served JavaScript; a server that hosts
a WebAssembly client keeps its own, full copy of the bundle (use lazy scripts there). The same property
trims the other shape too: wherever the module files are published - a lazy-scripts app, or an app keeping
both shapes - only the modules the trimmed assembly can still name are published, and the rest of the
`modules/` folder is dropped. Nothing can 404 over it: the identifier that would have imported a dropped
module is gone from the assembly with it. `<BitButilIncludeScriptModules>true</BitButilIncludeScriptModules>`
in the csproj publishes every module regardless, for an app that reaches them from outside its own interop
calls. All of this happens in `dotnet publish` only: a build - and `dotnet run` and `dotnet watch` on top of
it - keeps the full bundle and every module, so what you debug is never the trimmed JavaScript. And it
happens in the project that publishes the app's static web assets only - see
[where these properties go](#where-these-properties-go). Opt out with `false`, or opt in elsewhere with
`true`:

```xml
<PropertyGroup>
  <BitButilTrimScripts>false</BitButilTrimScripts>
</PropertyGroup>
```

**Publishing without trimming?** `BitButilTrimScripts` decides *whether* to trim the JavaScript; what it
trims against is a separate question, and a publish with `PublishTrimmed` off has no trimmed assembly to
read. `BitButilScriptScan` answers it from the app's own assemblies instead, and it **defaults to
`TypeReferences` wherever `BitButilTrimScripts` is `true`** - so turning the trimming on is all an app
writes, and the switch is never on with nothing behind it. An `@inject Clipboard` is a reference to
`Bit.Butil.Clipboard`, and the package's build logic reads `Bit.Butil.dll` to know which JavaScript module
answering that class takes - through base classes and internal interop helpers, so `LocalStorage` correctly
pulls in `storage` and `Window` pulls in `events` as well as `window`. On this repository's own trimming
harness it reaches exactly the module set ILLink does. It works in every hosting model - a WebAssembly app
with `PublishTrimmed` off, Blazor Server, a server host that prerenders - and it costs the publish one pass
over the app's assemblies, tens of milliseconds; a build is untouched either way. It reads the app's own
assembly and its copy-local references by default - override with `BitButilScriptScanAssembly` if the code
calling Bit.Butil lives elsewhere, and with `BitButilUntrimmedAssembly` in the rare layout where the
reference to `Bit.Butil.dll` itself cannot be resolved from those.

```xml
<!-- The whole of it for an app published untrimmed: the scan comes with the switch -->
<PropertyGroup>
  <BitButilTrimScripts>true</BitButilTrimScripts>
</PropertyGroup>
```

`TypeNames` is the other mode: it matches the library's type names against the names in each assembly, with
no metadata tables read at all. It is coarser - an app with a class of its own called `Window`, `Console` or
`Storage` pulls in that module too - and it over-includes rather than missing anything, so the default stays
`TypeReferences`. Either is ignored when `PublishTrimmed` is `true`: the trimmed assembly answers the same
question more precisely. `None` is the third value, and the way to publish the full bundle from one project
while `BitButilTrimScripts` stays `true` for the rest.

**Keeping a module none of that can see.** `BitButilScriptModule` names modules, or the Bit.Butil classes
behind them, that must survive whatever the scan or the trimmer concluded - for an API reached by reflection,
or from your own JavaScript. It is always *added* to what they found, never used instead of it, and a name
that is neither a module nor a Bit.Butil class fails the build rather than being quietly ignored:

```xml
<ItemGroup>
  <BitButilScriptModule Include="Clipboard;geolocation" />
</ItemGroup>
```

With none of the three in play - no `PublishTrimmed`, `BitButilScriptScan` set to `None`, no
`BitButilScriptModule` - there is nothing to trim against, and the full bundle is published.

<a id="where-these-properties-go"></a>
**Where these properties go.** On the project you publish - the Blazor WebAssembly head, or the server
project of a Blazor Web App - and **not** in a shared `Directory.Build.props`. From there they also reach
every Razor class library and every MAUI/Blazor Hybrid head in the solution, and none of those publish the
app's static web assets: a class library asked what JavaScript "the app" uses would answer from its own
references, which are not the app's, and hand the head a bundle short of the modules only the head names.

The trimming knows this and stands down in those projects rather than trimming against the wrong reference
closure - what it decides on is the SDK the project loaded, which is the Web SDK or the Blazor WebAssembly
SDK for every project that publishes an app and neither of those for a class library or a hybrid head.
Where what reached such a project describes what the app calls - a `BitButilScriptScan` written out by hand, or a
`BitButilScriptModule` list - it says so in the build output. So a shared props file is no longer a broken
build, but it is still not where the answer comes from. Put the
properties on the head, one per app you publish:

```xml
<!-- Boilerplate.Server.Web.csproj - the project that publishes the Blazor Web App -->
<PropertyGroup>
  <BitButilTrimScripts>true</BitButilTrimScripts>
</PropertyGroup>

<ItemGroup>
  <BitButilScriptModule Include="WebAuthn" />
</ItemGroup>
```

`BitButilTrimScripts` on its own is the exception worth keeping shared, since it is only a switch and
already defaults to `true` exactly where it applies. The scan comes with it wherever it lands, but only the
project that publishes the app ever acts on one, so sharing the switch is still just sharing a switch.

The demo site has a page of its own for all of this - **JavaScript trimming**, under Overview - including a
live check that reads back which modules the app you are looking at actually downloaded.

**Lazy scripts.** No script tag at all: the first call into an API `import()`s that API's module
(`_content/Bit.Butil/modules/clipboard.js` for `Clipboard`), so only the JavaScript for the APIs the
app actually calls is ever downloaded - in every hosting model, trimmed or not. Each module file is
self-contained and safe to load more than once. Set the property in every project that uses Butil
(a Blazor Web App's server and client both) and drop the script tag from the host page:

```xml
<PropertyGroup>
  <BitButilLazyScripts>true</BitButilLazyScripts>
</PropertyGroup>
```

The property also drops the bundle from the published output (and, in the default mode, the module
files) and turns the mode on at runtime - through the `Bit.Butil.LazyScripts` runtime switch and,
since runtime configuration does not reach a .NET 8 WebAssembly app, a one-line module initializer
compiled into the project that calls `BitButil.UseLazyScripts()`. `BitButilIncludeScriptBundle` and
`BitButilIncludeScriptModules` override which shape ends up in the published output when you want both. In a
project that also trims (a WebAssembly publish does by default) the modules published are the ones the
trimmed assembly can still import; naming `BitButilIncludeScriptModules` in the csproj publishes all of them.

Prefer to keep it in C#? The registration call takes the same switches:

```csharp
builder.Services.AddBitButilServices(options =>
{
    options.LazyScripts = true;                  // or false to insist on the bundle
    options.ScriptModulesPath = "/cdn/butil/";   // optional, when the modules are served elsewhere
    options.FastInvoke = true;                   // optional, same as BitButil.UseFastInvoke()
});
```

That is equivalent to `BitButil.UseLazyScripts()` / `BitButil.UseBundledScripts()` (process-wide, last
call wins) and needs no script tag either. What it cannot do is change the published output - which is why
`BitButilLazyScripts` is the better switch wherever the csproj is an option. The default (bundle mode)
publishes the bundle and drops the module files, so the C# switch on its own leaves the `import()`s with
nothing to fetch; keep them with

```xml
<PropertyGroup>
  <BitButilIncludeScriptModules>true</BitButilIncludeScriptModules>
</PropertyGroup>
```

and the bundle, still published, is simply never loaded (harmless - but a PWA precaches it). Publish-time
bundle trimming has no runtime counterpart at all, since it happens inside `dotnet publish`.

Trade-offs: lazy scripts cost one extra request the first time each module is used, and the modules'
first call is necessarily asynchronous (every Butil API already is). The trimmed bundle is one request
and needs no thought, but only in the project that publishes trimmed.

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
