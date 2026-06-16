# Bit.Butil end-to-end tests

NUnit + Microsoft.Playwright suite that boots `Bit.Butil.Demo.Web` (Blazor WASM) as a child process and exercises two deterministic harness pages. Uses the **Microsoft.Testing.Platform** runner (mandated by the repo `global.json`) via NUnit's MTP runner.

## First-time setup

```powershell
# Restore + build.
dotnet build .\Bit.Butil.E2ETests.csproj

# Install the Playwright-managed Chromium (skip if you'll use a system browser, see below).
pwsh .\bin\Debug\net10.0\playwright.ps1 install chromium
```

## Running

```powershell
dotnet test .\Bit.Butil.E2ETests.csproj
```

### Browser selection (environment variables)

The test base class manages Playwright directly and reads configuration from env vars (this is more reliable than runsettings under the MTP runner):

| Variable | Effect |
| --- | --- |
| `BUTIL_E2E_CHANNEL` | Launch an installed browser channel instead of the bundled Chromium - e.g. `chrome`, `msedge`. Handy when the Playwright download is blocked. |
| `BUTIL_E2E_EXECUTABLE` | Full path to a chromium-family executable. |
| `BUTIL_E2E_HEADED` | Set to `1` to watch the run in a visible window. |
| `BUTIL_E2E_BASE_URL` | Point at an already-running demo server (e.g. `https://localhost:5041`) instead of auto-launching one. |

Example - run against a system Chrome with no Playwright download:

```powershell
$env:BUTIL_E2E_CHANNEL = "chrome"
dotnet test .\Bit.Butil.E2ETests.csproj
```

## Layout

* `Infrastructure/DemoServerFixture.cs` - assembly-level `[SetUpFixture]` that starts the demo on a free TCP port and tears it down at the end of the run.
* `Infrastructure/ButilHarnessTestBase.cs` - self-managing Playwright base class (launch + context + page) that reads the env vars above.
* `Infrastructure/ButilPageTest.cs` / `ButilObserversPageTest.cs` - thin bases pinning each harness route.
* `*Tests.cs` - narrowly-scoped tests grouped by Butil surface.
* `ci/bit.ci.Butil.e2e.yml` - ready-to-merge GitHub Actions workflow (copy into `.github/workflows/`).

## Harness pages

Two deterministic pages live in `Bit.Butil.Demo.Core/Pages`:

* `/e2e` - storage (round-trip, typed JSON, removeItem, length/key/containsKey, clear), cookie, crypto (UUID, random bytes, SHA-256, AES-GCM, AES-CBC, HMAC, ECDSA, PBKDF2), performance.now, window (base64, inner size, secure context, matchMedia), document (title, visibility/charset/url), location (href + protocol/pathname/origin), history (pushState, replaceState + state, scrollRestoration).
* `/e2e-observers` - PerformanceObserver, performance mark/measure/getEntries/clearMarks, StorageManager, NetworkInformation, IntersectionObserver, ResizeObserver, MutationObserver, BroadcastChannel, IndexedDB, CacheStorage, Web Locks, Object URLs, CookieStore, navigator/userAgent/screen platform getters.

Both expose stable element ids and funnel results through a single `#status` element so test selectors stay simple.

## Why a custom harness page?

Real Butil pages (`/clipboard`, `/notification`, …) trigger permission prompts that can't be granted reliably in headless. The harness pages only exercise APIs that work with no user gesture and expose outputs through one stable `#status` element so the test selectors don't have to chase per-feature DOM.
