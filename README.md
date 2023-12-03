![Header](https://user-images.githubusercontent.com/6169846/251658486-b16e1db8-5481-46c4-9fc1-c9b279a4364a.png)

<br/>

![License](https://img.shields.io/github/license/bitfoundation/bitplatform.svg)
![Code size](https://img.shields.io/github/languages/code-size/bitfoundation/bitplatform.svg?logo=github)
![CI Status](https://github.com/bitfoundation/bitplatform/actions/workflows/bit.ci.yml/badge.svg)
![NuGet version](https://img.shields.io/nuget/v/bit.blazorui.svg?logo=nuget)
[![Nuget downloads](https://img.shields.io/badge/packages_download-3.9M-blue.svg?logo=nuget)](https://www.nuget.org/profiles/bit-foundation)

<br/>

# 🧾 Introduction

[bit platform](https://bitplatform.dev) is the home ❤️ for .NET developers.

Using C#, HTML, and CSS it offers a full featured dotnet project template equipped with a lot of features a .NET developer needs. With this template, one can also easily switch between different app modes:

* **Blazor Server**: Best for fast development and debugging with hot reload. With the Blazor Server hosting model, the app is executed on the server from within an ASP.NET Core app. UI updates, event handling, and JavaScript calls are handled over a SignalR connection using the WebSockets protocol.
* **Blazor WebAssembly**: Best for SPA deployment. Blazor WebAssembly (WASM) apps run client-side in the browser on a WebAssembly-based .NET runtime. The Blazor app, its dependencies, and the .NET runtime are downloaded to the browser. The app is executed directly on the browser UI thread. UI updates and event handling occur within the same process.
* **Blazor Auto**: Blazor seamlessly combines Blazor Server and WebAssembly. This approach enhances user interaction initially through Blazor Server, while simultaneously downloading Blazor WebAssembly for subsequent visits, reducing server load.
* **Blazor Hybrid - MAUI**: Blazor can also be used to build native client apps using a hybrid approach. Hybrid apps are native apps that leverage web technologies for their functionality. In a Blazor Hybrid app, Razor components run directly in the native app (not on WebAssembly). Blazor Hybrid is on top of .NET MAUI and has access to all native features of supported platforms (Android, iOS, macOS and Windows)

With different deployment types:

* **SPA**: It's referring to a Typical Single Page Application (SPA) without pre-rendering. Best for development / debugging. It is the default option.
* **PWA**: A Blazor WebAssembly app built as a Progressive Web App (PWA) uses modern browser APIs to enable many of the capabilities of a native client app, such as working offline, running in its own app window, launching from the host's operating system, receiving push notifications, and automatically updating in the background.
* **SPA-Prerendered**: Server-side rendering (SSR), is the ability of an application to contribute by displaying the web-page on the server instead of rendering it in the browser. Blazor pre-renders the requested page on the server and sends it as a static page, then later the page becomes an interactive Blazor app on the client. This behavior is intended to serve pages quickly to search engines with time-based positioning. It improves SEO.
* **PWA-Prerendered**: Almost the same as the SPA version, but with the PWA capability which reduces the toll on the server considerebly after the first render.
* **Prerender-Only**: Statically renders the component with the specified parameters without any interactivity on the client. It's recommended when the target is building a static content like a landing page.

This project template is powered by [bit BlazorUI](https://components.bitplatform.dev) components, which are super-fast 🌶 and lightweight making them the best toolbox for developing common apps.

<br/>

# 🎁 OSS Showcases

The following apps are our open-source projects powered by the bit platform showcasing the different capabilities of our toolchain:

| | &nbsp;&nbsp;&nbsp;Web&nbsp;&nbsp;&nbsp; | &nbsp;&nbsp;&nbsp;iOS&nbsp;&nbsp;&nbsp; | Android | Windows | macOS |
|:-:|:--:|:--:|:--:|:--:|:--:|
| bit BlazorUI | [![Prerendered PWA](https://github-production-user-asset-6210df.s3.amazonaws.com/6169846/251381583-8b8eb895-80c9-4811-9641-57a5a08db163.png)](https://components.bitplatform.dev) | [![iOS app](https://github-production-user-asset-6210df.s3.amazonaws.com/6169846/251381842-e72976ce-fd20-431d-a677-ca1ed625b83b.png)](https://apps.apple.com/us/app/bit-blazor-ui/id6450401404) | [![Android app](https://github-production-user-asset-6210df.s3.amazonaws.com/6169846/251381958-24931682-87f6-44fc-a1c7-eecf46387005.png)](https://play.google.com/store/apps/details?id=com.bitplatform.BlazorUI.Demo) | [![Windows app](https://github-production-user-asset-6210df.s3.amazonaws.com/6169846/251382080-9ae97fea-934c-4097-aca4-124a2aed1595.png)](https://github.com/bitfoundation/bitplatform/releases/latest/download/BlazorUIDemo-Windows.zip) | [![macOS app](https://github-production-user-asset-6210df.s3.amazonaws.com/6169846/251382211-0d58f9ba-1a1f-4481-a0ca-b23a393cca9f.png)](https://github.com/bitfoundation/bitplatform/releases/latest/download/BlazorUIDemo-macOS.pkg)
| Todo | [![Prerendered PWA](https://github-production-user-asset-6210df.s3.amazonaws.com/6169846/251381583-8b8eb895-80c9-4811-9641-57a5a08db163.png)](https://todo.bitplatform.dev) | [![iOS app](https://github-production-user-asset-6210df.s3.amazonaws.com/6169846/251381842-e72976ce-fd20-431d-a677-ca1ed625b83b.png)](https://apps.apple.com/us/app/bit-todotemplate/id6450611072) | [![Android app](https://github-production-user-asset-6210df.s3.amazonaws.com/6169846/251381958-24931682-87f6-44fc-a1c7-eecf46387005.png)](https://play.google.com/store/apps/details?id=com.bitplatform.Todo.Template) | [![Windows app](https://github-production-user-asset-6210df.s3.amazonaws.com/6169846/251382080-9ae97fea-934c-4097-aca4-124a2aed1595.png)](https://github.com/bitfoundation/bitplatform/releases/latest/download/TodoTemplate-Windows.zip) | [![macOS app](https://github-production-user-asset-6210df.s3.amazonaws.com/6169846/251382211-0d58f9ba-1a1f-4481-a0ca-b23a393cca9f.png)](https://github.com/bitfoundation/bitplatform/releases/latest/download/TodoTemplate-macOS.pkg)
| AdminPanel | [![SPA](https://github-production-user-asset-6210df.s3.amazonaws.com/6169846/251395129-71a5a79c-af74-4d4e-a0f7-ed9a15cf2e46.png)](https://adminpanel.bitplatform.dev) | [![iOS app](https://github-production-user-asset-6210df.s3.amazonaws.com/6169846/251381842-e72976ce-fd20-431d-a677-ca1ed625b83b.png)](https://apps.apple.com/us/app/bit-adminpanel/id6450611349) | [![Android app](https://github-production-user-asset-6210df.s3.amazonaws.com/6169846/251381958-24931682-87f6-44fc-a1c7-eecf46387005.png)](https://play.google.com/store/apps/details?id=com.bitplatform.AdminPanel.Template) | [![Windows app](https://github-production-user-asset-6210df.s3.amazonaws.com/6169846/251382080-9ae97fea-934c-4097-aca4-124a2aed1595.png)](https://github.com/bitfoundation/bitplatform/releases/latest/download/AdminPanel-Windows.zip) | [![macOS app](https://github-production-user-asset-6210df.s3.amazonaws.com/6169846/251382211-0d58f9ba-1a1f-4481-a0ca-b23a393cca9f.png)](https://github.com/bitfoundation/bitplatform/releases/latest/download/AdminPanel-macOS.pkg) |

<br/>

# How to contribute?

We welcome contributions! Many people all over the world have helped make this project better.

* [Contributing](CONTRIBUTING.md) explains what kinds of contributions we welcome.
* [Build Instructions](docs/how-to-build.md) explains how to build and test.
* [Get Up and Running on bit platform](docs/up-and-running.md) explains how to get the latest builds and their libraries to test them in your own projects.

<br/>

# **Contributions**

![Alt](https://repobeats.axiom.co/api/embed/66dc1fc04ed967094b98ac118e8f18fa38b19f6a.svg "bit platform open source contributions report")
