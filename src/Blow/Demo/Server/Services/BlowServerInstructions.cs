using System.Reflection;

namespace Bit.Blow.Demo.Server.Services;

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
public static class BlowServerInstructions
{
    /// <summary>
    /// The build the answers come from, so the text names it rather than claiming to be timeless.
    /// Read off the library assembly rather than written down here: the package version is stamped
    /// into it by Bit.Build.props, so there is no second copy for a release to forget. The
    /// informational version's build metadata - everything after '+', usually the commit hash - is
    /// dropped: it identifies nothing a caller could act on.
    /// </summary>
    public static readonly string BlowVersion = (
        typeof(BlowCanvas).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(BlowCanvas).Assembly.GetName().Version?.ToString()
        ?? "unknown").Split('+')[0];

    public static readonly string Text = $"""
        Bit.Blow is a node-based editor and diagram library for Blazor, a Blazor counterpart to React Flow: `BlowCanvas`
        with draggable, resizable and connectable nodes, edges, handles, sub-flows, auto-layout, alignment guides,
        undo/redo, copy/paste, a minimap, controls and full keyboard accessibility. Interaction, layout and geometry
        run in C#; the browser layer only measures the DOM.

        Every answer here is read out of Bit.Blow {BlowVersion} as it is loaded in this server - its compiled XML
        documentation, its README, its stylesheet, the canvas itself and the demo's source files. Where what you
        remember (of React Flow too) and what these tools say disagree, the tools are describing the build in front of you.

        Working rules:

        - Start with `SearchBlow` unless you already know the section or type name you want. It covers the guide, every
          public member, the setup guides, the theme, the stylesheet and the demo sources and examples at once, and
          every hit names the exact follow-up call that returns its full text.
        - Every reference tool takes the key of one thing and answers with that thing; leave the key out and it answers
          with the index of what there is - guide sections, public types, theme properties, source files.
        - Do not write a Blow type or member from memory. There is one package and one namespace, `Bit.Blow`, and every
          public type is prefixed `Blow` (`BlowCanvas`, `BlowNode`, `BlowEdge`, `BlowHandle`, `BlowLayout`...);
          `GetBlowApi` gives the real names, types and default values.
        - Call `GetBlowSetupGuide` before writing wiring. There are no services to register, but the stylesheet link is
          required, the canvas needs a container with a height, and it must render interactively.
        - The canvas edits the `List<BlowNode>`/`List<BlowEdge>` you hand it in place and reports through
          `NodesChanged`/`EdgesChanged`; restyle it through the `--bit-blow-*` properties `GetBlowTheme` lists.
        - To check a flow - duplicate ids, edges to missing nodes or handles, sub-flows - or to see what a layout does,
          call `InspectBlowFlow`: it renders the flow through the real canvas.

        Every tool on this server is read-only and cheap: none of them changes anything, anywhere, so there is no reason
        to ration them or to ask before calling one.
        """;
}
