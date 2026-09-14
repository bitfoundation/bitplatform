# Bit.Butil hosting tests

In-process HTTP tests (`WebApplicationFactory`) against `Bit.Butil.Tests.Harness.Web` - the Blazor Web App that
serves the `Bit.Butil.Samples.Core` pages in any render mode - on every target framework the library ships for.
No browser: these assert what only a server response shows, and run in the regular CI job
(`.github/workflows/bit.ci.Butil.yml`). The browser, hybrid and trimmed-publish coverage of the same host is
`Bit.Butil.Tests.E2E`.

| Fixture | What it proves |
| --- | --- |
| `PrerenderSweepTests` | Every public member of every `[ButilService]` class, called during a static render (`/prerender-sweep`, see `PrerenderSweep.cs` in the harness host), neither throws nor hangs. Argument validation (`ArgumentException`) and exception types Bit.Butil declares itself count as deliberate; a `NullReferenceException`, a `JsonException` or a call that waits on JavaScript that will never answer is a call site missing the prerender guard. Members whose parameters cannot be made up (a `MediaStreamHandle` only the library creates) are skipped and listed in the test output. A new member is swept with no list to update. |
| `SamplePagesHostingTests` | Every `@page` in Samples.Core (found by reflection) renders statically with a 200 and nothing logged at Error in SSR, Server, WebAssembly and Auto. |
| `HarnessHostingTests` | The harness pages are prerendered where the mode prerenders and report `starting` rather than `ready` there; non-prerendered modes send only the component marker; the bundle is on the page in bundle mode and absent in lazy mode; `bit-butil.js`, the per-module files, and the worker/frame/stream documents the harness pages load are served as themselves. |

## Running

From inside `src` (so `src/global.json` selects the test runner):

```powershell
dotnet test --project tests\Bit.Butil.Tests.Hosting\Bit.Butil.Tests.Hosting.csproj            # net8.0, net9.0 and net10.0
dotnet test --project tests\Bit.Butil.Tests.Hosting\Bit.Butil.Tests.Hosting.csproj -f net10.0 # one framework
```

Each framework needs its runtime installed to run.
