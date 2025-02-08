# bit BlazorUI: Native High-Performance Library for Blazor
[![NuGet Version](https://img.shields.io/nuget/v/Bit.BlazorUI.svg?style=flat)](https://www.nuget.org/packages/Bit.BlazorUI/) ![Nuget](https://img.shields.io/nuget/dt/Bit.BlazorUI.svg)

offers a comprehensive set of native, high-performance UI controls for Blazor. Developed in C#, it stands independent of external JavaScript frameworks or libraries, ensuring seamless and efficient integration.

**Why choose bit BlazorUI Components?**
- **Free and Open Source**: Bit BlazorUI components come at no cost and are open to the community for contributions and improvements.
- **High Performance**: Built for optimal speed and performance.
- **Blazor Native**: Specifically designed as native Blazor components, ensuring a consistent development experience.
- **Universal Support**: Compatible with all Blazor interactive modes — **Server**, **WASM**, and **Hybrid**.

## Getting Started

To use Bit BlazorUI components, follow these steps:

1. Install the `Bit.BlazorUI` nuget package.
2. In the default document (`App.razor` or `index.html`), add the `Bit.BlazorUI` CSS file reference:

```html

<link rel="stylesheet" href="_content/Bit.BlazorUI/styles/bit.blazorui.css" />

```

3. In the default document (`App.razor` or `index.html`), add the `Bit.BlazorUI` JS file reference:

```html

<script src="_content/Bit.BlazorUI/scripts/bit.blazorui.js"></script>

```

4. In the `_Imports.razor`, add the using `Bit.BlazorUI` to make it available throughout the project.

```razor

@using Bit.BlazorUI;

```

5. Start using BlazorUI components in pages/components following its documents: https://blazorui.bitplatform.dev