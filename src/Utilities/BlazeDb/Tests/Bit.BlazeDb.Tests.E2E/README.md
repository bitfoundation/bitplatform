# Bit.BlazeDb end-to-end tests

The engine and EF Core tests in `Bit.BlazeDb.Tests` run the database without a browser, over the in-memory
and file backends. That covers the engine well and leaves out everything that only exists in a real browser:

- the browser backends' ES modules (`Bit.BlazeDb.Browser`) never execute, so nothing proves that OPFS writes are atomic and appends
  are appends, that IndexedDB's read-modify-write appends and its no-create open work, that WebCrypto seals
  and opens a frame, or that a checkpoint announcement reaches the other tabs;
- there is no Web Locks election, so nothing proves that a second tab of the same origin loses it, or that a
  closed tab gives it up;
- there is no WebAssembly runtime, so the `[JSImport]` bindings are never called;
- the demo pages, which drive a real database with real application code, never run.

This suite boots the demo (`Bit.BlazeDb.Demo.Server`, which serves the client and runs every page on
WebAssembly without prerendering) and drives it with Playwright - in Chromium by default, in Firefox or WebKit
when `BLAZEDB_E2E_BROWSER` says so.

## What it proves

| Suite | What it covers |
|---|---|
| `PersistenceTests` | A row inserted on OPFS survives a reload, read back out of the write-ahead log; a second tab of the same origin loses the Web Lock and falls back to memory instead of writing the same files; a closed tab releases the lock for the next one. |
| `BrowserModuleTests` | The four modules the package ships, called the way the `[JSImport]` bindings call them: IndexedDB and OPFS writes, appends, reads and deletes, and their no-create opens; the Web Lock won once and handed over on release; AES-GCM sealing through WebCrypto, with a tampered frame and a frame moved to another file both refused; a BroadcastChannel announcement reaching the other tab and not echoing back. |
| `ChangeFeedTests` | The `/changes` page on WebAssembly: each commit reported once in the database's change feed, a rolled-back transaction and an insert-then-delete reported not at all, and a live query re-running only for the commits that concern it. |
| `DemoPageTests` | Every page of the site renders through the navigation without a console error or the error UI; a deep link opens its page; an unknown route answers 404 with the site's own page; the host page carries the boot screen and nothing prerendered; the modules are served as static web assets. |
| `DemoScenarioTests` | Every button of every interactive page pressed without a console error or the error UI (the benchmark run aside); the EF Core scenarios report what they describe - an empty tracker under `AsNoTracking`, a duplicate key surfacing as `DbUpdateException` with nothing kept, `Find` and `Include` working in the trimmed client; every case on the errors page behaving as documented, in the runtime's own words rather than resource keys; the durability scenarios reaching their outcome even on a log with no records yet, and a self-checkpoint noticed by the snapshot's name; the date ranges opening on rows of the tab's database, however old it is. |
| `FeatureDemoTests` | The pages that show one feature each reach the outcome they describe: every scenario of the writes, transactions, EF Core and backups pages without a step reporting "unexpected"; an encrypted database opened with its key and refused with any other, a flipped byte and a torn frame told apart; a replica tab following the writer's announced checkpoints and refusing writes; a flush past the storage quota refused, retried by the flusher and completed once space is freed; a stream yielding between batches and refusing a write made meanwhile; a failing change handler surfacing after its commit. |

The tests that need a writable Origin Private File System (the OPFS module test and `PersistenceTests`) end
**Inconclusive** in a browser that gives a Playwright context none: WebKit's contexts are ephemeral, like a
private window, and refuse `navigator.storage.getDirectory()`, so the demo runs there on its in-memory
fallback - which every other test still drives.

Nothing is prerendered, so the app replacing the boot screen is the app running. Each test then waits
until the demo database is open - `data-storage` on the storage badge (`.bd-status`, mounted beside the
router on every page) turns from `Opening` to where it ended up (`Opfs`, or one of the in-memory
fallbacks) - before it touches the page.

## Running locally

From `src/Utilities/BlazeDb` (inside `src`, so `src/global.json` selects the Microsoft.Testing.Platform runner):

```bash
# The suite runs the demo server's build output: build it first, in the configuration the tests run in.
dotnet build Demo/Server/Bit.BlazeDb.Demo.Server.csproj
dotnet build Tests/Bit.BlazeDb.Tests.E2E/Bit.BlazeDb.Tests.E2E.csproj

# Once per machine (and after a Playwright update): the browsers Playwright drives.
pwsh Tests/Bit.BlazeDb.Tests.E2E/bin/Debug/net10.0/playwright.ps1 install chromium firefox webkit

cd Tests/Bit.BlazeDb.Tests.E2E && dotnet test

# One test class, or one test
dotnet test --filter "FullyQualifiedName~.BrowserModuleTests."
dotnet test --filter "FullyQualifiedName~survives_a_reload"

# Another engine
BLAZEDB_E2E_BROWSER=firefox dotnet test
```

A build output is not what ships, though: only a publish trims the WebAssembly client, and whatever the trimming
breaks - a member something finds by reflection - works in every build and fails only there. CI therefore runs
the suite against the demo published the way `blazedb.demo.cd.yml` publishes it, once per engine, in
`.github/workflows/bit.ci.BlazeDb.e2e.yml`, which only runs when started by hand (Actions > "bit platform CI -
BlazeDb E2E" > Run workflow). The pull-request pipeline, `bit.ci.BlazeDb.yml`, runs the build, engine, EF Core
provider and MCP tests only. To do what CI does:

```bash
dotnet publish src/Utilities/BlazeDb/Demo/Server/Bit.BlazeDb.Demo.Server.csproj -c Release -o demo -p:InvariantGlobalization=true
dotnet build src/Utilities/BlazeDb/Tests/Bit.BlazeDb.Tests.E2E/Bit.BlazeDb.Tests.E2E.csproj -c Release
pwsh src/Utilities/BlazeDb/Tests/Bit.BlazeDb.Tests.E2E/bin/Release/net10.0/playwright.ps1 install --with-deps chromium
(cd demo && dotnet Bit.BlazeDb.Demo.Server.dll --urls http://localhost:5000 &)
cd src/Utilities/BlazeDb/Tests/Bit.BlazeDb.Tests.E2E && BLAZEDB_E2E_BASE_URL=http://localhost:5000 dotnet test -c Release --no-build --report-trx --report-trx-filename blazedb-e2e.trx
```

Without PowerShell 7, the browser can be installed with the Node.js runtime Playwright ships:
`bin/Debug/net10.0/.playwright/node/<platform>/node bin/Debug/net10.0/.playwright/package/cli.js install chromium`.

The suite starts the demo server itself (`dotnet Bit.BlazeDb.Demo.Server.dll --urls http://localhost:<free port>`,
in the Development environment so the build output finds its static web assets), waits until it answers,
shares it between all tests and stops it when the run ends. `localhost` rather than `127.0.0.1`: OPFS, Web
Locks and WebCrypto exist only in a secure context, which `http://localhost` is. Each test gets a browser
context of its own - and with it an Origin Private File System, an IndexedDB and Web Locks of its own. The
tests run one at a time: a second page booting WebAssembly next to a test that waits on a background flush is
exactly the load that turns the wait into a flake.

When no browser can be launched or the demo does not start, every test reports **Inconclusive** with the
reason. On a build server (`CI=true`) the same tests **fail** instead, so a suite that never ran cannot pass.

### Environment variables

| Variable | Effect |
|---|---|
| `BLAZEDB_E2E_CONFIGURATION` | Build configuration of the demo server to boot; defaults to the test assembly's own. |
| `BLAZEDB_E2E_BASE_URL` | Drive an already running demo (a published one, or one started from the IDE) instead of booting one. |
| `BLAZEDB_E2E_BROWSER` | The engine: `chromium` (the default), `firefox` or `webkit`, as Playwright installs them. |
| `BLAZEDB_E2E_CHANNEL` / `BLAZEDB_E2E_EXECUTABLE` | Use an installed Chrome/Edge (`chrome`, `msedge`; Chromium only) or a specific binary of the engine. Without either, the browser Playwright installed is used - for Chromium falling back to an installed Chrome, then Edge. |
| `BLAZEDB_E2E_HEADED=1` | Show the browser. |
