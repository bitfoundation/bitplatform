# Belemetry improvement plan

A living document: the audit of `src/Utilities/Belemetry`, the gaps found, what was changed about them, and the
evidence. It is updated at the end of every improvement round. `README.md` stays the user-facing reference and
`CLAUDE.md` the contributor contract; this file records *why* things changed and what is still open.

Status legend: **done** (implemented, tested, documented), **partial**, **open**, **deferred** (in scope, not yet
scheduled), **out of scope** (deliberately not in the core), **platform-limited**.

---

## 1. Baseline (round 0, 2026-10-24)

### 1.1 What Belemetry is

One telemetry SDK for every Blazor host, covering the browser and .NET halves of an app without the OpenTelemetry
packages:

| Layer | Implementation |
| --- | --- |
| .NET collectors | `ActivityListener` (traces), `MeterListener` (metrics, own aggregation with cumulative + delta state), `ILoggerProvider` (logs) |
| .NET pipeline | consent gate -> privacy -> enrichment -> processors -> bounded queue -> batches -> exporters, on async loops only (WebAssembly has one thread) |
| Exporters | OTLP/HTTP protobuf + JSON (hand-written encoders), OTLP/gRPC (hand-framed HTTP/2), Azure Monitor `/v2.1/track` + Live Metrics, console, in-memory, custom `IBelemetryExporter` |
| Delivery | in-process retry with backoff and `Retry-After`, Azure 206 partial retry, gRPC status handling, offline persistence (files / `localStorage`), unload drain (WebAssembly hands serialized batches to the page for `keepalive`) |
| Browser script | `bit-belemetry.js` (TypeScript, esbuild): page views (history, enhanced navigation), visit time, clicks, web vitals (LCP/INP/CLS/FCP/TTFB), document load, resource timing, long tasks, JS errors + unhandled rejections, fetch/XHR client spans with `traceparent`, console capture |
| Consent | 12 feature bits granted/revoked at runtime, held-before-decision records, purge on revoke, GPC, persisted choice |
| Hosts | WebAssembly (pipeline in the browser), Server/SSR (script only unless `Server.CollectDotnetTelemetry`), Auto, Hybrid (MAUI/WPF/WinForms, relay transport) |
| Extra packages | `Bit.Belemetry.EntityFrameworkCore` (command spans), `Bit.Belemetry.Hangfire` (job spans with context propagation) |

### 1.2 Supported targets and verified environment limits

- Library: `net8.0`, `net9.0`, `net10.0`; `IsTrimmable` + `IsAotCompatible`; one browser script (ES2019).
- This workstation has only the .NET 10 runtime (10.0.13). **The net8.0 and net9.0 test legs cannot run here**
  (`You must install or update .NET to run this application`); they build, and CI (`bit.ci.Belemetry.yml`) runs
  all three. Every test result below is net10.0 unless stated otherwise.
- The Playwright E2E suite (`Tests.E2E`) and the publish gate are on-demand (`bit.ci.Belemetry.e2e.yml`) and need a
  Chromium install; they are not part of the per-round verification here.

### 1.3 Baseline results

| Check | Result |
| --- | --- |
| `dotnet build Bit.Belemetry.csproj` (3 TFMs) | 0 warnings, 0 errors |
| `Tests/Bit.Belemetry.Tests` (net10.0) | 222 passed |
| `Tests/Bit.Belemetry.Tests.EntityFrameworkCore` (net10.0) | 9 passed |
| `Tests/Bit.Belemetry.Tests.Hangfire` (net10.0) | 4 passed |
| `Tests/Bit.Belemetry.Tests.Hosting` (net10.0) | 29 passed |
| `Tests/Bit.Belemetry.Tests.Mcp` (net10.0) | 59 passed |
| `Tests/bit-belemetry-js` (vitest) | 131 passed |
| net8.0 / net9.0 test legs | not run (runtimes missing locally) |
| E2E (Playwright), publish gate | not run (on-demand suite) |

---

## 2. Competitive research

Sources and findings: section 7. The final status of every gap below, and of every audit category: section 8.

---

## 3. Gap analysis

Ranked by risk first (security, privacy, correctness), then completeness. IDs are stable across rounds.

### P0 - security, privacy, correctness

| ID | Gap | Evidence | Plan |
| --- | --- | --- | --- |
| G-01 | **.NET and browser scrub different things.** The browser scrubs every string attribute; .NET only `exception.message`, `exception.stacktrace`, `url.query` and URL attributes. A structured log value (`logger.LogInformation("User {Email}", email)`), a `TrackEvent` attribute or `url.path` reaches the backend raw from .NET while the same value is scrubbed from the body. Pattern order and regexes also differ (`Bearer eyJ...` gives `Bearer [jwt]` in .NET, `Bearer [token]` in JS; unsigned JWTs are missed by .NET). | `BelemetryPrivacy._textAttributes`; `privacy.ts clean()`; the C# test asserting "Only known text attributes are scrubbed" vs the JS test scrubbing `tags`. | Scrub every string (and string array) attribute in .NET as the script does, exempting the ids and `server.address`; one pattern set and order on both sides; cheap pre-checks so the common attribute costs no regex. |
| G-02 | **Exporter headers of a server leak into every page.** `<BelemetryScript />` serializes `BelemetryOtlpExporterOptions.Headers` into the prerendered HTML in Direct transport (the default). On a Server/SSR host those headers come from server configuration (API keys from appsettings / Key Vault) that no browser would otherwise see. | `BelemetryBrowserConfigFactory.Create`: `Headers = otlp.Headers`. | A separate, explicit `BrowserHeaders` for what the page may send; a server host stops publishing `Headers` and warns when an exporter relied on them. WebAssembly/Hybrid keep sending `Headers` (the configuration already lives on the client there). |
| G-03 | **Relay is an open door on Blazor Server.** `Belemetry.Relay` is invocable by any connected client whatever the transport, without a rate limit, into the queue shared by every user (one abusive circuit evicts everybody's telemetry), and a relayed resource may override the server's `service.name`/`service.version`/environment. | `BelemetryJsCallbacks.Relay` -> `BelemetryPipeline.Relay` (no transport or rate check; `Settings.Resource.Merge(browser resource)`). | Accept relays only with the Relay transport; a per-connection token bucket; the server's identity keys win over a relayed resource on a server host. |
| G-04 | **The export loop inherits the ambient execution context** of whoever first resolved logging (an `Activity`, `AsyncLocal` state, culture), for the life of the process. `DisposeAsync` is not idempotent (a second call throws `ObjectDisposedException` on the token source). | `BelemetryPipeline.Start` -> `RunAsync`; `DisposeAsync`. | Start the loop with flow suppressed; make disposal idempotent. |

### P1 - reliability and essential completeness

| ID | Gap | Evidence | Plan |
| --- | --- | --- | --- |
| G-05 | No error deduplication or rate limit: an error thrown in a loop (timer, render loop) floods the queue (1024 in the page, 2048 in .NET) and evicts every other record. | `errors.ts`, `BelemetryLogger`, `TrackException`. | Per-signature suppression window plus a per-minute cap, counted in an attribute on the next record. |
| G-06 | Concurrent `keepalive` requests share a 64 KiB budget in the browser; the script sets `keepalive` per request (`<= 60 KiB`) without tracking what is in flight, so parallel batches fail spuriously and are retried or lost. | `transport.ts post()`. | Track in-flight keepalive bytes; only claim the budget when it fits. |
| G-07 | No attribute value length limit (OTel `OTEL_ATTRIBUTE_VALUE_LENGTH_LIMIT`); a multi-megabyte log body or stack trace is exported whole (and dooms an unload request). | `BelemetryAttributes` caps the count only. | A configurable limit, applied on both sides. |
| G-08 | Browser records cannot be filtered or enriched by the app in Direct transport (processors exist only in .NET). | `pipeline.ts` has no hook. | A JS processor/`beforeSend` hook and global attributes. |
| G-09 | Persisted requests are resent all at once on start/online, each with in-process retries, even when the backend is still down. | `transport.ts resendPersisted()`. | Resend in order, stop at the first failure (as .NET's `RetryStoredAsync` does). |
| G-10 | SDK self-observability: dropped records (queue overflow, pending limit, rejected requests) are not counted anywhere a user can see. | `AddToQueue`, `hold`, sender. | Counters exposed as diagnostics and, optionally, as metrics. |

### P2 - production readiness

| ID | Gap | Plan |
| --- | --- | --- |
| G-11 | OTLP partial-success responses are ignored (protobuf not decoded; JSON not logged). | Decode `rejected_*` counts and the error message; log them. |
| G-12 | A relayed batch creates a new resource object each time, so the export groups by reference and sends one request per relayed batch. | Reuse an equal resource. |
| G-13 | Cross-origin `Script error.` (no stack, not actionable) is recorded by default. | Add it to the default ignored messages. |
| G-14 | A faulty processor is silently skipped for every record. | Report it once per processor. |
| G-15 | XHR abort and timeout are reported as `NetworkError`. | Distinguish `abort`/`timeout`. |
| G-16 | Sensitive attribute keys (`http.request.header.authorization`, `*.password`, cookies) are not redacted by name. | Key-based redaction list. |

### P3 - advanced

| ID | Gap |
| --- | --- |
| G-17 | `session.previous_id` and session start/end events (OTel session conventions). |
| G-18 | Route templates for page views (`/orders/{id}` rather than `/orders/42`) from the Blazor router. |
| G-19 | A browser `startSpan` API for custom user-action timing from JS. |
| G-20 | Frustration signals (rage/dead clicks), soft navigations. |

---

## 4. Feature matrix

Requirement -> implementation -> tests -> docs -> status. Rows are added as rounds touch them; baseline capabilities
that were audited and found sound are marked "baseline".

| Area | Requirement | Implementation | Tests | Docs | Status |
| --- | --- | --- | --- | --- | --- |
| Privacy | Same scrubbing on both sides, every string attribute | `BelemetryPrivacy`, `privacy.ts` | `BelemetryPrivacyTests`, `privacy.test.js` | README Privacy, demo Privacy | done (R1) |
| Privacy | Query/fragment redaction, `url.query` | same | same | same | done (R1) |
| Security | Server secrets never rendered into pages | `BelemetryBrowserConfigFactory.BrowserHeaders`, `BrowserHeaders` option, startup warning | `BelemetryBrowserConfigFactoryTests` | README OTLP, demo OTLP/Transport | done (R1) |
| Security | Relay as an untrusted input on Server | `BelemetryJsBridge.RelayAsync`, `BelemetryPipeline.Relay`, `BelemetryRateLimiter` | `BelemetryRelayTests`, `BelemetryRateLimiterTests` | README Transport, CLAUDE.md | done (R1) |
| Lifecycle | Export loop isolated from the starter's context; idempotent dispose | `BelemetryPipeline.Start/DisposeAsync` | `BelemetryPipelineTests` | - | done (R1) |
| Consent | Per-feature consent, held records, purge on revoke, GPC | `BelemetryConsentState`, `consent.ts` | existing | README Consent | baseline |
| Export | OTLP protobuf/JSON/gRPC, Azure Monitor, retries, persistence | `Exporters/*` | existing (conformance vs official protos) | README Exporters | baseline |
| Browser | Page views, interactions, web vitals, errors, fetch/XHR | `Scripts/instrumentations/*` | existing | README Browser | baseline |

---

## 5. Architectural decisions

- **AD-1 (baseline, kept):** no OpenTelemetry package dependency; BCL listeners plus hand-written encoders. Keeps the
  WebAssembly download small and the package trimmable/AOT-safe.
- **AD-2 (baseline, kept):** consent is a first-class gate per feature on both sides; the server pipeline answers to
  operator configuration (`Server.Features`), never to one user's consent.
- **AD-3 (R1):** one privacy rule set, implemented twice (C#, TS) and tested on both sides with the same examples.
  Scrubbing every string attribute costs character scans, not regexes, for values that match no pattern.
- **AD-4 (R1):** what a server renders into a page is public. Exporter credentials reach a page only through an
  option whose name says so (`BrowserHeaders`).
- **AD-5 (R1):** on Blazor Server, anything arriving over the circuit from the script is untrusted input: bounded per
  circuit, gated by configuration, and unable to change the service identity.

---

## 6. Round log

### Round 1 - P0 security, privacy and lifecycle (G-01 to G-04)

**G-01 privacy parity - done.** `BelemetryPrivacy.ApplyToAttributes` now scrubs every string attribute and string
array, as `privacy.ts` does; the ids, `server.address` and `user_agent.original` stay verbatim on both sides (the
adversarial pass found that four-part user-agent versions such as `Chrome/120.0.0.0` read as IPv4 addresses, which
would have mangled ASP.NET Core 10's `user_agent.original` request attribute). One pattern order (bearer, JWT, e-mail,
card, IPv4) and one JWT regex (unsigned tokens included). `url.query` gets parameter redaction on both sides and is
dropped with `StripQueryString`; a kept fragment's own query (`#/cb?access_token=`) is redacted on both sides.
Character pre-checks keep the common attribute regex-free. Tests: `BelemetryPrivacyTests` (+5, one inverted),
`privacy.test.js` (+5).

**G-02 header exposure - done (behavior change).** New `BelemetryOtlpExporterOptions.BrowserHeaders`. A server host
publishes only `BrowserHeaders`; WebAssembly/Hybrid publish `Headers` + `BrowserHeaders`. A startup warning names a
server exporter whose page now exports without its headers. This also stops the Aspire dashboard key from
`OTEL_EXPORTER_OTLP_HEADERS` being rendered into pages. Tests: `BelemetryBrowserConfigFactoryTests` (+4),
`BelemetryOptionsBindingTests` (binds `BrowserHeaders`). Docs: README (OTLP), CLAUDE.md, demo Transport/OTLP pages,
the MCP code reviewer's BLM003 message.

**G-03 relay hardening - done.** `Belemetry.Relay` is ignored unless the transport is Relay; on a server each bridge
(circuit) gets a token bucket (`BelemetryRateLimiter`, burst `Browser.MaxQueueSize + Consent.PendingLimit`, refilled
per minute; logs before spans when trimming); features switched off in configuration are dropped; the server keeps
the service identity keys and no longer stamps its own host/process attributes on browser records. The relayed
resource is cached per page (`BelemetryRelaySource`), so a page's batches export together (G-12, done with it).
Tests: `BelemetryRelayTests` (7), `BelemetryRateLimiterTests` (2).

**G-04 loop context - done.** The export loop starts through `Task.Run` with execution-context flow suppressed: it no
longer resumes on a MAUI/WPF/WinForms UI thread's `SynchronizationContext` or carries the starter's `Activity`. The
test was mutation-checked: it fails against the previous start code. `DisposeAsync` is idempotent.

**Verification.** Core 242/242 (net10.0), JS 136/136, Hosting 29/29, MCP 59/59; solution build clean except
`Samples/Maui` (needs the `maui-android` workload, not installed here). Not run: net8.0/net9.0 legs, E2E, publish gate.

**Breaking changes.** G-02: a Blazor Server/SSR app whose browser exports relied on `Headers` must move public keys to
`BrowserHeaders` (or use Relay). Relay on a server: relayed records no longer carry the server's `host.*`,
`process.*`, `os.*`, `cloud.*` resource attributes.

**New gaps from the sweep.**

| ID | Gap | Priority |
| --- | --- | --- |
| G-21 | Pattern scrubbing turns four-part version numbers in free text into `[ip]` (`"upgraded to 1.2.3.4"`). Inherent to a regex scrubber; documented. | P3 |
| G-22 | A server publishes the endpoints of all its OTLP exporters in the page's `ignoreUrls`, server-only internal addresses included (needed so a WebAssembly client with the same configuration does not trace its own exports). Low-severity information disclosure. | P2 |
| G-23 | The relay budget is per circuit; a client opening many circuits gets many budgets (bounded by the server's circuit limits). | P3 |
| G-24 | The library never uses `ConfigureAwait(false)`: an app's own `FlushAsync` from a UI thread serializes on it. The background loop no longer does. | P3 |

### Round 2 - P1 delivery reliability and resource bounds (G-05, G-06, G-07, G-09, G-13)

**G-05 error floods - done.** New `BelemetryOptions.Errors` (`DuplicateWindow` 5 s, `MaxPerMinute` 100), one rule set
in `BelemetryErrorLimiter`/`BelemetryRateLimiter` and `limits.ts`: a signature of scope, type, message (or body) and the
first two stack lines; a duplicate is suppressed until the window from the last *recorded* occurrence ends; a token
bucket caps the rest. Applied to every Errors-feature record in the script (JS errors, rejections, console errors,
`trackException`), and to .NET's own records on WebAssembly/Hybrid (not relayed ones, not on a server). Both sides are
tested against the same signature string.

**G-06 keepalive budget - done.** `transport.ts` tracks the bytes of its keepalive requests in flight and claims the
64 KiB budget only when a body fits; a request that does not fit goes without keepalive, and during unload is stored
directly (with `Persistence`) instead of firing a request the browser would refuse.

**G-07 value length - done.** `BelemetryOptions.MaxValueLength` (32768) cuts attribute strings (and string arrays), log
bodies and span status messages on both sides, after privacy scrubbing (tested: a cut before would leave
`jane@exa...`), never inside a surrogate pair.

**G-09 sequential resend - done.** `resendPersisted` sends stored requests oldest first, one attempt each, and stops at
the first that fails, putting the rest back with their order and age.

**G-13 - done.** `^Script error\.?$` is a default ignored error message on both sides.

**Adversarial pass.** Found and fixed: a `string[]` attribute holding nulls (possible through `IEnumerable<string>`)
made the new cut throw, and `IBelemetry.TrackEvent/TrackPageView/TrackException` called the pipeline unguarded - an
internal failure would have surfaced in app code. Both fixed and tested (`AStringListWithNulls_IsCutWithoutThrowing`).

**Verification.** Core 252/252, JS 148/148 (vitest), `tsc --noEmit` clean.

**Behavior changes.** Identical errors within 5 s and errors past 100/min are no longer recorded on client hosts
(switch off with `Errors.DuplicateWindow = TimeSpan.Zero`, `Errors.MaxPerMinute = 0`). `Script error.` is ignored by
default. Strings over 32768 characters are cut.

### Round 3 - protocol conformance, extensibility, sensitive names (G-08, G-11, G-14, G-15, G-16, G-25 to G-28)

The ecosystem survey (section 7) arrived at the start of this round and reordered it: two of its findings were
conformance defects.

**G-25 OTLP retry policy - done (spec conformance).** The OTLP specification allows retrying 429, 502, 503 and 504
only ("All other 4xx or 5xx response status codes MUST NOT be retried"); Belemetry retried and persisted 408, 439 and
500 for every backend. Retry policy is now per exporter on both sides (`IBelemetryHttpExporter.IsRetryable`,
`transport.ts isRetryableFor`); Azure Monitor keeps its own list. The browser also recognized only its own
`azureMonitor` name for 206 handling, not .NET's `azuremonitor{n}` requests resent through the page; fixed.

**G-11 partial success - done.** `OtlpPartialSuccess` reads `partial_success` from protobuf, JSON (int64 as string or
number) and uncompressed gRPC responses; the sender logs "N rejected: message" as a warning and does not retry.
Rejected (non-retryable) requests are warnings too now - data loss with a fixable cause (a key, an endpoint). Tested
with responses built from the official generated protobuf types, for all three protocols.

**G-28 User-Agent - done.** .NET exports send `User-Agent: Bit.Belemetry/<version>` unless an exporter header sets
one, as the OTLP exporter spec asks; never in the browser (a page cannot, and the header would need CORS approval).

**G-16 sensitive names - done.** `Privacy.RedactedAttributes` (default: password, secret, token, API key,
authorization and cookie names) replaces the value of a matching key - whole key or last dot segment,
case-insensitive, or `prefix*` - with `REDACTED` on both sides, with a bounded per-key verdict cache. Tested on both
sides with the same examples (`{Password}`, `user.password`, header attributes; `password_policy` and `author.name`
untouched).

**G-27 pre-signed URLs - done.** `X-Amz-Signature`, `X-Amz-Credential`, `X-Amz-Security-Token`, `X-Goog-Signature`,
`X-Goog-Credential` join the default redacted query parameters (the OpenTelemetry URL conventions' list).

**G-26 keepalive request cap - done.** Besides the 64 KiB, at most 9 keepalive requests in flight (what Chromium
allows, and what the OpenTelemetry and Faro browser exporters assume).

**G-15 HTTP client outcomes - done.** An aborted fetch/XHR keeps `error.type = AbortError` but no error status (it did
not fail); XHR timeouts are `TimeoutError`, network failures `NetworkError`. A method outside the conventions' list is
`_OTHER` with `http.request.method_original`, span name `HTTP`.

**G-08 browser processors - done.** `BitBelemetry.addProcessor(fn)` (runs at export, so one added after the
auto-start still sees the initial page view; `false` drops; a throw skips) and `BitBelemetry.setAttributes(...)`
(global attributes, scrubbed, never over a record's own or the consent-governed ids).

**G-14 processor failures - done.** A throwing .NET processor is reported once, as a warning; the script reports once
per processor in its debug output.

**Verified, already correct:** OTLP `LogRecord.event_name` (field 12) carries the event name on both sides, which is
what the specification now requires instead of the deprecated `event.name` attribute (conformance-tested).

**Verification.** Core 265/265, JS 156/156, `tsc` clean, Hosting 29/29, MCP 59/59.

**Behavior changes.** OTLP 408/439/500 are final (dropped with a warning, not persisted). Attributes named like
credentials are `REDACTED`. Aborted requests are no longer error spans. Unknown HTTP methods are `_OTHER`.

### Round 4 - back/forward cache, kill switch, self-observability (G-10, G-29, G-30)

**G-29 back/forward cache - done.** A `pageshow` with `persisted` emits a page view of type `restore` (no history event
fires for it) without counting the time spent in the cache as a visit; web vitals start a new generation, as Google's
web-vitals library does - new ids, FCP/LCP from the restore to the next painted frame (two animation frames), CLS and
INP from zero with the interaction count re-based, navigation type `back-forward-cache`.

**G-30 `OTEL_SDK_DISABLED` - done.** `true` (any case) turns Belemetry off when environment variables are used; unlike
the variables that fill unset options it wins over code (the OpenTelemetry SDKs' semantics).

**G-10 self-observability - done.** A `Bit.Belemetry` meter (`BelemetrySelfTelemetry`, opt-in through
`Metrics.Meters`): `belemetry.records.dropped` by `belemetry.reason` (queue_full, pending_full, not_consented,
consent_revoked, error_limit, relay_limit, filtered) and `belemetry.signal`, and `belemetry.records.exported` by
exporter and outcome (delivered, rejected, stored, failed). The meter listener lets these through the export's
suppression scope, where they are recorded. Requests carry their record count. In the browser `getStats()` returns the
same reasons (plus `sampled_out`) and request outcomes. Names are Belemetry's own on purpose: the OpenTelemetry
`otel.sdk.*` self-observability conventions are still in development.

**Found while doing it:** with `Consent.PendingLimit = 0` the first undecided record made the pipeline remove the
first element of an empty list (`ArgumentOutOfRangeException`, swallowed by the logger, thrown from `IBelemetry`
before round 2's guard). Fixed and tested.

**Verification.** Core 271/271, JS 159/159, `tsc` clean; Hosting 29/29, MCP 59/59, EF 9/9, Hangfire 4/4; solution
build clean (MAUI sample excluded: workload missing).

### Round 5 - disclosure, frustration signals, measured performance (G-20, G-22, G-33)

**G-22 internal endpoints - done.** A server no longer lists any exporter URL in the page's `ignoreUrls` (its exports
never go through the page's fetch; the script recognizes its own exporters' URLs by itself). A WebAssembly client's
`start` now appends its own exporter URLs to the script's list, and the HTTP tracer recompiles the list when it
grows - which also fixes a latent gap: a client whose exporters differed from the server's had its .NET exports
traced as dependencies.

**G-20 rage clicks - done.** `Browser.TrackRageClicks` (default on): three clicks on one element within a second emit
one `browser.rage_click` event per burst (feature Interactions). Dead and error clicks remain open (G-20b): they
need DOM-mutation and error correlation heuristics whose false-positive rate would need field data to tune.

**G-33 benchmarks - done.** New `Tests/Bit.Belemetry.Benchmarks` (BenchmarkDotNet 0.15.8, the repository's version):
privacy rules, the synchronous recording paths, the encoders. The first run showed round 1's all-attribute scrubbing
costing ~860 ns per HttpClient span; exact pre-checks (13 digits for a card, 3 dots for an IPv4 address) and a fast
path for URLs with nothing to redact (C# and TS) brought it to ~280-590 ns, and an `ILogger` entry from 1066 to
892 ns. Boundary tests on both sides pin the pre-checks. Numbers: the benchmarks' README.

**Demo.** The browser page shows the error-loop suppression (20 identical throws, one record), rage clicks in the
interactions feed, and a live `getStats()` panel.

**Found by the Hosting suite:** four assertions pinned the old behavior (a server listing exporter URLs); updated to
the new contract, with the reason in the test.

### Round 6 - independent review of the whole change set

The `code-review` skill (high effort) read the working tree cold, without this plan. Ten candidate findings; all
were checked against the code, nine were real and fixed, one (JS retry policy keyed on .NET's exporter names) was
real coupling, now locked by a contract test.

| Finding | Severity | Fix | Test |
| --- | --- | --- | --- |
| A negative protobuf length in the new partial-success reader moved the reader backwards: an **infinite loop in the export loop** on a hostile or broken 200 response; other malformed bodies threw types the filter missed, making a delivered request look failed (resent, duplicated) | high | lengths checked against what is left; catch-all (it is diagnostics); reporting wrapped so it can never fail a send | `AHostileBody_NeverHangsOrThrows` (6 bodies, under a timeout) |
| `ExecutionContext.SuppressFlow()` throws when flow is already suppressed: logging would fail to start inside a library's suppressed block | medium | guarded with `IsFlowSuppressed()` | `Start_WithTheFlowAlreadySuppressed_StillStartsTheLoop` |
| (pre-existing) An Azure 206 on the last attempt returned Failed and the caller stored the original full request: accepted items resent at every retry pass | medium | `SendWithRemainderAsync` returns what is undelivered; the set stores only that, a replay replaces the stored request with it; the narrowed request gets one more attempt while it shrinks | two sender tests (remainder; a server that keeps rejecting the same item stops) |
| The error limiter opened a duplicate window for an error the per-minute cap dropped (C# and TS) | medium | the signature is recorded only when the error is | one test per side |
| Scrubbing every .NET attribute turned version values (`{AppVersion} = "1.2.0.0"`) into `[ip]` | medium | version-named keys keep IPv4-looking values (both sides) | one test per side |
| `RedactedAttributes` vs the host exemption, and blocked ids, were applied in a different order in C# and TS | medium (parity) | one order on both sides, documented in CLAUDE.md | one test per side |
| `ConcurrentDictionary.Count` (all locks) on every cache miss once the key cache was full | low | own counter | - |
| The script rebuilt its query-parameter map for every URL | low | cached per configured list | covered by the privacy suite |
| The partial-retry request lost `Time` (its expiry) and `Count` (self-telemetry) | low | copied / set | asserted in the remainder test |
| JS picks Azure's retry policy by .NET's exporter names | coupling | contract test `ExporterNames_AreTheContractTheScriptRetriesBy` | - |

The adversarial pass on these fixes caught one more of my own: `.map(scrub)` in the script would have passed the array
index as the new `addresses` argument (index 0 is falsy). Fixed before it ran.

**Measured again.** The version-key and ordering fixes added ~80 ns per span; caching one rule per attribute key
(`BelemetryPrivacy.Rule`) more than recovered it - final numbers in section 8.2 and the benchmarks' README.

**Verification.** Core 280/280, JS 164/164, `tsc` clean, Hosting 29/29, MCP 59/59, EF 9/9, Hangfire 4/4, solution
build clean (MAUI sample excluded).

---

## 7. Research notes

Surveyed 2026-10-24 from the official specifications, documentation and source (OpenTelemetry spec and semconv 1.44,
OpenTelemetry JS / opentelemetry-browser, OpenTelemetry .NET 1.20, Application Insights JS 3.4, Sentry JS 11, Datadog
Browser RUM, Grafana Faro 2.13, Elastic RUM, MDN / WHATWG / W3C). Items not verified first-hand are marked.

| Finding | Source | Effect on Belemetry |
| --- | --- | --- |
| OTLP/HTTP retries 429/502/503/504 only; partial success must not be retried; `Retry-After` should be honored; 4 MiB response limit | opentelemetry.io/docs/specs/otlp | G-25, G-11 fixed (R3) |
| OTLP exporters should send a `User-Agent` | otel/protocol/exporter | G-28 fixed (R3) |
| `event.name` attribute deprecated in favor of LogRecord `EventName` | semconv registry `event` | already correct |
| Attribute defaults: 128 per record, unlimited value length; resources exempt | otel/common | Belemetry: 128 (baseline) + 32768-char cut (R2, deliberate deviation for payload safety; configurable, 0 = spec default) |
| Span events and `exception.escaped` deprecated; exceptions move to log events | OTel blog 2026 (OTEP 4430), semconv exceptions | Belemetry already records exceptions as log events (`exception` event); span exception events from .NET `Activity.AddException` still pass through - fine until the span event API is removed |
| `TraceIdRatioBased` deprecated (no removal before 2027) in favor of the composable ProbabilitySampler (Dev, `ot=th:` tracestate threshold) | otel/trace/sdk | deferred (G-31): keep ratio sampling compatible with OTel .NET; revisit when ProbabilitySampler is stable |
| `session.id`, `session.previous_id`, `session.start`/`session.end` events (Dev) | semconv session | G-17 |
| Only `browser.web_vital` is a defined browser event (Dev, attributes `browser.web_vital.*`); no page-view event in semconv | semconv browser | Belemetry's `browser.page_view` stays a Belemetry convention; `web_vital.*` attribute names differ from the Dev `browser.web_vital.*` - deferred (G-32), a rename would break Azure mapping and dashboards |
| Default URL redaction: `X-Amz-Signature`, `X-Amz-Credential`, `X-Amz-Security-Token`, `sig`, `X-Goog-Signature` | semconv url | G-27 fixed (R3) |
| HTTP client: unknown methods `_OTHER`; cancelled requests not errors | semconv http-spans | G-15 fixed (R3) |
| Keepalive: 64 KiB in flight and ~9 requests (Chromium); Firefox keepalive historically behaved like plain fetch [unverified]; `sendBeacon` needs a CORS-safelisted type to avoid a preflight and sends credentials | WHATWG fetch, MDN, OTel JS and Datadog source comments | G-06, G-26 fixed; sendBeacon not adopted (OTLP content types need preflight, credentials mode `include`) - platform-limited |
| `visibilitychange: hidden` is the reliable exit signal; bfcache restores re-report web vitals with a new id | MDN, web-vitals | Belemetry flushes on hide (baseline); bfcache restore handling is a gap (G-29) |
| Mature SDKs: pre-send hooks (all), dedupe (Sentry, Faro), "Script error." filtered (Sentry), rate limits (Elastic 80/min, Datadog), frustration signals (Datadog, Faro), consent states (Datadog: granted/not-granted; pending on mobile only) | vendor docs | G-05, G-08, G-13 fixed; rage clicks fixed (G-20, R5), dead/error clicks deferred; Belemetry's per-feature held-before-decision consent goes beyond the surveyed SDKs |
| Blazor .NET 10: `Microsoft.AspNetCore.Components` activities `Navigate` (tag `aspnetcore.components.route`) and `HandleEvent`; meters `aspnetcore.components.navigation`, `.event_handler`, lifecycle and circuit meters | aspnetcore release/10.0 source, learn.microsoft.com | collected by default (`Microsoft.AspNetCore.Components*`); the route tag is a path to G-18 |
| OpenTelemetry .NET: `OTEL_SDK_DISABLED` honored since 1.15 | OTel .NET changelog | G-30 fixed (R4) |

---

## 8. Final audit

### 8.1 Gap status

| Status | Gaps |
| --- | --- |
| **done** | G-01 privacy parity, G-02 server headers, G-03 relay trust boundary, G-04 loop context (R1); G-05 error floods, G-06 keepalive bytes, G-07 value length, G-09 sequential resend, G-12 relay resource reuse, G-13 `Script error.` (R1-R2); G-08 browser processors/attributes, G-11 partial success, G-14 processor failures, G-15 HTTP outcomes, G-16 sensitive names, G-25 OTLP retry policy, G-26 keepalive request cap, G-27 pre-signed URLs, G-28 User-Agent (R3); G-10 self-observability, G-29 bfcache, G-30 `OTEL_SDK_DISABLED` (R4); G-20 rage clicks, G-22 published endpoints, G-33 benchmarks (R5); the review's nine fixes (R6) |
| **deferred** (in scope, not done; reason) | G-17 `session.start`/`session.end` events and `session.previous_id` - Development semconv, and without `Sessions` consent every page load is a new in-memory session, so the events would be per-page noise; needs a product decision. G-18 route templates on page views - .NET 10's `Microsoft.AspNetCore.Components.Navigate` activity carries `aspnetcore.components.route` and is collected by default; joining it to the script's page view needs a cross-boundary design (WASM-only; Server has it on the server). G-19 a `startSpan` JS API - Blazor apps time custom work with `ActivitySource` in C#, which works in every host. G-20b dead/error clicks - heuristics need field data. G-31 the composable ProbabilitySampler - Development; ratio sampling stays compatible with OpenTelemetry .NET. G-32 `browser.web_vital.*` attribute names (Development semconv) - a rename would break the Azure mapping and existing dashboards; revisit when stable. |
| **documented limitation** | G-21 IPv4-looking version numbers in free text (log bodies) become `[ip]` - inherent to pattern scrubbing; mitigated for version-named keys. G-23 the relay budget is per circuit. G-24 an app's own `FlushAsync` serializes on the caller's context (the loop does not). Delivery during unload is best effort. Firefox's keepalive behavior is unverified here. |
| **out of scope** | gRPC from the browser (impossible: fetch has no trailers); `sendBeacon` (OTLP content types need a CORS preflight, and beacons send credentials); session replay; a dashboard UI in the SDK. |

### 8.2 Capability audit (the mission's categories)

| Category | State after the rounds | Evidence |
| --- | --- | --- |
| A. API and foundations | Strongly typed options (binder-generator bindable, every new option bound in the test), safe no-op when disabled (`Enabled`, `OTEL_SDK_DISABLED`), explicit lifecycle (idempotent start/dispose, flush), telemetry calls never throw into app code past argument validation | `BelemetryOptionsBindingTests`, `BelemetrySettingsTests`, `Dispose_FlushesOnce_AndIsIdempotent`, the `IBelemetry` guard |
| B. OpenTelemetry | OTLP retry/partial-success semantics now per spec; `event_name`; `User-Agent`; `OTEL_SDK_DISABLED`; semconv HTTP `_OTHER`, cancellations; pre-signed URL redaction. Not claimed: full conformance (no conformance suite run against a collector here) | `OtlpPartialSuccessTests` (official proto types), `OtlpProtobufConformanceTests`, sender tests |
| C. Browser and Blazor instrumentation | + bfcache restores (page views, web vitals), rage clicks, XHR abort/timeout | `instrumentations.test.js`, `fetch.test.js` |
| D. Tracing and correlation | baseline (W3C, parent-based sampling, propagation allow-list); unchanged | existing suites |
| E. Metrics | baseline aggregation and cardinality; + self-telemetry meter | `SelfTelemetry_CountsDropsAndExports_OnTheBitBelemetryMeter` |
| F. Logging and exceptions | + error dedupe and rate limit (both sides), structured values scrubbed, credential names redacted, processor failures reported | `BelemetryLimitsTests`, `limits.test.js`, privacy suites |
| G. Sessions and context | baseline sessions; + global attributes in the script that cannot bypass consent | `api.test.js` |
| H. Export and transport | per-backend retry policy, partial successes, keepalive budget (bytes + requests), sequential resend, remainder-only persistence after a partial success | sender, transport and store tests |
| I. Buffering and delivery | bounded queues and stores (baseline) + bounded error limiter, relay budget, value length; delivery guarantees documented (best effort at unload) | README "Offline persistence" |
| J. Processing | .NET processors (baseline, now reported on failure) + browser processors at export time | `AProcessorThatThrows_IsReportedOnce`, `api.test.js` |
| K. Privacy, security, consent | the R1 fixes (parity, server secrets, relay boundary, endpoint disclosure) + sensitive names + version-safe scrubbing; consent model unchanged and re-verified by the suites | privacy, config-factory and relay suites |
| L. Configuration and DI | `BrowserHeaders`, `Errors`, `MaxValueLength`, `RedactedAttributes`, `TrackRageClicks`; startup warnings for misconfiguration | binding and settings tests |
| M. Resilience and lifecycle | loop off the UI context and starter's ambient state, suppressed-flow start, idempotent dispose, `PendingLimit = 0` crash fixed, hostile responses cannot hang the loop | pipeline tests |
| N. Performance | measured (benchmarks project): the net privacy cost per span 867 -> 487 ns (-44%; 181 ns and no allocation for a query-less URL), per structured log 973 -> 673 ns (-31%), a full `ILogger` entry 1066 -> 869 ns | `Tests/Bit.Belemetry.Benchmarks/README.md` |
| O. Extensibility | browser processors, self-telemetry meter; exporter/processor contracts unchanged | - |
| P. Developer experience | README (new sections, upgrade notes, troubleshooting with `getStats()`), CLAUDE.md contracts, demo additions, MCP reference tables updated through the docs it indexes | - |
| Q. UI accessibility | the SDK has no UI; the demo's new stats panel uses `aria-live`; the demo site was not otherwise audited | - |
| R. Testing | +58 .NET tests and +33 JS tests over the baseline; mutation-checked where the test was subtle | section 8.3 |

### 8.3 Results

| Check | Baseline | Final |
| --- | --- | --- |
| Library build (net8.0, net9.0, net10.0; trim/AOT analyzers on) | 0 warnings | 0 warnings |
| `Tests/Bit.Belemetry.Tests` (net10.0) | 222 | 280 passed |
| `Tests/bit-belemetry-js` (vitest) | 131 | 164 passed |
| `tsc --noEmit` | clean | clean |
| `Tests/Bit.Belemetry.Tests.Hosting` (net10.0) | 29 | 29 passed (4 assertions updated to the G-22 contract) |
| `Tests/Bit.Belemetry.Tests.Mcp` | 59 | 59 passed |
| `Tests/Bit.Belemetry.Tests.EntityFrameworkCore`, `.Hangfire` (net10.0) | 9, 4 | 9, 4 passed |
| Benchmarks (BenchmarkDotNet short job) | - | run; numbers in section 8.2 and the benchmarks' README |
| Solution build | MAUI sample needs a workload | same (workload missing locally) |
| **Not run** | net8.0/net9.0 test legs (runtimes missing here; CI runs them), Playwright E2E and the trimmed/AOT publish gate (`bit.ci.Belemetry.e2e.yml`, on demand), real browsers other than happy-dom, real backends | |

Rounds completed: 6 (of at most 10). The rounds stopped because every P0 and P1 gap is closed with tests, the
independent review's findings are fixed, and what remains is deferred for a stated reason or platform-limited; the
next highest-value work (G-18 route templates, G-17 session events) needs product decisions rather than engineering.
