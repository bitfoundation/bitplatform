### bit BlazorUI Extras: The extra components package of bit BlazorUI
[![NuGet Version](https://img.shields.io/nuget/v/Bit.BlazorUI.Extras.svg?style=flat)](https://www.nuget.org/packages/Bit.BlazorUI/) ![Nuget](https://img.shields.io/nuget/dt/Bit.BlazorUI.Extras.svg)


**bit BlazorUI Extra** offers a set of extensive UI controls for Blazor.

**Why choose bit BlazorUI Extra components?**
- **Free and Open Source**: Bit BlazorUI components come at no cost and are open to the community for contributions and improvements.
- **Universal Support**: Compatible with all interactive Blazor modes — **Server**, **WASM**, and **Hybrid**.

### Getting Started

To use bit BlazorUI Extra components, follow these steps:

1. Install the `Bit.BlazorUI.Extras` nuget package.

using command line:

```
dotnet add package Bit.BlazorUI.Extras
```

or using Package Manager Console:

```
Install-Package Bit.BlazorUI.Extras
```
 
2. In the default document (`App.razor` or `index.html`), add the `Bit.BlazorUI.Extras` CSS file reference:

```
<link rel="stylesheet" href="_content/Bit.BlazorUI.Extras/styles/bit.blazorui.extras.css" />
```

3. In the default document (`App.razor` or `index.html`), add the `Bit.BlazorUI.Extras` JS file reference:

```
<script src="_content/Bit.BlazorUI.Extras/scripts/bit.blazorui.extras.js"></script>
```

4. In the `_Imports.razor`, add the using `Bit.BlazorUI` to make it available throughout the project.

```
@using Bit.BlazorUI;
```

5. Start using BlazorUI Extra components following its documents: https://blazorui.bitplatform.dev