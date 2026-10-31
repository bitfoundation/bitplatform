using System.Reflection;

namespace Bit.Bortable.Demo.Server.Services;

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
public static class BortableServerInstructions
{
    /// <summary>
    /// The build the answers come from, so the text names it rather than claiming to be timeless.
    /// Read off the library assembly rather than written down here: the package version is stamped
    /// into it by Bit.Build.props, so there is no second copy for a release to forget. The
    /// informational version's build metadata - everything after '+', usually the commit hash - is
    /// dropped: it identifies nothing a caller could act on.
    /// </summary>
    public static readonly string BortableVersion = (
        typeof(Bortable<>).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(Bortable<>).Assembly.GetName().Version?.ToString()
        ?? "unknown").Split('+')[0];

    public static readonly string Text = $"""
        Bit.Bortable is a Blazor-native drag-and-drop sortable list, a Blazor counterpart to SortableJS: `Bortable<TItem>`
        reorders a bound `IList<TItem>` live, moves or clones items between lists of a group, and has swap mode,
        multi-drag, drag handles, filters and nested lists. The sorting engine is C# on the browser's native drag
        events; the browser layer only hit-tests, installs handles and animates.

        Every answer here is read out of Bit.Bortable {BortableVersion} as it is loaded in this server - its compiled XML
        documentation, its README, its stylesheet, the component itself and the demo's source files. Where what you
        remember (of SortableJS too) and what these tools say disagree, the tools are describing the build in front of you.

        Working rules:

        - Start with `SearchBortable` unless you already know the section or type name you want. It covers the guide,
          every public member, the setup guides, the theme, the stylesheet's classes and the demo's pages at once, and
          every hit names the exact follow-up call that returns its full text.
        - Every reference tool takes the key of one thing and answers with that thing; leave the key out and it answers
          with the index of what there is - guide sections, public types, theme properties, source files.
        - Do not write a Bortable type or member from memory. There is one package and one namespace, `Bit.Bortable`;
          the component is `Bortable<TItem>` and the rest is `BortablePull`, `BortableDirection`,
          `BortableEventArgs<TItem>` and `AddBitBortableServices`. `GetBortableApi` gives the real names and defaults.
        - Call `GetBortableSetupGuide` before writing wiring: `AddBitBortableServices()` is required - in both projects
          of a Blazor Web App, and left out only by a WebAssembly client that lazy loads Bit.Bortable and sets the
          `Bit.Bortable.Unregistered` runtime switch - the stylesheet link is required, and the lists must render
          interactively.
        - Bind `@bind-Items` to an `IList<TItem>` field holding a mutable list of unique items; the list is changed in
          place. Restyle the drag states through the `--bit-bortable-*` properties `GetBortableTheme` lists.
        - To learn what a drag will do - whether a list takes the item, what lands where, which events fire in which
          order - call `InspectBortableDrag`: it runs the real component over lists you describe.

        Every tool on this server is read-only and cheap: none of them changes anything, anywhere, so there is no reason
        to ration them or to ask before calling one.
        """;
}
