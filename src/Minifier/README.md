# bit Minifier

Shrinks the .NET assemblies a Blazor WebAssembly app downloads.

```
dotnet add package Bit.Minifier
```

Install it in the Blazor WebAssembly project and publish in Release. There is nothing else to do.

## What it does

ILLink removes the code an app doesn't use. Right after it, Bit.Minifier rewrites every trimmed
assembly (the app's, the libraries' and the framework's) and its pdb:

- It removes the attributes only the compiler reads (`[CompilerGenerated]`, `[Nullable]`,
  `[NotNullWhen]`, ...).
- It shortens compiler-generated names: `<BuildRenderTree>b__12_0` becomes `<a>b__12_0`.

Public and hand-written names stay, the pdb keeps breakpoints and line numbers working, and
`dotnet build`, `dotnet run` and `dotnet watch` are never touched.

## Aggressive

```xml
<BitMinifyAggressive>true</BitMinifyAggressive>
```

At this level everything that can go, goes: every non-public type, method and field name,
non-public parameter names and properties, and the attributes only debuggers and analyzers read.
Public names stay, so a logged stack trace still names its public frames.

These stay too, because something reads them by name at runtime:

- names a string literal mentions (`nameof(...)`, `GetMethod("...")`, `[UnsafeAccessor(Name = "...")]`);
- public properties and fields (serializers);
- Blazor component types;
- members with runtime attributes (`[Parameter]`, `[JSInvokable]`, ...).

When the app publishes EF Core, which may map the types of any assembly, the nullable metadata and
the backing field names it reads are kept everywhere but in the bit libraries, so the model still
matches its migrations. Other libraries that find private members by a name no string spells out
may not work at this level.

## Super aggressive (experimental)

```xml
<BitMinifySuperAggressive>true</BitMinifySuperAggressive>
```

Everything aggressive does, and public names go too: public types and their namespaces, public
non-virtual methods and property accessors, public fields and parameter names, generic parameter
names and event metadata,
along with the attributes only the compiler and the trimmer read. It is for apps whose names nothing
reads from outside the WebAssembly client, published by someone who tests the result. Stack traces
lose their public names as well; [reading a stack trace](#reading-a-stack-trace) is the way back.

On top of what aggressive keeps, these stay:

- names any assembly of the app mentions in a string, namespaces included (`Type.GetType("My.App.Plugin")`);
- virtual and interface members, properties, constructor parameters and enum members;
- interface method parameter names (Refit), `ShouldSerializeX()` / `ResetX()`, public constants,
  and public fields when Newtonsoft.Json is published;
- exception types, and the types of Blazor component parameters (a server names both);
- the `Main` the WebAssembly host starts;
- the public names of an assembly that an assembly left unminified references;
- `System.Private.CoreLib` and `System.Runtime.InteropServices.JavaScript`, which the runtime binds to by name.

It breaks what finds a public name some other way, so don't use it when:

- a server passes a component parameter whose runtime type isn't the declared one (a derived type, or `object`);
- code builds a name at runtime (`"On" + name`), or lists types or fields and shows or stores their names;
- a serializer reads public fields (`XmlSerializer`, System.Text.Json with `IncludeFields`) or type names
  (`TypeNameHandling`, `DataContractSerializer` namespaces);
- configuration names framework types, like `Logging:LogLevel` categories in `appsettings.json`;
- the app stores a type name across releases: the short names change from one build to the next.

## Numbers

bitplatform.dev's WebAssembly client, published in Release (managed `.wasm` files, brotli):

| | size |
|---|---:|
| without Bit.Minifier | 2,043 KB |
| default | 1,979 KB (-3.1%) |
| aggressive | 1,869 KB (-8.5%) |

The BlazorUI demo's WebAssembly client, measured the same way:

| | size |
|---|---:|
| without Bit.Minifier | 6,445 KB |
| default | 6,345 KB (-1.6%) |
| aggressive | 6,038 KB (-6.3%) |
| super aggressive | 5,987 KB (-7.1%) |

## Reading a stack trace

Every publish writes `obj/<configuration>/<tfm>/bit-minifier.map`, one tab-separated line per name that
changed - the assembly, the kind (`T` type, `M` method, `F` field, `G` generic parameter), the name the
source has and the name it was given:

```
Bit.BlazorUI.Demo.Client.Core	T	...Services.HttpMessageHandlers.ExceptionDelegatingHandler	_aN
Bit.BlazorUI.Demo.Client.Core	M	Microsoft.Extensions.DependencyInjection.IServiceCollectionExtensions::AddSessioned	ah
```

The tool that wrote it reads it back. It is in the package, and the package's targets run it:

```
dotnet msbuild MyApp.Client.csproj -t:BitMinifierDecode -p:Configuration=Release -p:BitMinifyTrace=trace.txt
```

turns the stack trace a published client logged

```
   at _aN+_a.MoveNext()
   at Microsoft.Extensions.DependencyInjection._aD.ah[a,b](IServiceCollection services)
```

into the one its source wrote:

```
   at ...Services.HttpMessageHandlers.ExceptionDelegatingHandler+<SendAsync>d__1.MoveNext()
   at Microsoft.Extensions.DependencyInjection.IServiceCollectionExtensions.AddSessioned[TService,TImplementation](IServiceCollection services)
```

Without `BitMinifyTrace` it reads the trace from standard input, so a trace can be pasted in or piped from
anywhere. `-p:BitMinifyMap=<path>` reads a map kept somewhere else - the one archived with the release the
stack trace came from. The tool can also be run on its own, wherever NuGet put the package:

```
dotnet ~/.nuget/packages/bit.minifier/<version>/tools/Bit.Minifier.dll --decode bit-minifier.map trace.txt
```

- **Keep the map with the release it belongs to.** It is written to `obj`, so a clean takes it away, and a
  CI job that publishes from a fresh checkout leaves nothing behind unless it collects the map as an
  artifact. Every publish renames afresh, so a map reads the stack traces of its own publish and of no
  other. Where the releases are kept is where it belongs, not with the app: served next to the client, it
  hands the names back to everyone who downloads it.
- Any text that holds these names is decoded, not only a stack trace: a log line, a message quoting a
  `Type.FullName`, a serialized type name.
- Files and line numbers never needed it - the pdbs are rewritten along with the assemblies, so they are
  in the trace already.
- What no map holds: parameter names, which aggressive clears rather than renames, and the names of local
  variables, which are the pdb's. A short name that several assemblies ended up with is read as the first
  of them, and the other readings are named in brackets at the end of the line, with the assembly each
  belongs to - a stack trace says which assembly a frame is in no more than it says what its names were.

## Good to know

- **Nullable metadata:** it is kept when the app sets `NullabilityInfoContextSupport` to `true`, and
  outside the bit libraries when the app publishes EF Core.
- **Nothing ships broken:** if any reference would stop resolving, or anything else fails, nothing
  is written, and the publish shows warning `BITMIN001` and ships the trimmed assemblies as they were.
  With warnings as errors, add `BITMIN001` to `MSBuildWarningsNotAsErrors` to keep it a warning. The next
  publish tries again.
- **Switching levels:** the next publish trims afresh, so nothing is minified twice.
