using System.Reflection;

namespace Bit.Blex.Demo.Server.Services;

/// <summary>
/// The text the server returns as <c>instructions</c> from the <c>initialize</c> handshake.
/// <para>
/// A client puts this in front of the model once, before any tool has been called, which makes it the
/// only place where the server can say something the model will read whether or not it decides to use
/// the tools at all. An agent that never calls <c>GetBlexApi</c> because it is confident it remembers
/// Fluxor's API is the failure this server exists to prevent. So it stays short - every word is
/// permanently in someone's context window - and spends itself on the working rules.
/// </para>
/// </summary>
public static class BlexServerInstructions
{
    /// <summary>
    /// The build the answers come from, without the build metadata after '+' (usually the commit hash),
    /// which identifies nothing a caller could act on.
    /// </summary>
    public static readonly string BlexVersion = (
        typeof(BlexStoreBase).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(BlexStoreBase).Assembly.GetName().Version?.ToString()
        ?? "unknown").Split('+')[0];

    public static readonly string Text = $"""
        Bit.Blex is reactive state management for Blazor and .NET MAUI. A source generator (the Bit.Blex.Generators
        package) turns a `partial class` marked [BlexStore] - state in [BlexState] fields, actions in [BlexAction] OnXxx
        methods, derived values in [BlexComputed] ComputeXxx methods, async work in [BlexEffect] methods - into an
        observable store with middleware, persistence, undo/redo and Redux DevTools time-travel.

        Every answer here is read out of Bit.Blex {BlexVersion} as it is loaded in this server - its compiled XML
        documentation, its README, this documentation site's own pages, the generator's own diagnostics and the demo's
        source files. Where what you remember and what these tools say disagree, the tools describe the build in front of you.

        Working rules:

        - Start with `SearchBlex` unless you already know the section, slug or type name you want. Every hit names the exact
          follow-up call that returns its full text.
        - Every reference tool takes the key of one thing and answers with that thing; leave the key out and it answers with
          the index of what there is - guide sections, docs pages, public types, diagnostics, source files.
        - Blex is not Fluxor or Redux: there are no action classes, reducers or features to write. Do not write a Blex
          attribute, option or method from memory - `GetBlexApi` gives the real names, types and defaults.
        - Check every store you write with `InspectBlexStore` before building on it. It runs the real generator and returns its
          diagnostics plus the exact members it generates (`_count` becomes `Count`, `OnIncrement` becomes `Increment`), so you
          call the generated names rather than guessed ones. `GetBlexDiagnostics` explains any BLEX error it reports.
        - Call `GetBlexSetupGuide` before writing any wiring: which DI container registers the stores, and with which lifetime,
          depends on the hosting model, and a store registered in only one of a Web App's two containers fails during prerendering.
        - Prefer the library's own mechanism to a hand-rolled one: [BlexEffect] over manual loading/error flags, [BlexComputed]
          over recomputing in the component, a selector subscription over re-rendering on every change, BlexEntityAdapter over a
          hand-maintained dictionary, middleware over wrapping every action.

        Every tool on this server is read-only and cheap: none of them changes anything, anywhere, so there is no reason to
        ration them or to ask before calling one.
        """;
}
