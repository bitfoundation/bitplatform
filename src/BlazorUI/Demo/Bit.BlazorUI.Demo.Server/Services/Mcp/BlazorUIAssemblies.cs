using System.Reflection;

namespace Bit.BlazorUI.Demo.Server.Services.Mcp;

/// <summary>One shipped NuGet package: the assembly it carries and the wiring it needs.</summary>
/// <param name="PackageId">The NuGet package id, which is also the <c>_content/</c> folder its static assets are served from.</param>
/// <param name="Assembly">The assembly loaded in this process - what every answer here is read out of.</param>
/// <param name="Registration">The service-registration call the package needs, or null when it registers nothing.</param>
/// <param name="Stylesheet">The one stylesheet the package's components need, or null.</param>
/// <param name="Script">The one script the package's components need, or null.</param>
/// <param name="Required">Whether an app that uses bit BlazorUI at all needs this package.</param>
/// <param name="Summary">What the package is for, in one line.</param>
public sealed record BlazorUIPackage(
    string PackageId,
    Assembly Assembly,
    string? Registration,
    string? Stylesheet,
    string? Script,
    bool Required,
    string Summary)
{
    public string StylesheetTag => $"<link rel=\"stylesheet\" href=\"_content/{PackageId}/styles/{Stylesheet}\" />";

    public string ScriptTag => $"<script src=\"_content/{PackageId}/scripts/{Script}\"></script>";
}

/// <summary>
/// The packages bit BlazorUI ships as, in the order an app adds them.
/// <para>
/// Which package a component comes from is the single most consequential fact about it that its
/// own signature does not carry: a <c>BitDataGrid</c> that will not compile, or a
/// <c>BitIconName</c> that renders an empty box, is nearly always a missing package reference or a
/// missing asset tag rather than a misuse of the component. So the package is resolved from the
/// assembly the type is actually loaded from, and travels with every component this server
/// describes.
/// </para>
/// </summary>
public static class BlazorUIAssemblies
{
    public static BlazorUIPackage Core { get; } = new(
        "Bit.BlazorUI", typeof(BitButton).Assembly,
        "AddBitBlazorUIServices()", "bit.blazorui.css", "bit.blazorui.js", Required: true,
        "Every component under Buttons, Inputs, Layouts, Lists, Navs, Notifications, Progress, Surfaces and Utilities, plus the theming engine.");

    public static BlazorUIPackage Extras { get; } = new(
        "Bit.BlazorUI.Extras", typeof(BitDataGrid<>).Assembly,
        "AddBitBlazorUIExtrasServices()", "bit.blazorui.extras.css", "bit.blazorui.extras.js", Required: false,
        "The heavier components - chart, data grid, map, PDF viewer, the editors, the pro modal and panel - and the Fluent 2, Material and Cupertino theme presets.");

    public static BlazorUIPackage Icons { get; } = new(
        "Bit.BlazorUI.Icons", typeof(BitIconName).Assembly,
        null, "bit.blazorui.icons.css", null, Required: false,
        "The Fabric (MDL2) icon font and the BitIconName constants that name its glyphs.");

    public static BlazorUIPackage Assets { get; } = new(
        "Bit.BlazorUI.Assets", typeof(BitFileVersionProvider).Assembly,
        null, "bit.blazorui.assets.css", null, Required: false,
        "The Segoe UI and Roboto web fonts behind the Fluent and Material type ramps, and the <Script>/<Link> components that fingerprint an asset URL by its content.");

    public static BlazorUIPackage Legacy { get; } = new(
        "Bit.BlazorUI.Legacy", typeof(Bit.BlazorUI.Legacy._Imports).Assembly,
        "AddBitBlazorUILegacyServices()", "bit.blazorui.legacy.css", "bit.blazorui.legacy.js", Required: false,
        "The previous implementations of chart, data grid, the editors and the PDF reader, kept under their own namespace so both generations can live in one app.");

    /// <summary>Every package, in the order an app adds them.</summary>
    public static BlazorUIPackage[] Packages { get; } = [Core, Extras, Icons, Assets, Legacy];

    /// <summary>The assemblies the type and component catalogs read from.</summary>
    public static Assembly[] All { get; } = [.. Packages.Select(p => p.Assembly).Distinct()];

    /// <summary>The version the packages ship as - read from the library rather than written down.</summary>
    public static string Version => Info.Version;

    /// <summary>The package a type is loaded from, or <see cref="Core"/> when it is from none of them.</summary>
    public static BlazorUIPackage Of(Type type) => Packages.FirstOrDefault(p => p.Assembly == type.Assembly) ?? Core;
}
