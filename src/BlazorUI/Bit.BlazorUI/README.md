# bit BlazorUI: Native High-Performance Library for Blazor
[![NuGet Version](https://img.shields.io/nuget/v/Bit.BlazorUI.svg?style=flat)](https://www.nuget.org/packages/Bit.BlazorUI/) ![Nuget](https://img.shields.io/nuget/dt/Bit.BlazorUI.svg)

**bit BlazorUI** offers a comprehensive set of native, light-weight, and high-performance UI controls for Blazor. Developed in C#, it stands independent of external JavaScript frameworks or libraries, ensuring seamless and efficient integration.

**Why choose bit BlazorUI components?**
- **Free and Open Source**: bit BlazorUI components come at no cost and are open to the community for contributions and improvements.
- **Light Weight**: Built for low load time and fast render.
- **High Performance**: Built for optimal speed and performance.
- **Blazor Native**: Specifically designed as native Blazor components, ensuring a consistent development experience.
- **Universal Support**: Compatible with all interactive Blazor modes - **Server**, **WASM**, and **Hybrid**.

### Getting Started

To use bit BlazorUI components, follow these steps:

1. Install the `Bit.BlazorUI` nuget package.

using command line:

```
dotnet add package Bit.BlazorUI
```

or using Package Manager Console:

```
Install-Package Bit.BlazorUI
```
 
2. In the default document (`App.razor` or `index.html`), add the `Bit.BlazorUI` CSS file reference:

```
<link rel="stylesheet" href="_content/Bit.BlazorUI/styles/bit.blazorui.css" />
```

3. In the default document (`App.razor` or `index.html`), add the `Bit.BlazorUI` JS file reference:

```
<script src="_content/Bit.BlazorUI/scripts/bit.blazorui.js"></script>
```

4. In the `_Imports.razor`, add the using `Bit.BlazorUI` to make it available throughout the project.

```
@using Bit.BlazorUI;
```

5. Start using BlazorUI components following its documents: https://blazorui.bitplatform.dev

### Only what your app uses, in production

When an app is published, `bit.blazorui.css` and `bit.blazorui.js` (and the bundles of `Bit.BlazorUI.Extras`, `Bit.BlazorUI.Legacy` and `Bit.BlazorUI.Icons`) are put back together from only the components and scripts the app can use - under the same names, so the tags above stay as they are. A build, `dotnet run` and `dotnet watch` always serve the full bundles.

- A standalone **Blazor WebAssembly** app gets it by default, off what the .NET trimmer kept - as does any project published with `PublishTrimmed`.
- A **Blazor Web App** or **Blazor Server** app turns it on in the server project: `<BitBlazorUITrimAssets>true</BitBlazorUITrimAssets>`. The publish then reads the app's assemblies for what it uses (`BitBlazorUIScan`: `TypeReferences`, the default, or the coarser `TypeNames`).
- `BitBlazorUITrimScripts`, `BitBlazorUITrimStyles`, `BitBlazorUITrimVendorAssets` and `BitBlazorUITrimIcons` (off unless set) control each kind of file, and `<BitBlazorUIComponent Include="..." />`, `<BitBlazorUIAsset Include="..." />` and `<BitBlazorUIIcon Include="..." />` keep what the publish cannot see the app use.

See https://blazorui.bitplatform.dev/getting-started#trimming for the details.

### Lazy scripts

`<BitBlazorUILazyScripts>true</BitBlazorUILazyScripts>` in the project you publish replaces the script bundles with one module per script, imported the first time a component calls into it: remove the `<script>` tags of the packages, and each page downloads only the scripts of the components it uses (a publish keeps only the modules the app can reach). The same switch at runtime: `AddBitBlazorUIServices(options => options.LazyScripts = true)`. See https://blazorui.bitplatform.dev/getting-started#lazy-scripts.