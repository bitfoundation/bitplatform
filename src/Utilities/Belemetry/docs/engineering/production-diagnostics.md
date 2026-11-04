# Production diagnostics and support bundles - engineering record

The audit, the gap analysis, the decisions and the evidence behind `BelemetryOptions.Diagnostics`. The user guide is the
README's "Production diagnostics and support bundles" section; the contributor contracts are in `CLAUDE.md`
("Production diagnostics"). Started 2026-11-04.

## 1. Audit (before the change)

| Area | What existed | Reusable for diagnostics |
| --- | --- | --- |
| Signals | `ILogger` provider, `ActivityListener`, `MeterListener`, browser script (page views, clicks, web vitals, fetch/XHR, JS errors, console) | Yes: every record already passes consent, privacy, value limits and processors in `BelemetryPipeline.Enqueue` / `pipeline.ts emit` |
| Consent | 12 feature bits, held-before-decision records, purge on revoke, GPC, cross-tab adoption | Yes: the timeline follows the same rules |
| Privacy | One rule set on both sides (blocked/redacted keys, URL redaction, pattern scrubbing, value cut) | Yes, with scrubbing forced on for bundles |
| Error limits | Signature dedupe + per-minute cap on client hosts | Yes: they now protect the timeline too |
| Correlation | W3C `traceparent` from the browser (same origin by default), trace ids on .NET logs | Yes: the link between page, server and backend |
| Session ids | `session.id` per tab (consent-governed) | Deliberately not used: a bundle is about a failure, not a person |
| Hosts | WebAssembly (pipeline in the page), Server (script only by default), Hybrid (Relay) | The timeline needed a per-circuit design for Server |
| BevTools | Component tree, render profiler, timeline, error center (reflection over Blazor internals, not trim-safe, dev only) | Only through an integration package: Belemetry is AOT-safe production code |
| Framework (.NET 10/11) | `Microsoft.AspNetCore.Components` activities `Navigate` (route template, component type) and `HandleEvent` (component type, `code.function.name`, attribute name, `error.type`); `Server.Circuits.StartCircuit`; lifecycle meter `render_diff.duration` by component type; circuit host log categories | Yes (verified by reading the shipped 10.0.14 assemblies); WebAssembly and Hybrid need `AddComponentsTracing()` to emit them |

## 2. Gap analysis

| Requirement (plan phase) | Before | Done | Where |
| --- | --- | --- | --- |
| Shared event model, versioned contracts (P1) | missing | `BelemetryDiagnosticEvent`, `BelemetryDiagnosticException`, JSON writer/reader by hand | `Diagnostics/BelemetryDiagnosticEvent.cs`, `BelemetryDiagnosticJson.cs` |
| Stable component type vs short-lived instance ids (P1) | missing | type full name everywhere; capture-local `c1..cN` keys, never renderer ids | `BelemetryComponentInfo.InstanceKey` |
| Component and render context (P2.1) | partial (spans only when traces exported, none in WASM) | framework activities through the hub's own listener; opt-in render statistics; stack-inferred component marked `stack`; BevTools adds ancestry | `BelemetryDiagnosticHub`, `BelemetryDiagnosticEventFactory` |
| Route templates, no raw URLs (P2.2) | missing | router templates (`framework`); browser paths templated (`inferred`); URLs as templates with allowlisted query parameters | `BelemetryUrlTemplate`, `diagnostics.ts templateUrl` |
| Exceptions from every source, deduplicated (P2.3) | partial | renderer/circuit/error boundary/interop/app/tracked/browser origins; error limits shared with export; relayed records skipped (browser timeline is the one browser source) | `OriginOf`, `BelemetryPipeline.Enqueue` |
| Bounded timeline, triggers, post-trigger window (P3) | missing | two rings + memory budget + eviction counters; automatic captures with cooldown, pending limit, retention | `BelemetryDiagnosticRecorder` |
| Safe by default, central sanitizer, consent (P4) | partial | one sanitizer (key redaction incl. credential fragments, app sanitizers that fail closed, forced scrubbing, limits); consent purge; preview + confirm required for export | `BelemetryDiagnosticSanitizer`, `BelemetryDiagnosticCapture.Confirm` |
| Grouping and releases (P5) | missing (only the error limiter's signature) | versioned fingerprint, classifier override/critical/ignore, release markers, `GroupAcrossReleases` | `BelemetryExceptionFingerprint`, `IBelemetryExceptionClassifier`, `BelemetryReleaseOptions` |
| Client/server and circuit correlation (P6) | partial | per-circuit timelines by renderer `SynchronizationContext` + tagged activities; server trace index found by the page's own trace ids; reconnect dialog transitions; circuit handler sample | `BelemetryDiagnosticHub.Resolve`, `BelemetryTraceIndex`, `Samples/Server/SampleCircuitDiagnostics.cs` |
| Snapshots (P7) | missing | named providers, isolated (timeout, failure), dictionary-only data, recorded snapshots | `IBelemetrySnapshotProvider`, `TakeSnapshotsAsync` |
| Bundles (P8) | missing | ZIP + manifest (SHA-256 per file), deterministic, bounded, validated reader | `BelemetrySupportBundle` |
| Issue reports (P9) | missing | observed / inferred / user-provided findings, steps, missing evidence, Markdown that cannot carry markup | `BelemetryIssueReportBuilder`, `BelemetryIssueReport` |
| BevTools (P10) | missing | `Bit.BevTools.Belemetry`: Support panel, jump to component, trace filter, preview/export/import, component context provider | `src/Utilities/BevTools/Bit.BevTools.Belemetry` |
| Storage and transport (P11) | n/a | in-memory; optional tab-scoped `sessionStorage` copy of the browser timeline (Persistence consent); download (`DotNetStreamReference` on Server) or the app's own sink | `diagnostics.ts persist/restore`, `BelemetryJsBridge.DownloadAsync` |
| Performance (P12) | n/a | measured, section 5 | `Tests/Bit.Belemetry.Benchmarks/DiagnosticsBenchmarks.cs` |
| Tests (P13) | n/a | section 4 | |
| Docs, samples (P14) | n/a | README section, demo page, samples' Support page, CLAUDE.md contracts | |

## 3. Decisions

- **D-1: no new consent bit.** The timeline holds what the pipeline accepted under the user's existing grants, held while
  undecided and purged on revoke; a capture includes granted features only; export needs the user's confirmation of
  the previewed content. A separate "diagnostics" bit would collide with the existing `Diagnostics` group name, change
  the persisted `All` value and leave a timeline that cannot hold anything the telemetry consent did not cover anyway.
  Application state (snapshots) is opt-in per capture instead.
- **D-2: tap after the pipeline's rules.** Diagnostics see each record after consent, privacy, value limits, the error
  limits and the app's processors - one place where a record is judged, so a processor that drops PII drops it from
  bundles too. The script runs the processors on queued records before reading the timeline (memoized).
- **D-3: metadata, not values.** Each category keeps a fixed attribute allowlist plus `AllowedAttributes`; log entries
  keep their template unless `IncludeLogMessages`; exceptions keep a scrubbed message unless `IncludeExceptionMessages`
  is off. Pattern scrubbing is forced on for bundles even when export turns it off.
- **D-4: per-circuit isolation by synchronization context**, the technique BevTools' `SessionResolver` uses, plus an
  activity tag for work that leaves the renderer. Never "the only live session" on a server. A missing event is the
  failure mode, never a leaked one.
- **D-5: framework activities through a separate listener** sampling `AllData`: it never makes an activity `Recorded`.
  But an activity that exists is the parent of the spans under it, and an unsampled parent makes a parent-based sampler
  drop them (found by the review): so the listener creates one only when Belemetry's export covers the source (its
  decision, `IgnoredActivityNames` respected) or `CreateFrameworkActivities` says so - by default on WebAssembly and
  Hybrid, where Belemetry is the tracer, not on a server, which usually has its own. `AddComponentsTracing()` is now
  called by `AddBitBelemetryServices` so WebAssembly and Hybrid emit them at all (behavior change, upgrade notes). The
  lifecycle meter is not registered automatically: its per-render cost is opt-in (`ComponentRenderStatistics`).
- **D-6: no reflection, no object graphs.** Snapshot providers return dictionaries; anything else is a marker. All JSON
  is written and read by hand (`Utf8JsonWriter`, `JsonDocument`): trimming and AOT safe like the rest of the package.
- **D-7: the browser is untrusted on a server.** The page's timeline is read as a stream (circuit message size), bounded,
  re-sanitized, ids validated, exceptions fingerprinted in .NET. JS-initiated triggers only key a capture whose own
  limits bound it.
- **D-8: BevTools stays a separate, development-only package.** Belemetry defines `IBelemetryComponentContextProvider`;
  `Bit.BevTools.Belemetry` implements it with BevTools' registry (internals granted, as to `Bit.BevTools.Server`). The
  dependency never goes the other way.
- **D-9: "observed" means read from evidence.** A finding is observed only when it rests on a fact recorded as such
  (the same trace as a `HandleEvent`, a status code, the router's template); time proximity and stack frames make
  inferences, and the report says so.

## 4. Evidence

| Suite | Result | Notes |
| --- | --- | --- |
| `Tests/Bit.Belemetry.Tests` (net10.0, net11.0) | 393 per framework, all passing (313 before) | 80 new: fingerprints, sanitizer, timeline, consent, server isolation, capture/preview/export, bundle security, preview component (bUnit), C#/TS contracts, options binding |
| `Tests/bit-belemetry-js` (vitest) | 214 passing (192 before) | 22 new: templates, recorder, processors, purge, persistence, reconnect transitions, triggers, sampled-out sessions, Blob stream, download |
| `Tests/Bit.Belemetry.Tests.E2E` web modes (Chromium) | 44 passing (41 before) | new: a failure captured and a bundle downloaded through the preview in Server, WebAssembly and Auto, validated against the format without the library |
| `Tests/Bit.Belemetry.Tests.E2E` hybrid (WebView2) | 3 passing | regression only |
| Hosting, MCP, EF Core, Hangfire | 49, 62, 18, 8 passing | regression only (the MCP search ranks README text) |
| Release build, trim/AOT analyzers on | 0 warnings | one IL2091 on `AddBitBelemetrySnapshotProvider<T>` found and annotated |
| `Tests/Bit.BevTools.Tests` (net10.0, net11.0) | 649 per framework, all passing | 5 new integration tests |

Found by the benchmarks and a real-browser run of the Server sample, and fixed with tests
(`BelemetryDiagnosticFollowUpTests`): an exported entry below the timeline's level was recorded anyway; the stack was
parsed three times per exception; app code under `Bit.Belemetry.*` (the samples, the demo) was treated as library frames;
one request seen from both sides (page and server, one trace) was reported twice; an `ErrorBoundary` exception passed to
`CaptureAsync` duplicated the event the framework had logged.

Found by the tests while building it: camelCase credential keys (`couponSecret`) escaped name-based redaction (now
fragment matching in diagnostics); `DotNet.createJSStreamReference` refuses a stream reference made in JS when .NET asks
for `IJSStreamReference` (the page returns a Blob and Blazor wraps it).

### Independent review

A fresh-context review of the whole change found 18 defects; all were real and are fixed, each with a test
(`BelemetryDiagnosticReviewTests`, `diagnostics.test.js`):

| # | Finding | Severity | Fix |
| --- | --- | --- | --- |
| 1 | The hub's listener created framework activities the export ignored; their unsampled flag made parent-based samplers drop the spans under them | high | created only when the export covers them or `CreateFrameworkActivities` (not on a server by default) |
| 2 | The circuit tag flowed into timers and loops started under a click handler: later work, for anyone, landed on that circuit's timeline and kept it alive | high | the tag is weak and only honored while the activity runs; closed timelines are skipped |
| 3 | The preview's redactions did not reach the report | high | the report reads attributes through the capture |
| 4 | Removed events and sections stayed in `exceptions.json` | medium | occurrences filtered; browser-only groups follow the Browser section |
| 5 | The exception object bypassed blocked attributes and processors; a sampled-out session bypassed the browser's processors | medium | messages and stacks from the record's attributes; processors run before the timeline keeps a sampled-out record |
| 6 | `MinimumLogLevel` not enforced for exported entries | medium | (also found by the benchmarks) |
| 7 | A nested scope replaced, then unregistered, its circuit's timeline | medium | one shared, reference-counted timeline per renderer context |
| 8 | A capture built across a revoke, or open in the preview, held revoked data | medium | revoke generations; consent checked again at every read and export |
| 9 | `Clear` left the page's timeline | medium | `ClearAsync` clears both |
| 10 | Without exception messages, templates still carried quoted text | medium | templates go with the messages |
| 11 | An unhandled `JSException` was not "unhandled" | low | origin by reporter first, category by type |
| 12 | `AllExceptions` dropped the browser's triggers | low | accepted |
| 13 | The page's events ignored `Lookback` and `IncludeRecentActivity` | low | applied |
| 14 | The preview listed 200 events of more | low | lists all by default, or says how many it does not |
| 15 | A rendered circuit id could reach the timeline with `IncludeLogMessages` | low | template when a message renders a redacted key |
| 16 | Trace-index events could repeat an id in a bundle | low | renumbered on joining a capture |
| 17 | Cancelled .NET requests were failures | low | cancellation checked first |
| 18 | Page hides recorded without PageViews | low | gated |

## 5. Performance

Measured with `DiagnosticsBenchmarks` (details in `Tests/Bit.Belemetry.Benchmarks/README.md`, "Production diagnostics"):
an exported entry below the timeline's level costs nothing more (257 vs 254 ns); a warning on the timeline +230 ns and
+400 B; a logged exception with an inner one +5.7 us (sanitizing and fingerprinting two exceptions - the error limits
cap it in a loop); the framework's `HandleEvent` activity ~410 ns per UI event (it is not created at all without a
listener); a capture of a full timeline 17 us; capture + export about 1-1.6 ms. Acceptable overhead was set from these
measurements: nothing on the paths below the timeline's level, sub-microsecond per recorded activity event, and
exception costs bounded by the existing error limits.

## 6. Known limitations

- **Component hierarchy** needs a component-context provider (BevTools); Belemetry alone names types only.
- **JS interop** failures are evidence when they surface as exceptions or logs (`JSException`, `JSDisconnectedException`);
  successful interop calls are not recorded (no supported hook without wrapping `IJSRuntime`).
- **Circuit transitions** in the browser need the app's reconnect dialog (`#components-reconnect-modal`); the server side
  needs a scoped `CircuitHandler` (sample provided) - the framework exposes no per-circuit event without one.
- **Server work outside a circuit's context or its UI events** (background services, `Task.Run` started elsewhere) is not
  attributed to any circuit, by design.
- **Prerendering** gets its own short-lived .NET timeline; the browser timeline spans the transition.
- **Server-side HTTP spans** need the server pipeline's traces (`Server.CollectDotnetTelemetry`).
- **Render statistics** in WebAssembly/Hybrid need `services.AddComponentsMetrics()` by the app.
- **Hybrid downloads** depend on the WebView; apps write `ExportAsync` to a file or share sheet.
- **The trace index** (Server) returns server events for trace ids the page reports from its own fetch spans; a page
  that knew another user's 128-bit trace id could ask for that trace's sanitized metadata. Trace ids are not exposed by
  default, so this needs a separate leak first.

## 7. Next tasks

- Hybrid-specific sample flow (MAUI share sheet for `ExportAsync`).
- A `dotnet` CLI or MCP tool over bundles (BevTools' `bevtools` tool reads its own exports; a bundle reader could join it).
- Dead/error click correlation for the "triggering action" (G-20b in the improvement plan) once field data exists.
