using System.Reflection;

namespace Bit.Bonboarding.Demo.Server.Services;

/// <summary>
/// The text the server returns as <c>instructions</c> from the <c>initialize</c> handshake.
/// <para>
/// A client puts this in front of the model once, before any tool has been called, which makes it
/// the only place where the server can say something the model will read whether or not it decides
/// to use the tools at all. So it stays short - every word is permanently in someone's context
/// window - and spends itself on the working rules rather than on describing the library, which the
/// tools do far better.
/// </para>
/// </summary>
public static class BonboardingServerInstructions
{
    /// <summary>
    /// The build the answers come from, so the text names it rather than claiming to be timeless.
    /// The informational version's build metadata - everything after '+', usually the commit hash -
    /// is dropped: it identifies nothing a caller could act on.
    /// </summary>
    public static readonly string BonboardingVersion = (
        typeof(BonboardingRoot).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(BonboardingRoot).Assembly.GetName().Version?.ToString()
        ?? "unknown").Split('+')[0];

    public static readonly string Text = $"""
        Bit.Bonboarding is an onboarding and product-tour library for Blazor: multi-step tours, spotlights, contextual
        hints, branching and multi-page flows, persistence and built-in accessibility, with every decision made in C#.

        Every answer here is read out of Bit.Bonboarding {BonboardingVersion} as it is loaded in this server - its compiled
        XML documentation, its README, its stylesheet, its placement engine and the demo's source files. Where what you
        remember and what these tools say disagree, the tools are describing the build in front of you.

        Working rules:

        - Start with `SearchBonboarding` unless you already know the section or type name you want. It covers the guide,
          every public member, the theme variables and the demo sources at once, and every hit names the exact
          follow-up call that returns its full text.
        - Every reference tool takes the key of one thing and answers with that thing; leave the key out and it answers
          with the index of what there is - guide sections, public types, source files.
        - Do not write a Bonboarding type, parameter or option from memory. Every public type is prefixed `Bonboarding`
          (`BonboardingTour`, `BonboardingStepDefinition`, `BonboardingPlacement`...), and `GetBonboardingApi` gives the
          real names, types and default values.
        - Call `GetBonboardingSetupGuide` before writing any wiring. A tour needs the services in every DI container that
          renders it, the stylesheet, and exactly one `<BonboardingRoot />` - each of them fails silently when missing.
        - Mark targets with `data-bonboarding-anchor="name"` and point steps at them with `Anchor="name"`: anchors
          survive re-renders and work for elements that do not exist yet, unlike captured element references.
        - Restyle through the `--bit-bonboarding-*` variables (`GetBonboardingThemeVariables`), not by overriding the
          library's selectors. To know where a popover will land, ask `InspectBonboardingPlacement` - it runs the
          library's own placement engine.

        Every tool on this server is read-only and cheap: none of them changes anything, anywhere, so there is no reason
        to ration them or to ask before calling one.
        """;
}
