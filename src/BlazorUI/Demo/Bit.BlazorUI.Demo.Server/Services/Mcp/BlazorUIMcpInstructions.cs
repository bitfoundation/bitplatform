namespace Bit.BlazorUI.Demo.Server.Services.Mcp;

/// <summary>
/// The server's <c>instructions</c>: the text an MCP client is handed at <c>initialize</c> and
/// keeps in the model's context for the whole session.
/// <para>
/// It is the only place this server gets to speak before it is asked anything, and it is paid for
/// on every request of every session, so it carries exactly two things a per-tool description
/// cannot: which of the seven tools to reach for first, and the handful of facts about this library
/// that decide whether markup that compiles also looks right. Everything else - what a parameter is
/// called, what an enum's values are - is a tool call away and belongs there, where it is only paid
/// for when it is wanted.
/// </para>
/// <para>
/// Because this text is always present, nothing else on the server restates it: there is no
/// "overview" tool re-sending these rules to a model that already has them, and the prompts point
/// at them rather than repeating them. The same test applies in the other direction, and it is the
/// harder one to see - this text and the tool descriptions arrive in the SAME request, so a
/// sentence here explaining what a tool returns is bought twice. What each tool returns is its own
/// description's job; what is left for this is the ORDER, and the rules no single tool owns.
/// </para>
/// <para>
/// The counts are interpolated from the catalogs rather than written down, for the same reason
/// every other number on this server is: a hand-typed "110 components" is wrong the week a new one
/// is added, and it is wrong in the one message that cannot be checked against anything.
/// </para>
/// </summary>
public static class BlazorUIMcpInstructions
{
    public static string Text { get; } =
        $"""
        bit BlazorUI {BlazorUIAssemblies.Version} is a Blazor component library: {BlazorUIComponentCatalog.Components.Length}
        components across {BlazorUIAssemblies.Packages.Length} packages, themed end to end by CSS custom properties.
        These tools answer from the assemblies loaded in this server and from this site's own pages, so they
        describe the version that ships today rather than one recalled from training.

        Call SearchBitBlazorUI first, even when a component name comes to mind: the name a task suggests is
        rarely the name this library chose - a "select" is BitDropdown, a "toast" is BitSnackBar, a "skeleton"
        is BitShimmer, an "expander" is BitAccordion, a "switch" is BitToggle.

        Then, before writing any markup: GetBitBlazorUIComponent for every component you are about to use, and
        GetBitBlazorUIComponentExamples for the one you are least sure of - the examples are the working code
        from the documentation site rather than a sketch of it. GetBitBlazorUISetupGuide is called once per
        app, for its hosting model. The rest answer one thing each: GetBitBlazorUIType for any type a
        signature names, GetBitBlazorUIThemingGuide for anything about colors, tokens or dark mode, and
        FindBitBlazorUIIcons for a glyph. Call GetBitBlazorUIComponent, GetBitBlazorUIType or
        GetBitBlazorUIThemingGuide with no argument to get the list of what each can return, rather than
        looking for a separate tool that lists them.

        Six things hold across the whole library. Apply them without being asked:
        1. Call AddBitBlazorUIServices() in EVERY DI container that renders components - a Blazor Web App
           with an interactive client has two - and put <link href="_content/Bit.BlazorUI/styles/bit.blazorui.css">
           and <script src="_content/Bit.BlazorUI/scripts/bit.blazorui.js"> in the host page. Neither
           omission is a build error: the first throws inside a component, the second renders your app as
           unstyled HTML whose callouts and dropdowns never open.
        2. Reach for a Bit component before writing HTML and CSS by hand. Nearly every layout primitive is
           already here - BitStack, BitGrid, BitCard, BitSeparator, BitSpacer, BitText - and one written by
           hand is one that will not follow the theme.
        3. Style through the API, never with hard-coded values. Variant, Color and Size are enums on almost
           every component; per-part overrides go in its Classes / Styles bag; an app-wide change is a
           --bit-* token. A literal hex color is correct in exactly one theme and wrong in the other three.
        4. An enum parameter takes the enum, not a string: Color="BitColor.Primary", not Color="Primary".
        5. Disabled is IsEnabled="false" (never the native disabled attribute), hidden is
           Visibility="BitVisibility.Hidden" or Collapsed (never display:none) - both are BitComponentBase
           parameters that every component has, and both keep the accessibility behaviour the component
           implements.
        6. Two-way binding uses the @bind- form the component declares (@bind-Value, @bind-IsOpen,
           @bind-SelectedItem). Setting the one-way parameter and handling the change callback yourself
           works, but it is the long way round and it is where value-out-of-sync bugs come from.

        Where a tool cannot resolve an argument it answers with the nearest candidates and the call that
        would list them, so a near miss is worth reading rather than retrying blind.
        """;
}
