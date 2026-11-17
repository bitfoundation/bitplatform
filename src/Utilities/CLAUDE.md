# bit Utilities

Guidance that holds for every library under `src/Utilities` (Balette, BbScanner, Bcrdt, Bdnd, Belemetry, BevTools,
Bheadless, Bink, BlazeDb, Blex, Blow, Bmage, Bmotion, Bonboarding, Borm, Bortable, Bsqlite, ...). A library's own
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
  became `JpegHuffmanTable` and `Vp8LHuffmanTable`; the generator's `BlazeDbDiagnostics` became
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
