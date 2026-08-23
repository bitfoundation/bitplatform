# Bit.Butil.Tests.Manual

A hand-run harness that answers three questions about a trimmed consumer of `Bit.Butil`:

1. Does it only pay for the Butil classes it actually uses?
2. Do the pieces the library reaches **by name at runtime** survive that trimming?
3. Does the JavaScript follow suit - is the bundle a trimmed publish would ship exactly the modules the
   trimmed assembly still calls, and does the lazy per-module loader behave?

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

## The checks

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

**JavaScript modules** ([`ScriptTrimming.cs`](ScriptTrimming.cs)). Every interop call goes through a
literal `"BitButil.<module>.<function>"` identifier, and the trimmer rewrites the assembly's user-string heap
to hold only the strings that surviving method bodies still reference. So the set of `BitButil.<module>.`
prefixes left in the trimmed `Bit.Butil.dll` is exactly the set of JavaScript modules the app can still reach.
That is what the consumer-side publish step in the NuGet package (`Bit.Butil.Build`, wired up by
`buildTransitive/Bit.Butil.targets`) reads to assemble a smaller `bit-butil.js`, and this harness runs the very same
code against the very same trimmed assembly:

- untrimmed, every module the library ships must answer to some `BitButil.<module>.*` call site (except
  `butil` and `utils`, which exist only as dependencies) - an orphan module is JavaScript for a C# API that
  no longer exists - and every call site must name a module that exists;
- trimmed, the modules still called must be exactly `MustSurviveModules` (the JavaScript behind the
  services `ConsumerComponent` uses, plus `events`, reached through `Window.SubscribeEvent`) - a module
  nothing in this project calls surviving is the same regression as a service surviving;
- Bit.Butil's own JavaScript build outputs have to agree with each other and with the sources: a chunk and
  a lazy-loadable file per module in the manifest, and the manifest's dependencies equal to what the
  TypeScript sources reference (the same rule `build.mjs` uses), so a stale build is caught here rather
  than by a consumer whose trimmed bundle lacks a module.

It also reports the size story - full bundle, trimmed bundle (raw / gzip / brotli), and the total a lazy
app would download for the same modules - which is the benchmark for the whole feature.

**Script bundling** ([`ScriptBundling.cs`](ScriptBundling.cs), [`verify-bundle.mjs`](verify-bundle.mjs)).
The check above runs the publish-time bundler once, over this project's one assembly, and asks whether the
answer is the expected one. These go after the bundler *itself* - the parts a real consumer's build reaches
and this repository never does, plus the artifacts the whole feature rests on:

- **the identifier rule** (`TryGetModule`), which decides what counts as a call into a module. It runs over
  every literal in the assembly, so a literal it misses is a module missing from a bundle the app needs, and
  one it wrongly claims is a phantom module in a consumer's build warning;
- **the manifest** (`ReadManifest`): comments, blank lines and stray separators read the way `build.mjs`
  writes them, and every malformed shape - a module listed twice, a dependency that is not a module, a line
  without a separator, a file that is not there - refused rather than half-read, since a line silently
  skipped is a bundle missing JavaScript the app calls;
- **resolution** (`Resolve`): dependencies come with their module, a dependency two modules share is included
  once, the result follows the manifest's dependency-first order rather than the call order, modules nothing
  calls are left out, an identifier naming no module is *reported* rather than dropped (the BUTIL001 warning
  a consumer sees), and a manifest with a cycle in it resolves instead of hanging the publish;
- **writing the bundle** (`WriteBundle`): the chunks concatenated in the order given, no BOM, and - the
  reason it assembles in memory first - a missing chunk leaves the previous bundle intact and no temporary
  file behind, because a consumer's incremental publish reads that file back on the next run;
- **reading the assembly** (`UserStringHeap`): the modules a bundle is built from are exactly what the
  assembly's literals name, and a file that is not a managed image - or one cut short - comes out as the
  `BadImageFormatException` the MSBuild task catches, not as an index off the end of an array;
- **the shipped artifacts**, which nothing enforces and everything assumes: the chunks assemble byte-for-byte
  into the `bit-butil.js` the package ships, each `modules/<name>.js` is byte-for-byte the bundle its own
  dependency closure assembles to, every chunk carries the guard that makes a second evaluation a no-op and
  appears in the bundle exactly once, and the manifest lists every module after the modules it depends on;
- **running the result** - the bundle a publish of *this* assembly would ship (trimmed: the 8-module,
  9 KB one), the full bundle, and two overlapping lazy module files loaded one after the other. Each is
  evaluated under Node in a browser-like sandbox and has to register exactly the expected `BitButil`
  namespaces, none of them empty, and register nothing a second time (the sentinel each namespace is marked
  with has to survive re-evaluation - a guard that stopped holding would reset a module's listener
  bookkeeping in a real app). Byte comparisons cannot tell whether a bundle is a script that loads;
- **the package layout**: the chunks, the manifest and the MSBuild task are packed (Bit.Butil.csproj) into
  the folders the consumer-side targets read them from (`buildTransitive/Bit.Butil.targets`), and the two
  targets files under both `buildTransitive/` and `build/`. Nothing else connects those two files, and every
  project in this repository overrides both paths - so a consumer would be the first to find out.

Node runs the last of those; it is already a build dependency of Bit.Butil (it compiles the TypeScript), so
a checkout that can produce the artifacts under test can always run them.

**Lazy scripts** ([`LazyScripts.cs`](LazyScripts.cs)). Against a recording `IJSRuntime`, with
`BitButil.UseLazyScripts()` on: the first call into an API must `import()` that API's module - and only
that one - before invoking it; later calls, and other services on the same runtime, must not import again;
callers that arrive while an import is in flight must share it rather than issue their own; an import that
fails - or that the runtime refuses to issue at all - must be retried on the next call; a custom modules
path must be honoured, normalized into a specifier the browser will import (a path relative to the base
href gets the `./` that keeps it from being read as a bare module specifier; an absolute URL is passed
through); the
`AddBitButilServices(options)` overload must flip the same switches (true, false, and null = leave alone); and with lazy
scripts off nothing may be imported. Runs in both modes of the harness, so the loader is also proven to
survive trimming through the runtime override alone (this project never sets the `BitButilLazyScripts`
switch).

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
./bin/Release/net10.0/<rid>/publish/Bit.Butil.Tests.Manual
```

The untrimmed run has to come first: a trimmed run with no usable manifest to compare against **fails**,
because it would otherwise report `PASS` having verified none of the interop contract - and a manifest
read only partly would report `PASS` having verified less of it than the output claims.

## Reference results

| | untrimmed | trimmed |
| --- | --- | --- |
| `Bit.Butil.dll` | 622,080 bytes | 118,272 bytes |
| types in assembly | 793 | 149 |
| `[ButilService]` discovered / registered | 57 / 57 | 5 / 5 |
| interop contract | 43 types captured | 10 checked, 33 trimmed away, 0 problems |
| JavaScript modules called | 63 of 65 | 6 of 65 (clipboard, cookie, events, geolocation, storage, window) |
| `bit-butil.js` a publish would ship | 112,422 bytes, all 65 modules | 9,134 bytes, 8 modules (3,046 gzip / 2,695 brotli) - 8.1% |
| lazy scripts would download | 147,730 bytes over 63 files | 11,940 bytes over 6 files |
| script-bundling checks | 76 / 76 | 76 / 76 |
| lazy-loader checks | 16 / 16 | 16 / 16 |

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
- **`JavaScript modules nothing in this project calls survived trimming`** - a `BitButil.<module>.*`
  identifier outside `MustSurviveModules` is still in the trimmed assembly's strings, so a consumer's
  trimmed bundle would carry that module too. Same causes as the service check above; otherwise a service
  gained a call into a new namespace and `MustSurviveModules` was not updated.
- **`module 'X' is behind an API ConsumerComponent uses but its identifiers did not survive trimming`** -
  the trimmed bundle would lack JavaScript the app calls. Usually an identifier stopped being a plain
  string literal (built with interpolation or concatenation): the bundler can only see literals.
- **`the C# side invokes 'BitButil.X.*' but Bit.Butil ships no JavaScript module called 'X'`** - a
  namespace with no `Scripts/X.ts` behind it; the call fails in the browser and lazy scripts would
  import a file that does not exist.
- **`JavaScript module 'X' is not called by any 'BitButil.X.*' identifier`** - dead script, or a helper
  module that should be listed as dependency-only in `ScriptTrimming.DependencyOnlyModules`.
- **`the manifest ... Bit.Butil's JavaScript build is stale`** - `Bit.Butil/obj/butil-js` does not match
  the TypeScript sources; rebuild Bit.Butil.
- **`script bundling: ...`** - the publish-time bundler, Bit.Butil's JavaScript build outputs, or the
  package layout. The message names the claim that broke; the ones worth knowing on sight are *byte-for-byte
  the bit-butil.js the package ships* and *every lazy module file is exactly the bundle its own dependency
  closure assembles to* (the chunks, the bundle and the lazy files have drifted apart - rebuild Bit.Butil,
  and if that does not settle it, `build.mjs` no longer writes the three from one source), *registers exactly
  the expected BitButil namespaces* (an assembled bundle is missing a module or carries one it should not -
  what a consumer would see as `BitButil.x is undefined`), and *packed into the folder the consumer-side
  targets read them from* (Bit.Butil.csproj and buildTransitive/Bit.Butil.targets disagree about where the
  chunks, the manifest or the task live, which breaks every consumer's publish and no build in this repo).
- **`lazy scripts: ...`** - the lazy loader imported the wrong module, imported twice, did not retry a
  failed import, or imported with lazy scripts off. See `LazyScripts.cs` for the exact expectation.
