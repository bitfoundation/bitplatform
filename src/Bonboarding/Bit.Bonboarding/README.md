# Bit.Bonboarding

A Blazor-native onboarding and user-guidance library: product tours, spotlights, tooltips,
contextual hints and full application walkthroughs. There is no JavaScript tour engine underneath -
the browser layer is a small module that measures elements, watches them for movement, scrolls and
routes keyboard events. Every decision about what to show, when, and where is made in C#.

## Quick start

```csharp
// Program.cs (in both projects of a Blazor Web App)
builder.Services.AddBitBonboardingServices();
```

```html
<!-- App.razor / index.html -->
<link rel="stylesheet" href="_content/Bit.Bonboarding/bit-bonboarding.css" />
```

```razor
@* MainLayout.razor: one host renders every tour the application starts *@
<BonboardingRoot />
```

```razor
@using Bit.Bonboarding

<BonboardingTour Id="welcome" AutoStart="BonboardingAutoStart.Once">
    <BonboardingStep Anchor="new-project"
                     Title="Create a project"
                     Description="Everything starts here." />

    <BonboardingStep Anchor="search"
                     Title="Find anything"
                     Placement="BonboardingPlacement.Bottom" />
</BonboardingTour>

<button data-bonboarding-anchor="new-project">New project</button>
<input data-bonboarding-anchor="search" />
```

Tours can also be built as data (`BonboardingTourDefinition` / `BonboardingStepDefinition`) and
started from anywhere through `IBonboardingService`:

```csharp
await BonboardingService.StartOnceAsync(tour); // skipped if the user already finished or dismissed it
```

## Features

- Multi-step tours with progress indicators, keyboard navigation and resumable, versioned state.
- Collision-aware positioning computed in C#: shift before flip, arrow clamping, viewport padding.
- Targets that render late or never: wait (mutation observer, no polling), skip, centre or fail.
- Interactive steps that advance when the user actually uses the highlighted control.
- Conditional and branching flows, where Back follows the path the user took.
- Route-aware tours that span pages.
- Accessibility built in: dialog semantics, focus trap and restoration, live-region announcements,
  reduced motion and forced colors.
- Theming through CSS variables, per-step templates, or a fully headless popover.
- Analytics-grade events and pluggable persistence and localization.
- Blazor Server, WebAssembly and Web App (including prerendering) on .NET 8, 9 and 10.

## Documentation

The full guide - targeting, branching, multi-page tours, persistence, theming, headless rendering,
localization and the API reference - is in the
[Bonboarding README](https://github.com/bitfoundation/bitplatform/blob/develop/src/Bonboarding/README.md).
