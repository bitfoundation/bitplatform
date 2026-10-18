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
  `WebAssemblyTrimmingTests`, one of which also runs the trimmed app in Chromium, Firefox and WebKit.
- `Bit.BlazorUI.Tests.Trimming.Hosted` - a Blazor Web App's server hosting `Bit.BlazorUI.Tests.Trimming.Hosted.Client`, a
  WebAssembly client with a service worker. `HostedAppTrimmingTests` checks that the client's service worker asset
  manifest, which the server rewrites after trimming, lists only what the publish serves, with its hash.

`LazyScriptsTrimmingTests` publishes both apps with `BitBlazorUILazyScripts`: one module per script and the boot modules
instead of the bundles, trimmed to what the app can reach, and - with `RUN_BROWSER_TESTS=1` - runs them (the WebAssembly
one through `wwwroot/index.lazy.html`, the host page without the packages' script tags). It also checks that a bundle-mode
publish carries none of the lazy scripts.

`PackageTrimmingTests` packs the packages into a feed of their own and publishes apps outside the repository that
reference them, with each of the .NET 8, 9 and 10 SDKs: the package layout and the SDK paths a ProjectReference never
takes.

## Running

```
dotnet build
dotnet exec bin/Debug/net10.0/Bit.BlazorUI.Tests.Trimming.dll
```

(`dotnet test` reports zero tests under the Microsoft.Testing.Platform runner this repository mandates; run the built
assembly, or `dotnet run`.) The publishes take a few minutes in all.

| Variable | Adds |
| --- | --- |
| `RUN_BROWSER_TESTS=1` | the trimmed WebAssembly and server apps run in Chromium, Firefox and WebKit, each driven through the components whose scripts the trimming kept (Playwright's browsers installed, as for `Tests/Performance`) |
| `RUN_PACKAGE_TESTS=1` | the packed packages under every installed .NET 8, 9 and 10 SDK; their version (`99.0.0-trimming.*`) lands in the machine's NuGet cache |

The publishes run with `-nodeReuse:false`: a node kept from an earlier build holds the task assembly it loaded then,
and the source tree's task is rebuilt along with the packages. Do the same when publishing one of these apps by hand
after changing `Bit.BlazorUI.Build`.
