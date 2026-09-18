# bit Minifier

Shrinks the .NET assemblies a Blazor WebAssembly app downloads.

```
dotnet add package Bit.Minifier
```

Install it in the Blazor WebAssembly project and publish in Release. There is nothing else to do.

## What it does

ILLink removes the code an app doesn't use. Right after it, Bit.Minifier rewrites every trimmed
assembly (the app's, the libraries' and the framework's) and its pdb. Everything that can go, goes:
every non-public type, method and field name, non-public parameter names and properties, compiler-generated
names, and the attributes only the compiler, debuggers and analyzers read (`[CompilerGenerated]`,
`[Nullable]`, `[NotNullWhen]`, `[DebuggerDisplay]`, ...).

Public names stay, so a logged stack trace still names its public frames, the pdb keeps breakpoints and
line numbers working, and `dotnet build`, `dotnet run` and `dotnet watch` are never touched.

These stay too, because something reads them by name at runtime:

- names a string literal mentions (`nameof(...)`, `GetMethod("...")`, `[UnsafeAccessor(Name = "...")]`);
- public properties and fields (serializers);
- Blazor component types;
- members with runtime attributes (`[Parameter]`, `[JSInvokable]`, ...).

Libraries that find private members by a name no string spells out may not work.

## Your own code keeps its names

Whatever the level, the assemblies your own projects produce are left with every name they have: the
project being published and every project it references, which the build already tells apart from the
packages it uses (`ReferenceSourceTarget`, not a list anyone maintains). Their attributes still go, and
the calls they make into the libraries still follow those libraries' new names - only their own types and
methods are untouched, so an exception your app logs reads as your source does and points at the code to
open. The libraries and the framework, where a stack trace only says which one it passed through, are
minified as the level says.

## Aggressive (experimental)

```xml
<BitMinifyAggressive>true</BitMinifyAggressive>
```

Everything above, and public names go too: public types and their namespaces, public
non-virtual methods and property accessors, public fields and parameter names, generic parameter
names and event metadata,
along with the attributes only the compiler and the trimmer read. It is for apps whose names nothing
reads from outside the WebAssembly client, published by someone who tests the result. Stack traces
lose their public names as well; [reading a stack trace](#reading-a-stack-trace) is the way back.

On top of what the default level keeps, these stay:

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

An app picked at random, its WebAssembly client published in Release (managed `.wasm` files, brotli):

| | size | saved |
|---|---:|---:|
| without Bit.Minifier | 6,223 KB | |
| default | 5,827 KB | 396 KB (-6.4%) |
| aggressive | 5,756 KB | 467 KB (-7.5%) |

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
stack trace came from.

Whoever reads the trace usually has neither the project nor the publish: a map kept with a release and a text
file are enough, and the same tool is a package of its own, run without installing anything:

```
dnx Bit.Minifier.Cli --decode bit-minifier.map trace.txt
```

Leaving the trace out reads it from standard input there too. `Bit.Minifier` is what an app installs to be
minified; `Bit.Minifier.Cli` is the same program for whoever holds a map.

- **Keep the map with the release it belongs to.** It is written to `obj`, so a clean takes it away, and a
  CI job that publishes from a fresh checkout leaves nothing behind unless it collects the map as an
  artifact. Every publish renames afresh, so a map reads the stack traces of its own publish and of no
  other. Where the releases are kept is where it belongs, not with the app: served next to the client, it
  hands the names back to everyone who downloads it.
- Any text that holds these names is decoded, not only a stack trace: a log line, a message quoting a
  `Type.FullName`, a serialized type name.
- Files and line numbers never needed it - the pdbs are rewritten along with the assemblies, so they are
  in the trace already.
- What no map holds: parameter names, which are cleared rather than renamed, and the names of local
  variables, which are the pdb's. A short name that several assemblies ended up with is read as the first
  of them, and the other readings are named in brackets at the end of the line, with the assembly each
  belongs to - a stack trace says which assembly a frame is in no more than it says what its names were.

## Good to know

- **Nullable metadata:** it is kept when the app sets `NullabilityInfoContextSupport` to `true`, the
  switch .NET already has for the one thing that reads it at runtime.
- **Nothing ships broken:** if any reference would stop resolving, or anything else fails, nothing
  is written, and the publish shows warning `BITMIN001` and ships the trimmed assemblies as they were.
  With warnings as errors, add `BITMIN001` to `MSBuildWarningsNotAsErrors` to keep it a warning. The next
  publish tries again.
- **Switching levels:** the next publish trims afresh, so nothing is minified twice.
