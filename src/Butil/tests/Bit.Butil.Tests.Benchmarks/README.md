# Bit.Butil.Tests.Benchmarks

The performance suite. It answers two questions that the other test projects deliberately do not:

1. **How many bytes does an app download for each module it calls into?** `build.mjs` already enforces a
   line budget on the TypeScript sources, but that is a proxy - this is the number itself, minified and
   compressed, including the dependencies a module's lazy-loaded file inlines.
2. **What does a call through Bit.Butil actually cost inside a browser** - and does the rate limiting on
   high-frequency subscriptions remove the traffic it claims to?

It is a console app rather than a test project, for the same reason `Bit.Butil.Tests.Manual` is: the
subject is a build artifact and a deployed app, not a method. The report it prints is the point, and it
exits non-zero when a measurement is outside its budget, so the same executable serves as the thing you
read and the thing CI runs.

## Running it

```bash
dotnet run                # both halves
dotnet run -- --weight    # module download weight only: no browser, exactly reproducible
dotnet run -- --runtime   # interop cost and rate limiting only: boots the sample app, drives a browser
```

The weight half needs the Bit.Butil JavaScript build to have run at least once (it reads the chunks and
manifest `build.mjs` writes under `Bit.Butil/obj/butil-js/`), and it minifies them with the same esbuild
the Release build uses - resolved out of `Bit.Butil/node_modules`, so the two agree by construction.

The runtime half boots `Bit.Butil.Samples.Web` in Release as a child process and drives its `/benchmark`
page with Playwright. Environment variables:

| Variable | What it does |
| --- | --- |
| `BUTIL_BENCH_BASE_URL` | Measure an already-running deployment instead of booting one - which is what you want when the figure you care about is a published, minified build |
| `BUTIL_E2E_CHANNEL` | Browser channel to launch, e.g. `chrome` / `msedge` (shared with the E2E suite) |
| `BUTIL_E2E_EXECUTABLE` | Path to a chromium-family executable |
| `BUTIL_E2E_HEADED` | Set to `1` to watch the run |

## What it measures, and where from

The timings are taken **in .NET, on the page** ([`BenchmarkPage.razor`](../../Samples/Bit.Butil.Samples.Core/Pages/BenchmarkPage.razor)),
not out in the runner. The round trip is the thing being measured, and timing it from Playwright would put
the automation channel inside every figure. The page emits one machine-readable line per measurement -
`name|iterations=…|totalMs=…|perOpUs=…` - and the runner parses those and holds them against
[`Budgets.cs`](Budgets.cs). Each measurement runs a warm-up pass that is not counted, so a module's
lazy-load import and the JIT of the generic interop machinery do not swamp the steady-state number.

| Measurement | Why it is here |
| --- | --- |
| `invoke-value` | A value-returning round trip with a deserialized result - the shape most Butil calls have |
| `invoke-void` | The same without a result |
| `invoke-fast` | The synchronous in-process path (`FastInvoke`), which only exists under WebAssembly |
| `element-read` | A read through an `ElementReference`, which additionally marshals the element |
| `dom-handle` | A `Dom.Query` plus a read through the handle - two round trips, and the handle registry |
| `payload-*` | 1 KB / 64 KB / 1 MB in **both** directions, through `TextEncoding.Encode` |
| rate limiting | The same burst of events fired at a gated and an ungated subscription, for both a DOM event and a `ResizeObserver` |
| lazy scripts | The same value call again with `UseLazyScripts` on, so the loader that guards every call in that mode is measured rather than assumed |

## Reading a failure

The two kinds of budget are not equally trustworthy, and the report says so when it fails.

**A weight failure is real.** The same sources minify to the same bytes on every machine. Raising one of
those budgets is a decision to ship more JavaScript to every app that touches the module - make it
deliberately, not to turn a red run green. The usual cause is a module gaining a dependency: `utils` is
reached by nearly everything, so a few bytes there move the p90 for the whole library at once.

**A timing failure usually is not.** Those ceilings are set an order of magnitude above a healthy run
precisely so that only a call shape that stopped being a single round trip can cross them; a figure that
drifted from 60 to 90 microseconds is the machine. Re-run before believing it.

The assertions worth trusting on any machine are the **ratios**, because both halves are measured in the
same run on the same box: the fast path beating the async path, and a gated subscription producing less
traffic than an ungated one over an identical burst. The two gate ratios are held differently on purpose -
a DOM event burst is not frame-bound and the reduction comes out in the hundreds, while a `ResizeObserver`
already delivers at most once a frame, so the most a gate can do there is `interval / frameTime`. The
runner measures that frame time on the same burst and asks for a share of what it allows, so a slow CI box
pacing frames at 30 fps is held to the ceiling it actually had rather than to a 60 Hz one.

## What it does not cover

Correctness. That the trailing send still delivers the settled value - the half of the rate limit a
traffic measurement cannot see - is checked by `Bit.Butil.Tests.E2E`
(`ResizeObserver_RateLimited_Still_Delivers_The_Settled_Size`), which runs on every build rather than only
when someone asks for numbers.
