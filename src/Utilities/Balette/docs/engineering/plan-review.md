# Review of the "world-class improvement plan" (2026-11-10)

The plan asked for a 19-phase evolution of Bit.Balette into a "command and action platform" with eleven integration
adapters. Balette had already been through thirteen audit-and-improve iterations (see [PROGRESS.md](../../PROGRESS.md)),
so the first job was to map every phase onto what exists, and to decide - phase by phase - what fits the product.

The product is a keyboard-first command palette: one Razor component, one scoped service, C# search, a small browser
layer. Every decision below was weighed against that: does it make the palette better for people using it and for the
developers integrating it, or does it make Balette a framework they have to learn?

## Verdict per phase

Legend: **Done** - existed before this review, with the evidence; **Built** - added in this pass; **Backlog** - worth
doing, scheduled below; **Declined** - does not fit, with the reason.

| Phase | Verdict | Evidence / reason |
|---|---|---|
| 0 Audit | Done | Iterations 1-13 audited the code against cmdk, kbar, VS Code, Raycast, GitHub and the WAI-ARIA APG; this file maps the plan onto the result. |
| 1 Command model | Done | Explicit or derived stable ids (symbol-only titles get code-point ids), subtitle, description, keywords, tags, group, icon, shortcut, badge, value, `Data`, priority, nested children, `VisibleWhen` + `Refresh()`, `Disabled` + reason, `Register` handles, `Update`/`Unregister`, a per-scope registry with copy-on-read snapshots under a lock, duplicate-id warnings, a fluent builder and Razor markup. |
| 1 Typed `BaletteCommand<T>` | Declined | `Data` plus closures already carry typed state into handlers; a generic command type doubles the API surface (builder, markup, sources, results) for no scenario the closures cannot express. |
| 1 Several registries | Declined | One registry per scope, plus `Commands`/markup per palette and `IncludeRegisteredCommands="false"` for an inline palette that lists only its own, covers application-wide and component-scoped commands. A named-registry concept would duplicate it. |
| 2 Execution | Done | Cancellation on close, busy state after 150 ms, single-flight per palette, confirmations, results (close, keep open, back, reset, page, prompt, error, toast), errors logged with generic text, `CommandExecuted`/`CommandFailed`, nested execution through `ctx.Palette`. |
| 2 Guards and permission checks | **Built** | `Guard` per command, guards of pages over everything under them, `IBaletteCommandGuard` in DI, asked at execution time on every path, failing closed; `CommandRefused`. See below. |
| 2 Execution events with outcome | **Built** | `balette.command.executions` with `succeeded`/`failed`/`cancelled`/`refused`, plus `CommandRefused`. |
| 2 Middleware pipeline | Declined | Guards (before) and the executed/failed/refused events (after) are the two hooks real apps asked for. A general middleware chain invites ordering bugs, hides control flow, and duplicates what wrapping a handler in the builder already does. |
| 2 Per-command timeouts, concurrency policies | Declined | A handler owns its `CancellationToken` and its `HttpClient` timeout; sources already have `SourceTimeoutMs`. Running one command at a time per palette is the right UX for a palette; parallel or queued execution is a job runner's concern. |
| 2 `ExecuteAsync` returning a result | Backlog (P3) | Changing the return type breaks `IBaletteService` implementers; a default-implemented `TryExecuteAsync` returning the outcome is the compatible way, once someone needs it beyond the events. |
| 3 Search | Done | fzf-style DP with word-boundary and camelCase bonuses (which is what makes `gs` find "Go to Settings" - acronym matching), `'exact`, `^prefix`, `suffix$`, `!negate`, `"phrases"`, `a \| b`, typo tolerance, Unicode folding (Latin diacritics, Arabic/Persian variants and digits, fullwidth), weighted fields, nested paths, a pluggable matcher, highlights that never split a grapheme, deterministic ties (title, then registration order), debounce + cancellation for stale results. |
| 3 Pins boosting search rank | Backlog (P3) | Pins lead the empty query; while searching they rank like any frecency. A small `PinWeight` would be consistent; no one has asked. |
| 3 Pluggable ranker | Declined | `BaletteRanker` is weights over score, priority and frecency; the weights are options and the matcher is pluggable. A second extension point for the same decision has no use case yet. |
| 4 Providers | Done | `IBaletteSource` (debounced, cancellable, timed out, retry row, paging with cursor and total, `ApplyMatcher` interleaving), mode sources, page sources, DI sources, delegate and static sources. |
| 4 Source caching / request dedup | Declined | Debounce and cancellation already drop superseded requests; caching policy belongs to the app's `HttpClient`/`IMemoryCache`, which knows what is stale. |
| 5 Navigation and keyboard | Done | Nested pages with clickable breadcrumbs, `Backspace`/`←`/`Esc` up one level, Home/End/PageUp/PageDown, `Ctrl+N`/`P`, Vim bindings, `Tab` action panel, `Mod+Enter`, prefix modes, RTL-aware arrows. |
| 5 Forward navigation, rebindable in-palette keys, transition hooks | Declined | No palette users rely on forward navigation; the in-palette keys are the WAI-ARIA combobox keys and must stay predictable for assistive technology; `OnQueryChanged`/`OnCommandExecuted` and the service events cover the hooks apps use. |
| 6 Shortcuts | Done | `Mod`, sequences, input-element suppression, IME, non-Latin layouts, auto-repeat, AltGr, conflicts (later registration wins, logged), browser-reserved warnings, toggle shadowing, two palettes, inline palettes never claiming keys, pending-sequence hint. |
| 6 User-editable shortcuts | Backlog (P3) | Needs a store, a conflict UI and a reason; the registry already accepts `Update` with a new shortcut, which is what an app's own settings page would call. |
| 6 A standalone shortcut service | Declined | The shortcuts are the palette's browser layer; a general hotkey library is a different product. |
| 7.1 Details | Done | `Description`, `DetailTemplate`, `ItemTemplate`, `Actions`, badges, values. |
| 7.2 Multi-select | Done | Checkboxes, `Space`/`Enter` to toggle, `Ctrl+Enter` to submit with the selection, kept across source searches. Select-all and a selection limit: Backlog (P3). |
| 7.3 Structured prompts | Done / Declined | Free text with validation and `Error` done; a choice is a page, a yes/no is a toggle. A multi-field typed form engine (and a Borm adapter) is declined: forms belong in a dialog, not a palette row. |
| 7.4 Quick actions | Done | Action panel, confirmations, toasts, follow-up pages and prompts. |
| 7.5 History | Done | Recents, frecency, pins in pin order, remove one, clear all from the palette, `EnableHistory`. Stored query history is declined: the README promises that queries are never stored. |
| 8 Extensibility | Done / Built | DI sources, history store, matcher, strings, templates, modes - and now guards. A "versioned plugin contract" is declined: normal .NET assemblies and DI are the contract, versioned by the package. |
| 9 Context and permissions | Done / Built | `VisibleWhen` + `Refresh()` for visibility, guards for permission at execution time, both reading the scope's services. A separate context-provider model is declined: the app's DI services already are the context. |
| 10 Persistence | Done | `IBaletteHistoryStore` (localStorage default, in-memory, custom), loaded data sanitized, failures never stop the palette, `MaxHistoryEntries` bound, frecency decay. A BlazeDb adapter is declined: it is a short `IBaletteHistoryStore` an app writes against its own schema. |
| 11 Theming | Done | Tokens for colours, sizes, radius, shadow, blur, motion; light/dark/auto; forced colours; more contrast; RTL with logical properties; density; reduced motion; unstyled mode; eight templates. |
| 11 Bheadless infrastructure | Declined (for now) | Balette's native `<dialog>` and combobox are audited by axe-core and asserted against Chromium's accessibility tree in three engines. Swapping to another overlay stack adds a dependency and risk without a user-visible gain; revisit if Bheadless ships a combobox/listbox primitive. |
| 12 Accessibility | Done | axe-core in five states, light/dark, two themes; accessibility-tree assertions; live regions; forced colours. Manual screen-reader pass still open (needs readers and the people who use them). |
| 13 Performance | Done | Snapshot caching, top-k heap, per-thread matcher buffers, bounded inputs; BenchmarkDotNet now to 50,000 commands (linear: a first keystroke 33 ms, `sett` 19 ms at 50,000; see the benchmarks README). CI regression thresholds: declined - shared CI runners are too noisy for BenchmarkDotNet thresholds to mean anything. |
| 14 Hosting | Done | E2E on WebAssembly and Blazor Server in Chromium, Firefox and WebKit; lazy loading (trimmed and AOT); prerendering; two palettes; disposal. Hybrid only through the unit suite (open). |
| 15 Belemetry | **Built** | `BaletteDiagnostics`: a `Meter` and an `ActivitySource` named `Bit.Balette`; Belemetry (or OpenTelemetry) listens with one line. No adapter package: the BCL is the integration. |
| 15 Brouter | Declined (for now) | Brouter's route helpers are generated into the app at compile time; there is no runtime route catalog for an adapter to read. Navigation commands are one `BaletteCmd.Navigate` each. |
| 15 Blex | Declined | The integration is `VisibleWhen` reading the store plus `Refresh()` on change - one line in the app; an adapter package would wrap one line. |
| 15 Butil | Declined | Clipboard, fullscreen and history commands are choices an app makes with its own wording; the demo shows clipboard and share-link commands. |
| 15 BevTools | Backlog (P3) | BevTools has an extension model (`IBevToolsExtension`); surfacing its commands in the palette is a product decision for BevTools, and would live in a BevTools package, not here. |
| 15 Borm, BlazeDb, Bcrdt, Bonboarding, Bmotion | Declined | Borm: see 7.3. BlazeDb: see 10. Bcrdt: shared commands across users need authorization the palette cannot provide. Bonboarding: `BaletteTrigger` already shows the shortcut, and a tour is the app's. Bmotion: the palette's CSS transitions honour `prefers-reduced-motion` already. |
| 16 Developer experience | Done | Three lines to a working palette, builder and markup, XML docs on every public member, logged diagnostics, an MCP server over the API and guide. |
| 17 Tests | Done / Built | Unit 710 (net10.0 + net11.0), MCP 188, E2E in three engines and two render modes; this pass added guard, diagnostics and browser guard tests. |
| 18 Docs and demos | Done / Built | README guide (now with guards and diagnostics), a demo page per area; the Options & security page now runs guards live. |
| 19 Final audit | Built | A fresh-context reviewer over this diff; see PROGRESS.md, iteration 14. |

## What was built

### Guards

`VisibleWhen` decides what is listed and was the only availability check, evaluated when the list was built. The
README said "visibility is not authorization", which left permission checks entirely to handlers - and the palette
ran a hidden command from `ExecuteAsync`, a stale row whose state had changed since the list was built, and anything
under a page whatever the page's rules were.

- `BaletteCommand.Guard` / `.Guard(...)`: async with a message, or a sync predicate.
- `IBaletteCommandGuard` + `AddBitBaletteCommandGuard<T>()`: scoped, so it can use `AuthenticationStateProvider`.
- Asked on every path: row (keyboard or pointer), `Mod+Enter` (first action, link in a new tab), global shortcut,
  `ExecuteAsync`, the second `Enter` of a confirmation, prompt and multi-select submits.
- A page's guard covers its registered descendants and, from the palette, whatever its page lists (lazy children,
  page sources, handler-built pages have no `Parent`).
- Fails closed on exceptions; cancelled quietly when the palette closes; shows busy after 150 ms like a handler.
- `CommandRefused` event, an information log, `BaletteStrings.CommandRefused`.
- Stale rows: a row whose `VisibleWhen` (or an ancestor's) turned false since the list was built is refused with
  `BaletteStrings.CommandUnavailable`. `ExecuteAsync` keeps running hidden commands (documented): visibility is still
  not authorization, guards are.
- No guard registered: no extra await, no behaviour change.

### Diagnostics

`BaletteDiagnostics.MeterName` / `ActivitySourceName` (`Bit.Balette`): `balette.command.executions`,
`balette.command.duration`, `balette.source.duration`, `balette.search.duration`; `balette.command` and
`balette.source` activities. Tags are low-cardinality (kind, trigger, outcome, source name); ids only on activities
and only while `RecordCommandIdsInTraces` is on; never a query, prompt value, title or selection.

## Backlog after this pass

| Pri | Item |
|---|---|
| P3 | `TryExecuteAsync` returning the outcome. |
| P3 | Select-all and a selection limit on multi-select pages. |
| P3 | A pin weight while searching. |
| P3 | User-editable shortcuts (an app settings page over `Update`). |
| P3 | BevTools commands in the palette (a BevTools package). |
