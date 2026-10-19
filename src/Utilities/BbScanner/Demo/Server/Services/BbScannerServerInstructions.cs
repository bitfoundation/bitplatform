using System.Reflection;

namespace Bit.BbScanner.Demo.Server.Services;

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
public static class BbScannerServerInstructions
{
    /// <summary>
    /// The build the answers come from, so the text names it rather than claiming to be timeless.
    /// Read off the library assembly rather than written down here: the package version is stamped
    /// into it by Bit.Build.props, so there is no second copy for a release to forget. The
    /// informational version's build metadata - everything after '+', usually the commit hash - is
    /// dropped: it identifies nothing a caller could act on.
    /// </summary>
    public static readonly string BbScannerVersion = (
        typeof(BbBarcodeDecoder).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(BbBarcodeDecoder).Assembly.GetName().Version?.ToString()
        ?? "unknown").Split('+')[0];

    public static readonly string Text = $"""
        Bit.BbScanner is a barcode scanner for Blazor: the `BbScanner` component reads QR Code, Data Matrix, Code 128,
        Code 39, EAN-13, EAN-8, UPC-A, UPC-E and ITF from the camera or from still images, and `BbBarcodeDecoder` decodes
        pixels anywhere .NET runs. The decoding is C#; the browser only opens the camera and copies pixels.

        Every answer here is read out of Bit.BbScanner {BbScannerVersion} as it is loaded in this server - its compiled XML
        documentation, its README, its symbology flags and option defaults, its decoder and the demo's source files. Where
        what you remember and what these tools say disagree, the tools are describing the build in front of you.

        Working rules:

        - Start with `SearchBbScanner` unless you already know the section or type name you want. It covers the guide,
          every public member, the symbologies, the stylesheet's classes and custom properties and the demo sources at
          once, and every hit names the exact follow-up call that returns its full text.
        - Every reference tool takes the key of one thing and answers with that thing; leave the key out and it answers
          with the index of what there is - guide sections, public types, symbologies, source files.
        - Do not write a BbScanner type or member from memory. There is one package and one namespace, `Bit.BbScanner`;
          the component is `BbScanner` and every other public type is prefixed `Bb` (`BbBarcodeFormat`,
          `BbBarcodeResult`, `BbScannerOptions`, `BbBarcodeDecoder`...). `GetBbScannerApi` gives the real names, types and
          default values.
        - Camera scanning runs on WebAssembly only. Call `GetBbScannerSetupGuide` before writing wiring: there are no
          services to register and the stylesheet injects itself, but under Server or Hybrid rendering the component
          reports NotSupported, and the page must be served over HTTPS or from localhost.
        - To see the `BbBarcodeResult` a payload produces - its text, raw bytes, corners, metadata and timings - or to
          check whether a symbol survives rotation or blur, call `InspectBbScannerSymbol`: it runs the library's own decoder.

        Every tool on this server is read-only and cheap: none of them changes anything, anywhere, so there is no reason
        to ration them or to ask before calling one.
        """;
}
