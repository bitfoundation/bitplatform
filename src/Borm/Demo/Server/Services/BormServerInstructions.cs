using System.Reflection;

namespace Bit.Borm.Demo.Server.Services;

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
public static class BormServerInstructions
{
    /// <summary>
    /// The build the answers come from, so the text names it rather than claiming to be timeless.
    /// Read off the library assembly rather than written down here: the package version is stamped
    /// into it by Bit.Build.props, so there is no second copy for a release to forget. The
    /// informational version's build metadata - everything after '+', usually the commit hash - is
    /// dropped: it identifies nothing a caller could act on.
    /// </summary>
    public static readonly string BormVersion = (
        typeof(BormView).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(BormView).Assembly.GetName().Version?.ToString()
        ?? "unknown").Split('+')[0];

    public static readonly string Text = $"""
        Bit.Borm is a schema-driven form engine for Blazor: describe a form once - from a C# type and its DataAnnotations
        (`BormSchemaGenerator`), with a fluent builder (`BormBuilder.For<T>()`) or as a JSON Schema document
        (`BormJsonSchemaImporter`) - and `BormView` renders it, validates it and keeps its state (`BormState`): conditional
        visibility, requiredness and read-only fields, computed values, cascading options, wizards and repeaters. Every
        decision runs in C#; there is no JavaScript.

        Every answer here is read out of Bit.Borm {BormVersion} as it is loaded in this server - its compiled XML
        documentation, its README, its stylesheet, its JSON Schema importer and exporter and the demo's source files. Where
        what you remember and what these tools say disagree, the tools are describing the build in front of you.

        Working rules:

        - Start with `SearchBorm` unless you already know the section or type name you want. It covers the guide, every
          public member, the setup guides, the theme, the stylesheet and the demo's working forms at once, and every hit
          names the exact follow-up call that returns its full text.
        - Every reference tool takes the key of one thing and answers with that thing; leave the key out and it answers
          with the index of what there is - guide sections, public types, theme properties, source files.
        - Do not write a Borm type or member from memory. There is one namespace, `Bit.Borm`, and every public type is
          prefixed `Borm` (`BormView`, `BormDefinition`, `BormFieldDefinition`, `BormState`, `BormBuilder`...); `GetBormApi`
          gives the real names, signatures and default values. FluentValidation support is the separate
          `Bit.Borm.FluentValidation` package.
        - Call `GetBormSetupGuide` before writing wiring: `AddBorm()` is required in every container that renders a form
          (both of a Blazor Web App's), the stylesheet link is required, and the page must render interactively.
        - Restyle through the `--bit-borm-*` properties `GetBormTheme` lists, overriding them on `.bit-borm-form`.
        - To check a JSON Schema - what form it becomes, what `Validate()` reports, what survives export - call
          `InspectBormSchema`: it runs the document through the library's own importer, checks and exporter.

        Every tool on this server is read-only and cheap: none of them changes anything, anywhere, so there is no reason
        to ration them or to ask before calling one.
        """;
}
