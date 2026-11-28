namespace Bit.Websites.Platform.Client.Shared;

public sealed record ProductLink(string Title, string Description, string Url, bool External = false, bool Disabled = false);

/// <summary>
/// The one list of bit platform products. The header Products menu, the footer and the products
/// page (its Core and Utilities accordions, its Toolings cards and its bit Fabric section) all list exactly these entries,
/// in this order, so adding, renaming or re-linking a product is a single edit. The accordions only
/// add their long-form copy on top, matched by URL, and fall back to the description here.
/// </summary>
public static class ProductCatalog
{
    public static readonly ProductLink[] Main =
    [
        new("BlazorUI", "Native Blazor UI components", Urls.BlazorUI, External: true),
        new("Butil", "Blazor utils for browser APIs", Urls.Butil, External: true),
        new("Bswup", "Blazor PWA on steroids", Urls.Bswup, External: true),
        new("Brouter", "Modern declarative Blazor router", Urls.Brouter, External: true),
    ];

    /// <summary>
    /// The projects under src/Utilities.
    /// </summary>
    public static readonly ProductLink[] Utilities =
    [
        new("Bmotion", "Blazor-native animation library", Urls.Bmotion, External: true),
        new("Bmage", "Blazor-native image processing", Urls.Bmage, External: true),
        new("Bsqlite", "SQLite and EF Core in the browser", Urls.Bsqlite, External: true),
        new("Bink", "Blazor-native drawing and ink", Urls.Bink, External: true),
        new("BbScanner", "Blazor-native barcode scanner", Urls.BbScanner, External: true),
        new("Bdnd", "Declarative drag and drop for Blazor", Urls.Bdnd, External: true),
        new("BlazeDb", "Memory-first database for Blazor", Urls.BlazeDb, External: true),
        new("Blow", "Blazor-native node-based editors", Urls.Blow, External: true),
        new("Borm", "Schema-driven forms for Blazor", Urls.Borm, External: true),
        new("Bcrdt", "Real-time collaborative editing for Blazor", Urls.Bcrdt, External: true),
        new("Bonboarding", "Blazor-native product tours", Urls.Bonboarding, External: true),
        new("Bortable", "Blazor-native sortable lists", Urls.Bortable, External: true),
        new("Blex", "Reactive state management for Blazor and MAUI", Urls.Blex, External: true),
        new("Belemetry", "OpenTelemetry for every Blazor app", Urls.Belemetry, External: true),
        new("Bheadless", "Accessible, unstyled Blazor UI primitives", Urls.Bheadless, External: true),
    ];

    public static readonly ProductLink[] Toolings =
    [
        new("Minifier", "Smaller Blazor WebAssembly downloads", Urls.Minifier),
        new("BlazorES2019", "Blazor apps on older browsers", Urls.BlazorES2019),
        new("Code Analyzers", "Best practices checked as you type", Urls.CodeAnalyzers),
        new("Source Generators", "Less code to write and review", Urls.SourceGenerators),
    ];

    /// <summary>
    /// bit Fabric, which builds whole apps on these libraries, and its CLI. Both live at bitfabric.co.
    /// </summary>
    public static readonly ProductLink[] Fabric =
    [
        new("bit Fabric", "Full apps on these libraries, built with AI", Urls.BitFabric, External: true),
        new("bit CLI", "bit Fabric's command line", Urls.BitFabricCli, External: true),
    ];

    /// <summary>
    /// Every entry above, in order. Declared last, as static fields initialize in declaration order.
    /// </summary>
    public static readonly ProductLink[] All = [.. Main, .. Utilities, .. Toolings, .. Fabric];
}
