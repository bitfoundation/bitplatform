# Bit.Butil

Strongly-typed C# wrappers over browser Web APIs for Blazor (WebAssembly, Server, Hybrid, prerendering; net8.0/9.0/10.0).
`README.md` in this folder is the reference guide and is also served by the MCP server - keep it accurate.

## Layout

| Path | What it is |
| --- | --- |
| `Bit.Butil/Publics/` | The public API: one injectable service class per browser API, plus its DTOs/enums in a same-named subfolder |
| `Bit.Butil/Internals/` | Interop helpers, JS-callable relay classes, JS-shaped option types |
| `Bit.Butil/Scripts/*.ts` | One TypeScript file = one JS module = one `BitButil.<module>` namespace |
| `Bit.Butil/build.mjs` | Assembles `wwwroot/bit-butil.js`, `wwwroot/modules/*.js` and the packed chunks + manifest (run by MSBuild; outputs are generated and git-ignored) |
| `Bit.Butil.Build/` | MSBuild task run in a consumer's publish: script scanning, trimming, bundling |
| `Bit.Butil.Demo/` | The documentation site (Client) and its host (Server), which also hosts the MCP server at `/mcp` |
| `Samples/` | Minimal hosting samples: `Samples.Core` (shared pages), `Samples.Web` (standalone WebAssembly), `Samples.Maui` (Hybrid) |
| `tests/` | `Tests.E2E` (Playwright), `Tests.Mcp` (MSTest against the live MCP server), `Tests.Manual` (trimming/bundling console harness), `Tests.PublishFixture` (the consumer app it publishes) |

## Coding style

Follow the `.editorconfig` at the root of `src`. Match the surrounding code: file-scoped namespaces, primary
constructors, expression-bodied one-liners, `is null` / `is false` over `== null` / `!`. Comments explain *why*
a thing is shaped the way it is (trimming, prerendering, browser quirks), not what the next line does.

Every public type and member carries XML documentation ending in a link to its MDN page. That documentation is
what IntelliSense and the MCP server hand out, so it is part of the feature, not decoration.

Each top-level type lives in its own file, and the file is named after the type. No file declares more than
one top-level type (nested types belong to their container, so they stay with it). The single exception is a
generic type accompanying an existing non-generic one of the same name - those two may share a file.

## Adding or changing an API

1. **Service class** in `Publics/`, marked `[ButilService(typeof(TheClass))]` and taking `IJSRuntime` through a
   primary constructor. The attribute is the only registration step - there is no list to update - and its type
   argument is what preserves the constructor under trimming. Payload DTOs and enums go in `Publics/<Api>/`.
2. **Interop identifiers must be literal strings** of the form `"BitButil.<module>.<function>"`. Never build one
   by interpolation or concatenation: the string literals surviving trimming *are* the list of JS modules a
   published app can still reach, and both the publish-time bundler and the `Bit.Butil.Build` scanner read them.
3. **Call through the `Invoke` / `InvokeVoid` extensions** (`Extensions/JSRuntimeExtensions.cs`), not
   `IJSRuntime` directly - they return safe defaults instead of throwing during prerender/SSR, and route through
   the lazy-script loader.
4. **JavaScript** in a `Scripts/<module>.ts` that attaches to `window.BitButil`, following the existing shape.
   Expose an `isSupported()` where the API is not universally implemented. Modules must be safe to evaluate more
   than once; cross-module references (`butil.utils.*`) are discovered by `build.mjs` as dependencies.
5. **Types crossing the interop boundary** need `[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(T))]`
   at the call site so trimming keeps what `System.Text.Json` reflects over. `[JSInvokable]` callbacks use the
   explicit-identifier form (`[JSInvokable(InvokeMethodName)]`).
6. **Anything attaching a listener returns a `ButilSubscription`**; anything holding a browser resource open
   (streams, recorders, handles) is `IAsyncDisposable`. Document the gesture/HTTPS/permission preconditions.
7. Add the service to the **`README.md` "What's in the box"** table.

## Showcases - the Demo and Samples projects

Every feature has to be demonstrable, not merely documented. A new or extended API is not done until:

- **`Bit.Butil.Demo/Client/Pages/<Api>Page.razor`** covers it: a `PageHeader` (category, lead, MDN link,
  `InjectAs`), one `DemoSection` per public member or coherent group of members - each with a runnable control,
  a `Code` snippet the reader can copy, and a `DemoConsole` for output - and an `ApiTable` row for every public
  member with its signature. Wrap calls in try/catch and report failures through the console, since most of these
  APIs fail by refusing rather than by throwing something meaningful.
- **`Bit.Butil.Demo/Client/Docs/DocsNav.cs`** lists the page: title, slug (matching its `@page`), summary, page
  type, `ApiSupport`, `ApiNeeds`, and `Services` when the type names are not the title. That list is the single
  source of truth for the sidebar, the home grid, the browser-support matrix, the pager, the search index and the
  MCP capability catalog - a page missing from it is invisible everywhere except its own URL.
- **`Samples/Bit.Butil.Samples.Core/Pages/`** carries the smaller, hosting-model-neutral version (a `DemoCard`
  per scenario) with an entry in `Shared/NavMenu.razor`, so the same code is exercised under WebAssembly, Server
  and MAUI.
- The extent of the showcase matches the extent of the API: a member with no way to exercise it in the Demo is a
  member nobody can verify works.

## MCP server

The demo server hosts an MCP server (`Bit.Butil.Demo/Server/Controllers/McpController.cs`, seven tools, mirrored
as plain GETs under `/api/mcp/...`). Every feature must be reachable through it, which in practice means:

- Its answers are **derived, not hand-written**: the API reference is reflected out of the shipped assembly with
  its XML docs, capabilities come from `DocsNav`, and the guide and sources are embedded files. So a new API
  reaches the tools by having XML documentation, a `DocsNav` entry, and a page that renders **outside the router
  and layout** (the tools render pages standalone - a page reading something from its surroundings breaks there).
- New files under `Client/`, `Server/Components/` or `Samples/` are picked up by the `EmbeddedResource` globs in
  `Bit.Butil.Demo/Server/Bit.Butil.Demo.Server.csproj`; a file outside them is a source the tools cannot serve.
- Hand-maintained lists that reflection cannot reach - notably `_fastInvokeServices` in
  `Server/Services/ButilCapabilityCatalog.cs` - must be updated along with the feature.
- **Do not add tools.** The surface is deliberately seven: a tool description is paid for in every request of
  every session. A listing is what a retrieval tool answers when called with no argument, not a tool of its own.
  Adding one is a deliberate decision that also changes `tests/Bit.Butil.Tests.Mcp/Infrastructure/ButilMcp.cs`.
- Tool, resource and prompt names are identifiers clients store: renaming one is a breaking change.

## Tests

Cover a feature in whichever of these it belongs to - in more than one, where it belongs to more than one:

| Project | Covers | Run |
| --- | --- | --- |
| `tests/Bit.Butil.Tests.E2E` | Real browser behaviour, through the deterministic harness pages `Samples.Core/Pages/E2EPage.razor` and `E2EObserversPage.razor`. Give every control a stable `id`, write results to the single status element, and avoid APIs that prompt, so the suite stays headless and flake-free. | `dotnet test tests/Bit.Butil.Tests.E2E` (see its README for the browser env vars) |
| `tests/Bit.Butil.Tests.Mcp` | The MCP server against a real child-process deployment driven by a real MCP client: tool surface, behaviour, failures, search, resources, prompts, completions, the HTTP mirror, and cross-catalog consistency. | `dotnet test tests/Bit.Butil.Tests.Mcp` |
| `tests/Bit.Butil.Tests.Manual` | Trimming, the interop contract, and script scanning/bundling/trimming/publishing. A console app because the subject is a *publish* output; it exits non-zero on failure. | See its README - run untrimmed then trimmed from that folder, sharing `interop-manifest.txt` |

`interop-manifest.txt` (this folder, and the Manual harness's copy) is generated from an untrimmed run and is the
contract for `[JSInvokable]` identifiers, JSON payload members and the `[ButilService]` roster. Regenerate it
deliberately when the surface changes; a diff in it is a real interop change, not noise.

New JS modules, or new cross-module dependencies, change what `build.mjs` emits - the bundling checks in the
Manual harness compare the shipped artifacts against the sources, so run that harness after touching `Scripts/`.
