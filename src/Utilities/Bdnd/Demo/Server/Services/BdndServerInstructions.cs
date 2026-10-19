using System.Reflection;

namespace Bit.Bdnd.Demo.Server.Services;

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
public static class BdndServerInstructions
{
    /// <summary>
    /// The build the answers come from, so the text names it rather than claiming to be timeless.
    /// Read off the library assembly rather than written down here: the package version is stamped
    /// into it by Bit.Build.props, so there is no second copy for a release to forget. The
    /// informational version's build metadata - everything after '+', usually the commit hash - is
    /// dropped: it identifies nothing a caller could act on.
    /// </summary>
    public static readonly string BdndVersion = (
        typeof(BdndProvider).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(BdndProvider).Assembly.GetName().Version?.ToString()
        ?? "unknown").Split('+')[0];

    public static readonly string Text = $"""
        Bit.Bdnd is a declarative drag-and-drop library for Blazor inspired by React DnD: `BdndDraggable` (useDrag),
        `BdndDropTarget` (useDrop), `BdndDragLayer` (useDragLayer) and `BdndDragHandle` inside a `BdndProvider`, on the
        browser's HTML5 drag-and-drop API - typed items matched to targets by type strings, drop results, custom drag
        previews, drag handles, nested sources and targets, and native drops of files, text, links and HTML.

        Every answer here is read out of Bit.Bdnd {BdndVersion} as it is loaded in this server - its compiled XML
        documentation, its README, the components themselves and the demo's source files. Where what you remember (of
        React DnD too) and what these tools say disagree, the tools are describing the build in front of you.

        Working rules:

        - Start with `SearchBdnd` unless you already know the section or type name you want. It covers the guide, every
          public member, the setup guides and the demo's pages at once, and every hit names the exact follow-up call
          that returns its full text.
        - Every reference tool takes the key of one thing and answers with that thing; leave the key out and it answers
          with the index of what there is - guide sections, public types, source files.
        - Do not write a Bdnd type or member from memory. There is one package and one namespace, `Bit.Bdnd`, and every
          public type is prefixed `Bdnd`; React DnD's hooks and connector refs do not exist here. `GetBdndApi` gives the
          real names, types and default values.
        - Call `GetBdndSetupGuide` before writing wiring. `builder.Services.AddBitBdndServices()` is required - in a
          Blazor Web App in both Program.cs files - there is no stylesheet to link, every draggable and drop target must
          sit inside a `BdndProvider`, and the page must render interactively.
        - A target only reacts to an item whose draggable `Type` is in its `Accept`/`AcceptTypes`; nested targets all
          get `OnDrop`, innermost first; native OS drags arrive through `OnNativeDrop`, never `OnDrop`.
        - To check how a drag plays out - which targets can drop, deep and shallow hover, the order of the drops and
          what `OnEnd` receives - call `InspectBdndDrop`: it runs the drag through the real components.

        Every tool on this server is read-only and cheap: none of them changes anything, anywhere, so there is no reason
        to ration them or to ask before calling one.
        """;
}
