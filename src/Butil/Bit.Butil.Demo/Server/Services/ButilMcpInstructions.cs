namespace Bit.Butil.Demo.Server.Services;

/// <summary>
/// The server's <c>instructions</c>: the text an MCP client is handed at <c>initialize</c> and
/// keeps in the model's context for the whole session.
/// <para>
/// It is the only place this server gets to speak before it is asked anything, and it is paid for
/// on every request of every session, so it carries exactly two things a per-tool description
/// cannot: which of the seven tools to reach for first, and the handful of facts about this library
/// that decide whether code that compiles also runs. Everything else - what a member is called,
/// what an API needs from the page - is a tool call away and belongs there instead, where it is
/// only paid for when it is wanted.
/// </para>
/// <para>
/// Because this text is always present, nothing else on the server restates it. There is no
/// "overview" tool re-sending the four rules below to a model that already has them, and the
/// prompts point at these rules rather than repeating them - a fact stated twice in one context
/// window costs twice and is believed no harder.
/// </para>
/// <para>
/// The counts are interpolated from the catalogs rather than written down, for the same reason
/// every other number on this server is: a hand-typed "sixty services" is wrong the week a
/// sixty-first is added, and it is wrong in the one message that cannot be checked against
/// anything.
/// </para>
/// </summary>
public static class ButilMcpInstructions
{
    public static string Text { get; } =
        $"""
        Bit.Butil {ButilApiCatalog.DisplayVersion} wraps the browser platform - window, document, storage, crypto,
        media, sensors, workers - as injectable, XML-documented C# services for Blazor. These tools answer
        from the assembly loaded in this server and from this site's own pages, so they describe the version
        that ships today rather than one recalled from training: {ButilApiCatalog.Services.Length} injectable
        services across {ButilCapabilityCatalog.Capabilities.Length} documented browser APIs.

        Start with SearchButil. It covers the guide, the docs pages, every public type and member, the
        browser-support matrix and the demo's sources in one query, and every hit names the exact follow-up
        call. Use it even when a service name comes to mind, because the browser's name for a capability is
        rarely the name the task suggests: "copy some text" is Clipboard.WriteText, "am I online" is
        NetworkInformation, "keep the screen on" is WakeLock.

        Then, before writing any code:
        - GetButilApiDetails for every type you are about to call. The wrappers follow the browser API's own
          naming, so a plausible member name is usually not the real one.
        - PlanButilFeature with the APIs involved - one name or the whole set - to learn what the page has to
          arrange first. This is where the bugs that compile come from.
        - GetButilSetupGuide once per app, for its hosting model - the wiring differs per model and getting it
          wrong is silent.

        The remaining tools read one thing each: GetButilDocsPage, GetButilGuideSection, GetButilSourceFile.
        Call any of them with no argument to get the list of what it can return - the docs index doubles as the
        browser-support matrix - rather than looking for a separate tool that lists them.

        Four things hold for every Butil call. Apply them without being asked:
        1. Touch the browser from OnAfterRenderAsync or an event handler, never OnInitializedAsync. While the
           app prerenders there is no JS runtime, so reads return safe defaults and void calls are no-ops:
           misplaced code does not throw, it quietly does nothing.
        2. Call AddBitButilServices() in EVERY DI container that renders components - a Blazor Web App with an
           interactive client has two - and put <script src="_content/Bit.Butil/bit-butil.js"> in the host page
           BEFORE the Blazor script.
        3. Dispose every ButilSubscription and every handle. They hold a listener, a lock, or hardware.
        4. A denied permission or a dismissed picker comes back as false/null rather than as an exception.
           Treat it as a branch and show the user something.

        Every tool here is read-only and answers only from this build - none of them reaches the internet, and
        none of them changes anything. Where a tool cannot resolve an argument it answers with the nearest
        candidates and the call that would list them, so a near miss is worth reading rather than retrying blind.
        """;
}
