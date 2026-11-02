using Boilerplate.WorkbenchTests.Features.Review;

namespace Boilerplate.WorkbenchTests.Features.Flows;

/// <summary>What the flows' browser tests share: a flow drawn the way the agent draws one, its board and the parts on it.</summary>
public abstract class FlowPageTest : ReviewPageTest
{
    protected const string TheArrow = "Switch the theme";

    protected ILocator Cards => Page.Locator(".screen-card");

    protected ILocator Arrows => Page.Locator(".bit-blow-edge");

    protected ILocator ArrowLabels => Page.Locator(".bit-blow-edge-label");

    protected ILocator Presenter => Page.GetByRole(AriaRole.Toolbar, new() { Name = WorkbenchStrings.Present });

    protected ILocator PresentButton => Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.Present, Exact = true });

    protected ILocator DeleteFlowButton => Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.DeleteFlow });

    protected ILocator CardOf(string screenId) => Page.Locator($".bit-blow-node[data-id='{screenId}'] .screen-card");

    protected IFrameLocator Screen(string screenId) => Page.FrameLocator($"iframe[data-screen-id='{screenId}']");

    protected static string ScreenOf(int number, int count) => string.Format(CultureInfo.InvariantCulture, WorkbenchStrings.ScreenOf, number, count);

    /// <summary>Draws a flow of the given screens and arrows, as the agent does, and returns it as saved.</summary>
    protected async Task<WorkbenchFlow> DrawFlow(string name, WorkbenchFlowScreenRequest[] screens, WorkbenchFlowLinkRequest[]? links = null, string? id = null)
    {
        var (flowId, problem) = await Server.Agent.SaveFlow("Test Agent", new(name, screens, links, id, null), CancellationToken.None);

        Assert.IsNull(problem);

        return await FlowById(flowId!);
    }

    /// <summary>A flow of the theme page in light and in dark, with an arrow between them.</summary>
    protected Task<WorkbenchFlow> DrawThemeFlow(string? name = null) => DrawFlow(name ?? Unique("Browsing the theme"),
                                                                                 [new("light", "theme", "?device=iphone-se", "Light", null), new("dark", "theme", "?device=iphone-se&theme=dark", "Dark", null)],
                                                                                 [new("light", "dark", TheArrow, null)]);

    protected async Task<WorkbenchFlow> FlowById(string id) => (await Server.Agent.GetFlows(CancellationToken.None)).Single(f => f.Id == id);

    /// <summary>Waits until the flow, as the server has it, is in the expected state.</summary>
    protected async Task<WorkbenchFlow> WaitForFlow(string id, Func<WorkbenchFlow, bool> condition)
    {
        return (await WorkbenchTestReviewer.WaitFor(() => Server.Agent.GetFlows(CancellationToken.None), flows => flows.Any(f => f.Id == id && condition(f)))).Single(f => f.Id == id);
    }

    protected async Task OpenFlowsPage()
    {
        await Page.GotoAsync(new Uri(Server.Address, WorkbenchUrls.Flows).ToString());

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.Flows, Level = 1 })).ToBeVisibleAsync();
    }

    /// <summary>Opens a flow's board and waits until its screens are on it.</summary>
    protected async Task OpenBoard(string flowId, int screens)
    {
        await Page.GotoAsync(new Uri(Server.Address, WorkbenchUrls.Flow(flowId)).ToString());

        await Expect(Page.Locator("main.board")).ToBeVisibleAsync();
        await Expect(Cards).ToHaveCountAsync(screens);

        if (screens > 0)
        {
            await SettledBoardViewport();
        }
    }

    /// <summary>The board's pan and zoom, which change when it fits another part of the flow into sight.</summary>
    protected Task<string> BoardViewport() => Page.Locator(".bit-blow-viewport").EvaluateAsync<string>("e => e.style.transform");

    /// <summary>Waits until the board has fitted its screens into sight and stays put, as a press on a moving board goes astray.</summary>
    protected async Task<string> SettledBoardViewport()
    {
        var (last, reads) = (string.Empty, 0);

        await WorkbenchTestReviewer.WaitFor(async () =>
        {
            var viewport = await BoardViewport();

            reads = viewport == last && Regex.IsMatch(viewport, @"translate\(0(px)?,\s*0(px)?\)") is false ? reads + 1 : 0;
            last = viewport;

            return reads;
        }, count => count >= 6);

        return last;
    }

    /// <summary>The zoom in a value of <see cref="BoardViewport"/>.</summary>
    protected static double ScaleOf(string viewport) => double.Parse(Regex.Match(viewport, @"scale\(([\d.]+)\)").Groups[1].Value, CultureInfo.InvariantCulture);
}
