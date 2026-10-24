# Bit.Balette improvement log

A record of the audit-and-improve iterations run over Bit.Balette: what was found, what changed, how it was verified,
and what is still open. Newest iteration last.

## Baseline (2026-10-23)

- `dotnet build Bit.Balette.slnx`: succeeds; one pre-existing nullable warning in `Bit.Balette.Tests.Mcp`.
- `Bit.Balette.Tests` (bUnit + model tests, net8.0/net9.0/net10.0): 378 passed (126 per target framework), 0 failed.
- E2E (Playwright) and MCP suites exist; E2E is run on demand (`bit.ci.Balette.e2e.yml`).

### Audit findings

Priorities: **P0** critical, **P1** essential, **P2** important, **P3** optional.

| # | Pri | Area | Finding (evidence) | Plan |
|---|---|---|---|---|
| 1 | P1 | Robustness | `BaletteHistory` dereferences stored values unchecked: `{"id": null}` in `localStorage` throws on every rebuild, breaking the palette until storage is cleared. A custom store that throws on load leaves history faulted for the session; one that throws on save surfaces as an unobserved task exception. Load and record can mutate the dictionary off the renderer's thread while it is read. | Sanitize loaded data, catch and log store failures, copy-on-write snapshots. |
| 2 | P1 | Security | `Href` is passed to `NavigationManager.NavigateTo` / `window.open` unchecked; a `javascript:` URL from an async source's data would run script. | Allow-list URL schemes (`BaletteOptions.AllowedUrlSchemes`); refuse others. |
| 3 | P1 | Correctness | Lazily loaded children use `CancellationToken.None`: leaving or closing the page never cancels the request, and one shared `_loading` flag lets an abandoned load clear the spinner of a newer one. A failed load shows "Nothing to show yet." with no error or retry. | Per-page load state and cancellation; an error state with retry. |
| 4 | P1 | Errors | A handler exception is rethrown unless `Handled`: from `Enter` it becomes an unhandled promise rejection in the browser with no feedback in the palette; from a click it is fatal to a Blazor Server circuit. When handled, the raw `Exception.Message` is shown to the user. | UI-initiated runs log the error and show a localized message (overridable through the event args); programmatic `ExecuteAsync` still propagates. |
| 5 | P1 | Keyboard / i18n | Shortcuts match `KeyboardEvent.key` only, so `Ctrl+K`, `Ctrl+N`/`P` and `G I` do nothing on Russian, Persian, Arabic, Greek or Hebrew layouts. Holding a toggle shortcut auto-repeats it (the palette flickers open/closed). Safari's IME commit `keydown` (`keyCode` 229) is not ignored. | Fall back to the physical key for non-ASCII letters; ignore repeats; ignore `keyCode` 229. |
| 6 | P1 | Registration | Derived ids collapse: an emoji-only title derives an empty slug, `C#` and `C++` both derive `c`, and the second silently replaces the first. Duplicate ids and shortcut conflicts are never reported. When two commands claim one shortcut, the first in snapshot order wins, which is the opposite of the "later registration wins" rule for ids. | Fallback id for symbol-only titles, warnings for duplicate ids in one registration and for shortcut conflicts, later-registered shortcut wins. |
| 7 | P1 | Reactivity | `VisibleWhen` is re-evaluated only when the list rebuilds, so a global shortcut keeps the visibility it had at the last rebuild: a newly visible command's shortcut does nothing, a newly hidden one swallows its key. | `IBaletteService.Refresh()`. |
| 8 | P2 | Hosting | Several awaited JS calls (`open`, `focusInput`, `scrollIntoView`, `close`) let `JSDisconnectedException`/`TaskCanceledException` escape into rendering. | One guarded interop helper. |
| 9 | P2 | DX | `DebounceMs < 0` silently disables every source (the exception dies in a fire-and-forget task); `RowHeight <= 0` breaks virtualization. | Validate in the setters. |
| 10 | P1 | A11y | Group headings are `role="presentation"` siblings of the options, so screen readers never hear group names; toggle state and "pinned" are conveyed only visually (`aria-label` on a generic `span` is ignored); in multi-select pages `aria-selected` marks the *active* row while `aria-checked` marks the chosen ones. | Iteration 2. |
| 11 | P1 | A11y | No `forced-colors` styles: the active row is a background colour only and disappears in Windows High Contrast. `--bit-balette-fg-subtle` (#9ca3af on white, 2.5:1) is used for 11px group headings. | Iteration 2. |
| 12 | P1 | RTL / i18n | Physical CSS (`margin-left`, `border-left`, `left`/`translateX` toggle knob), unmirrored arrows and chevrons, `←`/`→` not direction-aware, no `dir="auto"` on user text, shortcut key caps reversed in RTL. Hard-coded English: `aria-label="Breadcrumb"`, "then" in shortcut hints. | Iteration 2. |
| 13 | P2 | Search | No diacritic folding (`cafe` misses "Café"), no Persian/Arabic letter-variant or digit folding (`ي`/`ی`, `ك`/`ک`, `۱۲`/`12`), no quoted phrases, no typo tolerance (`settigns` finds nothing). | Iteration 3. |
| 14 | P2 | Navigation | Nested commands are unreachable from the root query (kbar finds them with their ancestors); recents and pins of nested commands are recorded but never shown. | Iteration 3. |
| 15 | P2 | Execution | No busy indicator while a handler awaits; no prompt validation. | Iteration 4. |
| 16 | P2 | Sources | A throwing source is silently treated as empty (no log, no error, no retry). Sources run only on the root page, so `BaletteSourceContext.ParentCommandId` is always null and a nested page cannot search a server as the user types. | Iteration 4. |
| 17 | P2 | Performance | Every keystroke copies the registry, rebuilds the id/parent sets and re-syncs global shortcuts (evaluating every `VisibleWhen`), although none of that changes while typing. | Iteration 5, measured with the benchmarks. |
| 18 | P2 | Embedding | Only a modal dialog; no inline/embedded palette for search pages or settings panels. | Later iteration. |
| 19 | P3 | API | `BaletteRowKind.Separator` and the strings `MoreResults`, `Suggestions`, `Other`, `ClearRecents` are public but unused. | Later iteration. |

## Iteration 1 (2026-10-23) - correctness, robustness, security

Findings 1-9 above.

**Changed**

- `BaletteHistory`: loaded data is sanitized (null values, blank ids and negative counts dropped or clamped); a store
  that throws on load or save is logged (`ILogger<BaletteHistory>`) and the history keeps working in memory; every
  change publishes a new snapshot under a lock, so readers on the renderer's thread never enumerate a dictionary being
  written; never-used entries are no longer listed as recents.
- `BaletteOptions.AllowedUrlSchemes` (`http`, `https`, `mailto`, `tel`): any other scheme in `Href` is refused and
  logged. The scheme is parsed the way browsers parse it (control characters, tabs and newlines stripped).
- Lazily loaded pages: per-page loading state and a `CancellationToken` cancelled when the page is left, reset, loaded
  again or the palette disposed; an abandoned load changes nothing. A failed load shows an error state
  (`data-balette="error"`, `role="alert"`) with a Retry button, and `Enter` retries too.
- Command failures: logged through `ILogger<Balette>` and shown as a localized `BaletteStrings.CommandFailed` toast
  (`data-kind="error"`, assertive) instead of `Exception.Message`; `BaletteCommandErrorEventArgs.Message` lets a
  subscriber pick the text. UI-initiated failures are never rethrown (a click used to end a Blazor Server circuit);
  `IBaletteService.ExecuteAsync` still rethrows unhandled ones to its caller.
- Browser layer: shortcuts fall back to `KeyboardEvent.code` for non-ASCII keys (Russian, Persian, Arabic, Greek,
  Hebrew layouts), in-dialog chords too; auto-repeat is claimed but runs nothing; `keyCode` 229 is treated as IME
  composition, as MDN recommends.
- Ids and shortcuts: a symbol-only title derives `u<code points>`; duplicate ids within one `Register` call, shortcut
  conflicts, shortcuts the toggle takes and sequences that can never complete are logged once each; on a conflict the
  command registered last wins (it used to be the first).
- `IBaletteService.Refresh()` (a default interface member, so existing implementations still compile) re-evaluates
  `VisibleWhen` for the open list and the global shortcuts.
- One guarded helper for awaited JS calls; `DebounceMs`, `MaxResults`, `MaxRecents`, `MaxHistoryEntries`, `RowHeight`
  validate their values.

**Verified**

- Library build: 0 warnings, 0 errors on net8.0/net9.0/net10.0.
- `Bit.Balette.Tests`: 513 passed, 0 failed (171 per framework; 45 new: `BaletteReliabilityTests`,
  `BaletteCorruptHistoryTests`, `HistoryRobustnessTests`, `UrlSafetyTests`, `OptionValidationTests`, `DerivedIdTests`).
- E2E (Chromium, Playwright): 64 passed, 0 failed, 0 skipped, including the new `KeyboardLayoutTests` (Russian `Ctrl+K`,
  Persian `G S`, Russian `Ctrl+N`/`Ctrl+P`, a held `Ctrl+K`, IME keys globally and inside the palette).

**Compatibility notes**

- Behaviour change: a handler exception from the palette's UI is no longer rethrown, and the toast shows
  `BaletteStrings.CommandFailed` instead of `Exception.Message` (set `e.Message = e.Exception.Message` in
  `CommandFailed` to get the old text).
- Behaviour change: when two commands share a global shortcut, the later registration now wins.
- New public surface: `IBaletteService.Refresh`, `BaletteCommandErrorEventArgs.Message`, `BaletteOptions.AllowedUrlSchemes`,
  constructor overloads of `BaletteService` and `BaletteHistory` taking a logger, three strings.

## Iteration 2 (2026-10-23) - accessibility, right-to-left text, localization

Findings 10-12, plus what the competitor research and the new axe-core audit turned up.

**Research that shaped it** (cmdk, kbar, WAI-ARIA APG, fzf, VS Code, Raycast, GitHub): kbar#269 and cmdk#179 (group
headings read as options, invalid listbox children), the APG grouped-listbox and multi-select guidance (one selection
state per list), cmdk#370 (a click takes focus from the input), cmdk#196 (hard-coded English), Sarah Higley's notes on
`aria-activedescendant`, VS Code's `KeyboardEvent.code` dispatch, hotkeys-js#539 (pure `code` matching breaks Dvorak).

**Changed**

- Groups are `role="group"` named by their heading; ungrouped commands under Pinned/Recent get a "Suggestions"
  heading; virtualized rows carry `aria-describedby` (their heading), `aria-posinset` and `aria-setsize`.
- Multi-select pages use `aria-checked` only; single-select lists `aria-selected` only.
- Toggle state, "Pinned" and shortcut hints are spoken (visually hidden text) instead of `aria-label` on a `span`.
- Always-present live regions for toasts (`status`) and errors (`alert`), whatever the footer shows; page moves are
  announced ("Theme. 3 results available"); the list is `aria-busy` while loading.
- `BaletteStrings`: every property documented; `Breadcrumb`, `On`, `Off`, `ShortcutThen`, `PageAnnouncement`, and an
  overridable `ResultsAnnouncementFor(int)` for plural rules.
- RTL: logical CSS properties, mirrored arrows and chevrons (`data-mirror`), `dir="auto"` on user text, LTR key caps,
  `←`/`→` swapped in RTL text by the browser layer.
- Forced colours: outlines and system colours for the active row, dialog edge, checkboxes, switches, highlights.
- Contrast: `fg-muted`, `fg-subtle`, `danger` adjusted; headings and "then" use muted text; highlighted characters use
  the full text colour. New tokens for previously hard-coded sizes, the top offset, blur and animation duration.
- Rows no longer take focus on `mousedown`; the close button's name includes its visible "Esc" (WCAG 2.5.3); buttons
  are at least 24 px; highlights never split a grapheme (`BaletteHighlightedText.HitMask`).
- Bug found by the new tests: `Balette.SetQueryAsync` did not re-render; it now does, on the renderer's dispatcher.
- Bug found by the E2E suite: revealing a group's first row scrolled its sticky heading, which stops partway once the
  heading is inside its group; it now scrolls to the group's top.

**Verified**

- `Bit.Balette.Tests`: 558 passed (186 per framework; new `BaletteAccessibilityTests`, `HighlightedTextTests`).
- E2E: new `AccessibilityTests` (axe-core 4.14 through `Deque.AxeCore.Playwright`, WCAG 2.0-2.2 A/AA tags, five palette
  states x light/dark x demo theme/library defaults; forced colours; RTL arrows and chevrons; focus kept on click).
  axe found four real defects, all fixed: the close button's label-in-name, the "then" text's contrast, highlighted
  subtitle text's contrast, and (by the scroll test) the sticky-heading scroll.

## Iteration 3 (2026-10-23) - search quality and cross-page search

Findings 13-14.

**Changed**

- `BaletteTextFold`: one-to-one folding (positions stay exact). Latin diacritics (table generated from Unicode NFD,
  plus stroked letters, as fzf's) are folded only when the term has none of its own; Arabic/Persian letter variants,
  Arabic-Indic/Persian digits and fullwidth ASCII are always folded. `BaletteFuzzyMatcher.Normalize` and
  `BaletteSubstringMatcher.Normalize` (default true); operators and `!negate` follow the matcher.
- Query syntax: `"quoted phrases"` (straight or curly quotes, open quotes while typing, with `!`, `^`, `$`).
- Typo tolerance (`BaletteOptions.TypoTolerance`): a query that matched nothing is retried with optimal string
  alignment (1 edit for 4-7 letters, 2 from 8), scored below exact matches, announced and marked as close matches.
- Nested search from the root (`BaletteOptions.SearchNestedCommands`): nested registered/declared commands match with
  their ancestors' titles (`Weights.Path`), render "Theme › Dark", group under their root ancestor, rank after an equal
  root match; hidden/disabled/multi-select ancestors exclude them; modes never include them. Opening a nested page (also
  through `OpenAsync(pageCommandId)`) pushes its ancestors, so `Esc` climbs one level at a time. Recents and pins of
  nested commands are listed.
- Bug: pinned and recent commands were looked up only among the first `MaxResults` ranked rows; now among all matches.

**Verified**

- `Bit.Balette.Tests`: 684 passed (228 per framework; new `NormalizationTests`, `QuotedPhraseTests`,
  `TypoToleranceTests`, `BaletteSearchFeatureTests`).
- E2E: 71 passed, 0 failed - every axe audit clean in all four configurations after the iteration 2 fixes.
- Benchmarks (BenchmarkDotNet short job, same machine, the unmodified code extracted from `HEAD` as the baseline):
  the fuzzy matcher per field is unchanged (0.83-1.08x); a whole command with fuzzy terms 1.02-1.15x; a keystroke
  0.93-1.20x (median about 1.06x). The literal operators (`'x`, `^x`, `x$`) were 2-2.9x slower, because folding
  replaced a vectorized `IndexOf` - fixed in iteration 6. The Release build ran the trim and AOT analyzers on the new
  code with no warnings.

## Iteration 4 (2026-10-23) - execution and async workflows

Findings 15-16.

**Changed**

- Busy state: after 150 ms a running handler shows the spinner, `aria-busy`, `data-busy` on the dialog,
  `data-running` on its row, and announces "Working…" (`BaletteStrings.Running`).
- Sources: a source that throws or outlives `BaletteOptions.SourceTimeoutMs` (10 s) is logged and leaves a retry row
  (`data-error`) instead of silently vanishing; the other sources' results stay.
- Page sources: `BaletteCommand.ChildSources` / `.Source(...)` search as the user types on a command's page
  (`ParentCommandId` now meaningful); multi-select pages keep checked source results across searches.
- Prompts: `Prompt(title, onSubmit, validate, ...)` with `aria-invalid`, a described error and an assertive
  announcement; `BaletteCommandResult.Error(message)` for failures a handler expects (keeps the prompt open).
- `Ctrl+Enter` opens a plain link in a new tab (scheme-checked); a right click opens a row's action panel.
- Bug found while testing nested search: an option that answers `Back` (Theme › Dark) chosen from the root did
  nothing - not even a rebuild. `Back` with nowhere to go now behaves like `KeepOpen`.
- Demo: the rename prompt validates and refuses "admin" with an `Error`; "Find a person…" searches the directory as
  you type; a Language page (Français, Türkçe, فارسی, العربية, ...) shows normalization, nested search and RTL.

**Verified**: `Bit.Balette.Tests` 717 passed (239 per framework; new `BaletteExecutionTests`).

## Iteration 5 (2026-10-23) - per-keystroke cost

Finding 17.

**Changed**

- The palette keeps its snapshot of the registry (`_all`, the parent ids, a lookup by id) and takes a new one only when
  the registry's version, the markup commands or the `Commands` parameter changed. Global shortcuts are re-synced on a
  new snapshot, on open and close, after a command ran and on `Refresh()` - no longer on every keystroke.
- Benchmarks: `KeystrokeBenchmarks` mirrors the new `Rebuild` (no snapshot, nested pass); new
  `NestedKeystrokeBenchmarks` (a quarter of the registry nested, two levels) and `SnapshotBenchmarks`.

**Verified**

- `Bit.Balette.Tests`: 726 passed; a test proves typing no longer re-evaluates `VisibleWhen` for shortcuts (it would
  have failed before), and others that running a command and registering one still update them.
- Keystroke against the original code: 0.82-1.08x the time, and far less garbage - at 20,000 commands `qjz` allocates
  0.6 KB instead of 670 KB, `sett` 622 KB instead of 1.26 MB, `open sett` 285 KB instead of 944 KB. The snapshot,
  1.2 ms at 20,000 commands, is now paid once per registry change. A registry with a quarter of it nested costs about
  what a flat one does (`sett` at 2,000 commands: 933 vs 920 µs).
- MCP suite: 92 passed after two fixes it prompted - an oracle test located the light rule by its exact first line
  (the inline selector now comes first), and the search index learned the words theme tokens abbreviate (`bg`,
  `fg`, `kbd`, `z`...), so "active row background" finds `--bit-balette-active-bg` again.

## Iteration 6 (2026-10-23) - inline palettes, density, operator speed

Finding 18.

**Changed**

- `Inline="true"`: a `<section data-balette="inline">` in the page, always open; `Esc` goes back or clears; a closing
  command resets it. It answers no service open/close/execute request and claims no toggle or global shortcut, so a
  layout's modal palette and a page's inline one never both run a command; a key the modal one claimed is ignored.
  History and empty-query sources start on its first interactive render.
- `IncludeRegisteredCommands="false"`: only the palette's own commands; no registry commands, sources or modes.
- `Density` (`comfortable`, `compact`) as `data-density` and token overrides.
- Literal operators: an ASCII term in an ASCII text goes straight to the runtime's vectorized ordinal search.
- Demo: the settings section of /commands has a compact inline palette over the page's own settings.

**Verified**: `Bit.Balette.Tests` 750 passed (250 per framework; new `BaletteInlineTests`).

**Verified (iteration 6)**: E2E 79 passed after the suite's own locators were scoped to the modal dialog (with an inline
palette on /commands, `[data-balette='input']` matched two inputs); new `InlinePaletteTests` (driven in place, `Ctrl+K`
from its box opens only the modal, a global shortcut runs once, axe clean) and `SearchFeatureTests` (Français found as
`francais` from the root, `settigns`, Persian turning the page RTL, the validated rename prompt and its `Error`, the
directory page searching as you type). Literal operators: 1.3-1.7x baseline (from 2.3-2.9x), 0.24 µs per command.

## Iteration 7 (2026-10-23) - whole-library reassessment and polish

Reassessed every requirement area (A-R) against the code and the research. Small, worthwhile gaps fixed:

- Shortcuts browsers never hand to a page (`Mod+W`, `Mod+T`, `Mod+N`, `Ctrl+Tab`, ...) are reported once.
- Ancestor breadcrumbs are buttons back to their page; the current one keeps `aria-current="page"`.
- `prefers-contrast: more`: stronger borders, an outlined active row, underlined highlights.
- An inline palette honours `ResetOnClose = false`, and only scrolls its active row into view while it has the focus
  (revealing a row scrolls the page too, and an unused inline palette must not pull the page to it).
- Demo: the flows page lists the searching page and the root search, and its prompt sample shows validation and
  `Error`.

**Verified**: `Bit.Balette.Tests` 759 passed (253 per framework; new `BalettePolishTests`); E2E 79 passed; MCP 92 passed;
`dotnet build -c Release` (trim and AOT analyzers on) 0 warnings for net8.0/net9.0/net10.0; `dotnet pack` produces
`lib/` for all three frameworks with XML docs, both static web assets and the README, and the minified stylesheet keeps
the `content` fallbacks for browsers without alternative-text syntax.

## Iteration 8 (2026-10-23) - independent review and fixes

A separate reviewer, given only the code (not these notes' conclusions), reported ten defects. All ten were checked
against the code and confirmed (two, 2 and 5, partly predate these changes); all ten are fixed. Its regression tests
then exposed an eleventh, pre-existing one, and fixing them unmasked two more (12, 13).

| # | Defect | Fix |
|---|---|---|
| 1 | An inline palette trapped `Tab` (WCAG 2.1.2) and swallowed `Esc` even with nothing to undo. | Inline: `Tab` is never claimed; `Esc` only with a query, a page or an armed confirmation. |
| 2 | A character typed with AltGr (Windows: Ctrl+Alt) or Option fired the shortcut of its physical key. | No physical-key fallback under `AltGraph`; an Alt-composed character in a text field is typing. |
| 3 | The DI sources belonged to whichever palette started first; disposing it removed them for the rest. | DI sources are registered once per scope and never removed with a palette. |
| 4 | `SourceTimeoutMs` only stopped sources that honoured their token. | The search is awaited with `WaitAsync(timeout)`; a late failure is observed. |
| 5 | Every `OperationCanceledException` from a handler was swallowed, an `HttpClient` timeout included. | Quiet only when the palette cancelled it or it does not wrap a `TimeoutException`. |
| 6 | A handler's answer was applied to whatever page was current after `Esc` left its own. | `Back`/`Page`/`Prompt`/`Reset` are dropped when the page changed while it ran. |
| 7 | A page without sources kept the previous page's search, its spinner and `aria-busy`. | `PushPage` always restarts (or stops) the source search. |
| 8 | A fresh inline palette never sent `→`/`Backspace` on an empty query (it had not learnt its input). | The input is taken from the key event's target. |
| 9 | A history load started with a cancelled token stayed cancelled for good, and cancelled other callers. | The shared load never takes a caller's token; callers `WaitAsync` theirs. |
| 10 | An unused inline palette announced result counts (history loading, sources answering). | It announces only while it has the focus (`focusin`/`focusout` reported once each). |
| 11 | `services.AddBitBaletteSource<T>()` crashed every palette on its first render: registering the source rebuilt the list before the root page existed (present before these changes, untested until now). | The root page is created first in `OnInitialized`. |
| 12 | (Unmasked by fix 7.) The prompt, empty, loading and error states rendered their text inside `role="listbox"`, which may own only options and groups (axe `aria-required-children`, critical). It had passed the audit only because a stale search left the list `aria-busy`, which axe skips. | The list is a listbox only while it has rows; `aria-expanded` follows; a prompt's input is a text field named by its question. |
| 13 | (Found by the new E2E test.) Chrome makes a scrollable box with no focusable children a tab stop, so `Tab` in an inline palette landed on its list. | `tabindex="-1"` on the list. |

**Verified**: `Bit.Balette.Tests` 786 passed (262 per framework; new `BaletteReviewFixTests`,
`HistoryLoadCancellationTests` and a prompt-semantics test); five consecutive full runs before these fixes without a
flaky failure. E2E 81 passed (new `InlinePaletteTests` for `Tab`/`Esc` and Option typing; every axe audit clean, the
prompt state now audited without the `aria-busy` that had hidden it). MCP 92 passed. `dotnet build -c Release`: 0
warnings on net8.0/net9.0/net10.0 with the trim and AOT analyzers; `dotnet pack` succeeds.

## Iteration 9 (2026-10-23) - the verification gaps

**Changed**

- Blazor Server in the browser: the demo takes `Demo:RenderMode=Server` (off in the published site) and the E2E host
  starts it so when `BALETTE_E2E_RENDER_MODE=Server`; the fatal-error check also recognises a circuit ended by an
  exception; `bit.ci.Balette.e2e.yml` runs the suite in both modes.
- The first Server run: 79 of 81 passed. Both failures were tests assuming WebAssembly's speed, not palette defects - one
  proved a page had opened by a row that nested search also lists on the root, one read a highlight before the server's
  render arrived. Both now wait for what they need.
- `AccessibilityTreeTests`: Chromium's own accessibility tree, read through the DevTools protocol - the roles, names and
  states it hands to screen readers - for the combobox and its active option (title, subtitle and shortcut in words, in
  a named group), a toggle's state, a multi-select page (checked, not selected), a prompt (a textbox named by its
  question, no listbox) and the dialog (named, modal, polite and assertive live regions).

## Iteration 10 (2026-10-23) - the optional gaps

**Changed**

- Source paging: `IBaletteSource.SearchPageAsync(query, cursor, ...)` (a default interface method: existing sources answer
  one page), `BaletteSourcePage(Commands, NextCursor, TotalCount)`, a paging `BaletteDelegateSource` constructor and
  `.Source(name, (query, cursor, ct) => ...)`. A page with a cursor gets a "Show 20 more…" (or "Show more…") row that
  appends the next page, skipping what is listed, and selects its first row; a failed next page keeps what is listed.
- Pins are listed in the order they were pinned (`BaletteCommandUsage.PinnedAt`, an optional field, so stored history
  from before still loads).
- `a | b` alternatives, as in fzf; the query parser is public (`BaletteQuery`, `BaletteQueryTerm`,
  `BaletteQueryOperator`) and the MCP search lab uses it instead of its own copy of the syntax, which had already
  drifted (it split quoted phrases into words).
- A pending sequence (`G` of `G S`) is shown and announced near the bottom of the page until it completes or times out
  (`ShowSequenceHint`), and reported through `IBaletteService.SequencePending`.
- The unused public members: "Clear recent commands" (`ClearRecents`) is in every action panel; the confirmation's
  `ConfirmHint` is announced with it; `MoreResults` labels the paging row; `BaletteRowKind.Separator` is `[Obsolete]`.
- Literal operators: a term's ASCII check is made once per command instead of once per field. Measured, it changed
  nothing - the remaining 0.1 µs per command is the term-group loop and the per-field text check - and it is kept as
  an accepted cost.

**Verified (iterations 9 and 10)**: `Bit.Balette.Tests` 825 passed (275 per framework; new `QueryParserTests`,
`PinOrderTests`, `BaletteRemainingGapsTests`). E2E 87 passed on WebAssembly and 87 passed on Blazor Server (new
`AccessibilityTreeTests` and a pending-sequence test; one lookup in the new tests fixed after the first run, as the
page's root also reports the focus). MCP 92 passed (its API-index test now expects `BaletteQuery` to be public, and
checks that the internals - `BaletteTextFold`, `BaletteUrl`, `BaletteTypoTolerantMatcher`, `BaletteLog` - stay out).
`dotnet build -c Release`: 0 warnings with the trim and AOT analyzers; `dotnet pack` succeeds.

## Iteration 11 (2026-10-23) - every demo page in Chromium, Firefox and WebKit

The browser suite had only ever run in Chromium. It now runs in any of Playwright's three engines
(`BALETTE_E2E_BROWSER`), and `bit.ci.Balette.e2e.yml` runs all three in both render modes. Past the suite, every demo
page was driven by Playwright scripts in each engine: screenshots at 1280, 820 and 390 px in light and dark; console
errors, failed requests and horizontal overflow on every page; what each page's controls do (the Try chips, the
counter and the inline palette, the search lab, the theming controls, registering 2,000 commands then measuring and
virtualizing them, the source call log, the docs search, drawer, rail, pager and copy buttons, every flow on /pages);
the macOS key path with the platform faked; and the clipboard.

**Found and fixed**

| # | Where | Defect | Fix |
|---|---|---|---|
| 1 | WebKit (Safari), Firefox on macOS | A click does not focus the button it lands on - the focus goes to its nearest focusable ancestor, here `<main tabindex="-1">` - so closing the palette opened from the trigger, a mode button or any page button gave the focus to that ancestor, and the next `Tab` started from the top of the page. | The browser layer remembers the control the last pointer press landed on, and gives the focus back to it when the focus sits on its ancestor or nowhere. |
| 2 | Every engine | The demo's `data-palette-ready`, which the suite waits for before pressing `Ctrl+K`, followed `IBaletteService.PlatformDetected` - which every palette raises. On /commands the inline palette often started first while the modal palette's key listener was 3-50 ms away: a `Ctrl+K` pressed then was lost (4 of 6 WebKit loads, 2 of 6 Chromium ones). | The dialog and the inline section carry `data-ready` once their own browser layer listens for keys; the suite waits for it on every palette of the page (0 of 20 loads early since), and the demo's flag is gone. |
| 3 | Demo | "Copy share link" (`Mod+Shift+C`) said "Link copied to clipboard" and copied nothing. | It writes the page's address to the clipboard, and answers with `Error` when the browser refuses; it writes in all three engines, from the palette and from the chord. |
| 4 | Demo | Applying one label said "1 labels applied". | Singular for one, in the demo, its sample on /pages and the README. |
| 5 | Docs | The Theming page and the README gave `--bit-balette-fg-muted` as `#6b7280` (it is `#5f6673`), the README `-fg-subtle` as `#9ca3af` (`#6b7280`) and `-danger` as `#dc2626` (`#c81e1e`); the trigger's fallback colour was the old one. | Corrected. |
| 6 | Blazor Server, every engine | Keys typed faster than the circuit's round trip were lost: the render answering one key wrote its older value into the input over the keys typed since ("theme" became "teme", "nuget" "nuet"). Firefox and WebKit, which dispatch keys closer together, showed it on localhost (2 of 5 Firefox runs); over a real network's latency any browser would. The input was `value` + `@oninput`, and only a binding tells the renderer what the field holds when each key's event arrives. | `@bind` with `@bind:event="oninput"` and `@bind:after`; 0 of 20 runs lose a key since, and a new test types a long query. The demo's two other text fields - the site's docs search and the search lab - had the same pattern and are bound too. |

**The suite in other engines** (its own assumptions, not palette defects)

- axe-core through `Deque.AxeCore.Playwright` ran the test process out of memory under WebKit and closed the browser -
  its partial run hands each frame's results to a page of its own. axe now runs inside the page under test
  (`Infrastructure/Axe.cs`, with the script `Deque.AxeCore.Commons` bundles): the same rules and results.
- A browser that a test crashed is launched again for the next test, so one crash fails one test.
- `AccessibilityTreeTests` reads Chromium's accessibility tree through the DevTools protocol: Inconclusive elsewhere.
- Firefox computes a 2px outline as 1.83333px (it snaps to device pixels), so the forced-colours test takes 1.5-2px -
  and now also checks the outline is the system `Highlight`.
- Headless Chromium refuses a clipboard write without the permission; the test that copies grants it.
- Playwright's Firefox driver can miss the end of a client-side navigation on Blazor Server: `WaitForURLAsync` timed out
  with the page already on the new address and rendered. The tests poll the URL instead (`ExpectUrlAsync`).
- On Blazor Server a click's effect renders a round trip later. The page walk looked for an open palette right after
  each click, found none, and clicked the docs search's own Close button next; it now waits until the page has been
  still for 150 ms after a click, and for the search's input to have the focus before its `Esc`.
- New: a page button that opened the palette gets the focus back (fix 1); keys typed faster than the palette renders
  are all kept (fix 6); `data-ready` is rendered (bUnit).

**Verified**: E2E, 89 tests in each of the six combinations - Chromium 89 passed, Firefox and WebKit 84 passed with
the 5 DevTools tests inconclusive - on WebAssembly and on Blazor Server alike (before the fixes, Firefox on Server failed
13 and WebKit on Server 8). The page walk, which had raced on Server, passed 33 of 33 since in WebKit. Every demo page
in every engine at 1280, 820 and 390 px, light and dark: no console error, no failed request, no horizontal overflow
(the only error is the 404 of the not-found page, on purpose). The page scripts: 12 of 12 in each engine.
`Bit.Balette.Tests` 828 passed; MCP 92 passed; `dotnet build -c Release` of the library: 0 warnings.

**Seen and left**: Firefox logs on every page that the WebAssembly runtime's legacy `try` instruction is deprecated -
that is .NET's `dotnet.native.wasm`, not this code. A closing toast from a command run by its shortcut with the
palette closed shows nothing, as documented ("a toast that stays open" opens it); the demo's activity log is the
feedback there. The site kit's `Layout/CommandPalette.razor` is the same file in every Utilities demo; only this copy
is bound (fix 6), as only this demo runs on Blazor Server - on WebAssembly a key's render lands before the next key.

## Where it stands

Ten iterations, the mission's limit, and an eleventh across browser engines. Every P0 and P1 finding is fixed and
verified, and so are the P2 and P3 ones except those below.

**Definition of done**

- Audit of the implementation and of cmdk, kbar, GitHub, VS Code, Raycast and Blazor alternatives: done (iteration 2).
- Search and ranking: normalization, phrases, alternatives, typo fallback, nested search; unit-tested and benchmarked.
- Registration, nested workflows, shortcuts, execution: conflicts and duplicates reported, lifecycle-safe, tested.
- Async sources and large collections: timeouts, retries, paging, page sources, snapshot caching; benchmarked to
  20,000 commands.
- Accessibility, keyboard, focus, localization, RTL: axe-core clean in every audited state, and the accessibility tree
  Chromium hands screen readers asserted directly; no manual screen reader pass (below).
- Theming: tokens documented; forced colours, more contrast, density, inline.
- Hosting: WebAssembly and Blazor Server both driven in Chromium, Firefox and WebKit; Hybrid and .NET 8/9 by the unit
  suite.
- Security, privacy, lifecycle, errors: URL schemes, generic error text, sanitized history, clearing recents from the
  palette, guarded interop.
- Tests: 828 unit and component, 89 browser tests in each of three engines and two render modes, 92 MCP.
- Documentation and demo: README, package README, benchmark and E2E READMEs, demo pages, the E2E workflow.

**Remaining gaps**

| Pri | Gap | Why it remains |
|---|---|---|
| P2 | No manual screen reader pass (NVDA, JAWS, VoiceOver, TalkBack). The accessibility tree is asserted, but how each reader phrases and times it - and VoiceOver's known looseness with `aria-activedescendant` - only testing with them settles. | Needs the readers and people who use them. |
| P3 | Blazor Hybrid is not driven in a browser suite. | Needs a MAUI, WPF or WinForms host; the palette's code paths are the WebAssembly ones there. |
| P3 | On Blazor Server, keys typed faster than the circuit's round trip right after `Enter` opens a page can be read as typed on that page (seen as a race in a test, not reproduced by hand). | A property of Blazor Server's input model; the browser layer cannot know whether `Enter` will open a page. |
| P3 | Literal operators (`'x`, `^x`, `x$`) cost about 0.1 µs more per command than before normalization (measured twice). | The term-group loop and a per-field ASCII check; removing it would mean caching text properties on every command. |
| P3 | `BaletteStrings.Other` and `ModeHintTitle` are public and unused by the palette (documented as kept for templates). | Removing public members is a breaking change. |
