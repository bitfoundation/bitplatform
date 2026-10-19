using System.Reflection;

namespace Bit.Balette.Demo.Server.Services;

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
public static class BaletteServerInstructions
{
    /// <summary>
    /// The build the answers come from, so the text names it rather than claiming to be timeless.
    /// Read off the library assembly rather than written down here: the package version is stamped
    /// into it by Bit.Build.props, so there is no second copy for a release to forget. The
    /// informational version's build metadata - everything after '+', usually the commit hash - is
    /// dropped: it identifies nothing a caller could act on.
    /// </summary>
    public static readonly string BaletteVersion = (
        typeof(BaletteOptions).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(BaletteOptions).Assembly.GetName().Version?.ToString()
        ?? "unknown").Split('+')[0];

    public static readonly string Text = $"""
        Bit.Balette is a keyboard-first command palette for Blazor: the Ctrl+K / Cmd+K box with fzf-style fuzzy search,
        nested pages, prefix modes, async sources, recents and frecency, global shortcuts and sequences, multi-select,
        prompts and theming. Matching, ranking and every decision are C#; the browser only owns the dialog, focus and keys.

        Every answer here is read out of Bit.Balette {BaletteVersion} as it is loaded in this server - its compiled XML
        documentation, its README, its stylesheet and component markup, its matcher and ranker, and the demo's source files.
        Where what you remember and what these tools say disagree, the tools are describing the build in front of you.

        Working rules:

        - Start with `SearchBalette` unless you already know the section or type name you want. It covers the guide, every
          public member, the setup guides, the stylesheet's variables and classes and the demo sources at once, and every
          hit names the exact follow-up call that returns its full text.
        - Every reference tool takes the key of one thing and answers with that thing; leave the key out and it answers
          with the index of what there is - guide sections, public types, source files.
        - Do not write a Balette type or member from memory. There is one package and one namespace, `Bit.Balette`, and
          every public type is prefixed `Balette` (`Balette` is the component, `BaletteCommand`, `BaletteCmd`,
          `IBaletteService`, `BaletteOptions`...); `GetBaletteApi` gives the real names, types and default values.
        - Call `GetBaletteSetupGuide` before writing wiring: the services go in every DI container that renders the
          palette, the host page links `_content/Bit.Balette/bit-balette.css` itself (it is not injected), and the one
          `<Balette />` sits in an interactively rendered layout - each fails silently when missing.
        - Commands are immutable: give them an explicit `Id` when the title can change, and register a new list (or call
          `Update`) to change one. Dispose what a page registers.
        - To see why a command ranks where it does, or does not show up at all, call `InspectBaletteSearch` - it runs the
          library's own matcher and ranker. Restyle through the `--bit-balette-*` variables (`GetBaletteThemeVariables`).

        Every tool on this server is read-only and cheap: none of them changes anything, anywhere, so there is no reason
        to ration them or to ask before calling one.
        """;
}
