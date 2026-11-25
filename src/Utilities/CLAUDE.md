# bit Utilities

Guidance that holds for every library under `src/Utilities` (Balette, BbScanner, Bcrdt, Bdnd, Belemetry, BevTools,
Bheadless, Bink, BlazeDb, Blex, Blow, Bmage, Bmotion, Bonboarding, Borm, Bsqlite, ...). A library's own
`CLAUDE.md`, where it has one, adds to these rules; it never relaxes them. Coding style comes from the `.editorconfig`
at the root of the `src` folder ([../CLAUDE.md](../CLAUDE.md)).

## One namespace per library

**Every type of a library lives in exactly one namespace: `Bit.<Library>`.** All of Bmage is `Bit.Bmage`, all of
Bsqlite is `Bit.Bsqlite`, all of Blex is `Bit.Blex`, the same way all of BlazorUI is `Bit.BlazorUI`.

- **Every project of the library follows it, not only the main one.** The integration and add-on packages
  (`Bit.Bmage.Blazor`, `Bit.Bmage.Bink`, `Bit.Bmage.BbScanner`, `Bit.Bcrdt.Server`, `Bit.Bcrdt.SignalR`,
  `Bit.BlazeDb.EntityFrameworkCore`, `Bit.Belemetry.Hangfire`, ...), the platform packages (`*.Maui`), the test
  toolkits a library ships (`*.Testing`), its source generators (`*.Generators`) and its tools (`Bit.BevTools.Cli`) all
  declare their types in `Bit.<Library>`. A project's name is its package and assembly name, never its namespace.
- **Folders never make namespaces.** Organise files in folders freely (`Codecs/Png`, `Storage/Opfs`, `Editor/Components`),
  but every `.cs` file still says `namespace Bit.<Library>;`.
- **Every library `.csproj` sets `<RootNamespace>Bit.<Library></RootNamespace>`**, so new files, embedded resources
  and generated code start out right.
- **Razor files declare `@namespace Bit.<Library>` themselves.** A component without one takes its namespace from the
  root namespace plus its folder (`Editor/Components/BmageEditor.razor` would land in `Bit.Bmage.Editor.Components`),
  so a `.razor` file in any folder below the project root must carry the directive.
- **No borrowed namespaces either.** Service registration and other extension methods (`AddBitBsqliteServices`,
  `UseBitBlazeDb`, `UseBelemetry` on Hangfire's configuration, ...) are declared in `Bit.<Library>` too, never in
  `Microsoft.Extensions.DependencyInjection`, `Microsoft.EntityFrameworkCore`, `Hangfire` or another framework's
  namespace. An app imports `Bit.<Library>` once and sees the whole library.
- **Resolve conflicts, never split the namespace to dodge them.** When two projects of a library would put two types of
  the same name into `Bit.<Library>`, rename them after what tells them apart (Bmage's two internal `HuffmanTable`s
  became `BmageJpegHuffmanTable` and `BmageVp8LHuffmanTable`; the generator's `BlazeDbDiagnostics` became
  `BlazeDbGeneratorDiagnostics` next to the engine's public one). When
  a type collides with a type of another namespace the library imports (another bit library, `System.Drawing`, EF Core,
  ...), qualify or alias the foreign one at the use site. When you add a type, check the whole library for its name,
  not only the project you are in.
- **Code that names a namespace follows it.** Generators emit `global::Bit.<Library>.Type`, reflection and
  `GetTypeByMetadataName` use `Bit.<Library>.Type`, and docs, READMEs, the MCP servers' catalogs and setup guides tell
  the reader to add `using Bit.<Library>;` / `@using Bit.<Library>` and nothing more specific.

The only exceptions:

- **Tests, demos and samples** (everything under a library's `Tests/`, `Demo/` and `Samples/` folders) keep their own
  namespaces (`Bit.Bmage.Tests`, `Bit.Bmage.Demo.Client`, ...); they are not part of any package.
- **Compiler-required polyfills**, such as `IsExternalInit` in a `netstandard2.0` generator, stay in the namespace the
  compiler looks them up in (`System.Runtime.CompilerServices`), internal to their assembly.

Strings that only look like namespaces are not namespaces and keep their names: package and assembly names
(`Bit.Bcrdt.Server`, `_content/Bit.Bmage.Blazor/...`), `ActivitySource`/`Meter` names (`Bit.Belemetry.Hangfire`) and
analyzer diagnostic categories (`Bit.Blex.Generators`).

## One type per file

**Every type lives in its own file, named exactly after the type.** `BmageImageBuffer` is in `BmageImageBuffer.cs`,
`IBevToolsStateProvider` in `IBevToolsStateProvider.cs`, the `BevToolsStatePanel` component in
`BevToolsStatePanel.razor`.

- **Never put two top-level types in one file**, however small or closely related they are: classes, records,
  structs, interfaces, enums and delegates each get their own file. Don't write grab-bag files such as `Models.cs`,
  `Enums.cs`, `Records.cs` or `Exceptions.cs`. A small enum or event-args record used by one class still goes in its own file.
- **The file name is the type name.** Rename the file when you rename the type, and keep the file in the folder it
  belongs to. Folders are free, as the namespace rule above says.
- **Partial types** may span several files named `<Type>.<Part>.cs` (`BinkDocument.Layout.cs`), and a component's
  code-behind is `<Component>.razor.cs` (with `<Component>.razor.css` / `.razor.js` beside it). Each of those files
  still holds only that one type.
- **A generic type that shares its name with another type** of the library goes in `<Type>{T}.cs`
  (`BlexAction{TStore,TPayload}.cs` next to `BlexAction.cs`). A generic type with a name of its own is plain `<Type>.cs`.
- **Nested types** stay inside their containing type's file. They are members of that type, not separate types.

## Every type name starts with the library name

**Every type's name starts with its library's full name**: `BmageImageBuffer`, `BmageLayer`, `BbScannerAfterScan`,
`IBevToolsStateProvider`, `BelemetrySentryDsn`, `BmotionSpring`. The rule covers public and internal types alike,
including classes, records, structs, interfaces, enums, delegates, attributes (`BevToolsSensitiveAttribute`) and Razor
components (`BevToolsStatePanel`).

- **Interfaces put the `I` first**, then the library name: `IBmageImageDecoder`, `IBbScannerCameraSource`.
- **An abbreviation is not the library name.** `Bb`, `Bev` and `Bm` are not prefixes: `BbAfterScan` became
  `BbScannerAfterScan`, `IBevStateProvider` became `IBevToolsStateProvider` and `BmSpring` became `BmotionSpring`.
- **Every project of the library uses the library's name**, not the project's name: the types of `Bit.Bmage.Blazor`,
  `Bit.BevTools.Server` and `Bit.BbScanner.Maui` start with `Bmage`, `BevTools` and `BbScanner`. Add the project's
  part after the prefix when it helps: `BmageBinkRenderedImage`, `BevToolsBelemetryPanel`.
- **Two types must never end up with the same name.** Name each after what tells it apart: BevTools'
  `BevRuntimeAttachment` and the static `RuntimeAttachment` became `BevToolsRuntimeAttachment` and
  `BevToolsRuntimeAttacher`.
- **Members keep their own names.** Only the type is renamed: `BmageLayer.Mask`, `BmageImageMetadata.Orientation`
  and `BbScannerPixelFormat.Rgba32` stay as they are.

The only exceptions:

- **Tests, demos and samples** (everything under a library's `Tests/`, `Demo/` and `Samples/` folders) are exempt from
  this rule and the one-type-per-file rule.
- **Bheadless** keeps its `BitBh` prefix (`BitBhButton`, `IBitBhAnnouncer`, `BitBhServiceCollectionExtensions`), the
  way BlazorUI keeps `Bit`. New Bheadless types start with `BitBh` too.
- **The static `Bit<Library>` registration classes** (`BitBmage`, `BitBcrdt`, `BitBcrdtServer`, `BitBlexMaui`,
  `BitBevToolsLoader`, `BitBsqlite`, ...), which hold `AddBit<Library>Services` and the other entry-point extension
  methods, keep their names. Nothing else uses `Bit<Library>`: options classes are `BmageOptions`, `BevToolsOptions`
  and `BmotionOptions`.
- **Bmotion's `Bm` factory** (`Bm.To(...)`, `Bm.Spring(...)`) keeps its short name, because it is written in nearly
  every Razor attribute and `Bmotion` is already the component.
- **Nested types**, which their containing type already qualifies.
- **Code in `Shared/`** that is linked into several libraries (`McpSearch`, `McpSearchEntry`), since no single
  library name fits it, and **compiler-required polyfills** such as `IsExternalInit`.

When you add or rename a type, also check the name against the rest of the library and update every place that spells
it as text: docs, READMEs, MCP catalogs, demo code snippets, generator output and reflection lookups.
