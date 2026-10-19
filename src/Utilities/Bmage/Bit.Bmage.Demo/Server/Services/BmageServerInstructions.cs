using System.Reflection;

namespace Bit.Bmage.Demo.Server.Services;

/// <summary>
/// The text the server returns as <c>instructions</c> from the <c>initialize</c> handshake.
/// <para>
/// A client puts this in front of the model once, before any tool has been called, which makes it
/// the only place where the server can say something the model reads whether or not it decides to
/// use the tools at all. An agent that never calls <c>GetBmageSetupGuide</c> because it is sure it
/// remembers how image libraries are registered is the failure this server exists to prevent, and
/// no tool description reaches an agent that is not looking. So it stays short - every word is
/// permanently in someone's context window - and spends itself on what Bmage is and the rules that
/// are not visible in any signature.
/// </para>
/// </summary>
public static class BmageServerInstructions
{
    /// <summary>
    /// The build the answers come from, so the text names it rather than claiming to be timeless. The
    /// informational version's build metadata - everything after '+', usually the commit hash - is
    /// dropped: it identifies nothing a caller could act on.
    /// </summary>
    public static readonly string BmageVersion = (
        typeof(ImageBuffer).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(ImageBuffer).Assembly.GetName().Version?.ToString()
        ?? "unknown").Split('+')[0];

    public static readonly string Text = $"""
        Bit.Bmage is image processing for Blazor, written in C#: load, crop, resize, adjust, filter, draw, annotate,
        redact and export images on the user's device, or headlessly on a server. Three packages: Bit.Bmage.Core (the
        engine - pixels, pipelines, managed PNG/JPEG/BMP/GIF codecs, metadata, drawing; no Blazor dependency),
        Bit.Bmage (browser integration - ImageLoader, ImageExporter, the browser's WebP/AVIF codecs, capability probing)
        and Bit.Bmage.Editor (the BmageEditor component).

        Every answer here is read out of Bit.Bmage {BmageVersion} as loaded in this server - the XML documentation of the
        three assemblies, the README and docs/ guides, the demo's sources - or measured by running its engine. Where what
        you remember and what these tools say disagree, the tools describe the build in front of you.

        Working rules:

        - Start with `SearchBmage` unless you already know the type, guide or recipe you want; every hit names the exact
          follow-up call. `GetBmageRecipe` has complete, correct code for the common features (thumbnail, upload
          optimisation, avatar crop with a size budget, stripping GPS, screenshot annotation and redaction, batch
          processing, editor customisation, server-side processing).
        - Call `GetBmageSetupGuide` before writing any wiring. Browser apps register `AddBitBmageServices` - in BOTH
          containers of a Blazor Web App, because prerendering instantiates components on the server. Servers, console
          apps and workers register `AddBitBmageHeadlessServices` instead; it registers `ImageProcessor` and none of the
          browser services. A browser app gets a processor from `ImageProcessorFactory.GetAsync()`, never by injecting
          `ImageProcessor`.
        - Browser features (capabilities, ImageExporter downloads and object URLs, clipboard, paste, remote URLs) run in
          event handlers or `OnAfterRenderAsync`, never `OnInitialized(Async)`: there is no browser during prerendering.
        - Dispose every `ImageBuffer` you are handed (`using var`): they are pooled, and large.
        - WebP and AVIF are browser codecs: export them with `EncodeWithFallbackAsync`, and never on a server.
        - Metadata is removed by default. `MetadataPolicy.Preserve` keeps GPS; use `StripSensitive` to keep camera data.
        - Look up a type with `GetBmageApiDetails` before using it - namespaces are spread over the three packages.
        - Choose formats, qualities and size budgets with `RunBmagePipeline`, which measures them on the real engine,
          and run `ReviewBmageCode` on what you wrote: its rules catch the mistakes that compile cleanly.

        Every tool on this server is read-only and changes nothing anywhere, so there is no reason to ration them or to
        ask before calling one.
        """;
}
