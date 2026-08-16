# Bit.Butil.ManualTests

A hand-run harness that answers one question: **does a consumer of `Bit.Butil` only pay for the Butil
classes it actually uses once trimming is on?**

It is a console app rather than a test project on purpose. Trimming is a *publish* step, so the thing
under test is the produced output, and the same executable is both the report and the check - it exits
non-zero when the outcome does not match what it expects.

## What it exercises

[`ConsumerComponent`](ConsumerComponent.cs) stands in for a consumer's Blazor component. It injects
`LocalStorage`, `Clipboard` and `Cookie` through `[Inject]` properties and resolves them through the
**non-generic** `GetRequiredService(Type)` - the same shape razor's `@inject` produces. That detail is
the whole point: `@inject` references a service's *type* and never its *constructor*, and
`GetRequiredService<T>()` would annotate the type argument with `PublicConstructors` and preserve the
constructor by itself, quietly hiding the failure this project exists to catch.

Everything else in Bit.Butil is untouched, so a trimmed publish should drop it.

## Running it

```bash
# untrimmed: all 57 [ButilService] classes present and registered
dotnet run -c Release

# trimmed, TrimMode=full (what Blazor WebAssembly uses)
dotnet publish -c Release
./bin/Release/net10.0/<rid>/publish/Bit.Butil.ManualTests
```

The report prints the size of the `Bit.Butil.dll` that ended up next to the app, how many types are
left in it, and every surviving `[ButilService]` class with its public constructor count.

## Reference results

| | untrimmed | trimmed |
| --- | --- | --- |
| `Bit.Butil.dll` | 612,352 bytes | 30,720 bytes |
| types in assembly | 784 | 36 |
| `[ButilService]` discovered / registered | 57 / 57 | 3 / 3 |

## What a failure means

- **`X survived with no public constructor`** - `ButilServiceAttribute` lost the
  `[DynamicallyAccessedMembers(PublicConstructors)]` annotation on its type argument. The class stays
  in the app but DI can no longer activate it, which shows up as a runtime error in consumer apps
  only after publishing.
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
