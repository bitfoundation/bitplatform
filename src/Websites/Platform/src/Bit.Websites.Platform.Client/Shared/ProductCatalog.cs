namespace Bit.Websites.Platform.Client.Shared;

public sealed record ProductLink(string Title, string Description, string Url, bool External = false, bool Disabled = false);

/// <summary>
/// The one list of bit platform products, shared by the header Products menu and the products page,
/// so adding, renaming or re-describing an entry is a single edit and the two can never disagree.
/// </summary>
public static class ProductCatalog
{
    public static readonly ProductLink[] Main =
    [
        new("Boilerplate", "Feature-rich project template", Urls.Templates),
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
        new("Balette", "Blazor-native command palette", Urls.Balette, External: true),
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
        new("bit CLI", "Ready-to-run projects in one command", Urls.Cli),
        new("Minifier", "Smaller Blazor WebAssembly downloads", Urls.Minifier),
        new("BlazorES2019", "Blazor apps on older browsers", Urls.BlazorES2019),
        new("Code Analyzers", "Best practices checked as you type", Urls.CodeAnalyzers),
        new("Source Generators", "Less code to write and review", Urls.SourceGenerators),
    ];
}
