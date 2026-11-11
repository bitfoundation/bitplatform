# Bit.BlazorUI.Tests.Trimming

Checks the publish-time trimming of the packages' scripts and stylesheets (see `CLAUDE.md` and
`Bit.BlazorUI/buildTransitive/Bit.BlazorUI.targets`) the only way it can be checked: by publishing real apps and reading
what came out. Everything the trimming does happens inside `dotnet publish` - the gate, the task, the static web asset
swap, the endpoints - so no test calling into `Bit.BlazorUI.Build` reaches it.

Each test publishes one of two apps with a set of properties, in Release, and compares the published bundles with the
pieces the packages' builds wrote (`obj/bit-assets`), which a trimmed bundle is a subset of in the full bundle's order:

- `Bit.BlazorUI.Tests.Trimming.Fixture` - a Blazor Web App's server rendering a known handful of components
  interactively, trimmed off the scan of its assemblies (a publish ILLink does not touch). `ServerAppTrimmingTests`, one
  of which also runs the trimmed app in Chromium, Firefox and WebKit.
- `Bit.BlazorUI.Tests.Consumer` - a Blazor WebAssembly app, trimmed by default off what ILLink kept.
  `WebAssemblyTrimmingTests`, one of which also runs the trimmed app in Chromium, Firefox and WebKit. Its publish copies
  what ILLink left of the packages' assemblies beside it (`trimmed/`), since the published ones are WebAssembly modules no
  metadata reader opens: what the tests read of the params types an app creates or not, and of the map of the lazy
  scripts.
- `Bit.BlazorUI.Tests.Trimming.Hosted` - a Blazor Web App's server hosting `Bit.BlazorUI.Tests.Trimming.Hosted.Client`, a
  WebAssembly client with a service worker, in the `Default` static web asset mode of a Blazor Web App's client. The client
  writes `service-worker-assets.js` from the full bundles and every vendored file before the server trims, and
  `HostedAppTrimmingTests` checks that the server's publish rewrites it to what it serves - in bundle and in lazy mode, and
  with the client under a base path of its own (`BitTrimmingClientBasePath`), which the SDK writes every URL under - and
  that the client published on its own, a standalone PWA, needs no rewrite at all: every listed file published under its
  hash, the service worker carrying the manifest's version, every compressed copy and every endpoint describing the file
  it serves. Each reads the rewrite's own line out of the build output and
  asserts it either way, so a fixture that stopped producing the mismatch fails rather than pass over a manifest nobody had
  to fix. The rewriting itself, on the shapes of manifest no publish here produces, is `BitServiceWorkerManifestTests` in
  `Bit.BlazorUI.Tests`.

`AiWireFormatTrimmingTests` publishes `Bit.BlazorUI.Tests.Trimming.AiWire`, a console app, trimmed and as native AOT, and
runs it: every built-in `BitAiEvent` and a request round-trip through the AI package's wire format, with no trim or AOT
warning from the packages. The native AOT one is inconclusive on a machine without the platform linker.

`LazyAiTrimmingTests` publishes `Bit.BlazorUI.Tests.Trimming.LazyAi`, a trimmed Blazor WebAssembly app with the lazy
scripts whose chat is lazy-loaded the way the Trimming page's "Lazy-loading the AI package" section sets it up:
`Bit.BlazorUI.AI`, `Bit.BlazorUI.AI.Core` and `Bit.BlazorUI.Tests.Trimming.LazyAi.Chat` (the library holding the chat's
page) are `BlazorWebAssemblyLazyLoad` assemblies, and nothing of the AI packages is registered at startup. It checks the
boot configuration lists them as lazy, that the publish still keeps the chat's stylesheets, modules, boot module,
initializer and glyph font, and - with `RUN_BROWSER_TESTS=1` - that the app starts without downloading any of the AI
assemblies, modules or font, and that once the chat opens they come, the chat is styled and answers, and its copy
button draws its glyph and reaches its script.

`LazyScriptsTrimmingTests` publishes both apps with `BitBlazorUILazyScripts`: one module per script and the boot modules
instead of the bundles, trimmed to what the app can reach, and - with `RUN_BROWSER_TESTS=1` - runs them (the WebAssembly
one through `wwwroot/index.lazy.html`, the host page without the packages' script tags). It also checks that a bundle-mode
publish carries none of the lazy scripts.

`PackageTrimmingTests` packs the packages into a feed of their own and publishes apps outside the repository that
reference them, with the .NET 10 SDK: the package layout and the SDK paths a ProjectReference never takes. Its Blazor
Web App's client has a service worker too, whose manifest has to come out rewritten, and so does its Blazor WebAssembly
app, a standalone PWA whose own manifest must be left to the SDK.

`TrimmedSizeReportTests` records what the three apps keep: for every bundle the trimming put together (the line it logs,
`Bit.BlazorUI: {package}/{bundle} -> {kept} of {all} pieces, {trimmed} of {full} bytes`), the pieces and bytes kept of
the full bundle's, and the size of the Brotli and gzip copies the publish wrote of it - and, from `LazyAiTrimmingTests`' publish, what the AI
package costs an app that lazy-loads it, part by part (`lazyAiPackage`: what it downloads as it starts and once the chat
opens, the figures the Trimming page states). It writes them, with the commit,
the SDK and the OS, to `trimmed-bundle-sizes.json` in the run's results directory (`--results-directory`, `TestResults`
beside the test app otherwise), which CI uploads with the TRX. No size fails it: it is there to follow the sizes from
one run to the next. It reuses the publishes the other classes make, so it costs a run of the whole suite nothing. A change that moves the
AI package's figures much updates the Trimming page's "What it costs" table.

## Running

```
dotnet build
dotnet exec bin/Debug/net11.0/Bit.BlazorUI.Tests.Trimming.dll
```

`dotnet test` runs it too, under the Microsoft.Testing.Platform runner `src/global.json` selects, and so does
`dotnet run`; filter as usual (`dotnet test --filter "FullyQualifiedName~ServerAppTrimmingTests"`). The publishes take
about ten minutes in all, without the opt-in tests below.

CI runs it with both variables below set, in the `trimming` job of `.github/workflows/bit.ci.BlazorUI.e2e.yml`.

| Variable | Adds |
| --- | --- |
| `RUN_BROWSER_TESTS=1` | the trimmed WebAssembly and server apps run in Chromium, Firefox and WebKit, each driven through the components whose scripts the trimming kept (Playwright's browsers installed, as for `Tests/Performance`) |
| `RUN_PACKAGE_TESTS=1` | the packed packages under the installed .NET 10 SDK; their version (`99.0.0-trimming.*`) lands in the machine's NuGet cache |

The publishes run with `-nodeReuse:false`: a node kept from an earlier build holds the task assembly it loaded then,
and the source tree's task is rebuilt along with the packages. Do the same when publishing one of these apps by hand
after changing `Bit.BlazorUI.Build`.
