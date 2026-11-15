# Bit.Bcrdt.Butil

Browser lifecycle integration for [bit Bcrdt](https://www.nuget.org/packages/Bit.Bcrdt) in Blazor WebAssembly, through
[bit Butil](https://www.nuget.org/packages/Bit.Butil). The browser knows things a .NET process in a tab cannot see for
itself - that the network is back, that the page is going away - and this package acts on them for every open document:

| The browser reports | What happens |
|---|---|
| `online`; the page visible again; a frozen page resumed | Documents waiting out a reconnection backoff reconnect at once, instead of up to 30 seconds later. |
| `offline` | `IsOnline` turns false and `OnlineChanged` fires - the moment to tell the user their edits are kept for later. |
| The page hidden, unloaded or frozen | Every open document's changes are waited for until they reach its store, then `AfterFlush` runs. |
| `pagehide` / `pageshow` | Presence is withdrawn while the page is unloaded or in the back/forward cache, and restored when it is shown, so a closed tab leaves the participant list straight away rather than after the presence timeout. |
| Start-up, if asked | `navigator.storage.persist()`, so the browser does not evict documents kept for offline use. |

```bash
dotnet add package Bit.Bcrdt.Butil
dotnet add package Bit.Butil          # referenced directly, so its script and build logic reach the app
```

```csharp
using Bit.Bcrdt.Butil;

builder.Services.AddBitBcrdtServices(collaboration => collaboration
    .UseTransport(new BcrdtSignalRTransportFactory(hubUrl))
    .UseIndexedDbStore()
    .UseBrowserLifecycle(options => options.RequestPersistentStorage = true));

WebAssemblyHost host = builder.Build();
await host.Services.GetRequiredService<BcrdtBrowserLifecycle>().StartAsync();
await host.RunAsync();
```

Bit.Butil reaches the browser through its script, so the host page needs `_content/Bit.Butil/bit-butil.js` before the
Blazor script, or Butil's lazy scripts - see the [Butil README](https://github.com/bitfoundation/bitplatform/tree/develop/src/Butil).

## Where it runs

In the browser only. Everywhere else - prerendering, Blazor Server, Blazor Hybrid, a test host - `StartAsync` does
nothing, so the registration can stay in a project that is also prerendered or hosted on the server. In those hosts the
documents, their connections and their stores live in a .NET process that a tab going offline or out of sight does not
touch; for presence in Blazor Server, `AddBitBcrdtCircuitPresence` from Bit.Bcrdt.Server does the equivalent job.

## Settings

- `ReconnectWhenOnline` (on) - only a document in `Reconnecting` is touched: never one the application disconnected on
  purpose, nor one that was refused. A fresh attempt that fails leaves the provider's own schedule in charge.
- `FlushWhenHidden` (on) and `AfterFlush` - bit Bcrdt writes changes in the background as they happen, so the flush
  waits for writes rather than starting them; `AfterFlush` is where a store whose writes are buffered is made durable,
  such as a `BcrdtBlazeDbDocumentStore` with `WaitForDurability` off: `options.AfterFlush = ct => database.FlushAsync(ct)`.
  Failures are reported through `BcrdtBrowserLifecycle.Error`.
- `SuspendPresenceOnPageHide` (on) - the withdrawal is sent while the page is torn down, so it reaches the others when
  the browser still lets the connection send; the presence timeout covers the rest.
- `RequestPersistentStorage` (off) - some browsers ask the user, so it is the application's call; `IsStoragePersistent`
  says what the browser answered.

## Why there is no OPFS store here

Bit.Butil can reach the origin private file system, but a bit Bcrdt store on top of it would rebuild what already
ships: [Bit.Bcrdt.BlazeDb](https://www.nuget.org/packages/Bit.Bcrdt.BlazeDb) keeps documents on OPFS through BlazeDb's
own backend, with its write-ahead log, torn-tail recovery and Web Locks writer election, and Bit.Bcrdt's
`BcrdtIndexedDbDocumentStore` already lets several tabs persist the same document - which OPFS, whose files take one
writer at a time, would make harder rather than easier.

The full guide is in the [Bcrdt README](https://github.com/bitfoundation/bitplatform/tree/develop/src/Utilities/Bcrdt).
