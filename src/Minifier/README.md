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

`<BitMinify>false</BitMinify>` turns all of it off without uninstalling anything: the next publish trims afresh
and ships the assemblies as ILLink wrote them.

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
along with the attributes only the compiler and the trimmer read, and the assembly metadata an app can read of
itself (`[AssemblyProduct]`, `[AssemblyFileVersion]`, `[AssemblyTitle]`, ... - `[AssemblyInformationalVersion]`
stays, so there is still a version to show). It is for apps whose names nothing
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
- the app shows its own assembly metadata (`Assembly.GetCustomAttribute<AssemblyProductAttribute>()` and the
  rest): read `[AssemblyInformationalVersion]`, or the assembly's version, which is its identity and never goes;
- the app stores a type name across releases: the short names change from one build to the next.

## Numbers

The bit BlazorUI demo, its WebAssembly client published in Release (the managed `.wasm` files, brotli - which
is what a browser downloads):

| | size | saved | time |
|---|---:|---:|---:|
| without Bit.Minifier | 6,604 KB | | |
| default | 6,132 KB | 472 KB (-7.2%) | 7.6s |
| aggressive | 6,064 KB | 540 KB (-8.2%) | 7.9s |

Uncompressed, the same assemblies go from 34,847 KB to 32,675 KB (-6.2%) and 32,427 KB (-7.0%): brotli gains a
little more, because a short name repeated is a name that compresses well. This app lazily loads three of its
assemblies, so what it downloads to start is 5,885 KB, 5,477 KB (-6.9%) and 5,415 KB (-8.0%).

The time is what the minifier itself adds to the publish, for the 94 trimmed assemblies (34 MB of IL) this app
ships. It runs once per publish, after ILLink.

Where it comes from: 102,000 attributes removed, and 60,000 names shortened at the default level or 85,000 at
aggressive. The libraries give up the most - `Bit.BlazorUI` 594 KB of its 3.6 MB (-16%), `Bit.BlazorUI.Extras`
404 KB (-15%), `Newtonsoft.Json` 137 KB (-20%), `System.Text.Json` 72 KB (-18%). The app's own
`Bit.BlazorUI.Demo.Client.Core` keeps every name it has and still gives up 310 KB, all of it attributes.

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
file are enough, and the decoder is a package of its own, run without installing anything:

```
dnx Bit.Minifier.Cli --decode bit-minifier.map trace.txt
```

Leaving the trace out reads it from standard input there too. `Bit.Minifier` is what an app installs to be
minified; `Bit.Minifier.Cli` is the decoder for whoever holds a map - the same source files, so the two read a
map the same way, and nothing else, so it is a package with no dependencies.

- **Keep the map with the release it belongs to.** It is written to `obj`, so a clean takes it away, and a
  CI job that publishes from a fresh checkout leaves nothing behind unless it collects the map as an
  artifact. `<BitMinifyMapOutputPath>` copies it out of `obj` on every publish, for the job to pick up:

  ```
  dotnet publish MyApp.Client.csproj -c Release -p:BitMinifyMapOutputPath=bit-minifier.map
  ```

  Every publish renames afresh, so a map reads the stack traces of its own publish and of no
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
- **Nothing ships broken:** nothing is written unless every reference into the rewritten assemblies still
  resolves. If one wouldn't, the assemblies whose new names broke it are left as ILLink wrote them and
  everything else is minified around them - one assembly never costs a publish its savings. An assembly that
  can't be read or written (a Windows pdb next to it, a ReadyToRun image) is left alone the same way. Either
  way the publish shows warning `BITMIN001` naming what was left unminified and why, and if nothing can be
  minified at all it says so and ships the trimmed assemblies as they were. With warnings as errors, add
  `BITMIN001` to `MSBuildWarningsNotAsErrors` to keep it a warning. The next publish tries again.
- **The same publish gives the same names:** the assemblies are read in a fixed order rather than the order the
  file system happens to hold them in, so the same input produces the same assemblies and the same map on every
  machine that publishes it.
- **Switching levels:** the next publish trims afresh, so nothing is minified twice. Turning `BitMinify` off
  takes the map with it, since it would describe assemblies that are no longer there.
