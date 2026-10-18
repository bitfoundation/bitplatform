# Bit.BlazeDb end-to-end tests

The engine and EF Core tests in `Bit.BlazeDb.Tests` run the database without a browser, over the in-memory
and file backends. That covers the engine well and leaves out everything that only exists in a real browser:

- `Bit.BlazeDb.Browser`'s ES modules never execute, so nothing proves that OPFS writes are atomic and appends
  are appends, that IndexedDB's read-modify-write appends and its no-create open work, that WebCrypto seals
  and opens a frame, or that a checkpoint announcement reaches the other tabs;
- there is no Web Locks election, so nothing proves that a second tab of the same origin loses it, or that a
  closed tab gives it up;
- there is no WebAssembly runtime, so the `[JSImport]` bindings are never called;
- the demo pages, which drive a real database with real application code, never run.

This suite boots the demo (`Bit.BlazeDb.Demo.Server`, which serves the client and runs every page on
WebAssembly without prerendering) and drives it in Chromium with Playwright.

## What it proves

| Suite | What it covers |
|---|---|
| `PersistenceTests` | A row inserted on OPFS survives a reload, read back out of the write-ahead log; a second tab of the same origin loses the Web Lock and falls back to memory instead of writing the same files; a closed tab releases the lock for the next one. |
| `BrowserModuleTests` | The four modules the package ships, called the way the `[JSImport]` bindings call them: IndexedDB and OPFS writes, appends, reads and deletes, and their no-create opens; the Web Lock won once and handed over on release; AES-GCM sealing through WebCrypto, with a tampered frame and a frame moved to another file both refused; a BroadcastChannel announcement reaching the other tab and not echoing back. |
| `DemoPageTests` | Every page of the site renders through the navigation without a console error or the error UI; a deep link opens its page; an unknown route answers 404 with the site's own page; the host page carries the boot screen and nothing prerendered; the modules are served as static web assets. |

Nothing is prerendered, so the app's shell replacing the boot screen is the app running. Each test then waits
until the demo database is open - `data-storage` on the shell turns from `Opening` to where it ended up
(`Opfs`, or one of the in-memory fallbacks) - before it touches the page.

## Running locally

From `src/BlazeDb` (inside `src`, so `src/global.json` selects the Microsoft.Testing.Platform runner):

```bash
# The suite runs the demo server's build output: build it first, in the configuration the tests run in.
dotnet build Bit.BlazeDb.Demo/Server/Bit.BlazeDb.Demo.Server.csproj
dotnet build Tests/Bit.BlazeDb.Tests.E2E/Bit.BlazeDb.Tests.E2E.csproj

# Once per machine (and after a Playwright update): the browser Playwright drives.
pwsh Tests/Bit.BlazeDb.Tests.E2E/bin/Debug/net10.0/playwright.ps1 install chromium

cd Tests/Bit.BlazeDb.Tests.E2E && dotnet test

# One test class, or one test
dotnet test --filter "FullyQualifiedName~.BrowserModuleTests."
dotnet test --filter "FullyQualifiedName~survives_a_reload"
```

CI does the same in Release in `.github/workflows/bit.ci.BlazeDb.e2e.yml`, which only runs when started by hand (Actions > "bit platform CI - BlazeDb E2E" > Run workflow). The pull-request pipeline, `bit.ci.BlazeDb.yml`, runs the build, engine, EF Core provider and MCP tests only:

```bash
dotnet build src/BlazeDb/Bit.BlazeDb.Demo/Server/Bit.BlazeDb.Demo.Server.csproj -c Release
dotnet build src/BlazeDb/Tests/Bit.BlazeDb.Tests.E2E/Bit.BlazeDb.Tests.E2E.csproj -c Release
pwsh src/BlazeDb/Tests/Bit.BlazeDb.Tests.E2E/bin/Release/net10.0/playwright.ps1 install --with-deps chromium
cd src/BlazeDb/Tests/Bit.BlazeDb.Tests.E2E && dotnet test -c Release --no-build --report-trx --report-trx-filename blazedb-e2e.trx
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
| `BLAZEDB_E2E_BASE_URL` | Drive an already running demo (for example one started from the IDE) instead of booting one. |
| `BLAZEDB_E2E_CHANNEL` / `BLAZEDB_E2E_EXECUTABLE` | Use an installed Chrome/Edge (`chrome`, `msedge`) or a specific Chromium binary. Without either, the Chromium Playwright installed is used, falling back to an installed Chrome, then Edge. |
| `BLAZEDB_E2E_HEADED=1` | Show the browser. |
