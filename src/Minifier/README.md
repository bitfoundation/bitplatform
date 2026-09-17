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

Libraries that read backing fields or nullable metadata by themselves, such as EF Core without a
compiled model, may not work at this level.

## Numbers

bitplatform.dev's WebAssembly client, published in Release (managed `.wasm` files, brotli):

| | size |
|---|---:|
| without Bit.Minifier | 2,094 KB |
| default | 2,026 KB (-3.2%) |
| aggressive | 1,909 KB (-8.8%) |

## Good to know

- **Nullable metadata:** it is kept when the app sets `NullabilityInfoContextSupport` to `true`.
- **Reading stack traces:** `obj/<configuration>/<tfm>/bit-minifier.map` lists every renamed member.
- **Nothing ships broken:** if any reference would stop resolving, nothing is written, and the
  publish shows warning `BITMIN001` and ships the trimmed assemblies as they were.
