# Bit.Bcrdt end-to-end tests

The tests in `Bit.Bcrdt.Tests` run the CRDT without a browser: merges, the sync handshake over the in-memory
transport and the relays, presence, persistence, undo, the text input binding behind the editor and the conflict
lab's procedure are all checked in .NET. That covers the logic well and leaves out everything that only exists in a
real browser:

- `bit-bcrdt.ts` never executes, so nothing proves that a keystroke in a real `<textarea>` becomes the minimal
  insertion it should, or that a remote edit lands in the control without overwriting what is being typed;
- the replicas never run on WebAssembly, where the side-by-side demos keep every replica of a document in the page's
  own memory, connected through the library's in-memory transport, and render their merges through
  `BcrdtComponentBase`'s coalesced re-renders;
- no browser ever reaches the demo server's relays, so neither the SignalR hub (`/hub/bcrdt`, through
  `Bit.Bcrdt.SignalR`) nor the WebSocket endpoint (`/ws/bcrdt/{documentId}`) is exercised by the WebAssembly clients
  that use them;
- the demo pages, which compose the library with real application code, never run.

This suite boots the demo (`Bit.Bcrdt.Demo.Server`, which prerenders the pages and runs them on WebAssembly) and
drives it in Chromium with Playwright. It asserts **effects**, not markup: what each replica's editor holds, what its
connection badge and presence list say, the raw state each replica projects as JSON under the panels, and which relay
a tab opened its socket to.

## What it proves

| Suite | What it covers |
|---|---|
| `CollaborativeTextTests` | Typing into one of the three replicas reaches the other two, and each replica lists the other two as present; a replica taken offline drops out of the others' presence lists and keeps only its own edit while the two still connected follow each other, and when it comes back all three converge on one text holding both concurrent sentences, each in one piece, with nothing lost or duplicated; "Clear the document (from Replica A)" empties all three, and the cleared document still syncs. |
| `CollaborativeListTests` | Items added on either replica appear on both; a tick, a move and a delete on one replica reach the other (the tick travelling with its item); with Replica B offline, a rename on A and a tick of the same item on B both survive the merge, and B's new item reaches A; both replicas project identical state. |
| `CollaborativeFormTests` | Title, owner, status and notes edited on one replica follow on the other; edits made while one replica is offline keep exactly one of the two titles (last writer wins, the same one on both) and both sentences of the notes (a nested text merges). |
| `ConflictLabTests` | Each of the six scenarios run from its button ends with the log's "The two replicas are identical." and the convergence verdict; concurrent insertion at the same index keeps both runs contiguous; the same map key keeps exactly one value and different keys keep both; the reset empties both replicas and the log. |
| `UndoTests` | A run of keystrokes is one undo step; Undo on A removes A's sentence from both replicas and leaves B's later sentence alone; Redo puts it back where it was; B's own Undo removes only B's sentence. |
| `InternalsTests` | "Type 500 characters" reaches the peer and is held in a handful of blocks on both replicas; the update log tells A's local edits from what B received; a character typed into B's editor reaches A; 200 scattered edits grow the block count; both replicas project the same document. |
| `LiveTests` | Two browser contexts (two people, two WebAssembly instances) on the same `/live/{room}` link meet through the SignalR hub, and with `?relay=websocket` through the WebSocket endpoint - each tab's socket goes to the relay the link names; each tab lists the other in its presence list; text typed in either reaches the other; a tab taken offline leaves the other's presence list, and when it comes back both tabs converge on both paragraphs. |
| `DemoPagesTests` | Every demo page (the routes are read from the `@page` directives under `Demo/Client/Pages`, and the expected `h1` from each page's source, so a new page is swept without being listed; the live page on both relays) answers 200, turns interactive, shows its heading, opens and connects its replicas, loads `bit-bcrdt.js` when it has an editor, and logs no console error, uncaught exception, failed request or error status, without showing the error banner; an unknown address answers 404 with the not-found page, from which the menu still navigates; `/live` without a room is sent to a fresh room, on the relay asked for. |

Every page is prerendered, so its heading and explanations are on screen before anything listens to a click, and
the replica panels are not there at all (the prerendered page shows an "Opening…" placeholder instead). Each test
waits until the page has rendered in the browser - the router's `FocusOnNavigate` has then moved the focus to the
page's `h1`, making it focusable with `tabindex="-1"`, which the prerendered markup never carries - and then until
the page's replica panels are there and every one of their connection badges reads "synced".

Every test also fails on any uncaught page error and on any console error in which Blazor reports an unhandled
exception, whatever the test was looking at: an exception that escapes a handler or a change notification can leave
every later edit unapplied, and the test would otherwise time out pointing at the merge rather than at the exception.

## Running locally

From `src/Bcrdt` (inside `src`, so `src/global.json` selects the Microsoft.Testing.Platform runner):

```bash
# The suite runs the demo server's build output: build it first, in the configuration the tests run in.
dotnet build Demo/Server/Bit.Bcrdt.Demo.Server.csproj
dotnet build Tests/Bit.Bcrdt.Tests.E2E/Bit.Bcrdt.Tests.E2E.csproj

# Once per machine (and after a Playwright update): the browser Playwright drives.
pwsh Tests/Bit.Bcrdt.Tests.E2E/bin/Debug/net10.0/playwright.ps1 install chromium

cd Tests/Bit.Bcrdt.Tests.E2E && dotnet test

# One test class, or one test
dotnet test --filter "FullyQualifiedName~.LiveTests."
dotnet test --filter "FullyQualifiedName~A_replica_taken_offline"
```

The workflow `.github/workflows/bit.ci.Bcrdt.e2e.yml` does the same in Release. It is started by hand
(`workflow_dispatch`, Actions > "bit platform CI - Bcrdt E2E" > Run workflow) only, since the suite takes far longer
than the build-and-unit pipeline in `bit.ci.Bcrdt.yml`, which runs the build, the unit tests, the MCP server tests
and the pack:

```bash
dotnet build src/Bcrdt/Demo/Server/Bit.Bcrdt.Demo.Server.csproj -c Release
dotnet build src/Bcrdt/Tests/Bit.Bcrdt.Tests.E2E/Bit.Bcrdt.Tests.E2E.csproj -c Release
pwsh src/Bcrdt/Tests/Bit.Bcrdt.Tests.E2E/bin/Release/net10.0/playwright.ps1 install --with-deps chromium
cd src/Bcrdt/Tests/Bit.Bcrdt.Tests.E2E && dotnet test -c Release --no-build --report-trx --report-trx-filename bcrdt-e2e.trx
```

Without PowerShell 7, the browser can be installed with Windows PowerShell
(`powershell -ExecutionPolicy Bypass -File bin/Debug/net10.0/playwright.ps1 install chromium`) or with the Node.js
runtime Playwright ships:
`bin/Debug/net10.0/.playwright/node/<platform>/node bin/Debug/net10.0/.playwright/package/cli.js install chromium`.

The suite starts the demo server itself (`dotnet Bit.Bcrdt.Demo.Server.dll --urls http://127.0.0.1:<free port>`,
in the Development environment so the build output finds its static web assets), waits until it answers, shares it
between all tests and stops it when the run ends. Rebuilding the demo while a run is in progress changes the
fingerprinted assets under the running server and fails the pages that boot afterwards, so rebuild between runs.
Each test gets a browser context of its own - its own WebAssembly instance, and with it its own in-memory network
and replica stores, so nothing one test typed is there when the next one opens a page - and each live test opens a
room no other test uses. The tests run one at a time: most of them race an edit in one replica against its arrival
in another, and a second page booting WebAssembly next to them is exactly the load that turns those waits into
flakes.

When no browser can be launched or the demo does not start, every test reports **Inconclusive** with the reason. On a
build server (`CI=true`) the same tests **fail** instead, so a suite that never ran cannot pass.

### Environment variables

| Variable | Effect |
|---|---|
| `BCRDT_E2E_CONFIGURATION` | Build configuration of the demo server to boot; defaults to the test assembly's own. |
| `BCRDT_E2E_BASE_URL` | Drive an already running demo (for example one started from the IDE) instead of booting one. |
| `BCRDT_E2E_CHANNEL` / `BCRDT_E2E_EXECUTABLE` | Use an installed Chrome/Edge (`chrome`, `msedge`) or a specific Chromium binary. Without either, the Chromium Playwright installed is used, falling back to an installed Chrome, then Edge. |
| `BCRDT_E2E_HEADED=1` | Show the browser. |
