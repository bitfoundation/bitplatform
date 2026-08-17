# Bit.Butil.ManualTests

A hand-run harness that answers two questions about a trimmed consumer of `Bit.Butil`:

1. Does it only pay for the Butil classes it actually uses?
2. Do the pieces the library reaches **by name at runtime** survive that trimming?

It is a console app rather than a test project on purpose. Trimming is a *publish* step, so the thing
under test is the produced output, and the same executable is both the report and the check - it exits
non-zero when the outcome does not match what it expects.

## What it exercises

[`ConsumerComponent`](ConsumerComponent.cs) stands in for a consumer's Blazor component. It injects
`LocalStorage`, `Clipboard`, `Cookie`, `Window` and `Geolocation` through `[Inject]` properties and
resolves them through the **non-generic** `GetRequiredService(Type)` - the same shape razor's `@inject`
produces. That detail is the whole point: `@inject` references a service's *type* and never its
*constructor*, and `GetRequiredService<T>()` would annotate the type argument with `PublicConstructors`
and preserve the constructor by itself, quietly hiding the failure this project exists to catch.

Its calls deliberately cross the interop boundary in both directions: DTO-returning APIs
(`Cookie.GetAll`, `Geolocation.GetCurrentPosition`, `Window.GetLocationBar`) and a DOM subscription
(`Window.SubscribeEvent`), which drags in the internal `DomEventsInterop` and its `[JSInvokable]`
callbacks. Everything else in Bit.Butil is untouched, so a trimmed publish should drop it.

## The two checks

**Registration.** Every `[ButilService]` class that survives must be registered and must still have a
public constructor; the services nothing references must be gone.

That check starts from the attribute, so on its own it is blind to a service class that never got one -
the report would happily say "57 of 57 registered" while consumers hit *Cannot provide a value for
property* at runtime. So the harness also looks for service classes by **shape**: a public, constructible
class taking an `IJSRuntime`. Any such class without `[ButilService]` is a failure. (`ButilStorage` is
excluded: it takes an `IJSRuntime` too, but it is the shared base of `LocalStorage` and `SessionStorage`
rather than a service in its own right.)

**Interop contract** ([`InteropContract.cs`](InteropContract.cs)). Registration only settles who gets
*registered*. Two other things are resolved by name at runtime and would fail silently in the browser:

- **`[JSInvokable]` callbacks**, which JS dispatches through a `DotNetObjectReference` by the *identifier*
  on the attribute - `[JSInvokable(InvokeMethodName)]`, as most of Bit.Butil writes it - falling back to
  the method name where the attribute sets none. Including ones on internal types a consumer never names.
- **JSON payload types**, whose constructors and properties `System.Text.Json` reflects over, so a
  trimmed-away property becomes a silently null field rather than an error.

The untrimmed run captures both into `interop-manifest.txt` - along with the roster of `[ButilService]`
names - and the trimmed run checks the trimmed assembly against it. Types the trimmer removed entirely
are skipped - that is the feature working. Only a type that **survived while losing members it is
reflected over** counts as a defect.

Members are captured and verified *including inherited ones*, because interop sees a type whole: JS
dispatches an inherited `[JSInvokable]` method just the same, and `System.Text.Json` serializes a
payload's inherited properties along with its own.

Callbacks are verified through the attribute rather than the method name, so a method that survives while
its `[JSInvokable]` does not counts as a defect - `JSInterop` resolves the attribute, and a name alone is
not callable from JS. The manifest ends with an `@count|N` record giving the number of type contracts
written; a file that fails to parse, or that reads back short of what it declares, is rejected outright
rather than read partially, since the contracts that went missing are exactly the ones worth checking.

## How it knows which run it is

From `trimmed-publish.marker`, which the csproj copies to the **publish** output only (never to the build
output `dotnet run` executes) and only when `PublishTrimmed` is on. Inferring the mode from the assembly
instead - "some of the expected services are missing, so this must be trimmed" - cannot tell a trimmed
build from a name in `MustSurvive` that went stale after a rename, and getting that wrong turns the
checks into no-ops. `MustSurvive` is itself validated against the real service roster (the assembly's own
when untrimmed, the manifest's when trimmed), so a stale name is reported as a stale name.

Payload types are named explicitly in `ConsumerComponent.ExercisedPayloadTypes` (and expanded
transitively through their own properties) rather than inferred from every public signature. Inferring
over-reports badly: `ScrollOptions` and `WindowFeatures` belong to `Window` methods this project never
calls, so the trimmer strips them to shells - correct behaviour that looks like a defect. Only a type on
a genuinely exercised code path can be asserted on.

## Running it

Run both from this folder, so they share the manifest:

```bash
# untrimmed: all 57 [ButilService] classes present; writes interop-manifest.txt
dotnet run -c Release

# trimmed, TrimMode=full (what Blazor WebAssembly uses); checks against the manifest
dotnet publish -c Release
./bin/Release/net10.0/<rid>/publish/Bit.Butil.ManualTests
```

The untrimmed run has to come first: a trimmed run with no usable manifest to compare against **fails**,
because it would otherwise report `PASS` having verified none of the interop contract - and a manifest
read only partly would report `PASS` having verified less of it than the output claims.

## Reference results

| | untrimmed | trimmed |
| --- | --- | --- |
| `Bit.Butil.dll` | 612,352 bytes | 109,056 bytes |
| types in assembly | 784 | 140 |
| `[ButilService]` discovered / registered | 57 / 57 | 5 / 5 |
| interop contract | 43 types captured | 10 checked, 33 trimmed away, 0 problems |

The trimmed run keeps `DomEventsInterop` with all 11 `[JSInvokable]` methods and
`GeolocationCoordinates` with all 7 properties - neither is named anywhere in this project's code.

Injecting fewer services shrinks it further: with only `LocalStorage`, `Clipboard` and `Cookie` the
assembly comes out at 30,720 bytes and 36 types.

## What a failure means

- **`X survived with no public constructor`** - `ButilServiceAttribute` lost the
  `[DynamicallyAccessedMembers(PublicConstructors)]` annotation on its type argument. The class stays
  in the app but DI can no longer activate it, which shows up in consumer apps only after publishing.
- **`X.Y is [JSInvokable] but no longer resolves while its type survived`** - a JS callback would dispatch
  to nothing, because either the method or its attribute is gone. Usually means a `DotNetObjectReference`
  was created through a path that does not carry the `PublicMethods` annotation.
- **`X.Y is part of a JSON interop payload but was trimmed away`** - an `Invoke<T>` overload lost its
  `LinkerFlags.JsonSerialized` annotation on `T`; the property would deserialize as null/default.
- **`X is marked [ButilService] but was not registered`** - discovery in `AddBitButilServices` and the
  attribute have drifted apart.
- **`X is marked [ButilService(typeof(Y))]`** - copy-paste slip; the argument has to be the decorated
  type itself or the wrong service gets registered.
- **`X looks like a Butil service ... but carries no [ButilService]`** - a new service class was added
  without the attribute. Nothing registers it, and the only symptom otherwise is a consumer's runtime
  *Cannot provide a value for property*.
- **`X is an expected Butil service name but ...`** - a name in `MustSurvive` no longer matches a service
  (usually a rename). Fix the list, or the checks built on it are asserting nothing.
- **`the interop contract was not checked at all`** - the trimmed run found no `interop-manifest.txt`, or
  found one it could not read whole (a malformed line, or fewer contracts than its `@count` record claims).
  Run `dotnet run -c Release` from the project folder first, then re-run the published executable there.
- **`services nothing in this project references survived trimming`** - a `[ButilService]` class outside
  `MustSurvive` is still in the trimmed assembly. Usually something re-introduced a static reference to
  the whole service set (a hard-coded `AddScoped<T>()` list, a `Type[]` of all services, a `switch` over
  them); otherwise `ConsumerComponent` gained a reference without `MustSurvive` being updated to match.
  That is the regression this harness is guarding against.
- **`X is used by ConsumerComponent but did not survive trimming`** - a used service is being dropped,
  which would break consumers outright.
