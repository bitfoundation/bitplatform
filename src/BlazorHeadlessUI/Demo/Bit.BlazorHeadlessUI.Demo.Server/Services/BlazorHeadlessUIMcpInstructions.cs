namespace Bit.BlazorHeadlessUI.Demo.Server.Services;

/// <summary>
/// The server's <c>instructions</c>: the text an MCP client is handed at <c>initialize</c> and keeps in
/// the model's context for the whole session.
/// <para>
/// It is paid for on every request, so it carries only what no single tool description can: which tool
/// to reach for first, and the few facts about this library that decide whether markup that compiles
/// also works. What each tool returns is its own description's job. The counts are read off the
/// catalog rather than typed, so they cannot go stale.
/// </para>
/// </summary>
public static class BlazorHeadlessUIMcpInstructions
{
    public static string Text { get; } =
        $"""
        bit BlazorHeadlessUI {BlazorHeadlessUIComponentCatalog.Version} is a library of accessible, unstyled Blazor components:
        {BlazorHeadlessUIComponentCatalog.Families.Count(family => family.CssPage is not null)} component families of
        {BlazorHeadlessUIComponentCatalog.Families.Sum(family => family.Parts.Length)} parts that render semantic HTML with the ARIA wiring, keyboard
        interaction and focus management built in, and expose their state as attributes for you to style. These tools
        answer from the assembly loaded in this server and from this site's demo pages.

        Call SearchBitBlazorHeadlessUI first when you do not know the name - ask by what the UI has to do. Before writing
        markup, call GetBitBlazorHeadlessUIComponent for each family you use (its parts only work composed), then
        GetBitBlazorHeadlessUIExamples for working code, in plain CSS or Tailwind. GetBitBlazorHeadlessUIGuide covers
        installation and the concepts that hold across the library.

        Apply these without being asked:
        1. Register builder.Services.AddBitBlazorHeadlessUIServices() in every DI container that renders components (the
           server and, for WebAssembly or Auto, the client) and add @using Bit.BlazorHeadlessUI. There is no script tag
           to add.
        2. A component responds to input only in an interactive render mode; prerendering emits its initial markup.
        3. The library ships no CSS. Style each part through class / style and its state hooks - [data-state="open"],
           [data-disabled], [aria-expanded="true"] ... - which each part's answer lists. A part given an explicit
           display must keep [hidden] working: .panel[hidden] {"{"} display: none; {"}"}.
        4. Keep a family's parts nested as its examples nest them: they share state through cascading contexts.
        5. A stateful part is controlled (@bind-X), uncontrolled (DefaultX) or only notified (OnXChange); content that
           depends on its state goes in its Template, which receives that state.
        """;
}
