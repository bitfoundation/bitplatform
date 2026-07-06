# Bit.Brouter route-scalability benchmark

A dedicated console harness for the one scalability question the code review raised:

> Every route in Brouter is a live `Broute` `ComponentBase` mounted in the render tree (including the
> synthetic ones emitted per attribute-discovered route). Unlike the built-in Blazor `Router` - which
> keeps routes as a `RouteTable` (data) and instantiates only the *matched* component - an app with
> several hundred pages keeps several hundred `Broute` components permanently alive. Worth benchmarking
> against the built-in router at 200-500 routes before calling it production-ready for large apps.

This project measures that cost so the trade-off is backed by numbers, not intuition.

## What it compares

| Scenario | What it renders | Models |
|----------|-----------------|--------|
| **A - Brouter** | `<Brouter>` with *N* hand-declared `<Broute>` children | The current Brouter model. A hand-declared `<Broute>` has the same per-route cost as the synthetic one Brouter emits per discovered route, so this measures the discovered-route case too. |
| **B - RouteTable** | *N* routes as a `template → type` dictionary; only the single matched component is instantiated | The built-in Blazor `Router` architecture (and the "lazy discovered routes" design we discussed). Scalable to any *N* without needing hundreds of real `@page` types. |

The **gap between A and B** is the instantiation / steady-state cost the review flagged.

> Why not benchmark the real built-in `Router`? It discovers routes by reflecting over an assembly for
> `[Route]` types, so driving it at 500 routes would require synthesizing 500 real routable types
> (`Reflection.Emit`). Scenario B reproduces its *architecture* (route table + instantiate-only-matched)
> faithfully and at arbitrary *N*, which is what the comparison needs.

## Running it

```bash
# From this folder. Release is required - a Debug build's numbers are meaningless.
dotnet run -c Release

# Custom route-count sweep (defaults to 50 100 200 500 1000):
dotnet run -c Release -- 100 250 500
```

It renders each scenario in an isolated [bUnit](https://bunit.dev) host (bUnit is used purely as a
lightweight Blazor renderer + fakes for `NavigationManager` / JS interop - not as a test framework),
over several trials after warmup, and reports the median.

## What it measures, per route count

- **render** - wall-clock to instantiate the tree and do the first match (ms).
- **alloc** - bytes allocated during that render (MB).
- **retained** - managed heap still held *after* the render while the rendered tree is kept alive (KB).
  This is the "permanently alive" cost the review is about.

Absolute values include a fixed bUnit renderer/host overhead present in **both** columns; the signal is
the **difference** between the columns and how it **scales** with route count.

## Indicative results

Captured on .NET 10, Release, a developer laptop. Treat as relative/indicative - absolute numbers are
machine-dependent; re-run locally for your own hardware.

| routes | Brouter render | Brouter retained | RouteTable retained | retained delta (A−B) | ~per route |
|-------:|---------------:|-----------------:|--------------------:|---------------------:|-----------:|
|     50 |         1.2 ms |          234 KB  |              76 KB  |             ~158 KB  |   ~3.2 KB  |
|    100 |         1.6 ms |          378 KB  |              80 KB  |             ~298 KB  |   ~3.0 KB  |
|    200 |         2.4 ms |          764 KB  |              91 KB  |             ~673 KB  |   ~3.4 KB  |
|    500 |         4.5 ms |         2623 KB  |             116 KB  |            ~2507 KB  |   ~5.1 KB  |
|   1000 |         7.8 ms |         5742 KB  |             163 KB  |            ~5579 KB  |   ~5.7 KB  |

### Takeaways

- **The review's concern is real and grows linearly.** Brouter's render time and retained memory both
  scale with route count; the RouteTable baseline stays essentially flat (only the string table grows).
- **But the magnitude is bounded.** Each live `Broute` costs on the order of **3-6 KB** of retained
  managed heap. At **500 routes** that's roughly **2.5 MB** extra memory and **~4 ms** extra startup;
  at **1000 routes**, ~5.6 MB and ~8 ms. Material for a very large all-`@page` app, negligible for a
  typical one (tens of routes).
- **This isolates instantiation cost, which the first-segment match index does not address.** Matching
  cost is a separate axis (and is already optimized); see the `GetRouteIndex` note in `Brouter.cs` and
  the "Performance & scalability" section of the main README.

Use these numbers to decide whether the "lazy discovered routes" optimization (instantiate only the
matched route, keep the rest as data) is worth the added matcher complexity **for your route counts** -
which is exactly the decision the review asked to defer until measured.
