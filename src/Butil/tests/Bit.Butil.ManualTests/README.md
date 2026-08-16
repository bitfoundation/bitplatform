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

**Interop contract** ([`InteropContract.cs`](InteropContract.cs)). Registration only settles who gets
*registered*. Two other things are resolved by name at runtime and would fail silently in the browser:

- **`[JSInvokable]` callbacks**, which JS dispatches by method name through a `DotNetObjectReference` -
  including ones on internal types a consumer never names.
- **JSON payload types**, whose constructors and properties `System.Text.Json` reflects over, so a
  trimmed-away property becomes a silently null field rather than an error.

The untrimmed run captures both into `interop-manifest.txt`; the trimmed run checks the trimmed
assembly against it. Types the trimmer removed entirely are skipped - that is the feature working. Only
a type that **survived while losing members it is reflected over** counts as a defect.

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

Without a manifest the interop check reports `SKIPPED` instead of failing, so a bare publish-and-run
still gives you the registration half.

## Reference results

| | untrimmed | trimmed |
| --- | --- | --- |
| `Bit.Butil.dll` | 612,352 bytes | 109,056 bytes |
| types in assembly | 784 | 140 |
| `[ButilService]` discovered / registered | 57 / 57 | 5 / 5 |
| interop contract | 41 types captured | 9 checked, 32 trimmed away, 0 problems |

The trimmed run keeps `DomEventsInterop` with all 11 `[JSInvokable]` methods and
`GeolocationCoordinates` with all 7 properties - neither is named anywhere in this project's code.

Injecting fewer services shrinks it further: with only `LocalStorage`, `Clipboard` and `Cookie` the
assembly comes out at 30,720 bytes and 36 types.

## What a failure means

- **`X survived with no public constructor`** - `ButilServiceAttribute` lost the
  `[DynamicallyAccessedMembers(PublicConstructors)]` annotation on its type argument. The class stays
  in the app but DI can no longer activate it, which shows up in consumer apps only after publishing.
- **`X.Y is [JSInvokable] but was trimmed away while its type survived`** - a JS callback would dispatch
  to a method that no longer exists. Usually means a `DotNetObjectReference` was created through a path
  that does not carry the `PublicMethods` annotation.
- **`X.Y is part of a JSON interop payload but was trimmed away`** - an `Invoke<T>` overload lost its
  `LinkerFlags.JsonSerialized` annotation on `T`; the property would deserialize as null/default.
- **`X is marked [ButilService] but was not registered`** - discovery in `AddBitButilServices` and the
  attribute have drifted apart.
- **`X is marked [ButilService(typeof(Y))]`** - copy-paste slip; the argument has to be the decorated
  type itself or the wrong service gets registered.
- **`unused services survived trimming`** - something re-introduced a static reference to the whole
  service set (a hard-coded `AddScoped<T>()` list, a `Type[]` of all services, a `switch` over them).
  That is the regression this harness is guarding against.
- **`X is used by ConsumerComponent but did not survive trimming`** - a used service is being dropped,
  which would break consumers outright.

`RESULT: PARTIAL` means only some of the unused services were trimmed - read the list, it is a real
finding rather than a harness glitch.
