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
lose their public names as well; `bit-minifier.map` is the way back.

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

## Good to know

- **Nullable metadata:** it is kept when the app sets `NullabilityInfoContextSupport` to `true`, and
  outside the bit libraries when the app publishes EF Core.
- **Reading stack traces:** `obj/<configuration>/<tfm>/bit-minifier.map` lists every renamed member.
- **Nothing ships broken:** if any reference would stop resolving, or anything else fails, nothing
  is written, and the publish shows warning `BITMIN001` and ships the trimmed assemblies as they were.
  With warnings as errors, add `BITMIN001` to `MSBuildWarningsNotAsErrors` to keep it a warning. The next
  publish tries again.
- **Switching levels:** the next publish trims afresh, so nothing is minified twice.
