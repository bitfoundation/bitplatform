# Bit.Bonboarding

A Blazor-native onboarding and user-guidance library: product tours, spotlights, tooltips,
contextual hints and full application walkthroughs, written in C# and Razor.

It is not a wrapper. There is no JavaScript tour engine underneath - the browser layer is a single
small module that measures elements, watches them for movement, scrolls, and routes keyboard events.
Every decision about what to show, when to show it, and where to put it is made in C#.

```razor
<BonboardingTour @ref="tour" Id="welcome" AutoStart="BonboardingAutoStart.Once">
    <BonboardingStep Anchor="new-project"
                     Title="Create a project"
                     Description="Start by creating your first project." />

    <BonboardingStep Anchor="settings"
                     Title="Customise your workspace"
                     Description="Configure your preferences here." />
</BonboardingTour>
```

```razor
<button data-bonboarding-anchor="new-project">New project</button>
```

## Highlights

- **Multi-step tours** with progress indicators, keyboard navigation and resumable state.
- **Collision-aware positioning** computed in C#: shift before flip, arrow clamping, viewport
  padding, and sane behaviour for targets larger than the screen.
- **Elements that are not there yet** - wait, skip, centre or fail, per step, driven by a mutation
  observer rather than a polling timer.
- **Interactive steps** that advance when the user actually uses the highlighted control.
- **Conditional and branching flows**, where Back follows the path the user took.
- **Route-aware tours** that span pages, and that wait rather than fighting the user for the address
  bar.
- **Accessibility as architecture**: dialog semantics, focus trap and restoration, live-region
  announcements, reduced motion, forced colors.
- **Theming through CSS variables**, per-step templates, or a fully headless popover that still gets
  measurement, placement and focus management.
- **Guidance that stays on the page**: beacons that open hints or tours, onboarding checklists that tick
  themselves off, and a list of tours to start again from a help menu.
- **Respectful by default**: an automatic tour never interrupts one the user is taking (it queues, by
  priority), "remind me later" snoozes, and completion is remembered across replays of the same tour version.
- **Definitions checked before anyone sees them**, and a user held on a step is told why.
- **Analytics-grade events**, including how long each step was on screen.
- Works unchanged on **Blazor Server, Blazor WebAssembly and Blazor Web App**, including while
  prerendering, and can be lazy loaded on WebAssembly.
- No external CSS framework, and no JavaScript dependency beyond the library's own module.

## Quick start

```bash
dotnet add package Bit.Bonboarding
```

Register the services (in both projects of a Blazor Web App):

```csharp
builder.Services.AddBitBonboardingServices();
```

Reference the stylesheet from your host page:

```html
<link rel="stylesheet" href="_content/Bit.Bonboarding/bit-bonboarding.css" />
```

Add one host component, usually in your main layout:

```razor
<BonboardingRoot />
```

The full walkthrough is in [getting started](#getting-started) and [installation](#installation).

## Requirements

- .NET 10 or .NET 11
- Blazor Server, Blazor WebAssembly, or Blazor Web App (Server, WebAssembly or Auto)

## The demo

```bash
dotnet run --project src/Utilities/Bonboarding/Demo/Server
```

The demo covers placement and spotlights, dynamic, missing and replaced targets, steps that advance on
an interaction or an application signal, conditional and branching flows, driving, queueing, snoozing
and validating tours, beacons, checklists and tour lists, a tour spanning two routes, theming, RTL and
localisation, headless rendering, and a live event log with the stored record beside it.

[`Samples`](Samples/) is the hosting-model harness rather than the docs: one shared [Core](Samples/Core/) project of
pages - a welcome tour that starts once by itself, a tour across two routes with a step that waits for a signal, a
beacon, a checklist and a tour list, and the stored progress beside the live event stream - hosted unchanged under
[Server](Samples/Server/), [WASM](Samples/Wasm/) and [Auto](Samples/Auto/), and under [MAUI](Samples/Maui/)
(Android, iOS, Mac Catalyst and Windows), where a `BlazorWebView` runs them as a Blazor Hybrid app. Their
`Program.cs` (and the MAUI sample's `MauiProgram.cs`) is the registration to copy.

```bash
dotnet run --project src/Utilities/Bonboarding/Samples/Server
dotnet run --project src/Utilities/Bonboarding/Samples/Wasm/Bit.Bonboarding.Samples.Wasm
dotnet run --project src/Utilities/Bonboarding/Samples/Auto/Bit.Bonboarding.Samples.Auto
dotnet build -t:Run -f net10.0-android src/Utilities/Bonboarding/Samples/Maui
```

`Bit.Bonboarding.slnx` includes the MAUI sample, whose Android, iOS and Mac Catalyst (and, on Windows, Windows)
targets need the `maui-*` workloads (`dotnet workload restore Samples/Maui/Bit.Bonboarding.Samples.Maui.csproj`
from this folder); without them the solution build fails with `NETSDK1147`. `Bit.Bonboarding.Web.slnf` is every
other project and needs nothing but the SDK: it is what CI builds, and what to build and test from the command line
(`dotnet build Bit.Bonboarding.Web.slnf`, `dotnet test --solution Bit.Bonboarding.Web.slnf -f net10.0`).

## How it fits together

```
IBonboardingService             Starts, stops and queries tours. Scoped, one per user.
 └── IBonboardingTourSession    The state machine: step selection, branching, waiting,
      │                         routing, persistence. Renders nothing.
      ├── BonboardingPlacementEngine
      │                         Pure C#. Given a target rectangle, a popover size and a
      │                         viewport, decides where the popover goes.
      ├── IBonboardingStore     Where progress lives.
      └── IBonboardingInterop   The only seam to the DOM: measure, observe, scroll, focus,
                                keys. Substitutable, which is how the engine is tested
                                without a browser.

BonboardingRoot                 Renders the running session. Owns the popover element so
                                measurement, placement, focus and dialog semantics stay
                                correct even when you supply the contents.
BonboardingTour / Step          Markup that builds a BonboardingTourDefinition. No rendering
                                of their own.
```

The separation is the point: the engine has no opinion about rendering, the layout engine has no
opinion about the DOM, and the browser layer has no opinion about tours.

## MCP server

The demo hosts a [Model Context Protocol](https://modelcontextprotocol.io) server at `/mcp`, so an AI
agent building tours reads the library itself - its compiled API documentation, this guide, its
stylesheet and the demo's working tours - instead of guessing at an API it half remembers.

```json
{
  "mcpServers": {
    "bonboarding": { "type": "http", "url": "https://bonboarding.bitplatform.dev/mcp" }
  }
}
```

| Tool | What it answers |
|---|---|
| `SearchBonboarding` | Everything at once - the guide, every public member, the theme variables and the demo sources - each hit with the exact follow-up call |
| `GetBonboardingSetupGuide` | The complete wiring for one render mode: `server`, `wasm`, `auto` or `standalone-wasm` |
| `GetBonboardingGuideSection` | One section of this guide, or the index of sections |
| `GetBonboardingApi` | One public type with every parameter, property and method, its real default and its documentation - or the index of types |
| `GetBonboardingThemeVariables` | Every `--bit-bonboarding-*` variable with its light and dark value |
| `InspectBonboardingPlacement` | Where the library's own placement engine puts a popover for a given target, popover and viewport, and why |
| `GetBonboardingSourceFile` | One demo file verbatim, or the index of files |

Every tool is read-only. The same material is exposed as `bonboarding://` resources, and the
`add-bonboarding-to-app`, `implement-bonboarding-tour` and `debug-bonboarding-tour` prompts walk an
agent through the common jobs in the right order. Every tool is also a plain `GET` under
`/api/mcp/...`, for reading in a browser exactly what an agent is handed.

## Repository layout

```
Bit.Bonboarding                 The library
Demo                            Demo application (Blazor Web App: Server + Client) and the MCP server
Samples/Core                    The samples' shared pages
Samples/{Server,Wasm,Auto,Maui} The same pages in each hosting model
Tests/Bit.Bonboarding.Tests     Engine, layout and component tests
Tests/Bit.Bonboarding.Tests.Mcp The MCP server, exercised over the protocol
Tests/Bit.Bonboarding.Tests.E2E The demo in Chromium, Firefox and WebKit, with axe-core (see its README)
```

## Documentation

- [Getting started](#getting-started)
- [Installation](#installation)
- [Basic tours](#basic-tours)
- [Highlighting and contextual help](#highlighting-and-contextual-help)
- [Beacons, checklists and tour lists](#beacons-checklists-and-tour-lists)
- [Dynamic and async targets](#dynamic-and-async-targets)
- [Conditional and branching flows](#conditional-and-branching-flows)
- [Multi-page tours](#multi-page-tours)
- [Theming](#theming)
- [Headless and custom rendering](#headless-and-custom-rendering)
- [Localization and RTL](#localization-and-rtl)
- [Accessibility](#accessibility)
- [Persistence](#persistence)
- [Events and analytics](#events-and-analytics)
- [Performance](#performance)
- [Upgrading](#upgrading)
- [API reference](#api-reference)

## Getting started

This walks through a real first tour: marking targets, declaring steps, starting it once per user,
and letting people replay it from a help menu.

If you have not installed the package yet, start with [installation](#installation).

### 1. Mark the things you want to point at

A step needs to know which element it is about. The most robust way is an attribute on the element:

```razor
<button class="primary" data-bonboarding-anchor="new-project">New project</button>

<input type="search" data-bonboarding-anchor="search" placeholder="Search" />

<nav data-bonboarding-anchor="sidebar"> ... </nav>
```

`data-bonboarding-anchor` is just an attribute. It survives re-renders, works on elements that do not exist
yet, and does not require you to hold on to an `ElementReference`.

If you prefer, there is a helper that splats the same attribute:

```razor
<button @attributes="BitBonboarding.Anchor("new-project")">New project</button>
```

CSS selectors and captured element references work too - see [basic tours](#targeting).

### 2. Declare the tour

```razor
@page "/"

<BonboardingTour @ref="_tour" Id="welcome" Title="Welcome tour">

    <BonboardingStep Id="intro"
                     Title="Welcome aboard"
                     Description="A quick look around. Press Escape at any point to leave." />

    <BonboardingStep Id="create"
                     Anchor="new-project"
                     Title="Create a project"
                     Description="Everything starts here."
                     Placement="BonboardingPlacement.Bottom" />

    <BonboardingStep Id="search"
                     Anchor="search"
                     Title="Find anything"
                     Description="Search across every project you can see." />

    <BonboardingStep Id="done"
                     Title="That is it"
                     Description="You can replay this from the help menu."
                     DoneLabel="Finish" />

</BonboardingTour>
```

Some things worth noticing:

- **The first and last steps have no target.** A step without one is shown as a centred card, which
  is the right shape for a welcome or a summary.
- **Every step has an explicit `Id`.** Ids are optional, but they are what makes resuming, branching
  and analytics stable when you later insert a step.
- `BonboardingTour` renders no markup of its own. The `BonboardingRoot` in your layout displays it.

### 3. Start it

Once per user, automatically:

```razor
<BonboardingTour Id="welcome" AutoStart="BonboardingAutoStart.Once">
```

`Once` checks the store first and does nothing if the user already completed or dismissed the tour.
It starts after the first render, so it is safe under prerendering.

Or start it yourself, which is what you want when the tour should follow some application event:

```razor
@inject IBonboardingService BonboardingService

<button @onclick="StartAsync">Show me around</button>

@code {
    private BonboardingTour? _tour;

    private Task StartAsync() => _tour!.StartAsync();
}
```

`IBonboardingService` is the general entry point:

```csharp
await BonboardingService.StartAsync(tour);          // always start, resuming saved progress
await BonboardingService.StartOnceAsync(tour);      // only if not already completed or dismissed
await BonboardingService.RestartAsync(tour);        // forget progress and start from the top
```

### 4. Let people replay it

Because a tour is registered by id, a help menu anywhere in the application can start it:

```razor
@inject IBonboardingService BonboardingService

<button @onclick='() => BonboardingService.StartAsync("welcome")'>Replay the tour</button>
```

### 5. Know how it went

```csharp
builder.Services.AddBitBonboardingObserver<TourAnalytics>();
```

```csharp
public sealed class TourAnalytics(IAnalyticsClient client) : IBonboardingObserver
{
    public ValueTask OnEventAsync(BonboardingEvent e, CancellationToken ct = default)
        => e.Kind switch
        {
            BonboardingEventKind.StepShown =>
                client.TrackAsync("tour_step", new { tour = e.Tour.Id, step = e.Step?.Id }),

            BonboardingEventKind.TourCompleted or BonboardingEventKind.TourSkipped =>
                client.TrackAsync("tour_ended", new { tour = e.Tour.Id, reason = e.Reason }),

            _ => ValueTask.CompletedTask,
        };
}
```

See [events and analytics](#events-and-analytics) for the full stream, including per-step durations.

### The same tour, as data

The declarative components build a `BonboardingTourDefinition`. Building one directly is equally supported, and
is what you want when tours come from configuration, a CMS or a database:

```csharp
var tour = new BonboardingTourDefinition
{
    Id = "welcome",
    Title = "Welcome tour",
    Steps =
    {
        new BonboardingStepDefinition
        {
            Id = "intro",
            Title = "Welcome aboard",
            Description = "A quick look around.",
        },
        new BonboardingStepDefinition
        {
            Id = "create",
            Target = BonboardingStepTarget.Anchor("new-project"),
            Placement = BonboardingPlacement.Bottom,
            Title = "Create a project",
            Description = "Everything starts here.",
        },
    },
};

await BonboardingService.StartOnceAsync(tour);
```

### Where to go next

- [Basic tours](#basic-tours) - targeting, buttons, per-step options
- [Dynamic targets](#dynamic-and-async-targets) - elements that render late or never
- [Conditional and branching flows](#conditional-and-branching-flows) - different tours for different users
- [Theming](#theming) - make it look like your product

## Installation

### Package

```bash
dotnet add package Bit.Bonboarding
```

Targets .NET 10 and .NET 11.

### 1. Register the services

```csharp
using Bit.Bonboarding;

builder.Services.AddBitBonboardingServices();
```

Everything is registered as **scoped**, which is per-circuit on Blazor Server and per-application on
WebAssembly. That matches the lifetime of a user's onboarding state. A WebAssembly app that downloads Bonboarding
only when it is needed turns a switch on instead - see [lazy loading](#lazy-loading-on-webassembly).

Global defaults can be set at the same time:

```csharp
builder.Services.AddBitBonboardingServices(options =>
{
    options.Placement = BonboardingPlacement.Bottom;
    options.Progress = BonboardingProgressStyle.Bar;
    options.Labels.Skip = "Not now";
    options.StorageKeyPrefix = "acme:onboarding:";
});
```

Every option is described in [the API reference](#bonboardingoptions).

#### Blazor Web App

A Blazor Web App has two projects, and a component rendered with `InteractiveServer` resolves
services from the server project while a component rendered with `InteractiveWebAssembly` resolves
them from the client project. Register in **both**, with the same configuration:

```csharp
// Server project: Program.cs
builder.Services.AddBitBonboardingServices(ConfigureOnboarding);

// Client project: Program.cs
builder.Services.AddBitBonboardingServices(ConfigureOnboarding);
```

If your tours only ever run inside `InteractiveServer` components, registering in the server project
alone is enough. `InteractiveWebAssembly` components need both unless their prerendering is off.

While a page prerenders, every component on it runs on the server, whatever its render mode, so it gets the
server project's registration and the options configured there. Once the page turns interactive, an
`InteractiveServer` component keeps those, while an `InteractiveWebAssembly` component switches to the client
project's registration and options - and an `InteractiveAuto` one uses the project it ends up running in. If the two
configurations differ, the placement, labels and storage prefix change under the user as the page turns interactive.

A missing registration therefore fails at different moments:

- **Missing in the server project:** the page fails while it prerenders, and an `InteractiveServer` component fails
  whether it prerenders or not. A Bonboarding component fails with an error that names `AddBitBonboardingServices`;
  a component that injects `IBonboardingService` fails with Blazor's own "There is no registered service of type
  'Bit.Bonboarding.IBonboardingService'". Only components that never run on the server - `InteractiveWebAssembly`
  with prerendering off - are unaffected.
- **Missing in the client project:** prerendering succeeds, on the server project's registration, and the same two
  errors appear only once the page turns interactive on WebAssembly. The `Bit.Bonboarding.LazyLoad` switch the
  error mentions is not the fix: it is for [lazy loading](#lazy-loading-on-webassembly), and an injected
  `IBonboardingService` fails with it on too.

Call `AddBitBonboardingServices` in both projects, through one shared method.

### 2. Reference the stylesheet

Add this to `App.razor` (Blazor Web App), `index.html` (WebAssembly) or `_Host.cshtml`
(Blazor Server), before your own stylesheet so your overrides win:

```html
<link rel="stylesheet" href="_content/Bit.Bonboarding/bit-bonboarding.css" />
```

If your project uses fingerprinted static assets, the usual form applies:

```razor
<link rel="stylesheet" href="@Assets["_content/Bit.Bonboarding/bit-bonboarding.css"]" />
```

The stylesheet has no dependencies and defines no global selectors: everything is scoped under
`.bit-bonboarding-root`. See [theming](#theming) to restyle it.

### 3. Add the host component

Place one `BonboardingRoot` in your layout. It renders whichever tour is running, from anywhere in the
application:

```razor
@* MainLayout.razor *@
<div class="page">
    @Body
</div>

<BonboardingRoot />
```

One host is enough for the whole application. Add more only when you deliberately want to render a
particular session yourself - see [headless rendering](#headless-and-custom-rendering).

### 4. Add `@using Bit.Bonboarding`

Put it in `_Imports.razor` so the components and types are available everywhere:

```razor
@using Bit.Bonboarding
```

### Prerendering

The library is prerender-safe. During prerendering there is no browser, so:

- no measurement happens and no popover is positioned;
- the default store falls back to its in-memory mirror, so reads return nothing rather than throwing;
- the engine still runs, so a tour started during prerendering does not crash the render. Its hooks
  run, its events are published and its progress is written to the in-memory mirror, all for a tour
  nobody sees.

The important consequence is **where you start a tour**. `OnInitializedAsync` runs during
prerendering *and* again after hydration, which would start a tour twice in two different scopes.
Start tours from `OnAfterRenderAsync(firstRender: true)`, from a user action, or with
`BonboardingTour.AutoStart`, which already does the right thing:

```razor
<BonboardingTour Id="welcome" AutoStart="BonboardingAutoStart.Once">
    ...
</BonboardingTour>
```

### Blazor Server reconnection

When a circuit drops and reconnects, Blazor creates a new scope: services, sessions and the JS module
are all rebuilt. The library treats interop failures during teardown as expected rather than as
errors, so a lost circuit never leaves a broken overlay behind or throws into your logs.

Because tour progress is written to the store after every step, a reconnected user resumes where they
were as long as persistence is on and the tour is started with `Resume` (the default).

### Content Security Policy

The library loads one ES module from your own origin (`_content/Bit.Bonboarding/bit-bonboarding.js`) and
uses inline `style` attributes for positioning. A policy that allows Blazor itself will already allow
both, with the exception of a strict `style-src` that forbids `unsafe-inline`; positioning cannot be
expressed without inline styles.

### Lazy loading on WebAssembly

A WebAssembly app can fetch Bonboarding only when someone opens a part of the app that has tours. Such an app cannot
call `AddBitBonboardingServices` - `Program.cs` runs before the assembly is there - so it turns on the
`Bit.Bonboarding.LazyLoad` runtime switch instead. With the switch on and the services not registered,
`BonboardingRoot`, `BonboardingTour`, `BonboardingBeacon`, `BonboardingChecklist` and `BonboardingTourList` create
every service they need themselves, once per browser connection, and `IServiceProvider.GetBonboardingService()` hands
the app's own code the same `IBonboardingService`. A tour's hooks still reach the app's own services through their
context. In the project that runs in the browser, mark the assembly as lazy loaded and turn the switch on, then load
the assembly before the first page that uses it renders:

```xml
<ItemGroup>
  <BlazorWebAssemblyLazyLoad Include="Bit.Bonboarding.wasm" />
  <RuntimeHostConfigurationOption Include="Bit.Bonboarding.LazyLoad" Value="true" />
</ItemGroup>
```

```razor
@* App.razor: the router waits for OnNavigateAsync before it renders the page. *@
@using Microsoft.AspNetCore.Components.WebAssembly.Services
@inject LazyAssemblyLoader LazyAssemblyLoader

<Router AppAssembly="typeof(App).Assembly" OnNavigateAsync="OnNavigateAsync">
    ...
</Router>

@code {
    private async Task OnNavigateAsync(NavigationContext context)
    {
        // The pages with tours: fetch Bonboarding before they render. A second call loads nothing again. Routes
        // match regardless of case, so /Admin/users is one of them too, and /administrators is not.
        var path = context.Path.Split('?', '#')[0]; // the path keeps the query string and the fragment
        if (path.Equals("admin", StringComparison.OrdinalIgnoreCase) || path.StartsWith("admin/", StringComparison.OrdinalIgnoreCase))
            await LazyAssemblyLoader.LoadAssembliesAsync(["Bit.Bonboarding.wasm"]);
    }
}
```

Those pages, or a layout only they use, host the tours and reach the service through `GetBonboardingService()`
instead of injecting `IBonboardingService`:

```razor
@* AdminLayout.razor *@
@inherits LayoutComponentBase
@inject IServiceProvider Services

<nav>
    <a href="admin/users" data-bonboarding-anchor="users">Users</a>
    <button @onclick="ReplayAsync">Replay the tour</button>
</nav>

@Body

<BonboardingTour Id="admin-welcome" AutoStart="BonboardingAutoStart.Once">
    <BonboardingStep Anchor="users" Title="Users" Description="Invite people and manage their roles here." />
</BonboardingTour>

<BonboardingRoot />

@code {
    private Task ReplayAsync() => Services.GetBonboardingService().StartAsync("admin-welcome");
}
```

The stylesheet is a plain `<link>`, served whether the assembly has loaded or not, and the components import their
JavaScript module the first time they need it.

- Leave `AddBitBonboardingServices` out of that project's `Program.cs`: calling it references Bonboarding's types
  before the assembly is there. For the same reason only what renders after the assembly has loaded may use
  Bonboarding's types - the pages it is loaded for and a layout only they use, not `App.razor`, a layout every page
  shares or `Program.cs`. That includes `<BonboardingRoot />`: it goes in that layout, not in the main one.
- The switch is what tells a lazy loading app apart from one that forgot the registration: without it, a missing
  registration fails with an error that names `AddBitBonboardingServices`, as it does everywhere else.
  `AppContext.SetSwitch("Bit.Bonboarding.LazyLoad", true);` at the top of `Program.cs` turns it on too, without
  referencing Bonboarding's types.
- No type in an assembly the router reads when the app starts - the app's own assembly first of all - may derive
  from or implement one of Bonboarding's: no `IBonboardingObserver`, `IBonboardingStore` or `IBonboardingLocalizer`
  class. The router reads the types of those assemblies at startup, and on WebAssembly a type whose base type or
  interface lives in an assembly that is not loaded yet fails to load - and keeps failing after Bit.Bonboarding has
  loaded, breaking the app's use of it for the rest of the session. Put such a class in a library that is lazy loaded
  together with Bit.Bonboarding.
- Without the registration the components run on the default `BonboardingOptions`. A tour and a step override most
  of them - every `BonboardingStepOptions` member, `Labels` and `Persist` - on their definition or as parameters of
  `BonboardingTour` and `BonboardingStep`. The rest - `StorageKeyPrefix`, `StorageLocation`, `StorageScope`,
  `AutoNavigateToStepRoute` and `EnableJsDiagnostics` - keep their defaults; an app that needs them registers the
  services rather than lazy loading.
- The store, the localizer and the observers cannot be replaced either: progress goes to the browser's
  `localStorage` under the default `bit-bonboarding:` prefix, which every app on the same origin shares, for the
  device as a whole rather than per user. A tour that must not write to the browser sets `Persist = false`; analytics
  subscribe to `GetBonboardingService().EventRaised` instead of registering an observer.
- Do not lazy load in an AOT build. AOT compiles every assembly into `dotnet.native.wasm`, so there is nothing left
  to download later, and lazy loading an AOT-compiled assembly can fail at startup.

### Verifying the setup

The quickest check:

```razor
@inject IBonboardingService BonboardingService

<button data-bonboarding-anchor="probe" @onclick="RunAsync">Test</button>

@code {
    private Task RunAsync() => BonboardingService.StartAsync(new BonboardingTourDefinition
    {
        Id = "probe",
        Persist = false,
        Steps =
        {
            new BonboardingStepDefinition
            {
                Target = BonboardingStepTarget.Anchor("probe"),
                Title = "It works",
                Description = "Positioning, spotlight and keyboard navigation are all live.",
            },
        },
    });
}
```

If the popover appears but is not positioned, the stylesheet is not loaded. If nothing appears at
all, `BonboardingRoot` is missing from the layout.

## Basic tours

### Two ways to define a tour

#### Declarative

`BonboardingTour` collects its `BonboardingStep` children, in render order, into a `BonboardingTourDefinition`
and registers it with the service. Neither component renders any markup.

```razor
<BonboardingTour @ref="_tour" Id="welcome" Title="Welcome" Version="1">
    <BonboardingStep Anchor="new-project" Title="Create a project" />
    <BonboardingStep Anchor="search" Title="Find anything" />
</BonboardingTour>
```

The definition is rebuilt whenever parameters change, and the same object is kept, so editing a
running tour is picked up without restarting it.

Steps normally keep their declaration order. A step with an `Id` that is removed and rendered again
returns to its original position, but one that only appears after the others (inside an `@if` that
starts out false, say) is placed last. Give a step an explicit `Order` when it is rendered
conditionally and you need its position pinned, or keep it rendered and skip it with `When`.

`Order="n"` places a step as if it were the n-th step to render, just ahead of the step that actually
rendered n-th. Steps with the same `Order` keep their render order among themselves:

```razor
<BonboardingStep Id="intro" ... />
@if (showAdvanced)
{
    <BonboardingStep Id="advanced" Order="2" ... />   @* always second, ahead of "finish" *@
}
<BonboardingStep Id="finish" ... />
```

`Configure` on the tour and on each step runs every time the definition is rebuilt, which is whenever
the tour or one of its steps receives parameters. Keep it idempotent: assign values, do not append
to collections or subscribe to events.

#### Programmatic

```csharp
var tour = new BonboardingTourDefinition
{
    Id = "welcome",
    Steps =
    {
        new BonboardingStepDefinition { Target = BonboardingStepTarget.Anchor("new-project"), Title = "Create a project" },
        new BonboardingStepDefinition { Target = "#search", Title = "Find anything" },
    },
};
```

Use this when tours are data: loaded from configuration, generated per tenant, or assembled from a
database.

### Targeting

A step's `Target` is a `BonboardingStepTarget`. There are five kinds.

| | |
|---|---|
| `BonboardingStepTarget.Anchor("name")` | The element carrying `data-bonboarding-anchor="name"`. **Preferred.** |
| `BonboardingStepTarget.Css("#id .cls")` | The first element matching a CSS selector. |
| `BonboardingStepTarget.Element(reference)` | A captured `ElementReference`. |
| `BonboardingStepTarget.Dynamic(resolver)` | Decided when the step activates. See [dynamic targets](#dynamic-and-async-targets). |
| `BonboardingStepTarget.None` | No element: a centred card. The default. |

Strings and element references convert implicitly, so all of these are equivalent to explicit calls:

```csharp
new BonboardingStepDefinition { Target = "#search" }        // a CSS selector
new BonboardingStepDefinition { Target = _searchElement }   // an ElementReference
```

In markup, the `BonboardingStep` component exposes each kind as its own parameter, which reads better
than a cast:

```razor
<BonboardingStep Anchor="new-project" ... />
<BonboardingStep Selector="#search" ... />
<BonboardingStep Element="_element" ... />
<BonboardingStep Target="BonboardingStepTarget.Dynamic(...)" ... />
```

#### Prefer anchors over element references

An `ElementReference` is captured for one particular DOM node. If Blazor re-renders and replaces that
node, the reference is stale. An anchor is re-queried on every measurement, so it survives
re-rendering, virtualization and elements that appear later. Selectors have the same advantage.

#### Highlighting several elements at once

`AdditionalTargets` folds more elements into the same spotlight. The popover is positioned against
the bounding union:

```csharp
new BonboardingStepDefinition
{
    Target = BonboardingStepTarget.Anchor("toolbar-save"),
    AdditionalTargets = [BonboardingStepTarget.Anchor("toolbar-undo"), BonboardingStepTarget.Anchor("toolbar-redo")],
    Title = "Editing controls",
}
```

### Steps without a target

A step with no target is centred in the viewport with no spotlight and no arrow. This is the right
shape for a welcome, a summary, a mid-tour explanation, or a feature announcement:

```csharp
new BonboardingStepDefinition
{
    Title = "What is new in March",
    Description = "Three things worth two minutes of your time.",
}
```

### The buttons

The default footer shows, in order: progress, skip, back, your custom actions, and the forward
button. Each part can be turned off per step, per tour or globally:

```csharp
new BonboardingStepDefinition
{
    ShowSkip = false,
    ShowPrevious = false,
    ShowClose = false,
    Progress = BonboardingProgressStyle.None,
    NextLabel = "Got it",
}
```

`Back` is hidden on the first step and the forward button reads `DoneLabel` on the last one, both
automatically.

#### Custom actions

`Actions` adds buttons. Each has a handler, an effect, or both:

```csharp
new BonboardingStepDefinition
{
    Title = "Invite your team?",
    ShowNext = false,
    Actions =
    [
        new BonboardingStepAction
        {
            Label = "Invite people",
            Style = BonboardingStepActionStyle.Primary,
            OnClick = ctx => ctx.Services.GetRequiredService<IDialogs>().ShowInviteAsync(),
            Effect = BonboardingStepActionEffect.Next,
        },
        new BonboardingStepAction
        {
            Label = "Later",
            Style = BonboardingStepActionStyle.Ghost,
            Effect = BonboardingStepActionEffect.Next,
        },
    ],
}
```

The handler runs first and is awaited; the effect is applied afterwards, and is skipped if the
handler already ended the tour or moved to another step. Effects are `None`, `Next`, `Previous`,
`GoTo` (with `TargetStepId`), `Complete`, `Skip` and `Dismiss`.

Set `AutoFocus` on an action to make it, rather than the forward button, the control that receives
focus when the step appears.

### Driving a tour

```csharp
await session.NextAsync();
await session.PreviousAsync();
await session.GoToAsync("billing");   // by id
await session.GoToAsync(3);           // by index

await session.CompleteAsync();        // finished, recorded as completed
await session.SkipAsync();            // user skipped the rest
await session.DismissAsync();         // user closed it
await session.CancelAsync();          // interrupted; progress kept for next time

await session.PauseAsync();           // hide, keep the position
await session.ResumeAsync();

await session.RefreshAsync();         // re-measure after your own layout change
```

`CancelAsync` is the one worth knowing about: unlike dismissing, it does not record that the user
turned the tour down, so `StartOnceAsync` will offer it again and `StartAsync` will resume it.

#### Reading state

```csharp
session.Status            // Idle, Running, Waiting, Paused, Completed, Skipped, Dismissed, Cancelled, Failed
session.CurrentStep       // the BonboardingStepDefinition on screen
session.DisplayPosition   // 1-based, counting only steps that will actually be shown
session.DisplayCount
session.IsFirstStep / IsLastStep
session.History           // ids of the steps actually visited
session.State             // a per-session dictionary shared by every hook and template
```

Subscribe to `session.Changed` to re-render your own UI alongside a tour.

### Lifecycle hooks

```csharp
new BonboardingStepDefinition
{
    OnBeforeShow = async ctx => await ctx.Services.GetRequiredService<IDataLoader>().PreloadAsync(),
    OnShown      = ctx => { Log("shown", ctx.Step.Id); return ValueTask.CompletedTask; },
    OnBeforeLeave = ctx => ValidateAsync(ctx),   // throwing fails the tour; use CanAdvance to block
}
```

```csharp
new BonboardingTourDefinition
{
    CanStart = ctx => ValueTask.FromResult(user.IsInRole("Member")),
    OnStarted = ctx => ValueTask.CompletedTask,
    OnStepChanged = args => ValueTask.CompletedTask,
    OnEnded = args => ValueTask.CompletedTask,
}
```

Every hook receives a `BonboardingStepContext` (or `BonboardingTourContext`) carrying the session, the step, the service
provider and a cancellation token that fires when the step is left.

Throwing from a hook fails the tour cleanly: the session ends with `BonboardingTourEndReason.Failed`, `Error` is
populated, `OnEnded` still runs, and everything is torn down. It never escapes as an unobserved
exception.

### Running more than one tour

Starting a tour cancels any other running one by default, including one that is still starting, so
two starts that race each other (a double click, or a layout's `StartOnceAsync` alongside a page's
`AutoStart`) leave exactly one session behind. The cancelled tour keeps its progress and resumes next time.

That default suits a start the user asked for. A start nobody asked for - a page's tour starting itself -
should not take the screen from a tour the user is in the middle of, so it can **queue** instead:

```csharp
await BonboardingService.StartOnceAsync(featureTour, new BonboardingStartOptions
{
    WhenBusy = BonboardingBusyBehavior.Queue,   // or Skip: do not start at all if something is running
});
```

A queued start returns its session at once, in `BonboardingTourStatus.Idle`, and publishes `TourQueued`.
When the running tour ends, the queued tour with the highest `BonboardingTourDefinition.Priority` starts
first (equal priorities in the order they were queued), and only one runs at a time. Eligibility is checked
again at that moment - a once-only tour the user finished in the meantime, one they snoozed, or one whose
`CanStart` now says no is cancelled instead of started. Cancel a queued session yourself to withdraw it.
A declarative tour's `AutoStart` always queues, and withdraws its queued start when its page goes away.
Starting the tour that is already running is a restart, never a conflict to wait out. Nor is a tour the user
walked away from (see [Navigating away mid-tour](#navigating-away-mid-tour)): nothing of it is on screen and
they may never go back, so a queued start replaces it rather than waiting for it, and it keeps its progress.

To run several at once - a tour plus a persistent hint, say - opt out of all of this:

```csharp
await BonboardingService.StartAsync(hint, new BonboardingStartOptions { StopOthers = false });
```

Each session is independent. A single `BonboardingRoot` renders `IBonboardingService.Active`; to show a second
one, give another host an explicit `Session`. See [headless rendering](#several-hosts).

### Start options

```csharp
await BonboardingService.StartAsync(tour, new BonboardingStartOptions
{
    StartAtStepId = "billing",   // begin here instead of the first step
    Resume = false,              // ignore saved progress
    Force = true,                // start even if completed, dismissed, snoozed, or CanStart says no
    StopOthers = false,
    WhenBusy = BonboardingBusyBehavior.Queue,   // wait for a running tour instead of replacing it
    State = new Dictionary<string, object?> { ["plan"] = "pro" },
});
```

### Checking a definition

`StartAsync` validates the definition before anything is shown. **Errors** refuse the start with a
`BonboardingException` listing every one of them, because the tour could not work: two steps sharing an
id (a jump, Back and resuming would all find the first), a blank or reserved (`$end`) step id, an action
that jumps to a step that does not exist or names none, a negative `WaitTimeout`, a tour with no id or no
steps. **Warnings** are logged and the tour starts: a step that hides Next and has no other way forward, an
`AdvanceOn` step whose target the user cannot reach (`Interaction = Blocked`) or that has no target to
listen on, an absolute URL as a route, an `OverlayOpacity` outside 0-1.

Run the same check in a unit test, so a mistake is caught before a user meets it:

```csharp
[TestMethod]
public void The_welcome_tour_is_well_formed()
    => Assert.IsEmpty(WelcomeTour.Create().Validate());
```

Each `BonboardingValidationIssue` carries a `Severity`, a stable `Code` (`"duplicate-step-id"`), a message
naming the tour and the step, and the `StepId`.

## Highlighting and contextual help

Not everything is a tour. A single step is a perfectly good hint, and the same engine powers both.

### A one-step hint

```csharp
await BonboardingService.StartAsync(new BonboardingTourDefinition
{
    Id = "export-hint",
    Persist = false,
    Steps =
    {
        new BonboardingStepDefinition
        {
            Target = BonboardingStepTarget.Anchor("export"),
            Title = "Exports live here now",
            Description = "The old menu item has moved.",
            Progress = BonboardingProgressStyle.None,
            ShowSkip = false,
            ShowPrevious = false,
            DoneLabel = "Got it",
        },
    },
});
```

A one-step tour is automatically on its last step, so the forward button reads `DoneLabel` and
finishing it completes the tour.

### A feature announcement

Announcements usually want to be centred, shown once, and to not dim the page too heavily:

```csharp
await BonboardingService.StartOnceAsync(new BonboardingTourDefinition
{
    Id = "whats-new-2024-03",
    Version = 1,
    OverlayOpacity = 0.35,
    Steps =
    {
        new BonboardingStepDefinition
        {
            Title = "Three new things",
            Description = "Saved views, bulk editing, and a much faster search.",
            DoneLabel = "Have a look",
        },
    },
});
```

`StartOnceAsync` never shows it twice. Bump `Version` when the announcement changes and it will be
offered again.

### The spotlight

The spotlight is a transparent box whose outer shadow does the dimming, so the cut-out stays sharp at
any zoom level and animates smoothly between steps.

```csharp
new BonboardingStepDefinition
{
    ShowSpotlight = true,               // false dims without a cut-out
    SpotlightShape = BonboardingSpotlightShape.Rounded,   // Rounded, Rectangle, Circle, None
    SpotlightPadding = 8,               // breathing room around the target
    SpotlightRadius = 10,               // corner radius, ignored by Circle and Rectangle
}
```

`SpotlightPadding` also grows the rectangle the popover is positioned against, so the gap you see
between the highlight and the popover is `SpotlightPadding + Offset`.

To dim without highlighting anything, set `SpotlightShape = BonboardingSpotlightShape.None`. To highlight
without dimming, keep the spotlight and set `ShowOverlay = false`; the ring around the target
remains.

### Interaction modes

`BonboardingInteractionMode` decides how much of the page the user can still touch.

| Mode | Behaviour |
|---|---|
| `Blocked` (default) | A full-screen blocker swallows every pointer event outside the popover. The dialog is `aria-modal="true"`. |
| `TargetOnly` | Four blockers surround the spotlight, leaving the hole click-through. The user can operate the highlighted control and nothing else. |
| `Free` | No blockers at all. The whole page stays usable, and the popover floats above it. |

`TargetOnly` and `Free` render the dialog as `aria-modal="false"`, because the rest of the page is
genuinely still available to a screen reader user.

```csharp
new BonboardingStepDefinition
{
    Target = BonboardingStepTarget.Anchor("filters"),
    Title = "Try a filter",
    Interaction = BonboardingInteractionMode.TargetOnly,
}
```

### Let the user do it

An interactive step waits for a real interaction rather than a Next button:

```csharp
new BonboardingStepDefinition
{
    Target = BonboardingStepTarget.Anchor("new-project"),
    Title = "Now create your first project",
    Description = "Go ahead and press it.",
    AdvanceOn = BonboardingAdvanceTrigger.TargetClick,
    ShowNext = false,
}
```

Declaring `AdvanceOn` switches the step to `TargetOnly` automatically unless you set `Interaction`
yourself, because a step asking the user to click something has to let the click through.

The trigger can listen for any DOM event, on any element, and can filter delegated events:

```csharp
AdvanceOn = new BonboardingAdvanceTrigger
{
    EventName = "change",
    Selector = "#plan-picker",       // null listens on the step's own target
    MatchSelector = "[data-plan]",   // only when the event came from a matching element
    DelayMs = 250,                   // let the UI settle before advancing
}
```

The listener stays armed until the step changes, so if `CanAdvance` holds the user, their next attempt is
heard. Events while an advance is already on its way (a double click, every keystroke of an `input`
event) belong to that advance, and a click is never charged to the step after the one it was meant for.

#### Advancing on something the application does

Some tasks are not one DOM event: a project counts as created when the server says so, not when a button is
pressed. The code that knows raises a **signal**, and the step waiting for it moves on - with no reference to
the tour or the session:

```csharp
new BonboardingStepDefinition
{
    Target = BonboardingStepTarget.Anchor("new-project"),
    Title = "Create your first project",
    ShowNext = false,
    AdvanceOnSignal = "project-created",
}

// Wherever projects are created:
await _projects.CreateAsync(name);
await BonboardingService.SignalAsync("project-created");
```

Signals are compared ordinally, `CanAdvance` still decides, and `SignalAsync` returns whether a tour moved on.
They are not remembered: one raised while no step waits for it does nothing, so use `When` to skip a step
whose task the user had already done before reaching it.

### Scrolling

Off-screen targets are scrolled into view before the step is shown:

```csharp
new BonboardingStepDefinition
{
    Scroll = BonboardingScrollMode.Smooth,   // Smooth (default), Instant, Auto, None
    ScrollPadding = 24,           // margin kept around the target
}
```

Scrolling walks every scrollable ancestor, so nested panes, dialogs and virtualized lists all work.
A target that is already comfortably visible - in the viewport, and in every pane that scrolls it - is left
alone rather than being re-centred; one in the middle of the screen but scrolled out of sight inside its pane is
scrolled to like any other. A smooth
scroll is allowed to settle before the first measurement is taken so the popover does not appear
mid-flight.

### Closing behaviour

```csharp
new BonboardingStepDefinition
{
    CloseOnEscape = true,          // default
    CloseOnOverlayClick = false,   // default
}
```

Escape is handled at the document level. On a `Blocked` step, or whenever focus is in the popover,
it ends the tour wherever focus is. On a `TargetOnly` or `Free` step with focus out in the application,
it is left alone so the application's own menus and dialogs still close. Keep it on unless the tour
is genuinely mandatory; it is the escape hatch users reach for first.

### Very large and very small targets

Both are handled without special configuration:

- A target **larger than the viewport** is still measured as one rectangle. The popover is placed
  against whichever part is on screen and clamped inside the viewport padding.
- A target **smaller than the popover's corner radius** would leave the arrow pointing past its edge,
  so the arrow is clamped to stay within both the target and the popover's straight edge.
- When the popover ends up with no cross-axis overlap with the target at all, the arrow is hidden
  rather than pointing at nothing.
- A target **partly scrolled out of its pane** (a long table in a scrolling panel, a card in a carousel) is
  spotlit and pointed at only where its scrolling ancestors show it, not across the rest of the page. A target
  scrolled out of its pane entirely is pointed at on the edge of the pane it went past - where its visible part
  shrank to as it left - rather than with its own box drawn over whatever is next to the pane.

### The visible part of the screen

The popover is kept inside the part of the screen the user can actually see, not just the browser window:

- **The on-screen keyboard and pinch zoom.** Both shrink the visual viewport, and the popover is placed inside it,
  so an interactive step on a text field is not covered by the keyboard that field opened.
- **Notches and rounded corners.** When the page asks for the whole screen (`viewport-fit=cover`), the safe-area
  insets are kept clear as well. The host carries them as its padding, which moves nothing because everything
  inside it is fixed.
- **A host inside a transformed ancestor.** A `transform`, `filter`, `perspective` or `contain` on an ancestor makes
  it the containing block of every `position: fixed` element inside it, so fixed positions stop being viewport
  positions - the classic reason popovers drift once the page scrolls. Positions are measured relative to the
  host's own box, so the tour stays on its target either way. Placing `BonboardingRoot` outside such ancestors is
  still the better choice: it keeps the tour layer above them too.

## Beacons, checklists and tour lists

Not all guidance should take over the screen. These three components render on the page itself, use the same
engine, store and theme as the tours, and need nothing more than `BonboardingRoot` in the layout.

### Beacons

A beacon is a small pulsing dot on an element. Pressed, it opens a hint about that element, starts a tour, or
runs a handler of yours:

```razor
@* A hint: one non-modal step on the element, with no overlay and no focus trap. *@
<BonboardingBeacon Id="import-hint"
                   Anchor="import"
                   Title="Import is here"
                   Description="Bring projects in from a CSV file. It used to live under Settings." />

@* A tour. *@
<BonboardingBeacon Anchor="usage" Tour="UsageTour" Label="Take the usage tour" />

@* Anything else. *@
<BonboardingBeacon Selector="#plan-badge" Label="What's new" OnClick="OpenChangelog" />
```

- **Placement.** `Position` picks a corner, an edge or the centre of the element (`TopEnd` by default; Start and
  End are logical, so they mirror in right-to-left), and `OffsetX` / `OffsetY` nudge it. Place the component
  anywhere in the markup: it positions itself over its element.
- **It follows its element** as the page scrolls, resizes and re-renders, positioned in the browser with no call
  into .NET per frame. All beacons share one set of observers, so a page of them costs nothing while idle, on
  Blazor Server too. The element is found the way a step's target is - the first rendered match of the selector -
  and found again when a re-render replaces it.
- **It hides** while its element is missing, not rendered, or scrolled out of sight (in the viewport or in any
  scrolling ancestor), and while any other tour runs (`HideWhileTourRuns`, on by default). It sits one layer below
  the tour overlay.
- **It goes away for good** once read, when it has an `Id`: the hint is persisted like a one-step tour under that
  id, and once the user closes it the beacon does not come back (`ResetAsync(id)` brings it back). A beacon that
  starts a tour goes away once that tour is completed or dismissed, unless `KeepAfterClosed` is set. A beacon
  without an `Id` always shows.
- **Accessibility.** It is a real button, named by `Label`, then `Title`, then `BonboardingLabels.BeaconLabel`, with
  `aria-haspopup="dialog"` and `aria-expanded` while its hint or tour is open. The touch target is 24 pixels (WCAG
  2.2's minimum) around a smaller dot. The hint takes focus when it opens and gives it back to the beacon when it
  closes; a beacon that goes away for good moves focus to its element (or the first control inside it). The pulse
  stops under reduced motion, and forced colors draw the dot in `Highlight`.

The variables `--bit-bonboarding-beacon-size`, `--bit-bonboarding-beacon-dot` and `--bit-bonboarding-beacon-color`
restyle it.

### Checklists

A checklist shows a new user the handful of things worth doing, and ticks each off as it gets done:

```csharp
private static readonly BonboardingChecklistDefinition GettingStarted = new()
{
    Id = "getting-started",
    Title = "Get started",
    Items =
    {
        // Done once its tour is completed; pressing the item starts the tour.
        new() { Id = "tour", Title = "Take the project tour", Tour = ProjectTour },
        // Done, for good, when the application says so.
        new() { Id = "invite", Title = "Invite a teammate", CompleteOnSignal = "member-invited" },
        // Done while a condition holds.
        new() { Id = "avatar", Title = "Add a picture", IsComplete = ctx => ValueTask.FromResult(user.HasAvatar) },
        // Pressing it goes somewhere; listed only for administrators.
        new() { Id = "billing", Title = "Set up billing", Route = "settings/billing", When = ctx => ValueTask.FromResult(user.IsAdmin) },
    },
};
```

```razor
<BonboardingChecklist Checklist="GettingStarted" Dismissible="true" OnCompleted="Celebrate" />
```

An item is done when any of its sources says so: its tour (`Tour` or `TourId`) was completed, its `IsComplete`
condition holds, or the application ticked it off - with a signal while the checklist is on screen, or directly:

```csharp
await BonboardingService.SetChecklistItemAsync("getting-started", "invite");        // done
await BonboardingService.SetChecklistItemAsync("getting-started", "invite", false); // not done after all
```

Ticked items and a dismissal are stored in the `IBonboardingStore` next to the tours' records, under the key
`checklist:{id}` (so a custom store needs no changes), and in memory only when `Persist` is off. The checklist
refreshes itself when a tour ends and when its stored progress changes; call `RefreshAsync()` after your own state
changed something an `IsComplete` or `When` condition reads. It renders nothing until its progress has been read,
so it never shows every item unticked for a moment (nor while prerendering, where the store cannot be read).

It is a `<section>` labelled by its title (a `role="heading"` at `HeadingLevel`, 2 by default), with a
`progressbar` and a list whose items say "Done" or "Not done" to screen readers as well as showing it.
`ItemTemplate` replaces each item's markup and keeps the list; `HideWhenComplete` removes it once everything is
done. `GetCompletedChecklistItemsAsync`, `DismissChecklistAsync`, `IsChecklistDismissedAsync` and
`ResetChecklistAsync` are on `IBonboardingService`.

### Tour lists

A tour list is the entry point of a help menu or resource centre: the tours, where the user stands with each, and
a button to start, resume or replay it, so guidance can be found again on purpose rather than only offered once.

```razor
<BonboardingTourList Title="Guided tours" />                               @* every registered tour *@
<BonboardingTourList Tours="[WelcomeTour, BillingTour]" HideCompleted="true" />
```

Each tour shows as new, in progress or completed (`BonboardingTourProgress`), with **Start**, **Resume** or
**Replay**; a replay starts from the first step, anything else resumes. The button's accessible name includes the
tour's title, so a screen reader moving from button to button hears "Start Welcome tour" rather than "Start" five
times, and it is disabled while its tour runs (but not while the user is away from it). The list follows what it
is given, and the tours' progress as it changes, a reset included. `CanStart` still decides. Tours declared in markup are registered
only while their page is on screen, so register the ones a help menu should always offer at startup with
`IBonboardingService.Register`.

## Dynamic and async targets

Real applications render late. A tour that assumes every element exists the moment it starts will
break on the first slow query. Each step declares what should happen instead.

### Missing-target strategies

```csharp
new BonboardingStepDefinition
{
    Target = BonboardingStepTarget.Anchor("results-table"),
    MissingTarget = BonboardingMissingTargetBehavior.Wait,
    WaitTimeout = TimeSpan.FromSeconds(10),
    WaitTimeoutBehavior = BonboardingMissingTargetBehavior.Skip,
}
```

| Strategy | Behaviour |
|---|---|
| `Wait` (default) | Watch the DOM until the element appears, then show the step. On timeout, apply `WaitTimeoutBehavior`. |
| `Skip` | Move silently to the next step in the direction of travel. |
| `Center` | Show the step as a centred card with no spotlight. |
| `Fail` | End the tour with `BonboardingTourEndReason.Failed` and an `BonboardingException`. |

`WaitTimeoutBehavior` defaults to `Skip`. Setting it to `Wait` would spin forever, so it is treated
as `Skip`.

While waiting, the session's `Status` is `BonboardingTourStatus.Waiting`, `IsWaiting` is true and
`WaitReason` is `BonboardingWaitReason.Target`. The default UI shows a small card with a spinner and a close
button (replace it with `BonboardingRoot.WaitingTemplate`), and **the page stays usable**: nothing is blocked
and the keyboard is not trapped, whatever the step's `Interaction`. There is nothing on screen to protect yet,
and the element often appears because the user does something - opens the menu it lives in, expands a panel -
so a step can wait for exactly that. The step's own interaction mode and focus trap apply from the moment its
target is on screen.

A target that exists but is not rendered - `display: none`, inside a closed `<details>`,
`visibility: hidden`, or with no size - counts as missing, so the same strategy applies to it. When a
selector matches several elements, the first one that is rendered is used, which is what a responsive layout
with a desktop and a mobile copy of the same control needs. A selector the browser cannot parse is reported
as an error in the log (with the step and the selector) and handled straight away as a wait that has run out,
rather than waited on for the whole timeout.

#### Which to choose

- **`Wait`** for elements that are on their way: a table behind a query, a panel behind an animation.
- **`Skip`** for steps that are nice to have. This is the safest default for tours that run across
  different plans or permissions, though a `When` condition expresses that intent more clearly.
- **`Center`** when the step's text stands on its own and pointing at the element is a bonus.
- **`Fail`** only when a missing element means something is genuinely broken and you want to know.

### How waiting works

Waiting uses a `MutationObserver` on the document, coalesced onto animation frames. There is no
polling timer, so a step waiting for ten seconds costs nothing while the page is idle.

The observer watches for added and removed nodes anywhere in the tree, plus changes to the attributes
that affect whether a target matches or is rendered (`data-bonboarding-anchor`, `id`, `class`, `hidden`,
`style`, `open`). An element that becomes visible by animating in changes no attribute, so the end of every
CSS transition and animation triggers a check as well. When the element appears it is scrolled into view, the
observers are attached, and the step is shown.

A target that disappears while its step is on screen - removed, or hidden by a collapsed panel or a layout
switching to its mobile form - is handled the same way: if another rendered match exists the step moves to
it, otherwise the step's missing-target strategy applies. The one exception is the moment after the user
acted on the target of an `AdvanceOn` step: the action is often the very thing that hides it, and the step is
about to move on anyway.

### Targets that are replaced, not removed

Blazor frequently discards a DOM node and renders an identical one. If the library reported that as
"the target disappeared", tours would break constantly on ordinary re-renders.

Instead, when the observed element leaves the DOM the browser layer re-queries the step's target
first. If a new element matches, it rebinds to it silently and the step never notices. Only when
nothing matches is the target reported as lost, and the step's `MissingTarget` strategy then applies
exactly as it did on activation.

This is why anchors and selectors are preferred over captured `ElementReference`s: a reference names
one specific node and cannot be re-resolved.

### Targets chosen at run time

`BonboardingStepTarget.Dynamic` defers the decision until the step activates:

```csharp
new BonboardingStepDefinition
{
    Target = BonboardingStepTarget.Dynamic(ctx =>
        BonboardingStepTarget.Anchor($"row-{ctx.State["selectedRow"]}")),
    Title = "The row you selected",
}
```

The resolver receives the full `BonboardingStepContext`, so it can read the session state, the tour, and
anything in DI. It can be async:

```csharp
Target = BonboardingStepTarget.Dynamic(async ctx =>
{
    var id = await ctx.Services.GetRequiredService<IProjects>().GetFirstIdAsync(ctx.CancellationToken);
    return id is null ? BonboardingStepTarget.None : BonboardingStepTarget.Css($"[data-project='{id}']");
})
```

Returning `null` or `BonboardingStepTarget.None` makes the step a centred card. A resolver that returns another
dynamic target is resolved again, up to a small depth limit, so an accidental cycle degrades to
"no target" rather than hanging.

### Keeping up with a moving target

Once a step is showing, the browser layer keeps the popover glued to its target using:

- a `ResizeObserver` on the target, the popover and the document element;
- an `IntersectionObserver` for entering and leaving a scroll container;
- capture-phase `scroll` listeners, which catch every scrollable ancestor without having to find
  them;
- `resize` on both the window and the visual viewport, which is what mobile zoom and the on-screen
  keyboard change;
- a `MutationObserver` on the document (child lists, plus the `class`, `style`, `hidden` and `open`
  attributes), and the end of CSS transitions and animations. These catch a target that moves or disappears
  with nothing resized: a sibling growing inside a fixed-height pane, or a class that sets
  `visibility: hidden`. Changes inside the tour's own host are ignored.

All of them feed one animation-frame callback that takes a single measurement, compares it with the
last one, and only calls into .NET when something actually moved by more than half a pixel. An idle
page costs nothing; a momentum scroll costs one interop call per frame at most.

### Virtualized lists

A row inside `Virtualize` may be recycled or removed as the user scrolls. Two things make this work:

- the target is re-queried rather than remembered, so recycling rebinds cleanly;
- `BonboardingMissingTargetBehavior.Wait` combined with scrolling means a step pointing at a row that has
  scrolled out of the rendered window waits for it to come back rather than failing.

If the row may genuinely never return, prefer `Skip`.

### Refreshing manually

The observers cover layout changes the browser can see. If your own code changes geometry in a way
that produces no resize, scroll or mutation - animating a CSS transform, for instance - ask for a
fresh measurement:

```csharp
await session.RefreshAsync();
```

## Conditional and branching flows

### Conditional steps

`When` gates a step. A step whose condition is false is skipped in whichever direction the user is
travelling, and is left out of the progress count.

```csharp
new BonboardingStepDefinition
{
    Id = "billing",
    Target = BonboardingStepTarget.Anchor("billing"),
    Title = "Billing",
    When = ctx => ValueTask.FromResult(user.IsInRole("Administrator")),
}
```

Conditions are re-evaluated on **every transition**, not once at the start. That means a step can be
unlocked by something an earlier step did:

```csharp
new BonboardingStepDefinition
{
    Id = "connect",
    Title = "Connect a repository",
    OnBeforeLeave = ctx => { ctx.State["connected"] = true; return ValueTask.CompletedTask; },
},
new BonboardingStepDefinition
{
    Id = "deploy",
    Title = "Deploy it",
    When = ctx => ValueTask.FromResult(ctx.State.ContainsKey("connected")),
}
```

Because they are asked repeatedly, **conditions must be cheap and free of side effects**. Read state,
do not change it. If a condition needs data from the server, load it in `BonboardingTourDefinition.OnStarted` or
a step's `OnBeforeShow` and stash it in `Session.State`.

#### Should this be a condition or a missing-target strategy?

Both can hide a step. Prefer `When` when the reason is about the *user or the data* ("admins only",
"only on the pro plan"), and `MissingTarget = Skip` when the reason is about *the page* ("this
control is not rendered here"). Conditions also keep the progress indicator honest, which
missing-target skipping cannot do until the step is reached.

### Branching

#### With buttons

The simplest fork is a step with actions that jump:

```csharp
new BonboardingStepDefinition
{
    Id = "ask",
    Title = "What brings you here?",
    ShowNext = false,
    Actions =
    [
        new BonboardingStepAction
        {
            Label = "I design",
            Style = BonboardingStepActionStyle.Primary,
            Effect = BonboardingStepActionEffect.GoTo,
            TargetStepId = "design-path",
        },
        new BonboardingStepAction
        {
            Label = "I build",
            Style = BonboardingStepActionStyle.Primary,
            Effect = BonboardingStepActionEffect.GoTo,
            TargetStepId = "build-path",
        },
    ],
}
```

#### With a resolver

`ResolveNext` decides in code where the forward button goes:

```csharp
new BonboardingStepDefinition
{
    Id = "plan",
    ResolveNext = ctx => ValueTask.FromResult<string?>(
        ctx.State["plan"] as string == "free" ? "upgrade" : "advanced-features"),
}
```

Three return values matter:

| Return | Meaning |
|---|---|
| a step id | Jump there. An unknown id fails the tour. |
| `null` | Fall through to the next step in order. |
| `BonboardingTourDefinition.EndStepId` | Finish the tour now. |

A step with a `ResolveNext` is never reported as the last step, because that cannot be known without
running your code. The forward button keeps saying "Next", and the hook decides when it is pressed.

To end a branch without falling into the next one, point it at the step where the paths rejoin:

```csharp
new BonboardingStepDefinition
{
    Id = "design-path",
    ResolveNext = _ => ValueTask.FromResult<string?>("shared-ending"),
}
```

### Going back through a branch

Back follows the **visit history**, not the declaration order. If a user went from `ask` straight to
`build-path`, pressing Back returns them to `ask` - not to the `design-path` step that happens to sit
above `build-path` in the array.

`session.History` exposes the ids visited so far, oldest first.

`ResolvePrevious` overrides this when you need something else:

```csharp
new BonboardingStepDefinition
{
    ResolvePrevious = _ => ValueTask.FromResult<string?>("ask"),
}
```

A step with a `ResolvePrevious` always shows its Back button, since the hook can go somewhere even
from the first step.

### Validation

`CanAdvance` blocks the forward button while it returns false:

```csharp
new BonboardingStepDefinition
{
    Target = BonboardingStepTarget.Anchor("project-name"),
    Title = "Name your project",
    Interaction = BonboardingInteractionMode.TargetOnly,
    CanAdvance = ctx => ValueTask.FromResult(!string.IsNullOrWhiteSpace(_projectName)),
}
```

To tell the user *why* they are held on the step, give a message - on the step, or from the guard itself,
which wins:

```csharp
new BonboardingStepDefinition
{
    Title = "Name your project",
    ValidationMessage = "Give the project a name first.",
    CanAdvance = ctx =>
    {
        if (_projectName?.Length > 40) ctx.ValidationMessage = "Keep the name under 40 characters.";
        return ValueTask.FromResult(!string.IsNullOrWhiteSpace(_projectName) && _projectName.Length <= 40);
    },
}
```

The default popover shows it under the body with `role="alert"`, so a screen reader reads it out the moment
the user is told no; it stays until the step changes (`IBonboardingTourSession.ValidationMessage`,
`BonboardingStepRenderContext.ValidationMessage` for a custom template). Every held advance also publishes
`BonboardingEventKind.AdvanceBlocked`, which, counted per step, points at instructions people find hard to
follow. Without a message, a held advance is silent, as it always was.

The guard decides for every way forward: the Next button, the arrow keys, an `AdvanceOn` trigger (which stays
armed, so the user's next attempt is heard) and an application signal.

For validation that should stop the transition and surface an error, throw from `OnBeforeLeave`
instead; that fails the tour with your exception attached.

### Whether the tour should run at all

`CanStart` is checked by `StartAsync` and `StartOnceAsync`:

```csharp
new BonboardingTourDefinition
{
    Id = "admin-tour",
    CanStart = ctx => ValueTask.FromResult(user.IsInRole("Administrator")),
}
```

`StartOnceAsync` returns `null` when it declines. `StartAsync` throws an `BonboardingException`,
because you asked for it explicitly. `BonboardingStartOptions.Force` bypasses the check.

### A worked example

```csharp
var tour = new BonboardingTourDefinition
{
    Id = "setup",
    Steps =
    {
        new BonboardingStepDefinition
        {
            Id = "role",
            Title = "How will you use this?",
            ShowNext = false,
            Actions =
            [
                new BonboardingStepAction { Label = "Solo",  Effect = BonboardingStepActionEffect.GoTo, TargetStepId = "solo" },
                new BonboardingStepAction { Label = "Team",  Effect = BonboardingStepActionEffect.GoTo, TargetStepId = "team" },
            ],
        },

        new BonboardingStepDefinition
        {
            Id = "solo",
            Target = BonboardingStepTarget.Anchor("new-project"),
            Title = "Start a project",
            ResolveNext = _ => ValueTask.FromResult<string?>("finish"),
        },

        new BonboardingStepDefinition
        {
            Id = "team",
            Target = BonboardingStepTarget.Anchor("invite"),
            Title = "Invite your colleagues",
        },

        new BonboardingStepDefinition
        {
            Id = "team-roles",
            Target = BonboardingStepTarget.Anchor("roles"),
            Title = "Set their permissions",
            // Only relevant on a plan that has roles at all.
            When = ctx => ValueTask.FromResult(ctx.State["plan"] as string != "free"),
        },

        new BonboardingStepDefinition
        {
            Id = "finish",
            Title = "You are set up",
            DoneLabel = "Finish",
        },
    },
};
```

## Multi-page tours

A step can declare the route it belongs to. The engine navigates there, waits for the page to render,
and only then looks for the target.

```csharp
new BonboardingStepDefinition
{
    Id = "api-keys",
    Route = "workspace/settings",
    Target = BonboardingStepTarget.Anchor("api-keys"),
    Title = "Your API keys",
}
```

Routes are matched against the base-relative path. Leading and trailing slashes, query strings and
fragments are ignored, and matching is case-insensitive, so `"/settings/"`, `"settings"` and
`"settings?tab=keys"` are all the same route.

### Wildcards

A trailing `*` turns the route into a **constraint rather than a destination**:

```csharp
new BonboardingStepDefinition
{
    Route = "workspace/*",
    Title = "Somewhere under the workspace",
}
```

The step waits until the user is on a matching page, but the engine does not navigate, because there
is no single URL to navigate to. Use this for steps that apply across a section.

### Navigating away mid-tour

If the user leaves the page a step belongs to, the tour **holds its place**. The host renders nothing -
no overlay, no blocker, no card - the observers are torn down, and the session moves to
`BonboardingTourStatus.Waiting` with `WaitReason` set to `BonboardingWaitReason.Route`. When the user returns
to a matching route, the step is re-activated exactly where it was, and its popover takes focus again.

The same holds for every wait on a route: while the engine navigates to a step's page, and while a step
waits for the user to get to a wildcard route or to a route the engine was told not to navigate to. The page
stays fully usable, because using it is how the user gets there.

The library deliberately does not drag the user back. Fighting someone for control of the address bar
is never the right call, and a user who navigated away usually meant it.

While the user is away from a step's page - they left it, or it is a route the engine does not navigate to - the
tour is not one they are taking. An automatic start does not queue behind it but replaces it (cancelled, it keeps
its place, so a tour list's **Resume** or its own `AutoStart` picks it up again), beacons stop stepping aside for
it, and a tour list offers to resume it. A step the engine is navigating to is on its way to the screen, and still
counts as running.

To turn off automatic navigation entirely - so routes are only ever constraints - set:

```csharp
builder.Services.AddBitBonboardingServices(options => options.AutoNavigateToStepRoute = false);
```

### Surviving a full page load

Two things make this work:

- **Progress is written after every step**, so the store always knows where the user was.
- **`StartAsync` resumes by default.** Given the same tour id, it reads the record and starts at the
  saved step.

So the pattern for a tour that must survive a reload is simply to define it in one place and start it
from every page it touches:

```csharp
public static class WorkspaceTour
{
    public const string Id = "workspace";

    public static BonboardingTourDefinition Create() => new()
    {
        Id = Id,
        Version = 1,
        Steps = { /* ... */ },
    };
}
```

```razor
@* Both pages: *@
<button @onclick="() => BonboardingService.StartAsync(WorkspaceTour.Create())">Take the tour</button>
```

Or auto-start it once per user from your layout:

```razor
@code {
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender) await BonboardingService.StartOnceAsync(WorkspaceTour.Create());
    }
}
```

Because ids are what get persisted, **give every step in a multi-page tour an explicit `Id`**.
Auto-generated ids change on every construction and cannot be resumed into.

### Enhanced navigation and static rendering

Blazor's enhanced navigation replaces the page content without a full load. The library listens to
`NavigationManager.LocationChanged`, so both enhanced and full navigations are handled the same way.

On a same-page navigation, where the route requirement still matches, the engine takes a fresh
measurement instead - a query-string change usually means the content underneath the target has been
replaced.

Tours run in interactive components. A step whose target lives on a statically rendered page still
works, because targets are located by querying the DOM rather than by holding a component reference,
as long as the `BonboardingRoot` host itself is interactive.

### Where to put the host

One `BonboardingRoot` in the layout is enough, and it is the right place for a multi-page tour: a host
placed on a page would be disposed by the navigation the tour just performed.

### Choosing a route for the first step

If the first step declares a route and the user starts the tour elsewhere, the engine navigates there
before showing anything. That is usually what you want for a "start the product tour" button in a
help menu, which can then be pressed from anywhere.

If instead the tour should only be offered on the right page, leave the first step's route unset and
start it from that page.

### Waiting indefinitely

A session waiting on a route holds no observers, no listeners and no timers, so waiting forever costs
nothing. There is deliberately no timeout: the user may take a coffee break in the middle of a tour,
and quietly abandoning their progress would be worse than waiting.

If you want a bound, end the session yourself:

```csharp
await Task.Delay(TimeSpan.FromMinutes(5));
if (session.Status == BonboardingTourStatus.Waiting) await session.CancelAsync();
```

`CancelAsync` keeps the saved progress, so the tour resumes next time rather than being recorded as
refused.

## Theming

Every value in the default look is a CSS custom property under `.bit-bonboarding-root` (the tour layer) and
`.bit-bonboarding-themed` (the beacons, checklists and tour lists, which render on the page itself). Restyling
the library is a matter of overriding variables in your own stylesheet - no selector fights, no `!important`,
and no CSS framework to install.

```css
/* Your stylesheet, loaded after the library's. */
.bit-bonboarding-root,
.bit-bonboarding-themed {
    --bit-bonboarding-accent: #6d28d9;
    --bit-bonboarding-radius: 6px;
    --bit-bonboarding-font: "Inter", system-ui, sans-serif;
}
```

An override on `.bit-bonboarding-root` alone still restyles every tour, as it always did; add
`.bit-bonboarding-themed` to reach the on-page components too.

### The variables

#### Colour

| Variable | Default (light) | Used for |
|---|---|---|
| `--bit-bonboarding-surface` | `#ffffff` | Popover background, arrow |
| `--bit-bonboarding-surface-subtle` | `#f6f7f9` | Hover states, progress track |
| `--bit-bonboarding-text` | `#14161a` | Titles, primary text |
| `--bit-bonboarding-text-muted` | `#5c6270` | Descriptions, progress text |
| `--bit-bonboarding-border` | `#e3e6ea` | Popover and arrow borders |
| `--bit-bonboarding-border-strong` | `#cfd4dc` | Secondary button borders, inactive dots |
| `--bit-bonboarding-accent` | `#2563eb` | Primary button, current dot, focus ring |
| `--bit-bonboarding-accent-hover` / `--bit-bonboarding-accent-active` | | Primary button states |
| `--bit-bonboarding-accent-text` | `#ffffff` | Text on the primary button |
| `--bit-bonboarding-accent-soft` | 12% accent | Completed dots |
| `--bit-bonboarding-danger` / `--bit-bonboarding-danger-hover` | `#dc2626` | Danger-styled actions |
| `--bit-bonboarding-overlay` | `0, 0, 0` | Overlay colour, as `R, G, B` |
| `--bit-bonboarding-overlay-opacity` | `0.55` | Set from `OverlayOpacity`; overriding in CSS is not useful |
| `--bit-bonboarding-spotlight-ring` | 90% white | Inner hairline around the spotlight |
| `--bit-bonboarding-spotlight-glow` | 35% accent | Outer glow around the spotlight |

`--bit-bonboarding-overlay` is an unwrapped RGB triple so the opacity can be varied independently. To dim with a
brand colour rather than black:

```css
.bit-bonboarding-root { --bit-bonboarding-overlay: 15, 23, 42; }
```

#### Shape and depth

| Variable | Default |
|---|---|
| `--bit-bonboarding-radius` | `14px` |
| `--bit-bonboarding-radius-sm` | `8px` (buttons, close) |
| `--bit-bonboarding-radius-pill` | `999px` (dots, progress bar) |
| `--bit-bonboarding-shadow` | two-layer elevation shadow |
| `--bit-bonboarding-focus-ring` | `0 0 0 2px surface, 0 0 0 4px accent` |

#### Typography

| Variable | Default |
|---|---|
| `--bit-bonboarding-font` | system UI stack |
| `--bit-bonboarding-font-size` | `0.9375rem` |
| `--bit-bonboarding-font-size-sm` | `0.8125rem` (buttons, progress) |
| `--bit-bonboarding-title-size` | `1.0625rem` |
| `--bit-bonboarding-title-weight` | `620` |
| `--bit-bonboarding-line-height` | `1.55` |

#### Metrics and motion

| Variable | Default | Notes |
|---|---|---|
| `--bit-bonboarding-popover-width` | `22rem` | |
| `--bit-bonboarding-popover-max-height` | `min(70vh, 34rem)` | The body scrolls beyond this |
| `--bit-bonboarding-space` | `1rem` | Padding rhythm |
| `--bit-bonboarding-arrow` | `10px` | Set from `ArrowSize` |
| `--bit-bonboarding-duration` | `220ms` | Set from `AnimationDuration`; zero under reduced motion |
| `--bit-bonboarding-ease` | `cubic-bezier(0.22, 0.9, 0.3, 1)` | |
| `--bit-bonboarding-z` | `9000` | Set from `ZIndex` |

Variables written by the engine - `--bit-bonboarding-z`, `--bit-bonboarding-duration`, `--bit-bonboarding-overlay-opacity`, `--bit-bonboarding-arrow` -
are set inline on the root element, so a stylesheet rule cannot override them. Change the
corresponding option instead.

### Dark mode

The dark palette is applied by a `data-bonboarding-theme="dark"` attribute on the root element, which the
engine sets from the resolved `Theme`:

```csharp
new BonboardingTourDefinition { Theme = BonboardingTheme.Dark }
builder.Services.AddBitBonboardingServices(o => o.Theme = BonboardingTheme.Dark);
```

`BonboardingTheme.System` (the default) leaves the attribute as `system`, and the stylesheet applies the
dark palette to it while the operating system asks for a dark scheme (`prefers-color-scheme: dark`) and the
light one otherwise. It follows a change of the system setting immediately, with no round trip to .NET. If
your application has its own theme switch, bind it to the host:

```razor
<BonboardingRoot Theme="@(_dark ? BonboardingTheme.Dark : BonboardingTheme.Light)" />
```

The host parameter overrides both the tour and the global setting, which is what you want for a
switch: it keeps tours consistent with the rest of the page.

To adjust the dark palette, override both rules that apply it - the explicit dark theme, and the
system theme on a dark system:

```css
.bit-bonboarding-root[data-bonboarding-theme="dark"] {
    --bit-bonboarding-surface: #0b0e14;
    --bit-bonboarding-accent: #8b5cf6;
}

@media (prefers-color-scheme: dark) {
    .bit-bonboarding-root[data-bonboarding-theme="system"] {
        --bit-bonboarding-surface: #0b0e14;
        --bit-bonboarding-accent: #8b5cf6;
    }
}
```

The system rule is written with the specificity of a plain `.bit-bonboarding-root`, so an application
that restyles the palette on `.bit-bonboarding-root` (with its stylesheet loaded after the library's) keeps
its own colours on a dark system. An application that follows its own theme switch rather than the operating
system's maps `system` from there, as this repository's demo does:

```css
:root[data-app-theme="dark"] .bit-bonboarding-root[data-bonboarding-theme="system"] {
    --bit-bonboarding-surface: #15191e;
}
```

An explicit `Light` or `Dark` theme also sets `color-scheme` on the root, so native controls a custom step
body renders (inputs, selects, scrollbars) match the card. `System` inherits the page's `color-scheme`, which
an application with dark mode normally sets on `:root`.

### Scoping a theme

#### To one tour

`PopoverClass` is added to the popover element, and accumulates from global, tour and step levels:

```csharp
new BonboardingTourDefinition { Id = "checkout", PopoverClass = "checkout-tour" }
```

```css
.checkout-tour { --bit-bonboarding-accent: #16a34a; }
```

Because custom properties inherit, this restyles everything inside the popover. To also restyle the
overlay and spotlight, put the class on the host instead, where it lands on `.bit-bonboarding-root`:

```razor
<BonboardingRoot Class="checkout-theme" />
```

#### To one step

```csharp
new BonboardingStepDefinition { PopoverClass = "danger-step" }
```

### Layout tweaks

A wider popover for a step with a form:

```csharp
new BonboardingStepDefinition { PopoverClass = "wide-step" }
```

```css
.wide-step { --bit-bonboarding-popover-width: 32rem; }
```

The popover is `max-width: calc(100vw - 2rem)` and its body scrolls once the content exceeds
`--bit-bonboarding-popover-max-height`, so a wide or long step degrades gracefully on a phone rather than
overflowing.

### Mobile

Below `34rem` the library narrows the popover to the viewport, reduces padding and tightens the
footer. Under `pointer: coarse` it enlarges buttons for touch. Both are plain media queries in the
stylesheet, so you can extend or replace them:

```css
@media (max-width: 30rem) {
    .bit-bonboarding-root { --bit-bonboarding-popover-width: calc(100vw - 1rem); }
}
```

### Replacing the stylesheet entirely

Nothing in the engine depends on the stylesheet's rules; it only depends on the elements existing.
If you would rather write your own, skip the `<link>` and style these hooks:

| Element | Class | Data attributes |
|---|---|---|
| Root | `.bit-bonboarding-root` | `data-bonboarding-theme`, `data-bonboarding-status`, `data-bonboarding-reduced-motion`, `dir` |
| Spotlight | `.bit-bonboarding-spotlight` | positioned inline |
| Flat overlay | `.bit-bonboarding-overlay` | shown when there is no spotlight |
| Pointer blockers | `.bit-bonboarding-blocker` | one or four, depending on `Interaction` |
| Popover | `.bit-bonboarding-popover` | `data-bonboarding-placed`, `data-bonboarding-side`, `data-bonboarding-untitled` |
| Arrow | `.bit-bonboarding-arrow` | positioned inline |
| Buttons | `.bit-bonboarding-button`, `.bit-bonboarding-button-primary` … | `data-bonboarding-action` |
| Live region | `.bit-bonboarding-live` | |

The popover must remain `position: fixed` with its position applied through the inline `transform`
the engine sets; everything else is yours. `Unstyled="true"` on the host drops the built-in popover
classes altogether - see [headless rendering](#headless-and-custom-rendering).

## Headless and custom rendering

There are three levels of customisation. Reach for the smallest one that does the job.

| Level | How | Use when |
|---|---|---|
| Restyle | [CSS variables](#theming) | The layout is right, the colours are not |
| Recompose | `Header`, `Body`, `Footer` on a step | One step needs a form, an image, or different buttons |
| Replace | `BonboardingRoot.ChildContent`, or a tour or step `Template` | The card should look nothing like the default |

### What stays yours, and what stays the library's

Even at the "replace" level, the library keeps the popover **element**: a `position: fixed` div that
it measures, positions, gives dialog semantics, focuses and traps focus within. You supply what goes
inside it.

That split is deliberate. Collision-aware placement, the resize and scroll observers, focus
restoration and the live region are the parts that are genuinely hard to get right, and
reimplementing them in every application is not a reasonable ask. Everything above them is markup,
which is exactly what Razor is for.

### Replacing the popover contents

```razor
<BonboardingRoot Unstyled="true" PopoverClass="my-card">
    <ChildContent>
        <h2>@context.Title</h2>
        <p>@context.Description</p>

        <footer>
            <button @onclick="context.CloseAsync">Close</button>
            @if (!context.IsFirst)
            {
                <button @onclick="context.PreviousAsync">@context.PreviousLabel</button>
            }
            <button data-bonboarding-autofocus @onclick="context.NextAsync">@context.NextLabel</button>
        </footer>
    </ChildContent>
</BonboardingRoot>
```

`Unstyled="true"` drops the built-in `.bit-bonboarding-popover` class so your own CSS starts from nothing. Leave
it off to keep the default card as a base and add to it.

`data-bonboarding-autofocus` marks the control that should receive focus when a step is shown.

### The render context

Every template receives a `BonboardingStepRenderContext`:

```csharp
context.Session        // the running IBonboardingTourSession
context.Step           // the BonboardingStepDefinition
context.Options        // fully resolved BonboardingEffectiveStepOptions for this step
context.Labels         // the label set in force

context.Title          // localized
context.Description    // localized
context.Index          // index in BonboardingTourDefinition.Steps
context.Position       // 1-based, counting only steps that will be shown
context.Count
context.IsFirst / IsLast / IsWaiting
context.TargetRect     // last measured target rectangle
context.Placement      // side, alignment, arrow offset

context.NextLabel      // already switched to the done label on the last step
context.PreviousLabel
context.SkipLabel
context.ProgressText   // "2 of 5", formatted from the labels

await context.NextAsync();
await context.PreviousAsync();
await context.SkipAsync();
await context.CloseAsync();
await context.CompleteAsync();
await context.GoToAsync("billing");
await context.InvokeAsync(action);
```

### Per-step and per-tour templates

Templates are resolved most-specific first: step `Template`, then tour `Template`, then the host's
`ChildContent`, then the default chrome.

```csharp
new BonboardingStepDefinition
{
    Template = context => builder => { /* this step only */ },
}
```

In markup, a step's `ChildContent` is its body, leaving the default header and footer in place - the
common case:

```razor
<BonboardingStep Anchor="plan" Title="Choose a plan">
    <PlanPicker Selected="_plan" SelectedChanged="OnPlanChanged" />
</BonboardingStep>
```

```csharp
private async Task OnPlanChanged(string plan)
{
    _plan = plan;
    await _tour!.Session!.NextAsync();
}
```

`Header` and `Footer` replace those regions individually:

```razor
<BonboardingStep Anchor="save" Title="Save your work">
    <Footer>
        <div class="my-footer">
            <span>@context.ProgressText</span>
            <button @onclick="context.NextAsync">@context.NextLabel</button>
        </div>
    </Footer>
</BonboardingStep>
```

Note that replacing the footer removes the close button along with everything else, so make sure
there is still a way out - or leave `CloseOnEscape` on.

### Replacing the overlay

```razor
<BonboardingRoot>
    <OverlayTemplate>
        @if (context.TargetRect is { } rect)
        {
            <svg class="my-overlay" width="100%" height="100%">
                <defs>
                    <mask id="hole">
                        <rect width="100%" height="100%" fill="white" />
                        <rect x="@rect.X" y="@rect.Y" width="@rect.Width" height="@rect.Height"
                              rx="12" fill="black" />
                    </mask>
                </defs>
                <rect width="100%" height="100%" fill="rgba(0,0,0,0.6)" mask="url(#hole)" />
            </svg>
        }
    </OverlayTemplate>
</BonboardingRoot>
```

`OverlayTemplate` replaces the visual layer only. The pointer blockers that implement `Interaction`
are rendered separately and keep working, so a custom overlay does not have to reimplement
click-through.

### Replacing the waiting state

```razor
<BonboardingRoot>
    <WaitingTemplate>
        <MySkeleton Text="Loading the next part of the page" />
    </WaitingTemplate>
</BonboardingRoot>
```

### Several hosts

A host given an explicit `Session` **claims** it, and the application's main host stops rendering
that session. This is what lets one page take over the presentation of one tour while the layout
keeps handling everything else:

```razor
@* Layout: handles every tour *@
<BonboardingRoot />
```

```razor
@* One page: renders its own tour with a custom card *@
@if (_session is not null)
{
    <BonboardingRoot Session="_session" Unstyled="true">
        <ChildContent> ... </ChildContent>
    </BonboardingRoot>
}

@code {
    private IBonboardingTourSession? _session;

    private async Task StartAsync()
    {
        _session = await BonboardingService.StartAsync(tour);
        StateHasChanged();
    }
}
```

The claim is released when the host is disposed, so navigating away hands the session back to the
layout rather than leaving it invisible. Render such a host only once it has its session, as above: a host
whose `Session` is `null` is an ambient host like the layout's, and would draw the active tour a second time.

### Building the UI from scratch

If you want no rendering from the library at all, do not place an `BonboardingRoot`. Drive
`IBonboardingService` and `IBonboardingTourSession` directly and render whatever you like from
`session.CurrentStep`, `session.Placement` and `session.TargetRect`, subscribing to `session.Changed`
to re-render.

The trade-off is real, and worth stating plainly: without the host, nothing calls
`SetPopoverElementAsync`, so the engine never learns your popover's size and `session.Placement`
stays `null`. You would be positioning it yourself. `BonboardingPlacementEngine.Place` is public and pure, so
you can still use the same algorithm:

```csharp
var result = BonboardingPlacementEngine.Place(new BonboardingPlacementRequest
{
    Target = session.TargetRect!.Value.Inflate(8),
    Popover = new BonboardingSize(320, 180),
    Viewport = new BonboardingRect(0, 0, viewportWidth, viewportHeight),
    Preferred = BonboardingPlacement.Bottom,
    Offset = 12,
    ViewportPadding = 16,
    ArrowSize = 10,
    CornerRadius = 12,
    HasTarget = true,
});
```

For almost every application, `Unstyled="true"` with your own `ChildContent` is the better trade: the
same visual freedom, none of the plumbing.

## Localization and RTL

### Labels

Every string the default UI renders comes from an `BonboardingLabels` instance.

```csharp
public sealed class BonboardingLabels
{
    public string Next { get; set; } = "Next";
    public string Previous { get; set; } = "Back";
    public string Skip { get; set; } = "Skip tour";
    public string Done { get; set; } = "Done";
    public string Close { get; set; } = "Close";

    public string DialogLabel { get; set; } = "Product tour";
    public string ProgressFormat { get; set; } = "{0} of {1}";
    public string StepAnnouncementFormat { get; set; } = "Step {0} of {1}. {2}";
    public string WaitingAnnouncement { get; set; } = "Waiting for the next part of the page.";
    public string ProgressLabel { get; set; } = "Tour progress";

    // Beacons, checklists and tour lists
    public string BeaconLabel { get; set; } = "Show tip";
    public string HintDone { get; set; } = "Got it";
    public string ChecklistProgressFormat { get; set; } = "{0} of {1} done";
    public string ChecklistDismiss { get; set; } = "Dismiss checklist";
    public string ChecklistItemDone { get; set; } = "Done";
    public string ChecklistItemTodo { get; set; } = "Not done";
    public string TourNew { get; set; } = "New";
    public string TourInProgress { get; set; } = "In progress";
    public string TourCompleted { get; set; } = "Completed";
    public string TourStart { get; set; } = "Start";
    public string TourResume { get; set; } = "Resume";
    public string TourReplay { get; set; } = "Replay";
}
```

`DialogLabel`, `StepAnnouncementFormat`, `WaitingAnnouncement`, `ProgressLabel`, `BeaconLabel`,
`ChecklistDismiss`, `ChecklistItemDone` and `ChecklistItemTodo` are read by assistive technology rather than
shown on screen. Translate them: they are the only thing a screen reader user hears. The on-page components
read the global labels (`IBonboardingLocalizer.GetLabels(null)`), since they belong to no one tour.

#### Globally

```csharp
builder.Services.AddBitBonboardingServices(options =>
{
    options.Labels.Next = "Weiter";
    options.Labels.Previous = "Zurück";
    options.Labels.Skip = "Überspringen";
    options.Labels.Done = "Fertig";
});
```

#### Per tour

```csharp
new BonboardingTourDefinition
{
    Id = "welcome",
    Labels = new BonboardingLabels { Next = "Weiter", Done = "Fertig" },
}
```

A tour's labels replace the global set wholesale rather than merging. `Clone()` gives you a copy of
an existing set to adjust, so changing one string does not mean restating the rest:

```csharp
var labels = BonboardingLabels.Default.Clone();
labels.Next = "Weiter";

var tour = new BonboardingTourDefinition { Id = "welcome", Labels = labels };
```

#### Per step

```csharp
new BonboardingStepDefinition
{
    NextLabel = "Show me",
    PreviousLabel = "Go back",
    SkipLabel = "Not now",
    DoneLabel = "Finish setup",
}
```

### Using `IStringLocalizer`

Register your own `IBonboardingLocalizer` to source labels from resource files or a translation
service:

```csharp
public sealed class ResourceOnboardingLocalizer(IStringLocalizer<Tours> localizer) : IBonboardingLocalizer
{
    public BonboardingLabels GetLabels(BonboardingTourDefinition? tour) => new()
    {
        Next = localizer["Tour_Next"],
        Previous = localizer["Tour_Back"],
        Skip = localizer["Tour_Skip"],
        Done = localizer["Tour_Done"],
        Close = localizer["Tour_Close"],
        DialogLabel = localizer["Tour_DialogLabel"],
        ProgressFormat = localizer["Tour_Progress"],
        StepAnnouncementFormat = localizer["Tour_Announcement"],
        WaitingAnnouncement = localizer["Tour_Waiting"],
        ProgressLabel = localizer["Tour_ProgressLabel"],
        BeaconLabel = localizer["Beacon_Label"],
        HintDone = localizer["Hint_Done"],
        ChecklistProgressFormat = localizer["Checklist_Progress"],
        ChecklistDismiss = localizer["Checklist_Dismiss"],
        ChecklistItemDone = localizer["Checklist_ItemDone"],
        ChecklistItemTodo = localizer["Checklist_ItemTodo"],
        TourNew = localizer["TourList_New"],
        TourInProgress = localizer["TourList_InProgress"],
        TourCompleted = localizer["TourList_Completed"],
        TourStart = localizer["TourList_Start"],
        TourResume = localizer["TourList_Resume"],
        TourReplay = localizer["TourList_Replay"],
    };

    // Lets step titles and descriptions be resource keys rather than literals.
    public string? Localize(string? text) => text is null ? null : localizer[text];
}
```

```csharp
builder.Services.AddBitBonboardingServices();
builder.Services.AddBitBonboardingLocalizer<ResourceOnboardingLocalizer>();
```

`Localize` is applied to step titles and descriptions before they are rendered. The default
implementation returns the value unchanged, so tours written with literal text keep working.

Labels are resolved when a step is activated, so a tour that is running when the culture changes
picks up the new strings on its next step. To re-render the current step immediately, call
`session.RefreshAsync()`.

### Right-to-left

```csharp
new BonboardingTourDefinition { Direction = BonboardingDirection.RightToLeft }
```

`Direction` is one of:

| | |
|---|---|
| `Auto` (default) | Follows the document's computed direction, sampled from the browser. |
| `LeftToRight` | Forced. |
| `RightToLeft` | Forced. |

`Auto` is usually right: set `dir="rtl"` on your `<html>` element as you already would, and tours
follow.

Direction changes more than text alignment:

- **`Start` and `End` alignments mirror.** `BonboardingPlacement.BottomStart` aligns to the target's right edge
  in RTL.
- **Logical sides follow the direction.** `Left` and `Right` are physical; `InlineStart` (with `InlineStartTop`
  and `InlineStartBottom`) is the side text starts from and `InlineEnd` the side it ends on, so one placement
  serves both directions: `InlineEnd` is on the right in English and on the left in Arabic or Hebrew.
- **Arrow keys swap.** In an RTL tour, Left advances and Right goes back, matching reading order.
- **Layout uses logical properties.** Padding, margins and the footer's button order all flip.

The root element carries `dir="rtl"`, so anything you render inside a template inherits it.

### Mixed-direction applications

A page can be LTR while one tour is RTL, or the reverse. Set `Direction` on the tour rather than
relying on `Auto`, and the popover's own `dir` attribute keeps its contents consistent regardless of
the page around it.

### Number and date formatting

`ProgressFormat` and `StepAnnouncementFormat` are passed through `string.Format` with the current
culture, so digits are localized the way the rest of your application formats them.

Everything the library generates for CSS - positions, sizes, opacities, durations - is formatted with
the invariant culture. A comma decimal separator would produce silently broken styles, and that is a
bug that only shows up for users in another locale.

## Accessibility

The target is WCAG 2.2 AA. Accessibility is handled in the engine and the host component rather than
left to the application, because a tour that is only usable with a mouse is not a tour, it is an
obstacle.

### What the library provides

#### Dialog semantics

The popover is a `role="dialog"` with `tabindex="-1"`, so it can receive focus.

- `aria-labelledby` points at the step title when the default chrome renders one; otherwise the
  title (then the tour title, then `Labels.DialogLabel`) is passed as `aria-label`, so the dialog is
  never unnamed - not even behind a `Template`, a `ChildContent` or a step `Header` of your own.
- `aria-describedby` points at the description when the default chrome renders one.
- `aria-modal` is `true` only when `Interaction` is `Blocked`. In `TargetOnly` and `Free` the rest of
  the page really is available, and claiming otherwise would hide content a screen reader user can
  legitimately reach.

#### Focus management

- The element focused before the tour started is remembered and restored when it ends.
  `RestoreFocus = false` opts out.
- Focus moves into the popover when a step is shown, once per step, after the content has rendered
  and the popover has been placed (it is hidden until its first measurement, and a hidden element
  cannot take focus).
  It prefers an element marked `data-bonboarding-autofocus`, then the first focusable control, then the dialog
  itself. `AutoFocus = false` opts out.
- Focus is trapped inside the popover while `TrapFocus` is on (the default), and the setting is
  honoured per step. On a `TargetOnly` step the trap also includes the highlighted element, so the
  user can Tab to the control the step is asking them to use. On a `Free` step the trap only
  applies while focus is already inside the popover: the rest of the page stays tabbable, which
  is what `Free` promises.

#### Keyboard

| Key | Action |
|---|---|
| `Escape` | Ends the tour, unless `CloseOnEscape` is false |
| `→` / `←` | Next / previous, reversed in RTL |
| `Home` / `End` | First / last step |
| `Tab` / `Shift+Tab` | Cycles inside the trap |
| `Enter` / `Space` | Activates the focused button, as any button does |

Arrow keys are ignored while focus is in a text input, textarea, select or contenteditable, so a step
that asks the user to type something does not steal their cursor keys.

Key handling is installed on the document in the capture phase. On a `Blocked` step the tour owns the
keyboard wherever focus is. On a `TargetOnly` or `Free` step it only handles `Escape` and the arrow
keys while focus is in the popover (or on nothing in particular): with focus in the application,
they belong to the application's menus, dialogs, sliders and lists.

#### Announcements

A polite live region outside the tour's own subtree announces each step:

> Step 2 of 5. Create a project

It also announces the waiting state. The region is rendered whether or not a tour is running, so
assistive technology never has to discover it mid-tour, and the format strings are part of
`BonboardingLabels` so they can be translated.

Progress indicators are announced correctly for their shape: the bar is a `role="progressbar"` with
`aria-valuenow`, `aria-valuemax` and `aria-valuetext`; the dots are decorative and carry a visually
hidden text equivalent.

#### Motion

`prefers-reduced-motion` is sampled and, when honoured, sets the animation duration to zero, which
disables the transitions, the entry fades and the spotlight's movement between steps. Set
`RespectReducedMotion = false` to ignore the preference, though there is rarely a good reason to.

#### Contrast and forced colors

The default light and dark palettes meet AA contrast for body text, muted text and button labels. A
`forced-colors` block replaces the shadow with a border, outlines the spotlight with `Highlight`,
gives buttons a `ButtonText` border, draws a beacon's dot in `Highlight`, and turns every focus ring
(drawn with `box-shadow`, which forced colors do not paint) into a `Highlight` outline, so nothing relies
on colour or shadow that Windows high contrast has removed.

#### Target size

Under `pointer: coarse`, buttons grow to a minimum of 36 CSS pixels tall and the close button to
36×36, clearing the WCAG 2.2 target-size minimum of 24×24 with room to spare.

### What you still own

**The content.** Titles and descriptions should make sense read aloud, out of visual context. "Click
the button on the left" is not a description; "Create a project from the toolbar" is.

**Custom templates.** If you replace the popover contents, the dialog element, its label, the focus
trap and the announcements are still handled for you, but the content inside is yours: use real
buttons, label your inputs, and keep a sensible tab order.

**Colour overrides.** Changing `--bit-bonboarding-accent` to something low-contrast against `--bit-bonboarding-accent-text`
will break contrast. Check both themes after restyling.

**Interactive steps.** A step with `AdvanceOn` asks the user to operate a control. Make sure that
control is reachable by keyboard, and say what to do in words rather than relying on the highlight.

### Testing

Things worth checking in your own application:

- Run a whole tour with the keyboard only, starting from a page where focus is in a text field.
- Confirm focus returns to the element you started from.
- Listen to a tour with a screen reader and check that each step is announced once.
- Turn on reduced motion and confirm nothing animates.
- Turn on Windows high contrast and confirm the spotlight and the popover are both visible.
- Zoom to 200% and confirm the popover stays on screen and its content scrolls rather than clipping.

The library's own test suite covers dialog semantics, labelling, live-region announcements, focus
capture and restoration, keyboard navigation, and the reduced-motion path. Its browser suite
(`Tests/Bit.Bonboarding.Tests.E2E`) also runs axe-core's WCAG 2.2 A and AA rules on every demo page, on a
spotlight step and a hint in both themes and on the waiting card, checks that Tab cannot leave a modal step
and is not trapped while a step waits, and that focus stays visible in forced colors. Automated rules catch a
part of what matters; the list above is still worth going through by hand.

### Known limitations

- **A target inside a modal `<dialog>`** opened with `showModal()`: the dialog sits in the browser's top layer
  above any `z-index`, and makes everything outside it inert, the tour's popover included. Open such dialogs
  without `showModal()` while a tour points into them, or guide the dialog with steps that have no target.
- **Shadow DOM and iframes**: targets are found with `querySelector` on the document, which does not look inside
  shadow roots or frames. A custom element's host can be targeted; its insides cannot.
- **A scaled ancestor** (`zoom`, `transform: scale()`) of `BonboardingRoot`: a transformed ancestor is compensated
  for, a scaled one is not. Place the host outside it.

## Persistence

The library records, per tour, whether the user finished it and where they got to. That is what makes
"show this once", "resume where I left off" and "replay from the help menu" all work.

### What is stored

```csharp
public sealed record BonboardingRecord
{
    public required string TourId { get; init; }
    public int Version { get; init; }
    public string? CurrentStepId { get; init; }
    public int CurrentIndex { get; init; }
    public bool Completed { get; init; }
    public bool Dismissed { get; init; }
    public IReadOnlyList<string> VisitedStepIds { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public DateTimeOffset? SnoozedUntil { get; init; }
    public IReadOnlyDictionary<string, string>? Data { get; init; }

    public bool IsClosed => Completed || Dismissed;
    public bool IsSnoozedAt(DateTimeOffset now);
}
```

A record is written after every step, and again when the tour ends. `Data` belongs to your
application: write it through your `IBonboardingStore`, and the library carries it over on every
write of its own.

**Completion is sticky.** `Completed` says the user ran the tour to the end at some point, so replaying a
finished tour from a help menu does not un-finish it, however the replay ends; `CompletedAt` is the most
recent completion, and `Dismissed` how the most recent run ended. A completion recorded by another version
of the tour is not carried over (see [Versioning](#versioning)).

### How a tour ends matters

| Ending | `Completed` | `Dismissed` | Offered again by `StartOnceAsync` |
|---|---|---|---|
| `CompleteAsync` | yes | no | no |
| `SkipAsync` | no | yes | no |
| `DismissAsync` | no | yes | no |
| `CancelAsync` | no | no | **yes** |
| `SnoozeAsync` | no | no | **after the snooze** |
| Failed | no | no | yes |

`CancelAsync` is the "interrupted, not refused" ending. Starting another tour cancels the running one
rather than dismissing it, so being interrupted never counts as the user turning a tour down.

`SnoozeAsync` is "remind me later". The record keeps the user's place and gets a `SnoozedUntil`; until
then `StartOnceAsync` and a declarative `AutoStart` pass the tour over, and afterwards it resumes where
it was snoozed. A start the user asks for (`StartAsync`) is not held back by a snooze. The simplest way to
offer it is the built-in action:

```csharp
new BonboardingStepDefinition
{
    Title = "A quick tour?",
    Actions = [BonboardingStepAction.RemindMeLater("Not now", TimeSpan.FromDays(3))],
}
```

Snoozes and record times come from the registered `TimeProvider` (`TimeProvider.System` unless you
register another), so tests can move time on.

### Resuming

`StartAsync` resumes by default:

```csharp
await BonboardingService.StartAsync(tour);                                  // resumes
await BonboardingService.StartAsync(tour, new BonboardingStartOptions { Resume = false });  // starts at step one
```

Resuming prefers the saved **step id** and falls back to the index, because inserting a step shifts
every index after it. History entries pointing at steps that no longer exist are dropped, so Back
cannot land on a ghost.

A record whose `IsClosed` is true is not resumed into: a user who finished or dismissed a tour and
then explicitly starts it again wants the beginning.

### Versioning

Bump `BonboardingTourDefinition.Version` whenever you edit the steps in a way that changes what a saved position
means:

```csharp
new BonboardingTourDefinition { Id = "welcome", Version = 2, ... }
```

A record written by an older version is discarded rather than resumed into, and
`IsCompletedAsync` / `IsClosedAsync` report `false`, so `StartOnceAsync` offers the updated tour
again.

When a bump should not mean "everyone sees it again", migrate the old record instead. Return it unchanged
to keep everything (the right choice after rewording a tour), map renamed step ids, or return `null` to
discard it after all. The result is stamped with the current version; a migration that throws is logged and
treated as `null`.

```csharp
new BonboardingTourDefinition
{
    Id = "welcome",
    Version = 3,
    MigrateRecord = record => record.Version == 2
        ? record with { CurrentStepId = record.CurrentStepId == "plans" ? "pricing" : record.CurrentStepId }
        : null,
}
```

### Turning it off

```csharp
new BonboardingTourDefinition { Id = "playground", Persist = false }             // one tour
builder.Services.AddBitBonboardingServices(o => o.Persist = false);         // everything
```

Transient hints, playgrounds and previews are usually better off unpersisted.

### Where it is stored

The default is `BonboardingBrowserStore`: one `localStorage` entry per tour, keyed by
`BonboardingOptions.StorageKeyPrefix` (default `bit-bonboarding:`), with an in-memory mirror.

The mirror matters more than it sounds:

- during prerendering there is no browser, so reads come from memory instead of throwing;
- in private browsing, or with site data blocked, writes fail silently and reads still work for the
  rest of the session;
- corrupt or hand-edited JSON is discarded and treated as "never seen this tour" rather than crashing.

`ClearAsync` only removes keys carrying the configured prefix. The rest of `localStorage` belongs to
your application.

#### Several people on one device

A shared computer, or an application where several accounts can be signed in at once, must not let one person's
finished tours hide them from the next. Tell the store whose progress it is:

```csharp
builder.Services.AddBitBonboardingServices(options =>
{
    options.StorageScope = async (services, ct) =>
        (await services.GetRequiredService<AuthenticationStateProvider>().GetAuthenticationStateAsync())
            .User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
});
```

Every key then carries the owner (`bit-bonboarding:@{user}/{tourId}`, the user escaped), the in-memory mirror is
kept per owner too, so signing in as someone else in the same tab never shows the previous person's progress, and
`ResetAllAsync` forgets only the current person's records (or, while nobody is signed in, only the device's).
`null` - the default, and the right answer while nobody is signed in - stores progress for the device as a whole.
The scope is asked on every read and write, so return a cached value; one that throws is logged and treated as
`null` rather than losing the progress.

#### Two tabs

`localStorage` is shared by every tab of the site, and the store listens to the browser's `storage` event, so what
one tab writes the others pick up: their cached copy is dropped, a checklist ticks off in every tab, a tour list
updates, and a tour the user completed, dismissed, skipped or snoozed in one tab stops in the others (cancelled,
which writes nothing, so the decision taken in the first tab stands). Progress made in one tab does not stop the
tour in another.

The notification comes from `IBonboardingStoreEvents`, which a store of your own can implement as well - a
server-backed store pushing changes made on another device over SignalR, say. Raise its `Changed` event with the
record's key, or `null` when everything may have changed.

#### One visit only

```csharp
builder.Services.AddBitBonboardingServices(options => options.StorageLocation = BonboardingStorageLocation.Session);
```

Keeps progress in `sessionStorage` instead: it lasts as long as the tab, so `StartOnceAsync` means once per visit.

#### Memory only

```csharp
builder.Services.AddBitBonboardingServices();
builder.Services.AddBitBonboardingInMemoryStore();
```

#### Server-side

Storing progress against the user's profile is usually what a real product wants: it follows people
across devices and survives a cleared browser.

```csharp
public sealed class ProfileOnboardingStore(IUserProfiles profiles, ICurrentUser user) : IBonboardingStore
{
    public ValueTask<BonboardingRecord?> GetAsync(string tourId, CancellationToken ct = default)
        => profiles.GetOnboardingAsync(user.Id, tourId, ct);

    public ValueTask SetAsync(BonboardingRecord record, CancellationToken ct = default)
        => profiles.SaveOnboardingAsync(user.Id, record, ct);

    public ValueTask RemoveAsync(string tourId, CancellationToken ct = default)
        => profiles.DeleteOnboardingAsync(user.Id, tourId, ct);

    public ValueTask ClearAsync(CancellationToken ct = default)
        => profiles.DeleteAllOnboardingAsync(user.Id, ct);
}
```

```csharp
builder.Services.AddBitBonboardingServices();
builder.Services.AddBitBonboardingStore<ProfileOnboardingStore>();
```

The store is resolved as a scoped service, so injecting the current user is fine.

Writes happen on every step transition. If yours is a network call, consider debouncing or
batching inside the store; the engine never blocks a transition on the result, and a store that
throws a transient failure is logged rather than allowed to break the tour.

### Querying and resetting

```csharp
await BonboardingService.IsCompletedAsync("welcome");   // ran to the end at some point
await BonboardingService.IsClosedAsync("welcome");      // completed, skipped or dismissed
await BonboardingService.GetRecordAsync("welcome");     // the whole record, or null

await BonboardingService.ResetAsync("welcome");         // forget one tour
await BonboardingService.ResetAllAsync();               // forget every tour, and every checklist's progress

await BonboardingService.RestartAsync(tour);            // reset, then start from the beginning
```

The checklists, tour lists and beacons on screen follow a reset at once. `RestartAsync` checks the definition
first, so a tour that cannot start (see [Checking a definition](#checking-a-definition)) throws without costing the
user their progress. A "replay the tours" button in a settings page is `ResetAllAsync` followed by a reload.

## Events and analytics

Onboarding is only worth building if you can tell whether it worked. Every moment of a tour is
published as an `BonboardingEvent`, including how long each step was on screen.

### The event stream

| Kind | When |
|---|---|
| `TourStarted` | A session started |
| `TourResumed` | It resumed from saved progress rather than starting fresh |
| `StepShown` | A step is on screen |
| `StepLeft` | A step that was on screen was left; `Duration` says for how long. Steps skipped before they were shown do not raise it |
| `StepSkipped` | A step was skipped because its target was missing |
| `TargetMissing` | A target could not be found, or disappeared mid-step |
| `TargetResolved` | A target was found, including after a wait |
| `ActionInvoked` | A custom footer action was pressed; `Action` is populated |
| `TourPaused` | `PauseAsync` was called |
| `TourCompleted` | Ran to the end |
| `TourSkipped` | The user skipped the rest |
| `TourDismissed` | The user closed it |
| `TourCancelled` | Interrupted by the application; progress kept. Also a queued start withdrawn before it ran |
| `TourFailed` | A hook threw, or a `Fail` strategy fired; `Error` is populated |
| `AdvanceBlocked` | `CanAdvance` held the user on the step |
| `TourSnoozed` | The user asked to be reminded later; progress kept |
| `TourQueued` | A start found another tour running and waits for it (`TourStarted` follows when it really starts) |

Each event carries:

```csharp
e.Kind
e.Session      // the IBonboardingTourSession
e.Tour         // shorthand for e.Session.Tour
e.Step         // the BonboardingStepDefinition, when the event is about one
e.StepIndex
e.Duration     // how long the step was shown, on StepLeft
e.Action       // on ActionInvoked
e.Reason       // BonboardingTourEndReason, on terminal events
e.Error        // on TourFailed
e.Timestamp
```

### Observers

The registered way to consume events is an `IBonboardingObserver`. Any number can be registered; each
receives every event.

```csharp
public sealed class TourAnalytics(IAnalyticsClient client, ILogger<TourAnalytics> logger)
    : IBonboardingObserver
{
    public async ValueTask OnEventAsync(BonboardingEvent e, CancellationToken ct = default)
    {
        switch (e.Kind)
        {
            case BonboardingEventKind.TourStarted:
                await client.TrackAsync("tour_started", new { tour = e.Tour.Id }, ct);
                break;

            case BonboardingEventKind.StepShown:
                await client.TrackAsync("tour_step_shown", new
                {
                    tour = e.Tour.Id,
                    step = e.Step?.Id,
                    position = e.Session.DisplayPosition,
                    total = e.Session.DisplayCount,
                }, ct);
                break;

            case BonboardingEventKind.StepLeft:
                await client.TrackAsync("tour_step_left", new
                {
                    step = e.Step?.Id,
                    ms = e.Duration?.TotalMilliseconds,
                }, ct);
                break;

            case BonboardingEventKind.TourCompleted:
            case BonboardingEventKind.TourSkipped:
            case BonboardingEventKind.TourDismissed:
                await client.TrackAsync("tour_ended", new
                {
                    tour = e.Tour.Id,
                    reason = e.Reason?.ToString(),
                    lastStep = e.Step?.Id,
                }, ct);
                break;

            case BonboardingEventKind.TourFailed:
                logger.LogError(e.Error, "Tour {TourId} failed at step {StepId}.", e.Tour.Id, e.Step?.Id);
                break;
        }
    }
}
```

```csharp
builder.Services.AddBitBonboardingObserver<TourAnalytics>();
```

An observer that throws is logged and the tour carries on. Analytics being down is never a reason for
onboarding to break.

### The quick way

For something local - a debug panel, a page that reacts to a tour - subscribe to the service:

```razor
@implements IDisposable
@inject IBonboardingService BonboardingService

@code {
    protected override void OnInitialized() => BonboardingService.EventRaised += OnEvent;

    public void Dispose() => BonboardingService.EventRaised -= OnEvent;

    private void OnEvent(object? sender, BonboardingEvent e) => InvokeAsync(StateHasChanged);
}
```

### Per-tour and per-step callbacks

For logic that belongs to one tour rather than to your analytics pipeline:

```csharp
new BonboardingTourDefinition
{
    OnStarted = ctx => ValueTask.CompletedTask,
    OnStepChanged = args => ValueTask.CompletedTask,   // Step, PreviousStep, Reason
    OnEnded = args => ValueTask.CompletedTask,         // Reason, LastStep, Error, IsSuccess
}
```

```csharp
new BonboardingStepDefinition
{
    OnBeforeShow = ctx => ValueTask.CompletedTask,
    OnShown = ctx => ValueTask.CompletedTask,
    OnBeforeLeave = ctx => ValueTask.CompletedTask,
}
```

`BonboardingStepChangedEventArgs.Reason` tells you how the user got there: `Start`, `Next`, `Previous`, `Jump`,
`Resume` or `Retarget` (a re-entry after a route change).

### Attaching your own dimensions

`BonboardingStepDefinition.Metadata` and `.Data` ride along to every event and template:

```csharp
new BonboardingStepDefinition
{
    Id = "connect-repo",
    Metadata = new Dictionary<string, object?>
    {
        ["funnel"] = "activation",
        ["required"] = true,
    },
}
```

```csharp
if (e.Step?.Metadata?.TryGetValue("funnel", out var funnel) == true)
{
    await client.TrackAsync("funnel_step", new { funnel, step = e.Step.Id });
}
```

Session-scoped values live in `e.Session.State`, which is shared by every hook and template in the
tour and is a good place for things like the branch a user chose.

### What to measure

A few things that tend to be worth the trouble:

- **Completion rate per tour.** `TourCompleted` over `TourStarted`, ignoring `TourCancelled`.
- **Drop-off by step.** The last `StepShown` before a `TourDismissed` or `TourSkipped`. This is the
  single most useful number: it tells you which step is not worth the interruption.
- **Time per step**, from `StepLeft.Duration`. A step people leave in under a second was not read.
- **`TargetMissing` counts.** A step whose target regularly goes missing is pointing at something
  that has moved or been renamed, and is quietly being skipped in production.
- **Resume rate.** `TourResumed` over `TourStarted` shows how often people come back to a tour they
  did not finish in one sitting.

## Performance

### What a running tour actually costs

**When nothing is happening: nothing.** There are no timers and no polling. The browser layer holds a
`ResizeObserver`, an `IntersectionObserver`, two passive listeners and, only while waiting for a
target, a `MutationObserver`. An idle page with a tour open does no work at all.

**When something moves:** at most one measurement per animation frame. Every observer feeds a single
`requestAnimationFrame` callback that takes one measurement of the target, the popover and the
viewport, and compares it with the previous frame. If nothing changed by more than half a pixel, no
interop call is made. A momentum scroll therefore costs one small interop call per frame, and a
resize storm the same.

**Per step:** one `activateStep` call, which resolves the target, scrolls it into view, installs the
observers and returns the first measurement. Not one call per operation.

**Per render:** one component. `BonboardingRoot` is the only thing that re-renders when a tour moves.
Nothing in your application re-renders because a tour advanced, unless you subscribed to the events
yourself.

### Where the cost actually is

#### Conditions are asked repeatedly

`BonboardingStepDefinition.When` is evaluated for **every step on every transition**, because that is what keeps
the progress count honest and lets a condition depend on state an earlier step changed.

So conditions must be cheap:

```csharp
// Good: reads state.
When = ctx => ValueTask.FromResult(_user.IsAdmin)

// Bad: a network call, several times per step change.
When = async ctx => (await _api.GetPermissionsAsync()).CanBill
```

Load once and cache in the session:

```csharp
new BonboardingTourDefinition
{
    OnStarted = async ctx =>
    {
        ctx.Session!.State["canBill"] = await _api.CanBillAsync();
    },
    Steps =
    {
        new BonboardingStepDefinition
        {
            When = ctx => ValueTask.FromResult(ctx.State["canBill"] is true),
        },
    },
}
```

#### Selectors are re-queried

Targets are located by `querySelector` on every measurement where the previous element left the DOM,
and on every explicit refresh. That is what makes re-rendered targets work, but it means an expensive
selector is run more often than you might expect.

`[data-bonboarding-anchor="x"]` is an attribute-value match on a single attribute and is about as cheap as a
selector gets. Prefer it to deep descendant selectors like `.sidebar > ul li:nth-child(3) a`.

#### Blazor Server round trips

On Blazor Server each interop call is a round trip over the circuit. The frame de-duplication is what
makes this viable: a scroll produces one small message per frame at most, and only while the geometry
is actually changing.

If your users are on high-latency connections and tours feel sluggish while scrolling, the usual
lever is `Scroll = BonboardingScrollMode.Instant`, which removes the settle-detection frames.

#### Very large tours

Nothing is rendered for steps that are not on screen, so a 50-step tour costs the same per step as a
3-step one. The plan rebuild is O(steps) per transition, which is only worth thinking about if your
conditions are expensive - see above.

### Things that are already handled

- **Out-of-order frames.** Measurements carry a monotonic sequence number and older ones are dropped,
  so a delayed message from a resize storm cannot move the popover backwards.
- **Rapid clicking.** Every command takes a single transition lock, so three fast clicks on Next
  advance three steps in order rather than interleaving.
- **Stale async work.** A transition bumps a generation counter. Work that resumes after the user has
  already moved on detects it lost the race and abandons its results instead of applying them to the
  wrong step.
- **Invariant formatting.** All generated CSS uses the invariant culture, so nothing changes
  behaviour under a comma-decimal locale.

### Cleanup

Everything is released when a tour ends or a scope is disposed: observers disconnected, listeners
removed, animation frames cancelled, timers cleared, the `DotNetObjectReference` disposed and the JS
module released.

Both synchronous and asynchronous disposal are implemented. Blazor disposes scopes asynchronously,
but plenty of hosts and test frameworks do not, and a service that only supported `IAsyncDisposable`
would make those throw.

Interop failures during teardown - a dropped circuit, a page unload, prerendering - are treated as
expected rather than as errors, so a closing tab never produces an exception in your logs. Other
survivable failures, such as a JavaScript error in the browser layer or an `InvalidOperationException`
from a custom store, do not stop the tour either, but are logged as warnings so they are not hidden.

### Measuring it yourself

The demo's [events page](#events-and-analytics) shows the live event stream, which is the easiest way
to see how many transitions a tour is actually performing. For interop volume, the browser's
performance profiler will show the `requestAnimationFrame` callbacks; a healthy idle tour shows none.

### A checklist

- Prefer `data-bonboarding-anchor` over deep CSS selectors.
- Keep `When` conditions synchronous and free of I/O.
- Give every step an explicit `Id` so persistence does not fall back to indices.
- Use `Persist = false` for playgrounds and transient hints so they never write.
- Set `Scroll = BonboardingScrollMode.None` for steps whose target is always in view.
- Place exactly one `BonboardingRoot`, in the layout.

## Upgrading

Nothing public was removed or renamed, and members added to `IBonboardingTourSession` and `IBonboardingInterop`
have default implementations, so code written against an earlier version compiles unchanged. The exception is an
implementation of `IBonboardingService` of your own, such as a decorator: it now has to forward `SignalAsync` and
the checklist members too, because the only default they could have would do nothing, silently. A few behaviours
changed; each made the library do what its documentation already promised, but an application may have come to
rely on the old behaviour:

| What changed | Before | Now | If you relied on the old behaviour |
|---|---|---|---|
| `BonboardingTheme.System` (the default) | Always the light palette | The dark palette on a dark operating system | Set `Theme = BonboardingTheme.Light`, or keep restyling `.bit-bonboarding-root`, which still wins (the system rule has the same specificity). |
| A step waiting for its route | The page was dimmed and blocked behind a waiting card | Nothing is rendered until the route matches | Render your own indicator from `session.WaitReason`. |
| A step waiting for its target | The page was blocked and Tab trapped | The page stays usable; the waiting card has a close button | Nothing to do, unless a test expected the blocker. |
| A target that exists but is not rendered | Counted as found: no spotlight, a centred card | Counted as missing: the missing-target strategy applies | Set `MissingTarget = Center` for the old look. |
| Replaying a completed tour | Wrote `Completed = false` until it was completed again | Completion is sticky (per version) | Use `ResetAsync` or `RestartAsync` to forget a completion on purpose. |
| A definition with duplicate step ids or a jump to nowhere | Started, and misbehaved | `StartAsync` throws `BonboardingException` listing the problems | Fix the definition; `Validate()` lists the same problems in a test. |
| A declarative `AutoStart` while another tour runs | Cancelled the running tour | Queues behind it, by `Priority` | Start the tour yourself with the default `WhenBusy = Replace`. |
| `BonboardingTour.StopAsync(BonboardingTourEndReason.Cancelled)` | Dismissed the tour | Cancels it (progress kept, not recorded as refused) | Pass `Dismissed`. |
| A double click on an action whose handler is still running | Ran the handler twice | The second click is ignored while the first runs | Nothing to do. |
| An `AdvanceOn` trigger held back by `CanAdvance` | Never fired again | Stays armed until the step changes | Nothing to do. |
| An unparseable target selector | Waited out the whole timeout silently | Logged as an error and handled as a timed-out wait at once | Nothing to do. |

New enum members are appended (`BonboardingTourStatus.Snoozed`, `BonboardingTourEndReason.Snoozed`,
`BonboardingStepActionEffect.Snooze`, `BonboardingEventKind.AdvanceBlocked`/`TourSnoozed`/`TourQueued`, the
`Inline*` placements), so stored and reported values keep their meaning; a `switch` over one of them may want the
new cases.

## API reference

Everything public lives in the `Bit.Bonboarding` namespace.

- [Components](#components)
- [Services](#services)
- [Tour and step model](#tour-and-step-model)
- [Options](#options)
- [Targeting](#targeting)
- [Sessions](#sessions)
- [Events](#events)
- [Persistence](#persistence)
- [Localization](#localization)
- [Layout](#layout)
- [Geometry](#geometry)
- [Enums](#enums)
- [Registration](#registration)
- [Browser layer](#browser-layer)

---

### Components

#### `BonboardingRoot`

Renders the running tour. Place one, usually in the layout.

| Parameter | Type | Description |
|---|---|---|
| `Session` | `IBonboardingTourSession?` | Render this session instead of the active one, and claim it so other hosts skip it. |
| `Theme` | `BonboardingTheme?` | Force a colour scheme for everything this host renders. |
| `Class` | `string?` | Extra classes on the root element. |
| `PopoverClass` | `string?` | Extra classes on the popover element. |
| `Unstyled` | `bool` | Drop the built-in popover classes, keeping the element and its behaviour. |
| `ChildContent` | `RenderFragment<BonboardingStepRenderContext>?` | Replace the popover contents for every step. |
| `OverlayTemplate` | `RenderFragment<BonboardingStepRenderContext>?` | Replace the overlay and spotlight visuals. |
| `WaitingTemplate` | `RenderFragment<BonboardingStepRenderContext>?` | Replace the waiting placeholder. |

#### `BonboardingTour`

Declares a tour in markup. Renders nothing.

| Parameter | Type | Description |
|---|---|---|
| `Id` | `string` | Required. Also the persistence key. |
| `Title` | `string?` | Dialog label fallback. |
| `Description` | `string?` | Shown by a tour list. |
| `Version` | `int` | Bump to discard stale saved progress. Default `1`. |
| `AutoStart` | `BonboardingAutoStart` | `None`, `Once` or `Always`. Default `None`. An automatic start queues behind a running tour and passes over a snoozed one. |
| `Priority` | `int` | Order among queued starts. Default `0`. |
| `CanStart` | `Func<BonboardingTourContext, ValueTask<bool>>?` | Gate evaluated before starting. |
| `Data` | `object?` | Arbitrary payload. |
| `ChildContent` | `RenderFragment?` | The `BonboardingStep` children. |
| `Template` | `RenderFragment<BonboardingStepRenderContext>?` | Replace the popover contents for this tour. |
| `Labels` | `BonboardingLabels?` | Override the label set. |
| `Persist` | `bool?` | Override global persistence. |
| `Placement`, `Theme`, `Direction`, `Progress`, `Interaction`, `ShowOverlay`, `ShowSpotlight`, `ShowSkip`, `ShowClose`, `ShowPrevious`, `ShowNext`, `CloseOnEscape`, `CloseOnOverlayClick`, `Offset`, `OverlayOpacity`, `SpotlightPadding`, `SpotlightRadius`, `SpotlightShape`, `Scroll`, `MissingTarget`, `WaitTimeout`, `WaitTimeoutBehavior`, `AnimationDuration`, `ZIndex`, `PopoverClass`, `KeyboardNavigation`, `TrapFocus`, `RestoreFocus`, `AutoFocus` | nullable | Defaults for every step. |
| `Configure` | `Action<BonboardingTourDefinition>?` | Escape hatch for anything not exposed as a parameter. Runs on every rebuild, so keep it idempotent. |
| `OnStarted` | `EventCallback` | |
| `OnStepChanged` | `EventCallback<BonboardingStepChangedEventArgs>` | |
| `OnEnded` | `EventCallback<BonboardingTourEndedEventArgs>` | |

Members: `Definition`, `Session`, `IsRunning`, `StartAsync`, `StartOnceAsync`, `RestartAsync`,
`StopAsync`.

#### `BonboardingStep`

Declares one step. Renders nothing. Must be inside an `BonboardingTour`.

| Parameter | Type | Description |
|---|---|---|
| `Id` | `string?` | Strongly recommended for persistence and branching. |
| `Order` | `int?` | Explicit position: placed as the n-th step, ahead of the step that rendered n-th. Otherwise declaration order. |
| `Target` | `BonboardingStepTarget?` | Any target kind. |
| `Anchor` | `string?` | Shorthand for `BonboardingStepTarget.Anchor`. |
| `Selector` | `string?` | Shorthand for `BonboardingStepTarget.Css`. |
| `Element` | `ElementReference?` | Shorthand for `BonboardingStepTarget.Element`. |
| `AdditionalTargets` | `IReadOnlyList<BonboardingStepTarget>?` | Folded into the same spotlight. |
| `Title`, `Description` | `string?` | |
| `ChildContent` | `RenderFragment<BonboardingStepRenderContext>?` | Step body. |
| `Header`, `Footer`, `Template` | `RenderFragment<BonboardingStepRenderContext>?` | Region or whole-popover replacements. |
| `Actions` | `IReadOnlyList<BonboardingStepAction>?` | Extra footer buttons. |
| `Route` | `string?` | Route this step belongs to; `*` suffix for a prefix match. |
| `When` | `Func<BonboardingStepContext, ValueTask<bool>>?` | Gate. |
| `CanAdvance` | `Func<BonboardingStepContext, ValueTask<bool>>?` | Holds the user on the step. |
| `ValidationMessage` | `string?` | Shown and announced when `CanAdvance` holds the user. |
| `ResolveNext`, `ResolvePrevious` | `Func<BonboardingStepContext, ValueTask<string?>>?` | Branching. |
| `AdvanceOn` | `BonboardingAdvanceTrigger?` | Advance on a real interaction. |
| `AdvanceOnSignal` | `string?` | Advance when the application raises this signal. |
| `OnBeforeShow`, `OnShown`, `OnBeforeLeave` | `Func<BonboardingStepContext, ValueTask>?` | Lifecycle. |
| `Data` | `object?` | Arbitrary payload. |
| `Metadata` | `IReadOnlyDictionary<string, object?>?` | Analytics dimensions. |
| `NextLabel`, `PreviousLabel`, `SkipLabel`, `DoneLabel` | `string?` | Per-step label overrides. |
| Presentation overrides | nullable | `Placement`, `PlacementFallbacks`, `Offset`, `ViewportPadding`, `ArrowSize`, `ShowSpotlight`, `SpotlightPadding`, `SpotlightRadius`, `SpotlightShape`, `ShowOverlay`, `OverlayOpacity`, `Interaction`, `CloseOnOverlayClick`, `CloseOnEscape`, `Scroll`, `ScrollPadding`, `MissingTarget`, `WaitTimeout`, `WaitTimeoutBehavior`, `Progress`, `ShowNext`, `ShowPrevious`, `ShowSkip`, `ShowClose`, `AnimationDuration`, `Theme`, `Direction`, `ZIndex`, `PopoverClass`, `KeyboardNavigation`, `TrapFocus`, `AutoFocus` |
| `Configure` | `Action<BonboardingStepDefinition>?` | Escape hatch. Runs on every rebuild, so keep it idempotent. |

#### `BonboardingBeacon`

A pulsing dot on an element; see [Beacons](#beacons).

| Parameter | Type | Description |
|---|---|---|
| `Id` | `string?` | Persists the hint under this id; the beacon goes away once it has been read. |
| `Target`, `Anchor`, `Selector`, `Element` | | The element it sits on (one is required). |
| `Position` | `BonboardingBeaconPosition` | Corner, edge or centre. Default `TopEnd`. |
| `OffsetX`, `OffsetY` | `double` | Nudge, in pixels (X towards the inline end). |
| `Title`, `Description`, `ChildContent` | | The hint. |
| `Placement` | `BonboardingPlacement?` | Where the hint goes. |
| `Tour`, `TourId` | | Start this tour instead of opening a hint. |
| `KeepAfterClosed` | `bool` | Keep showing after its tour was completed or dismissed. |
| `OnClick` | `EventCallback` | Run this instead. |
| `Label` | `string?` | Accessible name; defaults to `Title`, then `BonboardingLabels.BeaconLabel`. |
| `HideWhileTourRuns` | `bool` | Default `true`. |
| `Theme`, `Class` | | |

Member: `IsOpen`.

#### `BonboardingChecklist`

Renders a `BonboardingChecklistDefinition`; see [Checklists](#checklists).

| Parameter | Type | Description |
|---|---|---|
| `Checklist` | `BonboardingChecklistDefinition` | Required. |
| `Dismissible` | `bool` | Show a button that hides it for good. |
| `HideWhenComplete` | `bool` | Render nothing once every task is done. |
| `HeadingLevel` | `int` | 1-6, default `2`. |
| `ItemTemplate` | `RenderFragment<BonboardingChecklistItemState>?` | Replace each task's markup. |
| `OnCompleted` | `EventCallback` | The last task got done while on screen. |
| `Theme`, `Class` | | |

Members: `Items`, `IsComplete`, `RefreshAsync()`.

`BonboardingChecklistDefinition`: `Id`, `Title`, `Description`, `Items`, `FindItem`, constant `StorageKeyPrefix`.
`BonboardingChecklistItem`: `Id`, `Title`, `Description`, `Tour`, `TourId`, `Route`, `OnClick`, `IsComplete`,
`CompleteOnSignal`, `When`.

#### `BonboardingTourList`

Tours to start, resume or replay; see [Tour lists](#tour-lists).

| Parameter | Type | Description |
|---|---|---|
| `Tours` | `IEnumerable<BonboardingTourDefinition>?` | Defaults to every registered tour, by title. |
| `Title` | `string?` | Heading. |
| `HeadingLevel` | `int` | 1-6, default `2`. |
| `HideCompleted` | `bool` | Leave out completed tours. |
| `ItemTemplate` | `RenderFragment<BonboardingTourListItem>?` | Replace each tour's markup. |
| `EmptyContent` | `RenderFragment?` | Shown when there is nothing to list. |
| `Theme`, `Class` | | |

Members: `Items`, `RefreshAsync()`.

---

### Services

#### `IBonboardingService`

Scoped. The entry point for starting and querying tours.

```csharp
IBonboardingTourSession? Active { get; }
IReadOnlyList<IBonboardingTourSession> Sessions { get; }
IReadOnlyCollection<BonboardingTourDefinition> RegisteredTours { get; }

event EventHandler<BonboardingEvent>? EventRaised;
event EventHandler? SessionsChanged;

void Register(BonboardingTourDefinition tour);   // StartAsync does not register a tour
void Unregister(string tourId);
BonboardingTourDefinition? GetTour(string tourId);

Task<IBonboardingTourSession> StartAsync(BonboardingTourDefinition tour, BonboardingStartOptions? options = null, CancellationToken ct = default);
Task<IBonboardingTourSession> StartAsync(string tourId, BonboardingStartOptions? options = null, CancellationToken ct = default);
Task<IBonboardingTourSession?> StartOnceAsync(BonboardingTourDefinition tour, BonboardingStartOptions? options = null, CancellationToken ct = default);
Task<IBonboardingTourSession> RestartAsync(BonboardingTourDefinition tour, CancellationToken ct = default);
Task StopAllAsync(BonboardingTourEndReason reason = BonboardingTourEndReason.Cancelled, CancellationToken ct = default);

IBonboardingTourSession? FindSession(string tourId);
Task<bool> IsCompletedAsync(string tourId, CancellationToken ct = default);
Task<bool> IsClosedAsync(string tourId, CancellationToken ct = default);
Task<BonboardingRecord?> GetRecordAsync(string tourId, CancellationToken ct = default);
Task ResetAsync(string tourId, CancellationToken ct = default);
Task ResetAllAsync(CancellationToken ct = default);

Task<bool> SignalAsync(string signal, CancellationToken ct = default);

Task SetChecklistItemAsync(string checklistId, string itemId, bool done = true, CancellationToken ct = default);
Task<IReadOnlyCollection<string>> GetCompletedChecklistItemsAsync(string checklistId, CancellationToken ct = default);
Task DismissChecklistAsync(string checklistId, CancellationToken ct = default);
Task<bool> IsChecklistDismissedAsync(string checklistId, CancellationToken ct = default);
Task ResetChecklistAsync(string checklistId, CancellationToken ct = default);
```

`StartAsync` throws `BonboardingException` when the tour has no steps, when its definition has errors
(see [Checking a definition](#checking-a-definition)), when the id is not registered, when `CanStart`
declines, or when `WhenBusy = Skip` and another tour runs. `StartOnceAsync` returns `null` instead of
throwing for the last three. Either throws `OperationCanceledException` when its token is cancelled before the tour
got going, and leaves nothing behind.

A decorator of your own (a logging or telemetry wrapper) forwards every member to the service underneath.
`SignalAsync` and the checklist members have no default implementation on purpose: the only possible default does
nothing, and a wrapper that inherited it would silently drop signals and checklist progress. Checklists, tour lists
and beacons keep working behind a wrapper that forwards.

Where the services are not registered - an app that [lazy loads](#lazy-loading-on-webassembly) Bonboarding on
WebAssembly - `IServiceProvider.GetBonboardingService()` returns the one the components use; where they are, it
returns the registered `IBonboardingService`, which the components use too, so code that has to work both ways can
always call it.

#### `BonboardingStartOptions`

| Member | Default | Description |
|---|---|---|
| `StartAtStepId` | `null` | Begin at this step. |
| `StartAtIndex` | `null` | Begin at this index. Ignored when `StartAtStepId` is set. |
| `Resume` | `true` | Continue from persisted progress. |
| `Force` | `false` | Start even when closed or snoozed, and bypass `CanStart`. |
| `State` | `null` | Seed values for `IBonboardingTourSession.State`. |
| `StopOthers` | `true` | Deal with any other running tour first (see `WhenBusy`). |
| `WhenBusy` | `Replace` | `Replace` the running tour, `Queue` behind it, or `Skip` this start. |

---

### Tour and step model

#### `BonboardingTourDefinition`

Inherits every member of [`BonboardingStepOptions`](#bonboardingstepoptions) as defaults for its steps.

| Member | Type | Description |
|---|---|---|
| `Id` | `string` | Required. |
| `Title`, `Description` | `string?` | |
| `Steps` | `List<BonboardingStepDefinition>` | |
| `Labels` | `BonboardingLabels?` | Replaces the global set. |
| `Persist` | `bool?` | |
| `Version` | `int` | Default `1`. |
| `MigrateRecord` | `Func<BonboardingRecord, BonboardingRecord?>?` | Carry progress over a version bump. |
| `Priority` | `int` | Order among queued starts. Default `0`. |
| `Template` | `RenderFragment<BonboardingStepRenderContext>?` | |
| `CanStart` | `Func<BonboardingTourContext, ValueTask<bool>>?` | |
| `OnStarted` | `Func<BonboardingTourContext, ValueTask>?` | |
| `OnStepChanged` | `Func<BonboardingStepChangedEventArgs, ValueTask>?` | |
| `OnEnded` | `Func<BonboardingTourEndedEventArgs, ValueTask>?` | |
| `Data` | `object?` | |

Methods: `FindStep(string)`, `IndexOf(string)`, `Validate()`. Constant: `EndStepId` (`"$end"`).

#### `BonboardingStepDefinition`

Inherits every member of [`BonboardingStepOptions`](#bonboardingstepoptions). Same members as the `BonboardingStep`
parameters above, plus `Body` (the component's `ChildContent`) and `Metadata`.

`Id` is auto-generated when not set. Set it explicitly for anything that persists or branches.

#### `BonboardingStepAction`

| Member | Type | Default |
|---|---|---|
| `Id` | `string?` | Surfaced as `data-bonboarding-action`. |
| `Label` | `string` | Required. |
| `Style` | `BonboardingStepActionStyle` | `Secondary` |
| `OnClick` | `Func<BonboardingStepContext, ValueTask>?` | Awaited before `Effect`. |
| `Effect` | `BonboardingStepActionEffect` | `None` |
| `TargetStepId` | `string?` | For `GoTo`. |
| `SnoozeFor` | `TimeSpan?` | For `Snooze`; a day when unset. |
| `IsDisabled` | `Func<BonboardingStepContext, bool>?` | |
| `CssClass` | `string?` | |
| `AutoFocus` | `bool` | Receives focus instead of the forward button. |

`BonboardingStepAction.RemindMeLater(label, duration)` builds a "remind me later" button. While an action's
handler runs, its button is `aria-busy` and a second press is ignored, so a handler that creates something runs
once per request rather than once per click.

#### `BonboardingAdvanceTrigger`

| Member | Default | Description |
|---|---|---|
| `EventName` | `"click"` | Any DOM event. |
| `Selector` | `null` | Element to listen on; null means the step's target. |
| `MatchSelector` | `null` | Only advance when the event came from a matching element. |
| `DelayMs` | `150` | Settle time before advancing. |

`BonboardingAdvanceTrigger.TargetClick` returns a new instance of the defaults on each access.

#### `BonboardingStepContext` / `BonboardingTourContext`

`BonboardingStepContext`: `Session`, `Tour`, `Step`, `Index`, `Count`, `Services`, `CancellationToken`, `State`,
and the settable `ValidationMessage`.

`BonboardingTourContext`: `Tour`, `Services`, `Session?`.

#### `BonboardingStepRenderContext`

Passed to every template. See [headless rendering](#the-render-context).

---

### Options

#### `BonboardingStepOptions`

The inheritable knobs, shared by `BonboardingOptions`, `BonboardingTourDefinition` and `BonboardingStepDefinition`. Every
member is nullable; `null` means "inherit". Resolution order is step, tour, global, then
`BonboardingDefaults`.

| Member | Type | Default |
|---|---|---|
| `Placement` | `Placement?` | `Auto` |
| `PlacementFallbacks` | `IReadOnlyList<BonboardingPlacement>?` | derived |
| `Offset` | `double?` | `12` |
| `ViewportPadding` | `double?` | `16` |
| `ArrowSize` | `double?` | `10` |
| `ShowSpotlight` | `bool?` | `true` |
| `SpotlightPadding` | `double?` | `8` |
| `SpotlightRadius` | `double?` | `10` |
| `SpotlightShape` | `SpotlightShape?` | `Rounded` |
| `ShowOverlay` | `bool?` | `true` |
| `OverlayOpacity` | `double?` | `0.55` |
| `Interaction` | `BonboardingInteractionMode?` | `Blocked` |
| `CloseOnOverlayClick` | `bool?` | `false` |
| `CloseOnEscape` | `bool?` | `true` |
| `Scroll` | `BonboardingScrollMode?` | `Smooth` |
| `ScrollPadding` | `double?` | `24` |
| `MissingTarget` | `BonboardingMissingTargetBehavior?` | `Wait` |
| `WaitTimeout` | `TimeSpan?` | 10 seconds |
| `WaitTimeoutBehavior` | `BonboardingMissingTargetBehavior?` | `Skip` |
| `Progress` | `BonboardingProgressStyle?` | `Dots` |
| `ShowNext`, `ShowPrevious`, `ShowSkip`, `ShowClose` | `bool?` | `true` |
| `AnimationDuration` | `TimeSpan?` | 220 ms |
| `RespectReducedMotion` | `bool?` | `true` |
| `Theme` | `BonboardingTheme?` | `System` |
| `Direction` | `BonboardingDirection?` | `Auto` |
| `ZIndex` | `int?` | `9000` |
| `PopoverClass` | `string?` | accumulates across levels |
| `KeyboardNavigation` | `bool?` | `true` |
| `TrapFocus` | `bool?` | `true` |
| `RestoreFocus` | `bool?` | `true` |
| `AutoFocus` | `bool?` | `true` |

#### `BonboardingOptions`

`BonboardingStepOptions` plus:

| Member | Type | Default |
|---|---|---|
| `Labels` | `BonboardingLabels` | English |
| `Persist` | `bool` | `true` |
| `StorageKeyPrefix` | `string` | `"bit-bonboarding:"` |
| `StorageLocation` | `BonboardingStorageLocation` | `Local` (`Session` keeps progress for the visit) |
| `StorageScope` | `Func<IServiceProvider, CancellationToken, ValueTask<string?>>?` | `null` (whose progress it is; see [Several people on one device](#several-people-on-one-device)) |
| `AutoNavigateToStepRoute` | `bool` | `true` |
| `EnableJsDiagnostics` | `bool` | `false` |

#### `BonboardingEffectiveStepOptions`

The resolved, non-nullable snapshot for one step, exposed as `IBonboardingTourSession.CurrentOptions` and
`BonboardingStepRenderContext.Options`. Adds `ReducedMotion` (whether the preference is being honoured) and
`RightToLeft`.

#### `BonboardingDefaults`

The terminal fallback constants, so you can reference them rather than repeating literals.

---

### Targeting

#### `BonboardingStepTarget`

```csharp
BonboardingStepTarget.None
BonboardingStepTarget.Css(string selector)
BonboardingStepTarget.Anchor(string name)                     // [data-bonboarding-anchor="name"]
BonboardingStepTarget.Element(ElementReference element)
BonboardingStepTarget.Dynamic(Func<BonboardingStepContext, BonboardingStepTarget?>)
BonboardingStepTarget.Dynamic(Func<BonboardingStepContext, ValueTask<BonboardingStepTarget?>>)
```

Implicit conversions from `string` (a CSS selector) and `ElementReference`. Members: `IsNone`,
`IsDynamic`, `ResolveAsync(BonboardingStepContext)`. Equality is structural. Constant:
`BonboardingStepTarget.AnchorAttribute` (`"data-bonboarding-anchor"`).

#### `BitBonboarding.Anchor(string)`

Returns splattable attributes: `<button @attributes="BitBonboarding.Anchor("create")">`.

---

### Sessions

#### `IBonboardingTourSession`

State: `Id`, `Tour`, `Status`, `CurrentStep`, `CurrentIndex`, `DisplayPosition`, `DisplayCount`,
`CurrentOptions`, `TargetRect`, `Placement`, `HasTarget`, `IsWaiting`, `WaitReason`, `IsActive`, `IsFirstStep`,
`IsLastStep`, `History`, `State`, `EndReason`, `Error`, `ValidationMessage`.

Event: `Changed` (`BonboardingTourSessionChangedEventArgs.Change` is `Step`, `Geometry` and/or `Ended`).

Commands: `StartAsync`, `NextAsync`, `PreviousAsync`, `GoToAsync(string)`, `GoToAsync(int)`,
`SkipAsync`, `CompleteAsync`, `DismissAsync`, `CancelAsync`, `SnoozeAsync(TimeSpan)`, `PauseAsync`,
`ResumeAsync`, `RefreshAsync`, `InvokeActionAsync`.

Commands are serialised: concurrent calls queue rather than interleaving, and a command issued from
inside a hook runs inline rather than deadlocking on the lock its caller holds.

---

### Events

#### `BonboardingEvent`

`Kind`, `Session`, `Tour`, `Step`, `StepIndex`, `Duration`, `Action`, `Reason`, `Error`, `Timestamp`.

#### `IBonboardingObserver`

```csharp
ValueTask OnEventAsync(BonboardingEvent onboardingEvent, CancellationToken ct = default);
```

Register with `AddBitBonboardingObserver<T>()`. A throwing observer is logged and ignored.

#### `BonboardingStepChangedEventArgs`

`Session`, `Step`, `Index`, `PreviousStep`, `Reason`.

#### `BonboardingTourEndedEventArgs`

`Session`, `Reason`, `LastStep`, `LastIndex`, `Error`, `IsSuccess`.

#### `BonboardingException`

`TourId` and `StepId` when known.

---

### Persistence

#### `IBonboardingStore`

```csharp
ValueTask<BonboardingRecord?> GetAsync(string tourId, CancellationToken ct = default);
ValueTask SetAsync(BonboardingRecord record, CancellationToken ct = default);
ValueTask RemoveAsync(string tourId, CancellationToken ct = default);
ValueTask ClearAsync(CancellationToken ct = default);
```

Implementations: `BonboardingBrowserStore` (default) and `BonboardingInMemoryStore`.

#### `BonboardingRecord`

`TourId`, `Version`, `CurrentStepId`, `CurrentIndex`, `Completed`, `Dismissed`, `VisitedStepIds`,
`UpdatedAt`, `CompletedAt`, `SnoozedUntil`, `Data`, `IsClosed`, `IsSnoozedAt(DateTimeOffset)`.

A checklist's progress is a record too, under `checklist:{id}`: `Data` maps each ticked item to when it was
ticked, and `Dismissed` says the checklist was dismissed.

---

### Localization

#### `IBonboardingLocalizer`

```csharp
BonboardingLabels GetLabels(BonboardingTourDefinition? tour);
string? Localize(string? text) => text;    // default: unchanged
```

#### `BonboardingLabels`

`Next`, `Previous`, `Skip`, `Done`, `Close`, `DialogLabel`, `ProgressFormat`,
`StepAnnouncementFormat`, `WaitingAnnouncement`, `ProgressLabel`, and for the on-page components
`BeaconLabel`, `HintDone`, `ChecklistProgressFormat` (`{0}` done, `{1}` total), `ChecklistDismiss`,
`ChecklistItemDone`, `ChecklistItemTodo`, `TourNew`, `TourInProgress`, `TourCompleted`, `TourStart`,
`TourResume`, `TourReplay`. Static `Default`, instance `Clone()`.

---

### Layout

#### `BonboardingPlacementEngine`

```csharp
static BonboardingPlacementResult Place(in BonboardingPlacementRequest request);
```

Pure and deterministic. Usable on its own.

#### `BonboardingPlacementRequest`

`Target`, `Popover`, `Viewport`, `Preferred`, `Fallbacks`, `Offset`, `ViewportPadding`, `ArrowSize`,
`CornerRadius`, `RightToLeft`, `HasTarget`.

#### `BonboardingPlacementResult`

`Popover` (the final rectangle), `Side`, `Align`, `ShowArrow`, `ArrowOffset`, `UsedPreferred`,
`Overflow`, `SideName`.

#### `BonboardingRouteMatcher`

```csharp
static bool Matches(string? pattern, string? relativePath);
static string Normalize(string? path);
```

---

### Geometry

#### `BonboardingRect`

`X`, `Y`, `Width`, `Height`, `Left`, `Top`, `Right`, `Bottom`, `CenterX`, `CenterY`, `Area`,
`IsEmpty`. Methods: `FromEdges`, `Inflate`, `Offset`, `Contains`, `IntersectsWith`, `Intersect`,
`Union`, `ApproximatelyEquals`, `Round`.

#### `BonboardingSize`

`Width`, `Height`, `IsEmpty`.

---

### Enums

| Enum | Values |
|---|---|
| `Placement` | `Auto`, `Top`, `TopStart`, `TopEnd`, `Bottom`, `BottomStart`, `BottomEnd`, `Left`, `LeftStart`, `LeftEnd`, `Right`, `RightStart`, `RightEnd`, `Center`, `InlineStart`, `InlineStartTop`, `InlineStartBottom`, `InlineEnd`, `InlineEndTop`, `InlineEndBottom` |
| `BonboardingPlacementSide` | `Top`, `Right`, `Bottom`, `Left`, `Center` |
| `BonboardingPlacementAlign` | `Start`, `Center`, `End` |
| `BonboardingMissingTargetBehavior` | `Wait`, `Skip`, `Center`, `Fail` |
| `BonboardingTourStatus` | `Idle`, `Running`, `Waiting`, `Paused`, `Completed`, `Skipped`, `Dismissed`, `Cancelled`, `Failed`, `Snoozed` |
| `BonboardingTourEndReason` | `Completed`, `Skipped`, `Dismissed`, `Cancelled`, `Failed`, `Snoozed` |
| `BonboardingWaitReason` | `None`, `Target`, `Route` |
| `BonboardingBusyBehavior` | `Replace`, `Queue`, `Skip` |
| `BonboardingValidationSeverity` | `Warning`, `Error` |
| `BonboardingBeaconPosition` | `TopEnd`, `TopStart`, `Top`, `End`, `Center`, `Start`, `BottomEnd`, `BottomStart`, `Bottom` |
| `BonboardingTourProgress` | `New`, `InProgress`, `Completed` |
| `BonboardingStepChangeReason` | `Start`, `Next`, `Previous`, `Jump`, `Resume`, `Retarget` |
| `BonboardingInteractionMode` | `Blocked`, `TargetOnly`, `Free` |
| `BonboardingScrollMode` | `Auto`, `Smooth`, `Instant`, `None` |
| `SpotlightShape` | `Rounded`, `Rectangle`, `Circle`, `None` |
| `BonboardingProgressStyle` | `None`, `Text`, `Dots`, `Bar` |
| `BonboardingTheme` | `System`, `Light`, `Dark` |
| `BonboardingDirection` | `Auto`, `LeftToRight`, `RightToLeft` |
| `BonboardingAutoStart` | `None`, `Once`, `Always` |
| `BonboardingStepActionStyle` | `Primary`, `Secondary`, `Ghost`, `Danger` |
| `BonboardingStepActionEffect` | `None`, `Next`, `Previous`, `GoTo`, `Complete`, `Skip`, `Dismiss`, `Snooze` |
| `BonboardingEventKind` | see [events](#the-event-stream) |
| `BonboardingTourSessionChange` | `None`, `Step`, `Geometry`, `Ended` (flags) |

---

### Registration

```csharp
IServiceCollection AddBitBonboardingServices(Action<BonboardingOptions>? configure = null);
IServiceCollection AddBitBonboardingStore<TStore>()       where TStore : class, IBonboardingStore;
IServiceCollection AddBitBonboardingInMemoryStore();
IServiceCollection AddBitBonboardingLocalizer<TLocalizer>() where TLocalizer : class, IBonboardingLocalizer;
IServiceCollection AddBitBonboardingObserver<TObserver>()  where TObserver : class, IBonboardingObserver;

IBonboardingService GetBonboardingService(this IServiceProvider services); // registered, or lazy loaded - see lazy loading
```

All services are scoped.

---

### Browser layer

`IBonboardingInterop` is the single seam between the engine and the DOM. It is public so that it can
be substituted in tests, letting the whole engine run without a browser; applications do not normally
touch it.

```csharp
bool IsAvailable { get; }
ValueTask<BonboardingBrowserEnvironment> InitializeAsync(CancellationToken ct = default);
ValueTask AttachAsync(string sessionId, IBonboardingInteropCallbacks callbacks, CancellationToken ct = default);
ValueTask DetachAsync(string sessionId, CancellationToken ct = default);
ValueTask<BonboardingStepGeometry> ActivateStepAsync(BonboardingStepActivation activation, CancellationToken ct = default);
ValueTask DeactivateStepAsync(string sessionId, CancellationToken ct = default);
ValueTask<BonboardingStepGeometry> MeasureAsync(string sessionId, CancellationToken ct = default);
ValueTask SetPopoverAsync(string sessionId, ElementReference? popover, bool trapFocus, CancellationToken ct = default);
ValueTask CaptureFocusAsync(string sessionId, CancellationToken ct = default);
ValueTask RestoreFocusAsync(string sessionId, CancellationToken ct = default);
ValueTask FocusPopoverAsync(string sessionId, CancellationToken ct = default);
ValueTask<string?> GetStorageAsync(string key, CancellationToken ct = default);
ValueTask SetStorageAsync(string key, string value, CancellationToken ct = default);
ValueTask RemoveStorageAsync(string key, CancellationToken ct = default);
ValueTask<IReadOnlyList<string>> ListStorageKeysAsync(string prefix, CancellationToken ct = default);

// With default implementations, so a test double written for an earlier version still compiles:
ValueTask AttachBeaconAsync(BonboardingBeaconActivation activation, ElementReference beacon, CancellationToken ct = default);
ValueTask DetachBeaconAsync(string beaconId, CancellationToken ct = default);
ValueTask FocusTargetAsync(BonboardingTargetDescriptor target, CancellationToken ct = default);
```

Supporting types: `BonboardingTargetDescriptor`, `BonboardingTargetKind`, `BonboardingAdvanceTriggerDescriptor`, `BonboardingStepActivation`,
`BonboardingStepGeometry` (including `InvalidSelector`), `BonboardingBeaconActivation`, `BonboardingBrowserEnvironment`,
`IBonboardingInteropCallbacks`.
