namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Legacy.MarkdownViewer;

public partial class BitMarkdownViewerLegacyDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
           Name = "JsMiddlewareIdentifier",
           Type = "string?",
           DefaultValue = "null",
           Description = @"The fully qualified JavaScript function identifier to invoke as JavaScript middleware after parsing.
                           The string should reference a global JS function (e.g. <c>""myApp.sanitizeHtml""</c>) that accepts
                           an HTML string and returns the processed HTML string.
                           JavaScript middleware is skipped during server-side prerendering.",
        },
        new()
        {
           Name = "Markdown",
           Type = "string?",
           DefaultValue = "null",
           Description = "The Markdown string value to render as an html element.",
        },
        new()
        {
           Name = "Middleware",
           Type = "Func<string, Task<string>>?",
           DefaultValue = "null",
           Description = @"The C# function to run after parsing markdown and before rendering HTML.
                           The middleware receives the parsed HTML string and returns the processed HTML string.
                           C# middleware is applied after JavaScript middleware.",
        },
        new()
        {
           Name = "NoPrerender",
           Type = "bool",
           DefaultValue = "false",
           Description = "Disables parse and render of the markdown content in the prerendering phase.",
        },
        new()
        {
           Name = "OnParsing",
           Type = "EventCallback<string?>",
           DefaultValue = "null",
           Description = "A callback that is called before starting to parse the markdown.",
        },
        new()
        {
           Name = "OnParsed",
           Type = "EventCallback<string?>",
           DefaultValue = "null",
           Description = "A callback that is called after parsing the markdown.",
        },
        new()
        {
           Name = "OnRendered",
           Type = "EventCallback<string?>",
           DefaultValue = "null",
           Description = "A callback that is called after rendering the parsed markdown.",
        },
    ];



    private string advancedMarkdown = @"![Header](/_content/Bit.BlazorUI.Demo.Client.Core/images/markdown/bitplatform-banner.webp)

<br/>

![License](https://img.shields.io/github/license/bitfoundation/bitplatform.svg)
![CI Status](https://img.shields.io/github/actions/workflow/status/bitfoundation/bitplatform/bit.ci.BlazorUI.yml?logo=github)
![NuGet version](https://img.shields.io/nuget/v/bit.blazorui.svg?logo=nuget)
[![Nuget downloads](https://img.shields.io/badge/packages_download-8.2M-blue.svg?logo=nuget)](https://www.nuget.org/profiles/bit-foundation)
[![Closed issues](https://img.shields.io/github/issues-closed/bitfoundation/bitplatform?logo=github)](https://isitmaintained.com/project/bitfoundation/bitplatform ""Closed issues"")
[![Open issues](https://img.shields.io/github/issues/bitfoundation/bitplatform?logo=github)](https://isitmaintained.com/project/bitfoundation/bitplatform ""Open issues"")

<br/>

# 🧾 Introduction

At **bitplatform**, we've curated a comprehensive toolkit to empower you in crafting the finest projects using Blazor. Diverging from others merely offering UI Toolkits, ***bit BlazorUI components*** distinguishes itself with over 80 components, with a compact size of under 400 KB. These components boast both Dark and Light Themes, delivering unparalleled High Performance 🚀

Yet, bitplatform doesn't stop there. Our platform introduces exclusive tools that elevate your development experience:

***Bswup***: This unique tool harnesses the power of Progressive Web Apps (PWA) within the innovative new structure of dotnet 8. By amalgamating pre-rendering techniques reminiscent of renowned platforms like GitHub, Reddit, and Facebook, Bswup ensures an exceptional user experience 😍

***Butil***: Embracing Blazor because of your love for C#? Butil enables you to stay true to that sentiment by providing essential Browser APIs in C#, eliminating the need to revert to JavaScript for any functionality 👌

***Besql***: Dreaming of an offline web application capable of saving data and syncing later? Enter Besql, your solution to incorporating ef core & sqlite in your browser. It's a crucial aid for achieving this objective seamlessly 🕺

***Bit Boilerplate Project Template***: If the aforementioned features have piqued your interest, dive into the Bit Boilerplate project template. Experience everything mentioned above along with additional features such as ASP.NET Core Identity integration, multilingualism, and other cool features that empowers you to develop unified Web, Android, iOS, Windows, and macOS apps from a single codebase, while providing seamless integration with native platform features and third-party Java, Kotlin, Swift, Objective-C, and JavaScript libraries 💯

For more details, visit us at [bitplatform.dev](https://bitplatform.dev/).

<br/>

**Note**: This project is tested with [BrowserStack](https://www.browserstack.com/).

<br/>

# 🎁 OSS Showcases

The following apps are our open-source projects powered by the bit platform showcasing the different capabilities of our toolchain:

| | &nbsp;&nbsp;&nbsp;Web&nbsp;&nbsp;&nbsp; | &nbsp;&nbsp;&nbsp;iOS&nbsp;&nbsp;&nbsp; | Android | Windows | macOS |
|:-:|:--:|:--:|:--:|:--:|:--:|
| bitplatform | [![PWA](/_content/Bit.BlazorUI.Demo.Client.Core/images/markdown/badge-pwa.png)](https://bitplatform.dev)| *N/A* | *N/A* | *N/A* | *N/A* |
| Sales | [![PWA](/_content/Bit.BlazorUI.Demo.Client.Core/images/markdown/badge-pwa.png)](https://sales.bitplatform.dev) | *Soon!* | *Soon!* | [![Windows app](/_content/Bit.BlazorUI.Demo.Client.Core/images/markdown/badge-windows.png)](https://windows-sales.bitplatform.dev/SalesModule.Client.Windows-win-Setup.exe) | *Soon!* |
| bit BlazorUI | [![Prerendered PWA](/_content/Bit.BlazorUI.Demo.Client.Core/images/markdown/badge-pwa.png)](https://blazorui.bitplatform.dev) | [![iOS app](/_content/Bit.BlazorUI.Demo.Client.Core/images/markdown/badge-ios.png)](https://apps.apple.com/us/app/bit-blazor-ui/id6450401404) | [![Android app](/_content/Bit.BlazorUI.Demo.Client.Core/images/markdown/badge-android.png)](https://play.google.com/store/apps/details?id=com.bitplatform.BlazorUI.Demo) | [![Windows app](/_content/Bit.BlazorUI.Demo.Client.Core/images/markdown/badge-windows.png)](https://windows-components.bitplatform.dev/Bit.BlazorUI.Demo.Client.Windows-win-Setup.exe) | [![macOS app](/_content/Bit.BlazorUI.Demo.Client.Core/images/markdown/badge-macos.png)](https://apps.apple.com/nl/app/bit-blazor-ui/id6450401404)
| AdminPanel | [![Prerendered PWA](/_content/Bit.BlazorUI.Demo.Client.Core/images/markdown/badge-pwa.png)](https://adminpanel.bitplatform.dev) | [![iOS app](/_content/Bit.BlazorUI.Demo.Client.Core/images/markdown/badge-ios.png)](https://apps.apple.com/us/app/bit-adminpanel/id6450611349) | [![Android app](/_content/Bit.BlazorUI.Demo.Client.Core/images/markdown/badge-android.png)](https://play.google.com/store/apps/details?id=com.bitplatform.AdminPanel.Template) | [![Windows app](/_content/Bit.BlazorUI.Demo.Client.Core/images/markdown/badge-windows.png)](https://windows-admin.bitplatform.dev/AdminPanel.Client.Windows-win-Setup.exe) | [![macOS app](/_content/Bit.BlazorUI.Demo.Client.Core/images/markdown/badge-macos.png)](https://apps.apple.com/nl/app/bit-adminpanel/id6450611349) |
| Todo | [![Prerendered PWA](/_content/Bit.BlazorUI.Demo.Client.Core/images/markdown/badge-pwa.png)](https://todo.bitplatform.dev) | [![iOS app](/_content/Bit.BlazorUI.Demo.Client.Core/images/markdown/badge-ios.png)](https://apps.apple.com/us/app/bit-todotemplate/id6450611072) | [![Android app](/_content/Bit.BlazorUI.Demo.Client.Core/images/markdown/badge-android.png)](https://play.google.com/store/apps/details?id=com.bitplatform.Todo.Template) | [![Windows app](/_content/Bit.BlazorUI.Demo.Client.Core/images/markdown/badge-windows.png)](https://windows-todo.bitplatform.dev/TodoSample.Client.Windows-win-Setup.exe) | [![macOS app](/_content/Bit.BlazorUI.Demo.Client.Core/images/markdown/badge-macos.png)](https://apps.apple.com/nl/app/bit-todotemplate/id6450611072)

1. [bitplatform.dev](https://bitplatform.dev): .NET 9 Pre-rendered PWA with Blazor WebAssembly (Azure Web App + Cloudflare CDN)
2. [sales.bitplatform.dev](https://sales.bitplatform.dev): .NET 9 Sales Pre-rendered PWA with Blazor WebAssembly (Azure Web App + Cloudflare CDN)
3. [blazorui.bitplatform.dev](https://blazorui.bitplatform.dev): .NET 9 Pre-rendered PWA with Blazor WebAssembly (Azure Web App + Cloudflare CDN)
4. [adminpanel.bitplatform.dev](https://adminpanel.bitplatform.dev): .NET 9 PWA with Blazor WebAssembly (Azure Web App + Cloudflare CDN)
5. [todo.bitplatform.dev](https://todo.bitplatform.dev): .NET 8 Pre-rendered PWA with Blazor WebAssembly (Azure Web App + Cloudflare CDN)
6. [adminpanel.bitplatform.cc](https://adminpanel.bitplatform.cc): .NET 9 PWA with Blazor WebAssembly Standalone (Azure static web app)
7. [todo-aot.bitplatform.cc](https://todo-aot.bitplatform.cc): .NET 9 AOT Compiled PWA with Blazor WebAssembly Standalone (Azure static web app)
8. [todo-small.bitplatform.cc](https://todo-small.bitplatform.cc): .NET 9 Todo demo app with smaller download footprint (Azure static web app)
9. [todo-offline.bitplatform.cc](https://todo-offline.bitplatform.cc): .NET 9 Todo demo app with ef-core & sqlite (Azure static web app)

[Todo](https://todo.bitplatform.dev) & [Adminpanel](https://adminpanel.bitplatform.dev) web apps will launch their respective Android and iOS applications if you have already installed them, mirroring the behavior of apps like YouTube and Instagram. 

Prerendering combined with PWA functionality delivers an experience akin to that of GitHub and Reddit. The bitplatform solution, seamlessly integrated with the innovative new .NET 8 project structure, stands as the exclusive remedy for such a scenario within the realm of Blazor.

# How to contribute?

We welcome contributions! Many people all over the world have helped make this project better.

* [Contributing](CONTRIBUTING.md) explains what kinds of contributions we welcome.
* [Build Instructions](docs/how-to-build.md) explains how to build and test.
* [Get Up and Running on bit platform](docs/up-and-running.md) explains how to get the latest builds and their libraries to test them in your own projects.

<br/>

# **Contributions**

![Alt](/_content/Bit.BlazorUI.Demo.Client.Core/images/markdown/repobeats.svg ""bit platform open source contributions report"")";

    private DateTimeOffset? parsingDateTime;
    private DateTimeOffset? parsedDateTime;
    private DateTimeOffset? renderedDateTime;

    private void OnParsing(string? markdown)
    {
        parsingDateTime = DateTimeOffset.Now;
    }

    private void OnParsed(string? html)
    {
        parsedDateTime = DateTimeOffset.Now;
    }

    private void OnRendered(string? html)
    {
        renderedDateTime = DateTimeOffset.Now;
    }



    private readonly string example1RazorCode = @"
<BitMarkdownViewerLegacy Markdown=""@(""# Marked in the browser\n\nRendered by [**marked**](https://marked.js.org)."")"" />";

    private readonly string example2RazorCode = @"
<style>
    .advanced {
        img {
            max-width: 100%;
        }
    }
</style>
<BitMarkdownViewerLegacy Markdown=""@advancedMarkdown"" Class=""advanced"" />";
    private readonly string example2CsharpCode = @"
private string advancedMarkdown = @""![Header](/_content/Bit.BlazorUI.Demo.Client.Core/images/markdown/bitplatform-banner.webp)

<br/>

![License](https://img.shields.io/github/license/bitfoundation/bitplatform.svg)
![CI Status](https://img.shields.io/github/actions/workflow/status/bitfoundation/bitplatform/bit.ci.BlazorUI.yml?logo=github)
![NuGet version](https://img.shields.io/nuget/v/bit.blazorui.svg?logo=nuget)
[![Nuget downloads](https://img.shields.io/badge/packages_download-8.2M-blue.svg?logo=nuget)](https://www.nuget.org/profiles/bit-foundation)
[![Closed issues](https://img.shields.io/github/issues-closed/bitfoundation/bitplatform?logo=github)](https://isitmaintained.com/project/bitfoundation/bitplatform """"Closed issues"""")
[![Open issues](https://img.shields.io/github/issues/bitfoundation/bitplatform?logo=github)](https://isitmaintained.com/project/bitfoundation/bitplatform """"Open issues"""")

<br/>

# 🧾 Introduction

At **bitplatform**, we've curated a comprehensive toolkit to empower you in crafting the finest projects using Blazor. Diverging from others merely offering UI Toolkits, ***bit BlazorUI components*** distinguishes itself with over 80 components, with a compact size of under 400 KB. These components boast both Dark and Light Themes, delivering unparalleled High Performance 🚀

Yet, bitplatform doesn't stop there. Our platform introduces exclusive tools that elevate your development experience:

***Bswup***: This unique tool harnesses the power of Progressive Web Apps (PWA) within the innovative new structure of dotnet 8. By amalgamating pre-rendering techniques reminiscent of renowned platforms like GitHub, Reddit, and Facebook, Bswup ensures an exceptional user experience 😍

***Butil***: Embracing Blazor because of your love for C#? Butil enables you to stay true to that sentiment by providing essential Browser APIs in C#, eliminating the need to revert to JavaScript for any functionality 👌

***Besql***: Dreaming of an offline web application capable of saving data and syncing later? Enter Besql, your solution to incorporating ef core & sqlite in your browser. It's a crucial aid for achieving this objective seamlessly 🕺

***Bit Boilerplate Project Template***: If the aforementioned features have piqued your interest, dive into the Bit Boilerplate project template. Experience everything mentioned above along with additional features such as ASP.NET Core Identity integration, multilingualism, and other cool features that empowers you to develop unified Web, Android, iOS, Windows, and macOS apps from a single codebase, while providing seamless integration with native platform features and third-party Java, Kotlin, Swift, Objective-C, and JavaScript libraries 💯

For more details, visit us at [bitplatform.dev](https://bitplatform.dev/).

<br/>

**Note**: This project is tested with [BrowserStack](https://www.browserstack.com/).

<br/>

# 🎁 OSS Showcases

The following apps are our open-source projects powered by the bit platform showcasing the different capabilities of our toolchain:

| | &nbsp;&nbsp;&nbsp;Web&nbsp;&nbsp;&nbsp; | &nbsp;&nbsp;&nbsp;iOS&nbsp;&nbsp;&nbsp; | Android | Windows | macOS |
|:-:|:--:|:--:|:--:|:--:|:--:|
| bitplatform | [![PWA](/_content/Bit.BlazorUI.Demo.Client.Core/images/markdown/badge-pwa.png)](https://bitplatform.dev)| *N/A* | *N/A* | *N/A* | *N/A* |
| Sales | [![PWA](/_content/Bit.BlazorUI.Demo.Client.Core/images/markdown/badge-pwa.png)](https://sales.bitplatform.dev) | *Soon!* | *Soon!* | [![Windows app](/_content/Bit.BlazorUI.Demo.Client.Core/images/markdown/badge-windows.png)](https://windows-sales.bitplatform.dev/SalesModule.Client.Windows-win-Setup.exe) | *Soon!* |
| bit BlazorUI | [![Prerendered PWA](/_content/Bit.BlazorUI.Demo.Client.Core/images/markdown/badge-pwa.png)](https://blazorui.bitplatform.dev) | [![iOS app](/_content/Bit.BlazorUI.Demo.Client.Core/images/markdown/badge-ios.png)](https://apps.apple.com/us/app/bit-blazor-ui/id6450401404) | [![Android app](/_content/Bit.BlazorUI.Demo.Client.Core/images/markdown/badge-android.png)](https://play.google.com/store/apps/details?id=com.bitplatform.BlazorUI.Demo) | [![Windows app](/_content/Bit.BlazorUI.Demo.Client.Core/images/markdown/badge-windows.png)](https://windows-components.bitplatform.dev/Bit.BlazorUI.Demo.Client.Windows-win-Setup.exe) | [![macOS app](/_content/Bit.BlazorUI.Demo.Client.Core/images/markdown/badge-macos.png)](https://apps.apple.com/nl/app/bit-blazor-ui/id6450401404)
| AdminPanel | [![Prerendered PWA](/_content/Bit.BlazorUI.Demo.Client.Core/images/markdown/badge-pwa.png)](https://adminpanel.bitplatform.dev) | [![iOS app](/_content/Bit.BlazorUI.Demo.Client.Core/images/markdown/badge-ios.png)](https://apps.apple.com/us/app/bit-adminpanel/id6450611349) | [![Android app](/_content/Bit.BlazorUI.Demo.Client.Core/images/markdown/badge-android.png)](https://play.google.com/store/apps/details?id=com.bitplatform.AdminPanel.Template) | [![Windows app](/_content/Bit.BlazorUI.Demo.Client.Core/images/markdown/badge-windows.png)](https://windows-admin.bitplatform.dev/AdminPanel.Client.Windows-win-Setup.exe) | [![macOS app](/_content/Bit.BlazorUI.Demo.Client.Core/images/markdown/badge-macos.png)](https://apps.apple.com/nl/app/bit-adminpanel/id6450611349) |
| Todo | [![Prerendered PWA](/_content/Bit.BlazorUI.Demo.Client.Core/images/markdown/badge-pwa.png)](https://todo.bitplatform.dev) | [![iOS app](/_content/Bit.BlazorUI.Demo.Client.Core/images/markdown/badge-ios.png)](https://apps.apple.com/us/app/bit-todotemplate/id6450611072) | [![Android app](/_content/Bit.BlazorUI.Demo.Client.Core/images/markdown/badge-android.png)](https://play.google.com/store/apps/details?id=com.bitplatform.Todo.Template) | [![Windows app](/_content/Bit.BlazorUI.Demo.Client.Core/images/markdown/badge-windows.png)](https://windows-todo.bitplatform.dev/TodoSample.Client.Windows-win-Setup.exe) | [![macOS app](/_content/Bit.BlazorUI.Demo.Client.Core/images/markdown/badge-macos.png)](https://apps.apple.com/nl/app/bit-todotemplate/id6450611072)

1. [bitplatform.dev](https://bitplatform.dev): .NET 9 Pre-rendered PWA with Blazor WebAssembly (Azure Web App + Cloudflare CDN)
2. [sales.bitplatform.dev](https://sales.bitplatform.dev): .NET 9 Sales Pre-rendered PWA with Blazor WebAssembly (Azure Web App + Cloudflare CDN)
3. [blazorui.bitplatform.dev](https://blazorui.bitplatform.dev): .NET 9 Pre-rendered PWA with Blazor WebAssembly (Azure Web App + Cloudflare CDN)
4. [adminpanel.bitplatform.dev](https://adminpanel.bitplatform.dev): .NET 9 PWA with Blazor WebAssembly (Azure Web App + Cloudflare CDN)
5. [todo.bitplatform.dev](https://todo.bitplatform.dev): .NET 8 Pre-rendered PWA with Blazor WebAssembly (Azure Web App + Cloudflare CDN)
6. [adminpanel.bitplatform.cc](https://adminpanel.bitplatform.cc): .NET 9 PWA with Blazor WebAssembly Standalone (Azure static web app)
7. [todo-aot.bitplatform.cc](https://todo-aot.bitplatform.cc): .NET 9 AOT Compiled PWA with Blazor WebAssembly Standalone (Azure static web app)
8. [todo-small.bitplatform.cc](https://todo-small.bitplatform.cc): .NET 9 Todo demo app with smaller download footprint (Azure static web app)
9. [todo-offline.bitplatform.cc](https://todo-offline.bitplatform.cc): .NET 9 Todo demo app with ef-core & sqlite (Azure static web app)

[Todo](https://todo.bitplatform.dev) & [Adminpanel](https://adminpanel.bitplatform.dev) web apps will launch their respective Android and iOS applications if you have already installed them, mirroring the behavior of apps like YouTube and Instagram. 

Prerendering combined with PWA functionality delivers an experience akin to that of GitHub and Reddit. The bitplatform solution, seamlessly integrated with the innovative new .NET 8 project structure, stands as the exclusive remedy for such a scenario within the realm of Blazor.

# How to contribute?

We welcome contributions! Many people all over the world have helped make this project better.

* [Contributing](CONTRIBUTING.md) explains what kinds of contributions we welcome.
* [Build Instructions](docs/how-to-build.md) explains how to build and test.
* [Get Up and Running on bit platform](docs/up-and-running.md) explains how to get the latest builds and their libraries to test them in your own projects.

<br/>

# **Contributions**

![Alt](/_content/Bit.BlazorUI.Demo.Client.Core/images/markdown/repobeats.svg """"bit platform open source contributions report"""")"";";

    private readonly string example3RazorCode = @"
<BitMarkdownViewerLegacy Markdown=""@(""# Events of the BitMarkdownViewerLegacy:\n\n- OnParsing\n- OnParsed\n- OnRendered"")""
                   Id=""test-mdv""
                   OnParsing=""OnParsing""
                   OnParsed=""OnParsed""
                   OnRendered=""OnRendered"" />
<hr />
<div>Parsing at [@parsingDateTime?.ToString(""o"")]</div>
<div>Parsed at [@parsedDateTime?.ToString(""o"")]</div>
<div>Rendered at [@renderedDateTime?.ToString(""o"")]</div>";
    private readonly string example3CsharpCode = @"
private DateTimeOffset? parsingDateTime;
private DateTimeOffset? parsedDateTime;
private DateTimeOffset? renderedDateTime;

private void OnParsing(string? markdown)
{
    parsingDateTime = DateTimeOffset.Now;
}

private void OnParsed(string? html)
{
    parsedDateTime = DateTimeOffset.Now;
}

private void OnRendered(string? html)
{
    renderedDateTime = DateTimeOffset.Now;
}";
}
