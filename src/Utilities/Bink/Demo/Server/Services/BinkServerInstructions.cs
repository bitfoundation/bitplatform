using System.Reflection;

namespace Bit.Bink.Demo.Server.Services;

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
public static class BinkServerInstructions
{
    /// <summary>
    /// The build the answers come from, so the text names it rather than claiming to be timeless.
    /// Read off the library assembly rather than written down here: the package version is stamped
    /// into it by Bit.Build.props, so there is no second copy for a release to forget. The
    /// informational version's build metadata - everything after '+', usually the commit hash - is
    /// dropped: it identifies nothing a caller could act on.
    /// </summary>
    public static readonly string BinkVersion = (
        typeof(BinkDocument).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(BinkDocument).Assembly.GetName().Version?.ToString()
        ?? "unknown").Split('+')[0];

    public static readonly string Text = $"""
        Bit.Bink is a drawing and ink library for Blazor: `BinkCanvas` with pressure-aware strokes, brushes, erasers,
        selection, layers, shapes and annotations, undo/redo, a versioned JSON document format, SVG and PNG export and
        real-time collaboration. The document model and the drawing engine are C#; the browser only captures input and paints.

        Every answer here is read out of Bit.Bink {BinkVersion} as it is loaded in this server - its compiled XML
        documentation, its README, its brush registry, its serializer and exporters, and the demo's source files. Where
        what you remember and what these tools say disagree, the tools are describing the build in front of you.

        Working rules:

        - Start with `SearchBink` unless you already know the section or type name you want. It covers the guide, every
          public member, the built-in brushes, the stylesheet's classes and the demo sources at once, and every hit
          names the exact follow-up call that returns its full text.
        - Every reference tool takes the key of one thing and answers with that thing; leave the key out and it answers
          with the index of what there is - guide sections, public types, brushes, source files.
        - Do not write a Bink type or member from memory. There is one package and one namespace, `Bit.Bink`, and every
          public type is prefixed `Bink` (`BinkCanvas`, `BinkDocument`, `BinkPenBrush`, `BinkSerializer`...);
          `GetBinkApi` gives the real names, types and default values.
        - Call `GetBinkSetupGuide` before writing wiring. There are no services to register and the stylesheet injects
          itself, but where the canvas may live and what changes under Blazor Server differ per render mode.
        - Brushes are immutable records: derive variants with `with`, and read the defaults from `GetBinkBrushes`.
        - To check a saved document, or to see the JSON format, call `InspectBinkDocument` - it runs the library's own
          serializer and exporters on it.

        Every tool on this server is read-only and cheap: none of them changes anything, anywhere, so there is no reason
        to ration them or to ask before calling one.
        """;
}
