using System.Reflection;

namespace Bit.Brouter.Demo.Server.Services;

/// <summary>
/// The text the server returns as <c>instructions</c> from the <c>initialize</c> handshake.
/// <para>
/// A client puts this in front of the model once, before any tool has been called, which makes it
/// the only place where the server can say something the model will read whether or not it decides
/// to use the tools at all. That is worth more than another tool: an agent that never calls
/// <c>GetBrouterApiDetails</c> because it is confident it remembers the parameter is the failure
/// this server exists to prevent, and no tool description reaches an agent that is not looking.
/// So it stays short - every word is permanently in someone's context window - and spends itself
/// on the working rules rather than on describing the library, which the tools do far better.
/// </para>
/// </summary>
public static class BrouterServerInstructions
{
    /// <summary>
    /// The build the answers come from, so the text names it rather than claiming to be timeless.
    /// The informational version's build metadata - everything after '+', usually the commit hash -
    /// is dropped: it identifies nothing a caller could act on, and it would otherwise be repeated
    /// into every context window this text reaches.
    /// </summary>
    public static readonly string BrouterVersion = (
        typeof(Bit.Brouter.BrouterLink).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(Bit.Brouter.BrouterLink).Assembly.GetName().Version?.ToString()
        ?? "unknown").Split('+')[0];

    public static readonly string Text = $"""
        Bit.Brouter is a routing library for Blazor: a drop-in replacement for the built-in <Router> that adds
        declared and nested routes, guards, data loaders, keep-alive components and view transitions.

        Every answer here is read out of Bit.Brouter {BrouterVersion} as it is loaded in this server - its compiled
        XML documentation, its README, this documentation site's own pages and the demo's source files. Where what
        you remember and what these tools say disagree, the tools are describing the build in front of you.

        Working rules:

        - Start with `SearchBrouter` unless you already know the section, slug or type name you want. It covers the
          guide, the docs pages, every public member, the constraints and the demo sources at once, and every hit
          names the exact follow-up call that returns its full text.
        - Do not write a Brouter parameter, option or method from memory. `GetBrouterApiDetails` gives the real
          names, types and default values; a parameter borrowed from another router library is the single most
          common way code against this one goes wrong, and it compiles right up until it does not.
        - Check a route template with `InspectBrouterRouteTemplate` - or a whole table with
          `AnalyzeBrouterRouteTable` - before shipping it. Both parse with the router's own parser, so their
          verdict is the router's verdict rather than an opinion about it.
        - Call `GetBrouterSetupGuide` before writing any wiring. Which DI container registers the services depends
          on the Blazor render mode, and registering in only one of a Web App's two containers fails during
          prerendering rather than at compile time.
        - Prefer the framework's own mechanism to a hand-rolled one: a route `Guard`/`LeaveGuard` over checks in
          `OnInitialized`, a route `Loader` over fetching inside the component, `KeepAlive` over caching state
          yourself, `<BrouterOutlet />` over conditional rendering.

        Every tool on this server is read-only and cheap: none of them changes anything, anywhere, so there is no
        reason to ration them or to ask before calling one.
        """;
}
