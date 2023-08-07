### bit BlazorUI is a native high performance components for Blazor
[![NuGet Version](https://img.shields.io/nuget/v/Bit.Client.Web.BlazorUI.svg?style=flat)](https://www.nuget.org/packages/Bit.BlazorUI/) ![Nuget](https://img.shields.io/nuget/dt/Bit.Client.Web.BlazorUI.svg)

bit BlazorUI is a set of native and high performance Blazor UI controls. This is implemented in C# and this does not depend on or wrap existing JavaScript frameworks or libraries.

**Why choose bit BlazorUI Components?**
- bit BlazorUI components are free
- bit BlazorUI components are open source
- bit BlazorUI components have a high performance
- bit BlazorUI components are Blazor native components
- bit BlazorUI components are supported in both **server-side** and **client-side** (WASM) Blazor

To use the bit BlazorUI components, please follow these steps:

1. Install the `Bit.BlazorUI` nuget package
2. In the default document (`_Host.cshtml`, `index.html` or `_Layout.cshtml`), add the `Bit.BlazorUI` style reference in the head section.

```html

<link rel="stylesheet" href="_content/Bit.BlazorUI/styles/bit.blazorui.css" />

```

3. In the default document (`_Host.cshtml`, `index.html` or `_Layout.cshtml`), add the `Bit.BlazorUI` script reference in the end of body section.

```html

<script src="_content/Bit.BlazorUI/scripts/bit.blazorui.js"></script>

```

4. In the `_Imports.razor`, add the using `Bit.BlazorUI` to make it available throughout the project.

```razor

@using Bit.BlazorUI;

```