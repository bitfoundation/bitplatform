![Header](https://user-images.githubusercontent.com/6169846/251658486-b16e1db8-5481-46c4-9fc1-c9b279a4364a.png)

<br/>

![License](https://img.shields.io/github/license/bitfoundation/bitplatform.svg)
![Release CI Status](https://github.com/bitfoundation/bitplatform/actions/workflows/bit.ci.release.yml/badge.svg)
![NuGet version](https://img.shields.io/nuget/v/bit.blazorui.svg?logo=nuget)
[![Nuget downloads](https://img.shields.io/badge/packages_download-10M-blue.svg?logo=nuget)](https://www.nuget.org/profiles/bit-foundation)
[![Average time to resolve an issue](http://isitmaintained.com/badge/resolution/bitfoundation/bitplatform.svg)](http://isitmaintained.com/project/bitfoundation/bitplatform "Average time to resolve an issue")
[![Percentage of issues still open](http://isitmaintained.com/badge/open/bitfoundation/bitplatform.svg)](http://isitmaintained.com/project/bitfoundation/bitplatform "Percentage of issues still open")
[![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/bitfoundation/bitplatform)

<br/>

# 🧾 Introduction

**bitplatform** is a set of open-source tools for building fast, polished apps with Blazor and C#. The libraries target **.NET 8, 9 and 10** and are built for Blazor WebAssembly, Server, and Hybrid (MAUI) apps.

For more details, visit us at [bitplatform.dev](https://bitplatform.dev/).

<br/>

# 🚀 Get started

Create a ready-to-run [bit Boilerplate](src/Templates/Boilerplate/Readme.md) project on Windows, macOS or Linux with the [bit CLI](src/Cli/README.md):

```bash
dotnet tool install --global Bit.Cli --prerelease
bit new MyApp
```

`bit new` installs what the project needs (the .NET SDK, Node.js, Docker, the Aspire CLI, and so on), builds it, initializes git, adds the first migration, and opens your IDE. The [create project page](https://bitplatform.dev/templates/create-project) walks you through the options and builds the command for you.

<br/>

# 🧰 What's inside

## UI

- [***bit BlazorUI***](https://blazorui.bitplatform.dev) (`Bit.BlazorUI`): 90+ high-performance, accessible components with Dark and Light themes and RTL support, split across `Bit.BlazorUI`, `Bit.BlazorUI.Extras` (DataGrid, Chart, PdfViewer, RichTextEditor, Map, FullCalendar, CountryPicker, TimeZonePicker, and more), `Bit.BlazorUI.Icons` and `Bit.BlazorUI.Assets`.
- [***BbScanner***](src/BbScanner/README.md) (`Bit.BbScanner`): A dependency-free barcode scanner for Blazor that reads QR Code, Data Matrix, Code 128, Code 39, EAN-13, EAN-8, UPC-A, UPC-E and ITF from the camera or from still images. The decoding is written from scratch in C#; the browser only opens the camera and copies pixels.
- [***Bink***](src/Bink/README.md) (`Bit.Bink`): A Blazor-native drawing and ink library, with pressure- and tilt-aware strokes, brushes, erasers, shapes, annotations, selection, layers, undo/redo, real-time collaboration and SVG/PNG export. The document model and the drawing engine run in C#; the browser only captures input and paints.
- [***Bmotion***](src/Bmotion/README.md) (`Bit.Bmotion`): A Blazor-native animation library inspired by Motion (Framer Motion), with springs, gestures, keyframes, variants, drag, layout (FLIP) and exit animations. The animation math runs in C#, so there's no JavaScript to wire up.
- [***Bonboarding***](src/Bonboarding/README.md) (`Bit.Bonboarding`): A Blazor-native onboarding and product tour library, with spotlights, collision-aware placement, branching and multi-page tours, persistence and built-in accessibility. Every decision is made in C#; the browser layer only measures and observes.

## App infrastructure

- [***Brouter***](src/Brouter/README.md) (`Bit.Brouter`): A modern, declarative, nestable router for Blazor with async guards, data loaders, named routes, query string binding and global navigation hooks. It's the default router in bit Boilerplate.
- [***Butil***](src/Butil/README.md) (`Bit.Butil`): The browser platform in C#. The DOM, storage, media, sensors, crypto, workers and more, exposed as injectable, strongly-typed services, so you don't have to drop back to JavaScript.
- [***Blex***](src/Blex/README.md) (`Bit.Blex`): Reactive state management for Blazor and .NET MAUI. A source generator turns plain `[BlexState]` fields and `[BlexAction]` methods into observable stores, with computed state, effects, middleware, persistence, undo/redo and Redux DevTools time-travel built in.
- [***Besql***](src/Besql/README.md) (`Bit.Besql`): EF Core and SQLite in the browser, for offline-capable web apps that save data locally and sync later.
- [***Bswup***](src/Bswup/README.md) (`Bit.Bswup`): Turns your Blazor app into a PWA with service-worker update progress, and works with prerendering for an experience like GitHub's or Reddit's.

## Build & tooling

- [***bit CLI***](src/Cli/README.md) (`Bit.Cli`): Creates bitplatform projects (`bit new`), sets up cloned ones (`bit setup`), cleans build outputs, renames or removes projects, translates `.resx` files with an LLM, and decodes stack traces minified by Bit.Minifier.
- [***Minifier***](src/Minifier/README.md) (`Bit.Minifier`): Shrinks the .NET assemblies a Blazor WebAssembly app downloads by stripping non-public names and compiler-only attributes after trimming. Install it and publish in Release; there's nothing else to set up.
- [***BlazorES2019***](src/BlazorES2019/README.md) (`Bit.BlazorES2019`): An ES2019 build of the Blazor scripts, so apps also run on older browsers and WebViews (for example Safari before 16.4).
- [***Code Analyzers***](src/CodeAnalyzers/README.md) (`Bit.CodeAnalyzers`) and [***Source Generators***](src/SourceGenerators/README.md) (`Bit.SourceGenerators`): Analyzers that enforce our best practices, and source generators that improve code quality and performance.

## Project templates

- [***bit Boilerplate***](src/Templates/Boilerplate/Readme.md) (`Bit.Boilerplate`): A free, full-stack, cloud-ready project template that brings these libraries together. It builds unified Web, Android, iOS, Windows and macOS apps from a single codebase, with native platform integration and support for third-party Java, Kotlin, Swift, Objective-C and JavaScript libraries. You choose which features to include when you create the project, such as identity (passwordless, 2FA, SSO), multilingual support with RTL, offline database and sync, caching, OpenTelemetry, an AI chatbot with RAG, .NET Aspire, and CI/CD pipelines.
- [***bit BlazorEmpty***](src/Templates/BlazorEmpty/Readme.md) (`Bit.BlazorEmpty`): A minimal Blazor template for starting from scratch.

<br/>

**Note**: This project is tested with [BrowserStack](https://www.browserstack.com/).

<br/>

# 🎁 OSS Showcases

The following apps are our open-source projects powered by the bit platform showcasing the different capabilities of our toolchain:

| | &nbsp;&nbsp;&nbsp;Web&nbsp;&nbsp;&nbsp; | &nbsp;&nbsp;&nbsp;iOS&nbsp;&nbsp;&nbsp; | Android | Windows | macOS |
|:-:|:--:|:--:|:--:|:--:|:--:|
| bitplatform | [![PWA](https://github-production-user-asset-6210df.s3.amazonaws.com/6169846/251381583-8b8eb895-80c9-4811-9641-57a5a08db163.png)](https://bitplatform.dev)| *N/A* | *N/A* | *N/A* | *N/A* |
| Sales | [![PWA](https://github-production-user-asset-6210df.s3.amazonaws.com/6169846/251381583-8b8eb895-80c9-4811-9641-57a5a08db163.png)](https://sales.bitplatform.dev) | *Soon!* | *Soon!* | [![Windows app](https://github-production-user-asset-6210df.s3.amazonaws.com/6169846/251382080-9ae97fea-934c-4097-aca4-124a2aed1595.png)](https://windows-sales.bitplatform.dev/SalesModule.Client.Windows-win-Setup.exe) | *Soon!* |
| bit BlazorUI | [![Prerendered PWA](https://github-production-user-asset-6210df.s3.amazonaws.com/6169846/251381583-8b8eb895-80c9-4811-9641-57a5a08db163.png)](https://blazorui.bitplatform.dev) | [![iOS app](https://github-production-user-asset-6210df.s3.amazonaws.com/6169846/251381842-e72976ce-fd20-431d-a677-ca1ed625b83b.png)](https://apps.apple.com/us/app/bit-blazor-ui/id6450401404) | [![Android app](https://github-production-user-asset-6210df.s3.amazonaws.com/6169846/251381958-24931682-87f6-44fc-a1c7-eecf46387005.png)](https://play.google.com/store/apps/details?id=com.bitplatform.BlazorUI.Demo) | [![Windows app](https://github-production-user-asset-6210df.s3.amazonaws.com/6169846/251382080-9ae97fea-934c-4097-aca4-124a2aed1595.png)](https://windows-components.bitplatform.dev/Bit.BlazorUI.Demo.Client.Windows-win-Setup.exe) | [![macOS app](https://github-production-user-asset-6210df.s3.amazonaws.com/6169846/251382211-0d58f9ba-1a1f-4481-a0ca-b23a393cca9f.png)](https://apps.apple.com/nl/app/bit-blazor-ui/id6450401404)
| AdminPanel | [![Prerendered PWA](https://github-production-user-asset-6210df.s3.amazonaws.com/6169846/251381583-8b8eb895-80c9-4811-9641-57a5a08db163.png)](https://adminpanel.bitplatform.dev) | [![iOS app](https://github-production-user-asset-6210df.s3.amazonaws.com/6169846/251381842-e72976ce-fd20-431d-a677-ca1ed625b83b.png)](https://apps.apple.com/us/app/bit-adminpanel/id6450611349) | [![Android app](https://github-production-user-asset-6210df.s3.amazonaws.com/6169846/251381958-24931682-87f6-44fc-a1c7-eecf46387005.png)](https://play.google.com/store/apps/details?id=com.bitplatform.AdminPanel.Template) | [![Windows app](https://github-production-user-asset-6210df.s3.amazonaws.com/6169846/251382080-9ae97fea-934c-4097-aca4-124a2aed1595.png)](https://windows-admin.bitplatform.dev/AdminPanel.Client.Windows-win-Setup.exe) | [![macOS app](https://github-production-user-asset-6210df.s3.amazonaws.com/6169846/251382211-0d58f9ba-1a1f-4481-a0ca-b23a393cca9f.png)](https://apps.apple.com/nl/app/bit-adminpanel/id6450611349) |
| Todo | [![Prerendered PWA](https://github-production-user-asset-6210df.s3.amazonaws.com/6169846/251381583-8b8eb895-80c9-4811-9641-57a5a08db163.png)](https://todo.bitplatform.dev) | [![iOS app](https://github-production-user-asset-6210df.s3.amazonaws.com/6169846/251381842-e72976ce-fd20-431d-a677-ca1ed625b83b.png)](https://apps.apple.com/us/app/bit-todotemplate/id6450611072) | [![Android app](https://github-production-user-asset-6210df.s3.amazonaws.com/6169846/251381958-24931682-87f6-44fc-a1c7-eecf46387005.png)](https://play.google.com/store/apps/details?id=com.bitplatform.Todo.Template) | [![Windows app](https://github-production-user-asset-6210df.s3.amazonaws.com/6169846/251382080-9ae97fea-934c-4097-aca4-124a2aed1595.png)](https://windows-todo.bitplatform.dev/TodoSample.Client.Windows-win-Setup.exe) | [![macOS app](https://github-production-user-asset-6210df.s3.amazonaws.com/6169846/251382211-0d58f9ba-1a1f-4481-a0ca-b23a393cca9f.png)](https://apps.apple.com/nl/app/bit-todotemplate/id6450611072)

1. [bitplatform.dev](https://bitplatform.dev): .NET 10 Pre-rendered PWA with Blazor WebAssembly (Azure Web App + Cloudflare CDN)
2. [sales.bitplatform.dev](https://sales.bitplatform.dev): .NET 10 Sales Pre-rendered PWA with Blazor WebAssembly (Azure Web App + Cloudflare CDN)
3. [blazorui.bitplatform.dev](https://blazorui.bitplatform.dev): .NET 10 Pre-rendered PWA with Blazor WebAssembly (Azure Web App + Cloudflare CDN)
4. [adminpanel.bitplatform.dev](https://adminpanel.bitplatform.dev): .NET 10 PWA with Blazor WebAssembly (Azure Web App + Cloudflare CDN)
5. [todo.bitplatform.dev](https://todo.bitplatform.dev): .NET 10 Pre-rendered PWA with Blazor WebAssembly (Azure Web App + Cloudflare CDN)
6. [todo-offline.bitplatform.cc](https://todo-offline.bitplatform.cc): .NET 10 Todo demo app with ef-core, sqlite & sync (Azure static web app)
7. [todo-aot.bitplatform.cc](https://todo-aot.bitplatform.cc): .NET 10 AOT Compiled PWA with Blazor WebAssembly Standalone (Azure static web app)
8. [todo-small.bitplatform.cc](https://todo-small.bitplatform.cc): .NET 10 Todo demo app with smaller download footprint (Azure static web app)
9. [adminpanel.bitplatform.cc](https://adminpanel.bitplatform.cc): .NET 10 PWA with Blazor WebAssembly Standalone (Azure static web app)

[Todo](https://todo.bitplatform.dev) & [Adminpanel](https://adminpanel.bitplatform.dev) web apps will launch their respective Android and iOS applications if you have already installed them, mirroring the behavior of apps like YouTube and Instagram.

Prerendering combined with PWA functionality delivers an experience similar to GitHub's and Reddit's. bitplatform brings that combination to Blazor apps built on the modern .NET project structure.

<br/>

# 🤝 How to contribute?

We welcome contributions! Many people all over the world have helped make this project better.

* [Contributing](CONTRIBUTING.md) explains what kinds of contributions we welcome.
* [Build Instructions](docs/how-to-build.md) explains how to build and test.
* [Coding Style](docs/coding-style.md) explains the conventions we follow.
* [Get Up and Running on bit platform](docs/up-and-running.md) explains how to get the latest builds and their libraries to test them in your own projects.
